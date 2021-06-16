using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BitmapTest : MonoBehaviour
{
    [SerializeField] private RawImage _image = null;
    private Texture2D _tex = null;
    
    private void Start()
    {
        int w = 640;
        int h = 480;
        
        byte[] r = BitConverter.GetBytes(1f);
        byte[] g = BitConverter.GetBytes(0f);
        byte[] b = BitConverter.GetBytes(0f);
        byte[] a = BitConverter.GetBytes(1f);

        _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        // Texture2D t = new Texture2D(16, 8, TextureFormat.RGBA32, false);
        //
        // Color[] colors = new Color[16 * 8];
        // for (int i = 0; i < colors.Length; i++)
        // {
        //     colors[i] = new Color(1, 0, 0, 0);
        // }
        // t.SetPixels(colors);
        // byte[] d = t.GetRawTextureData();
        
        byte[] buffer = new byte[w * h * 4];
        for (int i = 0; i < buffer.Length; i += 4)
        {
            buffer[i + 0] = 255;
            buffer[i + 1] = 0;
            buffer[i + 2] = 0;
            buffer[i + 3] = 255;
            // Array.Copy(r, 0, buffer, i + 0, r.Length);
            // Array.Copy(g, 0, buffer, i + 4, g.Length);
            // Array.Copy(b, 0, buffer, i + 8, b.Length);
            // Array.Copy(a, 0, buffer, i + 16, a.Length);
        }
        
        // Color[] colors = new Color[w * h];
        // for (int i = 0; i < colors.Length; i++)
        // {
        //     colors[i] = new Color(1, 0, 0, 0);
        // }
        // _tex.SetPixels(colors);
        // byte[] d = _tex.GetRawTextureData();

        _tex.LoadRawTextureData(buffer);
        _tex.Apply();

        _image.texture = _tex;
    }
}
