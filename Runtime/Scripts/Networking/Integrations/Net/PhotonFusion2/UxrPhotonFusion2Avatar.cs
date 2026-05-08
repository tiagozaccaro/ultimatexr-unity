// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonFusion2Avatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
using System;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.StateSync;
using Fusion;
using UltimateXR.Locomotion;
#endif

namespace UltimateXR.Networking.Integrations.Net.PhotonFusion2
{
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK

    public class UxrPhotonFusion2Avatar : NetworkBehaviour, IUxrNetworkAvatar
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [Attributes.ReadOnly] private GameObject _networkCamera;
        [SerializeField] [Attributes.ReadOnly] private GameObject _networkHandLeft;
        [SerializeField] [Attributes.ReadOnly] private GameObject _networkHandRight;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets the last local avatar's position for movement. Used because the NetworkTransform on the root might override
        ///     the transform.
        /// </summary>
        public static Vector3 LocalAvatarWorldPosition { get; private set; }

        /// <summary>
        ///     Gets the last local avatar's rotation for movement. Used because the NetworkTransform on the root might override
        ///     the transform.
        /// </summary>
        public static Quaternion LocalAvatarWorldRotation { get; private set; }

        /// <summary>
        ///     Gets whether the last local avatar movement was set by smooth locomotion or not. The server will use this to know
        ///     whether the movement should be applied as a teleport (false) or as a smooth movement (true).
        /// </summary>
        public static bool LocalAvatarPosDataIsSmoothLocomotion { get; private set; }

        #endregion

        #region Implicit IUxrNetworkAvatar

        /// <inheritdoc />
        public bool IsInitialized => _implementer.IsInitialized;

        /// <inheritdoc />
        public bool IsLocal => _implementer.IsLocal;

        /// <inheritdoc />
        public UxrAvatar Avatar => _implementer.Avatar;

        /// <inheritdoc />
        public bool UsesDummyNetworkTransforms => true;

        /// <inheritdoc />
        public string AvatarName
        {
            get => _implementer.AvatarName;
            set => _implementer.AvatarName = value;
        }

        /// <inheritdoc />
        public event Action AvatarSpawned
        {
            add => _implementer.AvatarSpawned += value;
            remove => _implementer.AvatarSpawned -= value;
        }

        /// <inheritdoc />
        public event Action AvatarDespawned
        {
            add => _implementer.AvatarDespawned += value;
            remove => _implementer.AvatarDespawned -= value;
        }

        /// <inheritdoc />
        public bool ChangeParent(Transform child, Transform parent)
        {
            // TODO: Check specific network implementation.

            if (child == null)
            {
                return false;
            }

            child.SetParent(parent);
            return true;
        }

        #endregion

        #region Public Overrides NetworkBehaviour

        /// <inheritdoc />
        public override void Spawned()
        {
            base.Spawned();

            _implementer.HandleSpawn(Object.HasInputAuthority,
                                     UxrNetworkManager.IsServer || Runner.IsSharedModeMasterClient,
                                     Object.Id.ToString(),
                                     $"Player {Object.InputAuthority.PlayerId} ({(Object.HasInputAuthority ? "Local" : "External")})",
                                     $"{nameof(UxrPhotonFusion2Avatar)}.{nameof(Spawned)}",
                                     data => RPC_NewAvatarJoined(data),
                                     UxrManager_ComponentStateChanged);

            // We use dummy network transforms:
            // Read the avatar state from the dummy network transforms if it's an external avatar. Never write to them in client-server mode because only the server can do it. FixedUpdateNetwork() on the server will take care of that.
            // In shared mode we write to the network transforms directly.
            _implementer.SetupDummyNetworkTransforms(_networkCamera, _networkHandLeft, _networkHandRight, true, Runner.GameMode == GameMode.Shared);

            if (Object.HasInputAuthority)
            {
                UxrPhotonFusion2Network.ResetLastAvatarInput();
                LocalAvatarWorldPosition             = _implementer.Avatar.transform.position;
                LocalAvatarWorldRotation             = _implementer.Avatar.transform.rotation;
                LocalAvatarPosDataIsSmoothLocomotion = false;
            }
        }

