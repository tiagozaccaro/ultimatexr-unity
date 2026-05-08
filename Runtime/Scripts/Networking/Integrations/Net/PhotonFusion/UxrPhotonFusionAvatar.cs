// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonFusionAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
#if ULTIMATEXR_USE_PHOTONFUSION_SDK
using System;
using UltimateXR.Attributes;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.StateSync;
using Fusion;
#endif

namespace UltimateXR.Networking.Integrations.Net.PhotonFusion
{
#if ULTIMATEXR_USE_PHOTONFUSION_SDK

    [OrderAfter(typeof(NetworkTransform), typeof(NetworkRigidbody))]
    public class UxrPhotonFusionAvatar : NetworkBehaviour, IUxrNetworkAvatar
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [ReadOnly] private NetworkTransform _networkTransformRigAvatar;
        [SerializeField] [ReadOnly] private NetworkTransform _networkTransformRigCamera;
        [SerializeField] [ReadOnly] private NetworkTransform _networkTransformRigHandLeft;
        [SerializeField] [ReadOnly] private NetworkTransform _networkTransformRigHandRight;

        #endregion

        #region Implicit IUxrNetworkAvatar

        /// <inheritdoc />
        public bool IsInitialized => _implementer.IsInitialized;

        /// <inheritdoc />
        public bool IsLocal => _implementer.IsLocal;

        /// <inheritdoc />
        public UxrAvatar Avatar => _implementer.Avatar;

        /// <inheritdoc />
        public string AvatarName
        {
            get => _implementer.AvatarName;
            set => _implementer.AvatarName = value;
        }

