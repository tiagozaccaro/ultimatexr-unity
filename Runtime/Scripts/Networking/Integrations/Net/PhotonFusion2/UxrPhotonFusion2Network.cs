// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonFusion2Network.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK && UNITY_EDITOR
using UnityEditor;
#endif
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UltimateXR.Core.Settings;
using UltimateXR.Core.Threading.TaskControllers;
using UltimateXR.Extensions.System.Collections;
using UltimateXR.Extensions.Unity;
using UltimateXR.Manipulation;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using Behaviour = UnityEngine.Behaviour;
#endif

#pragma warning disable 414 // Disable warnings due to unused values

namespace UltimateXR.Networking.Integrations.Net.PhotonFusion2
{
    /// <summary>
    ///     Implementation of networking support using Photon Fusion 2.
    /// </summary>
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
    public class UxrPhotonFusion2Network : UxrNetworkImplementation, INetworkRunnerCallbacks
#else
    public class UxrPhotonFusion2Network : UxrNetworkImplementation
#endif
    {
        #region Inspector Properties/Serialized Fields

        [Tooltip("Show a UI during play mode with connection options to quickly prototype networking functionality")] [SerializeField] private bool _usePrototypingUI = true;

        #endregion

        #region Public Overrides UxrNetworkImplementation

        /// <inheritdoc />
        public override string SdkName => UxrConstants.SdkPhotonFusion2;

        /// <inheritdoc />
        public override bool IsServer
        {
            get
            {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
                return NetworkRunnerComponent != null && NetworkRunnerComponent.IsRunning && NetworkRunnerComponent.IsServer;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public override bool IsClient
        {
            get
            {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
                return NetworkRunnerComponent != null && NetworkRunnerComponent.IsRunning && NetworkRunnerComponent.IsClient;
#else
                return false;
#endif
            }
        }

        /// <inheritdoc />
        public override UxrNetworkCapabilities Capabilities => UxrNetworkCapabilities.NetworkTransform | UxrNetworkCapabilities.NetworkRigidbody;

        /// <inheritdoc />
        public override string NetworkRigidbodyWarning => "Photon Fusion's NetworkRigidbody components are meant to be used in Client/Server mode. If you plan to use Photon Fusion in Shared mode, do not set up NetworkRigidbody components here. Don't worry! UltimateXR will still synchronize grabbable physics-driven rigidbodies using RPC calls to try to keep the same position/velocity on all users.";

        /// <inheritdoc />
        public override void SetupGlobal(UxrNetworkManager networkManager, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

#if ULTIMATEXR_USE_PHOTONFUSION2_SDK && UNITY_EDITOR
            Component newComponent = networkManager.GetComponent<NetworkRunner>();

            if (newComponent == null)
            {
                newComponent = Undo.AddComponent<NetworkRunner>(networkManager.gameObject);
                Undo.RegisterFullObjectHierarchyUndo(networkManager.gameObject, "Setup Photon Component");
            }

            newComponents.Add(newComponent);

#endif
        }

        /// <inheritdoc />
        public override void SetupAvatar(UxrAvatar avatar, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

            if (avatar == null)
            {
            }

#if ULTIMATEXR_USE_PHOTONFUSION2_SDK && UNITY_EDITOR
            UxrPhotonFusion2Avatar fusionAvatar = avatar.GetOrAddComponent<UxrPhotonFusion2Avatar>();
            newComponents.Add(fusionAvatar);

            NetworkObject avatarNetworkObject = avatar.gameObject.GetOrAddComponent<NetworkObject>();

            if (avatarNetworkObject)
            {
                avatarNetworkObject.Flags |= NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;
                newComponents.Add(avatarNetworkObject);
            }
            /*
            // Photon Fusion in client-server mode allows NetworkTransform position/rot setting only from the server.
            // We need to create dummies for synching. 
            GameObject networkAvatarRoot = new GameObject("NetworkAvatarRoot");
            Undo.RegisterCreatedObjectUndo(networkAvatarRoot, "Create network avatar root");
            Undo.SetTransformParent(networkAvatarRoot.transform, avatar.transform, "Parent network avatar root");
            networkAvatarRoot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
*/
            GameObject networkCamera = new GameObject("NetworkCamera");
            Undo.RegisterCreatedObjectUndo(networkCamera, "Create avatar network camera");
            Undo.SetTransformParent(networkCamera.transform, avatar.transform, "Parent network camera");
            networkCamera.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject networkHandLeft = new GameObject("NetworkHandLeft");
            Undo.RegisterCreatedObjectUndo(networkHandLeft, "Create avatar network hand left");
            Undo.SetTransformParent(networkHandLeft.transform, avatar.transform, "Parent network hand left");
            networkHandLeft.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject networkHandRight = new GameObject("NetworkHandRight");
            Undo.RegisterCreatedObjectUndo(networkHandRight, "Create avatar network hand right");
            Undo.SetTransformParent(networkHandRight.transform, avatar.transform, "Parent network hand right");
            networkHandRight.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            List<Behaviour> avatarComponents    = SetupNetworkTransform(avatar.gameObject, true, UxrNetworkTransformFlags.ChildAll);
            List<Behaviour> cameraComponents    = SetupNetworkTransform(networkCamera,     true, UxrNetworkTransformFlags.ChildPositionAndRotation);
            List<Behaviour> leftHandComponents  = SetupNetworkTransform(networkHandLeft,   true, UxrNetworkTransformFlags.ChildTransform);
            List<Behaviour> rightHandComponents = SetupNetworkTransform(networkHandRight,  true, UxrNetworkTransformFlags.ChildTransform);

            newComponents.AddRange(avatarComponents.ToList().Concat(cameraComponents).Concat(leftHandComponents).Concat(rightHandComponents));
            newGameObjects.AddRange(new[] { networkHandLeft, networkHandRight, networkCamera });

            fusionAvatar.SetupDummyNetworkTransforms(networkCamera, networkHandLeft, networkHandRight);

            Undo.RegisterFullObjectHierarchyUndo(avatar.gameObject, "Setup Fusion 2 Avatar");
#endif
        }

        /// <inheritdoc />
        public override void SetupPostProcess(List<UxrAvatar> avatarPrefabs)
        {
        }

        /// <inheritdoc />
        public override List<Behaviour> AddNetworkTransform(GameObject gameObject, bool worldSpace, UxrNetworkTransformFlags networkTransformFlags)
        {
            List<Behaviour> newComponents = new List<Behaviour>();

#if ULTIMATEXR_USE_PHOTONFUSION2_SDK && UNITY_EDITOR
            if (networkTransformFlags.HasFlag(UxrNetworkTransformFlags.ChildTransform) == false)
            {
                NetworkObject networkObject = gameObject.GetOrAddComponent<NetworkObject>();
                newComponents.Add(networkObject);
            }

            NetworkTransform networkTransform = gameObject.GetOrAddComponent<NetworkTransform>();
            // NetworkTransform works always in local space
            // DisableSharedModeInterpolation is for objects that are updated inside Update() instead of FixedUpdateNetwork(). 
            networkTransform.DisableSharedModeInterpolation = true;
            newComponents.Add(networkTransform);
#endif

            return newComponents;
        }

        /// <inheritdoc />
        public override List<Behaviour> AddNetworkRigidbody(GameObject gameObject, bool worldSpace, UxrNetworkRigidbodyFlags networkRigidbodyFlagsFlags)
        {
            List<Behaviour> newComponents = new List<Behaviour>();

#if ULTIMATEXR_USE_PHOTONFUSION2_SDK && UNITY_EDITOR
            NetworkObject networkObject = gameObject.GetOrAddComponent<NetworkObject>();
            
            //NetworkRigidbody3D networkRigidbody = gameObject.GetOrAddComponent<NetworkRigidbody3D>();
            //networkRigidbody.InterpolationSpace = worldSpace ? Spaces.World : Spaces.Local;

            newComponents.Add(networkObject);
            //yield return networkRigidbody;
#endif

            return newComponents;
        }

        /// <inheritdoc />
        public override void EnableNetworkTransform(GameObject gameObject, bool enable)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            NetworkTransform[] networkTransforms = gameObject.GetComponentsInChildren<NetworkTransform>();
            networkTransforms.ForEach(nt => nt.SetEnabled(enable));
#endif
        }

        /// <inheritdoc />
        public override void EnableNetworkRigidbody(GameObject gameObject, bool enable)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            /*
            NetworkRigidbody[] networkRigidbodies = gameObject.GetComponentsInChildren<NetworkRigidbody>();
            networkRigidbodies.ForEach(nrb => nrb.SetEnabled(enable));*/
#endif
        }

        /// <inheritdoc />
        public override bool HasAuthority(GameObject gameObject)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            if (NetworkRunnerComponent == null)
            {
                return false;
            }

            NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();

            if (networkObject == null)
            {
                return false;
            }

            return networkObject.HasStateAuthority;
#else
            return false;
#endif
        }

