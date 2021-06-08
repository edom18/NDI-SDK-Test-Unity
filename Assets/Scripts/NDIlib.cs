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
    public enum recv_color_format_e
    {
        // No alpha channel: BGRX Alpha channel: BGRA
        recv_color_format_BGRX_BGRA = 0,

        // No alpha channel: UYVY Alpha channel: BGRA
        recv_color_format_UYVY_BGRA = 1,

        // No alpha channel: RGBX Alpha channel: RGBA
        recv_color_format_RGBX_RGBA = 2,

        // No alpha channel: UYVY Alpha channel: RGBA
        recv_color_format_UYVY_RGBA = 3,

        // On Windows there are some APIs that require bottom to top images in RGBA format. Specifying
        // this format will return images in this format. The image data pointer will still point to the
        // "top" of the image, althought he stride will be negative. You can get the "bottom" line of the image
        // using : video_data.p_data + (video_data.yres - 1)*video_data.line_stride_in_bytes
        recv_color_format_BGRX_BGRA_flipped = 200,

        // Read the SDK documentation to understand the pros and cons of this format.
        recv_color_format_fastest = 100,

        // Legacy definitions for backwards compatibility
        recv_color_format_e_BGRX_BGRA = recv_color_format_BGRX_BGRA,
        recv_color_format_e_UYVY_BGRA = recv_color_format_UYVY_BGRA,
        recv_color_format_e_RGBX_RGBA = recv_color_format_RGBX_RGBA,
        recv_color_format_e_UYVY_RGBA = recv_color_format_UYVY_RGBA
    }

    public enum recv_bandwidth_e
    {
        recv_bandwidth_metadata_only = -10,
        recv_bandwidth_audio_only = 10,
        recv_bandwidth_lowest = 0,
        recv_bandwidth_highest = 100,
    }

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

    [StructLayout(LayoutKind.Sequential)]
    public struct recv_create_v3_t
    {
        public source_t source_to_connect_to;
        public recv_color_format_e color_format;
        public recv_bandwidth_e bandwidth;

        [MarshalAs(UnmanagedType.U1)]
        public bool allow_video_fields;

        public IntPtr p_ndi_recv_name;
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

    [DllImport("Processing.NDI.Lib.x64.dll", EntryPoint = "NDIlib_recv_create_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr recv_create_v3(ref recv_create_v3_t p_create_settings);

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
