using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
using static DDSTextureLoader.NET.PixelFormatFlags;
using static DDSTextureLoader.NET.D3DFormat;

// ReSharper disable IdentifierTypo
#pragma warning disable 649

// ReSharper disable CommentTypo

namespace DDSTextureLoader.NET
{
    // ReSharper disable twice InconsistentNaming
    internal enum D3DFormat
    {
        D3DFMT_UNKNOWN = 0,

        D3DFMT_R8G8B8 = 20,
        D3DFMT_A8R8G8B8 = 21,
        D3DFMT_X8R8G8B8 = 22,
        D3DFMT_R5G6B5 = 23,
        D3DFMT_X1R5G5B5 = 24,
        D3DFMT_A1R5G5B5 = 25,
        D3DFMT_A4R4G4B4 = 26,
        D3DFMT_R3G3B2 = 27,
        D3DFMT_A8 = 28,
        D3DFMT_A8R3G3B2 = 29,
        D3DFMT_X4R4G4B4 = 30,
        D3DFMT_A2B10G10R10 = 31,
        D3DFMT_A8B8G8R8 = 32,
        D3DFMT_X8B8G8R8 = 33,
        D3DFMT_G16R16 = 34,
        D3DFMT_A2R10G10B10 = 35,
        D3DFMT_A16B16G16R16 = 36,

        D3DFMT_A8P8 = 40,
        D3DFMT_P8 = 41,

        D3DFMT_L8 = 50,
        D3DFMT_A8L8 = 51,
        D3DFMT_A4L4 = 52,

        D3DFMT_V8U8 = 60,
        D3DFMT_L6V5U5 = 61,
        D3DFMT_X8L8V8U8 = 62,
        D3DFMT_Q8W8V8U8 = 63,
        D3DFMT_V16U16 = 64,
        D3DFMT_A2W10V10U10 = 67,

        D3DFMT_UYVY = 1498831189, // MAKEFOURCC('U', 'Y', 'V', 'Y'),
        D3DFMT_R8G8_B8G8 = 1195525970, // MAKEFOURCC('R', 'G', 'B', 'G'),
        D3DFMT_YUY2 = 844715353, // MAKEFOURCC('Y', 'U', 'Y', '2'),
        D3DFMT_G8R8_G8B8 = 1111970375, // MAKEFOURCC('G', 'R', 'G', 'B'),
        D3DFMT_DXT1 = 827611204, // MAKEFOURCC('D', 'X', 'T', '1'),
        D3DFMT_DXT2 = 844388420, // MAKEFOURCC('D', 'X', 'T', '2'),
        D3DFMT_DXT3 = 861165636, // MAKEFOURCC('D', 'X', 'T', '3'),
        D3DFMT_DXT4 = 877942852, // MAKEFOURCC('D', 'X', 'T', '4'),
        D3DFMT_DXT5 = 894720068, // MAKEFOURCC('D', 'X', 'T', '5'),

        D3DFMT_D16_LOCKABLE = 70,
        D3DFMT_D32 = 71,
        D3DFMT_D15S1 = 73,
        D3DFMT_D24S8 = 75,
        D3DFMT_D24X8 = 77,
        D3DFMT_D24X4S4 = 79,
        D3DFMT_D16 = 80,

        D3DFMT_D32F_LOCKABLE = 82,
        D3DFMT_D24FS8 = 83,

        D3DFMT_D32_LOCKABLE = 84,
        D3DFMT_S8_LOCKABLE = 85,

        D3DFMT_L16 = 81,

        D3DFMT_VERTEXDATA = 100,
        D3DFMT_INDEX16 = 101,
        D3DFMT_INDEX32 = 102,

        D3DFMT_Q16W16V16U16 = 110,

        D3DFMT_MULTI2_ARGB8 = 827606349, // MAKEFOURCC('M','E','T','1'),

        D3DFMT_R16F = 111,
        D3DFMT_G16R16F = 112,
        D3DFMT_A16B16G16R16F = 113,

