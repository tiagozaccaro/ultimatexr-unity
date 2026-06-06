// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPhotonVoiceNetwork.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if ULTIMATEXR_USE_PHOTONFUSION_SDK || ULTIMATEXR_USE_PHOTONFUSION2_SDK
using System;
using System.Collections;
using UltimateXR.Core.Settings;
using UltimateXR.Extensions.Unity;
using Fusion;
#endif
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
#endif

namespace UltimateXR.Networking.Integrations.Voice.PhotonVoice
{
    /// <summary>
    ///     Implementation of networking voice support using Photon Fusion.
    /// </summary>
    public class UxrPhotonVoiceNetwork : UxrNetworkVoiceImplementation
    {
        #region Public Overrides UxrNetworkVoiceImplementation

        /// <inheritdoc />
        public override string SdkName => UxrConstants.SdkPhotonVoice;

        /// <inheritdoc />
        public override IEnumerable<string> CompatibleNetworkSDKs
        {
            get
            {
                yield return UxrConstants.SdkPhotonFusion;
                yield return UxrConstants.SdkPhotonFusion2;
            }
        }

        /// <inheritdoc />
        public override bool IsLocalMicSubscribed =>
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            _isLocalMicSubscribed;
#else
            false;
#endif

        /// <inheritdoc />
        public override bool IsMicMuted =>
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            _isMicMuted;
#else
            false;
#endif

        /// <inheritdoc />
        public override bool SuppressLocalMicDataWhileMuted =>
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            _suppressLocalMicDataWhileMuted;
#else
            false;
#endif

        /// <inheritdoc />
        public override void SetupGlobal(string networkingSdk, UxrNetworkManager networkManager, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

#if (ULTIMATEXR_USE_PHOTONFUSION_SDK || ULTIMATEXR_USE_PHOTONFUSION2_SDK) && ULTIMATEXR_USE_PHOTONVOICE_SDK && UNITY_EDITOR

            Component runner = networkManager.CreatedGlobalComponents.FirstOrDefault(g => g.GetComponent<NetworkRunner>() != null);

            if (runner)
            {
                GameObject recorderObject = new GameObject("Recorder");
                Undo.RegisterCreatedObjectUndo(recorderObject, "Create Photon Voice Support GameObject");
                Undo.SetTransformParent(recorderObject.transform, runner.transform, "Parent Photon Voice GameObject");
                recorderObject.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                FusionVoiceClient voiceClientComponent = Undo.AddComponent<FusionVoiceClient>(runner.gameObject);
                Recorder          recorderComponent    = Undo.AddComponent<Recorder>(recorderObject);

                voiceClientComponent.UseFusionAppSettings = true;
                voiceClientComponent.UseFusionAuthValues  = true;
                voiceClientComponent.PrimaryRecorder      = recorderComponent;

                Undo.RegisterFullObjectHierarchyUndo(runner.gameObject, "Setup Photon GameObject");

                newGameObjects.Add(recorderObject);
                newComponents.Add(recorderComponent);
                newComponents.Add(voiceClientComponent);
            }
            else
            {
                Debug.LogError($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonVoiceNetwork)}.{nameof(SetupGlobal)} Cannot find {nameof(NetworkRunner)} to set up.");
            }

#endif
        }

        /// <inheritdoc />
        public override void SetupAvatar(string networkingSdk, UxrAvatar avatar, out List<GameObject> newGameObjects, out List<Component> newComponents)
        {
            newGameObjects = new List<GameObject>();
            newComponents  = new List<Component>();

#if (ULTIMATEXR_USE_PHOTONFUSION_SDK || ULTIMATEXR_USE_PHOTONFUSION2_SDK) && ULTIMATEXR_USE_PHOTONVOICE_SDK && UNITY_EDITOR

            Camera cameraComponent = avatar.CameraComponent;

            if (cameraComponent != null)
            {
                GameObject photonVoice = new GameObject("PhotonVoice");
                Undo.RegisterCreatedObjectUndo(photonVoice, "Create Photon Voice Support GameObject");
                Undo.SetTransformParent(photonVoice.transform, cameraComponent.transform, "Parent Photon Voice GameObject");
                photonVoice.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                Component voiceNetworkObjectComponent = avatar.GetOrAddComponent<VoiceNetworkObject>();
                Component speakerComponent            = photonVoice.GetOrAddComponent<Speaker>();
                Component audioSourceComponent        = photonVoice.GetOrAddComponent<AudioSource>();

                Undo.RegisterCompleteObjectUndo(avatar.gameObject, "Setup Photon Voice");
                Undo.RegisterFullObjectHierarchyUndo(cameraComponent.gameObject, "Setup Photon Voice");

                newGameObjects.Add(photonVoice);

                newComponents.Add(voiceNetworkObjectComponent);
                newComponents.Add(speakerComponent);
                newComponents.Add(audioSourceComponent);
            }
            else if (UxrGlobalSettings.Instance.LogLevelNetworking >= UxrLogLevel.Errors)
            {
                Debug.LogError($"{UxrConstants.NetworkingModule} {nameof(UxrPhotonVoiceNetwork)}.{nameof(SetupAvatar)} Cannot find {nameof(Camera)} on avatar to set up voice components.");
            }

#endif
        }

