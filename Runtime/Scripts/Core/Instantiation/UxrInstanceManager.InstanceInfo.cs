// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrInstanceManager.InstanceInfo.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core.Serialization;
using UltimateXR.Core.Unique;
using UltimateXR.Exceptions;
using UltimateXR.Extensions.Unity;
using UnityEngine;

namespace UltimateXR.Core.Instantiation
{
    public partial class UxrInstanceManager
    {
        #region Private Types & Data

        /// <summary>
        ///     Stores instantiation information.
        /// </summary>
        private class InstanceInfo : IUxrSerializable
        {
            #region Public Types & Data

            /// <summary>
            ///     Gets the Id of the prefab that was instantiated. Null if it's an empty GameObject.
            /// </summary>
            public string PrefabId => _prefabId;

            /// <summary>
            ///     Gets the name of the instance.
            /// </summary>
            public string Name => _name;

            /// <summary>
            ///     Gets the Guid of the parent if the object was parented to any.
            /// </summary>
            public Guid ParentUniqueId => _parentGuid;

            /// <summary>
            ///     Gets the position. It will contain the relative position to the parent if parented.
            /// </summary>
            public Vector3 Position => _position;

            /// <summary>
            ///     Gets the rotation. It will contain relative rotation to the parent if parented.
            /// </summary>
            public Quaternion Rotation => _rotation;

            /// <summary>
            ///     Gets the local scale.
            /// </summary>
            public Vector3 Scale => _scale;
            
            /// <summary>
            ///     Gets whether the instance was created using <see cref="UxrInstanceManager.NotifyNetworkSpawn"/>.
            /// </summary>
            public bool IsNetworkSpawn => _isNetworkSpawn;

            /// <summary>
            ///     Gets the order in which the instance was created. This can be used when deserializing
            ///     to determine the order in which instances should be created. This way parenting will
            ///     be resolved in the correct order.
            /// </summary>
            public int Order => _order;

            /// <summary>
            ///     Gets or sets the parent if the object was parented to any.
            /// </summary>
            public IUxrUniqueId Parent
            {
                get => _parent;
                set
                {
                    _parent     = value;
                    _parentGuid = value?.UniqueId ?? Guid.Empty;
                }
            }

            #endregion

            #region Constructors & Finalizer

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="instance">The instance</param>
            /// <param name="prefabId">
            ///     The id of the prefab that was instantiated or null if the instance was created using
            ///     <see cref="UxrInstanceManager.InstantiateEmptyGameObject" />.
            /// </param>
            /// <param name="isNetworkSpawn">Whether the instance is the result of using <see cref="UxrInstanceManager.NotifyNetworkSpawn"/></param>
            public InstanceInfo(IUxrUniqueId instance, string prefabId, bool isNetworkSpawn)
            {
                _prefabId       = prefabId;
                _name           = instance.GameObject.name;
                _parent         = instance.Transform.parent != null ? instance.Transform.parent.GetTrackingUniqueIdComponent() : null;
                _parentGuid     = _parent?.UniqueId ?? Guid.Empty;
                _position       = _parent != null ? instance.Transform.localPosition : instance.Transform.position;
                _rotation       = _parent != null ? instance.Transform.localRotation : instance.Transform.rotation;
                _scale          = instance.Transform.localScale;
                _isNetworkSpawn = isNetworkSpawn;
                _order          = s_order++;
            }

            /// <summary>
            ///     Default constructor required for serialization.
            /// </summary>
            private InstanceInfo()
            {
            }

            #endregion

            #region Implicit IUxrSerializable

            /// <inheritdoc />
            public int SerializationVersion => 1;

