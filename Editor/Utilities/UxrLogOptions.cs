// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LogOptions.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Editor.Utilities
{
    /// <summary>
    ///     Enumerates the different log option flags.
    /// </summary>
    [Flags]
    public enum UxrLogOptions
    {
        /// <summary>
        ///     Output information of processed components.
        /// </summary>
        Processed = 1 << 0,

        /// <summary>
        ///     Output information of components that were not processed (ignored).
        /// </summary>
        Ignored = 1 << 1
    }
}