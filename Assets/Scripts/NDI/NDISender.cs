using System;
using System.Collections;
using System.Runtime.InteropServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace NDIPlugin
{
    public class NDISender : MonoBehaviour
    {
        [SerializeField] private string _ndiName;
        [SerializeField] private ComputeShader _encodeCompute;
        [SerializeField] private ComputeShader _computeShader;
        [SerializeField] private bool _enableAlpha = false;

        private IntPtr _sendInstance;
        private FormatConverter _formatConverter;
        private int _width;
        private int _height;

        private NativeArray<byte>? _nativeArray;
        private byte[] _bytes;

        private void Start()
        {
            WifiManager.Instance.SetupNetwork();

            if (!NDIlib.Initialize())
            {
                Debug.Log("NDIlib can't be initialized.");
                return;
            }

            _formatConverter = new FormatConverter(_encodeCompute);

            IntPtr nname = Marshal.StringToHGlobalAnsi(_ndiName);
            NDIlib.SendSettings sendSettings = new NDIlib.SendSettings { NdiName = nname };
            _sendInstance = NDIlib.send_create(sendSettings);
            Marshal.FreeHGlobal(nname);

            if (_sendInstance == IntPtr.Zero)
            {
                Debug.LogError("NDI can't create a send instance.");
                return;
            }

            StartCoroutine(CaptureCoroutine());
        }

        private void OnDestroy()
        {
            ReleaseInternalObjects();
        }

        private void ReleaseInternalObjects()
        {
            if (_sendInstance != IntPtr.Zero)
            {
                NDIlib.send_destroy(_sendInstance);
                _sendInstance = IntPtr.Zero;
            }

            if (_nativeArray != null)
            {
                _nativeArray.Value.Dispose();
                _nativeArray = null;
            }
            
            _encoderOutput?.Release();
        }

        private IEnumerator CaptureCoroutine()
        {
            for (var eof = new WaitForEndOfFrame(); true;)
            {
                yield return eof;

                ComputeBuffer converted = Capture();
                if (converted == null) continue;

                // AsyncGPUReadback.Request(converted, OnReadback);

                Send(converted);
            }
        }

        private ComputeBuffer Capture()
        {
            _width = Screen.width;
            _height = Screen.height;

            RenderTexture tempRT = RenderTexture.GetTemporary(_width, _height, 0);

            ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);

#if !UNITY_EDITOR && UNITY_ANDROID
            bool vflip = true;
#else
            bool vflip = false;
#endif
            // ComputeBuffer converted = _formatConverter.Encode(tempRT, _enableAlpha, vflip);
            ComputeBuffer converted = Encode(tempRT, _enableAlpha, vflip);
            RenderTexture.ReleaseTemporary(tempRT);

            return converted;
        }

        private ComputeBuffer _encoderOutput;

        private ComputeBuffer Encode(Texture source, bool enableAlpha, bool vflip)
        {
            int width = source.width;
            int height = source.height;
            int dataCount = _width * _height;

            // Reallocate the output buffer when the output size was changed.
            if (_encoderOutput != null && _encoderOutput.count != dataCount)
            {
                _encoderOutput?.Dispose();
            }

            // Output buffer allocation
            if (_encoderOutput == null)
            {
                _encoderOutput = new ComputeBuffer(dataCount, 4);
            }

            // Compute thread dispatching
            // int pass = enableAlpha ? 1 : 0;
            // _computeShader.SetInt("VFlip", vflip ? -1 : 1);
            _computeShader.SetTexture(0, "Source", source);
            _computeShader.SetBuffer(0, "Destination", _encoderOutput);
            _computeShader.Dispatch(0, width / 8, height / 8, 1);

            return _encoderOutput;
        }

        private unsafe void Send(ComputeBuffer buffer)
        {
            if (_nativeArray == null)
            {
                int length = _width * _height * 4;
                _nativeArray = new NativeArray<byte>(length, Allocator.Persistent);

                _bytes = new byte[length];
            }

            buffer.GetData(_bytes);
            _nativeArray.Value.CopyFrom(_bytes);

            void* pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(_nativeArray.Value);

            // Data size verification
            // if (_nativeArray.Value.Length / sizeof(uint) != Utils.FrameDataCount(_width, _height, _enableAlpha))
            // {
            //     return;
            // }

            // Frame data setup
            var frame = new NDIlib.video_frame_v2_t
            {
                xres = _width,
                yres = _height,
                line_stride_in_bytes = _width * 4,
                FourCC = NDIlib.FourCC_type_e.FourCC_type_RGBA,
                frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                p_data = (IntPtr)pdata,
                p_metadata = IntPtr.Zero,
            };

            // Send via NDI
            NDIlib.send_send_video_async_v2(_sendInstance, frame);
        }

        // private unsafe void Send(ComputeBuffer buffer)
        // {
        //     if (_nativeArray == null)
        //     {
        //         int length = Utils.FrameDataCount(_width, _height, _enableAlpha) * 4;
        //         _nativeArray = new NativeArray<byte>(length, Allocator.Persistent);
        //
        //         _bytes = new byte[length];
        //     }
        //
        //     buffer.GetData(_bytes);
        //     _nativeArray.Value.CopyFrom(_bytes);
        //
        //     void* pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(_nativeArray.Value);
        //
        //     // Data size verification
        //     if (_nativeArray.Value.Length / sizeof(uint) != Utils.FrameDataCount(_width, _height, _enableAlpha))
        //     {
        //         return;
        //     }
        //
        //     // Frame data setup
        //     var frame = new NDIlib.video_frame_v2_t
        //     {
        //         xres = _width,
        //         yres = _height,
        //         line_stride_in_bytes = _width * 2,
        //         FourCC = NDIlib.FourCC_type_e.FourCC_type_UYVY,
        //         frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
        //         p_data = (IntPtr)pdata,
        //         p_metadata = IntPtr.Zero,
        //     };
        //
        //     // Send via NDI
        //     NDIlib.send_send_video_async_v2(_sendInstance, frame);
        // }

        // private unsafe void OnReadback(AsyncGPUReadbackRequest request)
        // {
        //     // Ignore errors.
        //     if (request.hasError) return;
        //
        //     // Ignore it if the NDI object has been already disposed.
        //     if (_sendInstance == IntPtr.Zero) return;
        //
        //     // Readback data retrieval
        //     NativeArray<byte> data = request.GetData<byte>();
        //     void* pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);
        //
        //     // Data size verification
        //     if (data.Length / sizeof(uint) != Utils.FrameDataCount(_width, _height, _enableAlpha))
        //     {
        //         return;
        //     }
        //
        //     // Frame data setup
        //     var frame = new NDIlib.video_frame_v2_t
        //     {
        //         xres = _width, yres = _height, line_stride_in_bytes = _width * 2,
        //         FourCC = NDIlib.FourCC_type_e.FourCC_type_UYVY,
        //         frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
        //         p_data = (IntPtr)pdata, p_metadata = IntPtr.Zero,
        //     };
        //
        //     // Send via NDI
        //     NDIlib.send_send_video_async_v2(_sendInstance, frame);
        // }
    }
}