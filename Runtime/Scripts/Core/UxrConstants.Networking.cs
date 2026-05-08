// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrConstants.Networking.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Components;
using UltimateXR.Core.Settings;
using UltimateXR.Manipulation;

namespace UltimateXR.Core
{
    public static partial class UxrConstants
    {
        #region Public Types & Data

        /// <summary>
        ///     Contains constants for networking behavior.
        /// </summary>
        public static class Networking
        {
            #region Public Types & Data

            /// <summary>
            ///     Represents the default time interval, in seconds, at which grabbable object (<see cref="UxrGrabbableObject" />)
            ///     transforms are synchronized when using <see cref="UxrGlobalSettings.SyncGrabbablePhysics" />.
            /// </summary>
            public const float DefaultGrabbableSyncIntervalSeconds = 1.0f;

            /// <summary>
            ///     Represents the default time interval, in seconds, at which <see cref="UxrSyncObject" /> transforms that have
            ///     <see cref="UxrSyncObject.SyncTransformNetwork"/> enabled are synchronized via networkRPCs.
            /// </summary>
            public const float DefaultNetworkTransformSyncIntervalSeconds = 0.05f;

            #endregion
        }

        #endregion
    }
}