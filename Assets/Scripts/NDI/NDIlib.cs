using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NDIPlugin
{
    public static partial class NDIlib
    {
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

        [DllImport(Config.DllName, EntryPoint = "NDIlib_initialize", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool Initialize();

        [DllImport(Config.DllName, EntryPoint = "NDIlib_find_create_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr find_create_v2(ref find_create_t p_create_setings);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_find_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void find_destroy(IntPtr p_instance);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_find_wait_for_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool find_wait_for_sources(IntPtr p_instance, UInt32 timeout_in_ms);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_find_get_current_sources", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr find_get_current_sources(IntPtr p_instance, ref UInt32 p_no_sources);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_create_v3", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr recv_create_v3(ref recv_create_v3_t p_create_settings);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_set_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern bool recv_set_tally(IntPtr p_instance, ref tally_t p_tally);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_capture_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern frame_type_e recv_capture_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data, ref audio_frame_v2_t p_audio_data, ref metadata_frame_t p_metadata,
            UInt32 timeout_in_ms);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_ptz_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool recv_ptz_is_supported(IntPtr p_instance);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_recording_is_supported", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool recv_recording_is_supported(IntPtr p_instance);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_get_web_control", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr recv_get_web_control(IntPtr p_instance);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_free_string", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void recv_free_string(IntPtr p_instance, IntPtr p_string);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_free_video_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void recv_free_video_v2(IntPtr p_instance, ref video_frame_v2_t p_video_data);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_free_audio_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void recv_free_audio_v2(IntPtr p_intance, ref audio_frame_v2_t p_audio_data);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_free_metadata", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void recv_free_metadata(IntPtr p_instance, ref metadata_frame_t p_metadata);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_recv_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void recv_destroy(IntPtr p_instance);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_send_create", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr send_create(in SendSettings sendSettings);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_send_destroy", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_destroy(IntPtr send);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_send_send_video_async_v2", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void send_send_video_async_v2(IntPtr send, in video_frame_v2_t frame);

        [DllImport(Config.DllName, EntryPoint = "NDIlib_send_get_tally", ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        [return: MarshalAs(UnmanagedType.U1)]
        public static extern bool send_get_tally(IntPtr send);

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

        public static IntPtr StringToUtf8(String managedString)
        {
            int len = Encoding.UTF8.GetByteCount(managedString);

            byte[] buffer = new byte[len + 1];

            Encoding.UTF8.GetBytes(managedString, 0, managedString.Length, buffer, 0);

            IntPtr nativeUtf8 = Marshal.AllocHGlobal(buffer.Length);

            Marshal.Copy(buffer, 0, nativeUtf8, buffer.Length);

            return nativeUtf8;
        }
    }
}