        /// <inheritdoc />
        public override void SubscribeLocalMic()
        {
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            if (_isLocalMicSubscribed)
            {
                return;
            }

            _micDeviceName = null;
            _micSampleRate = AudioSettings.outputSampleRate;
            _micClip       = Microphone.Start(_micDeviceName, true, 1, _micSampleRate);

            if (_micClip == null)
            {
                Debug.LogWarning($"{nameof(UxrPhotonVoiceNetwork)}: Microphone.Start returned null.");
                return;
            }

            _micLastSamplePos     = 0;
            _micReadBuffer        = new float[_micSampleRate];
            _isLocalMicSubscribed = true;
            _micPollCoroutine     = StartCoroutine(PollMicrophoneCoroutine());
#endif
        }

        /// <inheritdoc />
        public override void UnsubscribeLocalMic()
        {
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            if (!_isLocalMicSubscribed)
            {
                return;
            }

            _isLocalMicSubscribed = false;

            if (_micPollCoroutine != null)
            {
                StopCoroutine(_micPollCoroutine);
                _micPollCoroutine = null;
            }

            if (Microphone.IsRecording(_micDeviceName))
            {
                Microphone.End(_micDeviceName);
            }

            _micClip       = null;
            _micReadBuffer = null;
#endif
        }

        /// <inheritdoc />
        public override bool SetMicMuted(bool muted, bool suppressLocalMicData)
        {
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            bool changed = _isMicMuted != muted || _suppressLocalMicDataWhileMuted != suppressLocalMicData;

            _isMicMuted                     = muted;
            _suppressLocalMicDataWhileMuted = suppressLocalMicData;

            Recorder recorder = GetPrimaryRecorder();

            if (recorder != null)
            {
                recorder.TransmitEnabled = !muted;
            }
            else
            {
                Debug.LogWarning($"{nameof(UxrPhotonVoiceNetwork)}: No Photon Voice Recorder found. Mic mute state will be applied when a recorder is available.");
            }

            if (changed)
            {
                string transmitState = muted ? "muted" : "unmuted";
                string localDataState = suppressLocalMicData ? "suppressed" : "available";
                Debug.Log($"{nameof(UxrPhotonVoiceNetwork)}: Network microphone transmission {transmitState}. Local microphone data callbacks are {localDataState}.");
            }

            return changed;
#else
            return false;
#endif
        }

        /// <inheritdoc />
        public override IEnumerable<AudioSource> GetActiveRemoteVoiceAudioSources()
        {
#if ULTIMATEXR_USE_PHOTONVOICE_SDK
            foreach (Speaker speaker in FindObjectsByType<Speaker>(FindObjectsSortMode.None))
            {
                AudioSource audioSource = speaker.GetComponent<AudioSource>();

                if (audioSource != null)
                {
                    yield return audioSource;
                }
            }
#else
            return Enumerable.Empty<AudioSource>();
#endif
        }

        #endregion

#if ULTIMATEXR_USE_PHOTONVOICE_SDK

        #region Unity

        /// <summary>
        ///     Subscribes to the <see cref="VoiceConnection.SpeakerLinked" /> event to detect when a remote player's
        ///     voice <see cref="AudioSource" /> becomes available.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            ApplyMicMuteToRecorder();

            VoiceConnection voiceConnection = FindFirstObjectByType<VoiceConnection>();

