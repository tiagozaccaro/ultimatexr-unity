// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrMovementInput.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Represents movement input data used to control smooth locomotion.
    /// </summary>
    public struct UxrMovementInput
    {
        #region Constructors & Finalizer

        /// <summary>
        ///     Default Constructor
        /// </summary>
        /// <param name="input">The joystick input.</param>
        /// <param name="useAcceleration">Whether to use acceleration.</param>
        public UxrMovementInput(Vector2 input, bool useAcceleration = true) : this()
        {
            Input           = input;
            UseAcceleration = useAcceleration;
        }

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Default Movement Input.
        /// </summary>
        public static readonly UxrMovementInput Default = default;

        /// <summary>
        ///     Whether to use acceleration.
        /// </summary>
        public readonly bool UseAcceleration;

        /// <summary>
        ///     The movement input in two axes: x = left/right, y = forward/backward. Range is [-1, 1].
        /// </summary>
        public Vector2 Input;

        #endregion
    }
}