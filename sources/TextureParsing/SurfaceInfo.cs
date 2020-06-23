using System;
using TerraFX.Interop;
using static TerraFX.Interop.DXGI_FORMAT;

#nullable enable

namespace DDSTextureLoader.NET.TextureParsing
{
    internal readonly struct SurfaceInfo
    {
        public SurfaceInfo(uint numBytes, uint rowBytes, uint numRows)
        {
            NumBytes = numBytes;
            RowBytes = rowBytes;
            NumRows = numRows;
        }

        public uint NumBytes { get; }
        public uint RowBytes { get; }
        public uint NumRows { get; }

        public static SurfaceInfo GetSurfaceInfo(Size2 size, DXGI_FORMAT format)
        {
            var bc = false;
            var packed = false;
            var planar = false;
            uint bpe = 0;

            switch (format)
            {
                case DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT_BC4_SNORM:
                    bc = true;
                    bpe = 8;
                    break;

                case DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT_BC7_UNORM_SRGB:
                    bc = true;
                    bpe = 16;
                    break;

                case DXGI_FORMAT_R8G8_B8G8_UNORM:
                case DXGI_FORMAT_G8R8_G8B8_UNORM:
                case DXGI_FORMAT_YUY2:
                    packed = true;
                    bpe = 4;
                    break;

                case DXGI_FORMAT_Y210:
                case DXGI_FORMAT_Y216:
                    packed = true;
                    bpe = 8;
                    break;

                case DXGI_FORMAT_NV12:
                case DXGI_FORMAT_420_OPAQUE:
                    planar = true;
                    bpe = 2;
                    break;

                case DXGI_FORMAT_P010:
                case DXGI_FORMAT_P016:
                    planar = true;
                    bpe = 4;
                    break;
            }

            if (bc)
            {
                return FromBc(size, bpe);
            }
            else if (packed)
            {
                return FromPacked(size, bpe);
            }
            else if (format == DXGI_FORMAT_NV11)
            {
                return FromNv11(size);
            }
            else if (planar)
            {
                return FromPlanar(size, bpe);
            }
            else
            {
                return FromOther(size, format);
            }
        }

        private static SurfaceInfo FromBc(Size2 size, uint bytesPerBlock)
        {
            (uint height, uint width) = size;
            uint numBlocksWide = 0;
            if (width > 0)
            {
                numBlocksWide = Math.Max(1, (width + 3) / 4);
            }

            uint numBlocksHigh = 0;
            if (height > 0)
            {
                numBlocksHigh = Math.Max(1, (height + 3) / 4);
            }

            var rowBytes = numBlocksWide * bytesPerBlock;
            var numRows = numBlocksHigh;
            var numBytes = rowBytes * numBlocksHigh;

            return new SurfaceInfo(numBytes, rowBytes, numRows);
        }

        private static SurfaceInfo FromPacked(Size2 size, uint bytesPerBlock)
        {
            (uint height, uint width) = size;
            var rowBytes = ((width + 1) >> 1) * bytesPerBlock;
            var numRows = height;
            var numBytes = rowBytes * height;

            return new SurfaceInfo(numBytes, rowBytes, numRows);
        }

        public static SurfaceInfo FromPlanar(Size2 size, uint bytesPerBlock)
        {
            (uint height, uint width) = size;

            var rowBytes = ((width + 1) >> 1) * bytesPerBlock;
            var numRows = height + ((height + 1) >> 1);
            var numBytes = (rowBytes * height) + ((rowBytes * height + 1) >> 1);

            return new SurfaceInfo(numBytes, rowBytes, numRows);
        }

        private static SurfaceInfo FromOther(Size2 size, DXGI_FORMAT format)
        {
            (uint height, uint width) = size;
            uint bpp = BitsPerPixel(format);
            var rowBytes = (width * bpp + 7) / 8; // round up to nearest byte
            var numRows = height;
            var numBytes = rowBytes * height;

            return new SurfaceInfo(numBytes, rowBytes, numRows);
        }

        private static SurfaceInfo FromNv11(Size2 size)
        {
            (uint height, uint width) = size;
            var rowBytes = ((width + 3) >> 2) * 4;
            var numRows = height * 2;
            var numBytes = rowBytes * numRows;

            return new SurfaceInfo(numBytes, rowBytes, numRows);
        }

        public static uint BitsPerPixel(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT_R32G32B32A32_TYPELESS:
                case DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT_R32G32B32A32_UINT:
                case DXGI_FORMAT_R32G32B32A32_SINT:
                    return 128;

                case DXGI_FORMAT_R32G32B32_TYPELESS:
                case DXGI_FORMAT_R32G32B32_FLOAT:
                case DXGI_FORMAT_R32G32B32_UINT:
                case DXGI_FORMAT_R32G32B32_SINT:
                    return 96;

                case DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT_R16G16B16A16_SNORM:
                case DXGI_FORMAT_R16G16B16A16_SINT:
                case DXGI_FORMAT_R32G32_TYPELESS:
                case DXGI_FORMAT_R32G32_FLOAT:
                case DXGI_FORMAT_R32G32_UINT:
                case DXGI_FORMAT_R32G32_SINT:
                case DXGI_FORMAT_R32G8X24_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                case DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
                case DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                case DXGI_FORMAT_Y416:
                case DXGI_FORMAT_Y210:
                case DXGI_FORMAT_Y216:
                    return 64;

                case DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT_R11G11B10_FLOAT:
                case DXGI_FORMAT_R8G8B8A8_TYPELESS:
                case DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case DXGI_FORMAT_R8G8B8A8_UINT:
                case DXGI_FORMAT_R8G8B8A8_SNORM:
                case DXGI_FORMAT_R8G8B8A8_SINT:
                case DXGI_FORMAT_R16G16_TYPELESS:
                case DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT_R16G16_SINT:
                case DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT_R32_SINT:
                case DXGI_FORMAT_R24G8_TYPELESS:
                case DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                case DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
                case DXGI_FORMAT_R8G8_B8G8_UNORM:
                case DXGI_FORMAT_G8R8_G8B8_UNORM:
                case DXGI_FORMAT_B8G8R8A8_UNORM:
                case DXGI_FORMAT_B8G8R8X8_UNORM:
                case DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
                case DXGI_FORMAT_B8G8R8A8_TYPELESS:
                case DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
                case DXGI_FORMAT_B8G8R8X8_TYPELESS:
                case DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                case DXGI_FORMAT_AYUV:
                case DXGI_FORMAT_Y410:
                case DXGI_FORMAT_YUY2:
                    return 32;

                case DXGI_FORMAT_P010:
                case DXGI_FORMAT_P016:
                    return 24;

                case DXGI_FORMAT_R8G8_TYPELESS:
                case DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT_R8G8_UINT:
                case DXGI_FORMAT_R8G8_SNORM:
                case DXGI_FORMAT_R8G8_SINT:
                case DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT_R16_SINT:
                case DXGI_FORMAT_B5G6R5_UNORM:
                case DXGI_FORMAT_B5G5R5A1_UNORM:
                case DXGI_FORMAT_A8P8:
                case DXGI_FORMAT_B4G4R4A4_UNORM:
                    return 16;

                case DXGI_FORMAT_NV12:
                case DXGI_FORMAT_420_OPAQUE:
                case DXGI_FORMAT_NV11:
                    return 12;

                case DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT_R8_SNORM:
                case DXGI_FORMAT_R8_SINT:
                case DXGI_FORMAT_A8_UNORM:
                case DXGI_FORMAT_AI44:
                case DXGI_FORMAT_IA44:
                case DXGI_FORMAT_P8:
                    return 8;

                case DXGI_FORMAT_R1_UNORM:
                    return 1;

                case DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT_BC4_SNORM:
                    return 4;

                case DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT_BC7_UNORM_SRGB:
                    return 8;

                default:
                    return 0;
            }
        }

    }
}
