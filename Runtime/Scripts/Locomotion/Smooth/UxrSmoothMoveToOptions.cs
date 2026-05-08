// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothMoveToOptions.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Options for controlling smooth locomotion behavior when moving an avatar to a target position.
    /// </summary>
    [Flags]
    public enum UxrSmoothMoveToOptions
    {
        /// <summary>
        ///     This value applies no additional constraints or modifications and uses the system's standard movement rules.
        /// </summary>
        Default = 0,

        /// <summary>
        ///     Forces the operation.
        /// </summary>
        Force = 1
    }
}