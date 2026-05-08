// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSyncObject.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Core.Components
{
    public partial class UxrSyncObject
    {
        #region Protected Overrides UxrComponent

        /// <inheritdoc />
        protected override UxrTransformSpace TransformStateSaveSpace => _transformSpace;

        /// <inheritdoc />
        protected override bool PreferForTracking => true;

        /// <inheritdoc />
        protected override bool SaveStateWhenDisabled => _syncWhileDisabled;

        /// <inheritdoc />
        protected override bool SerializeActiveAndEnabledState => _syncActiveAndEnabled;

        /// <inheritdoc />
        protected override bool RequiresTransformSerialization(UxrStateSaveLevel level)
        {
            // Save always
            return level >= UxrStateSaveLevel.ChangesSincePreviousSave && SyncTransform;
        }

        #endregion
    }
}