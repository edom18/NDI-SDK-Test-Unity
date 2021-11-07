using UnityEngine;

namespace NDIPlugin
{
    public class Texture2DSource : MonoBehaviour, IFrameTextureSource
    {
        [SerializeField] private Texture2D _texture;

        public bool IsReady => _texture != null;
        
        public Texture GetTexture() => _texture;
    }
}