using System;
using DDSTextureLoader.NET.TextureParsing;
using TerraFX.Interop;

#nullable enable

namespace DDSTextureLoader.NET
{
    /// <summary>
    /// Represents a DDS texture that has been loaded into memory and parsed
    /// </summary>
    public readonly struct DdsTextureDescription
    {
        internal DdsTextureDescription(
            Memory<byte> bitData,
            D3D12_RESOURCE_DIMENSION resourceDimension, 
            Size3 size, 
            uint mipCount, 
            uint arraySize, 
            DXGI_FORMAT format, 
            LoaderFlags loaderFlags, 
            D3D12_RESOURCE_FLAGS resourceFlags, 
            bool isCubeMap, 
            Memory<ManagedSubresourceData> subresourceData, 
            DDS_ALPHA_MODE alphaMode)
        {
            BitData = bitData;
            ResourceDimension = resourceDimension;
            _size = size;
            MipCount = mipCount;
            ArraySize = arraySize;
            Format = format;
            LoaderFlags = loaderFlags;
            ResourceFlags = resourceFlags;
            IsCubeMap = isCubeMap;
            SubresourceData = subresourceData;
            AlphaMode = alphaMode;
        }
        
        private readonly Size3 _size;
        
        /// <summary>
        /// The buffer that contains the data referenced by <see cref="SubresourceData"/>
        /// </summary>
        public Memory<byte> BitData { get; }
        
        /// <summary>
        /// The dimension of the DDS data
        /// </summary>
        public D3D12_RESOURCE_DIMENSION ResourceDimension { get; }
        
        /// <summary>
        /// The height of the texture
        /// </summary>
        public uint Height => _size.Height;
        
        /// <summary>
        /// The width of the texture
        /// </summary>
        public uint Width  => _size.Width;
        
        /// <summary>
        /// The depth of the texture
        /// </summary>
        public uint Depth => _size.Depth;
        
        /// <summary>
        /// The number of MIPs present
        /// </summary>
        public uint MipCount { get; }
        
        /// <summary>
        /// The size of the texture, if depth is 1
        /// </summary>
        public uint ArraySize { get; }
        
        /// <summary>
        /// The format of the texture
        /// </summary>
        public DXGI_FORMAT Format { get; }
        
        /// <summary>
        /// Flags used by the loader
        /// </summary>
        public LoaderFlags LoaderFlags { get; }
        
        /// <summary>
        /// Flags used during creation of the resource used for texture upload
        /// </summary>
        public D3D12_RESOURCE_FLAGS ResourceFlags { get; }
        
        /// <summary>
        /// Whether the texture is a cube map
        /// </summary>
        public bool IsCubeMap { get; }
        
        /// <summary>
        /// The subresource data, relative to <see cref="BitData"/>, for upload
        /// </summary>
        public Memory<ManagedSubresourceData> SubresourceData { get; }
        
        /// <summary>
        /// The alpha mode of the texture
        /// </summary>
        public DDS_ALPHA_MODE AlphaMode { get; }
    }
}
