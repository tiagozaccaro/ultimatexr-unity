// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUniqueIdAssetPostprocessor.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Core.Unique;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Core
{
    /// <summary>
    ///     Asset postprocessor that detects duplicated prefab assets in the Project window and regenerates
    ///     their unique IDs. When a <c>.prefab</c> is duplicated (e.g. Ctrl+D), the new file gets a new asset GUID
    ///     from Unity but its serialized <c>__prefabGuid</c> field still contains the old prefab's GUID.
    ///     This mismatch is used to detect copies and trigger ID regeneration.
    /// </summary>
    public sealed class UxrUniqueIdAssetPostprocessor : AssetPostprocessor
    {
        #region Unity

        /// <summary>
        ///     Called by Unity after assets are imported, deleted, or moved.
        ///     Filters for <c>.prefab</c> files in <paramref name="importedAssets" /> and checks for
        ///     duplicated prefab assets that need unique ID regeneration.
        /// </summary>
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string assetPath in importedAssets)
            {
                if (!assetPath.EndsWith(".prefab"))
                {
                    continue;
                }

                // Guard against re-import loops: when we fix a prefab and save it, Unity reimports it,
                // which triggers OnPostprocessAllAssets again.
                if (s_processingPaths.Contains(assetPath))
                {
                    continue;
                }

                if (!NeedsFix(assetPath))
                {
                    continue;
                }

                // Schedule deferred fix to avoid issues inside the postprocessor callback.
                string pathCopy = assetPath;
                s_processingPaths.Add(pathCopy);
                EditorApplication.delayCall += () => FixPrefabAsset(pathCopy);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Checks whether a prefab asset needs unique ID fixup by comparing the stored <c>__prefabGuid</c>
        ///     against the actual asset GUID. A mismatch indicates the prefab was duplicated or moved.
        /// </summary>
        /// <param name="assetPath">The asset path of the prefab to check</param>
        /// <returns>Whether the prefab needs fixing</returns>
        private static bool NeedsFix(string assetPath)
        {
            string currentAssetGuid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();

            if (string.IsNullOrEmpty(currentAssetGuid) || currentAssetGuid.All(c => c == '0'))
            {
                return false;
            }

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab == null)
            {
                return false;
            }

            foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
            {
                if (component is not IUxrUniqueId)
                {
                    continue;
                }

                SerializedObject   serializedObject   = new SerializedObject(component);
                SerializedProperty prefabGuidProperty = serializedObject.FindProperty(UxrEditorUtils.PropertyPrefabGuid);

                if (prefabGuidProperty != null && prefabGuidProperty.stringValue != currentAssetGuid)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Fixes a duplicated prefab asset by loading it directly via <see cref="AssetDatabase.LoadAssetAtPath{T}" />,
        ///     regenerating unique IDs on all <see cref="IUxrUniqueId" /> components, and saving.
        /// </summary>
        /// <param name="assetPath">The asset path of the prefab to fix</param>
        private static void FixPrefabAsset(string assetPath)
        {
            try
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

                if (prefab == null)
                {
                    return;
                }

                bool changed = false;

                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component is IUxrUniqueId)
                    {
                        changed |= UxrUniqueIdAutoFixer.FixComponentIfNeeded(component);
                    }
                }

                changed |= UxrUniqueIdAutoFixer.FixHierarchyDuplicateIds(prefab);

                if (changed)
                {
                    AssetDatabase.SaveAssets();
                    AssetDatabase.ImportAsset(assetPath);
                }
            }
            finally
            {
                // Remove guard after another delay to survive the reimport cycle triggered by the save.
                string pathCopy = assetPath;
                EditorApplication.delayCall += () => s_processingPaths.Remove(pathCopy);
            }
        }

        #endregion

        #region Private Types & Data

        private static readonly HashSet<string> s_processingPaths = new HashSet<string>();

        #endregion
    }
}