// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrTeleportLocomotionBase.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Locomotion.Teleport
{
    public abstract partial class UxrTeleportLocomotionBase
    {
        #region Protected Overrides UxrComponent

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
            
            int layerMaskValue = _layerMaskRaycast.value;

            SerializeStateValue(level, options, nameof(layerMaskValue),         ref layerMaskValue);
            SerializeStateValue(level, options, nameof(_teleportTargetEnabled), ref _teleportTargetEnabled);
            SerializeStateValue(level, options, nameof(_teleportTargetValid),   ref _teleportTargetValid);

            if (isReading)
            {
                _layerMaskRaycast.value = layerMaskValue;
                EnableTeleportObjects(_teleportTargetEnabled, _teleportTargetValid);
            }
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}