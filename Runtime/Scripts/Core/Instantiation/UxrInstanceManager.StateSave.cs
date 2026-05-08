// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrInstanceManager.StateSave.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSave;
using UltimateXR.Core.Unique;
using UltimateXR.Extensions.System.Collections;
using UltimateXR.Networking;
using UnityEngine;

namespace UltimateXR.Core.Instantiation
{
    public partial class UxrInstanceManager
    {
        #region Protected Overrides UxrAbstractSingleton

        /// <inheritdoc />
        protected override int SerializationOrder => UxrConstants.Serialization.SerializationOrderInstanceManager;

        #endregion

        #region Protected Overrides UxrComponent

        /// <inheritdoc />
        protected override void SerializeState(bool isReading, UxrStateSaveLevel level, UxrStateSaveOptions options)
        {
            base.SerializeState(isReading, level, options);

            // Version

            SerializeStateVersion(level, options, StateSerializationVersion, out int effectiveVersion);

            // Individual instantiations are already handled through events.
            // We save all generated instances in higher save levels.

            if (level > UxrStateSaveLevel.ChangesSincePreviousSave)
            {
                // When writing, update instance info first

                if (!isReading)
                {
                    foreach (KeyValuePair<Guid, InstanceInfo> pair in _currentInstancedPrefabs)
                    {
                        if (_currentInstances.TryGetValue(pair.Key, out GameObject instance) && instance != null)
                        {
                            /*                             
                             We don't do this anymore at the Complete level. TODO: Check if we need to remove it on other levels too.

                             If an object changes the parent used for instantiation, it would be saved here.
                             If this parent is itself another instantiation, after the object, when deserializing the parent would not be available causing errors.

                             If re-parenting after object instantiation needs to work, override RequiresTransformSerialization() on the component. This will take care
                             of setting the correct parent after UxrInstanceManager has deserialized/instantiated the object.                             
                             */

                            if (level != UxrStateSaveLevel.Complete)
                            {
                                pair.Value.UpdateInfoUsingObject(instance);
                            }
                        }
                    }
                }

                // We don't want to compare dictionaries, we save the instantiation info always by using null as the name to avoid overhead.
                SerializeStateValue(level, options, null, ref _currentInstancedPrefabs);

                if (isReading)
                {
                    if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
                    {
                        Debug.Log($"{UxrConstants.CoreModule} {nameof(UxrInstanceManager)}.{nameof(SerializeState)} when loading has {(_currentInstancedPrefabs == null ? "0" : _currentInstancedPrefabs.Count.ToString())} instances.");
                    }

                    // When reading, we need to update the scene:
                    //   -Destroy instances that do not exist anymore in the deserialized data
                    //   -Create instances that are not yet in the scene, from the deserialized data

                    // First, destroy existing instances in the scene that are not present in the deserialized list anymore:
                    // TODO: Check what happens if we delete an object whose child is an instance that needs re-parenting (because the parent changed) first.

                    List<Guid>                                       toRemove = null;
                    List<(GameObject go, InstanceInfo instanceInfo)> toUpdate = null;

                    foreach (KeyValuePair<Guid, GameObject> pair in _currentInstances)
                    {
                        if (pair.Value == null)
                        {
                            continue;
                        }

                        if (_currentInstancedPrefabs == null || !_currentInstancedPrefabs.ContainsKey(pair.Key))
                        {
                            // Before destroying the GameObject, unregister the components ahead of time too in case they are going to be re-created.
                            // Also, unsubscribe from Destroying to prevent the deferred Destroy() from triggering Component_Destroying,
                            // which would remove newly created entries from the dictionaries when the same CombineIdSource is reused.
                            IUxrUniqueId[] components = pair.Value.GetComponentsInChildren<IUxrUniqueId>(true);
                            components.ForEach(c =>
                            {
                                c.Destroying -= Component_Destroying;
                                c.Unregister();
                            });

                            Destroy(pair.Value);

                            toRemove ??= new List<Guid>();
                            toRemove.Add(pair.Key);
                        }
                        else if (_currentInstancedPrefabs != null && _currentInstancedPrefabs.TryGetValue(pair.Key, out InstanceInfo info))
                        {
                            // If the object is still present, mark it for update to the current state
                            toUpdate ??= new List<(GameObject go, InstanceInfo instanceInfo)>();
                            toUpdate.Add((pair.Value, info));
                        }
                    }

                    if (toRemove != null)
                    {
                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
                        {
                            Debug.Log($"{UxrConstants.CoreModule} {nameof(UxrInstanceManager)}.{nameof(SerializeState)}: Removing {toRemove.Count} instances.");
                        }
                        
                        foreach (Guid guid in toRemove)
                        {
                            _currentInstances.Remove(guid);
                        }
                    }

                    // Now instantiate prefabs that are present in the deserialized list but not in the scene:

                    List<(Guid CombineGuid, InstanceInfo Info)> toAdd = null;

                    if (_currentInstancedPrefabs != null)
                    {
                        foreach (KeyValuePair<Guid, InstanceInfo> pair in _currentInstancedPrefabs)
                        {
                            if (!_currentInstances.ContainsKey(pair.Key))
                            {
                                toAdd ??= new List<(Guid, InstanceInfo)>();
                                toAdd.Add((pair.Key, pair.Value));
                            }
                        }
                    }

                    if (toAdd != null)
                    {
                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
                        {
                            Debug.Log($"{UxrConstants.CoreModule} {nameof(UxrInstanceManager)}.{nameof(SerializeState)}: Creating {toAdd.Count} instances.");
                        }

                        // Sort by order so that parenting is resolved in the correct order.
                        toAdd.Sort((a, b) => a.Info.Order.CompareTo(b.Info.Order));

                        int parentsTotalCount = 0;
                        int parentsSolvedCount = 0;

                        foreach ((Guid CombineGuid, InstanceInfo Info) pair in toAdd)
                        {
                            // Solve parent first

                            if (pair.Info.ParentUniqueId != Guid.Empty)
                            {
                                parentsTotalCount++;

                                if (UxrUniqueIdImplementer.TryGetComponentById(pair.Info.ParentUniqueId, out IUxrUniqueId parentComponent))
                                {
                                    pair.Info.Parent = parentComponent;
                                    parentsSolvedCount++;
                                }
                                else if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Warnings && UxrUniqueIdImplementer.TryGetUniqueIdComponentDebugInfo(pair.Info.ParentUniqueId, out string debugInfo))
                                {
                                    GameObject prefab = GetPrefab(pair.Info.PrefabId);
                                    string objectName = pair.Info.Name ?? "Unknown";
                                    string prefabName = prefab == null ? "Unknown" : prefab.name;

                                    Debug.LogWarning($"{UxrConstants.CoreModule} {nameof(UxrInstanceManager)}.{nameof(SerializeState)}: Cannot resolve parent for instance {objectName}, prefab {prefabName}. Parent should be {debugInfo}.");
                                }
                            }

                            // Now create

                            Vector3    position = pair.Info.Parent != null ? pair.Info.Parent.Transform.TransformPoint(pair.Info.Position) : pair.Info.Position;
                            Quaternion rotation = pair.Info.Parent != null ? pair.Info.Parent.Transform.rotation * pair.Info.Rotation : pair.Info.Rotation;
                            Vector3    scale    = pair.Info.Scale;

                            if (!string.IsNullOrEmpty(pair.Info.PrefabId))
                            {
                                if (UxrNetworkManager.IsSessionActive && pair.Info.IsNetworkSpawn)
                                {
                                    // Only instantiate network spawns when we are not in a network session. For example, a replay.
                                }
                                else
                                {
                                    // Instantiate prefab. 
                                    GameObject newInstance = InstantiatePrefabInternal(pair.Info.PrefabId, pair.Info.Parent, position, rotation, 0, pair.CombineGuid);
                                    if (pair.Info.Name != null)
                                    {
                                        newInstance.name = pair.Info.Name;
                                    }
                                    newInstance.transform.localScale = scale;
                                    CheckNetworkSpawnPostprocess(newInstance);
                                }
                            }
                            else
                            {
                                // Empty GameObject
                                GameObject newObject = InstantiateEmptyGameObjectInternal(pair.Info.Name, pair.Info.Parent, position, rotation, 0, pair.CombineGuid);
                                newObject.transform.localScale = scale;
                            }
                        }

                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
                        {
                            Debug.Log($"{UxrConstants.CoreModule} {nameof(UxrInstanceManager)}.{nameof(SerializeState)}: Parents solved: {parentsSolvedCount}/{parentsTotalCount}.");
                        }
                    }

                    if (toUpdate != null)
                    {
                        foreach ((GameObject go, InstanceInfo instanceInfo) info in toUpdate)
                        {
                            info.instanceInfo.UpdateObjectUsingInfo(info.go);
                        }
                    }
                }
            }
        }

        #endregion

        #region Private Types & Data

        private const int StateSerializationVersion = 0;

        #endregion
    }
}