            /// <inheritdoc />
            public void Serialize(IUxrSerializer serializer, int serializationVersion)
            {
                serializer.Serialize(ref _prefabId);
                serializer.Serialize(ref _name);

                if (serializationVersion == 0)
                {
                    // Version 0 serialized the parent as an IUxrUniqueId, which means that when deserializing it will try to get the component
                    // from the scene and throw an exception when not found. This gives errors if a child is parented to another object that was instantiated.
                    // For improved backwards compatibility we can catch the exception and store the unique ID that wasn't found, which will allow us to
                    // solve it in a second pass just like the version >= 1 does.

                    try
                    {
                        serializer.SerializeUniqueIdComponent(ref _parent);
                    }
                    catch (UxrComponentNotFoundException e)
                    {
                        if (serializer.IsReading)
                        {
                            _parentGuid = e.UniqueId;
                        }
                    }
                }
                else
                {
                    // Starting from version 1, we serialize the parent using the ID so that we can solve the parenting on a second, safer pass. 
                    serializer.Serialize(ref _parentGuid);
                }

                serializer.Serialize(ref _position);
                serializer.Serialize(ref _rotation);
                serializer.Serialize(ref _scale);

                if (serializationVersion >= VersionWithNetworkSpawnAndInstantiationOrder)
                {
                    serializer.Serialize(ref _isNetworkSpawn);
                    serializer.Serialize(ref _order);
                }
            }

            #endregion

            #region Public Methods

            /// <summary>
            ///     Updates the info using the current object data.
            /// </summary>
            /// <param name="instance">The GameObject to update the information of</param>
            public void UpdateInfoUsingObject(GameObject instance)
            {
                Transform    instanceTransform = instance.transform;
                IUxrUniqueId parent            = instanceTransform.parent != null ? instanceTransform.parent.GetTrackingUniqueIdComponent() : null;
                Transform    parentTransform   = parent?.Transform;

                if (parentTransform != null)
                {
                    // Use relative parent data
                    _parent     = parent;
                    _parentGuid = parent.UniqueId;
                    _position   = instanceTransform.localPosition;
                    _rotation   = instanceTransform.localRotation;
                }
                else
                {
                    // Use world data
                    _parent     = null;
                    _parentGuid = Guid.Empty;
                    _position   = instanceTransform.position;
                    _rotation   = instanceTransform.rotation;
                }

                _scale = instanceTransform.localScale;
            }

            /// <summary>
            ///     Updates the object using the current information.
            /// </summary>
            /// <param name="instance">The GameObject to update</param>
            public void UpdateObjectUsingInfo(GameObject instance)
            {
                if (ParentUniqueId != Guid.Empty && UxrUniqueIdImplementer.TryGetComponentById(ParentUniqueId, out IUxrUniqueId parentComponent))
                {
                    Parent = parentComponent;
                }

                Transform instanceTransform = instance.transform;
                Transform parentTransform   = Parent?.Transform;

                if (parentTransform != null)
                {
                    // Use relative parent data

                    if (instanceTransform.parent != parentTransform)
                    {
                        instanceTransform.SetParent(parentTransform);
                    }

                    TransformExt.SetLocalPositionAndRotation(instanceTransform, Position, Rotation);
                }
                else
                {
                    // Use world

                    if (instanceTransform.parent != null)
                    {
                        instanceTransform.SetParent(null);
                    }

                    instanceTransform.SetPositionAndRotation(Position, Rotation);
                }

                instanceTransform.localScale = Scale;
            }

            #endregion

            #region Internal Methods

            /// <summary>
            ///     Resets the instantiation order counter.
            /// </summary>
            internal static void ResetInstantiationOrder()
            {
                s_order = 0;
            }

            #endregion

            #region Private Types & Data

            private const int VersionWithNetworkSpawnAndInstantiationOrder = 1;

            private static int s_order;

            private string       _prefabId;
            private string       _name;
            private IUxrUniqueId _parent;
            private Vector3      _position;
            private Guid         _parentGuid;
            private Quaternion   _rotation;
            private Vector3      _scale;
            private bool         _isNetworkSpawn;
            private int          _order;

            #endregion
        }

        #endregion
    }
}