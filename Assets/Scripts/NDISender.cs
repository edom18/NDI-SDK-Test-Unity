using System;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

namespace NDISample
{
    public class NDISender : MonoBehaviour
    {
        [SerializeField] private string _ndiName;

        private IntPtr _sendInstance;

        private void Start()
        {
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

            ComputeBuffer converted = null;

            RenderTexture.ReleaseTemporary(tempRT);

            return converted;
        }

        unsafe void OnReadback(AsyncGPUReadbackRequest request)
        {
            
        }
    }
}