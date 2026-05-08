// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSyncObjectEditor.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Linq;
using UltimateXR.Core.Components;
using UltimateXR.Core.StateSave;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Core.Components
{
    [CustomEditor(typeof(UxrSyncObject))]
    [CanEditMultipleObjects]
    public class UxrSyncObjectEditor : UnityEditor.Editor
    {
        #region Unity

        /// <summary>
        ///     Creates references to the serialized properties.
        /// </summary>
        private void OnEnable()
        {
            _propertySyncTransform                 = serializedObject.FindProperty(PropertyNameSyncTransform);
            _propertySyncTransformNetwork          = serializedObject.FindProperty(PropertyNameSyncTransformNetwork);
            _propertyTransformSpace                = serializedObject.FindProperty(PropertyNameTransformSpace);
            _propertySyncActiveAndEnabled          = serializedObject.FindProperty(PropertyNameSyncActiveAndEnabled);
            _propertySyncWhileDisabled             = serializedObject.FindProperty(PropertyNameSyncWhileDisabled);
            _propertyNetPositionDistanceThreshold  = serializedObject.FindProperty(PropertyNameNetPositionDistanceThreshold);
            _propertyNetScaleDeltaThreshold        = serializedObject.FindProperty(PropertyNameNetScaleDeltaThreshold);
            _propertyNetRotationDegreesThreshold   = serializedObject.FindProperty(PropertyNameNetRotationDegreesThreshold);
            _propertyOverrideDefaultNetSyncSeconds = serializedObject.FindProperty(PropertyNameOverrideDefaultNetSyncSeconds);
            _propertyNetSyncIntervalSeconds        = serializedObject.FindProperty(PropertyNameNetSyncIntervalSeconds);
        }

        /// <inheritdoc />
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            _foldoutGeneral = UxrEditorUtils.FoldoutStylish("General", _foldoutGeneral);

            if (_foldoutGeneral)
            {
                EditorGUILayout.PropertyField(_propertySyncTransform, ContentSyncTransform);

                if (_propertySyncTransform.boolValue)
                {
                    EditorGUILayout.PropertyField(_propertyTransformSpace, ContentTransformSpace);
                }

                foreach (Object selectedObject in targets)
                {
                    UxrSyncObject syncObject = selectedObject as UxrSyncObject;

                    if (syncObject == null)
                    {
                        continue;
                    }
                    
                    IUxrStateSave stateSaveTransform = syncObject.GetComponents<IUxrStateSave>().FirstOrDefault(c => !ReferenceEquals(c, syncObject) && c.RequiresTransformSerialization(UxrStateSaveLevel.ChangesSinceBeginning));

                    if (syncObject.SyncTransform && stateSaveTransform != null)
                    {
                        if (targets.Length > 1)
                        {
                            EditorGUILayout.HelpBox($"The transform in {syncObject.name} is already synced by a {stateSaveTransform.Component.GetType().Name} component on the same GameObject. Consider disabling transform syncing.", MessageType.Error);
                        }
                        else
                        {
                            EditorGUILayout.HelpBox($"The transform is already synced by a {stateSaveTransform.Component.GetType().Name} component on the same GameObject. Consider disabling transform syncing.", MessageType.Error);
                        }
                    }
                }

                EditorGUILayout.PropertyField(_propertySyncActiveAndEnabled, ContentSyncActiveAndEnabled);
                EditorGUILayout.PropertyField(_propertySyncWhileDisabled,    ContentSyncWhileDisabled);
            }

            _foldoutNetwork = UxrEditorUtils.FoldoutStylish("Networking", _foldoutNetwork);

            if (_foldoutNetwork)
            {
                EditorGUILayout.PropertyField(_propertySyncTransformNetwork, ContentSyncTransformNetwork);

                if (_propertySyncTransformNetwork.boolValue)
                {
                    EditorGUILayout.PropertyField(_propertyNetPositionDistanceThreshold, ContentNetPositionDistanceThreshold);
                    EditorGUILayout.PropertyField(_propertyNetScaleDeltaThreshold,       ContentNetScaleDeltaThreshold);
                    EditorGUILayout.PropertyField(_propertyNetRotationDegreesThreshold,  ContentNetRotationDegreesThreshold);
                    EditorGUILayout.PropertyField(_propertyOverrideDefaultNetSyncSeconds, ContentOverrideDefaultNetSyncSeconds);

                    if (_propertyOverrideDefaultNetSyncSeconds.boolValue)
                    {
                        EditorGUILayout.PropertyField(_propertyNetSyncIntervalSeconds, ContentNetSyncIntervalSeconds);
                    }
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        #endregion

        #region Private Types & Data

        private static GUIContent ContentSyncTransform                 { get; } = new GUIContent("Sync Transform",              "Synchronizes the transform in state saves. This saves the transform in game saves and during replays. It doesn't synchronize the transform in multiplayer mode. For multiplayer use Network Sync Transform instead.");
        private static GUIContent ContentTransformSpace                { get; } = new GUIContent("Space",                       "Space that the transform is saved in. This applies to both Sync Transform and Network Sync Transform.");
        private static GUIContent ContentSyncActiveAndEnabled          { get; } = new GUIContent("Sync Active/Enabled",         "Synchronizes the GameObject's active state and the component's enabled state. This applies to game saves and replays.");
        private static GUIContent ContentSyncWhileDisabled             { get; } = new GUIContent("Sync While Disabled",         "Synchronizes even while the Component/GameObject is disabled. This applies to game saves and replays.");
        private static GUIContent ContentSyncTransformNetwork          { get; } = new GUIContent("Network Sync Transform",      "Synchronise the transformation in multiplayer mode at time intervals. This is useful when there is no NetworkTransform/NetworkRigidbody component set up.");
        private static GUIContent ContentNetPositionDistanceThreshold  { get; } = new GUIContent("Position Distance Threshold", "Minimum distance to synchronize the position.");
        private static GUIContent ContentNetScaleDeltaThreshold        { get; } = new GUIContent("Scale Delta Threshold",       "Minimum scale change to synchronize the scale.");
        private static GUIContent ContentNetRotationDegreesThreshold   { get; } = new GUIContent("Rotation Degrees Threshold",  "Minimum rotation change to synchronize the rotation.");
        private static GUIContent ContentOverrideDefaultNetSyncSeconds { get; } = new GUIContent("Override Net Sync Interval",  "Whether to override the default networking synchronization interval. The default interval is set in the global UltimateXR settings.");
        private static GUIContent ContentNetSyncIntervalSeconds        { get; } = new GUIContent("Net Sync Seconds Interval",   "The networking synchronization interval in seconds.");

        private const string PropertyNameSyncTransform        = "_syncTransform";
        private const string PropertyNameTransformSpace       = "_transformSpace";
        private const string PropertyNameSyncActiveAndEnabled = "_syncActiveAndEnabled";
        private const string PropertyNameSyncWhileDisabled    = "_syncWhileDisabled";
        
        private const string PropertyNameSyncTransformNetwork          = "_syncTransformNetwork";
        private const string PropertyNameNetPositionDistanceThreshold  = "_netPositionDistanceThreshold";
        private const string PropertyNameNetScaleDeltaThreshold        = "_netScaleDeltaThreshold";
        private const string PropertyNameNetRotationDegreesThreshold   = "_netRotationDegreesThreshold";
        private const string PropertyNameOverrideDefaultNetSyncSeconds = "_overrideDefaultNetSyncIntervalSeconds";
        private const string PropertyNameNetSyncIntervalSeconds        = "_netSyncIntervalSecondsOverride";

        private SerializedProperty _propertySyncTransform;
        private SerializedProperty _propertyTransformSpace;
        private SerializedProperty _propertySyncActiveAndEnabled;
        private SerializedProperty _propertySyncWhileDisabled;

        private SerializedProperty _propertySyncTransformNetwork;
        private SerializedProperty _propertyNetPositionDistanceThreshold;
        private SerializedProperty _propertyNetScaleDeltaThreshold;
        private SerializedProperty _propertyNetRotationDegreesThreshold;
        private SerializedProperty _propertyOverrideDefaultNetSyncSeconds;
        private SerializedProperty _propertyNetSyncIntervalSeconds;

        private bool _foldoutGeneral = true;
        private bool _foldoutNetwork = true;

        #endregion
    }
}