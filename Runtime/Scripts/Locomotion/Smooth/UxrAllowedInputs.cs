// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAllowedInputs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Defines which smooth locomotion inputs are allowed. Multiple values can be combined using flags.
    /// </summary>
    [Flags]
    public enum UxrAllowedInputs
    {
        /// <summary>
        ///     No inputs are enabled.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Enables movement input.
        /// </summary>
        Movement = 1 << 0,

        /// <summary>
        ///     Enables rotation input.
        /// </summary>
        Rotation = 1 << 1,

        /// <summary>
        ///     Enables zoom input (in and out).
        /// </summary>
        Zoom = 1 << 2,

        /// <summary>
        ///     Enables jumping.
        /// </summary>
        Jump = 1 << 3,

        /// <summary>
        ///     Enables crouching.
        /// </summary>
        Crouch = 1 << 4,

        /// <summary>
        ///     Enables standing (returning from crouch).
        /// </summary>
        Stand = 1 << 5,

        /// <summary>
        ///     Enables sprinting.
        /// </summary>
        Sprint = 1 << 6,

        /// <summary>
        ///     Enables all inputs.
        /// </summary>
        All = 0x7FFFFFFF
    }
}