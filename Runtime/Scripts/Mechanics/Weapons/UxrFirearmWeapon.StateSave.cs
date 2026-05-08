// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrFirearmWeapon.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Mechanics.Weapons
{
    public partial class UxrFirearmWeapon
    {
        #region Protected Overrides UxrWeapon

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

            SerializeStateValue(level, options, nameof(_runtimeTriggers), ref _runtimeTriggers);
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}