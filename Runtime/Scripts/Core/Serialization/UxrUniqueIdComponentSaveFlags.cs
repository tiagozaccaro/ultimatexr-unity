// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUniqueIdComponentSaveFlags.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core.Unique;

namespace UltimateXR.Core.Serialization
{
    /// <summary>
    ///     Enumerates the different flags describing a <see cref="IUxrUniqueId" /> component is saved during serialization.
    /// </summary>
    [Flags]
    public enum UxrUniqueIdComponentSaveFlags
    {
        /// <summary>
        ///     If the flag is set, the component is not null. It's null if this flag is not set.
        /// </summary>
        NonNull = 1 << 0,

        /// <summary>
        ///     If the flag is set, the component will be serialized with
        /// </summary>
        DebugInfo = 1 << 1
    }
}