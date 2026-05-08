// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAutoSlideInObject.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Manipulation.Helpers
{
    public partial class UxrAutoSlideInObject
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

            SerializeStateValue(level, options, nameof(_insertAxis),                 ref _insertAxis);
            SerializeStateValue(level, options, nameof(_insertOffset),               ref _insertOffset);
            SerializeStateValue(level, options, nameof(_insertOffsetSign),           ref _insertOffsetSign);
            SerializeStateValue(level, options, nameof(_objectLocalSize),            ref _objectLocalSize);
            SerializeStateValue(level, options, nameof(_slideInTimer),               ref _slideInTimer);
            SerializeStateValue(level, options, nameof(_placedAfterSlidingIn),       ref _placedAfterSlidingIn);
            SerializeStateValue(level, options, nameof(_manipulationHapticFeedback), ref _manipulationHapticFeedback);
            SerializeStateValue(level, options, nameof(_minHapticAmplitude),         ref _minHapticAmplitude);
            SerializeStateValue(level, options, nameof(_maxHapticAmplitude),         ref _maxHapticAmplitude);
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}