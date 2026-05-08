// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ShowIfPropertyDrawer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Linq;
using UltimateXR.Attributes;
using UltimateXR.Core;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Attributes
{
    /// <summary>
    ///     Custom property drawer for <see cref="ShowIfAttribute" />.
    ///     Controls whether a field is displayed based on the value of another field.
    /// </summary>
    [CustomPropertyDrawer(typeof(ShowIfAttribute))]
    public class ShowIfPropertyDrawer : PropertyDrawer
    {
        #region Public Overrides PropertyDrawer

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            return ShouldShow(property, showIf) ? EditorGUI.GetPropertyHeight(property, label) : -EditorGUIUtility.standardVerticalSpacing;
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            ShowIfAttribute showIf = (ShowIfAttribute)attribute;
            if (ShouldShow(property, showIf))
            {
                EditorGUI.PropertyField(position, property, label, true);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Determines whether a field should be displayed based on the comparison between the given value and the expected
        ///     value.
        /// </summary>
        /// <param name="value">
        ///     The current value of the property being evaluated.
        /// </param>
        /// <param name="expectedValue">
        ///     The value that the current property's value is compared against to determine visibility.
        /// </param>
        /// <returns>
        ///     True if the field should be displayed, otherwise false.
        /// </returns>
        protected virtual bool ShouldShow(object value, object expectedValue)
        {
            return Equals(value, expectedValue);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Checks whether the property should be displayed based on the condition field's value.
        /// </summary>
        /// <param name="property">The serialized property being evaluated.</param>
        /// <param name="showIf">The <see cref="ShowIfAttribute" /> instance containing the condition.</param>
        /// <returns>True if the property should be displayed, false otherwise.</returns>
        private bool ShouldShow(SerializedProperty property, ShowIfAttribute showIf)
        {
            SerializedProperty conditionProperty = property.serializedObject.FindProperty(showIf.ConditionFieldName);

            if (conditionProperty == null)
            {
                // Try to resolve from full path if relative failed (handle nested structures)
                string path       = property.propertyPath;
                string parentPath = path.Substring(0, path.LastIndexOf('.'));
                string fullPath   = string.IsNullOrEmpty(parentPath) ? showIf.ConditionFieldName : $"{parentPath}.{showIf.ConditionFieldName}";
                conditionProperty = property.serializedObject.FindProperty(fullPath);
            }

            if (conditionProperty == null)
            {
                Debug.LogError($"{UxrConstants.CoreModule} {GetType().Name}: Could not find property '{showIf.ConditionFieldName}' in {property.serializedObject.targetObject.GetType().Name}");
                return true; // Show by default if the field is missing to avoid unexpected behavior
            }

            object   actualValue   = GetPropertyValue(conditionProperty);
            object   expectedValue = showIf.ExpectedValue;
            object[] otherValues   = showIf.OtherValues;
            
            // If the value is null, log an error and show the field
            if (actualValue == null && conditionProperty.propertyType != SerializedPropertyType.ObjectReference)
            {
                Debug.LogError($"{UxrConstants.CoreModule} {GetType().Name}: Unsupported property type '{conditionProperty.propertyType}' in {property.serializedObject.targetObject.GetType().Name} for field '{showIf.ConditionFieldName}'.");
                return true; // Always show in case of an unsupported type
            }

            // If the condition is an enum, convert expected value to an int for proper comparison
            if (conditionProperty.propertyType == SerializedPropertyType.Enum)
            {
                expectedValue = Convert.ToInt32(expectedValue); // Convert enum to int
            }

            return ShouldShow(actualValue, expectedValue) || otherValues.Any(v => ShouldShow(actualValue,v));
        }

        /// <summary>
        ///     Retrieves the actual value of the given SerializedProperty.
        ///     Properly handles enums as integer indices.
        /// </summary>
        private object GetPropertyValue(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.ObjectReference: return property.objectReferenceValue;
                case SerializedPropertyType.Boolean:         return property.boolValue;
                case SerializedPropertyType.Integer:         return property.intValue;
                case SerializedPropertyType.Float:           return property.floatValue;
                case SerializedPropertyType.Enum:            return property.enumValueIndex; // Get the index of the selected enum
                default:                                     return null;
            }
        }

        #endregion
    }
}