using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using TerraFX.Interop;

#nullable enable

namespace DDSTextureLoader.NET
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DdsAlphaMode
    {
        DDS_ALPHA_MODE_UNKNOWN = 0,
        DDS_ALPHA_MODE_STRAIGHT = 1,
        DDS_ALPHA_MODE_PREMULTIPLIED = 2,
        DDS_ALPHA_MODE_OPAQUE = 3,
        DDS_ALPHA_MODE_CUSTOM = 4,
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum DDS_MISC_FLAGS2 : ulong
    {
        DDS_MISC_FLAGS2_ALPHA_MODE_MASK = 0x7L,
    };

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public enum D3D11_RESOURCE_DIMENSION : uint
    {
        D3D11_RESOURCE_DIMENSION_UNKNOWN = 0,
        D3D11_RESOURCE_DIMENSION_BUFFER = 1,
        D3D11_RESOURCE_DIMENSION_TEXTURE1D = 2,
        D3D11_RESOURCE_DIMENSION_TEXTURE2D = 3,
        D3D11_RESOURCE_DIMENSION_TEXTURE3D = 4
    }

    public static unsafe class DdsTextureLoader
    {
        // ReSharper disable twice InconsistentNaming

        private const uint DDS_FOURCC = 0x00000004;
        private const uint DDS_RGB = 0x00000040;
        private const uint DDS_LUMINANCE = 0x00020000;
        private const uint DDS_ALPHA = 0x00000002;

        private const uint DDS_HEADER_FLAGS_VOLUME = 0x00800000;

        private const uint DDS_HEIGHT = 0x00000002;
        private const uint DDS_WIDTH = 0x00000004;


        private const uint DDS_CUBEMAP_POSITIVEX = 0x00000600; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEX
        private const uint DDS_CUBEMAP_NEGATIVEX = 0x00000a00; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEX
        private const uint DDS_CUBEMAP_POSITIVEY = 0x00001200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEY
        private const uint DDS_CUBEMAP_NEGATIVEY = 0x00002200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEY
        private const uint DDS_CUBEMAP_POSITIVEZ = 0x00004200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_POSITIVEZ
        private const uint DDS_CUBEMAP_NEGATIVEZ = 0x00008200; // DDSCAPS2_CUBEMAP | DDSCAPS2_CUBEMAP_NEGATIVEZ

        private const uint DDS_CUBEMAP_ALLFACES = (DDS_CUBEMAP_POSITIVEX | DDS_CUBEMAP_NEGATIVEX |
                                                   DDS_CUBEMAP_POSITIVEY | DDS_CUBEMAP_NEGATIVEY |
                                                   DDS_CUBEMAP_POSITIVEZ | DDS_CUBEMAP_NEGATIVEZ);

        private const uint DDS_CUBEMAP = 0x00000200;

        private struct DdsPixelFormat
        {
            public uint size;
            public uint flags;
            public uint fourCC;
            public uint RGBBitCount;
            public uint RBitMask;
            public uint GBitMask;
            public uint BBitMask;
            public uint ABitMask;
        };

        private struct DdsHeader
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

        public struct DdsHeaderDxt10
        {
            public DXGI_FORMAT dxgiFormat;
            public uint resourceDimension;
            public uint miscFlag; // see D3D11_RESOURCE_MISC_FLAG
            public uint arraySize;
            public uint miscFlags2;
        }

        //public void CreateDdsTextureFromMemory(
        //    [In] ID3D12Device* device,
        //    [In] ID3D12GraphicsCommandList* cmdList,
        //    [In] byte* ddsData,
        //    [In] UIntPtr ddsDataSize,
        //    [Out] out ID3D12Resource* texture,
        //    [Out] out ID3D12Resource* textureUploadHeap,
        //    [In] UIntPtr maxsize = default,
        //    [Out] DdsAlphaMode* alphaMode = null
        //)
        //{
        // TODO
        //}

        public static void CreateDdsTextureFromFile(
            [In] ID3D12Device* device,
            [In] ID3D12GraphicsCommandList* cmdList,
            [In] string fileName,
            [Out] out ID3D12Resource* texture,
            [Out] out ID3D12Resource* textureUploadHeap,
            [In] UIntPtr maxsize = default,
            [Out] DdsAlphaMode* alphaMode = null
            )
        {
            if (alphaMode != null)
            {
                *alphaMode = DdsAlphaMode.DDS_ALPHA_MODE_UNKNOWN;
            }

            if (device == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(device));
            }

            if (fileName == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(fileName));
            }

            LoadTextureDataFromFile(
                fileName!,
                out byte* ddsData,
                out DdsHeader* header,
                out byte* bitData,
                out ulong bitSize);

            CreateTextureFromDDS12(device, cmdList, ref *header, bitData, bitSize, maxsize, forceSrgb: false,
                                   out texture, out textureUploadHeap);

            if (alphaMode != null)
            {
                *alphaMode = GetAlphaMode(ref *header);
            }
        }

        private static DdsAlphaMode GetAlphaMode(ref DdsHeader header)
        {
            if ((header.ddspf.flags & DDS_FOURCC) != 0)
            {
                if (MakeFourCC((byte)'D', (byte)'X', (byte)'1', (byte)'0') == header.ddspf.fourCC)
                {
                    ref DdsHeaderDxt10 d3d10ext = ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(ref Unsafe.Add(ref header, 1));
                    var mode = (DdsAlphaMode)(d3d10ext.miscFlags2 & (uint)DDS_MISC_FLAGS2.DDS_MISC_FLAGS2_ALPHA_MODE_MASK);
                    switch (mode)
                    {
                        case DdsAlphaMode.DDS_ALPHA_MODE_STRAIGHT:
                        case DdsAlphaMode.DDS_ALPHA_MODE_PREMULTIPLIED:
                        case DdsAlphaMode.DDS_ALPHA_MODE_OPAQUE:
                        case DdsAlphaMode.DDS_ALPHA_MODE_CUSTOM:
                            return mode;
                    }
                }
                else if ((MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'2') == header.ddspf.fourCC)
                         || (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'4') == header.ddspf.fourCC))
                {
                    return DdsAlphaMode.DDS_ALPHA_MODE_PREMULTIPLIED;
                }
            }

            return DdsAlphaMode.DDS_ALPHA_MODE_UNKNOWN;
        }

        private static void CreateTextureFromDDS12(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            ref DdsHeader header,
            byte* bitData,
            ulong bitSize,
            UIntPtr maxsize,
            bool forceSrgb,
            out ID3D12Resource* texture,
            out ID3D12Resource* textureUploadHeap
            )
        {
            uint width = header.width;
            uint height = header.height;
            uint depth = header.depth;

            var resDim = (uint)D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_UNKNOWN;

            uint arraySize = 1;

            DXGI_FORMAT format = DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
            bool isCubeMap = false;

            ulong mipCount = header.mipMapCount;

            if (mipCount == 0)
            {
                mipCount = 1;
            }

            if (((header.ddspf.flags & DDS_FOURCC) != 0)
                && (header.ddspf.fourCC == MakeFourCC((byte)'D', (byte)'X', (byte)'1', (byte)'0')))
            {
                ref DdsHeaderDxt10 d3d10ext =
                    ref Unsafe.As<DdsHeader, DdsHeaderDxt10>(
                        ref Unsafe.Add(ref header, 1));

                arraySize = d3d10ext.arraySize;

                if (arraySize == 0)
                {
                    ThrowHelper.ThrowArgumentException("DDS has invalid data");
                }

                switch (d3d10ext.dxgiFormat)
                {
                    case DXGI_FORMAT.DXGI_FORMAT_AI44:
                    case DXGI_FORMAT.DXGI_FORMAT_IA44:
                    case DXGI_FORMAT.DXGI_FORMAT_P8:
                    case DXGI_FORMAT.DXGI_FORMAT_A8P8:
                        ThrowHelper.ThrowNotSupportedException("Format not supported");
                        break;
                    default:
                        if (BitsPerPixel(d3d10ext.dxgiFormat) == 0)
                        {
                            ThrowHelper.ThrowNotSupportedException("Format not supported");
                        }

                        break;
                }

                format = d3d10ext.dxgiFormat;

                switch ((D3D11_RESOURCE_DIMENSION)d3d10ext.resourceDimension)
                {
                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE1D:
                        if ((header.flags & 2) != 0 && height != 1)
                            ThrowHelper.ThrowArgumentException("DDS has invalid data");
                        height = depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D:
                        if ((d3d10ext.miscFlag & 4) != 0)
                        {
                            arraySize *= 6;
                            isCubeMap = true;
                        }
                        depth = 1;
                        break;

                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D:
                        if ((header.flags & DDS_HEADER_FLAGS_VOLUME) == 0)
                            ThrowHelper.ThrowArgumentException("DDS has invalid data");
                        if (arraySize > 1)
                            ThrowHelper.ThrowNotSupportedException("Unsupported DDS dimension");
                        break;

                    default:
                        ThrowHelper.ThrowNotSupportedException("Unsupported DDS dimension");
                        break;
                }

                switch ((D3D11_RESOURCE_DIMENSION)d3d10ext.resourceDimension)
                {
                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE1D:
                        resDim = (uint)D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D;
                        break;
                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D:
                        resDim = (uint)D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                        break;
                    case D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D:
                        resDim = (uint)D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D;
                        break;
                }
            }
            else
            {
                format = GetDxgiFormat(header.ddspf);

                if (format == DXGI_FORMAT.DXGI_FORMAT_UNKNOWN)
                    ThrowHelper.ThrowNotSupportedException("Unsupported DXGI format");

                if ((header.flags & DDS_HEADER_FLAGS_VOLUME) != 0)
                {
                    resDim = (uint)D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE3D;
                }
                else
                {
                    if ((header.caps2 & DDS_CUBEMAP) != 0)
                    {
                        if ((header.caps2 & DDS_CUBEMAP_ALLFACES) != DDS_CUBEMAP_ALLFACES)
                        {
                            ThrowHelper.ThrowNotSupportedException("Not supported CubeMap format");
                        }

                        arraySize = 6;
                        isCubeMap = true;
                    }

                    depth = 1;
                    resDim = (uint)D3D11_RESOURCE_DIMENSION.D3D11_RESOURCE_DIMENSION_TEXTURE2D;
                }

                Debug.Assert(BitsPerPixel(format) != 0);
            }

            if (mipCount > D3D12.D3D12_REQ_MIP_LEVELS)
            {
                ThrowHelper.ThrowNotSupportedException($"{D3D12.D3D12_REQ_MIP_LEVELS} MIP levels are required");
            }

            switch ((D3D12_RESOURCE_DIMENSION)resDim)
            {
                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE1D:
                    if (arraySize > D3D12.D3D12_REQ_TEXTURE1D_ARRAY_AXIS_DIMENSION
                        || width > D3D12.D3D12_REQ_TEXTURE1D_U_DIMENSION)
                    {
                        ThrowHelper.ThrowNotSupportedException("Not supported arraySize or width");
                    }
                    break;

                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    if (isCubeMap)
                    {
                        if (arraySize > D3D12.D3D12_REQ_TEXTURE2D_ARRAY_AXIS_DIMENSION
                            || width > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION
                            || height > D3D12.D3D12_REQ_TEXTURECUBE_DIMENSION)
                        {
                            ThrowHelper.ThrowNotSupportedException("Not supported arraySize, width, or height");
                        }
                    }
                    break;
                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE3D:
                    if (arraySize > 1
                        || width > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || height > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION
                        || depth > D3D12.D3D12_REQ_TEXTURE3D_U_V_OR_W_DIMENSION)
                    {
                        ThrowHelper.ThrowNotSupportedException("Not supported arraySize, width, height, or depth");
                    }
                    break;

                default:
                    ThrowHelper.ThrowNotSupportedException("Not supported dimension");
                    break;
            }

            var initData = (D3D12_SUBRESOURCE_DATA*)Marshal.AllocHGlobal((IntPtr)((uint)sizeof(D3D12_SUBRESOURCE_DATA) * mipCount * arraySize));

            FillInitData12(
                width, height, depth, mipCount, arraySize, format, (ulong)maxsize, bitSize, bitData,
                out ulong twidth, out ulong theight, out ulong tdepth, out ulong skipMip, initData);

            CreateD3DResources12(
                device, cmdList,
                resDim, twidth, theight, tdepth,
                mipCount - skipMip,
                arraySize,
                format,
                false, // forceSRGB
                isCubeMap,
                initData,
                out texture,
                out textureUploadHeap);
        }

        private static void CreateD3DResources12(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            uint resDim,
            ulong width,
            ulong height,
            ulong depth,
            ulong mipCount,
            uint arraySize,
            DXGI_FORMAT format,
            bool forceSrgb,
            bool isCubeMap,
            D3D12_SUBRESOURCE_DATA* initData,
            out ID3D12Resource* texture,
            out ID3D12Resource* textureUploadHeap)
        {
            if (device == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(device));
            }

            if (forceSrgb)
                format = MakeSrgb(format);

            texture = default;
            textureUploadHeap = default;

            switch ((D3D12_RESOURCE_DIMENSION)resDim)
            {
                case D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                    D3D12_RESOURCE_DESC texDesc = default;
                    texDesc.Dimension = D3D12_RESOURCE_DIMENSION.D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                    texDesc.Alignment = 0;
                    texDesc.Width = width;
                    texDesc.Height = (uint)height;
                    texDesc.DepthOrArraySize = (depth > 1) ? (ushort)depth : (ushort)arraySize;
                    texDesc.MipLevels = (ushort)mipCount;
                    texDesc.Format = format;
                    texDesc.SampleDesc.Count = 1;
                    texDesc.SampleDesc.Quality = 0;
                    texDesc.Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN;
                    texDesc.Flags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE;

                    Guid iid = D3D12.IID_ID3D12Resource;
                    var defaultHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT);

                    int hr;
                    {
                        ID3D12Resource* pTexture;
                        hr = device->CreateCommittedResource(
                            &defaultHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &texDesc,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                            null,
                            &iid,
                            (void**)&pTexture);

                        texture = pTexture;
                    }

                    Windows.ThrowExternalExceptionIfFailed(nameof(ID3D12Device.CreateCommittedResource), hr);

                    var num2DSubresources = (uint)(texDesc.DepthOrArraySize * texDesc.MipLevels);
                    ulong uploadBufferSize = D3D12.GetRequiredIntermediateSize(texture, 0, num2DSubresources);

                    var uploadHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD);
                    D3D12_RESOURCE_DESC buffer = D3D12_RESOURCE_DESC.Buffer(uploadBufferSize);

                    {
                        ID3D12Resource* pTextureUploadHeap;
                        hr = device->CreateCommittedResource(
                            &uploadHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &buffer,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                            null,
                            &iid,
                            (void**)&pTextureUploadHeap);

                        textureUploadHeap = pTextureUploadHeap;
                    }

                    Windows.ThrowExternalExceptionIfFailed(nameof(ID3D12Device.CreateCommittedResource), hr);

                    D3D12_RESOURCE_BARRIER commonToCopyDest = D3D12_RESOURCE_BARRIER.InitTransition(texture,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON, D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST);
                    cmdList->ResourceBarrier(1, &commonToCopyDest);

                    D3D12.UpdateSubresources(
                        cmdList,
                        texture,
                        textureUploadHeap,
                        0,
                        0,
                        num2DSubresources,
                        initData);

                    D3D12_RESOURCE_BARRIER copyDestToSrv = D3D12_RESOURCE_BARRIER.InitTransition(texture,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST, D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

                    cmdList->ResourceBarrier(1, &copyDestToSrv);

                    break;
            }

        }

        private static DXGI_FORMAT MakeSrgb(DXGI_FORMAT format)
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

        private static void FillInitData12(
            ulong width,
            ulong height,
            ulong depth,
            ulong mipCount,
            ulong arraySize,
            DXGI_FORMAT format,
            ulong maxsize,
            ulong bitSize,
            byte* bitData,
            out ulong twidth,
            out ulong theight,
            out ulong tdepth,
            out ulong skipMip,
            D3D12_SUBRESOURCE_DATA* initData)
        {
            if (initData == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(initData));
            }

            if (bitData == null || bitSize == 0)
            {
                ThrowHelper.ThrowArgumentException(nameof(bitData), "Cannot be empty");
            }

            skipMip = 0;
            twidth = 0;
            theight = 0;
            tdepth = 0;

            ulong NumBytes = 0;
            ulong RowBytes = 0;

            byte* pSrcBits = bitData;
            byte* pEndBits = bitData + bitSize;

            ulong index = 0;

            for (var i = 0U; i < arraySize; i++)
            {
                ulong w = width;
                ulong h = height;
                ulong d = depth;

                for (var j = 0U; j < mipCount; j++)
                {
                    GetSurfaceInfo(
                        w,
                        h,
                        format,
                        out NumBytes,
                        out RowBytes,
                        out _);

                    if (mipCount <= 1 || maxsize == 0 ||
                        w <= maxsize && h <= maxsize && d <= maxsize)
                    {
                        if (twidth == 0)
                        {
                            twidth = w;
                            theight = h;
                            tdepth = d;
                        }

                        Debug.Assert(index < mipCount * arraySize);
                        initData[index].pData = pSrcBits;
                        initData[index].RowPitch = (IntPtr)RowBytes;
                        initData[index].SlicePitch = (IntPtr)NumBytes;

                        index++;
                    }
                    else if (j == 0)
                    {
                        // Count number of skipped mipmaps (first item only)
                        ++skipMip;
                    }

                    if (pSrcBits + (NumBytes * d) > pEndBits)
                    {
                        ThrowHelper.ThrowArgumentException("File was too small");
                    }

                    pSrcBits += NumBytes * d;

                    w >>= 1;
                    h >>= 1;
                    d >>= 1;
                    if (w == 0)
                    {
                        w = 1;
                    }
                    if (h == 0)
                    {
                        h = 1;
                    }
                    if (d == 0)
                    {
                        d = 1;
                    }
                }
            }

            if (index == 0)
                ThrowHelper.ThrowArgumentException("Size was 0");
        }

        private static void GetSurfaceInfo(
            ulong width,
            ulong height,
            DXGI_FORMAT format,
            out ulong outNumBytes,
            out ulong outRowBytes,
            out ulong outNumRows)
        {
            ulong numBytes = 0;
            ulong rowBytes = 0;
            ulong numRows = 0;

            var bc = false;
            var packed = false;
            var planar = false;
            ulong bpe = 0;

            switch (format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                    bc = true;
                    bpe = 8;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                    bc = true;
                    bpe = 16;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_YUY2:
                    packed = true;
                    bpe = 4;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_Y210:
                case DXGI_FORMAT.DXGI_FORMAT_Y216:
                    packed = true;
                    bpe = 8;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_NV12:
                case DXGI_FORMAT.DXGI_FORMAT_420_OPAQUE:
                    planar = true;
                    bpe = 2;
                    break;

                case DXGI_FORMAT.DXGI_FORMAT_P010:
                case DXGI_FORMAT.DXGI_FORMAT_P016:
                    planar = true;
                    bpe = 4;
                    break;
            }

            if (bc)
            {
                ulong numBlocksWide = 0;
                if (width > 0)
                {
                    numBlocksWide = Math.Max(1, (width + 3) / 4);
                }

                ulong numBlocksHigh = 0;
                if (height > 0)
                {
                    numBlocksWide = Math.Max(1, (height + 3) / 4);
                }

                rowBytes = numBlocksWide * bpe;
                numRows = numBlocksHigh;
                numBlocksHigh = rowBytes * numBlocksHigh;
            }
            else if (packed)
            {
                rowBytes = ((width + 1) >> 1) * bpe;
                numRows = height;
                numBytes = rowBytes * height;
            }
            else if (format == DXGI_FORMAT.DXGI_FORMAT_NV11)
            {
                rowBytes = ((width + 3) >> 2) * 4;
                numRows = height * 2;
                numBytes = rowBytes * numRows;
            }
            else if (planar)
            {
                rowBytes = ((width + 1) >> 1) * bpe;
                numBytes = (rowBytes * height) + ((rowBytes * height + 1) >> 1);
                numRows = height + ((height + 1) >> 1);
            }
            else
            {
                ulong bpp = BitsPerPixel(format);
                rowBytes = (width * bpp + 7) / 8; // round up to nearest byte
                numRows = height;
                numBytes = rowBytes * height;
            }

            outNumBytes = numBytes;
            outRowBytes = rowBytes;
            outNumRows = numRows;
        }

        private static bool IsBitmask(uint r, uint g, uint b, uint a, DdsPixelFormat ddpf)
        {
            return ddpf.RBitMask == r && ddpf.GBitMask == g && ddpf.BBitMask == b && ddpf.ABitMask == a;
        }

        private static DXGI_FORMAT GetDxgiFormat(DdsPixelFormat ddpf)
        {
            if ((ddpf.flags & DDS_RGB) != 0)
            {
                switch (ddpf.RGBBitCount)
                {
                    case 32:
                        if (IsBitmask(0x000000ff, 0x0000ff00, 0x00ff0000, 0xff000000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM;
                        }

                        if (IsBitmask(0x00ff0000, 0x0000ff00, 0x000000ff, 0xff000000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM;
                        }

                        if (IsBitmask(0x00ff0000, 0x0000ff00, 0x000000ff, 0x00000000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM;
                        }

                        if (IsBitmask(0x3ff00000, 0x000ffc00, 0x000003ff, 0xc0000000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x000003ff,0x000ffc00,0x3ff00000,0xc0000000) aka D3DFMT_A2R10G10B10

                        if (IsBitmask(0x0000ffff, 0xffff0000, 0x00000000, 0x00000000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM;
                        }

                        if (IsBitmask(0xffffffff, 0x00000000, 0x00000000, 0x00000000, ddpf))
                        {
                            // Only 32-bit color channel format in D3D9 was R32F
                            return DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT; // D3DX writes this out as a FourCC of 114
                        }

                        break;
                    case 24:
                        break;
                    case 16:
                        if (IsBitmask(0x7c00, 0x03e0, 0x001f, 0x8000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM;
                        }

                        if (IsBitmask(0xf800, 0x07e0, 0x001f, 0x0000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x7c00,0x03e0,0x001f,0x0000) aka D3DFMT_X1R5G5B5

                        if (IsBitmask(0x0f00, 0x00f0, 0x000f, 0xf000, ddpf))
                        {
                            return DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM;
                        }

                        // No DXGI format maps to ISBITMASK(0x0f00,0x00f0,0x000f,0x0000) aka D3DFMT_X4R4G4B4

                        // No 3:3:2, 3:3:2:8, or paletted DXGI formats aka D3DFMT_A8R3G3B2, D3DFMT_R3G3B2, D3DFMT_P8, D3DFMT_A8P8, etc.
                        break;
                }
            }
            else if ((ddpf.flags & DDS_LUMINANCE) != 0)
            {
                if (ddpf.RGBBitCount == 8)
                {
                    if (IsBitmask(0x000000ff, 0x00000000, 0x00000000, 0x00000000, ddpf))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R8_UNORM; // D3DX10/11 writes this out as DX10 extension
                    }
                }

                if (ddpf.RGBBitCount == 16)
                {
                    if (IsBitmask(0x0000ffff, 0x00000000, 0x00000000, 0x00000000, ddpf))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R16_UNORM; // D3DX10/11 writes this out as DX10 extension
                    }
                    if (IsBitmask(0x000000ff, 0x00000000, 0x00000000, 0x0000ff00, ddpf))
                    {
                        return DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM; // D3DX10/11 writes this out as DX10 extension
                    }
                }
            }
            else if ((ddpf.flags & DDS_ALPHA) != 0)
            {
                if (ddpf.RGBBitCount == 8)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_A8_UNORM;
                }
            }
            else if ((ddpf.flags & DDS_FOURCC) != 0)
            {
                if (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'1') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM;
                }

                if (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'3') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'5') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                // While pre-multiplied alpha isn't directly supported by the DXGI formats,
                // they are basically the same as these BC formats so they can be mapped
                if (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM;
                }

                if (MakeFourCC((byte)'D', (byte)'X', (byte)'T', (byte)'4') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM;
                }

                if (MakeFourCC((byte)'A', (byte)'T', (byte)'I', (byte)'1') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC((byte)'B', (byte)'C', (byte)'4', (byte)'U') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM;
                }

                if (MakeFourCC((byte)'B', (byte)'C', (byte)'4', (byte)'S') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM;
                }

                if (MakeFourCC((byte)'A', (byte)'T', (byte)'I', (byte)'2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC((byte)'B', (byte)'C', (byte)'5', (byte)'U') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM;
                }

                if (MakeFourCC((byte)'B', (byte)'C', (byte)'5', (byte)'S') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM;
                }

                // BC6H and BC7 are written using the "DX10" extended header

                if (MakeFourCC((byte)'R', (byte)'G', (byte)'B', (byte)'G') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM;
                }

                if (MakeFourCC((byte)'G', (byte)'R', (byte)'G', (byte)'B') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM;
                }

                if (MakeFourCC((byte)'Y', (byte)'U', (byte)'Y', (byte)'2') == ddpf.fourCC)
                {
                    return DXGI_FORMAT.DXGI_FORMAT_YUY2;
                }

                switch (ddpf.fourCC)
                {
                    case 36: // D3DFMT_A16B16G16R16
                        return DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM;

                    case 110: // D3DFMT_Q16W16V16U16
                        return DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM;

                    case 111: // D3DFMT_R16F
                        return DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT;

                    case 112: // D3DFMT_G16R16F
                        return DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT;

                    case 113: // D3DFMT_A16B16G16R16F
                        return DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT;

                    case 114: // D3DFMT_R32F
                        return DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT;

                    case 115: // D3DFMT_G32R32F
                        return DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT;

                    case 116: // D3DFMT_A32B32G32R32F
                        return DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT;
                }
            }

            return DXGI_FORMAT.DXGI_FORMAT_UNKNOWN;
        }

        private static uint BitsPerPixel(DXGI_FORMAT format)
        {
            switch (format)
            {
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT:
                    return 128;

                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32B32_SINT:
                    return 96;

                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G32_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32G8X24_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT_S8X24_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT_X8X24_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_X32_TYPELESS_G8X24_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_Y416:
                case DXGI_FORMAT.DXGI_FORMAT_Y210:
                case DXGI_FORMAT.DXGI_FORMAT_Y216:
                    return 64;

                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10A2_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R11G11B10_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16G16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R32_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R24G8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_D24_UNORM_S8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R24_UNORM_X8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_X24_TYPELESS_G8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R9G9B9E5_SHAREDEXP:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_B8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_G8R8_G8B8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R10G10B10_XR_BIAS_A2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8A8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_B8G8R8X8_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_AYUV:
                case DXGI_FORMAT.DXGI_FORMAT_Y410:
                case DXGI_FORMAT.DXGI_FORMAT_YUY2:
                    return 32;

                case DXGI_FORMAT.DXGI_FORMAT_P010:
                case DXGI_FORMAT.DXGI_FORMAT_P016:
                    return 24;

                case DXGI_FORMAT.DXGI_FORMAT_R8G8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8G8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R16_FLOAT:
                case DXGI_FORMAT.DXGI_FORMAT_D16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R16_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R16_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_B5G6R5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_B5G5R5A1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_A8P8:
                case DXGI_FORMAT.DXGI_FORMAT_B4G4R4A4_UNORM:
                    return 16;

                case DXGI_FORMAT.DXGI_FORMAT_NV12:
                case DXGI_FORMAT.DXGI_FORMAT_420_OPAQUE:
                case DXGI_FORMAT.DXGI_FORMAT_NV11:
                    return 12;

                case DXGI_FORMAT.DXGI_FORMAT_R8_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_R8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8_UINT:
                case DXGI_FORMAT.DXGI_FORMAT_R8_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_R8_SINT:
                case DXGI_FORMAT.DXGI_FORMAT_A8_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_AI44:
                case DXGI_FORMAT.DXGI_FORMAT_IA44:
                case DXGI_FORMAT.DXGI_FORMAT_P8:
                    return 8;

                case DXGI_FORMAT.DXGI_FORMAT_R1_UNORM:
                    return 1;

                case DXGI_FORMAT.DXGI_FORMAT_BC1_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC4_SNORM:
                    return 4;

                case DXGI_FORMAT.DXGI_FORMAT_BC2_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC5_SNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC6H_SF16:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_TYPELESS:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM:
                case DXGI_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB:
                    return 8;

                default:
                    return 0;
            }
        }

        private static void LoadTextureDataFromFile(
            string fileName,
            out byte* ddsData,
            out DdsHeader* header,
            out byte* bitData,
            out ulong bitSize)
        {
            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            long fileSize = stream.Length;

            if (fileSize > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("File too large");
            }

            if (fileSize < sizeof(DdsHeader) + sizeof(uint))
            {
                ThrowHelper.ThrowArgumentException("File too small to be a valid DDS file");
            }

            ddsData = (byte*)Marshal.AllocHGlobal((IntPtr)fileSize);
            var temp = new byte[fileSize];

            stream.Read(temp, 0, (int)fileSize);
            temp.AsSpan().CopyTo(new Span<byte>(ddsData, checked((int)fileSize)));

            var magicNum = Unsafe.ReadUnaligned<uint>(ddsData);

            if (magicNum != 0x20534444 /* "DDS " */)
            {
                ThrowHelper.ThrowArgumentException("File not a valid DDS file");
            }

            var hdr = (DdsHeader*)(ddsData + sizeof(uint));

            var bDxt10Header = false;
            if ((hdr->ddspf.flags & DDS_FOURCC) != 0
                && MakeFourCC((byte)'D', (byte)'X', (byte)'1', (byte)'0') == hdr->ddspf.fourCC)
            {
                if (fileSize < sizeof(DdsHeader) + sizeof(uint) + sizeof(DdsHeaderDxt10))
                {
                    ThrowHelper.ThrowArgumentException("File too small to be a valid DDS file");
                }

                bDxt10Header = true;
            }

            header = hdr;

            int offset = sizeof(uint) + sizeof(DdsHeader) + (bDxt10Header ? sizeof(DdsHeaderDxt10) : 0);

            bitData = ddsData + offset;
            bitSize = (ulong)(fileSize - offset);
        }

        private static uint MakeFourCC(byte ch0, byte ch1, byte ch2, byte ch3)
        {
            return ch0 |
                   ((uint)ch1 << 8) |
                   ((uint)ch2 << 16) |
                   ((uint)ch3 << 24);
        }
    }

    [DebuggerNonUserCode]
    [DebuggerStepThrough]
    internal static class ThrowHelper
    {
        [DebuggerHidden]
        public static void ThrowArgumentException(string paramName, Exception inner) => throw new ArgumentException(paramName, inner);

        [DebuggerHidden]
        public static void ThrowArgumentException(string paramName, string message) => throw new ArgumentException(paramName, message);

        [DebuggerHidden]
        public static void ThrowArgumentException(string paramName) => throw new ArgumentException(paramName);

        [DebuggerHidden]
        public static void ThrowArgumentNullException(string paramName, Exception inner) => throw new ArgumentNullException(paramName, inner);

        [DebuggerHidden]
        public static void ThrowArgumentNullException(string paramName, string message) => throw new ArgumentNullException(paramName, message);

        [DebuggerHidden]
        public static void ThrowArgumentNullException(string paramName) => throw new ArgumentNullException(paramName);

        [DebuggerHidden]
        public static void ThrowArgumentOutOfRangeException(string paramName, Exception inner) => throw new ArgumentOutOfRangeException(paramName, inner);

        [DebuggerHidden]
        public static void ThrowArgumentOutOfRangeException(string paramName, string message) => throw new ArgumentOutOfRangeException(paramName, message);

        [DebuggerHidden]
        public static void ThrowArgumentOutOfRangeException(string paramName) => throw new ArgumentOutOfRangeException(paramName);

        [DebuggerHidden]
        public static void ThrowInvalidOperationException(string message, Exception? inner = null) => throw new InvalidOperationException(message, inner);

        [DebuggerHidden]
        public static void ThrowPlatformNotSupportedException(string message, Exception? inner = null) => throw new PlatformNotSupportedException(message, inner);

        [DebuggerHidden]
        public static void ThrowNotSupportedException(string message, Exception? inner = null) => throw new NotSupportedException(message, inner);
    }
}
