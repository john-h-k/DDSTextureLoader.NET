using System;
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
            bool forceSrgb, 
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
            ForceSrgb = forceSrgb;
            IsCubeMap = isCubeMap;
            SubresourceData = subresourceData;
            AlphaMode = alphaMode;
        }

        
        /// <summary>
        /// The buffer that contains the data referenced by <see cref="SubresourceData"/>
        /// </summary>
        public Memory<byte> BitData { get; }
        /// <summary>
        /// The dimension of the DDS data
        /// </summary>
        public D3D12_RESOURCE_DIMENSION ResourceDimension { get; }
        private readonly Size3 _size;
        /// <summary>
        /// The height
        /// </summary>
        public uint Height => _size.Height;
        public uint Width  => _size.Width;
        public uint Depth => _size.Depth;
        public uint MipCount { get; }
        public uint ArraySize { get; }
        public DXGI_FORMAT Format { get; }
        public bool ForceSrgb { get; }
        public bool IsCubeMap { get; }
        public Memory<ManagedSubresourceData> SubresourceData { get; }
        public DDS_ALPHA_MODE AlphaMode { get; }
    }
}
