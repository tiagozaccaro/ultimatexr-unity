// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrStandardSmoothLocomotion.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Attributes;
using UnityEngine;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Standard VR smooth locomotion implementation.
    /// </summary>
    public class UxrStandardSmoothLocomotion : UxrSmoothLocomotion
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [StylishFoldout("Direction")] private DirectionParameters _directionParameters;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets or sets the movement reference used to determine the locomotion direction.
        /// </summary>
        public UxrMovemementRelativeTo MovementRelativeTo
        {
            get => _directionParameters._relativeTo;
            set => _directionParameters._relativeTo = value;
        }

        #endregion

        #region Protected Overrides UxrSmoothLocomotion

        /// <inheritdoc />
        protected override IEnumerable<IUxrSmoothLocomotionInput> GetDefaultInputs()
        {
            _standardSmoothLocomotionInput ??= new UxrStandardSmoothLocomotionInput(Avatar);
            yield return _standardSmoothLocomotionInput;
        }

        /// <summary>
        ///     Gets the basis transform for locomotion movement, which determines the forward direction
        ///     depending on the defined reference: head, left hand, or right hand.
        /// </summary>
        /// <returns>
        ///     The transform used as the movement basis. Returns the head transform by default,
        ///     unless the relative reference is set to the left hand or right hand.
        /// </returns>
        protected override Transform GetMovementBasis()
        {
            Transform defaultBasis = CameraComponent.transform;
            
            switch (MovementRelativeTo)
            {
                case UxrMovemementRelativeTo.LeftHand:  return Avatar?.ControllerInput?.LeftController3DModel?.Forward  ?? defaultBasis; 
                case UxrMovemementRelativeTo.RightHand: return Avatar?.ControllerInput?.RightController3DModel?.Forward ?? defaultBasis;
                default:                                break;
            }

            return defaultBasis;
        }

        /// <inheritdoc />
        protected override void RotateAvatar(Vector2 rotationInput)
        {
            // Keep only the turn part of the rotation, since the head will be driven by the headset.
            rotationInput.y = 0f;
            base.RotateAvatar(rotationInput);
        }

        #endregion

        #region Private Types & Data

        [Serializable]
        private class DirectionParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(RelativeToTooltip)] public UxrMovemementRelativeTo _relativeTo = UxrMovemementRelativeTo.Head;

            #endregion

            #region Public Types & Data

            public const string RelativeToTooltip = "Specifies the reference for locomotion direction.";

            #endregion
        }

        private UxrStandardSmoothLocomotionInput _standardSmoothLocomotionInput;

        #endregion
    }
}