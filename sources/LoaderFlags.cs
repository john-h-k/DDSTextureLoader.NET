﻿using System;
using JetBrains.Annotations;
using TerraFX.Interop;

namespace DDSTextureLoader.NET
{
    /// <summary>
    /// Defines the set of flags that can be passed to the texture loader
    /// </summary>
    [Flags]
    [PublicAPI]
    public enum LoaderFlags
    {
        /// <summary>
        /// No flags
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Convert the <see cref="DXGI_FORMAT"/> return format to the equivalent format with SRGB enabled.
        /// Note: No data conversion occurs
        /// </summary>
        ForceSrgb = 0x1,
        
        /// <summary>
        /// Reserve space for MIPs
        /// </summary>
        ReserveMips = 0x8,
    }
}