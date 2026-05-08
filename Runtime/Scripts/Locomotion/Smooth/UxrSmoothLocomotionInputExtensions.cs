// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothLocomotionInputExtensions.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Helper methods to read smooth locomotion input from a list of input sources.
    /// </summary>
    public static class UxrSmoothLocomotionInputExtensions
    {
        #region Public Methods

        /// <summary>
        ///     Gets the first movement input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first movement input found, or the default movement input if none is found</returns>
        public static UxrMovementInput GetMovementInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                UxrMovementInput movementInput = input.GetMovementInput();
                if (movementInput.Input != Vector2.zero)
                {
                    return movementInput;
                }
            }
            return UxrMovementInput.Default;
        }

        /// <summary>
        ///     Gets the first rotation input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first rotation input found, or zero if none is found</returns>
        public static Vector2 GetRotationInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                Vector2 rotationInput = input.GetRotationInput();
                if (rotationInput != Vector2.zero)
                {
                    return rotationInput;
                }
            }
            return Vector2.zero;
        }

        /// <summary>
        ///     Checks whether any input source is pressing sprint.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>True if sprint is pressed by any input source</returns>
        public static bool IsSprintInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                if (input.IsSprintInput())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Checks whether any input source is pressing jump.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>True if jump is pressed by any input source</returns>
        public static bool IsJumpInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                if (input.IsJumpInput())
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        ///     Gets the first crouch input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first crouch input found, or zero if none is found</returns>
        public static float GetCrouchInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                float crouchInput = input.IsCrouchInput();
                if (crouchInput != 0f)
                {
                    return crouchInput;
                }
            }
            return 0f;
        }

        /// <summary>
        ///     Gets the first stand input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first stand input found, or zero if none is found</returns>
        public static float GetStandInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                float standInput = input.IsStandInput();
                if (standInput != 0f)
                {
                    return standInput;
                }
            }
            return 0f;
        }

        /// <summary>
        ///     Gets the first zoom-in input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first zoom-in input found, or zero if none is found</returns>
        public static float GetZoomInInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                float zoomInInput = input.GetZoomInInput();
                if (zoomInInput != 0f)
                {
                    return zoomInInput;
                }
            }
            return 0f;
        }

        /// <summary>
        ///     Gets the first zoom-out input that is not zero.
        /// </summary>
        /// <param name="inputs">The input sources to check</param>
        /// <returns>The first zoom-out input found, or zero if none is found</returns>
        public static float GetZoomOutInput(IReadOnlyList<IUxrSmoothLocomotionInput> inputs)
        {
            foreach (IUxrSmoothLocomotionInput input in inputs)
            {
                float zoomOutInput = input.GetZoomOutInput();
                if (zoomOutInput != 0f)
                {
                    return zoomOutInput;
                }
            }
            return 0f;
        }

        #endregion
    }
}