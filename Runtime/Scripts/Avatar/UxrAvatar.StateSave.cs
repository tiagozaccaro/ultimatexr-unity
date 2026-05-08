// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatar.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Linq;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Core;
using UltimateXR.Core.StateSave;
using UltimateXR.Devices;

namespace UltimateXR.Avatar
{
    public partial class UxrAvatar
    {
        #region Public Types & Data

        /// <summary>
        ///     Default smooth damp value for avatar position interpolation used for state interpolation (replays).
        /// </summary>
        public const float DefaultSmoothPosInterpolation = 0.15f;

        /// <summary>
        ///     Default smooth damp value for avatar rotation interpolation used for state interpolation (replays).
        /// </summary>
        public const float DefaultSmoothRotInterpolation = 0.15f;

        /// <summary>
        ///     Gets the camera position interpolator used for state interpolation (replays).
        /// </summary>
        public UxrVector3Interpolator CamPosInterpolator { get; } = new UxrVector3Interpolator(DefaultSmoothPosInterpolation);

        /// <summary>
        ///     Gets the camera rotation interpolator used for state interpolation (replays).
        /// </summary>
        public UxrQuaternionInterpolator CamRotInterpolator { get; } = new UxrQuaternionInterpolator(DefaultSmoothRotInterpolation);

        /// <summary>
        ///     Gets the left hand's position interpolator for state interpolation (replays).
        /// </summary>
        public UxrVector3Interpolator LeftHandPosInterpolator { get; } = new UxrVector3Interpolator(DefaultSmoothPosInterpolation);

        /// <summary>
        ///     Gets the left hand's rotation interpolator for state interpolation (replays).
        /// </summary>
        public UxrQuaternionInterpolator LeftHandRotInterpolator { get; } = new UxrQuaternionInterpolator(DefaultSmoothRotInterpolation);

        /// <summary>
        ///     Gets the right hand's position interpolator for state interpolation (replays).
        /// </summary>
        public UxrVector3Interpolator RightHandPosInterpolator { get; } = new UxrVector3Interpolator(DefaultSmoothPosInterpolation);

        /// <summary>
        ///     Gets the right hand's rotation interpolator for state interpolation (replays).
        /// </summary>
        public UxrQuaternionInterpolator RightHandRotInterpolator { get; } = new UxrQuaternionInterpolator(DefaultSmoothRotInterpolation);

        #endregion

        #region Protected Overrides UxrComponent

        /// <inheritdoc />
        protected override bool PreferForTracking => true;

        /// <inheritdoc />
        protected override UxrTransformSpace TransformStateSaveSpace => GetLocalTransformIfParentedOr(UxrTransformSpace.World);

        /// <inheritdoc />
        protected override bool RequiresTransformSerialization(UxrStateSaveLevel level)
        {
            // Save always
            return level >= UxrStateSaveLevel.ChangesSincePreviousSave;
        }

        /// <inheritdoc />
        protected override UxrVarInterpolator GetInterpolator(string varName)
        {
            if (IsTransformPositionVarName(varName, CamTransformName))
            {
                return CamPosInterpolator;
            }
            if (IsTransformRotationVarName(varName, CamTransformName))
            {
                return CamRotInterpolator;
            }
            if (IsTransformPositionVarName(varName, LeftHandTransformName))
            {
                return LeftHandPosInterpolator;
            }
            if (IsTransformRotationVarName(varName, LeftHandTransformName))
            {
                return LeftHandRotInterpolator;
            }
            if (IsTransformPositionVarName(varName, RightHandTransformName))
            {
                return RightHandPosInterpolator;
            }
            if (IsTransformRotationVarName(varName, RightHandTransformName))
            {
                return RightHandRotInterpolator;
            }

            // Null means using the default interpolator for the type
            return null;
        }

        /// <inheritdoc />
        protected override void InterpolateState(in UxrStateInterpolationVars vars, float t)
        {
            base.InterpolateState(in vars, t);

            InterpolateStateTransform(vars, t, CamTransformName,       CameraComponent.transform, UxrTransformSpace.Avatar);
            InterpolateStateTransform(vars, t, LeftHandTransformName,  LeftHandBone,              UxrTransformSpace.Avatar);
            InterpolateStateTransform(vars, t, RightHandTransformName, RightHandBone,             UxrTransformSpace.Avatar);
        }

        /// <inheritdoc />
        protected override void SerializeState(bool isReading, UxrStateSaveLevel level, UxrStateSaveOptions options)
        {
            base.SerializeState(isReading, level, options);

            // Version

            SerializeStateVersion(level, options, StateSerializationVersion, out int effectiveVersion);

            // TODO: Figure out how to avoid cheating by saving UxrCameraWallFade state too.

            SerializeStateTransform(level, options, CamTransformName,       UxrTransformSpace.Avatar, CameraComponent.transform);
            SerializeStateTransform(level, options, LeftHandTransformName,  UxrTransformSpace.Avatar, LeftHandBone);
            SerializeStateTransform(level, options, RightHandTransformName, UxrTransformSpace.Avatar, RightHandBone);

            // Controller and hand poses are already handled through events, we don't serialize them in incremental changes

            if (level > UxrStateSaveLevel.ChangesSincePreviousSave)
            {
                // Avatar render mode

                SerializeStateValue(level, options, nameof(_renderMode), ref _renderMode);

                // We serialize the controller input 

                SerializeStateValue(level, options, null, ref _externalControllerInput);

                // Hand poses

                string leftPoseName  = GetCurrentRuntimeHandPose(UxrHandSide.Left)?.PoseName;
                string rightPoseName = GetCurrentRuntimeHandPose(UxrHandSide.Right)?.PoseName;

                float leftBlendValue  = GetCurrentHandPoseBlendValue(UxrHandSide.Left);
                float rightBlendValue = GetCurrentHandPoseBlendValue(UxrHandSide.Right);

                SerializeStateValue(level, options, "leftPose",   ref leftPoseName);
                SerializeStateValue(level, options, "rightPose",  ref rightPoseName);
                SerializeStateValue(level, options, "leftBlend",  ref leftBlendValue);
                SerializeStateValue(level, options, "rightBlend", ref rightBlendValue);

                if (isReading)
                {
                    // Render mode

                    SetAvatarRenderMode(_renderMode, UxrControllerInput.GetComponents(this).ToList());

                    // When deserializing, we need to manually set the hand pose state from the serialized data.

                    if (leftPoseName != null)
                    {
                        SetCurrentHandPose(UxrHandSide.Left, leftPoseName, leftBlendValue);
                    }

                    if (rightPoseName != null)
                    {
                        SetCurrentHandPose(UxrHandSide.Right, rightPoseName, rightBlendValue);
                    }
                }
            }
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        private const string CamTransformName       = "cam.tf";
        private const string LeftHandTransformName  = "left.tf";
        private const string RightHandTransformName = "right.tf";

        #endregion
    }
}