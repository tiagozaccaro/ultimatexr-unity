// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUniqueIdAutoFixer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Core;
using UltimateXR.Core.Unique;
using UltimateXR.Extensions.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimateXR.Editor.Core
{
    /// <summary>
    ///     Automatically ensures all <see cref="IUxrUniqueId" /> components have correct unique ID information
    ///     with proper <see cref="SerializedProperty.prefabOverride" /> flags on prefab instances.
    ///     This replaces the need for the manual <c>UniqueIdGenerationWindow</c> tool.
    /// </summary>
    public static class UxrUniqueIdAutoFixer
    {
        #region Public Methods

        /// <summary>
        ///     Fixes the unique ID information of a component if necessary, using <see cref="SerializedObject" /> /
        ///     <see cref="SerializedProperty" /> to ensure <see cref="SerializedProperty.prefabOverride" /> flags
        ///     are correctly set on prefab instances.
        /// </summary>
        /// <param name="component">The component to fix</param>
        /// <returns>Whether the component required changes</returns>
        public static bool FixComponentIfNeeded(Component component)
        {
            return FixComponent(component, false, false);
        }

        /// <summary>
        ///     Fixes the unique ID information of a component, forcing unique ID regeneration.
        /// </summary>
        /// <param name="component">The component to fix</param>
        /// <returns>Whether the component required changes</returns>
        public static bool FixComponentForceRegenerate(Component component)
        {
            return FixComponent(component, true, false);
        }

        /// <summary>
        ///     Fixes duplicate or empty unique IDs inside a hierarchy.
        /// </summary>
        /// <param name="rootGameObject">The root object to validate</param>
        public static bool FixHierarchyDuplicateIds(GameObject rootGameObject)
        {
            return ResolveDuplicateIdsInHierarchy(rootGameObject);
        }

        /// <summary>
        ///     Fixes unique ID information in all prefab assets in the project.
        /// </summary>
        /// <param name="processedPrefabCount">Number of prefabs that were processed</param>
        /// <param name="changedPrefabCount">Number of prefabs that required changes</param>
        /// <param name="canceled">Whether the operation was canceled by the user</param>
        public static void FixAllProjectPrefabUniqueIds(out int processedPrefabCount, out int changedPrefabCount, out bool canceled)
        {
            processedPrefabCount = 0;
            changedPrefabCount   = 0;
            canceled             = false;

            string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" });
            bool     automaticIdGenerationWasEnabled = EditorPrefs.GetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, true);

            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, false);

            try
            {
                for (int i = 0; i < prefabGuids.Length; i++)
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);

                    if (EditorUtility.DisplayCancelableProgressBar("Fixing Unique IDs", $"Processing {assetPath}", prefabGuids.Length > 0 ? (float)i / prefabGuids.Length : 1.0f))
                    {
                        canceled = true;
                        return;
                    }

                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                    if (prefab == null)
                    {
                        continue;
                    }

                    PrefabAssetType prefabAssetType = PrefabUtility.GetPrefabAssetType(prefab);

                    if (prefabAssetType == PrefabAssetType.Model || prefabAssetType == PrefabAssetType.MissingAsset)
                    {
                        continue;
                    }

                    bool changed = false;

                    foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                    {
                        if (component is IUxrUniqueId)
                        {
                            changed |= FixComponentIfNeeded(component);
                        }
                    }

                    changed |= FixHierarchyDuplicateIds(prefab);
                    processedPrefabCount++;

                    if (changed)
                    {
                        changedPrefabCount++;
                        PrefabUtility.SavePrefabAsset(prefab);
                    }
                }

                if (changedPrefabCount > 0)
                {
                    AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, automaticIdGenerationWasEnabled);
            }
        }

        /// <summary>
        ///     Fixes the unique ID information of a component, with options to force regeneration or only check without modifying.
        /// </summary>
        /// <param name="component">The component to fix</param>
        /// <param name="forceRegenerateId">Whether to force the regeneration of the unique id</param>
        /// <param name="onlyCheck">
        ///     Whether to only check and return a value telling if the component requires changes
        ///     but not perform any modifications on it
        /// </param>
        /// <returns>Whether the component required changes</returns>
        public static bool FixComponent(Component component, bool forceRegenerateId, bool onlyCheck)
        {
            if (component == null || component is not IUxrUniqueId)
            {
                return false;
            }

            SerializedObject serializedObject = new SerializedObject(component);
            serializedObject.Update();

            SerializedProperty uniqueIdProperty   = serializedObject.FindProperty(UxrEditorUtils.PropertyUniqueId);
            SerializedProperty prefabGuidProperty = serializedObject.FindProperty(UxrEditorUtils.PropertyPrefabGuid);
            SerializedProperty isInPrefabProperty = serializedObject.FindProperty(UxrEditorUtils.PropertyIsInPrefab);

            if (uniqueIdProperty == null || prefabGuidProperty == null || isInPrefabProperty == null)
            {
                return false;
            }

            bool isOriginalSource = PrefabUtility.GetCorrespondingObjectFromSource(component) == null;

            if (!component.GetPrefabGuid(out string prefabGuid))
            {
                prefabGuid = string.Empty;
            }

            bool isInPrefab  = component.IsInPrefab();
            bool needsChange = false;

            // Detect duplicated prefab assets: the original source whose stored __prefabGuid doesn't match the actual asset GUID.
            // This happens when a .prefab file is duplicated in the Project window, the copy gets a new asset GUID,
            // but its serialized __prefabGuid still contains the old prefab's GUID.
            bool isPrefabAssetCopy = isOriginalSource
                                     && !string.IsNullOrEmpty(prefabGuid)
                                     && prefabGuidProperty.stringValue != prefabGuid;

            // Fix __prefabGuid: value mismatch OR non-original-source without prefabOverride.
            if (prefabGuidProperty.stringValue != prefabGuid || (!isOriginalSource && !prefabGuidProperty.prefabOverride))
            {
                if (!onlyCheck)
                {
                    ForceOverrideString(serializedObject, prefabGuidProperty, prefabGuid);
                }

                needsChange = true;
            }

            // Fix __isInPrefab: value mismatch OR non-original-source without prefabOverride.
            if (isInPrefabProperty.boolValue != isInPrefab || (!isOriginalSource && !isInPrefabProperty.prefabOverride))
            {
                if (!onlyCheck)
                {
                    ForceOverrideBool(serializedObject, isInPrefabProperty, isInPrefab);
                }

                needsChange = true;
            }

            // Fix _uxrUniqueId: forced regeneration OR duplicated prefab asset OR empty OR non-original-source without prefabOverride.
            if (forceRegenerateId || isPrefabAssetCopy || string.IsNullOrEmpty(uniqueIdProperty.stringValue) || (!isOriginalSource && !uniqueIdProperty.prefabOverride))
            {
                if (!onlyCheck)
                {
                    Guid newUniqueId = UxrUniqueIdImplementer.GetNewUniqueId();
                    ForceOverrideString(serializedObject, uniqueIdProperty, newUniqueId.ToString());
                }

                needsChange = true;
            }

            if (!onlyCheck && needsChange)
            {
                //Debug.Log("Auto-fixed unique ID for object: " + component.GetPathUnderScene() + " prefabGuid=" + prefabGuid + " uniqueId=" + uniqueIdProperty.stringValue + " isInPrefab=" + isInPrefab, component);
                serializedObject.ApplyModifiedProperties();
            }

            return needsChange;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when Unity publishes object change events. Provides fine-grained information about which objects
        ///     were created or structurally changed, allowing targeted processing.
        /// </summary>
        /// <param name="stream">The stream of object change events</param>
        private static void OnObjectChangeEvents(ref ObjectChangeEventStream stream)
        {
            bool hasChanges = false;

            for (int i = 0; i < stream.length; i++)
            {
                ObjectChangeKind type = stream.GetEventType(i);

                switch (type)
                {
                    case ObjectChangeKind.CreateGameObjectHierarchy:
                    {
                        stream.GetCreateGameObjectHierarchyEvent(i, out CreateGameObjectHierarchyEventArgs data);
                        AddGameObjectComponents(data.entityId);
                        hasChanges = true;
                        break;
                    }

                    case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
                    {
                        stream.GetChangeGameObjectStructureHierarchyEvent(i, out ChangeGameObjectStructureHierarchyEventArgs data);
                        AddGameObjectComponents(data.entityId);
                        hasChanges = true;
                        break;
                    }

                    case ObjectChangeKind.ChangeGameObjectStructure:
                    {
                        stream.GetChangeGameObjectStructureEvent(i, out ChangeGameObjectStructureEventArgs data);
                        AddGameObjectComponents(data.entityId);
                        hasChanges = true;
                        break;
                    }
                }
            }

            if (hasChanges)
            {
                ScheduleFixAll();
            }
        }

        /// <summary>
        ///     Called when the editor hierarchy changes. Acts as a fallback catch-all for cases not covered
        ///     by <see cref="ObjectChangeEvents" />.
        /// </summary>
        private static void OnHierarchyChanged()
        {
            if (s_pendingComponents.Count == 0)
            {
                // No targeted changes from ObjectChangeEvents — schedule a full-scene scan.
                ScheduleFixAll();
            }
        }

        /// <summary>
        ///     Called when a component is added in the editor. This helps cover direct Add Component operations that may
        ///     not map cleanly to a created GameObject hierarchy.
        /// </summary>
        /// <param name="component">The added component</param>
        private static void OnComponentWasAdded(Component component)
        {
            if (component == null)
            {
                return;
            }

            if (component is not IUxrUniqueId)
            {
                return;
            }

            s_pendingComponents.Add(component);
            ScheduleFixAll();
        }

        /// <summary>
        ///     Called before a prefab is saved in prefab editing mode (isolation/context).
        ///     <see cref="EditorSceneManager.sceneSaving" /> does not fire for prefab mode saves, so this
        ///     is needed to ensure all components in the prefab have valid unique IDs before persisting.
        /// </summary>
        /// <param name="prefabContentsRoot">The root GameObject of the prefab being saved</param>
        private static void OnPrefabSaving(GameObject prefabContentsRoot)
        {
            if (prefabContentsRoot == null)
            {
                return;
            }

            // Temporarily disable automatic ID generation to prevent OnValidate re-entrant calls.
            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, false);

            try
            {
                foreach (IUxrUniqueId unique in prefabContentsRoot.GetComponentsInChildren<IUxrUniqueId>(true))
                {
                    if (unique is Component component)
                    {
                        FixComponentIfNeeded(component);
                    }
                }

                ResolveDuplicateIdsInHierarchy(prefabContentsRoot);
            }
            finally
            {
                EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, true);
            }
        }

        /// <summary>
        ///     Called by Unity when the scene is about to be saved. Performs a synchronous fix pass and duplicate detection.
        /// </summary>
        /// <param name="scene">Scene to be saved</param>
        /// <param name="path">Path to save to</param>
        private static void OnSceneSaving(Scene scene, string path)
        {
            // Temporarily disable automatic ID generation to prevent OnValidate re-entrant calls.
            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, false);

            try
            {
                // Pass 1: Fix prefabOverride flags and empty IDs.
                foreach (GameObject rootGameObject in scene.GetRootGameObjects())
                {
                    foreach (IUxrUniqueId unique in rootGameObject.GetComponentsInChildren<IUxrUniqueId>(true))
                    {
                        if (unique is Component component)
                        {
                            FixComponentIfNeeded(component);
                        }
                    }
                }

                // Pass 2: Detect and fix duplicate IDs immediately before saving.
                ResolveDuplicateIdsInScene(scene);
            }
            finally
            {
                // Clear pending set since we just processed everything synchronously.
                s_pendingComponents.Clear();
                s_fixScheduled = false;

                // Re-enable automatic ID generation.
                EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, true);
            }
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Called when a prefab instance is updated after an Apply operation.
        /// </summary>
        /// <param name="instance">The prefab instance that was updated</param>
        private static void OnPrefabInstanceUpdated(GameObject instance)
        {
            if (instance == null)
            {
                return;
            }

            foreach (IUxrUniqueId unique in instance.GetComponentsInChildren<IUxrUniqueId>(true))
            {
                if (unique is Component component)
                {
                    s_pendingComponents.Add(component);
                }
            }

            ScheduleFixAll();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Method called when the editor loads. Hooks into Unity editor events for automatic unique ID fixup.
        /// </summary>
        [InitializeOnLoadMethod]
        private static void Initialize()
        {
            ObjectChangeEvents.changesPublished -= OnObjectChangeEvents;
            ObjectChangeEvents.changesPublished += OnObjectChangeEvents;

            EditorApplication.hierarchyChanged -= OnHierarchyChanged;
            EditorApplication.hierarchyChanged += OnHierarchyChanged;

            ObjectFactory.componentWasAdded -= OnComponentWasAdded;
            ObjectFactory.componentWasAdded += OnComponentWasAdded;

            PrefabUtility.prefabInstanceUpdated -= OnPrefabInstanceUpdated;
            PrefabUtility.prefabInstanceUpdated += OnPrefabInstanceUpdated;

            PrefabStage.prefabSaving -= OnPrefabSaving;
            PrefabStage.prefabSaving += OnPrefabSaving;

            EditorSceneManager.sceneSaving -= OnSceneSaving;
            EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        /// <summary>
        ///     Schedules a deferred fix pass if one is not already scheduled.
        /// </summary>
        private static void ScheduleFixAll()
        {
            if (!s_fixScheduled)
            {
                s_fixScheduled              =  true;
                EditorApplication.delayCall += ProcessPendingFixes;
            }
        }

        /// <summary>
        ///     Processes all pending components that need unique ID fixup.
        /// </summary>
        private static void ProcessPendingFixes()
        {
            s_fixScheduled = false;

            if (EditorApplication.isPlayingOrWillChangePlaymode ||
                EditorApplication.isCompiling                   ||
                BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            // Temporarily disable automatic ID generation to prevent re-entrant OnValidate calls.
            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, false);

            try
            {
                Component[] componentsToProcess = s_pendingComponents.Count > 0
                                                      ? s_pendingComponents.Where(c => c             != null).ToArray()
                                                      : GetAllSceneUniqueIdComponents().Where(c => c != null).ToArray();

                foreach (Component component in componentsToProcess)
                {
                    FixComponentIfNeeded(component);
                }

                HashSet<Scene> affectedScenes = new HashSet<Scene>();

                foreach (Component component in componentsToProcess)
                {
                    if (component == null || !component.gameObject.scene.IsValid())
                    {
                        continue;
                    }

                    affectedScenes.Add(component.gameObject.scene);
                }

                if (affectedScenes.Count == 0 && s_pendingComponents.Count == 0)
                {
                    for (int i = 0; i < SceneManager.sceneCount; i++)
                    {
                        Scene scene = SceneManager.GetSceneAt(i);

                        if (scene.isLoaded)
                        {
                            affectedScenes.Add(scene);
                        }
                    }
                }

                foreach (Scene scene in affectedScenes)
                {
                    ResolveDuplicateIdsInScene(scene);
                }
            }
            finally
            {
                s_pendingComponents.Clear();
                EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, true);
            }
        }

        /// <summary>
        ///     Gets all <see cref="IUxrUniqueId" /> components in all loaded scenes.
        /// </summary>
        /// <returns>All components implementing <see cref="IUxrUniqueId" /> in loaded scenes</returns>
        private static IEnumerable<Component> GetAllSceneUniqueIdComponents()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);

                if (!scene.isLoaded)
                {
                    continue;
                }

                foreach (GameObject rootGameObject in scene.GetRootGameObjects())
                {
                    foreach (IUxrUniqueId unique in rootGameObject.GetComponentsInChildren<IUxrUniqueId>(true))
                    {
                        if (unique is Component component)
                        {
                            yield return component;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Adds all <see cref="IUxrUniqueId" /> components from a <see cref="GameObject" /> (identified by entity ID)
        ///     and its children to the pending set.
        /// </summary>
        /// <param name="entityId">The entity ID of the GameObject</param>
        private static void AddGameObjectComponents(int entityId)
        {
            GameObject obj = EditorUtility.InstanceIDToObject(entityId) as GameObject;

            if (obj == null)
            {
                return;
            }

            foreach (IUxrUniqueId unique in obj.GetComponentsInChildren<IUxrUniqueId>(true))
            {
                if (unique is Component component)
                {
                    s_pendingComponents.Add(component);
                }
            }
        }

        /// <summary>
        ///     Resolves duplicate or empty unique IDs across a full scene.
        /// </summary>
        /// <param name="scene">The scene to validate</param>
        private static bool ResolveDuplicateIdsInScene(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return false;
            }

            Dictionary<Guid, IUxrUniqueId> sceneUxrComponents = new Dictionary<Guid, IUxrUniqueId>();
            bool                           changed            = false;

            foreach (GameObject rootGameObject in scene.GetRootGameObjects())
            {
                changed |= ResolveDuplicateIdsInHierarchy(rootGameObject, sceneUxrComponents);
            }

            return changed;
        }

        /// <summary>
        ///     Resolves duplicate or empty unique IDs inside a hierarchy.
        /// </summary>
        /// <param name="rootGameObject">The root object to validate</param>
        private static bool ResolveDuplicateIdsInHierarchy(GameObject rootGameObject)
        {
            return ResolveDuplicateIdsInHierarchy(rootGameObject, new Dictionary<Guid, IUxrUniqueId>());
        }

        /// <summary>
        ///     Resolves duplicate or empty unique IDs inside a hierarchy using an existing lookup.
        /// </summary>
        /// <param name="rootGameObject">The root object to validate</param>
        /// <param name="usedIds">Already used IDs to preserve uniqueness against</param>
        private static bool ResolveDuplicateIdsInHierarchy(GameObject rootGameObject, Dictionary<Guid, IUxrUniqueId> usedIds)
        {
            if (rootGameObject == null)
            {
                return false;
            }

            bool changed = false;

            foreach (IUxrUniqueId uniqueIdComponent in rootGameObject.GetComponentsInChildren<IUxrUniqueId>(true))
            {
                int attemptCount = 0;

                while (uniqueIdComponent.UniqueId == Guid.Empty || usedIds.ContainsKey(uniqueIdComponent.UniqueId))
                {
                    uniqueIdComponent.ChangeUniqueId(UxrUniqueIdImplementer.GetNewUniqueId());

                    if (uniqueIdComponent.Component != null)
                    {
                        EditorUtility.SetDirty(uniqueIdComponent.Component);
                    }

                    attemptCount++;
                    changed = true;

                    if (attemptCount > MaxDuplicateFixAttempts)
                    {
                        break;
                    }
                }

                if (uniqueIdComponent.UniqueId != Guid.Empty)
                {
                    usedIds[uniqueIdComponent.UniqueId] = uniqueIdComponent;
                }
            }

            return changed;
        }

        /// <summary>
        ///     Forces a string <see cref="SerializedProperty" /> to be marked as a prefab override by setting a dummy value,
        ///     applying, then setting the real value. This works around Unity not supporting direct
        ///     <see cref="SerializedProperty.prefabOverride" /> assignment in all cases.
        /// </summary>
        /// <param name="serializedObject">The serialized object</param>
        /// <param name="property">The property to override</param>
        /// <param name="value">The value to set</param>
        private static void ForceOverrideString(SerializedObject serializedObject, SerializedProperty property, string value)
        {
            property.stringValue = "AA";
            serializedObject.ApplyModifiedProperties();
            property.stringValue = value;
        }

        /// <summary>
        ///     Forces a bool <see cref="SerializedProperty" /> to be marked as a prefab override by setting a dummy value,
        ///     applying, then setting the real value.
        /// </summary>
        /// <param name="serializedObject">The serialized object</param>
        /// <param name="property">The property to override</param>
        /// <param name="value">The value to set</param>
        private static void ForceOverrideBool(SerializedObject serializedObject, SerializedProperty property, bool value)
        {
            property.boolValue = !value;
            serializedObject.ApplyModifiedProperties();
            property.boolValue = value;
        }

        #endregion

        #region Private Types & Data

        private const           int                MaxDuplicateFixAttempts = 100;
        private static readonly HashSet<Component> s_pendingComponents     = new HashSet<Component>();

        private static bool s_fixScheduled;

        #endregion
    }
}