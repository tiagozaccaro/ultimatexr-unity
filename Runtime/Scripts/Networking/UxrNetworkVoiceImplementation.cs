// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrNetworkVoiceImplementation.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core.Components;
using UnityEngine;

namespace UltimateXR.Networking
{
    /// <summary>
    ///     Base class required to add support for a network voice communication SDK.
    /// </summary>
    public abstract class UxrNetworkVoiceImplementation : UxrComponent, IUxrNetworkVoiceImplementation
    {
        #region Implicit IUxrNetworkSdk

        /// <inheritdoc />
        public abstract string SdkName { get; }

        #endregion

        #region Implicit IUxrNetworkVoiceImplementation

        /// <inheritdoc />
        public abstract IEnumerable<string> CompatibleNetworkSDKs { get; }

        /// <inheritdoc />
        public event Action<ArraySegment<float>, UxrAudioFormat> LocalMicDataReceived;

        /// <inheritdoc />
        public abstract bool IsLocalMicSubscribed { get; }

        /// <inheritdoc />
        public abstract bool IsMicMuted { get; }

        /// <inheritdoc />
        public abstract bool SuppressLocalMicDataWhileMuted { get; }

        /// <inheritdoc />
        public abstract void SetupGlobal(string networkingSdk, UxrNetworkManager networkManager, out List<GameObject> newGameObjects, out List<Component> newComponents);

        /// <inheritdoc />
        public abstract void SetupAvatar(string networkingSdk, UxrAvatar avatar, out List<GameObject> newGameObjects, out List<Component> newComponents);

        /// <inheritdoc />
        public abstract void SubscribeLocalMic();

        /// <inheritdoc />
        public abstract void UnsubscribeLocalMic();

        /// <inheritdoc />
        public abstract bool SetMicMuted(bool muted, bool suppressLocalMicData);

        /// <inheritdoc />
        public abstract IEnumerable<AudioSource> GetActiveRemoteVoiceAudioSources();

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Raises the <see cref="LocalMicDataReceived" /> event.
        /// </summary>
        /// <param name="buffer">The audio sample buffer</param>
        /// <param name="format">The audio format</param>
        protected void RaiseLocalMicDataReceived(ArraySegment<float> buffer, UxrAudioFormat format)
        {
            LocalMicDataReceived?.Invoke(buffer, format);
        }

        #endregion
    }
}
