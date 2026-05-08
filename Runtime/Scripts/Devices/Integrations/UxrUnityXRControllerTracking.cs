// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUnityXRControllerTracking.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Core;
using UltimateXR.Core.Settings;
using UltimateXR.Manipulation;
using UnityEngine;
using UnityEngine.XR;

namespace UltimateXR.Devices.Integrations
{
    /// <summary>
    ///     Base class for tracking devices based on UnityXR. Supports native OpenXR controllers.
    /// </summary>
    public abstract class UxrUnityXRControllerTracking : UxrControllerTracking
    {
        #region Unity

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            _nodeStates = new List<XRNodeState>();
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            if (UxrGrabManager.HasInstance)
            {
                UxrGrabManager.Instance.ObjectGrabbed  += UxrGrabManager_ObjectGrabbed;
                UxrGrabManager.Instance.ObjectReleased += UxrGrabManager_ObjectReleasingOrPlaced;
                UxrGrabManager.Instance.ObjectPlaced   += UxrGrabManager_ObjectReleasingOrPlaced;
            }
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            if (UxrGrabManager.HasInstance)
            {
                UxrGrabManager.Instance.ObjectGrabbed  -= UxrGrabManager_ObjectGrabbed;
                UxrGrabManager.Instance.ObjectReleased -= UxrGrabManager_ObjectReleasingOrPlaced;
                UxrGrabManager.Instance.ObjectPlaced   -= UxrGrabManager_ObjectReleasingOrPlaced;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when an object is about to be grabbed. Checks whether the grabber is on the same
        ///     avatar, local and using an OpenXR device. If the grabbed point specifies alignment to the controller's axes, it
        ///     will switch the OpenXR tracking to Aim mode for improved precision.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void UxrGrabManager_ObjectGrabbed(object sender, UxrManipulationEventArgs e)
        {
            if (RequiresSwitchingToAimMode(e))
            {
                SetAimModeEnabled(e.Grabber.Side, true);
            }
        }

        /// <summary>
        ///     Called when an object is about to be released or placed. Checks whether to restore a tracking source changed in
        ///     <see cref="UxrGrabManager_ObjectGrabbed" />.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">Event data.</param>
        private void UxrGrabManager_ObjectReleasingOrPlaced(object sender, UxrManipulationEventArgs e)
        {
            if (RequiresSwitchingToAimMode(e))
            {
                SetAimModeEnabled(e.Grabber.Side, false);
            }
        }

        #endregion

        #region Event Trigger Methods

        /// <inheritdoc />
        protected override void OnDeviceConnected(UxrDeviceConnectEventArgs e)
        {
            base.OnDeviceConnected(e);

            ConnectedUnityControllerInput = ConnectedControllerInput as UxrUnityXRControllerInput;

            if (ConnectedUnityControllerInput != null && e.IsConnected)
            {
                // If an OpenXR controller is connected, we need to re-initialize the sensor positions using either the grip or aim positions.
                // We will use the grip position if available, because some controllers don't position the aim at the tip (Meta Touch), otherwise the aim position.

                if (ConnectedUnityControllerInput.LeftDevice.isValid)
                {
                    _leftIsOpenXR       = false;
                    _leftOpenXRUsesGrip = false;

                    if (ConnectedUnityControllerInput.LeftDevice.name.Contains(UxrConstants.InputControllers.OpenXR))
                    {
                        if (LeftHandOpenXRGrip && ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out _))
                        {
                            if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Relevant)
                            {
                                Debug.Log($"{UxrConstants.DevicesModule} {nameof(UxrUnityXRControllerTracking)}.{nameof(OnDeviceConnected)}: Initializing left OpenXR controller sensor using Grip pose.");
                            }

                            SetupSensor(UxrHandSide.Left, LeftHandOpenXRGrip);
                            _leftIsOpenXR       = true;
                            _leftOpenXRUsesGrip = true;
                        }
                        else if (LeftHandOpenXRAim && ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(s_pointerPosition, out _))
                        {
                            if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Relevant)
                            {
                                Debug.Log($"{UxrConstants.DevicesModule} {nameof(UxrUnityXRControllerTracking)}.{nameof(OnDeviceConnected)}: Initializing left OpenXR controller sensor using Aim pose.");
                            }

                            SetupSensor(UxrHandSide.Left, LeftHandOpenXRAim);
                            _leftIsOpenXR = true;
                        }
                    }
                }

                if (ConnectedUnityControllerInput.RightDevice.isValid)
                {
                    _rightIsOpenXR       = false;
                    _rightOpenXRUsesGrip = false;

                    if (ConnectedUnityControllerInput.RightDevice.name.Contains(UxrConstants.InputControllers.OpenXR))
                    {
                        if (RightHandOpenXRGrip && ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out _))
                        {
                            if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Relevant)
                            {
                                Debug.Log($"{UxrConstants.DevicesModule} {nameof(UxrUnityXRControllerTracking)}.{nameof(OnDeviceConnected)}: Initializing right OpenXR controller sensor using Grip pose.");
                            }

                            SetupSensor(UxrHandSide.Right, RightHandOpenXRGrip);
                            _rightIsOpenXR       = true;
                            _rightOpenXRUsesGrip = true;
                        }
                        else if (RightHandOpenXRAim && ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(s_pointerPosition, out _))
                        {
                            if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Relevant)
                            {
                                Debug.Log($"{UxrConstants.DevicesModule} {nameof(UxrUnityXRControllerTracking)}.{nameof(OnDeviceConnected)}: Initializing right OpenXR controller sensor using Aim pose.");
                            }

