using System;
using System.Buffers;
using System.IO;
using DDSTextureLoader.NET.TextureParsing;
using TerraFX.Interop;
using static TerraFX.Interop.D3D12_RESOURCE_DIMENSION;

namespace DDSTextureLoader.NET
{
    /// <summary>
    /// The type used for loading of DDS files
    /// </summary>
    public static unsafe class DdsTextureLoader
    {

        /// <summary>
        /// Create a DDS texture from a file
        /// </summary>
        /// <param name="fileName">The file to create from</param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// /// <param name="resourceFlags">The flags used during creation of the resource</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static DdsTextureDescription CreateDdsTexture(
            string fileName,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None)
        {
            if (fileName is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(fileName));
            }

            using var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read);

            return CreateDdsTexture(stream, mipMapMaxSize, resourceFlags, loaderFlags);
        }

        /// <summary>
        /// Create a DDS texture from a strean
        /// </summary>
        /// <param name="stream">The stream to create from</param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// <param name="resourceFlags">The flags used during creation of the resource</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static DdsTextureDescription CreateDdsTexture(
            Stream stream,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None)
        {
            if (stream is null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(stream));
            }

            var streamSize = stream.Length;
            if (streamSize > int.MaxValue)
            {
                ThrowHelper.ThrowArgumentException("File too large");
            }

            byte[]? data = null;
            try
            {
                data = ArrayPool<byte>.Shared.Rent((int) streamSize);
                stream.Read(data);
                return CreateDdsTexture(
                    data!,
                    mipMapMaxSize,
                    resourceFlags,
                    loaderFlags
                );
            }
            finally
            {
                if (data is object)
                {
                    ArrayPool<byte>.Shared.Return(data);
                }
            }
        }

        /// <summary>
        /// Create a DDS texture from memory
        /// </summary>
        /// <param name="ddsData">The memory where the DDS data is stored </param>
        /// <param name="mipMapMaxSize">The largest size a mipmap can be (all larger will be discarded)</param>
        /// <param name="resourceFlags">The flags used during creation of the resource</param>
        /// <param name="loaderFlags">The flags used by the loader</param>
        /// <returns>A descriptor struct of the DDS texture</returns>
        public static DdsTextureDescription CreateDdsTexture(
            Memory<byte> ddsData,
            uint mipMapMaxSize = default,
            D3D12_RESOURCE_FLAGS resourceFlags = D3D12_RESOURCE_FLAGS.D3D12_RESOURCE_FLAG_NONE,
            LoaderFlags loaderFlags = LoaderFlags.None)
        {
            if (ddsData.Length < sizeof(DdsHeader) + sizeof(uint))
            {
                ThrowHelper.ThrowArgumentException("Data too small to be a valid DDS file");
            }

            var metadata = FileMetadata.FromMemory(ddsData);

            return ImplementationFunctions.CreateTextureFromDds12(
                metadata,
                mipMapMaxSize,
                resourceFlags,
                loaderFlags
            );
        }

        /// <summary>
        /// Record an upload copy to the GPU to execute asynchronously
        /// </summary>
        /// <param name="device">The device to create resources on</param>
        /// <param name="cmdList">The command list to record to</param>
        /// <param name="textureDescription">The texture to be uploaded</param>
        /// <param name="textureBuffer">A resource buffer that will contain the uploaded texture</param>
        /// <param name="textureBufferUploadHeap">An intermediate buffer used to copy over the texture</param>
        public static void RecordTextureUpload(
            ID3D12Device* device,
            ID3D12GraphicsCommandList* cmdList,
            in DdsTextureDescription textureDescription,
            out ID3D12Resource* textureBuffer,
            out ID3D12Resource* textureBufferUploadHeap)
        {
            if (device == null)
            {
                ThrowHelper.ThrowArgumentNullException(nameof(device));
            }

            DXGI_FORMAT format = textureDescription.LoaderFlags.HasFlag(LoaderFlags.ForceSrgb)
                ? InteropTypeUtilities.MakeSrgb(textureDescription.Format)
                : textureDescription.Format;

            textureBuffer = default;
            textureBufferUploadHeap = default;

            ID3D12Fence* fence;

            Guid iid = D3D12.IID_ID3D12Fence;
            device->CreateFence(0, D3D12_FENCE_FLAGS.D3D12_FENCE_FLAG_NONE, &iid, (void**) &fence);

            fixed (char* pName = "ID3D12Fence")
            {
                fence->SetName((ushort*) pName);
            }

            switch (textureDescription.ResourceDimension)
            {
                case D3D12_RESOURCE_DIMENSION_TEXTURE2D:
                {
                    D3D12_RESOURCE_DESC texDesc;
                    texDesc.Dimension = D3D12_RESOURCE_DIMENSION_TEXTURE2D;
                    texDesc.Alignment = 0;
                    texDesc.Width = textureDescription.Width;
                    texDesc.Height = textureDescription.Height;
                    texDesc.DepthOrArraySize = (textureDescription.Depth > 1)
                        ? (ushort) textureDescription.Depth
                        : (ushort) textureDescription.ArraySize;
                    texDesc.MipLevels = (ushort) textureDescription.MipCount;
                    texDesc.Format = format;
                    texDesc.SampleDesc.Count = 1;
                    texDesc.SampleDesc.Quality = 0;
                    texDesc.Layout = D3D12_TEXTURE_LAYOUT.D3D12_TEXTURE_LAYOUT_UNKNOWN;
                    texDesc.Flags = textureDescription.ResourceFlags;

                    iid = D3D12.IID_ID3D12Resource;
                    var defaultHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_DEFAULT);

                    {
                        ID3D12Resource* pTexture;
                        ThrowHelper.ThrowIfFailed(device->CreateCommittedResource(
                            &defaultHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &texDesc,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                            null,
                            &iid,
                            (void**) &pTexture));

                        textureBuffer = pTexture;
                    }

                    var num2DSubresources = (uint) (texDesc.DepthOrArraySize * texDesc.MipLevels);
                    ulong uploadBufferSize = D3D12.GetRequiredIntermediateSize(textureBuffer, 0, num2DSubresources);

                    var uploadHeapProperties = new D3D12_HEAP_PROPERTIES(D3D12_HEAP_TYPE.D3D12_HEAP_TYPE_UPLOAD);
                    D3D12_RESOURCE_DESC buffer = D3D12_RESOURCE_DESC.Buffer(uploadBufferSize);

                    {
                        ID3D12Resource* pTextureUploadHeap;
                        ThrowHelper.ThrowIfFailed(device->CreateCommittedResource(
                            &uploadHeapProperties,
                            D3D12_HEAP_FLAGS.D3D12_HEAP_FLAG_NONE,
                            &buffer,
                            D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_GENERIC_READ,
                            pOptimizedClearValue: null,
                            &iid,
                            (void**) &pTextureUploadHeap));

                        textureBufferUploadHeap = pTextureUploadHeap;
                    }

                    D3D12_RESOURCE_BARRIER commonToCopyDest = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COMMON,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST);
                    cmdList->ResourceBarrier(1, &commonToCopyDest);

                    fixed (ManagedSubresourceData* pManagedSubresourceData = textureDescription.SubresourceData.Span)
                    fixed (byte* pBitData = textureDescription.BitData.Span)
                    {
                        // Convert the ManagedSubresourceData to D3D12_SUBRESOURCE_DATA
                        // Just involves changing the offset (int32, relative to start of data) to an absolute pointer
                        for (var i = 0; i < num2DSubresources; i++)
                        {
                            var p = &pManagedSubresourceData[i];
                            ((D3D12_SUBRESOURCE_DATA*) p)->pData = pBitData + p->DataOffset;
                        }

                        D3D12.UpdateSubresources(
                            cmdList,
                            textureBuffer,
                            textureBufferUploadHeap,
                            0,
                            0,
                            num2DSubresources,
                            (D3D12_SUBRESOURCE_DATA*) pManagedSubresourceData);
                    }

                    D3D12_RESOURCE_BARRIER copyDestToSrv = D3D12_RESOURCE_BARRIER.InitTransition(textureBuffer,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_COPY_DEST,
                        D3D12_RESOURCE_STATES.D3D12_RESOURCE_STATE_PIXEL_SHADER_RESOURCE);

                    cmdList->ResourceBarrier(1, &copyDestToSrv);
                    return;
                }

                default:
                    ThrowHelper.ThrowNotSupportedException("Unsupported dimension");
                    return;
            }
        }
    }
}