            if (voiceConnection != null)
            {
                voiceConnection.SpeakerLinked += OnSpeakerLinked;
            }
        }

        /// <summary>
        ///     Unsubscribes from the <see cref="VoiceConnection.SpeakerLinked" /> event.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UnsubscribeLocalMic();

            VoiceConnection voiceConnection = FindFirstObjectByType<VoiceConnection>();

            if (voiceConnection != null)
            {
                voiceConnection.SpeakerLinked -= OnSpeakerLinked;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when a remote player's <see cref="Speaker" /> is linked to a voice stream.
        ///     Raises <see cref="UxrNetworkManager.RemoteVoiceAdded" /> with the associated <see cref="AudioSource" />.
        /// </summary>
        /// <param name="speaker">The speaker that was linked</param>
        private void OnSpeakerLinked(Speaker speaker)
        {
            AudioSource audioSource = speaker.GetComponent<AudioSource>();

            if (audioSource != null)
            {
                UxrNetworkManager.RaiseRemoteVoiceAdded(audioSource);
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Polls Unity's Microphone API each frame and raises <see cref="UxrNetworkVoiceImplementation.LocalMicDataReceived" />
        ///     with the captured PCM data.
        /// </summary>
        /// <returns>Coroutine IEnumerator</returns>
        private IEnumerator PollMicrophoneCoroutine()
        {
            UxrAudioFormat format = new UxrAudioFormat(_micSampleRate, 1);

            while (_isLocalMicSubscribed)
            {
                int currentPos = Microphone.GetPosition(_micDeviceName);

                if (currentPos >= 0 && _micClip != null)
                {
                    int samplesToRead;

                    if (currentPos >= _micLastSamplePos)
                    {
                        samplesToRead = currentPos - _micLastSamplePos;
                    }
                    else
                    {
                        samplesToRead = _micClip.samples - _micLastSamplePos + currentPos;
                    }

                    if (samplesToRead > 0)
                    {
                        if (samplesToRead > _micReadBuffer.Length)
                        {
                            _micReadBuffer = new float[samplesToRead];
                        }

                        _micClip.GetData(_micReadBuffer, _micLastSamplePos);
                        _micLastSamplePos = currentPos;

                        if (!ShouldSuppressLocalMicData)
                        {
                            RaiseLocalMicDataReceived(new ArraySegment<float>(_micReadBuffer, 0, samplesToRead), format);
                        }
                    }
                }

                yield return null;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Applies the current mute state to the Photon Voice recorder, when one is available.
        /// </summary>
        private void ApplyMicMuteToRecorder()
        {
            Recorder recorder = GetPrimaryRecorder();

            if (recorder != null)
            {
                recorder.TransmitEnabled = !_isMicMuted;
            }
        }

        /// <summary>
        ///     Finds the recorder used for network transmission.
        /// </summary>
        private static Recorder GetPrimaryRecorder()
        {
            FusionVoiceClient voiceClient = FindFirstObjectByType<FusionVoiceClient>();

            if (voiceClient != null && voiceClient.PrimaryRecorder != null)
            {
                return voiceClient.PrimaryRecorder;
            }

            return FindFirstObjectByType<Recorder>();
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Whether local microphone callbacks are subscribed.
        /// </summary>
        private bool _isLocalMicSubscribed;

        /// <summary>
        ///     Whether network microphone transmission is muted.
        /// </summary>
        private bool _isMicMuted;

        /// <summary>
        ///     Whether local microphone callbacks are suppressed while muted.
        /// </summary>
        private bool _suppressLocalMicDataWhileMuted;

        /// <summary>
        ///     Unity microphone device name currently being read.
        /// </summary>
        private string _micDeviceName;

        /// <summary>
        ///     Unity microphone clip used for local PCM polling.
        /// </summary>
        private AudioClip _micClip;

        /// <summary>
        ///     Last microphone sample position read.
        /// </summary>
        private int _micLastSamplePos;

        /// <summary>
        ///     Coroutine that polls Unity microphone samples.
        /// </summary>
        private Coroutine _micPollCoroutine;

        /// <summary>
        ///     Sample rate used for local microphone polling.
        /// </summary>
        private int _micSampleRate;

        /// <summary>
        ///     Buffer used to copy microphone samples.
        /// </summary>
        private float[] _micReadBuffer;

        /// <summary>
        ///     Whether local microphone data should be skipped.
        /// </summary>
        private bool ShouldSuppressLocalMicData => _isMicMuted && _suppressLocalMicDataWhileMuted;

        #endregion

#endif
    }
}
