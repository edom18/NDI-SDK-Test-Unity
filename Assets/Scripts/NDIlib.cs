using System;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace NDISample
{
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

        public enum FourCC_type_e
        {
            // YCbCr color space
            FourCC_type_UYVY = 0x59565955,

            // 4:2:0 formats
            NDIlib_FourCC_video_type_YV12 = 0x32315659,
            NDIlib_FourCC_video_type_NV12 = 0x3231564E,
            NDIlib_FourCC_video_type_I420 = 0x30323449,

            // BGRA
            FourCC_type_BGRA = 0x41524742,
            FourCC_type_BGRX = 0x58524742,

            // RGBA
            FourCC_type_RGBA = 0x41424752,
            FourCC_type_RGBX = 0x58424752,

            // This is a UYVY buffer followed immediately by an alpha channel buffer.
            // If the stride of the YCbCr component is "stride", then the alpha channel
            // starts at image_ptr + yres*stride. The alpha channel stride is stride/2.
            FourCC_type_UYVA = 0x41565955
        }

        public enum frame_type_e
        {
            frame_type_none = 0,
            frame_type_video = 1,
            frame_type_audio = 2,
            frame_type_metadata = 3,
            frame_type_error = 4,

            frame_type_status_change = 100,
        }

        public enum frame_format_type_e
        {
            frame_format_type_progressive = 1,
            frame_format_type_interleaved = 0,
            frame_format_type_field_0 = 2,
            frame_format_type_field_1 = 3,
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct video_frame_v2_t
        {
            public int xres;
            public int yres;
            public FourCC_type_e FourCC;
            public int frame_rate_N;
            public int frame_rate_D;
            public float picture_aspect_ratio;
            public frame_format_type_e frame_format_type;
            public Int64 timecode;
            public IntPtr p_data;
            public int line_stride_in_bytes;
            public IntPtr p_metadata;
            public Int64 timestamp;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct audio_frame_v2_t
        {
            public int sample_rate;
            public int no_channels;
            public int no_samples;
            public Int64 timecode;
            public IntPtr p_data;
            public int channels_stride_in_bytes;
            public IntPtr p_metadata;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct metadata_frame_t
        {
            public int length;
            public Int64 timecode;
            public IntPtr p_data;
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

            [MarshalAs(UnmanagedType.U1)] public bool allow_video_fields;

            public IntPtr p_ndi_recv_name;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct tally_t
        {
            [MarshalAs(UnmanagedType.U1)] public bool on_program;
            [MarshalAs(UnmanagedType.U1)] public bool on_preview;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SendSettings
        {
            public IntPtr NdiName;
            public IntPtr Groups;
            [MarshalAs(UnmanagedType.U1)] public bool ClockVideo;
            [MarshalAs(UnmanagedType.U1)] public bool ClockAudio;
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