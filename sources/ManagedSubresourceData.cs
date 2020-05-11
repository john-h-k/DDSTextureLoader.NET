using System;
using JetBrains.Annotations;

namespace DDSTextureLoader.NET
{
    /// <summary>
    /// Represents a section of data in a single resource
    /// </summary>
    [PublicAPI]
    public readonly struct ManagedSubresourceData
    {
        /// <summary>
        /// Creates a new instance of <see cref="ManagedSubresourceData"/>
        /// </summary>
        /// <param name="dataOffset">The offset from the resource start, in bytes</param>
        /// <param name="rowPitch">The row pitch, or width, or physical size, in bytes, of the subresource data</param>
        /// <param name="slicePitch">The depth pitch, or width, or physical size, in bytes, of the subresource data</param>
        public ManagedSubresourceData(uint dataOffset, uint rowPitch, uint slicePitch)
        {
            _paddedOffset = (IntPtr) dataOffset;
            _rowPitch = (IntPtr) rowPitch;
            _slicePitch = (IntPtr) slicePitch;
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

        /// <summary>
        /// The offset from the resource start, in bytes
        /// </summary>
        public uint DataOffset => (uint) _paddedOffset;
        
        /// <summary>
        /// The row pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public uint RowPitch => (uint) _rowPitch;
        
        /// <summary>
        /// The depth pitch, or width, or physical size, in bytes, of the subresource data
        /// </summary>
        public uint SlicePitch => (uint) _slicePitch;

        private readonly IntPtr _paddedOffset;
        private readonly IntPtr _rowPitch;
        private readonly IntPtr _slicePitch;
    }
}