        D3DFMT_R32F = 114,
        D3DFMT_G32R32F = 115,
        D3DFMT_A32B32G32R32F = 116,

        D3DFMT_CxV8U8 = 117,

        D3DFMT_A1 = 118,
        D3DFMT_A2B10G10R10_XR_BIAS = 119,
        D3DFMT_BINARYBUFFER = 199,

        D3DFMT_FORCE_DWORD = 0x7fffffff
    }

    [Flags]
    internal enum PixelFormatFlags : uint
    {
        DDS_FOURCC = 0x00000004,
        DDS_RGB = 0x00000040,
        DDS_LUMINANCE = 0x00020000,
        DDS_ALPHA = 0x00000002
    }

    [Flags]
    internal enum HeaderFlags
    {
        DDS_CAPS = 0x1,
        DDS_HEIGHT = 0x2,
        DDS_WIDTH = 0x4,
        DDS_PITCH = 0x8,
        DDS_PIXELFORMAT = 0x1000,
        DDS_MIPMAPCOUNT = 0x20000,
        DDS_LINEARSIZE = 0x80000,
        DDS_DEPTH = 0x800000,
        DDS_HEADER_FLAGS_VOLUME = 0x00800000,

    }

    [Flags]
    internal enum Caps2Flags : uint
    {
        DDS_CUBEMAP_POSITIVEX = 0x00000600, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
        DDS_CUBEMAP_NEGATIVEX = 0x00000a00, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
        DDS_CUBEMAP_POSITIVEY = 0x00001200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
        DDS_CUBEMAP_NEGATIVEY = 0x00002200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
        DDS_CUBEMAP_POSITIVEZ = 0x00004200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
        DDS_CUBEMAP_NEGATIVEZ = 0x00008200, // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

        DDS_CUBEMAP_ALLFACES = DDS_CUBEMAP_POSITIVEX | DDS_CUBEMAP_NEGATIVEX |
                               DDS_CUBEMAP_POSITIVEY | DDS_CUBEMAP_NEGATIVEY |
                               DDS_CUBEMAP_POSITIVEZ | DDS_CUBEMAP_NEGATIVEZ,
        DDS_CUBEMAP = 0x00000200
    }

    internal struct DdsPixelFormat
    {
        public uint Size;
        public PixelFormatFlags Flags;
        public D3DFormat FourCC;
        public uint RgbBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }

    internal unsafe struct DdsHeader
    {
        public uint Size;
        public HeaderFlags Flags;
        public uint Height;
        public uint Width;
        public uint PitchOrLinearSize;
        public uint Depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
        public uint MipMapCount;
        public fixed uint Reserved1[11];
        public DdsPixelFormat DdsPixelFormat;
        public uint Caps;
        public Caps2Flags Caps2;
        public uint Caps3;
        public uint Caps4;
        public uint Reserved2;
    }

    internal struct DdsHeaderDxt10
    {
        public DXGI_FORMAT DxgiFormat;
        public D3D11_RESOURCE_DIMENSION ResourceDimension;
        public D3D11_RESOURCE_MISC_FLAG MiscFlag; // see D3D11_RESOURCE_MISC_FLAG
        public uint ArraySize;
        public uint MiscFlags2;
    }

    [Flags]
    internal enum D3D11_RESOURCE_MISC_FLAG
    {
        D3D11_RESOURCE_MISC_GENERATE_MIPS,
        D3D11_RESOURCE_MISC_SHARED,
        D3D11_RESOURCE_MISC_TEXTURECUBE,
        D3D11_RESOURCE_MISC_DRAWINDIRECT_ARGS,
        D3D11_RESOURCE_MISC_BUFFER_ALLOW_RAW_VIEWS,
        D3D11_RESOURCE_MISC_BUFFER_STRUCTURED,
        D3D11_RESOURCE_MISC_RESOURCE_CLAMP,
        D3D11_RESOURCE_MISC_SHARED_KEYEDMUTEX,
        D3D11_RESOURCE_MISC_GDI_COMPATIBLE,
        D3D11_RESOURCE_MISC_SHARED_NTHANDLE,
        D3D11_RESOURCE_MISC_RESTRICTED_CONTENT,
        D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE,
        D3D11_RESOURCE_MISC_RESTRICT_SHARED_RESOURCE_DRIVER,
        D3D11_RESOURCE_MISC_GUARDED,
        D3D11_RESOURCE_MISC_TILE_POOL,
        D3D11_RESOURCE_MISC_TILED,
        D3D11_RESOURCE_MISC_HW_PROTECTED
    };

    

