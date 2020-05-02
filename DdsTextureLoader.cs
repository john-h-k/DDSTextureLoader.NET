using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using TerraFX.Interop;
using static DDSTextureLoader.NET.D3D11_RESOURCE_DIMENSION;
using static DDSTextureLoader.NET.DDS_ALPHA_MODE;
using static DDSTextureLoader.NET.DdsContants;
using static DDSTextureLoader.NET.D3D11_RESOURCE_MISC_FLAG;
using static DDSTextureLoader.NET.ThrowHelper;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;
using static TerraFX.Interop.DXGI_FORMAT;

#nullable enable

namespace DDSTextureLoader.NET
{
    public static unsafe partial class DdsTextureLoader
    {
        public static DdsTexture CreateDdsTexture(string fileName, UIntPtr maxsize = default)
        {
            if (fileName is null)
            {
                ThrowArgumentNullException(nameof(fileName));
            }

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return CreateDdsTexture(stream, maxsize);
        }

        public static DdsTexture CreateDdsTexture(Stream stream, UIntPtr maxsize = default)
        {
            if (stream is null)
            {
                ThrowArgumentNullException(nameof(stream));
            }

            var streamSize = stream.Length;
            if (streamSize > int.MaxValue)
            {
                ThrowArgumentException("File too large");
            }

            var data = new byte[streamSize];
            stream.Read(data);
            return CreateDdsTexture(data, maxsize);
        }

        public static DdsTexture CreateDdsTexture(Memory<byte> ddsData, UIntPtr maxsize = default)
        {
            if (ddsData.Length < sizeof(DdsHeader) + sizeof(uint))
            {
                ThrowArgumentException("Data too small to be a valid DDS file");
            }

            var metadata = FileMetadata.FromMemory(ddsData);

            return CreateTextureFromDds12(metadata, maxsize, forceSrgb: false);
        }

        private static DdsTexture CreateTextureFromDds12(FileMetadata metadata, UIntPtr maxsize, bool forceSrgb)
        {
            ref DdsHeader header = ref metadata.DdsHeader;

            uint width = header.width;
            uint height = header.height;
            uint depth = header.depth;

            D3D12_RESOURCE_DIMENSION resDim = D3D12_RESOURCE_DIMENSION_UNKNOWN;

            uint arraySize = 1;

            DXGI_FORMAT format;
            bool isCubeMap = false;

            uint mipCount = header.mipMapCount;

            if (mipCount == 0)
            {
                mipCount = 1;
            }

            if (((header.ddspf.flags & DDS_FOURCC) != 0)
                && (header.ddspf.fourCC == InteropTypeUtilities.MakeFourCC('D', 'X', '1', '0')))
            {
                ref DdsHeaderDxt10 d3d10Ext =
                    ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(
                        ref Unsafe.Add(
                                    ref header, 1));

                arraySize = d3d10Ext.arraySize;

                if (arraySize == 0)
                {
                    ThrowArgumentException("DDS has invalid data");
                }

                switch (d3d10Ext.dxgiFormat)
                {
                    case DXGI_FORMAT_AI44:
                    case DXGI_FORMAT_IA44:
                    case DXGI_FORMAT_P8:
                    case DXGI_FORMAT_A8P8:
                        ThrowNotSupportedException("Format not supported");
                        break;
                    default:
                        if (SurfaceInfo.BitsPerPixel(d3d10Ext.dxgiFormat) == 0)
                        {
                            ThrowNotSupportedException("Format not supported");
                        }

                        break;
                }

                format = d3d10Ext.dxgiFormat;

                switch (d3d10Ext.resourceDimension)
                {
                    case D3D11_RESOURCE_DIMENSION_TEXTURE1D:
                        if ((header.flags & DDS_HEIGHT) != 0 && height != 1)
                            ThrowArgumentException("DDS has invalid data");
                        height = 1;
                        depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION_TEXTURE2D:
                        if ((d3d10Ext.miscFlag & D3D11_RESOURCE_MISC_TEXTURECUBE) != 0)
                        {
                            arraySize *= 6;
                            isCubeMap = true;
                        }
                        depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION_TEXTURE3D:
                        if ((header.flags & DDS_HEADER_FLAGS_VOLUME) == 0)
                            ThrowArgumentException("DDS has invalid data");
                        if (arraySize > 1)
                            ThrowNotSupportedException("Unsupported DDS dimension");
                        break;

                    default:
                        ThrowNotSupportedException("Unsupported DDS dimension");
                        break;
                }

                resDim = d3d10Ext.resourceDimension switch
                {
                    D3D11_RESOURCE_DIMENSION_TEXTURE1D => D3D12_RESOURCE_DIMENSION_TEXTURE1D,
                    D3D11_RESOURCE_DIMENSION_TEXTURE2D => D3D12_RESOURCE_DIMENSION_TEXTURE2D,
                    D3D11_RESOURCE_DIMENSION_TEXTURE3D => D3D12_RESOURCE_DIMENSION_TEXTURE3D,
                    _ => resDim
                };
            }
            else
            {
                format = InteropTypeUtilities.GetDxgiFormat(header.ddspf);

                if (format == DXGI_FORMAT_UNKNOWN)
                    ThrowNotSupportedException("Unsupported DXGI format");

                if ((header.flags & DDS_HEADER_FLAGS_VOLUME) != 0)
                {
                    resDim = (D3D12_RESOURCE_DIMENSION)D3D11_RESOURCE_DIMENSION_TEXTURE3D;
                }
                else
                {
                    if ((header.caps2 & DDS_CUBEMAP) != 0)
                    {
                        if ((header.caps2 & DDS_CUBEMAP_ALLFACES) != DDS_CUBEMAP_ALLFACES)
                        {
                            ThrowNotSupportedException("Not supported CubeMap format");
                        }

                        arraySize = 6;
                        isCubeMap = true;
                    }

                    depth = 1;
                    resDim = (D3D12_RESOURCE_DIMENSION)D3D11_RESOURCE_DIMENSION_TEXTURE2D;
                }

                Debug.Assert(SurfaceInfo.BitsPerPixel(format) != 0);
            }

            if (mipCount > D3D12.D3D12_REQ_MIP_LEVELS)
            {
                ThrowNotSupportedException($"{D3D12.D3D12_REQ_MIP_LEVELS} MIP levels are required");
            }

            var size = new Size3(height, width, depth);

            EnsureValidResourceSizeAndDimension(resDim, arraySize, isCubeMap, size);



            var subresourceData = FillSubresourceData(size, mipCount, arraySize, format, (uint)maxsize, metadata.BitData, out Size3 texSize, out uint skipMip);

            return new DdsTexture(metadata.BitData, resDim, texSize, mipCount - skipMip, arraySize, format, forceSrgb, isCubeMap, subresourceData, InteropTypeUtilities.GetAlphaMode(ref metadata.DdsHeader));
        }

