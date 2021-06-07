using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro.EditorUtilities;
using UnityEngine;

public static class NDIlib
{
    [StructLayout(LayoutKind.Sequential)]
    public struct find_create_t
    {
        [MarshalAs(UnmanagedType.U1)] public bool show_local_sources;
        public IntPtr p_groups;
        public IntPtr p_extra_ips;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct source_t
    {
        public IntPtr p_ndi_name;
        public IntPtr p_url_address;
    }

    public class Source
    {
        private String _name = String.Empty;
        private String _computerName = String.Empty;
        private String _sourceName = String.Empty;
        private Uri _uri = null;

        public String Name
        {
            get => _name;
            
            private set
            {
                _name = value;

                int parenIdx = _name.IndexOf(" (");
                _computerName = _name.Substring(0, parenIdx);
                
                _sourceName = Regex.Match(_name, @"(?<=\().+?(?=\))").Value;

                String uriString = String.Format("ndi://{0}/{1}", _computerName, System.Net.WebUtility.UrlEncode(_sourceName));

                if (!Uri.TryCreate(uriString, UriKind.Absolute, out _uri))
                {
                    _uri = null;
                }
            }
        }
        
        public String ComputerName => _computerName;
        public String SourceName => _sourceName;
        public Uri Uri => _uri;

        public override string ToString() => Name;

        public Source()
        {
        }

        public Source(source_t source_t)
        {
            Name = Utf8ToString(source_t.p_ndi_name);
        }

        public Source(String name)
        {
            Name = name;
        }

        public Source(Source previousSource)
        {
            Name = previousSource.Name;
            _uri = previousSource._uri;
        }
    }
    
    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool Initialize();

    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_create_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr find_create_v2(ref find_create_t p_create_setings);

    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern void find_destroy(IntPtr p_instance);

    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_wait_for_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    [return: MarshalAs(UnmanagedType.U1)]
    public static extern bool find_wait_for_sources(IntPtr p_instance, UInt32 timeout_in_ms);

    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_find_get_current_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr find_get_current_sources(IntPtr p_instance, ref UInt32 p_no_sources);

    public static string Utf8ToString(IntPtr nativeUtf8, uint? length = null)
    {
        if (nativeUtf8 == IntPtr.Zero)
        {
            return String.Empty;
        }

        uint len = 0;

        if (length.HasValue)
        {
            len = length.Value;
        }
        else
        {
            while (Marshal.ReadByte(nativeUtf8, (int)len) != 0)
            {
                ++len;
            }
        }

        byte[] buffer = new byte[len];
        
        Marshal.Copy(nativeUtf8, buffer, 0, buffer.Length);

        return Encoding.UTF8.GetString(buffer);
    }
}
