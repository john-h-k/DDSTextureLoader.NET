using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
// ReSharper disable CommentTypo

namespace DDSTextureLoader.NET
{
    // ReSharper disable twice InconsistentNaming
    internal static class DdsContants
    {
        public const uint DDS_FOURCC = 0x00000004;
        public const uint DDS_RGB = 0x00000040;
        public const uint DDS_LUMINANCE = 0x00020000;
        public const uint DDS_ALPHA = 0x00000002;

        public const uint DDS_HEADER_FLAGS_VOLUME = 0x00800000;

        public const uint DDS_HEIGHT = 0x00000002;
        public const uint DDS_WIDTH = 0x00000004;


        public const uint DDS_CUBEMAP_POSITIVEX = 0x00000600; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
        public const uint DDS_CUBEMAP_NEGATIVEX = 0x00000a00; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
        public const uint DDS_CUBEMAP_POSITIVEY = 0x00001200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
        public const uint DDS_CUBEMAP_NEGATIVEY = 0x00002200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
        public const uint DDS_CUBEMAP_POSITIVEZ = 0x00004200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
        public const uint DDS_CUBEMAP_NEGATIVEZ = 0x00008200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

        public const uint DDS_CUBEMAP_ALLFACES = (DDS_CUBEMAP_POSITIVEX | DDS_CUBEMAP_NEGATIVEX |
                                                   DDS_CUBEMAP_POSITIVEY | DDS_CUBEMAP_NEGATIVEY |
                                                   DDS_CUBEMAP_POSITIVEZ | DDS_CUBEMAP_NEGATIVEZ);

        public const uint DDS_CUBEMAP = 0x00000200;
    }

    internal struct DdsPixelFormat
    {
        public uint size;
        public uint flags;
        public uint fourCC;
        public uint RGBBitCount;
        public uint RBitMask;
        public uint GBitMask;
        public uint BBitMask;
        public uint ABitMask;
    }

    internal unsafe struct DdsHeader
    {
        public uint size;
        public uint flags;
        public uint height;
        public uint width;
        public uint pitchOrLinearSize;
        public uint depth; // only if DDS_HEADER_FLAGS_VOLUME is set in flags
        public uint mipMapCount;
        public fixed uint reserved1[11];
        public DdsPixelFormat ddspf;
        public uint caps;
        public uint caps2;
        public uint caps3;
        public uint caps4;
        public uint reserved2;
    }