        /// <inheritdoc />
        public override void RequestAuthority(GameObject gameObject)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            if (NetworkRunnerComponent && NetworkRunnerComponent.GameMode == GameMode.Shared)
            {
                NetworkObject networkObject = gameObject.GetComponent<NetworkObject>();

                if (networkObject)
                {
                    networkObject.RequestStateAuthority();
                }
            }
#endif
        }

        /// <inheritdoc />
        public override void CheckReassignGrabAuthority(GameObject gameObject)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            UxrGrabbableObject grabbableObject = gameObject.GetComponent<UxrGrabbableObject>();
            NetworkObject      networkObject   = gameObject.GetComponent<NetworkObject>();

            if (networkObject != null && grabbableObject != null)
            {
                UxrAvatar avatarAuthority = UxrAvatar.EnabledComponents.FirstOrDefault(a => a.GetComponent<NetworkObject>() != null && a.GetComponent<NetworkObject>().StateAuthority == networkObject.StateAuthority);

                if (avatarAuthority == null || !UxrGrabManager.Instance.IsBeingGrabbedBy(grabbableObject, avatarAuthority))
                {
                    // No avatar has authority or the avatar that grabbed it doesn't have it anymore. Change authority to first one.

                    UxrAvatar firstAvatar = UxrGrabManager.Instance.GetGrabbingHands(grabbableObject).First().Avatar;

                    if (firstAvatar == UxrAvatar.LocalAvatar)
                    {
                        UxrNetworkManager.Instance.RequestAuthority(gameObject);
                    }
                }
            }