    [Flags]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum DDS_MISC_FLAGS2 : uint
    {
        DDS_MISC_FLAGS2_ALPHA_MODE_MASK = 0x7,
    };

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum D3D11_RESOURCE_DIMENSION : uint
    {
        D3D11_RESOURCE_DIMENSION_UNKNOWN = 0,
        D3D11_RESOURCE_DIMENSION_BUFFER = 1,
        D3D11_RESOURCE_DIMENSION_TEXTURE1D = 2,
        D3D11_RESOURCE_DIMENSION_TEXTURE2D = 3,
        D3D11_RESOURCE_DIMENSION_TEXTURE3D = 4
    }

    internal static class InteropTypeUtilities
    {
        public static DDS_ALPHA_MODE GetAlphaMode(ref DdsHeader header)
        {
            var flags = header.DdsPixelFormat.Flags;
            if (flags.HasFlag(DDS_FOURCC))
            {
                if (MakeFourCC('D', 'X', '1', '0') == header.DdsPixelFormat.FourCC)
                {
                    ref DdsHeaderDxt10 d3d10ext =
                        ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(ref Unsafe.Add(ref header, 1));
                    var mode = (DDS_ALPHA_MODE) (d3d10ext.MiscFlags2 &
                                                 (uint) DDS_MISC_FLAGS2.DDS_MISC_FLAGS2_ALPHA_MODE_MASK);
                    switch (mode)
                    {
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_STRAIGHT:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_PREMULTIPLIED:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_OPAQUE:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_CUSTOM:
                            return mode;
                    }
                }
                else if ((MakeFourCC('D', 'X', 'T', '2') == header.DdsPixelFormat.FourCC)
                         || (MakeFourCC('D', 'X', 'T', '4') == header.DdsPixelFormat.FourCC))
                {
                    return DDS_ALPHA_MODE.DDS_ALPHA_MODE_PREMULTIPLIED;
                }
            }

            return DDS_ALPHA_MODE.DDS_ALPHA_MODE_UNKNOWN;
        }

        public static DXGI_FORMAT MakeSrgb(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB;

                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    return DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB;

                default:
                    return format;
            }
        }

        private readonly struct Bitmask16 : IEquatable<Bitmask16>
        {
            public Bitmask16(uint e0, uint e1, uint e2, uint e3)
            {
                E0 = e0;
                E1 = e1;
                E2 = e2;
                E3 = e3;
            }
            
            public readonly uint E0, E1, E2, E3;

            public bool Equals(Bitmask16 other) => E0 == other.E0 && E1 == other.E1 && E2 == other.E2 && E3 == other.E3;

            public override bool Equals(object obj) => obj is Bitmask16 other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(E0, E1, E2, E3);
        }

