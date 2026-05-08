// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrController3DModel.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.StateSave;

namespace UltimateXR.Devices.Visualization
{
    public partial class UxrController3DModel
    {
        #region Protected Overrides

        /// <inheritdoc />
        protected override void SerializeState(bool isReading, UxrStateSaveLevel level, UxrStateSaveOptions options)
        {
            base.SerializeState(isReading, level, options);

            // Version

            SerializeStateVersion(level, options, StateSerializationVersion, out int effectiveVersion);

            // Don't save incremental changes since contacts will already be saved through events.

            if (level > UxrStateSaveLevel.ChangesSincePreviousSave)
            {
                SerializeStateValue(level, options, nameof(_fingerContacts),      ref _fingerContacts);
                SerializeStateValue(level, options, nameof(_fingerContactsLeft),  ref _fingerContactsLeft);
                SerializeStateValue(level, options, nameof(_fingerContactsRight), ref _fingerContactsRight);
                SerializeStateValue(level, options, nameof(_controllerHand),      ref _controllerHand);
                SerializeStateValue(level, options, nameof(_controllerHandLeft),  ref _controllerHandLeft);
                SerializeStateValue(level, options, nameof(_controllerHandRight), ref _controllerHandRight);
                SerializeStateValue(level, options, nameof(_isControllerVisible), ref _isControllerVisible);
                SerializeStateValue(level, options, nameof(_isHandVisible),       ref _isHandVisible);

                if (isReading)
                {
                    // Use the setters to fully update the state
                    
                    ControllerHand      = _controllerHand;
                    ControllerHandLeft  = _controllerHandLeft;
                    ControllerHandRight = _controllerHandRight;
                    IsControllerVisible = _isControllerVisible;
                    IsHandVisible       = _isHandVisible;
                }
            }
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}