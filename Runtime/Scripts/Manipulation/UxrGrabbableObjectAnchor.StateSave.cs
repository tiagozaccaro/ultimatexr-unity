// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGrabbableObjectAnchor.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Manipulation
{
    public partial class UxrGrabbableObjectAnchor
    {
        #region Protected Overrides UxrComponent

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

            SerializeStateValue(level, options, nameof(_currentPlacedObject), ref _currentPlacedObject);
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}