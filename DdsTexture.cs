using System;
using TerraFX.Interop;

#nullable enable

namespace DDSTextureLoader.NET
{
    public readonly struct DdsTexture
    {
        internal DdsTexture(
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
            Size = size;
            MipCount = mipCount;
            ArraySize = arraySize;
            Format = format;
            ForceSrgb = forceSrgb;
            IsCubeMap = isCubeMap;
            SubresourceData = subresourceData;
            AlphaMode = alphaMode;
        }

        public Memory<byte> BitData { get; }
        public D3D12_RESOURCE_DIMENSION ResourceDimension { get; }
        public Size3 Size { get; }
        public uint MipCount { get; }
        public uint ArraySize { get; }
        public DXGI_FORMAT Format { get; }
        public bool ForceSrgb { get; }
        public bool IsCubeMap { get; }
        public Memory<ManagedSubresourceData> SubresourceData { get; }
        public DDS_ALPHA_MODE AlphaMode { get; }
    }
}
