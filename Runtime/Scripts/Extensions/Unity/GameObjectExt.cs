// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GameObjectExt.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.Core.Unique;
using UltimateXR.Extensions.System;
using UltimateXR.Extensions.System.Collections;
using UltimateXR.Extensions.Unity.Math;
using UltimateXR.Manipulation;
using UltimateXR.UI.UnityInputModule;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace UltimateXR.Extensions.Unity
{
    /// <summary>
    ///     <see cref="GameObject" /> extensions.
    /// </summary>
    public static class GameObjectExt
    {
        #region Public Methods

        /// <summary>
        ///     Gets the Component of a given type in the GameObject or any of its parents. It also works on prefabs, where regular
        ///     <see cref="Component.GetComponentInParent" /> will not work:
        ///     https://issuetracker.unity3d.com/issues/getcomponentinparent-is-returning-null-when-the-gameobject-is-a-prefab
        ///     It also works on objects recently instantiated or on disabled objects.
        /// </summary>
        /// <typeparam name="T">Component type to get</typeparam>
        /// <returns>Component in same GameObject or any of its parents. Null if it wasn't found</returns>
        public static T SafeGetComponentInParent<T>(this GameObject self)
        {
            T parent = self.GetComponentInParent<T>();
            return parent ?? self.GetComponentsInParent<T>(true).FirstOrDefault();
        }

        /// <summary>
        ///     Traverses up the transform hierarchy from the specified starting point
        ///     and returns the topmost (highest in the hierarchy) component of the specified type, if any.
        /// </summary>
        /// <typeparam name="T">The type of component to search for</typeparam>
        /// <param name="start">The starting GameObject to begin the search from</param>
        /// <returns>
        ///     The topmost component of type <typeparamref name="T" /> found in the hierarchy,
        ///     or <c>null</c> if no such component exists.
        /// </returns>
        public static T GetTopmostComponentInHierarchy<T>(this GameObject start)
        {
            return start.transform.GetTopmostComponentInHierarchy<T>();
        }

        /// <summary>
        ///     Activates/deactivates the object if it isn't active already.
        /// </summary>
        /// <param name="self">GameObject to activate</param>
        /// <param name="activate">Whether to activate or deactivate the object</param>
        public static void CheckSetActive(this GameObject self, bool activate)
        {
            if (self.activeSelf != activate)
            {
                self.SetActive(activate);
            }
        }

        /// <summary>
        ///     Gets a unique path in the scene for the given GameObject. It will include sibling indices to make it unique.
        /// </summary>
        /// <param name="self">GameObject to get the unique path for</param>
        /// <returns>Unique GameObject path string</returns>
        /// <seealso cref="TransformExt.GetUniqueScenePath" />
        public static string GetUniqueScenePath(this GameObject self)
        {
            self.ThrowIfNull(nameof(self));
            return self.transform.GetUniqueScenePath();
        }

        /// <summary>
        ///     Gets the path in the scene of the given GameObject.
        /// </summary>
        /// <param name="self">GameObject to get the scene path for</param>
        /// <returns>Path of the GameObject in the scene</returns>
        /// <seealso cref="TransformExt.GetPathUnderScene" />
        public static string GetPathUnderScene(this GameObject self)
        {
            self.ThrowIfNull(nameof(self));
            return self.transform.GetPathUnderScene();
        }

        /// <summary>
        ///     Tries to get a component on the GameObject that implements the <see cref="IUxrUniqueId" /> interface.
        ///     It will prefer those with <see cref="IUxrUniqueId.PreferForTracking" /> set to true to minimize
        ///     cases where the component can't be found.
        /// </summary>
        /// <param name="self">GameObject to get a <see cref="IUxrUniqueId" /> component for</param>
        public static IUxrUniqueId GetTrackingUniqueIdComponent(this GameObject self)
        {
            IUxrUniqueId[] components = self.GetComponents<IUxrUniqueId>();

            foreach (IUxrUniqueId uniqueId in components)
            {
                if (uniqueId.PreferForTracking)
                {
                    return uniqueId;
                }
            }

            return components.FirstOrDefault();
        }

        /// <summary>
        ///     Combines all <see cref="IUxrUniqueId" /> components in the GameObject tree using a given id.
        ///     This ensures that unique ids in the hierarchy do not collide when instantiating the same prefab more than once.
        ///     Combination using the same id on different devices or sessions will always generate the same unique result.
        /// </summary>
        /// <param name="self">The GameObject whose components to combine</param>
        /// <param name="guid">A guid that will be used to combine all existing unique ids to produce the new unique ids</param>
        public static void CombineUniqueIds(this GameObject self, Guid guid)
        {
            IUxrUniqueId[] components = self.GetComponentsInChildren<IUxrUniqueId>(true);
            components.ForEach(c => c.CombineUniqueId(guid, false));
        }

        /// <summary>
        ///     Checks if the given GameObject is the root GameObject inside a prefab.
        /// </summary>
        /// <param name="self">GameObject to check</param>
        /// <returns>Whether the GameObject is the root GameObject inside a prefab</returns>
        public static bool IsPrefabRoot(this GameObject self)
        {
#if UNITY_EDITOR
            if (PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetCurrentPrefabStage().prefabContentsRoot == self)
            {
                return true;
            }
#endif

            return self.scene.name == null && self.transform.parent == null;
        }

        /// <summary>
        ///     Checks if the given GameObject is located inside a prefab.
        /// </summary>
        /// <param name="self">GameObject to check</param>
        /// <returns>Whether the GameObject is located inside a prefab</returns>
        public static bool IsInPrefab(this GameObject self)
        {
#if UNITY_EDITOR

            if (PrefabStageUtility.GetCurrentPrefabStage() != null && PrefabStageUtility.GetCurrentPrefabStage().IsPartOfPrefabContents(self))
            {
                return true;
            }

            if (PrefabUtility.IsPartOfPrefabAsset(self))
            {
                return true;
            }

            // Preview scene (e.g., LoadPrefabContents): the object is inside a prefab being processed,
            // but IsPartOfPrefabAsset returns false because it's loaded as an instance.
            if (self.scene.IsValid() && EditorSceneManager.IsPreviewScene(self.scene))
            {
                return true;
            }

            return false;
#else
            return self.scene.name == null;
#endif
        }

#if UNITY_EDITOR

        /// <summary>
        ///     Gets the GUID and asset path of the owning prefab for a given <see cref="GameObject" />.
        /// </summary>
        /// <remarks>
        ///     This method resolves the prefab context using the following rules:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 If the object is inside <b>Prefab Mode</b>, it returns the asset path of the prefab currently being
        ///                 edited.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the object is part of a <b>prefab asset</b>, it returns the asset path of the prefab root.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 If the object is part of a <b>prefab instance in a scene</b>, it returns the asset path of the
        ///                 <b>outermost prefab instance</b> (the owning/top-level prefab, not nested prefabs).
        ///             </description>
        ///         </item>
        ///     </list>
        /// </remarks>
        /// <param name="self">The <see cref="GameObject" /> to query.</param>
        /// <param name="prefabGuid">The GUID of the resolved prefab asset, or <c>null</c> if not found.</param>
        /// <param name="assetPath">The asset path of the resolved prefab, or an empty string if not found.</param>
        /// <returns>
        ///     <c>true</c> if a valid prefab asset path and GUID were resolved; otherwise, <c>false</c>.
        /// </returns>
        public static bool GetPrefabGuid(this GameObject self, out string prefabGuid, out string assetPath)
        {
            prefabGuid = null;
            assetPath  = string.Empty;

            if (self == null)
            {
                return false;
            }

            // 1) Object inside Prefab Mode -> return the prefab currently being edited
            PrefabStage stage = PrefabStageUtility.GetPrefabStage(self);
            if (stage != null && stage.IsPartOfPrefabContents(self))
            {
                assetPath = stage.assetPath;
            }
            // 2) Object inside a prefab asset (Project asset context)
            else if (PrefabUtility.IsPartOfPrefabAsset(self))
            {
                assetPath = AssetDatabase.GetAssetPath(self.transform.root.gameObject);
            }
            // 3) Object inside a prefab instance in a normal scene -> return owning top-level prefab
            else if (PrefabUtility.IsPartOfNonAssetPrefabInstance(self) && self.scene.IsValid())
            {
                GameObject instanceRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(self);
                GameObject rootPrefab = instanceRoot != null
                                            ? PrefabUtility.GetCorrespondingObjectFromSource(instanceRoot)
                                            : null;

                assetPath = rootPrefab != null ? AssetDatabase.GetAssetPath(rootPrefab) : string.Empty;
            }

            if (string.IsNullOrEmpty(assetPath))
            {
                return false;
            }

            string guid = AssetDatabase.GUIDFromAssetPath(assetPath).ToString();

            if (string.IsNullOrEmpty(guid) || guid.All(c => c == '0'))
            {
                return false;
            }

            prefabGuid = guid;
            return true;
        }

#endif

        /// <summary>
        ///     Checks whether the given GameObject is dynamic. Since <see cref="GameObject.isStatic" /> doesn't work at runtime
        ///     due to the static flags being editor-only, a workaround is required to try to find out if an object is dynamic or
        ///     not.
        /// </summary>
        /// <param name="self">GameObject to check</param>
        /// <returns>Whether the object appears to be dynamic</returns>
        public static bool IsDynamic(this GameObject self)
        {
            return self.GetComponentInParent<Rigidbody>()           != null ||
                   self.GetComponentInParent<SkinnedMeshRenderer>() != null ||
                   self.GetComponentInParent<UxrAvatar>()           != null ||
                   self.GetComponentInParent<UxrGrabbableObject>()  != null;
        }

        /// <summary>
        ///     Gets the Component of a given type. If it doesn't exist, it is added to the GameObject.
        /// </summary>
        /// <param name="self">Target GameObject where the component will be looked for and added to if it doesn't exist</param>
        /// <typeparam name="T">Component type to get or add</typeparam>
        /// <returns>Existing component or newly added if it didn't exist before</returns>
        public static T GetOrAddComponent<T>(this GameObject self) where T : Component
        {
            T component = self.GetComponent<T>();

            if (component == null)
            {
#if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    component = self.AddComponent<T>();
                }
                else
                {
                    component = Undo.AddComponent<T>(self);
                }
#else
                component = self.AddComponent<T>();
#endif
            }

            return component;
        }

        /// <summary>
        ///     Creates a new GameObject in the exact same position as the given one and parents it.
        /// </summary>
        /// <param name="parent">
        ///     GameObject to parent the new object to and also place it at the same position
        ///     and with the same orientation
        /// </param>
        /// <param name="newGameObjectName">Name for the new GameObject</param>
        /// <returns>New created GameObject</returns>
        public static GameObject CreateGameObjectAndParentSameTransform(GameObject parent, string newGameObjectName)
        {
            GameObject newGameObject = new GameObject(newGameObjectName);
            newGameObject.transform.parent        = parent.transform;
            newGameObject.transform.localPosition = Vector3.zero;
            newGameObject.transform.localRotation = Quaternion.identity;
            return newGameObject;
        }

        /// <summary>
        ///     Computes the geometric center of the given GameObject based on all the MeshRenderers in the hierarchy.
        /// </summary>
        /// <param name="self">GameObject to compute the geometric center of</param>
        /// <returns>Geometric center</returns>
        public static Vector3 GetGeometricCenter(this GameObject self)
        {
            IEnumerable<MeshRenderer> meshRenderers = self.GetComponentsInChildren<MeshRenderer>().Where(r => !r.hideFlags.HasFlag(HideFlags.HideInHierarchy) && r.enabled);
            bool                      initialized   = false;
            Vector3                   min           = Vector3.zero;
            Vector3                   max           = Vector3.zero;

            if (!meshRenderers.Any())
            {
                return self.transform.position;
            }

            foreach (MeshRenderer renderer in meshRenderers)
            {
                if (!initialized)
                {
                    initialized = true;
                    min         = renderer.bounds.min;
                    max         = renderer.bounds.max;
                }
                else
                {
                    min = Vector3.Min(min, renderer.bounds.min);
                    max = Vector3.Max(max, renderer.bounds.max);
                }
            }

            return (min + max) * 0.5f;
        }

        /// <summary>
        ///     Calculates the <see cref="GameObject" /> <see cref="Bounds" />. The bounds are the <see cref="Renderer" />'s bounds
        ///     if there is one in the GameObject. Otherwise it will encapsulate all renderers found in the children.
        ///     If <paramref name="forceRecurseIntoChildren" /> is true, it will also encapsulate all renderers found in
        ///     the children no matter if the GameObject has a Renderer component or not.
        /// </summary>
        /// <param name="self">The GameObject whose <see cref="Bounds" /> to get</param>
        /// <param name="forceRecurseIntoChildren">
        ///     Whether to also encapsulate all renderers found in the children no matter if the
        ///     GameObject has a Renderer component or not
        /// </param>
        /// <returns>
        ///     <see cref="Bounds" /> in world-space.
        /// </returns>
        public static Bounds GetBounds(this GameObject self, bool forceRecurseIntoChildren)
        {
            Renderer renderer = self.GetComponent<Renderer>();

            if (renderer != null && !forceRecurseIntoChildren)
            {
                if (renderer.enabled)
                {
                    return renderer.bounds;
                }

                return new Bounds(self.transform.position, Vector3.zero);
            }

            List<Renderer> renderers = self.GetComponentsInChildren<Renderer>().Where(r => !r.hideFlags.HasFlag(HideFlags.HideInHierarchy) && r.enabled).ToList();

            if (!renderers.Any())
            {
                return new Bounds(self.transform.position, Vector3.zero);
            }

            Vector3 min = Vector3Ext.Min(renderers.Select(r => r.bounds.min).ToList());
            Vector3 max = Vector3Ext.Max(renderers.Select(r => r.bounds.max).ToList());

            return new Bounds((max + min) * 0.5f, max - min);
        }

        /// <summary>
        ///     Calculates the <see cref="GameObject" /> <see cref="Bounds" /> in local space. The bounds are the
        ///     <see cref="Renderer" />'s bounds if there is one in the GameObject. Otherwise it will encapsulate all renderers
        ///     found in the children.
        ///     If <paramref name="forceRecurseIntoChildren" /> is true, it will also encapsulate all renderers found in
        ///     the children no matter if the GameObject has a Renderer component or not.
        /// </summary>
        /// <param name="self">The GameObject whose local <see cref="Bounds" /> to get</param>
        /// <param name="forceRecurseIntoChildren">
        ///     Whether to also encapsulate all renderers found in the children no matter if the
        ///     GameObject has a Renderer component or not
        /// </param>
        /// <returns>
        ///     Local <see cref="Bounds" />.
        /// </returns>
        public static Bounds GetLocalBounds(this GameObject self, bool forceRecurseIntoChildren)
        {
            Renderer renderer = self.GetComponent<Renderer>();

            if (renderer != null && !forceRecurseIntoChildren)
            {
                if (renderer.enabled)
                {
                    return renderer.localBounds;
                }

                return new Bounds();
            }

            List<Renderer> renderers = self.GetComponentsInChildren<Renderer>().Where(r => !r.hideFlags.HasFlag(HideFlags.HideInHierarchy) && r.enabled).ToList();

            if (!renderers.Any())
            {
                return new Bounds();
            }

            List<Vector3> allMinMaxToLocal = renderers.Select(r => self.transform.InverseTransformPoint(r.transform.TransformPoint(r.localBounds.min))).Concat(renderers.Select(r => self.transform.InverseTransformPoint(r.transform.TransformPoint(r.localBounds.max)))).ToList();
            Vector3       min              = Vector3Ext.Min(allMinMaxToLocal);
            Vector3       max              = Vector3Ext.Max(allMinMaxToLocal);

            return new Bounds((max + min) * 0.5f, max - min);
        }

        /// <summary>
        ///     Sets the layer of a GameObject and all its children.
        /// </summary>
        /// <param name="self">The root GameObject from where to start</param>
        /// <param name="layer">The layer value to assign</param>
        /// <param name="ignoreLayers">Optional array of layers to ignore</param>
        public static void SetLayerRecursively(this GameObject self, int layer, params int[] ignoreLayers)
        {
            if (self != null)
            {
                Transform selfTransform     = self.transform;
                bool      shouldIgnoreLayer = ignoreLayers != null && ignoreLayers.Contains(self.gameObject.layer);

                if (!shouldIgnoreLayer)
                {
                    self.gameObject.layer = layer;
                }

                for (int i = 0; i < selfTransform.childCount; ++i)
                {
                    selfTransform.GetChild(i).gameObject.SetLayerRecursively(layer);
                }
            }
        }

        /// <summary>
        ///     Checks whether the given GameObject's layer is present in a layer mask.
        /// </summary>
        /// <param name="self">The GameObject whose layer to check</param>
        /// <param name="layerMask">The layer mask to check against</param>
        /// <returns>Whether the GameObject's layer is present in the layer mask</returns>
        public static bool IsInLayerMask(this GameObject self, LayerMask layerMask)
        {
            return (1 << self.layer & layerMask.value) != 0;
        }

        /// <summary>
        ///     Gets the topmost <see cref="UxrCanvas" /> upwards in the hierarchy if it exists.
        /// </summary>
        /// <param name="self">The GameObject whose parents to look for</param>
        /// <returns>The topmost <see cref="UxrCanvas" /> component upwards in the hierarchy or null if it doesn't exists</returns>
        public static UxrCanvas GetTopmostCanvas(this GameObject self)
        {
            UxrCanvas[] canvases = self.GetComponentsInParent<UxrCanvas>();
            return ComponentExt.GetCommonRootComponentFromSet(canvases);
        }

        #endregion
    }
}