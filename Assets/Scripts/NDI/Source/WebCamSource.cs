using UnityEngine;

namespace NDIPlugin
{
    public class WebCamSource : MonoBehaviour, IFrameTextureSource
    {
        private WebCamTexture _webCam;

        public bool IsReady => _webCam.isPlaying;
        
        public Texture GetTexture() => _webCam;

        private void Awake()
        {
            _webCam = new WebCamTexture();
            _webCam.Play();
        }

        private void OnDestroy()
        {
            if (_webCam != null)
            {
                _webCam.Stop();
            }
        }
    }
}