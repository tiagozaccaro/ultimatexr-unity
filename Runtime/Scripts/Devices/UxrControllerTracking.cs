// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrControllerTracking.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Core;
using UltimateXR.Core.Settings;
using UltimateXR.Extensions.Unity.Math;
using UnityEngine;
using UnityEngine.XR;

namespace UltimateXR.Devices
{
    /// <summary>
    ///     Base class for standard tracking of left+right VR controllers.
    /// </summary>
    public abstract class UxrControllerTracking : UxrTrackingDevice, IUxrControllerTracking
    {
        #region Inspector Properties/Serialized Fields

        [Header("Device sensor tracking positions:")] [SerializeField] private Transform _leftHandSensor;
        [SerializeField]                                               private Transform _rightHandSensor;
        [SerializeField]                                               private Transform _leftHandOpenXRAim;
        [SerializeField]                                               private Transform _rightHandOpenXRAim;
        [SerializeField]                                               private Transform _leftHandOpenXRGrip;
        [SerializeField]                                               private Transform _rightHandOpenXRGrip;

        [Header("Update avatar using sensors:")] [SerializeField] private bool  _updateAvatarLeftHand  = true;
        [SerializeField]                                          private bool  _updateAvatarRightHand = true;
        [SerializeField] [Range(0, 1)]                            private float _smoothPosition;
        [SerializeField] [Range(0, 1)]                            private float _smoothRotation;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets if the avatar's left hand needs to be updated each time we get new sensor data for it
        /// </summary>
        public bool UpdateAvatarLeftHand => _updateAvatarLeftHand;

        /// <summary>
        ///     Gets if the avatar's right hand needs to be updated each time we get new sensor data for it
        /// </summary>
        public bool UpdateAvatarRightHand => _updateAvatarRightHand;

        #endregion

        #region Implicit IUxrControllerTracking

        /// <inheritdoc />
        public abstract Type RelatedControllerInputType { get; }

        /// <inheritdoc />
        public virtual bool HeadsetIs6Dof => true;

        /// <inheritdoc />
        public bool HasLeftHandSensorSetup => _leftHandSensor != null;

        /// <inheritdoc />
        public bool HasRightHandSensorSetup => _rightHandSensor != null;

        /// <inheritdoc />
        public Vector3 SensorLeftPos => Avatar.transform.TransformPoint(LocalAvatarLeftHandSensorPos);

        /// <inheritdoc />
        public Vector3 SensorRightPos => Avatar.transform.TransformPoint(LocalAvatarRightHandSensorPos);

        /// <inheritdoc />
        public Quaternion SensorLeftRot => Avatar.transform.rotation * LocalAvatarLeftHandSensorRot;

        /// <inheritdoc />
        public Quaternion SensorRightRot => Avatar.transform.rotation * LocalAvatarRightHandSensorRot;

        /// <inheritdoc />
        public Vector3 SensorLeftHandPos
        {
            get
            {
                Quaternion leftHandSensorRot = SensorLeftRot;

                if (!leftHandSensorRot.IsValid())
                {
                    return Vector3.zero;
                }

                Matrix4x4 mtxLeftHandSensor = Matrix4x4.TRS(SensorLeftPos, leftHandSensorRot.normalized, Vector3.one);
                return mtxLeftHandSensor.MultiplyPoint(_localSensorLeftHandPos);
            }
        }

        /// <inheritdoc />
        public Vector3 SensorRightHandPos
        {
            get
            {
                Quaternion rightHandSensorRot = SensorRightRot;

                if (!rightHandSensorRot.IsValid())
                {
                    return Vector3.zero;
                }

                Matrix4x4 mtxRightHandSensor = Matrix4x4.TRS(SensorRightPos, rightHandSensorRot.normalized, Vector3.one);
                return mtxRightHandSensor.MultiplyPoint(_localSensorRightHandPos);
            }
        }

        /// <inheritdoc />
        public Quaternion SensorLeftHandRot => SensorLeftRot * _localSensorLeftHandRot;