#endif
        }

        /// <inheritdoc />
        public override bool HasNetworkTransformSyncComponents(GameObject gameObject)
        {
#if ULTIMATEXR_USE_PHOTONFUSION2_SDK
            return gameObject.GetComponent<NetworkTransform>() != null; // || gameObject.GetComponent<NetworkRigidbody>() != null;
#else
            return false;
#endif
        }

        #endregion

#if ULTIMATEXR_USE_PHOTONFUSION2_SDK

        #region INetworkRunnerCallbacks

        /// <inheritdoc />
        public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        /// <inheritdoc />
        public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
        }

        /// <inheritdoc />
        public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnPlayerJoined)} PlayerId = {player.PlayerId}");
            }

            if (!_usePrototypingUI)
            {
                return;
            }

            if (CurrentGameMode == GameMode.Single || CurrentGameMode == GameMode.Server || CurrentGameMode == GameMode.Host || (CurrentGameMode == GameMode.AutoHostOrClient && NetworkRunnerComponent.IsServer))
            {
                SpawnPlayer(runner, player);
            }

            if (CurrentGameMode == GameMode.Shared && player == NetworkRunnerComponent.LocalPlayer)
            {
                SpawnPlayer(runner, player);
            }
        }

        /// <inheritdoc />
        public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnPlayerLeft)} PlayerId = {player.PlayerId}");
            }

            if (!_usePrototypingUI)
            {
                return;
            }

            if (CurrentGameMode == GameMode.Single || CurrentGameMode == GameMode.Server || CurrentGameMode == GameMode.Host)
            {
                TryDespawnPlayer(runner, player);
            }

            if (CurrentGameMode == GameMode.Shared && player == NetworkRunnerComponent.LocalPlayer)
            {
                // Avatar has "destroy when state authority leaves" enabled. 
            }
        }

        /// <inheritdoc />
        public void OnInput(NetworkRunner runner, NetworkInput input)
        {
            UxrAvatar localAvatar = UxrAvatar.LocalAvatar;

            if (localAvatar == null)
            {
                return;
            }
            
#if UNITY_EDITOR
            if (!EditorApplication.isFocused)
            {
                return;
            }
#endif
            Matrix4x4  inverseTm        = Matrix4x4.TRS(UxrPhotonFusion2Avatar.LocalAvatarWorldPosition, UxrPhotonFusion2Avatar.LocalAvatarWorldRotation, Vector3.one).inverse;
            Quaternion inverseAvatarRot = inverseTm.rotation;
            
            s_lastAvatarInput = new UxrPhotonFusion2AvatarInput
                                {
                                    IsSmooth                     = UxrPhotonFusion2Avatar.LocalAvatarPosDataIsSmoothLocomotion,
                                    AvatarPosition               = UxrPhotonFusion2Avatar.LocalAvatarWorldPosition,
                                    AvatarRotation               = UxrPhotonFusion2Avatar.LocalAvatarWorldRotation,
                                    LocalAvatarCameraPosition    = inverseTm.MultiplyPoint(localAvatar.CameraComponent.transform.position),
                                    LocalAvatarCameraRotation    = inverseAvatarRot * localAvatar.CameraComponent.transform.rotation,
                                    LocalAvatarLeftHandPosition  = inverseTm.MultiplyPoint(localAvatar.LeftHandBone.position),
                                    LocalAvatarLeftHandRotation  = inverseAvatarRot * localAvatar.LeftHandBone.rotation,
                                    LocalAvatarRightHandPosition = inverseTm.MultiplyPoint(localAvatar.RightHandBone.position),
                                    LocalAvatarRightHandRotation = inverseAvatarRot * localAvatar.RightHandBone.rotation
                                };

            s_hasLastAvatarInput = true;

            input.Set(s_lastAvatarInput);
        }

        /// <inheritdoc />
        public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            if (player == runner.LocalPlayer && s_hasLastAvatarInput)
            {
                 input.Set(s_lastAvatarInput);
            }
        }

        /// <inheritdoc />
        public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
            {
                Debug.LogWarning($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnShutdown)} Reason: {shutdownReason}");
            }

            _spawnedAvatars.Clear();
            ResetLastAvatarInput();
        }

        /// <inheritdoc />
        public void OnConnectedToServer(NetworkRunner runner)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnConnectedToServer)}");
            }
        }

        /// <inheritdoc />
        public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnDisconnectedFromServer)}. Reason: {reason}");
            }
        }

        /// <inheritdoc />
        public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnConnectRequest)} from {request.RemoteAddress}");
            }
        }

        /// <inheritdoc />
        public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Warnings)
            {
                Debug.LogWarning($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnConnectFailed)} Reason: {reason}");
            }
        }

        /// <inheritdoc />
        public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
        }

        /// <inheritdoc />
        public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
        }

        /// <inheritdoc />
        public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
        }

        /// <inheritdoc />
        public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {
        }

        /// <inheritdoc />
        public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            key.GetInts(out int key0, out int key1, out int key2, out int key3);

            UxrPhotonReliableMsgType msgType           = (UxrPhotonReliableMsgType)key0;     // The msg type
            int?                     exceptDestination = key1 == int.MinValue ? null : key1; // If != int.MinValue, the only player index to ignore for destination. 
            int?                     onlyDestination   = key2 == int.MinValue ? null : key2; // If != int.MinValue, the only player to send it to.
            int                      isToPlayer        = key3;                               // Whether the msg is being sent to the player directly (1), or to the server for broadcast (0).

            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnReliableDataReceived)}(): MsgType {msgType}, isToPlayer = {isToPlayer}, IsServer? {IsServer}.");
            }

            foreach (PlayerRef dstPlayer in GetDestinationPlayers(exceptDestination, onlyDestination))
            {
                if (isToPlayer != 0)
                {
                    // Target is player
                    foreach (UxrAvatar avatar in UxrAvatar.AllComponents)
                    {
                        UxrPhotonFusion2Avatar fusionAvatar = avatar.GetComponent<UxrPhotonFusion2Avatar>();

                        if (fusionAvatar != null && fusionAvatar.Object.InputAuthority == dstPlayer)
                        {
                            switch (msgType)
                            {
                                case UxrPhotonReliableMsgType.LoadGlobalState:
                                    fusionAvatar.LoadGlobalState(data.Array);
                                    break;

                                case UxrPhotonReliableMsgType.LoadAvatarState:
                                    fusionAvatar.LoadAvatarState(data.Array);
                                    break;
                            }
                        }
                    }
                }
                else
                {
                    // Target is server. Relay to player(s).
                    ReliableKey newKey = ReliableKey.FromInts((int)msgType, exceptDestination ?? int.MinValue, onlyDestination ?? int.MinValue, 1);
                    NetworkRunnerComponent.SendReliableDataToPlayer(dstPlayer, newKey, data.Array);
                }
            }
        }

        /// <inheritdoc />
        public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
        }

        /// <inheritdoc />
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(OnSceneLoadDone)}");
            }
        }

        /// <inheritdoc />
        public void OnSceneLoadStart(NetworkRunner runner)
        {
        }

        #endregion

        #region Public Properties

        /// <summary>
        ///     Gets the network runner.
        /// </summary>
        public NetworkRunner NetworkRunnerComponent { get; private set; }

        /// <summary>
        ///     Gets the current game mode if the .
        /// </summary>
        public GameMode CurrentGameMode { get; private set; } = GameMode.Single;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sends big chunks of custom data from a player to other players. Data too big for RPCs will be sent using this
        ///     method. If Shared mode is used, it will be sent directly. In a client-server topology, it will be sent to the
        ///     server and from there to the players.
        /// </summary>
        /// <param name="msgType">The type of message</param>
        /// <param name="data">The data</param>
        /// <param name="exceptDestination">
        ///     If non-null, the data will be sent to all players except this one.
        /// </param>
        /// <param name="onlyDestination">If non-null, the data will be sent only to this player</param>
        /// <remarks>
        ///     <paramref name="exceptDestination" /> has priority over <paramref name="onlyDestination" />. If both are
        ///     null, the data will be sent to all players except the local player.
        /// </remarks>
        public void BroadcastReliableData(UxrPhotonReliableMsgType msgType, byte[] data, int? exceptDestination, int? onlyDestination)
        {
            // Params:
            // MsgType
            // If != int.MinValue, the only player index to ignore for destination. 
            // If != int.MinValue, the only player to send it to.
            // Whether the msg is being sent to the player directly (1), or to the server for broadcast (0). 
            ReliableKey key = ReliableKey.FromInts((int)msgType, exceptDestination ?? int.MinValue, onlyDestination ?? int.MinValue, CurrentGameMode == GameMode.Shared ? 1 : 0);

            switch (msgType)
            {
                case UxrPhotonReliableMsgType.LoadGlobalState:
                case UxrPhotonReliableMsgType.LoadAvatarState:

                    if (CurrentGameMode == GameMode.Shared)
                    {
                        foreach (PlayerRef player in GetDestinationPlayers(exceptDestination, onlyDestination))
                        {
                            NetworkRunnerComponent.SendReliableDataToPlayer(player, key, data);
                        }
                    }
                    else
                    {
                        NetworkRunnerComponent.SendReliableDataToServer(key, data);
                    }

                    break;

                default:

                    if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                    {
                        Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(BroadcastReliableData)}(): Unknown msg type {msgType}.");
                    }

                    break;
            }
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Gets the network runner.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (!enabled)
            {
                return;
            }

            NetworkRunnerComponent = gameObject.GetComponent<NetworkRunner>();

            if (NetworkRunnerComponent == null)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Can't get network runner. Is Photon selected in the {nameof(UxrNetworkManager)}?");
                }
            }
        }

        /// <summary>
        ///     Shows the connection UI if its enabled.
        /// </summary>
        private void OnGUI()
        {
            if (!_usePrototypingUI)
            {
                return;
            }

            if (NetworkRunnerComponent != null && NetworkRunnerComponent.IsRunning)
            {
                return;
            }

            int labelHeight  = 25;
            int buttonWidth  = 200;
            int buttonHeight = 40;
            int posY         = 0;

            GUI.Box(new Rect(0, 0, Screen.width, Screen.height), string.Empty);

            GUI.Box(new Rect(0, posY, buttonWidth, buttonHeight), "UltimateXR Photon Fusion");
            posY += buttonHeight;

            if (NetworkRunnerComponent != null && NetworkRunnerComponent.IsStarting)
            {
                GUI.Box(new Rect(0, posY, buttonWidth, labelHeight), "Starting network");
                return;
            }

            GUI.Box(new Rect(0, posY, buttonWidth, labelHeight), "Select Room Name:");
            posY += labelHeight;

            _roomName =  GUI.TextField(new Rect(0, posY, buttonWidth, labelHeight), _roomName);
            posY      += buttonHeight;

            if (GUI.Button(new Rect(0, posY, buttonWidth, buttonHeight), "No Multiplayer"))
            {
                _ = new UxrTaskController(ct => StartPrototypeSession(GameMode.Single), true);
            }

            posY += buttonHeight;

            if (GUI.Button(new Rect(0, posY, buttonWidth, buttonHeight), "Start Host"))
            {
                _ = new UxrTaskController(ct => StartPrototypeSession(GameMode.Host), true);
            }

            posY += buttonHeight;

            if (GUI.Button(new Rect(0, posY, buttonWidth, buttonHeight), "Start Client"))
            {
                _ = new UxrTaskController(ct => StartPrototypeSession(GameMode.Client), true);
            }

            posY += buttonHeight;

            if (GUI.Button(new Rect(0, posY, buttonWidth, buttonHeight), "Auto Host/Client"))
            {
                _ = new UxrTaskController(ct => StartPrototypeSession(GameMode.AutoHostOrClient), true);
            }

            posY += buttonHeight;

            if (GUI.Button(new Rect(0, posY, buttonWidth, buttonHeight), "Start Shared"))
            {
                _ = new UxrTaskController(ct => StartPrototypeSession(GameMode.Shared), true);
            }
        }

        #endregion
        
        #region Internal Methods

        /// <summary>
        ///     Resets the local avatar input.
        /// </summary>
        internal static void ResetLastAvatarInput()
        {
            s_hasLastAvatarInput = false;
            s_lastAvatarInput    = default;
        }
        
        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets all the players that should receive a message.
        /// </summary>
        /// <param name="exceptDestination">
        ///     If non-null, the data will be sent to all players except this one.
        /// </param>
        /// <param name="onlyDestination">If non-null, the data will be sent only to this player</param>
        /// <returns>The players</returns>
        /// <remarks>
        ///     <paramref name="exceptDestination" /> has priority over <paramref name="onlyDestination" />. If both are
        ///     null, the data will be sent to all players except the local player.
        /// </remarks>
        private IEnumerable<PlayerRef> GetDestinationPlayers(int? exceptDestination, int? onlyDestination)
        {
            if (exceptDestination != null)
            {
                PlayerRef ignoreDestination = PlayerRef.FromIndex(exceptDestination.Value);

                foreach (PlayerRef player in NetworkRunnerComponent.ActivePlayers)
                {
                    if (player != ignoreDestination)
                    {
                        yield return player;
                    }
                }
            }
            else if (onlyDestination != null)
            {
                yield return PlayerRef.FromIndex(onlyDestination.Value);
            }
            else
            {
                foreach (PlayerRef player in NetworkRunnerComponent.ActivePlayers)
                {
                    if (player != NetworkRunnerComponent.LocalPlayer)
                    {
                        yield return player;
                    }
                }
            }
        }

        /// <summary>
        ///     Starts a multi-user session for prototyping.
        /// </summary>
        /// <param name="mode">The game mode</param>
        private async Task StartPrototypeSession(GameMode mode)
        {
            if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
            {
                Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(StartPrototypeSession)} in mode {mode}");
            }

            CurrentGameMode                     = mode;
            NetworkRunnerComponent.ProvideInput = true;

            INetworkSceneManager networkSceneManager = null;

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(NetworkSceneManagerDefault);

                if (type != null)
                {
                    networkSceneManager = gameObject.AddComponent(type) as INetworkSceneManager;
                    break;
                }
            }

            if (!SceneManager.GetActiveScene().IsValid())
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Can't start session because GetActiveScene() returns invalid scene.");
                }
                
                return;
            }

            if (SceneManager.GetActiveScene().buildIndex == -1)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Can't start session because {SceneManager.GetActiveScene().path} was not found in the build scene list. If the scene is there, this is likely a bug in Unity Multiplayer PlayMode. Try disabling the Player 2 checkbox to close its window, save the scene, and open it again.");
                }

                return;
            }

            var      sceneInfo = new NetworkSceneInfo();
            SceneRef scene     = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

            if (scene.IsValid)
            {
                sceneInfo.AddSceneRef(scene);
            }

            await NetworkRunnerComponent.StartGame(new StartGameArgs
                                           {
                                                       GameMode     = mode,
                                                       SessionName  = _roomName,
                                                       Scene        = sceneInfo,
                                                       SceneManager = networkSceneManager
                                           });
        }

        /// <summary>
        ///     Spawns a player's avatar.
        /// </summary>
        /// <param name="runner">The network runner</param>
        /// <param name="player">The player to spawn the avatar for</param>
        private void SpawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            UxrAvatar firstAvatarPrefab = UxrNetworkManager.Instance.RegisteredAvatarPrefabs.FirstOrDefault();

            if (firstAvatarPrefab == null)
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Can't spawn avatar prefab. Register avatars in {nameof(UxrNetworkManager)} first.");
                }
            }
            else
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
                {
                    Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(SpawnPlayer)} Spawning player for PlayerId = {player.PlayerId} in {CurrentGameMode} mode");
                }

                NetworkObject playerObject = runner.Spawn(firstAvatarPrefab.gameObject, Vector3.zero, Quaternion.identity, player);
                _spawnedAvatars.Add(player, playerObject);
            }
        }

        /// <summary>
        ///     Tries to despawn a player.
        /// </summary>
        /// <param name="runner">Network runner</param>
        /// <param name="player">The player to despawn</param>
        private void TryDespawnPlayer(NetworkRunner runner, PlayerRef player)
        {
            if (_spawnedAvatars.TryGetValue(player, out NetworkObject networkObject))
            {
                if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Relevant)
                {
                    Debug.Log($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonFusion2Network)}.{nameof(TryDespawnPlayer)} Despawning player for PlayerId = {player.PlayerId} in {CurrentGameMode} mode");
                }

                runner.Despawn(networkObject);
                _spawnedAvatars.Remove(player);
            }
        }

        #endregion

        #region Private Data

        private const string NetworkSceneManagerDefault = "Fusion.NetworkSceneManagerDefault";

        private static UxrPhotonFusion2AvatarInput s_lastAvatarInput;
        private static bool                        s_hasLastAvatarInput;

        private          string                               _roomName       = "TestRoom";
        private readonly Dictionary<PlayerRef, NetworkObject> _spawnedAvatars = new Dictionary<PlayerRef, NetworkObject>();

        #endregion

#endif
    }
}

#pragma warning restore 414