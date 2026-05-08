// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrStandardSmoothLocomotionInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Devices;
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Smooth locomotion input implementation using VR controllers.
    /// </summary>
    public class UxrStandardSmoothLocomotionInput : UxrSmoothLocomotionInput
    {
        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="avatar">The avatar this input is associated with.</param>
        public UxrStandardSmoothLocomotionInput(UxrAvatar avatar)
        {
            _avatar = avatar;
        }

        #endregion

        #region Protected Overrides UxrSmoothLocomotionInput

        /// <inheritdoc />
        protected override UxrMovementInput GetMovementInputInternal()
        {
            return new UxrMovementInput(_avatar.ControllerInput.GetInput2D(UxrHandSide.Left, UxrInput2D.Joystick));
        }

        /// <inheritdoc />
        protected override Vector2 GetRotationInputInternal()
        {
            return _avatar.ControllerInput.GetInput2D(UxrHandSide.Right, UxrInput2D.Joystick);
        }

        /// <inheritdoc />
        protected override bool IsSprintInputInternal()
        {
            return _avatar.ControllerInput.GetButtonsPressDown(UxrHandSide.Left, UxrInputButtons.Joystick);
        }

        /// <inheritdoc />
        protected override float IsCrouchInputInternal()
        {
            // Handled in VR by the headset itself.
            return 0f;
        }

        /// <inheritdoc />
        protected override float IsStandInputInternal()
        {
            // Handled in VR by the headset itself.
            return 0f;
        }

        /// <inheritdoc />
        protected override bool IsJumpInputInternal()
        {
            // TODO: Implement jumping
            return false;
        }

        /// <inheritdoc />
        protected override float GetZoomInInputInternal()
        {
            // Not supported in VR.
            return 0f;
        }

        /// <inheritdoc />
        protected override float GetZoomOutInputInternal()
        {
            // Not supported in VR.
            return 0f;
        }

        #endregion

        #region Private Types & Data

        private readonly UxrAvatar _avatar;

        #endregion
    }
}