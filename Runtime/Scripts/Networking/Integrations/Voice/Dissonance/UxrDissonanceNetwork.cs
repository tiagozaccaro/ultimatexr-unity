// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrDissonanceNetwork.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;
#if ULTIMATEXR_USE_DISSONANCE_SDK && UNITY_EDITOR
using UnityEditor;
#endif
#if ULTIMATEXR_USE_DISSONANCE_SDK
using System;
using Dissonance;
using Dissonance.Audio.Capture;
using Dissonance.Audio.Playback;
using NAudio.Wave;
#endif

namespace UltimateXR.Networking.Integrations.Voice.Dissonance
{
    /// <summary>
    ///     Implementation of networking voice support using Dissonance.
    /// </summary>
    public class UxrDissonanceNetwork : UxrNetworkVoiceImplementation
    {
        #region Public Overrides UxrNetworkVoiceImplementation

        /// <inheritdoc />
        public override string SdkName => UxrConstants.SdkDissonance;

        /// <inheritdoc />
        public override IEnumerable<string> CompatibleNetworkSDKs
        {
            get
            {
                yield return UxrConstants.SdkFishNet;
                yield return UxrConstants.SdkMirror;
                yield return UxrConstants.SdkPhotonFusion;
                yield return UxrConstants.SdkUnityNetCode;
            }
        }

        /// <inheritdoc />
        public override bool IsLocalMicSubscribed =>
#if ULTIMATEXR_USE_DISSONANCE_SDK
            _isLocalMicSubscribed;
#else
            false;
#endif

        /// <inheritdoc />
        public override void SetupGlobal(string networkingSdk, UxrNetworkManager networkManager, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

#if ULTIMATEXR_USE_DISSONANCE_SDK && UNITY_EDITOR

            if (string.IsNullOrEmpty(networkingSdk))
            {
                return;
            }

            string     setupPrefabGuid = null;
            GameObject setupInstance   = null;

            if (string.Equals(networkingSdk, UxrConstants.SdkFishNet))
            {
                Debug.LogWarning($"{UxrConstants.NetworkingModule} FishNet Dissonance integration package doesn't come with a prefab and components should be added manually. We're working on a pull request to add integration seamlessly.");
            }
            else if (string.Equals(networkingSdk, UxrConstants.SdkMirror))
            {
                setupPrefabGuid = "1264c01c7f8182e47ac9f784af03d895";
            }
            else if (string.Equals(networkingSdk, UxrConstants.SdkPhotonFusion))
            {
                setupPrefabGuid = "803e2767acc738a4498f245ae19bb598";
            }
            else if (string.Equals(networkingSdk, UxrConstants.SdkUnityNetCode))
            {
                setupPrefabGuid = "2c50758a6d3b8114a8ce30a2fd9e4380";
            }

            if (setupPrefabGuid != null)
            {
                setupInstance = PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(setupPrefabGuid))) as GameObject;

                if (setupInstance == null)
                {
                    Debug.LogError($"{UxrConstants.NetworkingModule} Could not find the {UxrConstants.SdkDissonance} setup prefab for {networkingSdk}. Check for the {networkingSdk} integration here: https://placeholder-software.co.uk/dissonance/docs/Basics/Getting-Started.html");

                }
                else
                {
                    Undo.RegisterCreatedObjectUndo(setupInstance, "Create Dissonance GameObject");
                }
            }

