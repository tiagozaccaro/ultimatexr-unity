// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrWallClipEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Avatar;
using UltimateXR.Core.Events;
using UnityEngine;

namespace UltimateXR.CameraUtils
{
    /// <summary>
    ///     Event data describing the current wall clipping state for an avatar, including clipping intensity,
    ///     spatial context, and portal configuration used to guide the user back to a valid position.
    /// </summary>
    public class UxrWallFadeEventArgs : UxrPooledEventArgs<UxrWallFadeEventArgs>
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets or sets the avatar experiencing the clipping state.
        /// </summary>
        public UxrAvatar Avatar { get; set; }

        /// <summary>
        ///     Gets or sets the current fade intensity applied to the view, where 0 means no fade and 1 means fully faded.
        /// </summary>
        public float FadeAlpha { get; set; }

        /// <summary>
        ///     Gets or sets the last known valid camera position outside the geometry, used as a reference to determine
        ///     how the user entered the wall and where the exit portal should be anchored.
        /// </summary>
        public Vector3 LastValidCameraPos { get; set; }

        /// <summary>
        ///     Gets or sets the vector from the last valid camera position to the current camera position, representing
        ///     the displacement that caused the clipping and can be used to infer movement direction and penetration depth.
        /// </summary>
        public Vector3 LastValidToCurrentCameraPos { get; set; }

        /// <summary>
        ///     Gets or sets the exit direction pointing back toward the valid space, typically derived from the inverse
        ///     of the entry direction and used to orient the portal plane guiding the user out of the geometry.
        /// </summary>
        public Vector3 ExitNormal { get; set; }

        /// <summary>
        ///     Gets or sets the world-space center of the portal sphere used to compute the visible opening, projected
        ///     along the entry direction so that the portal remains anchored in space while varying with penetration depth.
        /// </summary>
        public Vector3 PortalSphereCenter { get; set; }

        #endregion
    }
}