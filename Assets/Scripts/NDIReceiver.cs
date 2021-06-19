using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;

namespace NDISample
{
    public class NDIReceiver : MonoBehaviour
    {
        [field: SerializeField] private String ReceiveName;
        [SerializeField] private bool _videoEnabled = false;
        [SerializeField] private bool _audioEnabled = false;
        [SerializeField] private RawImage _image = null;

        [Description("Does the current source support PTZ functionality?")]
        public bool IsPtz
        {
            get => _isPtz;
            set => _isPtz = value;
        }

        public bool IsRecordingSupported
        {
            get => _canRecord;
            set => _canRecord = value;
        }

        private String _receiveName = "";
        private IntPtr _recvInstancePtr = IntPtr.Zero;

        private Thread _receiveThread = null;
        private bool _exitThread = false;

        private bool _isPtz = false;
        private bool _canRecord = false;

        private SynchronizationContext _mainThreadContext = null;

        private Texture2D _texture = null;

        private void Awake()
        {
            if (!NDIlib.Initialize())
            {
                Debug.Log("Cannot run NDI.");
            }
            else
            {
                Debug.Log("Initialized NDI.");

                _mainThreadContext = SynchronizationContext.Current;
            }
        }

        private void OnDestroy()
        {
            Disconnect();
        }

        public void Connect(NDIlib.Source source)
        {
            Disconnect();

            if (source == null || String.IsNullOrEmpty(source.Name))
            {
                return;
            }

            if (String.IsNullOrEmpty(ReceiveName))
            {
                throw new ArgumentException($"{nameof(ReceiveName)} can not be null or empty.");
            }

            NDIlib.source_t source_t = new NDIlib.source_t()
            {
                p_ndi_name = NDIlib.StringToUtf8(source.Name),
            };

            NDIlib.recv_create_v3_t recvDescription = new NDIlib.recv_create_v3_t()
            {
                source_to_connect_to = source_t,
                color_format = NDIlib.recv_color_format_e.recv_color_format_BGRX_BGRA,
                bandwidth = NDIlib.recv_bandwidth_e.recv_bandwidth_highest,
                allow_video_fields = false,
                p_ndi_recv_name = NDIlib.StringToUtf8(ReceiveName),
            };

            // Create a new instance connected to this source.
            _recvInstancePtr = NDIlib.recv_create_v3(ref recvDescription);

            Marshal.FreeHGlobal(source_t.p_ndi_name);
            Marshal.FreeHGlobal(recvDescription.p_ndi_recv_name);

            // Did it work?
            System.Diagnostics.Debug.Assert(_recvInstancePtr != IntPtr.Zero, "Failed to create NDI receive instance.");

            if (_recvInstancePtr == IntPtr.Zero)
            {
                return;
            }

            // We are now going to mark this source as being on program output for tally purposes (but not on preview)
            SetTallyIndicators(true, false);

            // Start up a thread to receive on
            _receiveThread = new Thread(ReceiveThreadProc) {IsBackground = true, Name = "NdiExampleReceiveThread"};
            _receiveThread.Start();
        }

        public void Disconnect()
        {
            SetTallyIndicators(false, false);

            // check for a running thread
            if (_receiveThread != null)
            {
                // tell it to exit
                _exitThread = true;

                // wait for it to end
                _receiveThread.Join();
            }

            // Reset thread defaults
            _receiveThread = null;
            _exitThread = false;

            // Destroy the receiver
            NDIlib.recv_destroy(_recvInstancePtr);

            // set function status to defaults
            IsPtz = false;
            IsRecordingSupported = false;
        }

        private void SetTallyIndicators(bool onProgram, bool onPreview)
        {
            if (_recvInstancePtr == IntPtr.Zero)
            {
                return;
            }

            NDIlib.tally_t tallyState = new NDIlib.tally_t()
            {
                on_program = onProgram,
                on_preview = onPreview,
            };

            NDIlib.recv_set_tally(_recvInstancePtr, ref tallyState);
        }

        private void ReceiveThreadProc()
        {
            while (!_exitThread && _recvInstancePtr != IntPtr.Zero)
            {
                // The descriptors.
                NDIlib.video_frame_v2_t videoFrame = new NDIlib.video_frame_v2_t();
                NDIlib.audio_frame_v2_t audioFrame = new NDIlib.audio_frame_v2_t();
                NDIlib.metadata_frame_t metadataFrame = new NDIlib.metadata_frame_t();

                switch (NDIlib.recv_capture_v2(_recvInstancePtr, ref videoFrame, ref audioFrame, ref metadataFrame, 500))
                {
                    // No data.
                    case NDIlib.frame_type_e.frame_type_none:
                        // No data received
                        break;

                    // Frame settings - check for extended functionality.
                    case NDIlib.frame_type_e.frame_type_status_change:
                        // check for PTZ
                        IsPtz = NDIlib.recv_ptz_is_supported(_recvInstancePtr);
                        IsRecordingSupported = NDIlib.recv_recording_is_supported(_recvInstancePtr);
                        break;

                    case NDIlib.frame_type_e.frame_type_video:
                        // If not enabled, just discard
                        // this can also occasionally happen when changing sources.
                        if (!_videoEnabled || videoFrame.p_data == IntPtr.Zero)
                        {
                            // always free received frames.
                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                            break;
                        }

                        // We need to be on the UI thread to write to our texture.
                        // Not very efficient, but this is just an example.
                        _mainThreadContext.Post(d =>
                        {
                            // get all our info so that we can free the frame
                            int yres = videoFrame.yres;
                            int xres = videoFrame.xres;

                            int stride = videoFrame.line_stride_in_bytes;
                            int bufferSize = yres * stride;

                            if (_texture == null)
                            {
                                _texture = new Texture2D(xres, yres, TextureFormat.BGRA32, false);
                                _image.texture = _texture;
                            }

                            _texture.LoadRawTextureData(videoFrame.p_data, bufferSize);
                            _texture.Apply();

                            NDIlib.recv_free_video_v2(_recvInstancePtr, ref videoFrame);
                        }, null);

                        break;

                    // Audio is beyond the scope of this example
                    case NDIlib.frame_type_e.frame_type_audio:
                        // if not audio or disabled, nothing to do.
                        if (!_audioEnabled || audioFrame.p_data == IntPtr.Zero || audioFrame.no_samples == 0)
                        {
                            // Always free received frames.
                            NDIlib.recv_free_audio_v2(_recvInstancePtr, ref audioFrame);
                            break;
                        }

                        // if the audio format changed, we need to reconfigure the audio device.
                        bool formatChanged = false;

                        // make sure our format has been created and matches the incomming audio.
                        // NOTE: Setup WaveAudio that is defined in NAudio.dll.

                        NDIlib.recv_free_audio_v2(_recvInstancePtr, ref audioFrame);
                        break;

                    // Metadata
                    case NDIlib.frame_type_e.frame_type_metadata:
                        // UTF-8 strings must be converted for use - length includes the terminating zero
                        // String metadata = Utf8ToString(metadataFrame.p_data, metadataFrame.length - 1);
                        // System.Diagnotics.Debug.Print(metadata);

                        // free frames that were received.
                        NDIlib.recv_free_metadata(_recvInstancePtr, ref metadataFrame);
                        break;
                }
            }
        }
    }
}