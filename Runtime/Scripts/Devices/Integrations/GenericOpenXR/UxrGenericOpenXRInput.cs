// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGenericOpenXRInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Core;
using UnityEngine;
using UnityEngine.XR;

namespace UltimateXR.Devices.Integrations.GenericOpenXR
{
    /// <summary>
    ///     Input for any OpenXR device. Mainly used when other well-known OpenXR devices were not detected.
    /// </summary>
    public class UxrGenericOpenXRInput : UxrUnityXRControllerInput
    {
        #region Public Overrides UxrControllerInput

        /// <summary>
        ///     Gets the SDK dependency: OpenXR.
        /// </summary>
        public override string SDKDependency => UxrConstants.SdkOpenXR;

        /// <inheritdoc />
        public override UxrControllerSetupType SetupType => UxrControllerSetupType.Dual;

        /// <inheritdoc />
        public override bool IsHandednessSupported => true;

        /// <inheritdoc />
        public override bool MainJoystickIsTouchpad => false;

        /// <inheritdoc />
        public override bool HasControllerElements(UxrHandSide handSide, UxrControllerElements controllerElements)
        {
            InputDevice inputDevice = GetInputDevice(handSide);
            
            if (!inputDevice.isValid)
            {
                return false;
            }
            
            // Enumerate capabilities.

            bool HasBoolFeature(InputFeatureUsage<bool> usage)
            {
                return inputDevice.TryGetFeatureValue(usage, out _);
            }

            bool HasFloatFeature(InputFeatureUsage<float> usage)
            {
                return inputDevice.TryGetFeatureValue(usage, out _);
            }

            bool HasVector2Feature(InputFeatureUsage<Vector2> usage)
            {
                return inputDevice.TryGetFeatureValue(usage, out _);
            }

            uint validElements = 0;

            if (HasVector2Feature(CommonUsages.primary2DAxis))
            {
                validElements |= (uint)(UxrControllerElements.Joystick | UxrControllerElements.DPad);
            }

            if (HasVector2Feature(CommonUsages.secondary2DAxis))
            {
                validElements |= (uint)UxrControllerElements.Joystick2;
            }

            if (HasFloatFeature(CommonUsages.grip) || HasBoolFeature(CommonUsages.gripButton))
            {
                validElements |= (uint)UxrControllerElements.Grip;
            }

            if (HasFloatFeature(CommonUsages.trigger) || HasBoolFeature(CommonUsages.triggerButton))
            {
                validElements |= (uint)UxrControllerElements.Trigger;
            }

            if (HasBoolFeature(CommonUsages.primaryButton))
            {
                validElements |= (uint)UxrControllerElements.Button1;
            }

            if (HasBoolFeature(CommonUsages.secondaryButton))
            {
                validElements |= (uint)UxrControllerElements.Button2;
            }

            if (HasBoolFeature(CommonUsages.menuButton))
            {
                validElements |= (uint)UxrControllerElements.Menu;
            }

            if (handSide == UxrHandSide.Right)
            {
                // Remove menu button from right controller, which is usually reserved.
                validElements = validElements & ~(uint)UxrControllerElements.Menu;
            }

            return (validElements & (uint)controllerElements) == (uint)controllerElements;
        }

        #endregion

        #region Protected Overrides UxrUnityXRControllerInput

        /// <summary>
        ///     Instead of returning the controller names, we override <see cref="IsSupportedController" /> to return true for all
        ///     OpenXR devices."/>
        /// </summary>
        protected override IEnumerable<string> ControllerNames
        {
            get { yield break; }
        }

        /// <summary>
        ///     Determines whether the specified controller is supported. We return true for all OpenXR devices.
        /// </summary>
        /// <param name="deviceName">The name of the controller device to check.</param>
        /// <returns>True if the controller is supported; otherwise, false.</returns>
        protected override bool IsSupportedController(string deviceName)
        {
            return deviceName.Contains("OpenXR");
        }

        #endregion
    }
}