            if (setupInstance != null)
            {
                VoiceBroadcastTrigger broadcastTrigger = Undo.AddComponent<VoiceBroadcastTrigger>(setupInstance);
                VoiceReceiptTrigger   receiptTrigger   = Undo.AddComponent<VoiceReceiptTrigger>(setupInstance);

                broadcastTrigger.ChannelType = CommTriggerTarget.Room;
                broadcastTrigger.RoomName    = "Global";
                receiptTrigger.RoomName      = "Global";

                Undo.RegisterFullObjectHierarchyUndo(setupInstance, "Setup Dissonance GameObject");

                newGameObjects.Add(setupInstance);
                newComponents.Add(broadcastTrigger);
                newComponents.Add(receiptTrigger);
            }
#endif
        }

        /// <inheritdoc />
        public override void SetupAvatar(string networkingSdk, UxrAvatar avatar, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

            // No setup required
        }

        /// <inheritdoc />
        public override void SubscribeLocalMic()
        {
#if ULTIMATEXR_USE_DISSONANCE_SDK
            if (_isLocalMicSubscribed)
            {
                return;
            }

            DissonanceComms comms = _comms != null ? _comms : FindFirstObjectByType<DissonanceComms>();

            if (comms == null)
            {
                Debug.LogWarning($"{nameof(UxrDissonanceNetwork)}: No DissonanceComms found. Cannot subscribe to local mic.");
                return;
            }

            IMicrophoneCapture capture = comms.GetComponent<IMicrophoneCapture>();

            if (capture == null)
            {
                Debug.LogWarning($"{nameof(UxrDissonanceNetwork)}: No IMicrophoneCapture found on DissonanceComms.");
                return;
            }

            _micAdapter           = new MicSubscriberAdapter(this);
            _micCapture           = capture;
            _micCapture.Subscribe(_micAdapter);
            _isLocalMicSubscribed = true;
#endif
        }

        /// <inheritdoc />
        public override void UnsubscribeLocalMic()
        {
#if ULTIMATEXR_USE_DISSONANCE_SDK
            if (!_isLocalMicSubscribed)
            {
                return;
            }

            if (_micCapture != null && _micAdapter != null)
            {
                try
                {
                    _micCapture.Unsubscribe(_micAdapter);
                }
                catch
                {
                    // Best effort.
                }
            }

            _micCapture           = null;
            _micAdapter           = null;
            _isLocalMicSubscribed = false;
#endif
        }

        /// <inheritdoc />
        public override IEnumerable<AudioSource> GetActiveRemoteVoiceAudioSources()
        {
#if ULTIMATEXR_USE_DISSONANCE_SDK
            foreach (VoicePlayback vp in FindObjectsByType<VoicePlayback>(FindObjectsSortMode.None))
            {
                if (vp.AudioSource != null)
                {
                    yield return vp.AudioSource;
                }
            }
#else
            return Enumerable.Empty<AudioSource>();
#endif
        }

        #endregion

#if ULTIMATEXR_USE_DISSONANCE_SDK

        #region Unity

        /// <summary>
        ///     Subscribes to the <see cref="DissonanceComms.OnPlayerJoinedSession" /> event to detect when a remote player's
        ///     voice <see cref="AudioSource" /> becomes available.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            DissonanceComms comms = FindFirstObjectByType<DissonanceComms>();

            if (comms != null)
            {
                _comms                        =  comms;
                _comms.OnPlayerJoinedSession  += OnPlayerJoinedSession;
            }
        }

        /// <summary>
        ///     Unsubscribes from the <see cref="DissonanceComms.OnPlayerJoinedSession" /> event.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UnsubscribeLocalMic();

            if (_comms != null)
            {
                _comms.OnPlayerJoinedSession -= OnPlayerJoinedSession;
                _comms                        = null;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when a remote player joins the voice session.
        ///     Raises <see cref="UxrNetworkManager.RemoteVoiceAdded" /> with the associated <see cref="AudioSource" />.
        /// </summary>
        /// <param name="playerState">The voice player state for the player that joined</param>
        private void OnPlayerJoinedSession(VoicePlayerState playerState)
        {
            if (playerState.IsLocalPlayer)
            {
                return;
            }

            if (playerState.Playback is VoicePlayback playback)
            {
                AudioSource audioSource = playback.AudioSource;

                if (audioSource != null)
                {
                    UxrNetworkManager.RaiseRemoteVoiceAdded(audioSource);
                }
            }
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Adapter that bridges Dissonance's <see cref="IMicrophoneSubscriber" /> to
        ///     <see cref="UxrNetworkVoiceImplementation.RaiseLocalMicDataReceived" />.
        /// </summary>
        private sealed class MicSubscriberAdapter : IMicrophoneSubscriber
        {
            private readonly UxrDissonanceNetwork _owner;

            public MicSubscriberAdapter(UxrDissonanceNetwork owner)
            {
                _owner = owner;
            }

            public void ReceiveMicrophoneData(ArraySegment<float> buffer, WaveFormat format)
            {
                _owner.RaiseLocalMicDataReceived(buffer, new UxrAudioFormat(format.SampleRate, format.Channels));
            }

            public void Reset()
            {
            }
        }

        private DissonanceComms      _comms;
        private MicSubscriberAdapter _micAdapter;
        private IMicrophoneCapture   _micCapture;
        private bool                 _isLocalMicSubscribed;

        #endregion

#endif
    }
}
