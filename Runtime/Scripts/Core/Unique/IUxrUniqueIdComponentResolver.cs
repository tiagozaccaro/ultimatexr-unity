// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUxrUniqueIdComponentResolver.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Core.Unique
{
    public interface IUxrUniqueIdComponentResolver
    {
        #region Public Methods

        /// <summary>
        ///     Tries to resolve a component using the debug information of a given component id. The debug information may
        ///     contain the name/path of the Component whose ID was used for serialization.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="debugInfo"></param>
        /// <returns></returns>
        public IUxrUniqueId TryResolveComponentUsingDebugInfo(Guid id, string debugInfo);

        #endregion
    }
}