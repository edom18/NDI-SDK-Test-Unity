using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NDIPlugin
{
    public static class Utils
    {
        public static int FrameDataCount(int width, int height, bool alpha)
            => width * height * (alpha ? 3 : 2) / 4;
    }
}