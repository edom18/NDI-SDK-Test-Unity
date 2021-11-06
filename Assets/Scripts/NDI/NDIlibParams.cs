using System;
using System.Runtime.InteropServices;

namespace NDIPlugin
{
    public static partial class NDIlib
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
    }
}