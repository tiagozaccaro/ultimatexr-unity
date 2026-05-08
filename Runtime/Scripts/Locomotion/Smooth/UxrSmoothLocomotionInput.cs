// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothLocomotionInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Base class used to handle smooth locomotion input in UltimateXR.
    /// </summary>
    /// <remarks>
    ///     Provides a common implementation for typical locomotion inputs such as movement, rotation, sprinting, and jumping.
    ///     Also for non-VR implementations inputs like crouching, standing, and zooming.
    ///     <para />
    ///     You can enable or disable specific inputs using <see cref="UxrAllowedInputs" />.
    ///     The class also sets up default input handling when needed, so derived classes only need to provide their own input
    ///     source.
    /// </remarks>
    public abstract class UxrSmoothLocomotionInput : IUxrSmoothLocomotionInput
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets or sets the allowed locomotion inputs for the component.
        /// </summary>
        /// <remarks>
        ///     By default, the property is set to <see cref="UxrAllowedInputs.All" />, allowing all input types.
        ///     Specific input-related methods in the class will only respond if the corresponding input type is enabled through
        ///     this property.
        /// </remarks>
        public UxrAllowedInputs AllowedInputs { get; set; } = UxrAllowedInputs.All;

        #endregion

        #region Implicit IUxrSmoothLocomotionInput

        /// <inheritdoc />
        public UxrMovementInput GetMovementInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Movement) ? GetMovementInputInternal() : UxrMovementInput.Default;
        }

        /// <inheritdoc />
        public Vector2 GetRotationInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Rotation) ? GetRotationInputInternal() : Vector2.zero;
        }

        /// <inheritdoc />
        public bool IsSprintInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Sprint) && IsSprintInputInternal();
        }

        /// <inheritdoc />
        public bool IsJumpInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Jump) && IsJumpInputInternal();
        }

        /// <inheritdoc />
        public float IsCrouchInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Crouch) ? IsCrouchInputInternal() : 0f;
        }

        /// <inheritdoc />
        public float IsStandInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Stand) ? IsStandInputInternal() : 0f;
        }

        /// <inheritdoc />
        public float GetZoomInInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Zoom) ? GetZoomInInputInternal() : 0f;
        }

        /// <inheritdoc />
        public float GetZoomOutInput()
        {
            return AllowedInputs.HasFlag(UxrAllowedInputs.Zoom) ? GetZoomOutInputInternal() : 0f;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Gets the movement input from the derived class implementation.
        /// </summary>
        /// <returns>The current movement input.</returns>
        protected abstract UxrMovementInput GetMovementInputInternal();

        /// <summary>
        ///     Gets the rotation input from the derived class implementation.
        /// </summary>
        /// <returns>The rotation input.</returns>
        protected abstract Vector2 GetRotationInputInternal();

        /// <summary>
        ///     Returns whether sprint input is active from the derived class implementation.
        /// </summary>
        /// <returns>True if sprinting.</returns>
        protected abstract bool IsSprintInputInternal();

        /// <summary>
        ///     Gets the crouch input value from the derived class implementation.
        /// </summary>
        /// <returns>The crouch input amount.</returns>
        protected abstract float IsCrouchInputInternal();

        /// <summary>
        ///     Gets the stand input value from the derived class implementation.
        /// </summary>
        /// <returns>The stand input amount.</returns>
        protected abstract float IsStandInputInternal();

        /// <summary>
        ///     Returns whether jump input is active from the derived class implementation.
        /// </summary>
        /// <returns>True if jumping.</returns>
        protected abstract bool IsJumpInputInternal();

        /// <summary>
        ///     Gets the zoom-in input value from the derived class implementation.
        /// </summary>
        /// <returns>The zoom-in amount.</returns>
        protected abstract float GetZoomInInputInternal();

        /// <summary>
        ///     Gets the zoom-out input value from the derived class implementation.
        /// </summary>
        /// <returns>The zoom-out amount.</returns>
        protected abstract float GetZoomOutInputInternal();

        #endregion
    }
}