// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrNetworkAvatarImplementer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Instantiation;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSave;
using UltimateXR.Core.StateSync;
using UltimateXR.Extensions.System;
using UltimateXR.Extensions.Unity;
using UnityEngine;

namespace UltimateXR.Networking
{
    /// <summary>
    ///     Implements common network avatar functionality shared across all SDK-specific network avatar classes.
    ///     Used via composition: each SDK avatar creates an instance and delegates common operations to it.
    /// </summary>
    public class UxrNetworkAvatarImplementer
    {
        #region Public Types & Data

        /// <summary>
        ///     Event called right after the avatar was spawned.
        /// </summary>
        public event Action AvatarSpawned;

        /// <summary>
        ///     Event called right after the avatar was despawned.
        /// </summary>
        public event Action AvatarDespawned;

        /// <summary>
        ///     Gets or sets a value indicating whether the synchronized scene state (sync-on-join) has been loaded.<br />
        ///     This value becomes <c>true</c> when:
        ///     <list type="bullet">
        ///         <item>
        ///             <description>
        ///                 <b>If the player is the server:</b> When the avatar is spawned as part of the server initializing the
        ///                 scene.
        ///             </description>
        ///         </item>
        ///         <item>
        ///             <description>
        ///                 <b>If the player is a client:</b> After the client receives and deserializes the current scene state
        ///                 requested from the server upon joining.
        ///             </description>
        ///         </item>
        ///     </list>
        /// </summary>
        /// <remarks>
        ///     This property is automatically reset to <c>false</c> when the last network avatar is despawned so that it can be
        ///     used across multiple sessions.
        /// </remarks>
        public static bool SyncOnJoinStateLoaded { get; private set; }

        /// <summary>
        ///     Gets whether the networking component was initialized using <see cref="InitializeNetworkAvatar" />.
        /// </summary>
        public bool IsInitialized { get; private set; }

        /// <summary>
        ///     Gets whether this avatar is the avatar controlled by the user (true) or a remote avatar (false).
        /// </summary>
        public bool IsLocal { get; private set; }

        /// <summary>
        ///     Gets whether this is the server.
        /// </summary>
        public bool IsServer { get; private set; }

        /// <summary>
        ///     Gets the avatar component.
        /// </summary>
        public UxrAvatar Avatar { get; private set; }

        /// <summary>
        ///     Gets or sets the avatar name.
        /// </summary>
        public string AvatarName
        {
            get => _avatarName;
            set
            {
                _avatarName = value;

                if (Avatar != null)
                {
                    Avatar.name = value;
                }
            }
        }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="networkBehaviour">The MonoBehaviour that owns this implementer</param>
        public UxrNetworkAvatarImplementer(MonoBehaviour networkBehaviour)
        {
            _networkBehaviour = networkBehaviour;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Handles the common spawn logic for all network avatar implementations.
        /// </summary>
        /// <param name="isOwner">Whether this is the owning client, meaning the avatar is the local avatar</param>
        /// <param name="isServer">Whether this is the server</param>
        /// <param name="uniqueId">Unique ID for the avatar</param>
        /// <param name="avatarName">Display name for the avatar</param>
        /// <param name="logPrefix">Prefix for logging. Should be the name of the SDK avatar class and the source method name</param>
        /// <param name="sendNewAvatarJoined">Callback to send the initial avatar state via SDK-specific RPC</param>
        /// <param name="componentStateChangedHandler">Handler for component state changes, stored for subscribe/unsubscribe</param>
        public void HandleSpawn(bool                                    isOwner,
                                bool                                    isServer,
                                string                                  uniqueId,
                                string                                  avatarName,
                                string                                  logPrefix,
                                Action<byte[]>                          sendNewAvatarJoined,
                                Action<IUxrStateSync, UxrSyncEventArgs> componentStateChangedHandler)
        {
            s_avatarCount++;

            Avatar = _networkBehaviour.GetComponent<UxrAvatar>();

            InitializeNetworkAvatar(Avatar, isOwner, isServer, uniqueId, avatarName);

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {logPrefix}: Is Local? {IsLocal}, Name: {AvatarName}. UniqueId: {Avatar.UniqueId}.");
            }

            AvatarSpawned?.Invoke();

            if (UxrInstanceManager.HasInstance)
            {
                UxrInstanceManager.Instance.NotifyNetworkSpawn(Avatar.gameObject);
            }

            // Notify the global UxrNetworkManager event
            UxrNetworkManager.RaiseAvatarSpawned(Avatar);

            if (isOwner)
            {
                _componentStateChangedHandler    =  componentStateChangedHandler;
                UxrManager.ComponentStateChanged += _componentStateChangedHandler;

                if (!isServer)
                {
                    byte[] localAvatarState = UxrManager.Instance.SaveStateChanges(new List<GameObject> { Avatar.gameObject }, null, UxrStateSaveLevel.ChangesSinceBeginning, UxrGlobalSettings.Instance.NetFormatInitialState, UxrGlobalSettings.Instance.NetUniqueIdDebugLevel);

                    if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
                    {
                        Debug.Log($"{UxrConstants.NetworkingModule} Requesting global state and sending local avatar state in {localAvatarState.Length} bytes.");
                    }

                    sendNewAvatarJoined(localAvatarState);
                }
                else
                {
                    SyncOnJoinStateLoaded = true;
                }
            }
        }

