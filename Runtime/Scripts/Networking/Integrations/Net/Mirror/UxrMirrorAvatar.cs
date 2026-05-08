// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrMirrorAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
#if ULTIMATEXR_USE_MIRROR_SDK
using System;
using UltimateXR.Avatar;
using UltimateXR.Core.StateSync;
using Mirror;
#endif

namespace UltimateXR.Networking.Integrations.Net.Mirror
{
#if ULTIMATEXR_USE_MIRROR_SDK
    public class UxrMirrorAvatar : NetworkBehaviour, IUxrNetworkAvatar
    {
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

        #region Public Methods

        /// <summary>
        ///     Request authority of the local avatar over an object.
        /// </summary>
        /// <param name="networkIdentity">The object to get authority over</param>
        public void RequestAuthority(NetworkIdentity networkIdentity)
        {
            CmdRequestAuthority(networkIdentity);
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
            _implementer.HandleComponentStateChanged(component, eventArgs, netIdentity.isOwned, sendRpc: data => CmdComponentStateChanged(data));
        }

        #endregion

        #region Event Trigger Methods

        /// <inheritdoc />
        public override void OnStartClient()
        {
            _implementer.HandleSpawn(netIdentity.isOwned,
                                     netIdentity.isServer,
                                     netId.ToString(),
                                     $"Player {netId} ({(netIdentity.isOwned ? "Local" : "External")})",
                                     $"{nameof(UxrMirrorAvatar)}.{nameof(OnStartClient)}",
                                     sendNewAvatarJoined: data => CmdNewAvatarJoined(data),
                                     componentStateChangedHandler: UxrManager_ComponentStateChanged);
        }

        /// <inheritdoc />
        public override void OnStopClient()
        {
            _implementer.HandleDespawn($"{nameof(UxrMirrorAvatar)}.{nameof(OnStopClient)}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Server RPC to request the current global state upon joining.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="sender">Information filled by Mirror with information about the sender</param>
        [Command]
        private void CmdNewAvatarJoined(byte[] avatarState, NetworkConnectionToClient sender = null)
        {
            byte[] serializedState = _implementer.HandleNewAvatarJoined(avatarState, sender.identity.netId.ToString(), gameObject);

            // Send global state to new user.
            TargetLoadGlobalState(sender, serializedState);

            // Broadcast initial state of new avatar.
            RpcLoadAvatarState(avatarState);
        }

        /// <summary>
        ///     Server RPC to propagate state change events to all other clients.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [Command]
        private void CmdComponentStateChanged(byte[] serializedEventData)
        {
            RpcComponentStateChanged(serializedEventData);
        }

        /// <summary>
        ///     Server RPC requesting authority over an object.
        /// </summary>
        /// <param name="networkIdentity">Object to get authority over</param>
        [Command]
        private void CmdRequestAuthority(NetworkIdentity networkIdentity)
        {
            networkIdentity.AssignClientAuthority(netIdentity.connectionToClient);
        }

        /// <summary>
        ///     Targeted client RPC to client that joined to sync to the current state.
        /// </summary>
        /// <param name="target">Target of the RPC</param>
        /// <param name="serializedStateData">The serialized state data</param>
        [TargetRpc]
        private void TargetLoadGlobalState(NetworkConnectionToClient target, byte[] serializedStateData)
        {
            _implementer.LoadSyncOnJoinState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC to sync the state of a new avatar that joined.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        [ClientRpc]
        private void RpcLoadAvatarState(byte[] serializedStateData)
        {
            _implementer.LoadJoinedAvatarState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC to execute a state change event. It will execute on all clients except the one that generated it,
        ///     which can be identified because it's the one with ownership.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [ClientRpc]
        private void RpcComponentStateChanged(byte[] serializedEventData)
        {
            _implementer.LoadRemoteComponentStateChanged(serializedEventData);
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

        #endregion

        #region Private Types & Data

        private UxrNetworkAvatarImplementer _implementer;

        #endregion
    }
#else
    public class UxrMirrorAvatar : MonoBehaviour
    {
    }
#endif
}
