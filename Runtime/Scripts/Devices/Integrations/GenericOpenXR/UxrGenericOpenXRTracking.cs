// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGenericOpenXRTracking.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core;

namespace UltimateXR.Devices.Integrations.GenericOpenXR
{
    /// <summary>
    ///     Tracking for any OpenXR-compatible device. Mainly used when other well-known OpenXR devices were not detected.
    /// </summary>
    public class UxrGenericOpenXRTracking : UxrUnityXRControllerTracking
    {
        #region Public Overrides UxrControllerTracking

        /// <inheritdoc />
        public override Type RelatedControllerInputType => typeof(UxrGenericOpenXRInput);

        #endregion

        #region Public Overrides UxrTrackingDevice

        /// <inheritdoc />
        public override string SDKDependency => UxrConstants.SdkOpenXR;

        #endregion
    }
}