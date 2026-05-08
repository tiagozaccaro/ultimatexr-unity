// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUxrSmoothLocomotionInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Provides the input used by smooth locomotion systems.
    /// </summary>
    public interface IUxrSmoothLocomotionInput
    {
        #region Public Methods

        /// <summary>
        ///     Gets the current movement input (typically from a joystick or WASD).
        /// </summary>
        UxrMovementInput GetMovementInput();

        /// <summary>
        ///     Gets the rotation input, usually from a secondary stick or mouse.
        /// </summary>
        Vector2 GetRotationInput();

        /// <summary>
        ///     Returns whether the sprint input is currently active.
        /// </summary>
        bool IsSprintInput();

        /// <summary>
        ///     Returns whether the jump input was triggered.
        /// </summary>
        bool IsJumpInput();

        /// <summary>
        ///     Gets the crouch input value. 0 = no crouch, 1 = full crouch. Designed for non-VR mode.
        /// </summary>
        float IsCrouchInput();

        /// <summary>
        ///     Gets the stand input value to return to an upright position. 0 = no standing, 1 = full standing. Designed for
        ///     non-VR mode.
        /// </summary>
        float IsStandInput();

        /// <summary>
        ///     Gets the zoom-in input amount, designed for non-VR mode.
        /// </summary>
        float GetZoomInInput();

        /// <summary>
        ///     Gets the zoom-out input amount, designed for non-VR mode.
        /// </summary>
        float GetZoomOutInput();

        #endregion
    }
}