// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrLookAtAvatar.AvatarTarget.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Animation.Transforms
{
    public partial class UxrLookAtAvatar
    {
        #region Private Types & Data

        /// <summary>
        ///     Supported look-at targets.
        /// </summary>
        public enum AvatarTarget
        {
            /// <summary>
            ///     Will use the local avatar camera.
            /// </summary>
            LocalAvatar = 0,

            /// <summary>
            ///     Will use the camera of the first avatar found in the parent hierarchy.
            ///     If there is no avatar, it will use the local avatar camera.
            /// </summary>
            FirstParentAvatar
        }

        #endregion
    }
}