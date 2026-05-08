// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InspectorButtonAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core;
using UnityEngine;

namespace UltimateXR.Attributes
{
    /// <summary>
    ///     Attribute used to draw a button in the Unity Inspector that invokes a method when clicked.
    ///     <para>
    ///         The method is searched on the object that owns the serialized field. If the field belongs to a nested
    ///         serializable class or struct, the search walks up the ownership chain until the method is found,
    ///         including the root <see cref="MonoBehaviour" /> or <see cref="ScriptableObject" />.
    ///     </para>
    ///     <para>
    ///         This allows buttons to be declared both on component fields and on fields inside nested serialized
    ///         classes while still invoking methods defined on the parent component.
    ///     </para>
    ///     Useful for quick editor tools, debugging, and testing.
    /// </summary>
    /// <example>
    ///     <code>
    ///     [InspectorButton("Test", nameof(PrintLine))] [SerializeField] private bool _buttonTest;
    /// 
    ///     private void PrintLine()
    ///     {
    ///         Debug.Log("This is a test");
    ///     }
    ///     </code>
    /// </example>
    public class InspectorButtonAttribute : PropertyAttribute
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the label text displayed on the button.
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        ///     Gets the name of the method that this button in the Unity Inspector is bound to.
        ///     The method will be invoked using reflection.
        /// </summary>
        public string MethodName { get; private set; }

        /// <summary>
        ///     Gets the button width in pixels.
        /// </summary>
        public int Width { get; private set; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="label">Label text displayed on the button</param>
        /// <param name="methodName">Name of the method that this button will invoke through reflection</param>
        /// <param name="width">Button width in pixels</param>
        public InspectorButtonAttribute(string label, string methodName, int width = UxrConstants.Editor.DefaultButtonWidth)
        {
            Label      = label;
            MethodName = methodName;
            Width      = width;
        }

        #endregion
    }
}