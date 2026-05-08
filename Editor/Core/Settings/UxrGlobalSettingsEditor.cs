// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGlobalSettingsEditor.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;
using UltimateXR.Core.Settings;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Core.Settings
{
    /// <summary>
    ///     Custom inspector for <see cref="UxrGlobalSettings" />.
    /// </summary>
    [CustomEditor(typeof(UxrGlobalSettings))]
    public class UxrGlobalSettingsEditor : UnityEditor.Editor
    {
        #region Unity

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField($"{UxrConstants.UltimateXR} version: {UxrConstants.Version}");
            EditorGUILayout.Space(10);

            DrawPropertiesExcluding(serializedObject, "m_Script");

            serializedObject.ApplyModifiedProperties();
        }

        #endregion
    }
}