using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace DDSTextureLoader.NET.TextureParsing
{
    internal static class InteropTypeUtilities
    {
        public static AlphaMode GetAlphaMode(ref DdsHeader header)
        {
            var flags = header.DdsPixelFormat.Flags;
            if (flags.HasFlag(PixelFormatFlags.DDS_FOURCC))
            {
                if (MakeFourCC('D', 'X', '1', '0') == header.DdsPixelFormat.FourCC)
                {
                    ref DdsHeaderDxt10 d3d10ext =
                        ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(ref Unsafe.Add(ref header, 1));
                    var mode = (AlphaMode) (d3d10ext.MiscFlags2 &
                                                 (uint) DDS_MISC_FLAGS2.DDS_MISC_FLAGS2_ALPHA_MODE_MASK);
                    switch (mode)
                    {
                        case AlphaMode.Straight:
                        case AlphaMode.Premultiplied:
                        case AlphaMode.Opaque:
                        case AlphaMode.Custom:
                            return mode;
                    }
                }
                else if ((MakeFourCC('D', 'X', 'T', '2') == header.DdsPixelFormat.FourCC)
                         || (MakeFourCC('D', 'X', 'T', '4') == header.DdsPixelFormat.FourCC))
                {
                    return AlphaMode.Premultiplied;
                }
            }

            return AlphaMode.Unknown;
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

            public override bool Equals(object? obj) => obj is Bitmask16 other && Equals(other);

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
            if (flags.HasFlag(PixelFormatFlags.DDS_RGB) || flags.HasFlag(PixelFormatFlags.DDS_LUMINANCE))
            {
                return _formatMap[Unsafe.As<uint, Bitmask16>(ref pixelFormat.RBitMask)];
            }

            if (flags.HasFlag(PixelFormatFlags.DDS_ALPHA))
            {
                if (pixelFormat.RgbBitCount == 8)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_A8_UNORM;
                }
            }
            else if (flags.HasFlag(PixelFormatFlags.DDS_FOURCC))
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
                    D3DFormat.D3DFMT_A16B16G16R16 => // D3DFMT_A16B16G16R16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM,
                    
                    D3DFormat.D3DFMT_Q16W16V16U16 => // D3DFMT_Q16W16V16U16
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM,
                    
                    D3DFormat.D3DFMT_R16F => // D3DFMT_R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT,
                    
                    D3DFormat.D3DFMT_G16R16F => // D3DFMT_G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT,
                    
                    D3DFormat.D3DFMT_A16B16G16R16F => // D3DFMT_A16B16G16R16F
                    DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT,
                    
                    D3DFormat.D3DFMT_R32F => // D3DFMT_R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT,
                    
                    D3DFormat.D3DFMT_G32R32F => // D3DFMT_G32R32F
                    DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT,
                    
                    D3DFormat.D3DFMT_A32B32G32R32F => // D3DFMT_A32B32G32R32F
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