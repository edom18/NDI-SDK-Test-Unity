using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class IntPtrTest : MonoBehaviour
{
    private void Start()
    {
        string stringA = "I seem to be turned around!";
        int copyLen = stringA.Length;

        IntPtr sptr = Marshal.StringToHGlobalAnsi(stringA);
        IntPtr dptr = Marshal.AllocHGlobal(copyLen + 1);

        unsafe
        {
            byte* src = (byte*) sptr.ToPointer();
            byte* dst = (byte*) dptr.ToPointer();

            if (copyLen > 0)
            {
                src += copyLen - 1;

                while (copyLen-- > 0)
                {
                    *dst++ = *src--;
                }

                *dst = 0;
            }
        }

        string stringB = Marshal.PtrToStringAnsi(dptr);

        Debug.Log(stringA);
        Debug.Log(stringB);

        Marshal.FreeHGlobal(dptr);
        Marshal.FreeHGlobal(sptr);
    }
}