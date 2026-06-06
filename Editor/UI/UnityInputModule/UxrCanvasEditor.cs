// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrCanvasEditor.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.UI.UnityInputModule;
using UnityEditor;

namespace UltimateXR.Editor.UI.UnityInputModule
{
    /// <summary>
    ///     Custom inspector for <see cref="UxrCanvas" />.
    /// </summary>
    [CustomEditor(typeof(UxrCanvas))]
    public class UxrCanvasEditor : UnityEditor.Editor
    {
        #region Unity

        /// <summary>
        ///     Caches the serialized properties.
        /// </summary>
        private void OnEnable()
        {
            _propertyInteractionTypes          = serializedObject.FindProperty("_interactionTypes");
            _propertyFingerTipMinHoverDistance = serializedObject.FindProperty("_fingerTipMinHoverDistance");
            _propertyAutoEnableLaserPointer    = serializedObject.FindProperty("_autoEnableLaserPointer");
            _propertyAutoEnableDistance        = serializedObject.FindProperty("_autoEnableDistance");
            _propertyAllowLeftHand             = serializedObject.FindProperty("_allowLeftHand");
            _propertyAllowRightHand            = serializedObject.FindProperty("_allowRightHand");
        }

        /// <summary>
        ///     Draws the custom inspector and handles input.
        /// </summary>
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_propertyInteractionTypes);

            if (((UxrInteractionTypes)_propertyInteractionTypes.intValue).HasFlag(UxrInteractionTypes.FingerTips))
            {
                EditorGUILayout.PropertyField(_propertyFingerTipMinHoverDistance);
            }
            if (((UxrInteractionTypes)_propertyInteractionTypes.intValue).HasFlag(UxrInteractionTypes.LaserPointers))
            {
                EditorGUILayout.PropertyField(_propertyAutoEnableLaserPointer);
                EditorGUILayout.PropertyField(_propertyAutoEnableDistance);
            }

            EditorGUILayout.PropertyField(_propertyAllowLeftHand);
            EditorGUILayout.PropertyField(_propertyAllowRightHand);

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Private Types & Data

        private SerializedProperty _propertyInteractionTypes;
        private SerializedProperty _propertyFingerTipMinHoverDistance;
        private SerializedProperty _propertyAutoEnableLaserPointer;
        private SerializedProperty _propertyAutoEnableDistance;
        private SerializedProperty _propertyAllowLeftHand;
        private SerializedProperty _propertyAllowRightHand;

        #endregion
    }
}