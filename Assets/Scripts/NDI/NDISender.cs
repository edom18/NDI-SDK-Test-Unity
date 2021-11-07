using System;
using System.Collections;
using System.Runtime.InteropServices;
using NRKernal;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace NDIPlugin
{
    public class NDISender : MonoBehaviour
    {
        [SerializeField] private string _ndiName;
        [SerializeField] private ComputeShader _encodeCompute;
        [SerializeField] private bool _enableAlpha = false;
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Shader _shdaer;

        [SerializeField] private RawImage _preview;
        [SerializeField] private Texture2D _dummyTexture;

        private IntPtr _sendInstance;
        private FormatConverter _formatConverter;
        private int _width;
        private int _height;

        private Material _material;
        private NRRGBCamTexture _rgbCamTexture;
        private RenderTexture _renderTexture;
        private RenderTexture _cameraTargetRenderTexture;
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

            _material = new Material(_shdaer);

            _rgbCamTexture = new NRRGBCamTexture();
            _rgbCamTexture.Play();

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

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }

            if (_cameraTargetRenderTexture != null)
            {
                _cameraTargetRenderTexture.Release();
                _cameraTargetRenderTexture = null;
            }

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }

        private IEnumerator CaptureCoroutine()
        {
            for (var eof = new WaitForEndOfFrame(); true;)
            {
                yield return eof;

                ComputeBuffer converted = Capture();
                if (converted == null) continue;

                Send(converted);
            }
        }

        private ComputeBuffer Capture()
        {
#if !UNITY_EDITOR && UNITY_ANDROID
            bool vflip = true;
#else
            bool vflip = false;
#endif
            if (!_rgbCamTexture.IsPlaying) return null;

            Texture2D texture = _rgbCamTexture.GetTexture();

            if (_renderTexture == null)
            {
                Debug.Log($">>>>>>>> NRRGBCamera texture size : {texture.width} - {texture.height}");
                
                int width = texture.width / 4;
                int height = texture.height / 4;
                _renderTexture = new RenderTexture(width, height, 0);
                _renderTexture.Create();
                _preview.texture = _renderTexture;

                _cameraTargetRenderTexture = new RenderTexture(width, height, 0);
                _cameraTargetRenderTexture.Create();
                _targetCamera.enabled = false;
                _targetCamera.targetTexture = _cameraTargetRenderTexture;
            }
            
            _width = _renderTexture.width;
            _height = _renderTexture.height;
            
            _targetCamera.Render();
            _material.SetTexture("_BcakGroundTex", _dummyTexture);
            _material.SetTexture("_MainTex", _cameraTargetRenderTexture);
            Graphics.Blit(null, _renderTexture, _material);
            ComputeBuffer converted = _formatConverter.Encode(_renderTexture, _enableAlpha, vflip);

            return converted;
        }

        private unsafe void Send(ComputeBuffer buffer)
        {
            if (_nativeArray == null)
            {
                int length = Utils.FrameDataCount(_width, _height, _enableAlpha) * 4;
                _nativeArray = new NativeArray<byte>(length, Allocator.Persistent);

                _bytes = new byte[length];
            }

            buffer.GetData(_bytes);
            _nativeArray.Value.CopyFrom(_bytes);

            void* pdata = NativeArrayUnsafeUtility.GetUnsafeReadOnlyPtr(_nativeArray.Value);

            // Data size verification
            if (_nativeArray.Value.Length / sizeof(uint) != Utils.FrameDataCount(_width, _height, _enableAlpha))
            {
                return;
            }

            // Frame data setup
            var frame = new NDIlib.video_frame_v2_t
            {
                xres = _width,
                yres = _height,
                line_stride_in_bytes = _width * 2,
                FourCC = _enableAlpha ? NDIlib.FourCC_type_e.FourCC_type_UYVA : NDIlib.FourCC_type_e.FourCC_type_UYVY,
                frame_format_type = NDIlib.frame_format_type_e.frame_format_type_progressive,
                p_data = (IntPtr)pdata,
                p_metadata = IntPtr.Zero,
            };

            // Send via NDI
            NDIlib.send_send_video_async_v2(_sendInstance, frame);
        }
    }
}