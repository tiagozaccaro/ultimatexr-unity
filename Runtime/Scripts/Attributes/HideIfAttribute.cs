// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HideIfAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Attributes
{
    /// <summary>
    ///     Attribute that conditionally hides a serialized field in the Unity Inspector depending on the value of
    ///     another field in the same component.
    ///     Supports comparisons against boolean, integer, float, enum, and object reference values, including
    ///     comparisons against other object references or null, allowing Inspector layouts to stay cleaner and
    ///     automatically show or hide properties that are not relevant for the current configuration.
    ///     <para>
    ///         Example using a boolean condition:
    ///     </para>
    ///     <code>
    ///     [SerializeField] private bool autoPlayAudio;
    ///     [HideIf(nameof(autoPlayAudio), false)]
    ///     [SerializeField] private float fadeInDuration;
    ///     </code>
    ///     <para>
    ///         Example using an enum condition:
    ///     </para>
    ///     <code>
    ///     private enum InteractionMode
    ///     {
    ///         Instant,
    ///         Hold,
    ///         Toggle
    ///     }
    ///
    ///     [SerializeField] private InteractionMode interactionMode;
    ///     [HideIf(nameof(interactionMode), InteractionMode.Instant, InteractionMode.Toggle)]
    ///     [SerializeField] private float holdDuration;
    ///     </code>
    /// </summary>
    public class HideIfAttribute : ShowIfAttribute
    {
        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="conditionFieldName">The name of the field that determines visibility.</param>
        /// <param name="expectedValue">The value required for the field to be hidden.</param>
        /// <param name="otherValues">Other optional values that also determine visibility.</param>
        public HideIfAttribute(string conditionFieldName, object expectedValue, params object[] otherValues) : base(conditionFieldName, expectedValue, otherValues)
        {
        }

        #endregion
    }
}