        /// <inheritdoc />
        public Quaternion SensorRightHandRot => SensorRightRot * _localSensorRightHandRot;

        #endregion  

        #region Unity

        /// <summary>
        ///     Stores if the component was enabled from the beginning to know later if it should be enabled when the device gets
        ///     connected.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (!SetupSensorInternal(_leftHandSensor, Avatar.LeftHandBone, out _localSensorLeftHandPos, out _localSensorLeftHandRot))
            {
                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.DevicesModule} {name}: Avatar Rig has no left wrist setup or left sensor was not specified in the tracking component. Avatar's left hand position may not be updated.");
                }
            }

            if (!SetupSensorInternal(_rightHandSensor, Avatar.RightHandBone, out _localSensorRightHandPos, out _localSensorRightHandRot))
            {
                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.DevicesModule} {name}: Avatar Rig has no right wrist setup or right sensor was not specified in the tracking component. Avatar's right hand position may not be updated.");
                }
            }

            // Start disabled and wait for input controllers to be connected. This way we don't have to check for controller presence in both input components and tracking components.
            // Controller connected events will be raised either:
            // a) In Start(), whenever a new scene is loaded and controllers are already enabled. The system will force a Connect event even if the controllers themselves don't send any.
            // b) At any point during execution
            enabled                                      =  false;
            UxrControllerInput.GlobalControllerConnected += UxrControllerInput_GlobalControllerConnected;
        }

        /// <summary>
        ///     Unsubscribes from events
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            UxrControllerInput.GlobalControllerConnected -= UxrControllerInput_GlobalControllerConnected;
        }

        /// <summary>
        ///     Starts the coroutine that tries to set up the camera
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (!_cameraInitialized)
            {
                StartCoroutine(RepeatSetupCameraCoroutine());
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Coroutine that tries to set up the camera
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator RepeatSetupCameraCoroutine()
        {
            while (!_cameraInitialized)
            {
                _cameraInitialized = SetupCamera();
                yield return null;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called whenever a controller input device is connected. We will check if it has a related tracking device type
        ///     associated, and if so enable or disable it accordingly.
        /// </summary>
        /// <param name="sender">Derived <see cref="UxrControllerInput" /> object that sent the event</param>
        /// <param name="e">Event args</param>
        private void UxrControllerInput_GlobalControllerConnected(object sender, UxrDeviceConnectEventArgs e)
        {
            if (RelatedControllerInputType != null && sender.GetType() == RelatedControllerInputType)
            {
                // Compatible device.

                ConnectedControllerInput = sender as UxrControllerInput;

                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Relevant)
                {
                    Debug.Log($"{UxrConstants.DevicesModule} Found compatible tracking component {GetType()}. Setting enabled to {e.IsConnected}");
                }

                enabled = e.IsConnected;
                OnDeviceConnected(e);
            }
        }

        #endregion

        #region Protected Overrides UxrTrackingDevice

        /// <inheritdoc />
        protected override void UpdateAvatar()
        {
            base.UpdateAvatar();

            Transform wristLeft  = Avatar.LeftHandBone;
            Transform wristRight = Avatar.RightHandBone;

            if (_updateAvatarLeftHand && wristLeft != null)
            {
                wristLeft.SetPositionAndRotation(SensorLeftHandPos, SensorLeftHandRot);
            }

            if (_updateAvatarRightHand && wristRight != null)
            {
                wristRight.SetPositionAndRotation(SensorRightHandPos, SensorRightHandRot);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     A helper method for <see cref="SetupSensorInternal" />.
        /// </summary>
        /// <param name="side">Which hand to set up the sensor for</param>
        /// <param name="sensorTransform">The sensor position</param>
        protected void SetupSensor(UxrHandSide side, Transform sensorTransform)
        {
            if (side == UxrHandSide.Left && !SetupSensorInternal(sensorTransform, Avatar.LeftHandBone, out _localSensorLeftHandPos, out _localSensorLeftHandRot))
            {
                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.DevicesModule} {name}: Avatar Rig has no left wrist setup or left sensor was not specified in the tracking component. Avatar's left hand position may not be updated.");
                }
            }

            if (side == UxrHandSide.Right && !SetupSensorInternal(sensorTransform, Avatar.RightHandBone, out _localSensorRightHandPos, out _localSensorRightHandRot))
            {
                if (UxrGlobalSettings.Instance.LogLevelDevices >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.DevicesModule} {name}: Avatar Rig has no right wrist setup or right sensor was not specified in the tracking component. Avatar's right hand position may not be updated.");
                }
            }
        }

        /// <summary>
        ///     Updates the sensor data of an XR controller, using smoothing if required.
        /// </summary>
        /// <param name="side">Which side the sensor belongs to</param>
        /// <param name="localPos">Controller position in local tracking space</param>
        /// <param name="localRot">Controller rotation in local tracking space</param>
        protected void UpdateSensor(UxrHandSide side, Vector3 localPos, Quaternion localRot)
        {
            if (side == UxrHandSide.Left)
            {
                LocalAvatarLeftHandSensorPos = UxrInterpolator.SmoothDampPosition(_lastLeftSensorLocalPos, localPos, _leftSensorInitialized ? _smoothPosition : 0.0f);
                LocalAvatarLeftHandSensorRot = UxrInterpolator.SmoothDampRotation(_lastLeftSensorLocalRot, localRot, _leftSensorInitialized ? _smoothRotation : 0.0f);

                _leftSensorInitialized  = true;
                _lastLeftSensorLocalPos = LocalAvatarLeftHandSensorPos;
                _lastLeftSensorLocalRot = LocalAvatarLeftHandSensorRot;
            }
            else if (side == UxrHandSide.Right)
            {
                LocalAvatarRightHandSensorPos = UxrInterpolator.SmoothDampPosition(_lastRightSensorLocalPos, localPos, _rightSensorInitialized ? _smoothPosition : 0.0f);
                LocalAvatarRightHandSensorRot = UxrInterpolator.SmoothDampRotation(_lastRightSensorLocalRot, localRot, _rightSensorInitialized ? _smoothRotation : 0.0f);

                _rightSensorInitialized  = true;
                _lastRightSensorLocalPos = LocalAvatarRightHandSensorPos;
                _lastRightSensorLocalRot = LocalAvatarRightHandSensorRot;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Tries to set up the camera
        /// </summary>
        /// <returns>Boolean telling whether the camera could be set up</returns>
        private bool SetupCamera()
        {
            List<XRInputSubsystem> inputSubsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(inputSubsystems);

            if (inputSubsystems.Count == 0)
            {
                return false;
            }

            bool initialized = true;

            foreach (XRInputSubsystem subSystem in inputSubsystems)
            {
                initialized &= SetupCamera(subSystem);
            }

            return initialized;
        }

        /// <summary>
        ///     Tries to set up the camera of a given <see cref="XRInputSubsystem" />
        ///     What it tries to do is set the camera tracking origin to floor
        /// </summary>
        /// <param name="subsystem">Input subsystem to try to set up</param>
        /// <returns>Boolean telling whether the camera was set up</returns>
        private bool SetupCamera(XRInputSubsystem subsystem)
        {
            if (subsystem == null)
            {
                return false;
            }

            TrackingOriginModeFlags supportedModes = subsystem.GetSupportedTrackingOriginModes();
            TrackingOriginModeFlags requestedMode  = TrackingOriginModeFlags.Floor;

            // We need to check for Unknown because we may not be in a state where we can read this data yet.
            if ((supportedModes & (TrackingOriginModeFlags.Floor | TrackingOriginModeFlags.Unknown)) == 0)
            {
                return false;
            }

            return subsystem.TrySetTrackingOriginMode(requestedMode);
        }

        /// <summary>
        ///     The goal of each left and right sensor is to position the visual hand in the correct position. This method
        ///     computes the initial bone position and rotation in local sensor coordinates to be able to reposition the
        ///     hand whenever the sensors get updated.
        /// </summary>
        /// <param name="sensorTransform">The given sensor's transform</param>
        /// <param name="boneTransform">The bone transform this sensor should position and orientate</param>
        /// <param name="localBonePos">Gets the bone position in local coordinates of the sensor transform</param>
        /// <param name="localBoneRot">Gets the bone rotation in local coordinates of the sensor transform</param>
        /// <returns></returns>
        private bool SetupSensorInternal(Transform sensorTransform, Transform boneTransform, out Vector3 localBonePos, out Quaternion localBoneRot)
        {
            localBonePos = Vector3.zero;
            localBoneRot = Quaternion.identity;

            if (sensorTransform == null || boneTransform == null)
            {
                return false;
            }

            localBonePos = sensorTransform.InverseTransformPoint(boneTransform.position);
            localBoneRot = Quaternion.Inverse(sensorTransform.rotation) * boneTransform.rotation;

            return true;
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Gets the transform of the left controller's sensor. This is the place in the controller prefab that is considered
        ///     when querying about a controller position/rotation. This is used by the native SDKs; when using OpenXR, it has been
        ///     replaced by the Grip and Aim positions/orientations.
        /// </summary>
        protected Transform LeftHandSensor => _leftHandSensor;

        /// <summary>
        ///     Gets the transform of the right controller's sensor. This is the place in the controller prefab that is considered
        ///     when querying about a controller position/rotation. This is used by the native SDKs; when using OpenXR, it has been
        ///     replaced by the Grip and Aim positions/orientations.
        /// </summary>
        protected Transform RightHandSensor => _rightHandSensor;

        /// <summary>
        ///     Gets the transform of the left controller's aim. This is the place in the controller prefab that tells where the
        ///     controller is pointing, used by the OpenXR implementation.
        /// </summary>
        protected Transform LeftHandOpenXRAim => _leftHandOpenXRAim;

        /// <summary>
        ///     Gets the transform of the right controller's aim. This is the place in the controller prefab that tells where the
        ///     controller is pointing, used by the OpenXR implementation.
        /// </summary>
        protected Transform RightHandOpenXRAim => _rightHandOpenXRAim;

        /// <summary>
        ///     Gets the transform of the left controller's grip. This is the place in the controller prefab that tells where the
        ///     user is holding the controller, used by the OpenXR implementation.
        /// </summary>
        protected Transform LeftHandOpenXRGrip => _leftHandOpenXRGrip;

        /// <summary>
        ///     Gets the transform of the right controller's grip. This is the place in the controller prefab that tells where the
        ///     user is holding the controller, used by the OpenXR implementation.
        /// </summary>
        protected Transform RightHandOpenXRGrip => _rightHandOpenXRGrip;

        /// <summary>
        ///     Gets the <see cref="UxrControllerInput" /> this tracking corresponds to. It's only available after the controller
        ///     gets connected.
        /// </summary>
        protected UxrControllerInput ConnectedControllerInput { get; private set; }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Gets the left-hand sensor position in local avatar coordinates
        /// </summary>
        private Vector3 LocalAvatarLeftHandSensorPos { get; set; }

        /// <summary>
        ///     Gets the left-hand sensor rotation in local avatar coordinates
        /// </summary>
        private Quaternion LocalAvatarLeftHandSensorRot { get; set; }

        /// <summary>
        ///     Gets the right-hand sensor position in local avatar coordinates
        /// </summary>
        private Vector3 LocalAvatarRightHandSensorPos { get; set; }

        /// <summary>
        ///     Gets the right-hand sensor rotation in local avatar coordinates
        /// </summary>
        private Quaternion LocalAvatarRightHandSensorRot { get; set; }

        private bool       _cameraInitialized;
        private Vector3    _localSensorLeftHandPos;
        private Vector3    _localSensorRightHandPos;
        private Quaternion _localSensorLeftHandRot;
        private Quaternion _localSensorRightHandRot;

        private bool       _leftSensorInitialized;
        private bool       _rightSensorInitialized;
        private Vector3    _lastLeftSensorLocalPos;
        private Vector3    _lastRightSensorLocalPos;
        private Quaternion _lastLeftSensorLocalRot;
        private Quaternion _lastRightSensorLocalRot;

        #endregion
    }
}