    private static readonly Dictionary<Bitmask16, DXGI_FORMAT> _formatMap =
            new Dictionary<Bitmask16, DXGI_FORMAT>()
            {
                [new Bitmask16(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                
                [new Bitmask16(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                
                [new Bitmask16(0x00ff0000u, 0x0000ff00, 0x000000ff, 0x00000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM,
                
                [new Bitmask16(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM,
                
                // No DXGI format maps to ISBITMASK(0x000003ff,0x000ffc00,0x3ff00000,0xc0000000), aka D3DFMT_A2R10G10B10
                [new Bitmask16(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM,
                
                [new Bitmask16(0xffffffff, 0x00000000, 0x00000000, 0x00000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT, // Only 32-bit color channel format in D3D9 was R32F - D3DX writes this out as a FourCC of 114
                
                [new Bitmask16(0x7c00u, 0x03e0u, 0x001fu, 0x8000u)] = DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM,
                
                [new Bitmask16(0xf800u, 0x07e0u, 0x001fu, 0x0000u)] = DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM,
                
                // No DXGI format maps to ISBITMASK(0x7c00,0x03e0,0x001f,0x0000), aka D3DFMT_X1R5G5B5
                [new Bitmask16(0x0f00u, 0x00f0u, 0x000fu, 0xf000u)] = DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM,
                
                [new Bitmask16(0x000000ffu, 0x00000000, 0x00000000, 0x00000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R8_UNORM, // D3DX10/11 writes this out as DX10 extension
                
                [new Bitmask16(0x0000ffffu, 0x00000000, 0x00000000, 0x00000000)] =
                    DXGI_FORMAT.DXGI_FORMAT_R16_UNORM, // D3DX10/11 writes this out as DX10 extension
                
                [new Bitmask16(0x000000ffu, 0x00000000, 0x00000000, 0x0000ff00)] =
                    DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM // D3DX10/11 writes this out as DX10 extension
            };

        public static DXGI_FORMAT GetDxgiFormat(DdsPixelFormat pixelFormat)
        {
            var flags = pixelFormat.Flags;
            if (flags.HasFlag(DDS_RGB) || flags.HasFlag(DDS_LUMINANCE))
            {
                return _formatMap[Unsafe.As<uint, Bitmask16>(ref pixelFormat.RBitMask)];
            }

            if (flags.HasFlag(DDS_ALPHA))
            {
                if (pixelFormat.RgbBitCount == 8)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_A8_UNORM;
                }
            }
            else if (flags.HasFlag(DDS_FOURCC))
            {
                if (MakeFourCC('D', 'X', 'T', '1') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '3') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '5') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                // While pre-multiplied alpha isn't directly supported by the DXGI formats,
                // they are basically the same as these BC formats so they can be mapped
                if (MakeFourCC('D', 'X', 'T', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '4') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '1') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'U') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'S') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'U') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'S') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM;
                }

                // BC6H and BC7 are written using the "DX10" extended header

                if (MakeFourCC('R', 'G', 'B', 'G') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM;
                }

                if (MakeFourCC('G', 'R', 'G', 'B') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM;
                }

                if (MakeFourCC('Y', 'U', 'Y', '2') == pixelFormat.FourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_YUY2;
                }

                return pixelFormat.FourCC switch
                {
                    D3DFMT_A16B16G16R16 => // D3DFMT_A16B16G16R16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM,
                    
                    D3DFMT_Q16W16V16U16 => // D3DFMT_Q16W16V16U16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM,
                    
                    D3DFMT_R16F => // D3DFMT_R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
                    
                    D3DFMT_G16R16F => // D3DFMT_G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT,
                    
                    D3DFMT_A16B16G16R16F => // D3DFMT_A16B16G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
                    
                    D3DFMT_R32F => // D3DFMT_R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,
                    
                    D3DFMT_G32R32F => // D3DFMT_G32R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    
                    D3DFMT_A32B32G32R32F => // D3DFMT_A32B32G32R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT,
                    
                    _ => DXGI_FORMAT.DXGI_FORMAT_UNKNOWN
                };
            }

            return DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
        }

        private static bool IsBitmask(uint r, uint g, uint b, uint a, DdsPixelFormat ddpf)
        {
            return ddpf.RBitMask == r && ddpf.GBitMask == g && ddpf.BBitMask == b && ddpf.ABitMask == a;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static D3DFormat MakeFourCC(char ch0, char ch1, char ch2, char ch3)
        {
            return (D3DFormat)MakeFourCC((byte) ch0, (byte) ch1, (byte) ch2, (byte) ch3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MakeFourCC(byte ch0, byte ch1, byte ch2, byte ch3)
        {
            return ch0 |
                   ((uint) ch1 << 8) |
                   ((uint) ch2 << 16) |
                   ((uint) ch3 << 24);
        }
    }
}