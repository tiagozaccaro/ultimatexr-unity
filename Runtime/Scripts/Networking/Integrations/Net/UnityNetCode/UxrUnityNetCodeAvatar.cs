// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUnityNetCodeAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Extensions.Unity;
using UnityEngine;
#if ULTIMATEXR_USE_UNITY_NETCODE
using System;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSync;
using Unity.Netcode;
#endif

namespace UltimateXR.Networking.Integrations.Net.UnityNetCode
{
#if ULTIMATEXR_USE_UNITY_NETCODE
    public class UxrUnityNetCodeAvatar : NetworkBehaviour, IUxrNetworkAvatar
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
            if (child == null)
            {
                return false;
            }

            NetworkObject childNo = child.GetComponent<NetworkObject>();
            if (childNo == null)
            {
                return false;
            }

            NetworkObject parentNo = parent ? parent.GetComponent<NetworkObject>() : null;

            // If the parent is null, send default(NOR) which the ServerRpc interprets as "remove parent"
            NetworkObjectReference parentRef = parentNo ? new NetworkObjectReference(parentNo) : default;

            if (parent != null && parentNo == null)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.NetworkingModule} {nameof(UxrUnityNetCodeAvatar)}.{nameof(ChangeParent)}() Parent GameObject doesn't have a {nameof(NetworkObject)} component. {child.GetPathUnderScene()} will be de-parented.");
                }
            }

            ChangeParentServerRpc(new NetworkObjectReference(childNo), parentRef);
            return true;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Request authority of the local avatar over an object.
        /// </summary>
        /// <param name="networkObject">The object to get authority over</param>
        public void RequestAuthority(NetworkObject networkObject)
        {
            RequestAuthorityServerRpc(networkObject);
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
            _implementer.HandleComponentStateChanged(component, eventArgs, IsOwner && Avatar.AvatarMode == UxrAvatarMode.Local, sendRpc: ComponentStateChangedServerRpc);
        }

        #endregion

        #region Event Trigger Methods

        /// <inheritdoc />
        public override void OnNetworkSpawn()
        {
            _implementer.HandleSpawn(IsOwner,
                                     IsServer,
                                     OwnerClientId.ToString(),
                                     $"Player {OwnerClientId} ({(IsOwner ? "Local" : "External")})",
                                     nameof(UxrUnityNetCodeAvatar) + "." + nameof(OnNetworkSpawn),
                                     sendNewAvatarJoined: data => NewAvatarJoinedServerRpc(data),
                                     componentStateChangedHandler: UxrManager_ComponentStateChanged);
        }

        /// <inheritdoc />
        public override void OnNetworkDespawn()
        {
            _implementer.HandleDespawn($"{nameof(UxrUnityNetCodeAvatar)}.{nameof(OnNetworkDespawn)}");
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Server RPC to request the current global state upon joining.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="serverRpcParams">Filled by NetCode with info</param>
        [ServerRpc]
        private void NewAvatarJoinedServerRpc(byte[] avatarState, ServerRpcParams serverRpcParams = default)
        {
            byte[] serializedState = _implementer.HandleNewAvatarJoined(avatarState, serverRpcParams.Receive.SenderClientId.ToString(), gameObject);

            // Send global state to new user.
            ClientRpcParams clientRpcParams = new ClientRpcParams
                                              {
                                                  Send = new ClientRpcSendParams
                                                         {
                                                             TargetClientIds = new[] { serverRpcParams.Receive.SenderClientId }
                                                         }
                                              };
            LoadGlobalStateClientRpc(serializedState, clientRpcParams);

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
        /// <param name="networkObjectReference">Object to get authority over</param>
        /// <param name="serverRpcParams">Filled by NetCode with info</param>
        [ServerRpc]
        private void RequestAuthorityServerRpc(NetworkObjectReference networkObjectReference, ServerRpcParams serverRpcParams = default)
        {
            if (networkObjectReference.TryGet(out NetworkObject networkObject))
            {
                NetworkManager networkManager = UxrNetworkManager.Instance.GetComponent<NetworkManager>();

                if (networkManager != null)
                {
                    networkObject.ChangeOwnership(serverRpcParams.Receive.SenderClientId);
                }
            }
            else
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.NetworkingModule} {nameof(UxrUnityNetCodeAvatar)}.{nameof(RequestAuthorityServerRpc)}() Cannot find target network object.");
                }
            }
        }

        /// <summary>
        ///     Server RPC that updates the parent of a specified child network object on the server side.
        ///     If the parent reference is null, the child's parent is removed.
        /// </summary>
        /// <param name="childRef">Reference to the child NetworkObject whose parent is being changed.</param>
        /// <param name="parentRef">Reference to the new parent NetworkObject. Pass default if removing the parent.</param>
        [ServerRpc]
        private void ChangeParentServerRpc(NetworkObjectReference childRef, NetworkObjectReference parentRef)
        {
            if (!childRef.TryGet(out NetworkObject childNo) || childNo == null)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} {nameof(UxrUnityNetCodeAvatar)}.{nameof(ChangeParentServerRpc)}() Cannot find target network object for child.");
                }

                return;
            }

            if (!childNo.IsSpawned)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} {nameof(UxrUnityNetCodeAvatar)}.{nameof(ChangeParentServerRpc)}() child NetworkObject is not spawned.");
                }

                return;
            }

            if (!parentRef.TryGet(out NetworkObject parentNo) || parentNo == null)
            {
                // No parent => remove parent
                childNo.TryRemoveParent();
                return;
            }

            if (!parentNo.IsSpawned)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} {nameof(UxrUnityNetCodeAvatar)}.{nameof(ChangeParentServerRpc)}() parent NetworkObject is not spawned.");
                }

                return;
            }

            childNo.TrySetParent(parentNo);
        }

        /// <summary>
        ///     Targeted client RPC to a client that joined to sync to the current state.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        /// <param name="clientRpcParams">Target of the RPC</param>
        [ClientRpc]
        private void LoadGlobalStateClientRpc(byte[] serializedStateData, ClientRpcParams clientRpcParams = default)
        {
            _implementer.LoadSyncOnJoinState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC to sync the state of a new avatar that joined.
        /// </summary>
        /// <param name="serializedStateData">The serialized state data</param>
        [ClientRpc]
        private void LoadAvatarStateClientRpc(byte[] serializedStateData)
        {
            _implementer.LoadJoinedAvatarState(serializedStateData);
        }

        /// <summary>
        ///     Client RPC call to execute a state change event. It will execute on all clients except the one that generated it,
        ///     which can be identified because it's the one with ownership.
        /// </summary>
        /// <param name="serializedEventData">The serialized state change data</param>
        [ClientRpc]
        private void ComponentStateChangedClientRpc(byte[] serializedEventData)
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
    public class UxrUnityNetCodeAvatar : MonoBehaviour
    {
    }
#endif
}