                            SetupSensor(UxrHandSide.Right, RightHandOpenXRAim);
                            _rightIsOpenXR = true;
                        }
                    }
                }
            }
        }

        #endregion

        #region Protected Overrides UxrTrackingDevice

        /// <inheritdoc />
        protected override void UpdateSensors()
        {
            base.UpdateSensors();

            if (Avatar.CameraComponent == null)
            {
                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.DevicesModule}: No camera has been setup for this avatar");
                }

                return;
            }

            // OpenXR tracking path

            if (ConnectedUnityControllerInput.LeftDevice.isValid && _leftIsOpenXR)
            {
                if (_leftOpenXRUsesGrip)
                {
                    ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localAvatarLeftGripPos);
                    ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localAvatarLeftGripRot);
                    UpdateSensor(UxrHandSide.Left, localAvatarLeftGripPos, localAvatarLeftGripRot);
                }
                else
                {
                    ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(s_pointerPosition, out Vector3 localAvatarLeftAimPos);
                    ConnectedUnityControllerInput.LeftDevice.TryGetFeatureValue(s_pointerRotation, out Quaternion localAvatarLeftAimRot);
                    UpdateSensor(UxrHandSide.Left, localAvatarLeftAimPos, localAvatarLeftAimRot);
                }
            }

            if (ConnectedUnityControllerInput.RightDevice.isValid && _rightIsOpenXR)
            {
                if (_rightOpenXRUsesGrip)
                {
                    ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(CommonUsages.devicePosition, out Vector3 localAvatarRightGripPos);
                    ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(CommonUsages.deviceRotation, out Quaternion localAvatarRightGripRot);
                    UpdateSensor(UxrHandSide.Right, localAvatarRightGripPos, localAvatarRightGripRot);
                }
                else
                {
                    ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(s_pointerPosition, out Vector3 localAvatarRightAimPos);
                    ConnectedUnityControllerInput.RightDevice.TryGetFeatureValue(s_pointerRotation, out Quaternion localAvatarRightAimRot);
                    UpdateSensor(UxrHandSide.Right, localAvatarRightAimPos, localAvatarRightAimRot);
                }
            }

            // UnityXR tracking path

            if (!_leftIsOpenXR || !_rightIsOpenXR)
            {
                InputTracking.GetNodeStates(_nodeStates);

                foreach (XRNodeState nodeState in _nodeStates)
                {
                    if (nodeState.nodeType == XRNode.LeftHand && !_leftIsOpenXR)
                    {
                        nodeState.TryGetRotation(out Quaternion localAvatarLeftHandSensorRot);
                        nodeState.TryGetPosition(out Vector3 localAvatarLeftHandSensorPos);

                        UpdateSensor(UxrHandSide.Left, localAvatarLeftHandSensorPos, localAvatarLeftHandSensorRot);
                    }

                    if (nodeState.nodeType == XRNode.RightHand && !_rightIsOpenXR)
                    {
                        nodeState.TryGetRotation(out Quaternion localAvatarRightHandSensorRot);
                        nodeState.TryGetPosition(out Vector3 localAvatarRightHandSensorPos);

                        UpdateSensor(UxrHandSide.Right, localAvatarRightHandSensorPos, localAvatarRightHandSensorRot);
                    }
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Determines whether switching to aim mode is required based on the specified manipulation event arguments.
        /// </summary>
        /// <param name="e">The event arguments containing information about the manipulation event.</param>
        private bool RequiresSwitchingToAimMode(UxrManipulationEventArgs e)
        {
            if (e.Grabber == null || e.Grabber.Avatar != Avatar || e.GrabbableObject == null)
            {
                return false;
            }

            if ((e.Grabber.Side == UxrHandSide.Left && !_leftIsOpenXR) || (e.Grabber.Side == UxrHandSide.Right && !_rightIsOpenXR))
            {
                return false;
            }

            return e.GrabbableObject.GetGrabPoint(e.GrabPointIndex).AlignToController;
        }

        /// <summary>
        ///     Enables or disables the aim mode for the specified side.
        /// </summary>
        /// <param name="grabberSide">The side of the hand for which to set the aim mode.</param>
        /// <param name="isAimEnabled">
        ///     Indicates whether the aim mode should be enabled or disabled. When disabled, it will switch to
        ///     grip mode.
        /// </param>
        private void SetAimModeEnabled(UxrHandSide grabberSide, bool isAimEnabled)
        {
            if (grabberSide == UxrHandSide.Left)
            {
                SetupSensor(UxrHandSide.Left, isAimEnabled ? LeftHandOpenXRAim : LeftHandOpenXRGrip);
                _leftOpenXRUsesGrip = !isAimEnabled;
            }
            else if (grabberSide == UxrHandSide.Right)
            {
                SetupSensor(UxrHandSide.Right, isAimEnabled ? RightHandOpenXRAim : RightHandOpenXRGrip);
                _rightOpenXRUsesGrip = !isAimEnabled;
            }
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Gets the <see cref="UxrUnityXRControllerInput" /> this tracking corresponds to. It's only available after the
        ///     controller gets connected.
        /// </summary>
        protected UxrUnityXRControllerInput ConnectedUnityControllerInput { get; private set; }

        #endregion

        #region Private Types & Data

        private static readonly InputFeatureUsage<Vector3>    s_pointerPosition = new InputFeatureUsage<Vector3>("PointerPosition");
        private static readonly InputFeatureUsage<Quaternion> s_pointerRotation = new InputFeatureUsage<Quaternion>("PointerRotation");

        private List<XRNodeState> _nodeStates;

        private bool _leftIsOpenXR;
        private bool _leftOpenXRUsesGrip;
        private bool _rightIsOpenXR;
        private bool _rightOpenXRUsesGrip;

        #endregion
    }
}