        /// <inheritdoc />
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            _implementer.HandleDespawn($"{nameof(UxrPhotonFusion2Avatar)}.{nameof(Despawned)}");
        }

        /// <inheritdoc />
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            if (Runner.GameMode == GameMode.Shared)
            {
                // In shared mode we write to the network transforms directly using _implementer.
                return;
            }
            
            // In client-server mode only the server can modify the transforms based on client input.

            if (Object.HasStateAuthority && GetInput(out UxrPhotonFusion2AvatarInput input))
            {
                if (_implementer.Avatar != null)
                {
                    if (input.IsSmooth)
                    {
                        _implementer.Avatar.transform.SetPositionAndRotation(input.AvatarPosition, input.AvatarRotation);
                    }
                    else
                    {
                        _networkTransformRoot.Teleport(input.AvatarPosition, input.AvatarRotation);
                    }
                }

                if (_networkTransformCamera)
                {
                    if (input.IsSmooth)
                    {
                        _networkTransformCamera.transform.SetPositionAndRotation(Avatar.transform.TransformPoint(input.LocalAvatarCameraPosition), Avatar.transform.rotation * input.LocalAvatarCameraRotation);
                    }
                    else
                    {
                        _networkTransformCamera.Teleport(Avatar.transform.TransformPoint(input.LocalAvatarCameraPosition), Avatar.transform.rotation * input.LocalAvatarCameraRotation);
                    }
                }

                if (_networkTransformLeftHand)
                {
                    if (input.IsSmooth)
                    {
                        _networkTransformLeftHand.transform.SetPositionAndRotation(Avatar.transform.TransformPoint(input.LocalAvatarLeftHandPosition), Avatar.transform.rotation * input.LocalAvatarLeftHandRotation);
                    }
                    else
                    {
                        _networkTransformLeftHand.Teleport(Avatar.transform.TransformPoint(input.LocalAvatarLeftHandPosition), Avatar.transform.rotation * input.LocalAvatarLeftHandRotation);
                    }
                }

                if (_networkTransformRightHand)
                {
                    if (input.IsSmooth)
                    {
                        _networkTransformRightHand.transform.SetPositionAndRotation(Avatar.transform.TransformPoint(input.LocalAvatarRightHandPosition), Avatar.transform.rotation * input.LocalAvatarRightHandRotation);
                    }
                    else
                    {
                        _networkTransformRightHand.Teleport(Avatar.transform.TransformPoint(input.LocalAvatarRightHandPosition), Avatar.transform.rotation * input.LocalAvatarRightHandRotation);
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets up the dummy network transforms that hang from the avatar. These network transforms will be used
        ///     to propagate the transform data, since Fusion 2 only can modify network transforms on the server-side.
        /// </summary>
        /// <param name="networkCamera">Dummy GameObject that will synchronize the camera transform</param>
        /// <param name="networkHandLeft">Dummy GameObject that will synchronize the left hand</param>
        /// <param name="networkHandRight">Dummy GameObject that will synchronize the right hand</param>
        public void SetupDummyNetworkTransforms(GameObject networkCamera, GameObject networkHandLeft, GameObject networkHandRight)
        {
            _networkCamera    = networkCamera;
            _networkHandLeft  = networkHandLeft;
            _networkHandRight = networkHandRight;
        }

        /// <summary>
        ///     Sent to the client that joined to sync to the current state.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        public void LoadGlobalState(byte[] serializedStateData)
        {
            _implementer.LoadSyncOnJoinState(serializedStateData);
        }

        /// <summary>
        ///     Sent to other clients to sync the state of a new avatar that joined.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        public void LoadAvatarState(byte[] serializedStateData)
        {
            _implementer.LoadJoinedAvatarState(serializedStateData);
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the network avatar implementer.
        /// </summary>
        private void Awake()
        {
            _implementer               = new UxrNetworkAvatarImplementer(this);
            _networkTransformRoot      = GetComponent<NetworkTransform>();
            _networkTransformCamera    = _networkCamera?.GetComponent<NetworkTransform>();
            _networkTransformLeftHand  = _networkHandLeft?.GetComponent<NetworkTransform>();
            _networkTransformRightHand = _networkHandRight?.GetComponent<NetworkTransform>();
        }

        /// <summary>
        ///     Subscribes to events.
        /// </summary>
        private void OnEnable()
        {
            UxrManager.StageUpdating    += UxrManager_StageUpdating;
            UxrManager.StageUpdated     += UxrManager_StageUpdated;
            UxrAvatar.GlobalAvatarMoved += UxrAvatar_GlobalAvatarMoved;
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        private void OnDisable()
        {
            UxrManager.StageUpdating    -= UxrManager_StageUpdating;
            UxrManager.StageUpdated     -= UxrManager_StageUpdated;
            UxrAvatar.GlobalAvatarMoved -= UxrAvatar_GlobalAvatarMoved;
        }

        /// <summary>
        ///     Unity's Update() method. This will check for the correct setup.
        /// </summary>
        private void Update()
        {
            _implementer.ValidateDummyNetworkTransforms();
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Handles the event triggered when a global avatar movement occurs.
        /// </summary>
        /// <param name="sender">The source of the event, typically the avatar that was moved.</param>
        /// <param name="e">Contains the event data specific to the avatar movement.</param>
        private void UxrAvatar_GlobalAvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            if (e.Avatar == Avatar && _implementer.IsLocal)
            {
                LocalAvatarWorldPosition             = e.NewPosition;
                LocalAvatarWorldRotation             = e.NewRotation;
                LocalAvatarPosDataIsSmoothLocomotion = sender is UxrLocomotion locomotion && locomotion.IsSmoothLocomotion;
            }
        }

        /// <summary>
        ///     Called when <see cref="UxrManager" /> is about to enter an update stage during a frame.
        /// </summary>
        /// <param name="stage">The stage that is about to be updated</param>
        private void UxrManager_StageUpdating(UxrUpdateStage stage)
        {
            if (Runner.GameMode == GameMode.Shared)
            {
                return;
            }
            
            // Override the NetworkTransform's position for the local avatar to keep it in place.
            // We do it before the frame starts (usually update) and before the manipulation (late update).
            if (stage == UxrUpdateStage.Update || stage == UxrUpdateStage.Manipulation)
            {
                if (_implementer != null && _implementer.IsLocal)
                {
                    _implementer.Avatar.transform.position = LocalAvatarWorldPosition;
                    _implementer.Avatar.transform.rotation = LocalAvatarWorldRotation;
                }
            }
        }

        /// <summary>
        ///     Called when <see cref="UxrManager" /> finished an update stage during a frame.
        /// </summary>
        /// <param name="stage">The stage that finished updating</param>
        private void UxrManager_StageUpdated(UxrUpdateStage stage)
        {
            _implementer.HandleStageUpdated(stage);
        }

        /// <summary>
        ///     Called when a component in UltimateXR had a state change.
        /// </summary>
        /// <param name="component">Component</param>
        /// <param name="eventArgs">Event parameters</param>
        private void UxrManager_ComponentStateChanged(IUxrStateSync component, UxrSyncEventArgs eventArgs)
        {
            _implementer.HandleComponentStateChanged(component, eventArgs, Object.HasInputAuthority, RPC_ComponentStateChanged);
        }

        /// <summary>
        ///     RPC from client to server/master client to request the current global state upon joining.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="info">Filled by Photon with RPC information</param>
        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        private void RPC_NewAvatarJoined(byte[] avatarState, RpcInfo info = default)
        {
            // Only server or master client in shared mode should answer.
            bool shouldAnswer = Runner.GameMode == GameMode.Shared ? Runner.IsSharedModeMasterClient : Object.HasStateAuthority;

            if (!shouldAnswer)
            {
                return;
            }
            
            byte[] serializedState = _implementer.HandleNewAvatarJoined(avatarState, info.Source.ToString(), gameObject);

            // Send global state to new user.
            PhotonFusionSingleton.BroadcastReliableData(UxrPhotonReliableMsgType.LoadGlobalState, serializedState, null, info.Source.AsIndex);

            // Broadcast initial state of new avatar.
            PhotonFusionSingleton.BroadcastReliableData(UxrPhotonReliableMsgType.LoadAvatarState, avatarState, info.Source.AsIndex, null);
        }

        /// <summary>
        ///     RPC to propagate state change events to all other clients.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [Rpc(RpcSources.All, RpcTargets.All, InvokeLocal = false)]
        private void RPC_ComponentStateChanged(byte[] serializedEventData)
        {
            _implementer.LoadRemoteComponentStateChanged(serializedEventData);
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Gets the Photon Fusion manager.
        /// </summary>
        private static UxrPhotonFusion2Network PhotonFusionSingleton
        {
            get
            {
                if (s_photonFusionSingleton == null && UxrNetworkManager.HasInstance)
                {
                    s_photonFusionSingleton = UxrNetworkManager.Instance.GetComponent<UxrPhotonFusion2Network>();
                }

                return s_photonFusionSingleton;
            }
        }

        private static UxrPhotonFusion2Network s_photonFusionSingleton;

        private UxrNetworkAvatarImplementer _implementer;
        private UxrPhotonFusion2AvatarInput _lastAvatarInput;
        private NetworkTransform            _networkTransformRoot;
        private NetworkTransform            _networkTransformCamera;
        private NetworkTransform            _networkTransformLeftHand;
        private NetworkTransform            _networkTransformRightHand;

        #endregion
    }
#else
    public class UxrPhotonFusion2Avatar : MonoBehaviour
    {
    }
#endif
}