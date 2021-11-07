using System;
using UnityEngine;

namespace NDIPlugin
{
    public class CameraSource : MonoBehaviour, IFrameTextureSource
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private int _width = 256;
        [SerializeField] private int _height = 256;

        private RenderTexture _renderTexture;

        public bool IsReady => _renderTexture != null;
        
        public Texture GetTexture() => _renderTexture;

        private void Awake()
        {
            _renderTexture = new RenderTexture(_width, _height, 1);
            _renderTexture.Create();

            _targetCamera.targetTexture = _renderTexture;
        }

        private void OnDestroy()
        {
            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }
        }
    }
}