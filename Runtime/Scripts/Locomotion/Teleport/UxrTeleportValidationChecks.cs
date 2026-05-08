// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTeleportValidationChecks.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Locomotion.Teleport
{
    /// <summary>
    ///     Enumerates the possible steps to perform a check whether a teleport component can currently teleport.
    /// </summary>
    [Flags]
    public enum UxrTeleportValidationChecks
    {
        None = 0,

        /// <summary>
        ///     Whether to check if the component is already currently teleporting the avatar.
        /// </summary>
        IsAlreadyTeleporting = 1 << 0,

        /// <summary>
        ///     Whether to check if the avatar is currently peeking through geometry.
        /// </summary>
        IsPeekingThroughGeometry = 1 << 1,

        /// <summary>
        ///     Whether to check if the hand the component is controlled with, is occluded. That is, if there is an object between
        ///     the camera and the hand.
        /// </summary>
        IsHandOccluded = 1 << 2,

        /// <summary>
        ///     Whether to check if there is something blocking at eye level between the source and destination.
        /// </summary>
        IsEyeLevelBlocked = 1 << 3,

        /// <summary>
        ///     Whether the destination height is above the height difference threshold.
        /// </summary>
        IsOverMaxAllowedHeightDifference = 1 << 4,

        /// <summary>
        ///     Whether to check if there is enough space at the destination to fit a sphere simulating the head.
        /// </summary>
        IsDestinationHeadSpaceFree = 1 << 5,

        /// <summary>
        ///     Whether to check if the slope of the destination is below the threshold.
        /// </summary>
        IsDestinationSteepnessValid = 1 << 6,

        /// <summary>
        ///     Performs a specific check for vertical walls. If not performed, it might give false positives when pointing near a
        ///     ball base.
        /// </summary>
        WallCheck = 1 << 7,

        /// <summary>
        ///     Whether to check if the destination has a <see cref="UxrTeleportSpawnCollider"/> component.
        /// </summary>
        DestinationHasSpawnCollider = 1 << 8,

        /// <summary>
        ///     Whether to check if the destination has a <see cref="UxrProhibitLocomotionDestination"/> component.
        /// </summary>
        DestinationHasProhibitedComponent = 1 << 9,

        /// <summary>
        ///     Performs all checks except the line of sight test which is mainly used for back stepping.
        /// </summary>
        Standard = All ^ IsEyeLevelBlocked,

        /// <summary>
        ///     Performs all checks.
        /// </summary>
        All = 0x7FFFFFFF
    }
}