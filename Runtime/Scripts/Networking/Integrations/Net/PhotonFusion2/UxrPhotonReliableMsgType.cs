// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonReliableMsgType.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Networking.Integrations.Net.PhotonFusion2
{
    /// <summary>
    ///     Custom Message types using Photon reliable data streaming.
    /// </summary>
    public enum UxrPhotonReliableMsgType
    {
        LoadGlobalState = 1,
        LoadAvatarState = 2
    }
}