        private static void EnsureValidResourceSizeAndDimension(D3D12_RESOURCE_DIMENSION resDim, in uint arraySize, in bool isCubeMap, in Size3 size)
        {
            switch (resDim)
            {
                case D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                    if (arraySize > D3D12.D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION
                        || size.Width > D3D12.D3D12_REQ_TEXTURE1D_U_DIMENSION)
                    {
                        ThrowNotSupportedException("Not supported arraySize or width");
                    }
                    break;

                case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    if (isCubeMap)
                    {
                        if (arraySize > D3D12.D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION
                            || size.Width > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION
                            || size.Height > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION)
                        {
                            ThrowNotSupportedException("Not supported arraySize, width, or height");
                        }
                    }
                    break;
                case D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                    if (arraySize > 1
                        || size.Width > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || size.Height > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || size.Depth > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
                    {
                        ThrowNotSupportedException("Not supported arraySize, width, height, or depth");
                    }
                    break;

                default:
                    ThrowNotSupportedException("Not supported dimension");
                    break;
            }
        }


        public static void RecordTextureUpload(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            in DdsTexture texture,
            out ID3D12Resource* textureBuffer,
            out ID3D12Resource* textureBufferUploadHeap)
        {
            if (device == null)
            {
                ThrowArgumentNullException(nameof(device));
            }


            DXGI_FORMAT format = texture.ForceSrgb ? InteropTypeUtilities.MakeSrgb(texture.Format) : texture.Format;

            textureBuffer = default;
            textureBufferUploadHeap = default;

            switch (texture.ResourceDimension)
            {
                case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    {
                        D3D12_RESOURCE_DESC texDesc;
                        texDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                        texDesc.Alignment = 0;
                        texDesc.Width = texture.Size.Width;
                        texDesc.Height = texture.Size.Height;
                        texDesc.DepthOrArraySize = (texture.Size.Depth > 1)
                            ? (ushort)texture.Size.Depth
                            : (ushort)texture.ArraySize;
                        texDesc.MipLevels = (ushort)texture.MipCount;
                        texDesc.Format = format;
                        texDesc.SampleDesc.Count = 1;
                        texDesc.SampleDesc.Quality = 0;
                        texDesc.Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN;
                        texDesc.Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE;

                        Guid iid = D3D12.IID_ID3D12Resource;
                        var defaultHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT);

                        {
                            ID3D12Resource* pTexture;
                            ThrowIfFailed(device->CreateCommittedResource(
                                &defaultHeapProperties,
                                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                                &texDesc,
                                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                                null,
                                &iid,
                                (void**)&pTexture));

                            textureBuffer = pTexture;
                        }

                        var num2DSubresources = (uint)(texDesc.DepthOrArraySize * texDesc.MipLevels);
                        ulong uploadBufferSize = D3D12.GetRequiredIntermediateSize(textureBuffer, 0, num2DSubresources);

                        var uploadHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD);
                        D3D12_RESOURCE_DESC buffer = D3D12_RESOURCE_DESC.Buffer(uploadBufferSize);

                        {
                            ID3D12Resource* pTextureUploadHeap;
                            ThrowIfFailed(device->CreateCommittedResource(
                                &uploadHeapProperties,
                                D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                                &buffer,
                                D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                                pOptimizedClearValue: null,
                                &iid,
                                (void**)&pTextureUploadHeap));

                            textureBufferUploadHeap = pTextureUploadHeap;
                        }

                        D3D12_RESOURCE_BARRIER commonToCopyDest = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST);
                        cmdList->ResourceBarrier(1, &commonToCopyDest);

                        fixed (ManagedSubresourceData* pManagedSubresourceData = texture.SubresourceData.Span)
                        fixed (byte* pBitData = texture.BitData.Span)
                        {
                            // Convert the ManagedSubresourceData to D3D12_SUBRESOURCE_DATA
                            // Just involves changing the offset (int32, relative to start of data) to an absolute pointer
                            for (var i = 0; i < num2DSubresources; i++)
                            {
                                var p = &pManagedSubresourceData[i];
                                ((D3D12_SUBRESOURCE_DATA*)p)->pData = pBitData + p->DataOffset;
                            }

                            D3D12.UpdateSubresources(
                                cmdList,
                                textureBuffer,
                                textureBufferUploadHeap,
                                0,
                                0,
                                num2DSubresources,
                                (D3D12_SUBRESOURCE_DATA*)pManagedSubresourceData);
                        }

                        D3D12_RESOURCE_BARRIER copyDestToSrv = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

                        cmdList->ResourceBarrier(1, &copyDestToSrv);
                        return;
                    }

