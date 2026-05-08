// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrFishNetAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
#if ULTIMATEXR_USE_FISHNET_SDK
using System;
using UltimateXR.Attributes;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSync;
using FishNet.Connection;
using FishNet.Object;
#endif

namespace UltimateXR.Networking.Integrations.Net.FishNet
{
#if ULTIMATEXR_USE_FISHNET_SDK
    public class UxrFishNetAvatar : NetworkBehaviour, IUxrNetworkAvatar
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [ReadOnly] private GameObject _networkCamera;
        [SerializeField] [ReadOnly] private GameObject _networkHandLeft;
        [SerializeField] [ReadOnly] private GameObject _networkHandRight;

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
        public bool UsesDummyNetworkTransforms => true;

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

        #region Public Methods

        /// <summary>
        ///     Sets up the dummy network transforms that hang from the avatar. These network transforms will be used
        ///     to propagate the transform data, since FishNet only can synchronize transforms in local space.
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
        ///     Request authority of the local avatar over an object.
        /// </summary>
        /// <param name="networkObject">The object to get authority over</param>
        public void RequestAuthority(NetworkObject networkObject)
        {
            RequestAuthorityServerRpc(networkObject);
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the network avatar implementer.
        /// </summary>
        private void Awake()
        {
            _implementer = new UxrNetworkAvatarImplementer(this);
            _implementer.SetupDummyNetworkTransforms(_networkCamera, _networkHandLeft, _networkHandRight);
        }

        /// <summary>
        ///     Subscribes to events.
        /// </summary>
        private void OnEnable()
        {
            UxrManager.StageUpdated += UxrManager_StageUpdated;
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        private void OnDisable()
        {
            UxrManager.StageUpdated -= UxrManager_StageUpdated;
        }

        /// <summary>
        ///     Unity's Update() method. This will check for correct setup.
        /// </summary>
        private void Update()
        {
            _implementer.ValidateDummyNetworkTransforms();
        }

        #endregion

        #region Event Handling Methods

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
            _implementer.HandleComponentStateChanged(component, eventArgs, IsOwner, sendRpc: data => ComponentStateChangedServerRpc(data));
        }

        #endregion

        #region Event Trigger Methods

        /// <inheritdoc />
        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!UxrNetworkManager.IsHost)
            {
                // Avoid calling in host mode, since OnStartClient() is already called.
                StartNetworkAvatar();
            }
        }

        /// <inheritdoc />
        public override void OnStartClient()
        {
            base.OnStartClient();

            StartNetworkAvatar();
        }

        /// <inheritdoc />
        public override void OnStopServer()
        {
            base.OnStopServer();

            if (!UxrNetworkManager.IsHost)
            {
                // Avoid calling in host mode, since OnStopClient() is already called.
                StopNetworkAvatar();
            }
        }

        /// <inheritdoc />
        public override void OnStopClient()
        {
            base.OnStopClient();

            StopNetworkAvatar();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Finishes the network avatar initialization.
        /// </summary>
        private void StartNetworkAvatar()
        {
            _implementer.HandleSpawn(IsOwner,
                                     UxrNetworkManager.IsServer,
                                     OwnerId.ToString(),
                                     $"Player {OwnerId} ({(IsOwner ? "Local" : "External")})",
                                     $"{nameof(UxrFishNetAvatar)}.{nameof(StartNetworkAvatar)}",
                                     sendNewAvatarJoined: data => NewAvatarJoinedServerRpc(data),
                                     componentStateChangedHandler: UxrManager_ComponentStateChanged);
        }

        /// <summary>
        ///     Stops a network avatar.
        /// </summary>
        private void StopNetworkAvatar()
        {
            _implementer.HandleDespawn($"{nameof(UxrFishNetAvatar)}.{nameof(StopNetworkAvatar)}");
        }

        /// <summary>
        ///     Server RPC to request the current global state upon joining.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="conn">Filled by FishNet with info</param>
        [ServerRpc]
        private void NewAvatarJoinedServerRpc(byte[] avatarState, NetworkConnection conn = null)
        {
            byte[] serializedState = _implementer.HandleNewAvatarJoined(avatarState, conn.ClientId.ToString(), gameObject);

            // Send global state to new user.
            LoadGlobalStateTargetRpc(conn, serializedState);

            // Broadcast initial state of new avatar.
            LoadAvatarStateClientRpc(avatarState);
        }

        /// <summary>
        ///     Server RPC call to propagate state change events to all other clients.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [ServerRpc]
        private void ComponentStateChangedServerRpc(byte[] serializedEventData)
        {
            ComponentStateChangedClientRpc(serializedEventData);
        }

        /// <summary>
        ///     Server RPC requesting authority over an object.
        /// </summary>
        /// <param name="networkObject">Object to get authority over</param>
        /// <param name="conn">Filled by FishNet with info</param>
        [ServerRpc]
        private void RequestAuthorityServerRpc(NetworkObject networkObject, NetworkConnection conn = null)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Granting authority to owner {OwnerId} over network object {networkObject.name}.");
            }

            NetworkObject[] networkObjects = networkObject.GetComponentsInChildren<NetworkObject>();

            foreach (NetworkObject no in networkObjects)
            {
                no.GiveOwnership(conn);
            }
        }

        /// <summary>
        ///     Targeted client RPC to client that joined to sync to the current state.
        /// </summary>
        /// <param name="conn">Target connection</param>
        /// <param name="serializedStateData">The serialized state data</param>
        [TargetRpc]
        private void LoadGlobalStateTargetRpc(NetworkConnection conn, byte[] serializedStateData)
        {
            _implementer.LoadSyncOnJoinState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC to sync the state of a new avatar that joined.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        [ObserversRpc]
        private void LoadAvatarStateClientRpc(byte[] serializedStateData)
        {
            _implementer.LoadJoinedAvatarState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC call to execute a state change event. It will execute on all clients except the one that generated it,
        ///     which can be identified because it's the one with ownership.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [ObserversRpc]
        private void ComponentStateChangedClientRpc(byte[] serializedEventData)
        {
            _implementer.LoadRemoteComponentStateChanged(serializedEventData);
        }

        #endregion

        #region Private Types & Data

        private UxrNetworkAvatarImplementer _implementer;

        #endregion
    }
#else
    public class UxrFishNetAvatar : MonoBehaviour
    {
    }
#endif
}
