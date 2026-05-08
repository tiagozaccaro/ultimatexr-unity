// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUniqueIdDebugLevel.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Exceptions;
using UltimateXR.Extensions.Unity;

namespace UltimateXR.Core.Unique
{
    /// <summary>
    ///     Controls the amount of debug information included when serializing a <see cref="IUxrUniqueId" /> component. During
    ///     development this helps debug <see cref="UxrComponentNotFoundException" /> exceptions by including the component
    ///     name and path in the scene next to the <see cref="Guid" />.
    /// </summary>
    public enum UxrUniqueIdDebugLevel
    {
        /// <summary>
        ///     No debug information is included.
        /// </summary>
        NoDebug = 0,

        /// <summary>
        ///     The GameObject name is included as debug information.
        /// </summary>
        ObjectName = 1,

        /// <summary>
        ///     The GameObject name plus the Component type is included as debug information.
        /// </summary>
        ObjectNameAndComponentType = 2,

        /// <summary>
        ///     The path in the scene is included as debug information. This is the path returned by
        ///     <see cref="ComponentExt.GetPathUnderScene" />.
        /// </summary>
        ScenePath = 3,

        /// <summary>
        ///     The fully qualified path is included as debug information. This is the path returned by
        ///     <see cref="ComponentExt.GetUniqueScenePath" />.
        /// </summary>
        UniqueScenePath = 4
    }
}