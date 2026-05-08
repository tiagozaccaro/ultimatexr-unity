// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShowIfAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UnityEngine;

namespace UltimateXR.Attributes
{
    /// <summary>
    ///     Attribute that conditionally displays a serialized field in the Unity Inspector depending on the value of
    ///     another field in the same component.
    ///     Supports comparisons against boolean, integer, float, enum, and object reference values, including
    ///     comparisons against other object references or null, allowing Inspector layouts to stay cleaner and
    ///     automatically show or hide properties that are not relevant for the current configuration.
    ///     <para>
    ///         Example using a boolean condition:
    ///     </para>
    ///     <code>
    ///     [SerializeField] private bool useCustomDamage;
    ///     [ShowIf(nameof(useCustomDamage), true)]
    ///     [SerializeField] private int customDamage;
    ///     </code>
    ///     <para>
    ///         Example using an enum condition:
    ///     </para>
    ///     <code>
    ///     private enum MovementType
    ///     {
    ///         Walk,
    ///         Fly,
    ///         Swim
    ///     }
    /// 
    ///     [SerializeField] private MovementType movementType;
    ///     [ShowIf(nameof(movementType), MovementType.Fly, MovementType.Swim)]
    ///     [SerializeField] private float flightOrSwimSpeed;
    ///     </code>
    /// </summary>
    public class ShowIfAttribute : PropertyAttribute
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the name of the variable that controls the visibility of the field.
        /// </summary>
        public string ConditionFieldName { get; }

        /// <summary>
        ///     Gets the expected value that determines the visibility of the field.
        /// </summary>
        public object ExpectedValue { get; }

        /// <summary>
        ///     Gets the other optional values that also determine the visibility of the field.
        /// </summary>
        public object[] OtherValues { get; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="conditionFieldName">The name of the field that determines visibility.</param>
        /// <param name="expectedValue">The value required for the field to be shown.</param>
        /// <param name="otherValues">Other optional values that are also valid</param>
        public ShowIfAttribute(string conditionFieldName, object expectedValue, params object[] otherValues)
        {
            ConditionFieldName = conditionFieldName;
            ExpectedValue      = expectedValue;
            OtherValues        = otherValues;
        }

        #endregion
    }
}