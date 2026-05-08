// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUxrUniqueIdDebugInfoResolver.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Core.Unique
{
    public interface IUxrUniqueIdDebugInfoResolver
    {
        #region Public Methods

        /// <summary>
        ///     Tries to resolve the debug information for a given component id. The debug information contains the name/path of
        ///     the Component whose ID was used for serialization.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="debugInfo"></param>
        /// <returns></returns>
        public bool TryResolveUniqueIdDebugInfo(Guid id, out string debugInfo);

        #endregion
    }
}