        /// <inheritdoc />
        public bool UsesDummyNetworkTransforms => false;

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
                                     UxrNetworkManager.IsServer,
                                     Object.Id.ToString(),
                                     $"Player {Object.InputAuthority.PlayerId} ({(Object.HasInputAuthority ? "Local" : "External")})",
                                     $"{nameof(UxrPhotonFusionAvatar)}.{nameof(Spawned)}",
                                     sendNewAvatarJoined: data => RPC_NewAvatarJoined(data),
                                     componentStateChangedHandler: UxrManager_ComponentStateChanged);
        }

        /// <inheritdoc />
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            _implementer.HandleDespawn($"{nameof(UxrPhotonFusionAvatar)}.{nameof(Despawned)}");
        }

        /// <inheritdoc />
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            // update the rig at each network tick
            if (GetInput<UxrPhotonFusionNetwork.RigInput>(out var input))
            {
                _networkTransformRigAvatar.transform.position    = input.avatarPosition;
                _networkTransformRigAvatar.transform.rotation    = input.avatarRotation;
                _networkTransformRigAvatar.transform.localScale  = input.avatarScale;
                _networkTransformRigCamera.transform.position    = input.cameraPosition;
                _networkTransformRigCamera.transform.rotation    = input.cameraRotation;
                _networkTransformRigHandLeft.transform.position  = input.leftHandPosition;
                _networkTransformRigHandLeft.transform.rotation  = input.leftHandRotation;
                _networkTransformRigHandRight.transform.position = input.rightHandPosition;
                _networkTransformRigHandRight.transform.rotation = input.rightHandRotation;
            }
        }

        #endregion

        #region Public Overrides SimulationBehaviour

        /// <inheritdoc />
        public override void Render()
        {
            base.Render();

            if (Object.HasInputAuthority)
            {
                // Extrapolate for local user

                _networkTransformRigAvatar.InterpolationTarget.position   = _actualAvatarPosition;
                _networkTransformRigAvatar.InterpolationTarget.rotation   = _actualAvatarRotation;
                _networkTransformRigAvatar.InterpolationTarget.localScale = Avatar.transform.localScale;
                _networkTransformRigCamera.InterpolationTarget.position   = Avatar.CameraComponent.transform.position;
                _networkTransformRigCamera.InterpolationTarget.rotation   = Avatar.CameraComponent.transform.rotation;

                if (Avatar.FirstControllerTracking != null)
                {
                    _networkTransformRigHandLeft.InterpolationTarget.position  = Avatar.FirstControllerTracking.SensorLeftHandPos;
                    _networkTransformRigHandLeft.InterpolationTarget.rotation  = Avatar.FirstControllerTracking.SensorLeftHandRot;
                    _networkTransformRigHandRight.InterpolationTarget.position = Avatar.FirstControllerTracking.SensorRightHandPos;
                    _networkTransformRigHandRight.InterpolationTarget.rotation = Avatar.FirstControllerTracking.SensorRightHandRot;
                }
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Initializes the network rig, that synchronizes the relevant avatar transforms.
        /// </summary>
        /// <param name="root">The GameObject that synchronizes the root transform</param>
        /// <param name="cam">The GameObject that synchronizes the camera transform</param>
        /// <param name="handLeft">The GameObject that synchronizes the left hand transform</param>
        /// <param name="handRight">The GameObject that synchronizes the right hand transform</param>
        internal void SetNetworkRig(GameObject root, GameObject cam, GameObject handLeft, GameObject handRight)
        {
            _networkTransformRigAvatar    = root.GetComponent<NetworkTransform>();
            _networkTransformRigCamera    = cam.GetComponent<NetworkTransform>();
            _networkTransformRigHandLeft  = handLeft.GetComponent<NetworkTransform>();
            _networkTransformRigHandRight = handRight.GetComponent<NetworkTransform>();

            UxrAvatar avatar = GetComponentInParent<UxrAvatar>();

            _networkTransformRigAvatar.InterpolationTarget    = avatar.transform;
            _networkTransformRigCamera.InterpolationTarget    = avatar.CameraComponent.transform;
            _networkTransformRigHandLeft.InterpolationTarget  = avatar.GetHand(UxrHandSide.Left).Wrist;
            _networkTransformRigHandRight.InterpolationTarget = avatar.GetHand(UxrHandSide.Right).Wrist;

            _networkTransformRigAvatar.InterpolateErrorCorrection    = false;
            _networkTransformRigCamera.InterpolateErrorCorrection    = false;
            _networkTransformRigHandLeft.InterpolateErrorCorrection  = false;
            _networkTransformRigHandRight.InterpolateErrorCorrection = false;
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the network avatar implementer.
        /// </summary>
        private void Awake()
        {
            _implementer = new UxrNetworkAvatarImplementer(this);
        }

        /// <summary>
        ///     Subscribes to events.
        /// </summary>
        private void OnEnable()
        {
            UxrAvatar.GlobalAvatarMoved += Avatar_GlobalAvatarMoved;
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        private void OnDisable()
        {
            UxrAvatar.GlobalAvatarMoved -= Avatar_GlobalAvatarMoved;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when a component in UltimateXR had a state change.
        /// </summary>
        /// <param name="component">Component</param>
        /// <param name="eventArgs">Event parameters</param>
        private void UxrManager_ComponentStateChanged(IUxrStateSync component, UxrSyncEventArgs eventArgs)
        {
            _implementer.HandleComponentStateChanged(component, eventArgs, Object.HasInputAuthority, sendRpc: data => RPC_ComponentStateChanged(data));
        }

        /// <summary>
        ///     Called when an avatar moved.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event parameters</param>
        private void Avatar_GlobalAvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            if (Object && Object.HasInputAuthority && ReferenceEquals(e.Avatar, UxrAvatar.LocalAvatar))
            {
                _actualAvatarPosition = e.NewPosition;
                _actualAvatarRotation = e.NewRotation;
            }
        }

        /// <summary>
        ///     RPC from client to server to request the current global state upon joining.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="info">Filled by Photon with RPC information</param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority, HostMode = RpcHostMode.SourceIsServer)]
        private void RPC_NewAvatarJoined(byte[] avatarState, RpcInfo info = default)
        {
            if (info.Source != PlayerRef.None)
            {
                byte[] serializedState = _implementer.HandleNewAvatarJoined(avatarState, info.Source.ToString(), gameObject);

                // Send global state to new user.
                RPC_LoadGlobalState(serializedState);

                // Broadcast initial state of new avatar.
                RPC_LoadAvatarState(avatarState);
            }
            else
            {
                // When using RpcHostMode.SourceIsServer, Source is None.
                // This means it is the host, and it doesn't require a request for the current state.
            }
        }

        /// <summary>
        ///     RPC from server to client that joined to sync to the current state.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
        private void RPC_LoadGlobalState(byte[] serializedStateData)
        {
            _implementer.LoadSyncOnJoinState(serializedStateData);
        }

        /// <summary>
        ///     RPC from server to all clients to sync the state of a new avatar that joined.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_LoadAvatarState(byte[] serializedStateData)
        {
            _implementer.LoadJoinedAvatarState(serializedStateData);
        }

        /// <summary>
        ///     RPC to propagate state change events to all other clients.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
        private void RPC_ComponentStateChanged(byte[] serializedEventData)
        {
            _implementer.LoadRemoteComponentStateChanged(serializedEventData);
        }

        #endregion

        #region Private Types & Data

        private UxrNetworkAvatarImplementer _implementer;

        private Vector3    _actualAvatarPosition;
        private Quaternion _actualAvatarRotation;
        private Vector3    _actualAvatarCameraPosition;
        private Quaternion _actualAvatarCameraRotation;
        private Vector3    _actualAvatarLeftHandPosition;
        private Quaternion _actualAvatarLeftHandRotation;
        private Vector3    _actualAvatarRightHandPosition;
        private Quaternion _actualAvatarRightHandRotation;

        #endregion
    }
#else
    public class UxrPhotonFusionAvatar : MonoBehaviour
    {
    }
#endif
}
