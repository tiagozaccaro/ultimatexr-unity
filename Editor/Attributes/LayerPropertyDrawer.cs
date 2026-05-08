// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LayerPropertyDrawer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Attributes;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Attributes
{
    [CustomPropertyDrawer(typeof(LayerAttribute))]
    public class LayerDrawer : PropertyDrawer
    {
        #region Unity

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.Integer)
            {
                // Get layer names
                
                string[] layers = new string[32];
                
                for (int i = 0; i < 32; i++)
                {
                    layers[i] = LayerMask.LayerToName(i);
                }

                // Filter out empty layers
                layers = Array.FindAll(layers, layer => !string.IsNullOrEmpty(layer));

                // Get current selected layer
                int    selectedLayer    = property.intValue;
                string currentLayerName = LayerMask.LayerToName(selectedLayer);
                int    selectedIndex    = Array.IndexOf(layers, currentLayerName);

                // Dropdown selection
                selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, layers);

                // Update property
                if (selectedIndex >= 0)
                {
                    property.intValue = LayerMask.NameToLayer(layers[selectedIndex]);
                }
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use [Layer] with int fields only.");
            }
        }

        #endregion
    }
}