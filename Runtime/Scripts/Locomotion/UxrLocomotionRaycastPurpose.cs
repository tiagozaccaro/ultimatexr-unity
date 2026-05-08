// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrLocomotionRaycastPurpose.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Defines the purpose of a raycast in the locomotion system, which can be used together with
    ///     <see cref="UxrLocomotionRaycastFilter" /> to ignore collisions against certain geometry.
    /// </summary>
    public enum UxrLocomotionRaycastPurpose
    {
        /// <summary>
        ///     The raycast serves a purpose different from the rest of the enumeration.
        ///     <see cref="UxrLocomotionRaycastFilter" /> components will be ignored.
        /// </summary>
        Other = 0,

        /// <summary>
        ///     The raycast is used to find a valid teleportation surface.
        ///     For example, <see cref="UxrTeleportLocomotion" /> traces an arc to locate the teleport destination.
        ///     Objects using <see cref="UxrLocomotionRaycastFilter" /> with
        ///     <see cref="UxrLocomotionRaycastFilter.BlockTargeting" /> disabled can be ignored by this cast.
        /// </summary>
        Targeting,

        /// <summary>
        ///     The raycast is used to verify whether the surrounding space at the target location is suitable for
        ///     teleportation.
        ///     Objects using <see cref="UxrLocomotionRaycastFilter" /> with
        ///     <see cref="UxrLocomotionRaycastFilter.BlockValidation" /> disabled can be ignored by this cast.
        /// </summary>
        Validation
    }
}