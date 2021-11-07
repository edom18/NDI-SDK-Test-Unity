using System;
using NRKernal;
using UnityEngine;

namespace NDIPlugin
{
    public class NrealCameraSource : MonoBehaviour, IFrameTextureSource
    {
        [SerializeField] private Camera _targetCamera;
        [SerializeField] private Shader _shdaer;

        private Material _material;
        private NRRGBCamTexture _rgbCamTexture;
        private RenderTexture _cameraTargetRenderTexture;
        private RenderTexture _renderTexture;

        public bool IsReady => _rgbCamTexture.IsPlaying;

        public Texture GetTexture()
        {
            Render();
            
            return _renderTexture;
        }

        private void Start()
        {
            _material = new Material(_shdaer);
            _rgbCamTexture = new NRRGBCamTexture();
            _rgbCamTexture.Play();
        }

        private void OnDestroy()
        {
            ReleaseObjects();
        }

        private void Render()
        {
            Texture2D nrealTex = _rgbCamTexture.GetTexture();
            
            if (_cameraTargetRenderTexture == null)
            {
                Debug.Log($">>>>>>>> NRRGBCamera texture size : {nrealTex.width} - {nrealTex.height}");
                
                _cameraTargetRenderTexture = new RenderTexture(nrealTex.width, nrealTex.height, 0);
                _cameraTargetRenderTexture.Create();
                _targetCamera.enabled = false;
                _targetCamera.targetTexture = _cameraTargetRenderTexture;

                _renderTexture = new RenderTexture(nrealTex.width, nrealTex.height, 0);
                _renderTexture.Create();
            }

            _targetCamera.Render();
            
            _material.SetTexture("_BcakGroundTex", nrealTex);
            _material.SetTexture("_MainTex", _cameraTargetRenderTexture);
            Graphics.Blit(null, _renderTexture, _material);
        }

        private void ReleaseObjects()
        {
            if (_rgbCamTexture != null)
            {
                _rgbCamTexture.Stop();
                _rgbCamTexture = null;
            }

            if (_cameraTargetRenderTexture != null)
            {
                _cameraTargetRenderTexture.Release();
                _cameraTargetRenderTexture = null;
            }

            if (_renderTexture != null)
            {
                _renderTexture.Release();
                _renderTexture = null;
            }

            if (_material != null)
            {
                Destroy(_material);
                _material = null;
            }
        }
    }
}