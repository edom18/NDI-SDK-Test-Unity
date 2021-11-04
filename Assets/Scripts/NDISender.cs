using System;
using System.Collections;
using System.Runtime.InteropServices;
using Klak.Ndi;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDISample
{
    public class NDISender : MonoBehaviour
    {
        [SerializeField] private string _ndiName;
        [SerializeField] private ComputeShader _encodeCompute;

        private IntPtr _sendInstance;
        private FormatConverter _formatConverter;

        private void Start()
        {
            _formatConverter = new FormatConverter(_encodeCompute);

            IntPtr nname = Marshal.StringToHGlobalAnsi(_ndiName);
            NDIlib.SendSettings sendSettings = new NDIlib.SendSettings { NdiName = nname };
            _sendInstance = NDIlib.send_create(sendSettings);
            Marshal.FreeHGlobal(nname);

            if (!NDIlib.Initialize())
            {
                Debug.Log("NDIlib can't be initialized.");
                return;
            }

            if (_sendInstance == IntPtr.Zero)
            {
                Debug.LogError("NDI can't create a send instance.");
                return;
            }
        }

        private IEnumerator CaptureCoroutine()
        {
            for (var eof = new WaitForEndOfFrame(); true;)
            {
                yield return eof;

                ComputeBuffer converted = Capture();
                if (converted == null) continue;

                AsyncGPUReadback.Request(converted, OnReadback);
            }
        }

        private ComputeBuffer Capture()
        {
            var tempRT = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);

            ScreenCapture.CaptureScreenshotIntoRenderTexture(tempRT);

            ComputeBuffer converted = _formatConverter.Encode(tempRT, false, false);

            RenderTexture.ReleaseTemporary(tempRT);

            return converted;
        }

        unsafe void OnReadback(AsyncGPUReadbackRequest request)
        {
            // Ignore errors.
            if (request.hasError) return;

            // Ignore it if the NDI object has been already disposed.
            if (_sendInstance == null || _sendInstance == IntPtr.Zero) return;

            // Readback data retrieval
            var data = request.GetData<byte>();
            var pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(data);

            // Data size verification
            if (data.Length / sizeof(uint) !=
                FrameDataCount(_width, _height, _enableAlpha)) return;

            // Frame data setup
            var frame = new NDIlib.video_frame_v2_t
            {
                xres = _width, yres = _height, line_stride_in_bytes = _width * 2,
                FourCC = NDIlib.FourCC_type_e.FourCC_type_UYVY,
                frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                p_data = (System.IntPtr)pdata, p_metadata = IntPtr.Zero,
            };

            // Send via NDI
            NDIlib.send_send_video_async_v2(_sendInstance, frame);
        }
    }
}