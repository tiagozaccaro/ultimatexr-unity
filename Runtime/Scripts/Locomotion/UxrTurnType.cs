// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTurnType.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Enumerates the supported types of rotation around the avatar's axis.
    /// </summary>
    public enum UxrTurnType
    {
        /// <summary>
        ///     Turning is disabled.
        /// </summary>
        NotAllowed,

        /// <summary>
        ///     Immediate turn.
        /// </summary>
        Snap,

        /// <summary>
        ///     Quick fade-out followed by the turn and fade-in.
        /// </summary>
        Fade,

        /// <summary>
        ///     Interpolated turn.
        /// </summary>
        Interpolate,

        /// <summary>
        ///     Smooth, continuous turning.
        /// </summary>
        Smooth
    }
}