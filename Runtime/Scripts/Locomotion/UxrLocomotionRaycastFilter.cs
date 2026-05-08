// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrLocomotionRaycastFilter.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Components;
using UnityEngine;

namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Component that controls whether an object blocks locomotion raycasts, which they do by default.
    ///     This helps ensure that only relevant objects affect locomotion and movement within an area.
    ///     Use it on objects that require a different behavior than the default, equivalent to
    ///     <see cref="BlockTargeting" /> and <see cref="BlockValidation" /> both <c>true</c>.
    /// </summary>
    /// <remarks>
    ///     To prevent moving onto a specific object or surface, use <see cref="UxrProhibitLocomotionDestination" /> instead.
    /// </remarks>
    public class UxrLocomotionRaycastFilter : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField]
        [Tooltip("Determines whether this object or any children block teleportation rays or arcs when aiming at a destination. " +
                 "Enable to prevent teleporting through this object. Disable to allow rays to pass through the colliders.")]
        private bool _blockTargeting = true;

        [SerializeField]
        [Tooltip("Determines whether this object or any children affect teleportation destination validation. " +
                 "Enable to include it in the area suitability check. Disable to allow movement through the colliders.")]
        private bool _blockValidation = true;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets or sets whether this object or any children block raycasts aimed at the teleportation destination, such as
        ///     those used in a teleportation ray or arc.
        ///     If <c>true</c>, the locomotion system will consider this object an obstacle, preventing teleportation to locations
        ///     behind it.
        ///     Set to <c>false</c> for objects that require colliders but should not obstruct teleportation rays or arcs.
        /// </summary>
        public bool BlockTargeting
        {
            get => _blockTargeting;
            set => _blockTargeting = value;
        }

        /// <summary>
        ///     Gets or sets whether this object or any children block destination validation raycasts.
        ///     These raycasts assess whether the surrounding area at the destination is spacious and suitable for movement.
        ///     If <c>true</c>, this object will be considered in the validation check and may prevent movement if it obstructs the
        ///     area. Set to <c>false</c> for objects that require colliders but should not block movement validation.
        /// </summary>
        public bool BlockValidation
        {
            get => _blockValidation;
            set => _blockValidation = value;
        }

        #endregion
    }
}