// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGrabbableResizable.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Manipulation.Helpers
{
    public sealed partial class UxrGrabbableResizable
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

            SerializeStateValue(level, options, nameof(_grabbingCount), ref _grabbingCount);
            SerializeStateValue(level, options, nameof(_grabbedCount),  ref _grabbedCount);
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}