    internal struct DdsHeaderDxt10
    {
        public DXGI_FORMAT dxgiFormat;
        public D3D11_RESOURCE_DIMENSION resourceDimension;
        public D3D11_RESOURCE_MISC_FLAG miscFlag; // see D3D11_RESOURCE_MISC_FLAG
        public uint arraySize;
        public uint miscFlags2;
    }

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

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DDS_ALPHA_MODE
    {
        DDS_ALPHA_MODE_UNKNOWN = 0,
        DDS_ALPHA_MODE_STRAIGHT = 1,
        DDS_ALPHA_MODE_PREMULTIPLIED = 2,
        DDS_ALPHA_MODE_OPAQUE = 3,
        DDS_ALPHA_MODE_CUSTOM = 4,
    }

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
            if ((header.ddspf.flags & DdsContants.DDS_FOURCC) != 0)
            {
                if (MakeFourCC('D', 'X', '1', '0') == header.ddspf.fourCC)
                {
                    ref DdsHeaderDxt10 d3d10ext = ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(ref Unsafe.Add(ref header, 1));
                    var mode = (DDS_ALPHA_MODE)(d3d10ext.miscFlags2 & (uint)DDS_MISC_FLAGS2.DDS_MISC_FLAGS2_ALPHA_MODE_MASK);
                    switch (mode)
                    {
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_STRAIGHT:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_PREMULTIPLIED:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_OPAQUE:
                        case DDS_ALPHA_MODE.DDS_ALPHA_MODE_CUSTOM:
                            return mode;
                    }
                }
                else if ((MakeFourCC('D', 'X', 'T', '2') == header.ddspf.fourCC)
                         || (MakeFourCC('D', 'X', 'T', '4') == header.ddspf.fourCC))
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

        private struct X64EqualityComparer : IEqualityComparer<Vector128<uint>>
        {
            public bool Equals(Vector128<uint> x, Vector128<uint> y)
            {
                if (Sse2.IsSupported)
                {
                    return Sse2.MoveMask(Sse2.CompareEqual(x.AsByte(), y.AsByte())) == 0xFFFF;
                }

                return x.Equals(y);
            }

            public int GetHashCode(Vector128<uint> obj)
            {
                if (Sse2.IsSupported)
                {
                    var x = obj;
                    var y = Sse2.ShiftRightLogical128BitLane(obj, 4);
                    x = Sse2.Xor(x, y);
                    var z = Sse2.ShiftRightLogical128BitLane(obj, 8);
                    x = Sse2.Xor(x, z);
                    var w = Sse2.ShiftRightLogical128BitLane(obj, 12);
                    x = Sse2.Xor(x, w);

                    return (int)x.ToScalar();
                }

                return obj.GetHashCode();
            }
        }

        private static readonly Dictionary<Vector128<uint>, DXGI_FORMAT> _formatMap =
            new Dictionary<Vector128<uint>, DXGI_FORMAT>(new X64EqualityComparer())
            {
                [Vector128.Create(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000)] = DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM,
                [Vector128.Create(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000)] = DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM,
                [Vector128.Create(0x00ff0000u, 0x0000ff00, 0x000000ff, 0x00000000)] = DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM,
                [Vector128.Create(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000)] = DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM,
                // No DXGI format maps to ISBITMASK(0x000003ff,0x000ffc00,0x3ff00000,0xc0000000), aka D3DFMT_A2R10G10B10
                [Vector128.Create(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000)] = DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM,
                [Vector128.Create(0xffffffff, 0x00000000, 0x00000000, 0x00000000)] = DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT, // Only 32-bit color channel format in D3D9 was R32F - D3DX writes this out as a FourCC of 114
                [Vector128.Create(0x7c00u, 0x03e0u, 0x001fu, 0x8000u)] = DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM,
                [Vector128.Create(0xf800u, 0x07e0u, 0x001fu, 0x0000u)] = DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM,
                // No DXGI format maps to ISBITMASK(0x7c00,0x03e0,0x001f,0x0000), aka D3DFMT_X1R5G5B5
                [Vector128.Create(0x0f00u, 0x00f0u, 0x000fu, 0xf000u)] = DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM,
                [Vector128.Create(0x000000ffu, 0x00000000, 0x00000000, 0x00000000)] = DXGI_FORMAT.DXGI_FORMAT_R8_UNORM, // D3DX10/11 writes this out as DX10 extension
                [Vector128.Create(0x0000ffffu, 0x00000000, 0x00000000, 0x00000000)] = DXGI_FORMAT.DXGI_FORMAT_R16_UNORM, // D3DX10/11 writes this out as DX10 extension
                [Vector128.Create(0x000000ffu, 0x00000000, 0x00000000, 0x0000ff00)] = DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM // D3DX10/11 writes this out as DX10 extension
            };

        public static DXGI_FORMAT GetDxgiFormat(DdsPixelFormat ddpf)
        {
            if ((ddpf.flags & DdsContants.DDS_RGB) != 0 || (ddpf.flags & DdsContants.DDS_LUMINANCE) != 0)
            {
                return _formatMap[Unsafe.As<uint, Vector128<uint>>(ref ddpf.RBitMask)];
            }
            else if ((ddpf.flags & DdsContants.DDS_ALPHA) != 0)
            {
                if (ddpf.RGBBitCount == 8)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_A8_UNORM;
                }
            }
            else if ((ddpf.flags & DdsContants.DDS_FOURCC) != 0)
            {
                if (MakeFourCC('D', 'X', 'T', '1') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '3') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '5') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                // While pre-multiplied alpha isn't directly supported by the DXGI formats,
                // they are basically the same as these BC formats so they can be mapped
                if (MakeFourCC('D', 'X', 'T', '2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC('D', 'X', 'T', '4') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '1') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'U') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC('B', 'C', '4', 'S') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM;
                }

                if (MakeFourCC('A', 'T', 'I', '2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'U') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC('B', 'C', '5', 'S') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM;
                }

                // BC6H and BC7 are written using the "DX10" extended header

                if (MakeFourCC('R', 'G', 'B', 'G') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM;
                }

                if (MakeFourCC('G', 'R', 'G', 'B') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM;
                }

                if (MakeFourCC('Y', 'U', 'Y', '2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_YUY2;
                }

                return ddpf.fourCC switch
                {
                    36 => // D3DFMT_A16B16G16R16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM,
                    110 => // D3DFMT_Q16W16V16U16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM,
                    111 => // D3DFMT_R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
                    112 => // D3DFMT_G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT,
                    113 => // D3DFMT_A16B16G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
                    114 => // D3DFMT_R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,
                    115 => // D3DFMT_G32R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    116 => // D3DFMT_A32B32G32R32F
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
        public static uint MakeFourCC(char ch0, char ch1, char ch2, char ch3)
        {
            return MakeFourCC((byte)ch0, (byte)ch1, (byte)ch2, (byte)ch3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint MakeFourCC(byte ch0, byte ch1, byte ch2, byte ch3)
        {
            return ch0 |
                   ((uint)ch1 << 8) |
                   ((uint)ch2 << 16) |
                   ((uint)ch3 << 24);
        }
    }
}