// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTeleportLocomotion.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Locomotion.Teleport
{
    public partial class UxrTeleportLocomotion
    {
        #region Protected Overrides UxrTeleportLocomotionBase

        /// <inheritdoc />
        protected override void SerializeState(bool isReading, UxrStateSaveLevel level, UxrStateSaveOptions options)
        {
            base.SerializeState(isReading, level, options);

            // Version

            SerializeStateVersion(level, options, StateSerializationVersion, out int effectiveVersion);

            if (level <= UxrStateSaveLevel.ChangesSincePreviousSave)
            {
                // Process all save levels above time sampling. Time sampling is not needed and covered by event synchronization.
                return;
            }

            SerializeStateValue(level, options, nameof(_previousFrameHadArc), ref _previousFrameHadArc);
            SerializeStateValue(level, options, nameof(_arcCancelled),        ref _arcCancelled);
            SerializeStateValue(level, options, nameof(_arcCancelledByAngle), ref _arcCancelledByAngle);

            SerializeStateValue(level, options, nameof(_lastSyncIsArcEnabled),    ref _lastSyncIsArcEnabled);
            SerializeStateValue(level, options, nameof(_lastSyncIsTargetEnabled), ref _lastSyncIsTargetEnabled);
            SerializeStateValue(level, options, nameof(_lastSyncIsValidTeleport), ref _lastSyncIsValidTeleport);

            if (isReading)
            {
                EnableArc(_lastSyncIsArcEnabled, _lastSyncIsValidTeleport);
            }
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}