                default:
                    ThrowNotSupportedException("Unsupported dimension");
                    return;
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
                ThrowArgumentException(nameof(bitData), "Cannot be empty");
            }

            skipMip = 0;
            texSize = default;
            uint offset = 0;

            var index = 0;

            var data = new ManagedSubresourceData[mipCount * arraySize];

            for (var i = 0U; i < arraySize; i++)
            {
                var tmpSize = (Size4)size;

                for (var j = 0U; j < mipCount; j++)
                {
                    var surface = SurfaceInfo.GetSurfaceInfo((Size2)tmpSize, format);

                    if (mipCount <= 1 || maxsize == 0 ||
                        tmpSize.Width <= maxsize && tmpSize.Height <= maxsize && tmpSize.Depth <= maxsize)
                    {
                        if (texSize.Width == 0)
                        {
                            texSize = (Size3)tmpSize;
                        }

                        Debug.Assert(index < mipCount * arraySize);
                        data[index] = new ManagedSubresourceData(offset, (IntPtr)surface.RowBytes, (IntPtr)surface.NumBytes);

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
                        ThrowArgumentException("File was too small");
                    }

                    if (Sse41.IsSupported)
                    {
                        var vector = Unsafe.As<Size4, Vector128<uint>>(ref tmpSize);
                        vector = Sse2.ShiftRightLogical(vector, 1);
                        vector = Sse41.Max(vector, Vector128.Create(1u));
                        tmpSize = Unsafe.As<Vector128<uint>, Size4>(ref vector);
                    }
                    else
                    {
                        tmpSize.Height >>= 1;
                        tmpSize.Width >>= 1;
                        tmpSize.Depth >>= 1;

                        if (tmpSize.Height == 0) tmpSize.Height = 1;
                        if (tmpSize.Width == 0) tmpSize.Width = 1;
                        if (tmpSize.Depth == 0) tmpSize.Depth = 1;
                    }

                    // TODO what can we assume for ISA
                }
            }

            if (index == 0)
                ThrowArgumentException("Size was 0");

            return data;
        }
    }
}
