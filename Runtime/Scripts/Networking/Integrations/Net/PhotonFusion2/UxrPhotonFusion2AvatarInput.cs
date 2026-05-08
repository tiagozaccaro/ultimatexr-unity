// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonFusion2AvatarInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
using UnityEngine;
using Fusion;

namespace UltimateXR.Networking.Integrations.Net.PhotonFusion2
{
    /// <summary>
    ///     Fusion input data used to synchronize the local avatar tracking pose.
    /// </summary>
    public struct UxrPhotonFusion2AvatarInput : INetworkInput
    {
        #region Public Types & Data

        public bool IsSmooth;
        
        public Vector3    AvatarPosition;
        public Quaternion AvatarRotation;

        public Vector3    LocalAvatarCameraPosition;
        public Quaternion LocalAvatarCameraRotation;

        public Vector3    LocalAvatarLeftHandPosition;
        public Quaternion LocalAvatarLeftHandRotation;

        public Vector3    LocalAvatarRightHandPosition;
        public Quaternion LocalAvatarRightHandRotation;

        #endregion
    }
}
#endif