        /// <summary>
        ///     Handles the common despawn logic for all network avatar implementations.
        /// </summary>
        /// <param name="logPrefix">Prefix for logging. Should be the name of the SDK avatar class and the source method name</param>
        public void HandleDespawn(string logPrefix)
        {
            s_avatarCount--;

            if (Avatar && IsLocal)
            {
                UxrManager.ComponentStateChanged -= _componentStateChangedHandler;
            }

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {logPrefix}: Is Local? {IsLocal}, Name: {AvatarName}");
            }

            AvatarDespawned?.Invoke();

            // Notify the global UxrNetworkManager event
            UxrNetworkManager.RaiseAvatarDespawned(Avatar);

            if (s_avatarCount == 0 || IsLocal)
            {
                SyncOnJoinStateLoaded = false;

                _pendingOutgoingQueue.Clear();
                _pendingIncomingQueue.Clear();
            }
        }

        /// <summary>
        ///     Handles the server-side logic when a new avatar joins.
        /// </summary>
        /// <param name="avatarState">The initial state of the avatar that joined</param>
        /// <param name="senderInfo">SDK-specific sender identifier (for logging)</param>
        /// <param name="selfGameObject">The avatar's own GameObject to exclude from global state</param>
        /// <returns>The serialized global state to send back to the joining client</returns>
        public byte[] HandleNewAvatarJoined(byte[] avatarState, string senderInfo, GameObject selfGameObject)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Received request for global state from client {senderInfo}.");
            }

            UxrManager.Instance.LoadStateChanges(avatarState);

            byte[] serializedState = UxrManager.Instance.SaveStateChanges(null, new List<GameObject> { selfGameObject }, UxrStateSaveLevel.ChangesSinceBeginning, UxrGlobalSettings.Instance.NetFormatInitialState, UxrGlobalSettings.Instance.NetUniqueIdDebugLevel);

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Sending global state in {serializedState.Length} bytes to client {senderInfo}. Broadcasting {avatarState.Length} bytes to sync new avatar.");
            }

            return serializedState;
        }

        /// <summary>
        ///     Loads the current scene state for a client avatar right after joining a session.
        /// </summary>
        /// <param name="serializedStateData">
        ///     The byte array representing the synchronization state to be applied. Sent by the
        ///     server.
        /// </param>
        public void LoadSyncOnJoinState(byte[] serializedStateData)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Receiving {serializedStateData.Length} bytes of global state data.");
            }

            UxrManager.Instance.LoadStateChanges(serializedStateData);
            SyncOnJoinStateLoaded = true;

            // Process enqueued messages in order

            if (_pendingIncomingQueue.Count > 0 && UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
            {
                Debug.LogWarning($"{UxrConstants.NetworkingModule} Loading {_pendingIncomingQueue.Count} pending incoming messages that were received before the sync-on-join.");
            }

            while (_pendingIncomingQueue.Count > 0)
            {
                LoadRemoteComponentStateChanged(_pendingIncomingQueue.Dequeue());
            }

            if (_pendingOutgoingQueue.Count > 0 && UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
            {
                Debug.LogWarning($"{UxrConstants.NetworkingModule} Sending {_pendingOutgoingQueue.Count} pending outgoing messages that were generated before the sync-on-join.");
            }

            while (_pendingOutgoingQueue.Count > 0)
            {
                PendingOutgoingMessage message = _pendingOutgoingQueue.Dequeue();
                message.SendRpc(message.Data);
            }
        }

        /// <summary>
        ///     Handles component state change events. Serializes and forwards via the provided RPC callback if they are from the
        ///     local avatar.
        /// </summary>
        /// <param name="component">Component that changed</param>
        /// <param name="eventArgs">Event parameters</param>
        /// <param name="isOwner">Whether this is the owning client</param>
        /// <param name="sendRpc">Callback to send serialized data via SDK-specific RPC</param>
        public void HandleComponentStateChanged(IUxrStateSync component, UxrSyncEventArgs eventArgs, bool isOwner, Action<byte[]> sendRpc)
        {
            if (!isOwner)
            {
                // Ignore state changes that are not local.
                return;
            }

            if (eventArgs.Options.HasFlag(UxrStateSyncOptions.Network))
            {
                byte[] serializedEvent = eventArgs.SerializeEventBinary(component);

                if (serializedEvent != null)
                {
                    if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
                    {
                        Debug.Log($"{UxrConstants.NetworkingModule} Sending {serializedEvent.Length} bytes from {component.Component.name} ({component.UniqueId}) {eventArgs}. Enqueuing? {!SyncOnJoinStateLoaded}.");
                    }

                    if (SyncOnJoinStateLoaded)
                    {
                        sendRpc(serializedEvent);
                    }
                    else
                    {
                        _pendingOutgoingQueue.Enqueue(new PendingOutgoingMessage(serializedEvent, sendRpc));
                    }
                }
            }
        }

        /// <summary>
        ///     Loads the avatar state data received from the network for an avatar that joined the session.
        /// </summary>
        /// <param name="serializedStateData">The serialized byte array containing the avatar state data to be loaded.</param>
        public void LoadJoinedAvatarState(byte[] serializedStateData)
        {
            if (IsLocal)
            {
                // Don't execute on the source of the event, we don't want to load our own avatar data.
                return;
            }

            if (IsServer)
            {
                // The server already loaded the avatar state data in HandleNewAvatarJoined().
                return;
            }

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Receiving {serializedStateData.Length} bytes of avatar state data.");
            }

            UxrManager.Instance.LoadStateChanges(serializedStateData);
        }

        /// <summary>
        ///     Processes the updated state of a remote component received via synchronization events.
        ///     The method ensures state updates are processed only after the initial state has been loaded.
        /// </summary>
        /// <param name="serializedEventData">The serialized event data representing the state of the remote component.</param>
        public void LoadRemoteComponentStateChanged(byte[] serializedEventData)
        {
            if (IsLocal)
            {
                // Don't load on the source of the event.
                return;
            }

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} Receiving {serializedEventData.Length} bytes of data. Base64: {Convert.ToBase64String(serializedEventData)}. Enqueuing? {!SyncOnJoinStateLoaded}.");
            }

            if (!SyncOnJoinStateLoaded)
            {
                // Enqueue sync events until the initial state is sent to make sure the syncs are only processed after the initial state.
                _pendingIncomingQueue.Enqueue(serializedEventData);
                return;
            }

            UxrManager.Instance.ExecuteStateSyncEvent(serializedEventData);
        }

        /// <summary>
        ///     Sets up the dummy network transforms for avatar transform synchronization.
        /// </summary>
        /// <param name="networkCamera">Dummy GameObject that will synchronize the camera transform.</param>
        /// <param name="networkHandLeft">Dummy GameObject that will synchronize the left hand.</param>
        /// <param name="networkHandRight">Dummy GameObject that will synchronize the right hand.</param>
        /// <param name="syncRead">Whether to read the avatar transforms from the network transforms.</param>
        /// <param name="syncWrite">Whether to write the avatar transforms to the network transforms.</param>
        public void SetupDummyNetworkTransforms(GameObject networkCamera, GameObject networkHandLeft, GameObject networkHandRight, bool syncRead = true, bool syncWrite = true)
        {
            _networkCamera            = networkCamera;
            _networkHandLeft          = networkHandLeft;
            _networkHandRight         = networkHandRight;
            _syncDummyTransformsRead  = syncRead;
            _syncDummyTransformsWrite = syncWrite;
        }

        /// <summary>
        ///     Handles UxrManager stage updates for dummy network transform synchronization.
        /// </summary>
        /// <param name="stage">The stage that finished updating</param>
        public void HandleStageUpdated(UxrUpdateStage stage)
        {
            if (Avatar == null)
            {
                return;
            }

            if (stage == UxrUpdateStage.AvatarUsingTracking && !IsLocal && _syncDummyTransformsRead)
            {
                // Copy from network transforms to avatar

                if (Avatar.CameraComponent && _networkCamera)
                {
                    Avatar.CameraComponent.transform.SetPositionAndRotation(_networkCamera.transform);
                }

                if (Avatar.LeftHandBone != null && _networkHandLeft != null)
                {
                    Avatar.LeftHandBone.SetPositionAndRotation(_networkHandLeft.transform);
                }

                if (Avatar.RightHandBone != null && _networkHandRight != null)
                {
                    Avatar.RightHandBone.SetPositionAndRotation(_networkHandRight.transform);
                }
            }
            else if (stage == UxrUpdateStage.PostProcess && IsLocal && _syncDummyTransformsWrite)
            {
                // Copy from avatar to network transforms

                if (Avatar.CameraComponent && _networkCamera)
                {
                    _networkCamera.transform.SetPositionAndRotation(Avatar.CameraComponent.transform);
                }

                if (Avatar.LeftHandBone != null && _networkHandLeft != null)
                {
                    _networkHandLeft.transform.SetPositionAndRotation(Avatar.LeftHandBone);
                }

                if (Avatar.RightHandBone != null && _networkHandRight != null)
                {
                    _networkHandRight.transform.SetPositionAndRotation(Avatar.RightHandBone);
                }
            }
        }

        /// <summary>
        ///     Validates that the dummy network transforms are set up correctly.
        ///     Should be called from Update() only by implementations that use dummy transforms.
        /// </summary>
        public void ValidateDummyNetworkTransforms()
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
            {
                if (_networkCamera == null || _networkHandLeft == null || _networkHandRight == null)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Avatar {_networkBehaviour}'s prefab needs to be setup again using {nameof(UxrNetworkManager)} because dummy NetworkTransforms are missing. If the avatar is already registered, remove it and add it again.");
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Initializes an avatar after it was spawned.
        /// </summary>
        /// <param name="avatar">Avatar component</param>
        /// <param name="isLocal">Whether the avatar is local</param>
        /// <param name="isServer">Whether this component is running on the server</param>
        /// <param name="uniqueId">A unique Id to identify the avatar</param>
        /// <param name="avatarName">The name of the avatar</param>
        private void InitializeNetworkAvatar(UxrAvatar avatar, bool isLocal, bool isServer, string uniqueId, string avatarName)
        {
            IsLocal           = isLocal;
            IsServer          = isServer;
            AvatarName        = avatarName;
            avatar.AvatarMode = isLocal ? UxrAvatarMode.Local : UxrAvatarMode.UpdateExternally;

            if (!IsInitialized)
            {
                avatar.CombineUniqueId(uniqueId.GetGuid());
                IsInitialized = true;
            }
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Stores a pending outgoing message to be sent via RPC.
        /// </summary>
        private struct PendingOutgoingMessage
        {
            #region Public Types & Data

            public byte[]         Data    { get; }
            public Action<byte[]> SendRpc { get; }

            #endregion

            #region Constructors & Finalizer

            public PendingOutgoingMessage(byte[] data, Action<byte[]> sendRpc)
            {
                Data    = data;
                SendRpc = sendRpc;
            }

            #endregion
        }

        private static int s_avatarCount;

        private readonly MonoBehaviour _networkBehaviour;

        private readonly Queue<byte[]>                 _pendingIncomingQueue = new Queue<byte[]>();
        private readonly Queue<PendingOutgoingMessage> _pendingOutgoingQueue = new Queue<PendingOutgoingMessage>();

        private Action<IUxrStateSync, UxrSyncEventArgs> _componentStateChangedHandler;
        private GameObject                              _networkCamera;
        private GameObject                              _networkHandLeft;
        private GameObject                              _networkHandRight;
        private string                                  _avatarName;
        private bool                                    _syncDummyTransformsRead;
        private bool                                    _syncDummyTransformsWrite;

        #endregion
    }
}