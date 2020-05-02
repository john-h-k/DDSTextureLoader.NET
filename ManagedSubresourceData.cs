using System;

#nullable enable

namespace DDSTextureLoader.NET
{
    public readonly struct ManagedSubresourceData
    {
        public ManagedSubresourceData(uint dataOffset, IntPtr rowPitch, IntPtr slicePitch)
        {
            _paddedOffset = (IntPtr)dataOffset;
            RowPitch = rowPitch;
            SlicePitch = slicePitch;
        }

        // Same format as D3D12_SUBRESOURCE_DATA, just with diff first member
        //
        // void* pData
        // IntPtr RowPitch
        // IntPtr SlicePitch
        //
        // Once the base pointer (which DataOffset is from) is pinned, you can read DataOffset, and it to the base pointer
        // and then reinterpret this type as a D3D12_SUBRESOURCE_DATA and write this value to pData
        // and then this object represents the correct subresource data for methods such as UpdateSubresources

        public uint DataOffset => (uint) _paddedOffset;
        private readonly IntPtr _paddedOffset;
        public readonly IntPtr RowPitch;
        public readonly IntPtr SlicePitch;
    }
}
