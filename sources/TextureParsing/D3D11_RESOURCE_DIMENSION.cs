﻿using System.Diagnostics.CodeAnalysis;

namespace DDSTextureLoader.NET.TextureParsing
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal enum D3D11_RESOURCE_DIMENSION : uint
    {
        D3D11_RESOURCE_DIMENSION_UNKNOWN = 0,
        D3D11_RESOURCE_DIMENSION_BUFFER = 1,
        D3D11_RESOURCE_DIMENSION_TEXTURE1D = 2,
        D3D11_RESOURCE_DIMENSION_TEXTURE2D = 3,
        D3D11_RESOURCE_DIMENSION_TEXTURE3D = 4
    }
}