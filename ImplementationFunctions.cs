using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using TerraFX.Interop;

namespace DDSTextureLoader.NET
{
    internal static class ImplementationFunctions
    {
        public static DdsTextureDescription CreateTextureFromDds12(FileMetadata metadata, uint maxsize, bool forceSrgb)
        {
            ref DdsHeader header = ref metadata.DdsHeader;

            uint width = header.Width;
            uint height = header.Height;
            uint depth = header.Depth;

            D3D12_RESOURCE_DIMENSION resDim = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_UNKNOWN;

            uint arraySize = 1;

            DXGI_FORMAT format;
            bool isCubeMap = false;

            uint mipCount = header.MipMapCount;

            var pixelFormatFlags = header.DdsPixelFormat.Flags;

            if (mipCount == 0)
            {
                mipCount = 1;
            }

            if (pixelFormatFlags.HasFlag(PixelFormatFlags.DDS_FOURCC)
                && (header.DdsPixelFormat.FourCC == InteropTypeUtilities.MakeFourCC('D', 'X', '1', '0')))
            {
                ref DdsHeaderDxt10 d3d10Ext =
                    ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(
                        ref Unsafe.Add(
                            ref header, 1));

                arraySize = d3d10Ext.ArraySize;

                if (arraySize == 0)
                {
                    ThrowHelper.ThrowArgumentException("DDS has invalid data");
                }

                switch (d3d10Ext.DxgiFormat)
                {
                    case DXGI_FORMAT.DXGI_FORMAT_AI44:
                    case DXGI_FORMAT.DXGI_FORMAT_IA44:
                    case DXGI_FORMAT.DXGI_FORMAT_P8:
                    case DXGI_FORMAT.DXGI_FORMAT_A8P8:
                        ThrowHelper.ThrowNotSupportedException("Format not supported");
                        break;
                    default:
                        if (SurfaceInfo.BitsPerPixel(d3d10Ext.DxgiFormat) == 0)
                        {
                            ThrowHelper.ThrowNotSupportedException("Format not supported");
                        }

                        break;
                }

                format = d3d10Ext.DxgiFormat;

                switch (d3d10Ext.ResourceDimension)
                {
                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE1D:
                        if (header.Flags.HasFlag(HeaderFlags.DDS_HEIGHT) && height != 1)
                            ThrowHelper.ThrowArgumentException("DDS has invalid data");
                        height = 1;
                        depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D:
                        if ((d3d10Ext.MiscFlag & D3D11_RESOURCE_MISC_FLAG.D3D11_RESOURCE_MISC_TEXTURECUBE) != 0)
                        {
                            arraySize *= 6;
                            isCubeMap = true;
                        }

                        depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D:
                        if ((header.Flags & HeaderFlags.DDS_HEADER_FLAGS_VOLUME) == 0)
                            ThrowHelper.ThrowArgumentException("DDS has invalid data");
                        if (arraySize > 1)
                            ThrowHelper.ThrowNotSupportedException("Unsupported DDS dimension");
                        break;

                    default:
                        ThrowHelper.ThrowNotSupportedException("Unsupported DDS dimension");
                        break;
                }

                resDim = d3d10Ext.ResourceDimension switch
                {
                    D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE1D => D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D,
                    D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D => D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                    D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D => D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D,
                    _ => resDim
                };
            }
            else
            {
                format = InteropTypeUtilities.GetDxgiFormat(header.DdsPixelFormat);

                if (format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                    ThrowHelper.ThrowNotSupportedException("Unsupported DXGI format");

                if ((header.Flags & HeaderFlags.DDS_HEADER_FLAGS_VOLUME) != 0)
                {
                    resDim = (D3D12_RESOURCE_DIMENSION) D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D;
                }
                else
                {
                    if (header.Caps2.HasFlag(Caps2Flags.DDS_CUBEMAP))
                    {
                        if ((header.Caps2 & Caps2Flags.DDS_CUBEMAP_ALLFACES) != Caps2Flags.DDS_CUBEMAP_ALLFACES)
                        {
                            ThrowHelper.ThrowNotSupportedException("Not supported CubeMap format");
                        }

                        arraySize = 6;
                        isCubeMap = true;
                    }

                    depth = 1;
                    resDim = (D3D12_RESOURCE_DIMENSION) D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D;
                }

                Debug.Assert(SurfaceInfo.BitsPerPixel(format) != 0);
            }

            if (mipCount > D3D12.D3D12_REQ_MIP_LEVELS)
            {
                ThrowHelper.ThrowNotSupportedException($"{D3D12.D3D12_REQ_MIP_LEVELS} MIP levels are required");
            }

            var size = new Size3(height, width, depth);

            EnsureValidResourceSizeAndDimension(resDim, arraySize, isCubeMap, size);

            var subresourceData = FillSubresourceData(size, mipCount, arraySize, format, (uint) maxsize,
                metadata.BitData, out Size3 texSize, out uint skipMip);

            return new DdsTextureDescription(metadata.BitData, resDim, texSize, mipCount - skipMip, arraySize, format, forceSrgb,
                isCubeMap, subresourceData, InteropTypeUtilities.GetAlphaMode(ref metadata.DdsHeader));
        }

        private static void EnsureValidResourceSizeAndDimension(D3D12_RESOURCE_DIMENSION resDim, in uint arraySize,
            in bool isCubeMap, in Size3 size)
        {
            switch (resDim)
            {
                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                    if (arraySize > D3D12.D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION
                        || size.Width > D3D12.D3D12_REQ_TEXTURE1D_U_DIMENSION)
                    {
                        ThrowHelper.ThrowNotSupportedException("Not supported arraySize or width");
                    }

                    break;

                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    if (isCubeMap)
                    {
                        if (arraySize > D3D12.D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION
                            || size.Width > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION
                            || size.Height > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION)
                        {
                            ThrowHelper.ThrowNotSupportedException("Not supported arraySize, width, or height");
                        }
                    }

                    break;
                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                    if (arraySize > 1
                        || size.Width > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || size.Height > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || size.Depth > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
                    {
                        ThrowHelper.ThrowNotSupportedException("Not supported arraySize, width, height, or depth");
                    }

                    break;

                default:
                    ThrowHelper.ThrowNotSupportedException("Not supported dimension");
                    break;
            }
        }

        private static ManagedSubresourceData[] FillSubresourceData(
            Size3 size,
            uint mipCount,
            uint arraySize,
            DXGI_FORMAT format,
            uint maxsize,
            Memory<byte> bitData,
            out Size3 texSize,
            out uint skipMip)
        {
            if (bitData.IsEmpty)
            {
                ThrowHelper.ThrowArgumentException(nameof(bitData), "Cannot be empty");
            }

            skipMip = 0;
            texSize = default;
            uint offset = 0;

            var index = 0;

            var data = new ManagedSubresourceData[mipCount * arraySize];

            for (var i = 0U; i < arraySize; i++)
            {
                var tmpSize = size;

                for (var j = 0U; j < mipCount; j++)
                {
                    var surface = SurfaceInfo.GetSurfaceInfo((Size2) tmpSize, format);

                    if (mipCount <= 1 || maxsize == 0 ||
                        tmpSize.Width <= maxsize && tmpSize.Height <= maxsize && tmpSize.Depth <= maxsize)
                    {
                        if (texSize.Width == 0)
                        {
                            texSize = (Size3) tmpSize;
                        }

                        Debug.Assert(index < mipCount * arraySize);
                        data[index] = new ManagedSubresourceData(offset, surface.RowBytes, surface.NumBytes);

                        index++;
                    }
                    else if (j == 0)
                    {
                        // Count number of skipped mipmaps (first item only)
                        ++skipMip;
                    }

                    offset += surface.NumBytes * tmpSize.Depth;

                    if (offset > bitData.Length)
                    {
                        ThrowHelper.ThrowArgumentException("File was too small");
                    }

                    tmpSize.Height >>= 1;
                    tmpSize.Width >>= 1;
                    tmpSize.Depth >>= 1;

                    if (tmpSize.Height == 0) tmpSize.Height = 1;
                    if (tmpSize.Width == 0) tmpSize.Width = 1;
                    if (tmpSize.Depth == 0) tmpSize.Depth = 1;
                }
            }

            if (index == 0)
                ThrowHelper.ThrowArgumentException("Size was 0");

            return data;
        }
    }
}