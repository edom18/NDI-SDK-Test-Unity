using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NDIPlugin
{
    public interface IFrameTextureSource
    {
        bool IsReady { get; }
        Texture GetTexture();
    }
}