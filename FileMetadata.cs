﻿using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static DDSTextureLoader.NET.DdsContants;

#nullable enable

namespace DDSTextureLoader.NET
{

    public static unsafe partial class DdsTextureLoader
    {
        private readonly ref struct FileMetadata
        {
            public static FileMetadata FromMemory(Memory<byte> ddsData)
            {
                Debug.Assert(!ddsData.IsEmpty);
                var dataStart = MemoryMarshal.GetReference(ddsData.Span);

                var magicNum = Unsafe.ReadUnaligned<uint>(ref dataStart);

                if (magicNum != 0x20534444 /* "DDS " */)
                {
                    ThrowHelper.ThrowArgumentException("File not a valid DDS file");
                }

                ref DdsHeader header = ref Unsafe.As<byte, DdsHeader>(ref Unsafe.Add(ref dataStart, sizeof(uint)));

                var hasDxt10Header = false;
                if ((header.ddspf.flags & DDS_FOURCC) != 0
                    && InteropTypeUtilities.MakeFourCC('D', 'X', '1', '0') == header.ddspf.fourCC)
                {
                    if ((void*)ddsData.Length < (void*)(sizeof(DdsHeader) + sizeof(uint) + sizeof(DdsHeaderDxt10)))
                    {
                        ThrowHelper.ThrowArgumentException("File too small to be a valid DDS file");
                    }

                    hasDxt10Header = true;
                }

                int offset = sizeof(uint) + sizeof(DdsHeader) + (hasDxt10Header ? sizeof(DdsHeaderDxt10) : 0);

                return new FileMetadata(ref header, ddsData.Slice(offset));
            }

            private FileMetadata(ref DdsHeader ddsHeader, Memory<byte> bitData)
            {
                _ddsHeader = MemoryMarshal.CreateSpan(ref ddsHeader, 1);
                BitData = bitData;
            }
            private readonly Span<DdsHeader> _ddsHeader;
            public ref DdsHeader DdsHeader => ref _ddsHeader[0];

            public readonly Memory<byte> BitData;
        }
    }
}