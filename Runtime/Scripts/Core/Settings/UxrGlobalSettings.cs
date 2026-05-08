// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGlobalSettings.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using UltimateXR.Attributes;
using UltimateXR.Core.Components;
using UltimateXR.Core.Serialization;
using UltimateXR.Core.Unique;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Core.Settings
{
    /// <summary>
    ///     UltimateXR global settings. The global settings is stores in a file called UxrGlobalSettings inside a /Resources
    ///     folder so that it can be loaded at runtime.
    ///     It can be accessed using the Tools->UltimateXR Unity menu.
    /// </summary>
    public class UxrGlobalSettings : ScriptableObject
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [StylishFoldout("Log Levels")] private LogLevelParameters   _logLevelParameters   = new LogLevelParameters();
        [SerializeField] [StylishFoldout("Networking")] private NetworkingParameters _networkingParameters = new NetworkingParameters();

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets the global settings.
        /// </summary>
        /// <remarks>
        ///     Global settings must be stored in a file called UxrGlobalSettings inside a /Resources folder.
        ///     If no global settings could be found, default settings are used
        /// </remarks>
        public static UxrGlobalSettings Instance
        {
            get
            {
                if (s_current == null)
                {
                    s_current = Resources.Load<UxrGlobalSettings>(nameof(UxrGlobalSettings));

                    if (s_current == null)
                    {
                        s_current = CreateInstance<UxrGlobalSettings>();
                    }
                }
                
                return s_current;
            }
        }

        /// <summary>
        ///     Gets or sets the log level for animation events.
        /// </summary>
        public UxrLogLevel LogLevelAnimation
        {
            get => _logLevelParameters._logLevelAnimation;
            set => _logLevelParameters._logLevelAnimation = value;
        }

        /// <summary>
        ///     Gets or sets the log level for avatar events.
        /// </summary>
        public UxrLogLevel LogLevelAvatar
        {
            get => _logLevelParameters._logLevelAvatar;
            set => _logLevelParameters._logLevelAvatar = value;
        }

        /// <summary>
        ///     Gets or sets the log level for core events.
        /// </summary>
        public UxrLogLevel LogLevelCore
        {
            get => _logLevelParameters._logLevelCore;
            set => _logLevelParameters._logLevelCore = value;
        }

        /// <summary>
        ///     Gets or sets the log level for controller input device events.
        /// </summary>
        public UxrLogLevel LogLevelDevices
        {
            get => _logLevelParameters._logLevelDevices;
            set => _logLevelParameters._logLevelDevices = value;
        }

        /// <summary>
        ///     Gets or sets the log level for locomotion events.
        /// </summary>
        public UxrLogLevel LogLevelLocomotion
        {
            get => _logLevelParameters._logLevelLocomotion;
            set => _logLevelParameters._logLevelLocomotion = value;
        }

        /// <summary>
        ///     Gets or sets the log level for manipulation events.
        /// </summary>
        public UxrLogLevel LogLevelManipulation
        {
            get => _logLevelParameters._logLevelManipulation;
            set => _logLevelParameters._logLevelManipulation = value;
        }

        /// <summary>
        ///     Gets or sets the log level for networking events.
        /// </summary>
        public UxrLogLevel LogLevelNetworking
        {
            get => _logLevelParameters._logLevelNetworking;
            set => _logLevelParameters._logLevelNetworking = value;
        }

        /// <summary>
        ///     Gets or sets the log level for rendering events.
        /// </summary>
        public UxrLogLevel LogLevelRendering
        {
            get => _logLevelParameters._logLevelRendering;
            set => _logLevelParameters._logLevelRendering = value;
        }

        /// <summary>
        ///     Gets or sets the log level for UI events.
        /// </summary>
        public UxrLogLevel LogLevelUI
        {
            get => _logLevelParameters._logLevelUI;
            set => _logLevelParameters._logLevelUI = value;
        }

        /// <summary>
        ///     Gets or sets the log level for weapon events.
        /// </summary>
        public UxrLogLevel LogLevelWeapons
        {
            get => _logLevelParameters._logLevelWeapons;
            set => _logLevelParameters._logLevelWeapons = value;
        }

        /// <summary>
        ///     Gets or sets the format of the network message that contains the initial state of the scene upon joining.
        /// </summary>
        public UxrSerializationFormat NetFormatInitialState
        {
            get => _networkingParameters._netFormatInitialState;
            set => _networkingParameters._netFormatInitialState = value;
        }

        /// <summary>
        ///     Gets or sets the format of the network messages to synchronize state changes.
        /// </summary>
        public UxrSerializationFormat NetFormatStateSync
        {
            get => _networkingParameters._netFormatStateSync;
            set => _networkingParameters._netFormatStateSync = value;
        }

        /// <summary>
        ///     Gets or sets the amount of debug info sent together with unique IDs through the network.
        ///     During development this can help to fix missing IDs by providing the name or path of the object that is missing.
        /// </summary>
        public UxrUniqueIdDebugLevel NetUniqueIdDebugLevel
        {
            get => _networkingParameters._netUniqueIdDebugLevel;
            set => _networkingParameters._netUniqueIdDebugLevel = value;
        }

        /// <summary>
        ///     Gets or sets whether to manually sync physics-driven grabbable objects that do not have native networking
        ///     components such as NetworkTransform/NetworkRigidbody.
        /// </summary>
        public bool SyncGrabbablePhysics
        {
            get => _networkingParameters._syncGrabbablePhysics;
            set => _networkingParameters._syncGrabbablePhysics = value;
        }

        /// <summary>
        ///     Gets or sets, when using <see cref="SyncGrabbablePhysics" />, the interval in seconds synchronization messages are
        ///     sent.
        /// </summary>
        public float GrabbableSyncIntervalSeconds
        {
            get => _networkingParameters._grabbableSyncIntervalSeconds;
            set => _networkingParameters._grabbableSyncIntervalSeconds = value;
        }

        /// <summary>
        ///     Gets or sets, when using <see cref="UxrSyncObject" />, the interval in seconds sync transform RPC messages are sent.
        /// </summary>
        public float SyncNetworkTransformIntervalSeconds
        {
            get => _networkingParameters._transformSyncIntervalSeconds;
            set => _networkingParameters._transformSyncIntervalSeconds = value;
        }

        #endregion

        #region Public Methods

#if UNITY_EDITOR

        /// <summary>
        ///     Accesses the global settings using the Unity menu.
        /// </summary>
        [MenuItem(UxrConstants.Editor.MenuPathRoot + "Global Settings", priority = UxrConstants.Editor.PriorityMenuPathRoot)]
        public static void SelectInProject()
        {
            UxrGlobalSettings settings = Resources.Load<UxrGlobalSettings>(nameof(UxrGlobalSettings));

            if (settings == null)
            {
                if (EditorUtility.DisplayDialog("Create global settings?", "Global settings file has not yet been created. Create one now?", "Yes", "Cancel"))
                {
                    settings = CreateInstance<UxrGlobalSettings>();

                    string resourcesFolder = "Resources";
                    string directory       = $"{Application.dataPath}/{resourcesFolder}/";

                    if (!File.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    AssetDatabase.CreateAsset(settings, $"Assets/{resourcesFolder}/{nameof(UxrGlobalSettings)}.asset");
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();
                }
                else
                {
                    return;
                }
            }

            Selection.activeObject = settings;
        }

#endif

        #endregion

        #region Unity

        private void OnValidate()
        {
            _logLevelParameters ??= new LogLevelParameters();
            _networkingParameters ??= new NetworkingParameters();
        }

        #endregion

        #region Private Types & Data

        [Serializable]
        private class LogLevelParameters
        {
            [Tooltip(AnimationLogLevelToolTip)]    [SerializeField] public UxrLogLevel _logLevelAnimation    = UxrLogLevel.Warnings;
            [Tooltip(AvatarLogLevelToolTip)]       [SerializeField] public UxrLogLevel _logLevelAvatar       = UxrLogLevel.Warnings;
            [Tooltip(CoreLogLevelToolTip)]         [SerializeField] public UxrLogLevel _logLevelCore         = UxrLogLevel.Relevant;
            [Tooltip(DevicesLogLevelToolTip)]      [SerializeField] public UxrLogLevel _logLevelDevices      = UxrLogLevel.Relevant;
            [Tooltip(LocomotionLogLevelToolTip)]   [SerializeField] public UxrLogLevel _logLevelLocomotion   = UxrLogLevel.Relevant;
            [Tooltip(ManipulationLogLevelToolTip)] [SerializeField] public UxrLogLevel _logLevelManipulation = UxrLogLevel.Relevant;
            [Tooltip(NetworkingLogLevelToolTip)]   [SerializeField] public UxrLogLevel _logLevelNetworking   = UxrLogLevel.Relevant;
            [Tooltip(RenderingLogLevelToolTip)]    [SerializeField] public UxrLogLevel _logLevelRendering    = UxrLogLevel.Relevant;
            [Tooltip(UiLogLevelToolTip)]           [SerializeField] public UxrLogLevel _logLevelUI           = UxrLogLevel.Warnings;
            [Tooltip(WeaponsLogLevelToolTip)]      [SerializeField] public UxrLogLevel _logLevelWeapons      = UxrLogLevel.Relevant;

            public const string AnimationLogLevelToolTip    = "Selects the console log level for animation events.";
            public const string AvatarLogLevelToolTip       = "Selects the console log level for avatar events.";
            public const string CoreLogLevelToolTip         = "Selects the console log level for core events.";
            public const string DevicesLogLevelToolTip      = "Selects the console log level for device events.";
            public const string LocomotionLogLevelToolTip   = "Selects the console log level for locomotion events.";
            public const string ManipulationLogLevelToolTip = "Selects the console log level for manipulation events.";
            public const string NetworkingLogLevelToolTip   = "Selects the console log level for networking events.";
            public const string RenderingLogLevelToolTip    = "Selects the console log level for rendering events.";
            public const string UiLogLevelToolTip           = "Selects the console log level for UI events.";
            public const string WeaponsLogLevelToolTip      = "Selects the console log level for weapon events.";
        }

        [Serializable]
        private class NetworkingParameters
        {
            [Tooltip(                                              NetFormatInitialStateToolTip)] [SerializeField]                              public UxrSerializationFormat _netFormatInitialState        = UxrSerializationFormat.BinaryGzip;
            [Tooltip(                                              NetFormatStateSyncToolTip)] [SerializeField]                                 public UxrSerializationFormat _netFormatStateSync           = UxrSerializationFormat.BinaryUncompressed;
            [Tooltip(                                              NetUniqueIdDebugLevelToolTip)] [SerializeField]                              public UxrUniqueIdDebugLevel  _netUniqueIdDebugLevel        = UxrUniqueIdDebugLevel.NoDebug;
            [Header("UxrGrabbableObject")] [Tooltip(               SyncGrabbablePhysicsToolTip)] [SerializeField]                               public bool                   _syncGrabbablePhysics         = true;
            [ShowIf(nameof(_syncGrabbablePhysics), true)] [Tooltip(GrabbableSyncIntervalSecondsToolTip)] [SerializeField][Range( 0.1f,  10.0f)] public float                  _grabbableSyncIntervalSeconds = UxrConstants.Networking.DefaultGrabbableSyncIntervalSeconds;
            [Header("UxrSyncObject")] [Tooltip(                    TransformSyncIntervalSecondsToolTip)] [SerializeField] [Range(0.01f, 1.0f)]  public float                  _transformSyncIntervalSeconds = UxrConstants.Networking.DefaultNetworkTransformSyncIntervalSeconds;

            public const string NetFormatInitialStateToolTip        = "Selects the message format used when the host sends the initial scene state to joining clients. Compression uses a little extra CPU but reduces bandwidth.";
            public const string NetFormatStateSyncToolTip           = "Selects the message format used for regular state synchronization updates. Compression uses a little extra CPU but reduces bandwidth.";
            public const string NetUniqueIdDebugLevelToolTip        = "Selects the amount of debug information sent together with unique IDs through the network. During development this can help to diagnose missing IDs by showing object names or paths.";
            public const string SyncGrabbablePhysicsToolTip         = "Enables synchronization using regular RPC messages for physics-driven UxrGrabbableObjects that do not use native networking components such as NetworkTransform or NetworkRigidbody.";
            public const string GrabbableSyncIntervalSecondsToolTip = "When grabbable physics sync is enabled, this is the interval in seconds between synchronization messages. Lower values increase update frequency and bandwidth usage.";
            public const string TransformSyncIntervalSecondsToolTip = "When using UxrSyncObject that have network transform synchronization enabled, this is the interval in seconds between transform synchronization messages. Lower values increase update frequency and bandwidth usage.";
        }

        private static UxrGlobalSettings s_current;

        #endregion
    }
}