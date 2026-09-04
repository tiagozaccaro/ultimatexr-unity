// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrManager.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Avatar;
using UltimateXR.Avatar.Controllers;
using UltimateXR.Core.Caching;
using UltimateXR.Core.Components;
using UltimateXR.Core.Components.Singleton;
using UltimateXR.Core.Serialization;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSave;
using UltimateXR.Core.StateSync;
using UltimateXR.Core.Unique;
using UltimateXR.Exceptions;
using UltimateXR.Extensions.System.IO;
using UltimateXR.Extensions.System.Threading;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Math;
using UltimateXR.Extensions.Unity.Render;
using UltimateXR.Guides;
using UltimateXR.Locomotion;
using UltimateXR.Manipulation;
using UltimateXR.Mechanics.Weapons;
using UltimateXR.Networking;
using UltimateXR.UI.UnityInputModule;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateXR.Core
{
    /// <summary>
    ///     <para>
    ///         Main manager in the UltimateXR framework. As a <see cref="UxrSingleton{T}">UxrSingleton</see> it can be
    ///         accessed from any point in the application using <see cref="UxrSingleton{T}.Instance">UxrManager.Instance</see>
    ///         .
    ///         It can be pre-instantiated in the scene in order to change default parameters through the inspector but it is
    ///         not required. When accessing the global <see cref="UxrSingleton{T}.Instance">UxrManager.Instance</see>, if no
    ///         <see cref="UxrManager" /> is currently available, one will be instantiated in the scene as the global
    ///         Singleton.
    ///     </para>
    ///     <para>
    ///         <see cref="UxrManager" /> is responsible for updating all key framework entities such as avatars each frame in
    ///         the correct order. Events and callbacks are provided so that custom updates can be executed at appropriate
    ///         stages of the updating process.
    ///     </para>
    ///     <para>
    ///         <see cref="UxrManager" /> also provides the following functionality:
    ///         <list type="bullet">
    ///             <item>
    ///                 Pre-caching prefabs when scenes are loaded to eliminate hiccups using the
    ///                 <see cref="IUxrPrecacheable" /> interface.
    ///             </item>
    ///             <item>Moving/rotating/teleporting avatars.</item>
    ///             <item>Events to get notified when avatars have been moved/rotated/teleported.</item>
    ///             <item>
    ///                 Events to get notified before and after updating a frame and at different stages of the updating
    ///                 process for finer control: <see cref="AvatarsUpdating" />/<see cref="AvatarsUpdated" /> and
    ///                 <see cref="StageUpdating" />/<see cref="StageUpdated" />.
    ///             </item>
    ///             <item>
    ///                 A single event to get notified of all state changes in any component in the framework or
    ///                 any custom user class: <see cref="ComponentStateChanged" />. Also a way to execute back state change
    ///                 events, helping implement network synchronization and save-to-file/replay functionality:
    ///                 <see cref="ExecuteStateSyncEvent" /> .
    ///             </item>
    ///             <item>
    ///                 Provide ways to save the current state of the scene and load it back, helping implement
    ///                 sync-on-join networking functionality and save-to-file/replays: <see cref="SaveStateChanges" /> and
    ///                 <see cref="LoadStateChanges" />.
    ///             </item>
    ///         </list>
    ///     </para>
    /// </summary>
    public sealed class UxrManager : UxrSingleton<UxrManager>
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private UxrPostUpdateMode _postUpdateMode = UxrPostUpdateMode.LateUpdate;
        [SerializeField] private bool _usePrecaching = true;
        [SerializeField] private bool _useAvatarFadeIn = true;
        [SerializeField] private int _precacheFrameCount = 50;

        #endregion

        #region Public Types & Data

        // Events

        /// <summary>
        ///     Invoked to notify a state change in a <see cref="IUxrStateSync" /> component. This can be used to synchronize the
        ///     event over the network or capture it in a replay/save state system. <br />
        ///     The <see cref="IUxrStateSync" /> interface is implemented in the <see cref="UxrComponent" /> base class,
        ///     serving as the foundation for all components within UltimateXR. While this interface is readily available in
        ///     <see cref="UxrComponent" />, users may also implement it in custom classes where inheritance from
        ///     <see cref="UxrComponent" /> is not feasible due to limitations in multiple inheritance.
        ///     <see cref="UxrStateSyncImplementer{T}" /> helps leverage this interface implementation.<br />
        ///     <see cref="UxrSyncEventArgs.SerializeEventBinary" /> is used to serialize the event.
        ///     Serialized events can subsequently be executed via <see cref="ExecuteStateSyncEvent" />.
        ///     This streamlined functionality simplifies networking implementation with UltimateXR:
        ///     <list type="bullet">
        ///         <item>
        ///             A central entry point to capture all events in UltimateXR: <see cref="ComponentStateChanged" />
        ///         </item>
        ///         <item>
        ///             A method to serialize the event into a byte array: <see cref="UxrSyncEventArgs.SerializeEventBinary" />
        ///         </item>
        ///         <item>
        ///             A means to execute an event for replicating the state change in another device or session:
        ///             <see cref="ExecuteStateSyncEvent" />
        ///         </item>
        ///     </list>
        ///     By default, nested state changes are ignored to optimize bandwidth usage and prevent redundant calls that
        ///     might result in inconsistencies.<br />
        ///     Since the root state change already triggers the nested ones, there's no need to synchronize them again.
        ///     For additional details, refer to <see cref="UxrManager.UseTopLevelStateChangesOnly" />.
        /// </summary>
        public static event Action<IUxrStateSync, UxrSyncEventArgs> ComponentStateChanged;

        /// <summary>
        ///     Called right before precaching is about to start. It's called on the first frame that is displayed black.
        ///     See <see cref="UsePrecaching" />.
        /// </summary>
        public static event Action PrecachingStarting;

        /// <summary>
        ///     Called right after precaching finished. It's called on the first frame that starts to fade-in from black.
        ///     See <see cref="UsePrecaching" />.
        /// </summary>
        public static event Action PrecachingFinished;

        /// <summary>
        ///     Called right before processing all update stages in the current frame. Equivalent to <see cref="StageUpdating" />
        ///     for <see cref="UxrUpdateStage.Update" />
        /// </summary>
        public static event Action AvatarsUpdating;

        /// <summary>
        ///     Called right after processing all update stages in the current frame. Equivalent to <see cref="StageUpdated" /> for
        ///     <see cref="UxrUpdateStage.PostProcess" />
        /// </summary>
        public static event Action AvatarsUpdated;

        /// <summary>
        ///     Called right before an update stage in the current frame. See <see cref="UxrUpdateStage" />.
        /// </summary>
        public static event Action<UxrUpdateStage> StageUpdating;

        /// <summary>
        ///     Called right after an update stage in the current frame. See <see cref="UxrUpdateStage" />.
        /// </summary>
        public static event Action<UxrUpdateStage> StageUpdated;

        /// <summary>
        ///     Gets or sets whether the <see cref="ComponentStateChanged" /> event will be triggered by top level
        ///     synchronization calls only. It is true by default to avoid redundant calls and inconsistencies.
        ///     When false, they will also be triggered by nested changes.
        /// </summary>
        public static bool UseTopLevelStateChangesOnly { get; set; } = true;

        /// <summary>
        ///     Gets whether the manager is currently pre-caching. This happens right after the local avatar is enabled and
        ///     <see cref="UsePrecaching" /> is set.
        /// </summary>
        public bool IsPrecaching => _precacheCoroutine != null;

        /// <summary>
        ///     Gets whether the local avatar is being teleported, including in/out smooth transitions.
        /// </summary>
        public bool IsTeleportingLocalAvatar => _teleportCoroutine != null;

        /// <summary>
        ///     Gets whether the manager is currently inside a StateSync call executed using <see cref="ExecuteStateSyncEvent" />.
        /// </summary>
        public bool IsInsideStateSync { get; private set; }

        /// <summary>
        ///     Gets whether the manager is currently inside <see cref="LoadStateChanges" />.
        /// </summary>
        public bool IsInsideLoadStateChanges { get; private set; }

        // Properties

        /// <summary>
        ///     Gets or sets when to perform the post-update. The post-update updates among others the avatar animation (hand
        ///     poses, manipulation mechanics and Inverse Kinematics).
        ///     It is <see cref="UxrPostUpdateMode.LateUpdate" /> by default to make sure they are played on top of any animation
        ///     generated by Unity built-in animation components like <see cref="Animator" />.
        /// </summary>
        public UxrPostUpdateMode PostUpdateMode
        {
            get => _postUpdateMode;
            set => _postUpdateMode = value;
        }

        /// <summary>
        ///     Gets or sets whether the manager uses pre-caching. Pre-caching happens right after the local avatar is enabled and
        ///     consists of instantiating objects described in all <see cref="IUxrPrecacheable" /> components in the scene. These
        ///     objects are placed right in front of the camera while it is faded black, so that they can't be seen, which forces
        ///     their resources to be loaded in order to reduce hiccups when they need to be instantiated during the session. After
        ///     that they are deleted and the scene is faded in.
        /// </summary>
        public bool UsePrecaching
        {
            get => _usePrecaching;
            set => _usePrecaching = value;
        }

        /// <summary>
        ///     Gets or sets the number of frames pre-cached objects are shown. These frames are drawn in black and right after the
        ///     scene will fade in, so that pre-caching is hidden to the user.
        /// </summary>
        public int PrecacheFrameCount
        {
            get => _precacheFrameCount;
            set => _precacheFrameCount = value;
        }

        /// <summary>
        ///     Gets or sets whether to automatically use a fade-in from black right after a new scene is loaded. The fade-in is
        ///     only available if <see cref="UsePrecaching" /> is used.
        /// </summary>
        public bool UseAvatarFadeIn
        {
            get => _useAvatarFadeIn;
            set => _useAvatarFadeIn = value;
        }

        /// <summary>
        ///     Gets or sets the color used when teleporting using screen fading transitions.
        /// </summary>
        public Color TeleportFadeColor { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Given a component that requires an <see cref="UxrAvatar" /> component in the hierarchy in order to work, logs an
        ///     error indicating that it's missing.
        /// </summary>
        /// <param name="component">Component that requires an <see cref="UxrAvatar" /> on its GameObject or any of its parents.</param>
        public static void LogMissingAvatarInHierarchyError(Component component)
        {
            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
            {
                Debug.LogError($"{UxrConstants.CoreModule}: {component.GetType().Name} requires to be part of an {nameof(UxrAvatar)} in order to work correctly. GameObject is {component.GetPathUnderScene()}.");
            }
        }

        /// <summary>
        ///     Given a component that requires an <see cref="UxrAvatar" /> component in the scene in order to work, logs an error
        ///     indicating that it's missing.
        /// </summary>
        /// <param name="component">Component that requires an <see cref="UxrAvatar" /> in the scene.</param>
        public static void LogMissingAvatarInScene(Component component)
        {
            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
            {
                Debug.LogError($"{UxrConstants.CoreModule}: {component.GetType().Name} requires an avatar in the scene to work correctly. GameObject is {component.GetPathUnderScene()}.");
            }
        }

        /// <summary>
        ///     Saves the state of all components. This can be used in multiplayer, save-to-file or replay functionality.
        ///     Components are saved using <see cref="IUxrStateSave.SerializationOrder" /> to control the order in which the
        ///     components will be serialized. The serialization order will determine the deserialization order used by
        ///     <see cref="LoadStateChanges" />.
        /// </summary>
        /// <param name="roots">A list of GameObjects whose hierarchy to serialize or null to serialize the whole scene</param>
        /// <param name="ignoreRoots">A list of GameObjects whose hierarchy to ignore, or null to not ignore anything</param>
        /// <param name="level">The level of changes to serialize</param>
        /// <param name="format">The serialization output format</param>
        /// <param name="debugLevel">
        ///     The debug level. This helps during development by showing the object and component information
        ///     in <see cref="UxrComponentNotFoundException" /> exceptions
        /// </param>
        /// <returns>A data stream that can be saved and loaded back using <see cref="LoadStateChanges" /></returns>
        public byte[] SaveStateChanges(List<GameObject> roots, List<GameObject> ignoreRoots, UxrStateSaveLevel level, UxrSerializationFormat format, UxrUniqueIdDebugLevel debugLevel = UxrUniqueIdDebugLevel.NoDebug)
        {
            int count = 0;
            int totalComponents = 0;

            byte[] bytes = null;
            int headerSize = 0;
            int originalSize = 0;

            UxrUniqueIdDebugLevel oldDebugLevel = BinaryWriterExt.UniqueIdDebugLevel;
            BinaryWriterExt.UniqueIdDebugLevel = debugLevel;

            Stopwatch sw = Stopwatch.StartNew();

            using (MemoryStream stream = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(stream))
            {
                // Write the header: header size, version, format, level and binary serialization version.

                writer.Write((short)0);
                writer.Write((ushort)StateSerializationVersion);
                writer.Write((byte)format);
                writer.Write((byte)level);
                writer.Write((ushort)UxrConstants.Serialization.CurrentBinaryVersion);
                stream.Flush();

                headerSize = (int)stream.Position;

                // Update the header size

                stream.Position = 0;
                writer.Write((short)headerSize);
                stream.Position = headerSize;
                stream.Flush();

                // Write the rest

                using UxrBinarySerializer serializer = new UxrBinarySerializer(writer);
                using MemoryStream componentStream = new MemoryStream();
                using BinaryWriter componentWriter = new BinaryWriter(componentStream);
                using UxrBinarySerializer componentSerializer = new UxrBinarySerializer(componentWriter);

                IEnumerable<IUxrStateSave> components;

                if (roots == null)
                {
                    components = level == UxrStateSaveLevel.Complete ? UxrStateSaveImplementer.AllSerializableComponents : UxrStateSaveImplementer.SaveRequiredComponents;
                }
                else
                {
                    if (level == UxrStateSaveLevel.Complete)
                    {
                        components = roots.SelectMany(go => go.GetComponentsInChildren<IUxrStateSave>(true));
                    }
                    else
                    {
                        components = roots.SelectMany(go => go.GetComponentsInChildren<IUxrStateSave>(true).Where(c => c.Component.isActiveAndEnabled));
                    }
                }

                foreach (IUxrStateSave stateSave in components.OrderBy(c => c.SerializationOrder))
                {
                    bool serialize = ignoreRoots == null || !ignoreRoots.Any(go => stateSave.Transform.HasParent(go.transform));

                    if (!serialize)
                    {
                        continue;
                    }

                    try
                    {
                        // First serialize using DontSerialize option, so that we can check whether any data needs to be saved or we can skip it entirely
                        if (stateSave != null && stateSave.Component != null && stateSave.SerializeState(serializer, level, UxrStateSaveOptions.DontCacheChanges | UxrStateSaveOptions.DontSerialize | UxrStateSaveOptions.DontWriteVersions))
                        {
                            // Now serialize it to the secondary componentStream so that we can know the size beforehand.
                            // Knowing the size beforehand allows writing the size before the component so that, when deserializing, if the component
                            // fails, it can still skip the component and continue with the rest of the data.
                            componentStream.Position = 0;
                            componentStream.SetLength(0);
                            componentWriter.WriteUniqueIdComponent(stateSave);
                            stateSave.SerializeState(componentSerializer, level);
                            componentWriter.Flush();
                            byte[] componentBytes = componentStream.ToArray();

                            // Now write it in the main stream, with the size at the beginning

                            long before = writer.BaseStream.Position;
                            int length = componentBytes.Length;

                            serializer.Serialize(ref length);
                            writer.Write(componentBytes, 0, length);

                            long after = writer.BaseStream.Position;

                            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Debug)
                            {
                                Debug.Log($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(SaveStateChanges)}(): {count + 1}: Serialized {stateSave.Component.name} (type {stateSave.GetType().Name}) to {after - before} bytes. Id is {stateSave.UniqueId}.");
                            }

                            count++;
                        }

                        totalComponents++;
                    }
                    catch (SerializationException e)
                    {
                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
                        {
                            Debug.LogError($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(SaveStateChanges)}(): Error serializing component {stateSave.Component.name} (type {stateSave.GetType().Name}). Most probably this type requires to implement the {nameof(ICloneable)} interface for state saving to work: {e}");
                        }
                    }
                    catch (Exception e)
                    {
                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
                        {
                            Debug.LogError($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(SaveStateChanges)}(): Error serializing component {stateSave.Component.name} (type {stateSave.GetType().Name}): {e}");
                        }
                    }
                }

                stream.Flush();
                bytes = stream.ToArray();
                originalSize = bytes.Length;
            }

            BinaryWriterExt.UniqueIdDebugLevel = oldDebugLevel;

            // Compress if requested

            if (format == UxrSerializationFormat.BinaryGzip)
            {
                using MemoryStream compressedStream = new MemoryStream();

                // Write the header bytes uncompressed
                compressedStream.Write(bytes, 0, headerSize);

                // Compress the remaining bytes and write them to the compressed stream, starting from index after header
                using (GZipStream zipStream = new GZipStream(compressedStream, CompressionMode.Compress, true))
                {
                    zipStream.Write(bytes, headerSize, bytes.Length - headerSize);
                    zipStream.Flush();
                }

                // Get the compressed bytes from the compressed stream
                bytes = compressedStream.ToArray();
            }

            // Log if required

            if ((level > UxrStateSaveLevel.ChangesSincePreviousSave && UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose) ||
                (level <= UxrStateSaveLevel.ChangesSincePreviousSave && UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Debug))
            {
                string rootName = roots != null ? $"{roots.Count} root object(s)" : "scene";
                string compressionInfo = string.Empty;

                if (originalSize != bytes.Length)
                {
                    compressionInfo = $" Compressed from {originalSize} bytes ({(float)originalSize / bytes.Length:0.00} compression ratio).";
                }
                sw.Stop();
                TimeSpan elapsed = sw.Elapsed;
                double milliseconds = (double)elapsed.Ticks / TimeSpan.TicksPerMillisecond;

                Debug.Log($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(SaveStateChanges)}(): Serialized {count}/{totalComponents} component(s) from {rootName} to {bytes.Length} bytes in {milliseconds:F3}ms. Format: {format}, level: {level}.{compressionInfo}");
            }

            return bytes;
        }

        /// <summary>
        ///     Loads the state of component changes from serialized data using <see cref="SaveStateChanges" />.
        /// </summary>
        /// <param name="serializedState">Serialized state</param>
        public void LoadStateChanges(byte[] serializedState)
        {
            if (serializedState == null || serializedState.Length == 0)
            {
                if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Warnings)
                {
                    Debug.LogWarning($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): Input bytes is null or empty.");
                }

                return;
            }

            Stopwatch sw = Stopwatch.StartNew();

            // This avoids propagation of any synchronization message while loading.
            IsInsideLoadStateChanges = true;

            int count = 0;
            long uncompressedLength = -1;
            List<UxrAvatar> loadedAvatars = [];

            // Read the header: header size, version, format, level and serialization version

            using MemoryStream stream = new MemoryStream(serializedState);
            using BinaryReader headerReader = new BinaryReader(stream);
            int headerSize = headerReader.ReadUInt16();
            int stateSerializationVersion = headerReader.ReadUInt16();
            UxrSerializationFormat format = (UxrSerializationFormat)stream.ReadByte();
            UxrStateSaveLevel level = (UxrStateSaveLevel)stream.ReadByte();
            int binarySerializationVersion = headerReader.ReadUInt16();

            // Read the rest

            switch (format)
            {
                case UxrSerializationFormat.BinaryUncompressed: DeserializeUncompressed(stream); break;

                case UxrSerializationFormat.BinaryGzip:
                    {
                        using MemoryStream compressedStream = new MemoryStream(serializedState, headerSize, serializedState.Length - headerSize);
                        using GZipStream gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                        using MemoryStream uncompressedStream = new MemoryStream();
                        gzipStream.CopyTo(uncompressedStream);
                        uncompressedLength = uncompressedStream.Length;
                        uncompressedStream.Position = 0;
                        DeserializeUncompressed(uncompressedStream);
                        break;
                    }

                default:

                    if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
                    {
                        Debug.LogError($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): Serialized data format is unknown ({format}).");
                    }

                    IsInsideLoadStateChanges = false;
                    return;
            }

            void DeserializeUncompressed(Stream inputStream)
            {
                using BinaryReader reader = new BinaryReader(inputStream);
                UxrBinarySerializer serializer = new UxrBinarySerializer(reader, binarySerializationVersion);

                try
                {
                    while (reader.BaseStream.Position < reader.BaseStream.Length)
                    {
                        int componentSize = -1;
                        IUxrUniqueId unique = null;

                        serializer.Serialize(ref componentSize);
                        long posBeforeComponent = reader.BaseStream.Position;

                        try
                        {
                            serializer.SerializeUniqueIdComponent(ref unique);

                            if (unique is IUxrStateSave stateSave)
                            {
                                stateSave.SerializeState(serializer, level);

                                if (unique is UxrAvatar avatar)
                                {
                                    loadedAvatars.Add(avatar);
                                }

                                if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Debug)
                                {
                                    Debug.Log($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): {count + 1}: Deserialized {unique.Component.name} (type {unique.GetType().Name}). Id is {unique.UniqueId}.");
                                }

                                count++;
                            }
                        }
                        catch (Exception e)
                        {
                            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Warnings)
                            {
                                Debug.LogWarning($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): Cannot deserialize a component. Skipping: {e}");
                            }
                        }

                        reader.BaseStream.Seek(posBeforeComponent + componentSize, SeekOrigin.Begin);
                    }
                }
                catch (Exception e)
                {
                    if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
                    {
                        Debug.LogError($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): Error deserializing a component length. Cannot continue with the remaining components: {e}");
                    }
                }
            }

            // When deserializing, make sure to trigger the avatar movement events by using MoveAvatarTo using the current position.

            foreach (UxrAvatar avatar in loadedAvatars)
            {
                MoveAvatarTo(avatar, avatar.CameraFloorPosition);
            }

            // This avoids propagation of any synchronization message while loading.
            IsInsideLoadStateChanges = false;

            if ((level > UxrStateSaveLevel.ChangesSincePreviousSave && UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose) ||
                (level <= UxrStateSaveLevel.ChangesSincePreviousSave && UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Debug))
            {
                string compressionInfo = string.Empty;

                if (uncompressedLength != -1)
                {
                    compressionInfo = $" Compressed from {uncompressedLength} bytes ({(float)uncompressedLength / serializedState.Length:0.00} compression ratio).";
                }

                sw.Stop();
                TimeSpan elapsed = sw.Elapsed;
                double milliseconds = (double)elapsed.Ticks / TimeSpan.TicksPerMillisecond;

                Debug.Log($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(LoadStateChanges)}(): Deserialized {count} components from {serializedState.Length} bytes in {milliseconds:F3}ms. Format: {format}, level: {level}.{compressionInfo}");
            }
        }

        /// <summary>
        ///     Executes a state change, serialized using <see cref="UxrSyncEventArgs.SerializeEventBinary" />, so that it is
        ///     processed by the same component.
        /// </summary>
        /// <param name="serializedEvent">Event serialized using <see cref="UxrSyncEventArgs.SerializeEventBinary" /></param>
        /// <returns>
        ///     The result, containing the target of the event and the event data. If there were errors deserializing the
        ///     data or trying to execute the event, <see cref="UxrStateSyncResult.IsError" /> will return true and
        ///     <see cref="UxrStateSyncResult.ErrorMessage" /> will get the error message.
        /// </returns>
        public UxrStateSyncResult ExecuteStateSyncEvent(byte[] serializedEvent)
        {
            IUxrStateSync stateSync = null;
            UxrSyncEventArgs eventArgs = null;
            string errorMessage = null;

            try
            {
                if (UxrSyncEventArgs.DeserializeEventBinary(serializedEvent, out stateSync, out eventArgs, out errorMessage))
                {
                    errorMessage = null;
                    IsInsideStateSync = true;
                    stateSync.SyncState(eventArgs);
                }
            }
            catch (Exception e)
            {
                errorMessage = e.ToString();
            }
            finally
            {
                IsInsideStateSync = false;
            }

            UxrStateSyncResult result = new UxrStateSyncResult(stateSync, eventArgs, errorMessage);

            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors && result.IsError)
            {
                Debug.LogError($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(ExecuteStateSyncEvent)}(): Error deserializing and processing {serializedEvent.Length} bytes of event data: {result}");
            }
            else if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.CoreModule}: {nameof(UxrManager)}.{nameof(ExecuteStateSyncEvent)}(): Deserialized and processed {serializedEvent.Length} bytes of event data: {result}");
            }

            return result;
        }

        /// <summary>
        ///     Translates an avatar.
        /// </summary>
        /// <param name="avatar">The avatar to translate</param>
        /// <param name="translation">Translation offset</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void TranslateAvatar(UxrAvatar avatar, Vector3 translation, bool propagateEvents = true, object source = null)
        {
            MoveAvatarTo(avatar, avatar.CameraFloorPosition + translation, avatar.ProjectedCameraForward, propagateEvents, source);
        }

        /// <summary>
        ///     Moves an avatar to a new position on the floor, keeping the same viewing direction. The eye level is maintained.
        /// </summary>
        /// <param name="avatar">The avatar to move</param>
        /// <param name="newFloorPosition">
        ///     The position on the floor above which the avatar's camera will be positioned.
        ///     Coordinates need to be specified at ground level since the eye camera level over the floor will be maintained.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void MoveAvatarTo(UxrAvatar avatar, Vector3 newFloorPosition, bool propagateEvents = true, object source = null)
        {
            MoveAvatarTo(avatar, newFloorPosition, avatar.ProjectedCameraForward, propagateEvents, source);
        }

        /// <summary>
        ///     Moves an avatar to a new position on the floor and a viewing direction. The eye level is maintained.
        /// </summary>
        /// <param name="avatar">The avatar to move</param>
        /// <param name="newFloorPosition">
        ///     The position on the floor above which the avatar's camera will be positioned.
        ///     Coordinates need to be specified at ground level since the eye camera level over the floor will be maintained.
        /// </param>
        /// <param name="newForward">The new viewing direction of the avatar, including the camera.</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void MoveAvatarTo(UxrAvatar avatar, Vector3 newFloorPosition, Vector3 newForward, bool propagateEvents = true, object source = null)
        {
            // This method will be synchronized through network
            BeginSync(UxrStateSyncOptions.Network);

            Transform avatarTransform = avatar.transform;

            Vector3 oldPosition = avatarTransform.position;
            Quaternion oldRotation = avatarTransform.rotation;
            Vector3 newPosition = oldPosition;
            Quaternion newRotation = oldRotation;

            TransformExt.ApplyAlignment(ref newPosition, ref newRotation, avatar.CameraFloorPosition, Quaternion.LookRotation(avatar.ProjectedCameraForward), newFloorPosition, Quaternion.LookRotation(newForward));

            OnAvatarMoving(source ?? this, avatar, UxrAvatarMoveEventArgs.GetFromPool(avatar, oldPosition, oldRotation, newPosition, newRotation, source), propagateEvents);
            avatarTransform.SetPositionAndRotation(newPosition, newRotation);

            // We place the EndSyncMethod() before the OnAvatarMoved() so that any synchronized events depending on AvatarMoved don't get nested and are processed instead. 
            EndSyncMethod(SyncParams(avatar, newFloorPosition, newForward, propagateEvents));

            OnAvatarMoved(source ?? this, avatar, UxrAvatarMoveEventArgs.GetFromPool(avatar, oldPosition, oldRotation, newPosition, newRotation, source), propagateEvents);
        }

        /// <summary>
        ///     See <see cref="MoveAvatarTo(UxrAvatar,UnityEngine.Vector3,UnityEngine.Vector3)">MoveAvatarTo</see>.
        /// </summary>
        /// <param name="avatar">The avatar to move</param>
        /// <param name="destination">The position and orientation on the floor</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void MoveAvatarTo(UxrAvatar avatar, Transform destination, bool propagateEvents = true, object source = null)
        {
            if (avatar && destination)
            {
                MoveAvatarTo(avatar, destination.position, destination.forward, propagateEvents, source);
            }
        }

        /// <summary>
        ///     Moves the avatar to a new floor level.
        /// </summary>
        /// <param name="avatar">The avatar to move</param>
        /// <param name="floorLevel">The new floor level (Y)</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void MoveAvatarTo(UxrAvatar avatar, float floorLevel, bool propagateEvents = true, object source = null)
        {
            if (avatar)
            {
                Vector3 newPosition = avatar.CameraFloorPosition;
                newPosition.y = floorLevel;
                MoveAvatarTo(avatar, newPosition, propagateEvents, source);
            }
        }

        /// <summary>
        ///     Rotates the avatar around its vertical axis, where a positive angle turns it to the right and a negative angle to
        ///     the left.
        /// </summary>
        /// <param name="avatar">The avatar to rotate</param>
        /// <param name="degrees">The degrees to rotate</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        public void RotateAvatar(UxrAvatar avatar, float degrees, bool propagateEvents = true, object source = null)
        {
            Transform avatarTransform = avatar.transform;
            MoveAvatarTo(avatar, avatar.CameraFloorPosition, avatar.ProjectedCameraForward.GetRotationAround(avatarTransform.up, degrees), propagateEvents, source);
        }

        /// <summary>
        ///     Teleports the local <see cref="UxrAvatar" />. The local avatar is the avatar controlled by the user using the
        ///     headset and input controllers. Non-local avatars are other avatars instantiated in the scene but not controlled by
        ///     the user, either other users through the network or other scenarios such as automated replays.
        /// </summary>
        /// <param name="newFloorPosition">
        ///     World-space floor-level position the avatar will be teleported over. The camera position will be on top of the
        ///     floor position, keeping the original eye-level.
        /// </param>
        /// <param name="newRotation">
        ///     World-space rotation the avatar will be teleported to. The camera will point in the rotation's forward direction.
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is <see cref="UxrConstants.TeleportTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the teleportation finished. It will receive a boolean parameter telling
        ///     whether the teleport finished completely (true) or was cancelled (false). If a fade effect has been requested, the
        ///     callback is executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Coroutine enumerator</returns>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public void TeleportLocalAvatar(Vector3 newFloorPosition,
                                        Quaternion newRotation,
                                        UxrTranslationType translationType = UxrTranslationType.Snap,
                                        float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                        Action teleportedCallback = null,
                                        Action<bool> finishedCallback = null,
                                        bool propagateEvents = true,
                                        object source = null)
        {
            CheckInterruptTeleportCoroutine();

            _teleportFinishedCallback = finishedCallback;
            _teleportCoroutine = StartCoroutine(TeleportLocalAvatarCoroutine(newFloorPosition, newRotation, translationType, transitionSeconds, teleportedCallback, () => finishedCallback?.Invoke(true), propagateEvents, source));
        }

        /// <summary>
        ///     Teleports the local <see cref="UxrAvatar" /> while making sure to keep relative position/orientation on moving
        ///     objects. Some <paramref name="translationType" /> values have a transition before the teleport to avoid motion
        ///     sickness. On worlds with moving platforms it is important to specify the destination transform so that:
        ///     <list type="bullet">
        ///         <item>Relative position/orientation to the destination is preserved.</item>
        ///         <item>Optionally the local avatar can be parented to the new destination.</item>
        ///     </list>
        ///     The local avatar is the avatar controlled by the user using the headset and input controllers. Non-local avatars
        ///     are other avatars instantiated in the scene but not controlled by the user, either other users through the network
        ///     or other scenarios such as automated replays.
        /// </summary>
        /// <param name="referenceTransform">
        ///     The object the avatar should keep relative position/orientation to. This should be the moving object the avatar has
        ///     teleported on top of
        /// </param>
        /// <param name="parentToReference">
        ///     Whether to parent the avatar to <paramref name="referenceTransform" />. The avatar should be parented if it's being
        ///     teleported to a moving hierarchy it is not part of
        /// </param>
        /// <param name="newFloorPosition">
        ///     World-space floor-level position the avatar will be teleported over. The camera position will be on top of the
        ///     floor position, keeping the original eye-level.
        /// </param>
        /// <param name="newRotation">
        ///     World-space rotation the avatar will be teleported to. The camera will point in the rotation's forward direction.
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is <see cref="UxrConstants.TeleportTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the teleportation finished. It will receive a boolean parameter telling
        ///     whether the teleport finished completely (true) or was cancelled (false). If a fade effect has been requested, the
        ///     callback is executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Coroutine enumerator</returns>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public void TeleportLocalAvatarRelative(Transform referenceTransform,
                                                bool parentToReference,
                                                Vector3 newFloorPosition,
                                                Quaternion newRotation,
                                                UxrTranslationType translationType = UxrTranslationType.Snap,
                                                float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                                Action teleportedCallback = null,
                                                Action<bool> finishedCallback = null,
                                                bool propagateEvents = true,
                                                object source = null)
        {
            CheckInterruptTeleportCoroutine();

            Vector3 newRelativeFloorPosition = referenceTransform != null ? referenceTransform.InverseTransformPoint(newFloorPosition) : newFloorPosition;
            Quaternion newRelativeRotation = referenceTransform != null ? Quaternion.Inverse(referenceTransform.rotation) * newRotation : newRotation;

            _teleportFinishedCallback = finishedCallback;
            _teleportCoroutine = StartCoroutine(TeleportLocalAvatarRelativeCoroutine(referenceTransform, parentToReference, newRelativeFloorPosition, newRelativeRotation, translationType, transitionSeconds, teleportedCallback, () => finishedCallback?.Invoke(true), propagateEvents, source));
        }

        /// <summary>
        ///     <para>
        ///         Asynchronous version of <see cref="TeleportLocalAvatar"> TeleportLocalAvatar</see>.
        ///     </para>
        ///     Teleports the local <see cref="UxrAvatar" />. The local avatar is the avatar controlled by the user using the
        ///     headset and input controllers. Non-local avatars are other avatars instantiated in the scene but not controlled by
        ///     the user, either other users through the network or other scenarios such as automated replays.
        /// </summary>
        /// <param name="newFloorPosition">
        ///     World-space floor-level position the avatar will be teleported over. The camera position will be on top of the
        ///     floor position, keeping the original eye-level.
        /// </param>
        /// <param name="newRotation">
        ///     World-space rotation the avatar will be teleported to. The camera will point in the rotation's forward direction.
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="ct">Optional cancellation token that can be used to cancel the task</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Awaitable <see cref="Task" /> that will finish after the avatar was teleported or if it was cancelled</returns>
        /// <exception cref="TaskCanceledException">Task was canceled using <paramref name="ct" /></exception>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public async Task TeleportLocalAvatarAsync(Vector3 newFloorPosition,
                                                   Quaternion newRotation,
                                                   UxrTranslationType translationType = UxrTranslationType.Snap,
                                                   float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                                   Action teleportedCallback = null,
                                                   CancellationToken ct = default,
                                                   bool propagateEvents = true,
                                                   object source = null)
        {
            CheckInterruptTeleportCoroutine();

            _teleportCoroutine = StartCoroutine(TeleportLocalAvatarCoroutine(newFloorPosition, newRotation, translationType, transitionSeconds, teleportedCallback, null, propagateEvents, source));
            await TaskExt.WaitUntil(() => _teleportCoroutine == null, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(_teleportCoroutine);
                _teleportCoroutine = null;
            }
        }

        /// <summary>
        ///     <para>
        ///         Asynchronous version of <see cref="TeleportLocalAvatarRelative"> TeleportLocalAvatar</see>.
        ///     </para>
        ///     Teleports the local <see cref="UxrAvatar" />. The local avatar is the avatar controlled by the user using the
        ///     headset and input controllers. Non-local avatars are other avatars instantiated in the scene but not controlled by
        ///     the user, either other users through the network or other scenarios such as automated replays.
        /// </summary>
        /// <param name="referenceTransform">
        ///     The object the avatar should keep relative position/orientation to. This should be the moving object the avatar has
        ///     teleported on top of
        /// </param>
        /// <param name="parentToReference">
        ///     Whether to parent the avatar to <paramref name="referenceTransform" />. The avatar should be parented if it's being
        ///     teleported to a moving hierarchy it is not part of
        /// </param>
        /// <param name="newFloorPosition">
        ///     World-space floor-level position the avatar will be teleported over. The camera position will be on top of the
        ///     floor position, keeping the original eye-level.
        /// </param>
        /// <param name="newRotation">
        ///     World-space rotation the avatar will be teleported to. The camera will point in the rotation's forward direction.
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="ct">Optional cancellation token that can be used to cancel the task</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Awaitable <see cref="Task" /> that will finish after the avatar was teleported or if it was cancelled</returns>
        /// <exception cref="TaskCanceledException">Task was canceled using <paramref name="ct" /></exception>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public async Task TeleportLocalAvatarRelativeAsync(Transform referenceTransform,
                                                           bool parentToReference,
                                                           Vector3 newFloorPosition,
                                                           Quaternion newRotation,
                                                           UxrTranslationType translationType = UxrTranslationType.Snap,
                                                           float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                                           Action teleportedCallback = null,
                                                           CancellationToken ct = default,
                                                           bool propagateEvents = true,
                                                           object source = null)
        {
            CheckInterruptTeleportCoroutine();

            Vector3 newRelativeFloorPosition = referenceTransform != null ? referenceTransform.InverseTransformPoint(newFloorPosition) : newFloorPosition;
            Quaternion newRelativeRotation = referenceTransform != null ? Quaternion.Inverse(referenceTransform.rotation) * newRotation : newRotation;

            _teleportCoroutine = StartCoroutine(TeleportLocalAvatarRelativeCoroutine(referenceTransform, parentToReference, newRelativeFloorPosition, newRelativeRotation, translationType, transitionSeconds, teleportedCallback, null, propagateEvents, source));
            await TaskExt.WaitUntil(() => _teleportCoroutine == null, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(_teleportCoroutine);
                _teleportCoroutine = null;
            }
        }

        /// <summary>
        ///     Rotates the local avatar around its vertical axis, where a positive angle turns it to the right and a negative
        ///     angle to the left. The rotation can be performed in different ways using <paramref name="turnType" />.
        /// </summary>
        /// <param name="degrees">The degrees to rotate</param>
        /// <param name="turnType">The type of rotation to use. By default, it will rotate immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="turnType" /> has a duration, it will specify how long the
        ///     rotation transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTurnSeconds" />
        /// </param>
        /// <param name="rotatedCallback">
        ///     Optional callback executed depending on the rotation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTurnType.Snap" />: Right after finishing the rotation.</item>
        ///         <item>
        ///             <see cref="UxrTurnType.Fade" />: When the screen is completely faded out and the avatar has rotated,
        ///             before fading back in. This can be used to enable/disable/change GameObjects in the scene since the screen
        ///             at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTurnType.Interpolate" />: Right after finishing the turn.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the teleportation finished. It will receive a boolean parameter telling
        ///     whether the teleport finished completely (true) or was cancelled (false). If a fade effect has been requested, the
        ///     callback is executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public void RotateLocalAvatar(float degrees,
                                      UxrTurnType turnType = UxrTurnType.Snap,
                                      float transitionSeconds = UxrConstants.Locomotion.DiscreteTurnSeconds,
                                      Action rotatedCallback = null,
                                      Action<bool> finishedCallback = null,
                                      bool propagateEvents = true,
                                      object source = null)
        {
            CheckInterruptTeleportCoroutine();

            _teleportFinishedCallback = finishedCallback;
            _teleportCoroutine = StartCoroutine(RotateLocalAvatarCoroutine(degrees, turnType, transitionSeconds, rotatedCallback, () => finishedCallback?.Invoke(true), propagateEvents, source));
        }

        /// <summary>
        ///     <para>Asynchronous version of <see cref="RotateLocalAvatar" />.</para>
        ///     <para>
        ///         Rotates the local avatar around its vertical axis, where a positive angle turns it to the right and a
        ///         negative angle to the left. The rotation can be performed in different ways using
        ///         <paramref name="turnType" />.
        ///     </para>
        /// </summary>
        /// <param name="degrees">The degrees to rotate</param>
        /// <param name="turnType">The type of rotation to use. By default, it will rotate immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="turnType" /> has a duration, it will specify how long the
        ///     rotation transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTurnSeconds" />
        /// </param>
        /// <param name="rotatedCallback">
        ///     Optional callback executed depending on the rotation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTurnType.Snap" />: Right after finishing the turn.</item>
        ///         <item>
        ///             <see cref="UxrTurnType.Fade" />: When the screen is completely faded out and the avatar has rotated,
        ///             before fading back in. This can be used to enable/disable/change GameObjects in the scene since the screen
        ///             at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTurnType.Interpolate" />: Right after finishing the turn.</item>
        ///     </list>
        /// </param>
        /// <param name="ct">Optional cancellation token to cancel the operation</param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Awaitable <see cref="Task" /> that will finish when the rotation finished</returns>
        public async Task RotateLocalAvatarAsync(float degrees,
                                                 UxrTurnType turnType = UxrTurnType.Snap,
                                                 float transitionSeconds = UxrConstants.Locomotion.DiscreteTurnSeconds,
                                                 Action rotatedCallback = null,
                                                 CancellationToken ct = default,
                                                 bool propagateEvents = true,
                                                 object source = null)
        {
            CheckInterruptTeleportCoroutine();

            _teleportCoroutine = StartCoroutine(RotateLocalAvatarCoroutine(degrees, turnType, transitionSeconds, rotatedCallback, null, propagateEvents, source));
            await TaskExt.WaitUntil(() => _teleportCoroutine == null, ct);

            if (ct.IsCancellationRequested)
            {
                StopCoroutine(_teleportCoroutine);
                _teleportCoroutine = null;
            }
        }

        #endregion

        #region Internal Methods

        /// <summary>
        ///     Registers a new component with the <see cref="IUxrStateSync" /> interface.
        /// </summary>
        /// <param name="component">Custom component</param>
        internal void RegisterStateSyncComponent<T>(Component component) where T : IUxrStateSync
        {
            StateSync_Registered(component as IUxrStateSync);
        }

        /// <summary>
        ///     Removes a component with the <see cref="IUxrStateSync" /> interface, because it was destroyed.
        /// </summary>
        /// <param name="component">Custom component</param>
        internal void UnregisterStateSyncComponent<T>(Component component) where T : IUxrStateSync
        {
            StateSync_Unregistered(component as IUxrStateSync);
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Subscribes to global events.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            UxrAvatar.GlobalEnabled += Avatar_Enabled;
            SceneManager.sceneLoaded += SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded += SceneManager_SceneUnloaded;
            Application.onBeforeRender += Application_OnBeforeRender;

#if UNITY_EDITOR
            EditorApplication.playModeStateChanged += EditorApplication_PlayModeStateChanged;
#endif
            UxrCompass.Instance.Poke();
        }

        /// <summary>
        ///     Unsubscribes from global events.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();

            UxrAvatar.GlobalEnabled -= Avatar_Enabled;
            SceneManager.sceneLoaded -= SceneManager_SceneLoaded;
            SceneManager.sceneUnloaded -= SceneManager_SceneUnloaded;
            Application.onBeforeRender -= Application_OnBeforeRender;

            DestroyPrecachedInstances();
        }

        /// <summary>
        ///     Tries to find Unity canvases (<see cref="Canvas" /> components) and automatically set them up so that they can be
        ///     used by the framework using <see cref="UxrCanvas" />.
        /// </summary>
        protected override void Start()
        {
            if (UxrPointerInputModule.Instance == null && UxrGlobalSettings.Instance.LogLevelUI >= UxrLogLevel.Warnings)
            {
                Debug.LogWarning($"{UxrConstants.UiModule}: no {nameof(EventSystem)} GameObject with a {nameof(UxrPointerInputModule)} component found. Add an {nameof(EventSystem)} using the menu GameObject->UI->EventSystem and add an {nameof(UxrPointerInputModule)} to it to use the Unity UI using UltimateXR");
            }

            SetupCanvases();
        }

        /// <summary>
        ///     Updates the key entities to the current frame. If the <see cref="PostUpdateMode" /> is set to
        ///     <see cref="UxrPostUpdateMode.Update" />, all the animation (hand poses, manipulation mechanics and Inverse
        ///     Kinematics) will also be updated.
        /// </summary>
        private void Update()
        {
            OnUpdating();
            OnUpdatingStage(UxrUpdateStage.Update);

            if (IsEnabledController(UxrAvatar.LocalAvatar))
            {
                UxrAvatarUpdateEventArgs e = UxrAvatarUpdateEventArgs.GetFromPool(UxrAvatar.LocalAvatar, UxrUpdateStage.Update);

                OnAvatarUpdating(e);
                ((IUxrAvatarControllerUpdater)UxrAvatar.LocalAvatar.AvatarController).UpdateAvatar();
                OnAvatarUpdated(e);
            }

            OnStageUpdated(UxrUpdateStage.Update);

            if (PostUpdateMode == UxrPostUpdateMode.Update)
            {
                PostUpdate();
            }
        }

        /// <summary>
        ///     Updates the key entities to the current frame. If the <see cref="PostUpdateMode" /> is set to
        ///     <see cref="UxrPostUpdateMode.LateUpdate" />, all the animation (hand poses, manipulation mechanics and Inverse
        ///     Kinematics) will also be updated.
        /// </summary>
        private void LateUpdate()
        {
            if (PostUpdateMode == UxrPostUpdateMode.LateUpdate)
            {
                PostUpdate();
            }

            UxrStateSaveImplementer.NotifyEndOfFrame();
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Public teleporting coroutine that can be yielded from an external coroutine.
        ///     Teleports the local <see cref="UxrAvatar" />. The local avatar is the avatar controlled by the user using the
        ///     headset and input controllers. Non-local avatars are other avatars instantiated in the scene but not controlled by
        ///     the user, either other users through the network or other scenarios such as automated replays.
        /// </summary>
        /// <param name="newFloorPosition">
        ///     Floor-level position the avatar will be teleported over. The camera position will be on top of the floor position,
        ///     keeping the original eye-level.
        /// </param>
        /// <param name="newRotation">
        ///     Rotation the avatar will be teleported to. The camera will point in the rotation's forward
        ///     direction
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the teleportation finished. If a fade effect has been requested, the
        ///     callback is executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Coroutine enumerator</returns>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public IEnumerator TeleportLocalAvatarCoroutine(Vector3 newFloorPosition,
                                                        Quaternion newRotation,
                                                        UxrTranslationType translationType = UxrTranslationType.Snap,
                                                        float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                                        Action teleportedCallback = null,
                                                        Action finishedCallback = null,
                                                        bool propagateEvents = true,
                                                        object source = null)
        {
            yield return TeleportLocalAvatarRelativeCoroutine(null, false, newFloorPosition, newRotation, translationType, transitionSeconds, teleportedCallback, finishedCallback, propagateEvents, source);
        }

        /// <summary>
        ///     Public teleporting coroutine that can be yielded from an external coroutine.
        ///     Teleports the local <see cref="UxrAvatar" /> while making sure to keep relative position/orientation on moving
        ///     objects. Some <paramref name="translationType" /> values have a transition before the teleport to avoid motion
        ///     sickness. On worlds with moving platforms it is important to specify the destination transform so that:
        ///     <list type="bullet">
        ///         <item>Relative position/orientation to the destination is preserved.</item>
        ///         <item>Optionally the local avatar can be parented to the new destination.</item>
        ///     </list>
        ///     The local avatar is the avatar controlled by the user using the headset and input controllers. Non-local avatars
        ///     are other avatars instantiated in the scene but not controlled by the user, either other users through the network
        ///     or other scenarios such as automated replays.
        /// </summary>
        /// <param name="referenceTransform">
        ///     The object the avatar should keep relative position/orientation to. This should be the moving object the avatar has
        ///     teleported on top of. If null, <paramref name="newRelativeFloorPosition" /> and
        ///     <paramref name="newRelativeRotation" /> will be interpreted as world coordinates.
        /// </param>
        /// <param name="parentToReference">
        ///     Whether to parent the avatar to <paramref name="referenceTransform" />. The avatar should be parented if it's being
        ///     teleported to a moving hierarchy it is not part of
        /// </param>
        /// <param name="newRelativeFloorPosition">
        ///     New floor-level position the avatar will be teleported over in <paramref name="referenceTransform" /> local
        ///     coordinates. If <paramref name="referenceTransform" /> is null, coordinates will be interpreted as being in
        ///     world-space. The camera position will be on top of the floor position, keeping the original eye-level.
        /// </param>
        /// <param name="newRelativeRotation">
        ///     Local rotation the avatar will be teleported to with respect to <see cref="referenceTransform" />. If
        ///     <paramref name="referenceTransform" /> is null, rotation will be in world-space. The camera will point in the
        ///     rotation's forward direction.
        /// </param>
        /// <param name="translationType">The type of translation to use. By default, it will teleport immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="translationType" /> has a duration, it will specify how long the
        ///     teleport transition will take in seconds. By default, it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTranslationSeconds" />
        /// </param>
        /// <param name="teleportedCallback">
        ///     Optional callback executed depending on the teleportation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTranslationType.Snap" />: Right after finishing the teleportation.</item>
        ///         <item>
        ///             <see cref="UxrTranslationType.Fade" />: When the screen is completely faded out and the avatar has been
        ///             moved, before fading back in. This can be used to enable/disable/change GameObjects in the scene since the
        ///             screen at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTranslationType.Interpolate" />: Right after finishing the teleportation.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the teleportation finished. If a fade effect has been requested, the
        ///     callback is executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Coroutine enumerator</returns>
        /// <remarks>
        ///     If <see cref="UxrTranslationType.Fade" /> translation mode was specified, the default black fade color can be
        ///     changed using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public IEnumerator TeleportLocalAvatarRelativeCoroutine(Transform referenceTransform,
                                                                bool parentToReference,
                                                                Vector3 newRelativeFloorPosition,
                                                                Quaternion newRelativeRotation,
                                                                UxrTranslationType translationType = UxrTranslationType.Snap,
                                                                float transitionSeconds = UxrConstants.Locomotion.DiscreteTranslationSeconds,
                                                                Action teleportedCallback = null,
                                                                Action finishedCallback = null,
                                                                bool propagateEvents = true,
                                                                object source = null)
        {
            if (UxrAvatar.LocalAvatar)
            {
                Vector3 oldFloorPosition = UxrAvatar.LocalAvatar.CameraFloorPosition;
                Quaternion oldFloorRotation = Quaternion.LookRotation(UxrAvatar.LocalAvatar.ProjectedCameraForward);
                Quaternion inverseReferenceRotation = referenceTransform != null ? Quaternion.Inverse(referenceTransform.rotation) : Quaternion.identity;
                Matrix4x4 inverseReferenceMatrix = referenceTransform != null ? referenceTransform.localToWorldMatrix.inverse : Matrix4x4.identity;
                Vector3 oldRelativePosition = inverseReferenceMatrix * oldFloorPosition;
                Quaternion oldRelativeRotation = inverseReferenceRotation * oldFloorRotation;

                void TranslateAvatarInternal(float t = 1.0f)
                {
                    Vector3 newPos = Vector3.Lerp(oldRelativePosition, newRelativeFloorPosition, t);
                    Quaternion newRot = oldRelativeRotation;

                    if (Mathf.Approximately(t, 1.0f))
                    {
                        newRot = newRelativeRotation;
                    }

                    if (referenceTransform != null)
                    {
                        newPos = referenceTransform.TransformPoint(newPos);
                        newRot = referenceTransform.rotation * newRot;
                    }

                    MoveAvatarTo(UxrAvatar.LocalAvatar, newPos, newRot * Vector3.forward, propagateEvents, source);
                }

                switch (translationType)
                {
                    case UxrTranslationType.Snap:

                        TranslateAvatarInternal();
                        teleportedCallback?.Invoke();
                        break;

                    case UxrTranslationType.Fade:

                        yield return UxrAvatar.LocalAvatar.CameraFade.StartFadeCoroutine(transitionSeconds * 0.5f, TeleportFadeColor.WithAlpha(0.0f), TeleportFadeColor.WithAlpha(1.0f));

                        TranslateAvatarInternal();
                        teleportedCallback?.Invoke();
                        yield return null;
                        yield return UxrAvatar.LocalAvatar.CameraFade.StartFadeCoroutine(transitionSeconds * 0.5f, TeleportFadeColor.WithAlpha(1.0f), TeleportFadeColor.WithAlpha(0.0f));

                        break;

                    case UxrTranslationType.Interpolate: yield return this.LoopCoroutine(transitionSeconds, TranslateAvatarInternal, UxrEasing.Linear, true); break;
                }

                if (parentToReference && referenceTransform != null)
                {
                    SetAvatarParent(UxrAvatar.LocalAvatar, referenceTransform.GetTrackingUniqueIdComponent());
                }
            }

            _teleportCoroutine = null;
            _teleportFinishedCallback = null;
            finishedCallback?.Invoke();
        }

        /// <summary>
        ///     Public avatar rotation coroutine that can be yielded from an external coroutine.
        ///     Rotates the avatar around its vertical axis, where a positive angle turns it to the right and a negative angle to
        ///     the left.
        /// </summary>
        /// <param name="degrees">The degrees to rotate</param>
        /// <param name="turnType">The type of rotation to use. By default, it will rotate immediately</param>
        /// <param name="transitionSeconds">
        ///     If <paramref name="turnType" /> has a duration, it will specify how long the
        ///     rotation transition will take in seconds. By default it is
        ///     <see cref="UxrConstants.Locomotion.DiscreteTurnSeconds" />
        /// </param>
        /// <param name="rotatedCallback">
        ///     Optional callback executed depending on the rotation mode:
        ///     <list type="bullet">
        ///         <item><see cref="UxrTurnType.Snap" />: Right after finishing the turn.</item>
        ///         <item>
        ///             <see cref="UxrTurnType.Fade" />: When the screen is completely faded out and the avatar has rotated,
        ///             before fading back in. This can be used to enable/disable/change GameObjects in the scene since the screen
        ///             at this point is fully rendered using the fade color.
        ///         </item>
        ///         <item><see cref="UxrTurnType.Interpolate" />: Right after finishing the turn.</item>
        ///     </list>
        /// </param>
        /// <param name="finishedCallback">
        ///     Optional callback executed right after the rotation finished. If a fade effect has been requested, the callback is
        ///     executed right after the screen has faded back in.
        /// </param>
        /// <param name="propagateEvents">
        ///     Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" />/
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> events
        /// </param>
        /// <param name="source">
        ///     Optional source object that originated the movement. It is exposed through
        ///     <see cref="UxrAvatarMoveEventArgs" /> so listeners can determine what originated the event, for example
        ///     whether the movement was caused by a locomotion component.
        /// </param>
        /// <returns>Coroutine enumerator</returns>
        /// <remarks>
        ///     If <see cref="UxrTurnType.Fade" /> translation mode was specified, the default black fade color can be changed
        ///     using <see cref="TeleportFadeColor" />.
        /// </remarks>
        public IEnumerator RotateLocalAvatarCoroutine(float degrees,
                                                      UxrTurnType turnType = UxrTurnType.Snap,
                                                      float transitionSeconds = UxrConstants.Locomotion.DiscreteTurnSeconds,
                                                      Action rotatedCallback = null,
                                                      Action finishedCallback = null,
                                                      bool propagateEvents = true,
                                                      object source = null)
        {
            if (UxrAvatar.LocalAvatar)
            {
                Vector3 initialForward = UxrAvatar.LocalAvatar.ProjectedCameraForward;
                Transform avatarTransform = UxrAvatar.LocalAvatar.transform;

                void RotateAvatarInternal(float t = 1.0f)
                {
                    MoveAvatarTo(UxrAvatar.LocalAvatar, UxrAvatar.LocalAvatar.CameraFloorPosition, initialForward.GetRotationAround(avatarTransform.up, degrees * t), propagateEvents, source);
                }

                switch (turnType)
                {
                    case UxrTurnType.Snap:

                        RotateAvatarInternal();
                        rotatedCallback?.Invoke();
                        break;

                    case UxrTurnType.Fade:

                        yield return UxrAvatar.LocalAvatar.CameraFade.StartFadeCoroutine(transitionSeconds * 0.5f, TeleportFadeColor.WithAlpha(0.0f), TeleportFadeColor.WithAlpha(1.0f));

                        RotateAvatarInternal();
                        rotatedCallback?.Invoke();
                        yield return null;
                        yield return UxrAvatar.LocalAvatar.CameraFade.StartFadeCoroutine(transitionSeconds * 0.5f, TeleportFadeColor.WithAlpha(1.0f), TeleportFadeColor.WithAlpha(0.0f));

                        break;

                    case UxrTurnType.Interpolate: yield return this.LoopCoroutine(transitionSeconds, RotateAvatarInternal, UxrEasing.Linear, true); break;
                }
            }

            _teleportCoroutine = null;
            _teleportFinishedCallback = null;
            finishedCallback?.Invoke();
        }

        /// <summary>
        ///     <para>
        ///         Precaching coroutine. It will try to find all <see cref="IUxrPrecacheable" /> components in the scene and
        ///         pre-instantiate their objects in front of the camera while the screen is still faded black.
        ///         The goal is to make sure all resources (meshes, textures) are in memory so that when they are instantiated at
        ///         any point, the resources are already available lowering the chances of rendering hiccups.
        ///         The scene is rendered black on top during a pre-determined number of frames (<see cref="PrecacheFrameCount" />)
        ///         after which the pre-instantiated objects will be destroyed and the scene will be faded in.
        ///     </para>
        ///     <para>
        ///         Another preprocessing that takes place is finding initially disabled <see cref="UxrComponent" /> components and
        ///         force registering their Unique IDs to overcome the limitation of initially disabled components not being able
        ///         to receive state synchronization messages.
        ///     </para>
        /// </summary>
        /// <param name="onStarting">Optional callback called when precaching is right about to start</param>
        /// <param name="onFinished">Optional callback called right after precaching finished</param>
        /// <returns>Coroutine enumerator</returns>
        private IEnumerator PrecacheCoroutine(Action onStarting = null, Action onFinished = null)
        {
            UxrAvatar avatar = UxrAvatar.LocalAvatar;

            while (avatar == null)
            {
                yield return null;

                avatar = UxrAvatar.LocalAvatar;
            }

            onStarting?.Invoke();

            DestroyPrecachedInstances();

            _precacheInstances = [];

            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; ++sceneIndex)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                AddScenePrecachedInstances(_precacheInstances, scene, avatar);
            }

            AddScenePrecachedInstances(_precacheInstances, Instance.gameObject.scene, avatar);

            for (int frame = 0; frame < _precacheFrameCount; ++frame)
            {
                if (avatar == null)
                {
                    // Another scene loaded
                    break;
                }

                if (avatar && UseAvatarFadeIn)
                {
                    avatar.CameraFade.EnableFadeColor(Color.black, 1.0f);
                }

                yield return null;
            }

            DestroyPrecachedInstances();

            onFinished?.Invoke();

            float startFadeTime = Time.time;
            float fadeDuration = 0.5f;

            while (Time.time - startFadeTime < fadeDuration)
            {
                if (avatar == null)
                {
                    // Another scene loaded
                    break;
                }

                if (avatar && UseAvatarFadeIn)
                {
                    avatar.CameraFade.EnableFadeColor(Color.black, 1.0f - (Time.time - startFadeTime) / fadeDuration);
                }

                yield return null;
            }

            if (avatar && UseAvatarFadeIn)
            {
                avatar.CameraFade.DisableFadeColor();
            }

            _precacheCoroutine = null;
        }

        /// <summary>
        ///     Terminates any ongoing teleport coroutine, invoking the associated callback to indicate the teleport
        ///     was not completed.
        /// </summary>
        private void CheckInterruptTeleportCoroutine()
        {
            if (_teleportCoroutine != null)
            {
                StopCoroutine(_teleportCoroutine);
                _teleportFinishedCallback?.Invoke(false);
                _teleportCoroutine = null;
                _teleportFinishedCallback = null;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Re-solves the local avatar's full body IK (neck, spine, chest, arms) right before rendering.
        ///     The TrackedPoseDriver fires at BeforeRenderOrder 0 and applies a final camera pose update after LateUpdate.
        ///     By running at order 100 we execute after that update, giving the entire visible avatar zero latency
        ///     relative to the camera for the rendered frame. Time-dependent smoothing is skipped to avoid
        ///     double-advancing body rotation and torsion state within the same frame.
        /// </summary>
        [BeforeRenderOrder(100)]
        private void Application_OnBeforeRender()
        {
            if (!Application.isPlaying)
            {
                return;
            }

#if UNITY_EDITOR
            if (!EditorApplication.isPlaying)
            {
                return;
            }
#endif

            UxrAvatar localAvatar = UxrAvatar.LocalAvatar;

            if (localAvatar != null && IsEnabledController(localAvatar))
            {
                (localAvatar.AvatarController as UxrStandardAvatarController)?.ResyncAvatarToCamera();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        ///     Unity Editor callback when the play mode state changes.
        /// </summary>
        /// <param name="playModeState">The new play mode state.</param>
        private void EditorApplication_PlayModeStateChanged(PlayModeStateChange playModeState)
        {
            if (playModeState == PlayModeStateChange.ExitingPlayMode)
            {
                OnApplicationQuit();
            }
        }
#endif

        /// <summary>
        ///     Called when any component with the <see cref="IUxrStateSync" /> interface has a state change.
        /// </summary>
        /// <param name="sender">Sender (component implementing <see cref="IUxrStateSync" />)</param>
        /// <param name="eventArgs">Event parameters</param>
        private void StateSync_StateChanged(object sender, UxrSyncEventArgs eventArgs)
        {
            // Don't generate ComponentChanged events inside ExecuteStateSyncEvent() to avoid infinite message loop 

            if (!IsInsideStateSync && !IsInsideLoadStateChanges && sender is IUxrStateSync component)
            {
                OnComponentStateChanged(component, eventArgs);
            }
        }

        /// <summary>
        ///     Called when an <see cref="UxrAvatar" /> is enabled. If the avatar is the local avatar, it is used as a signal to
        ///     set up canvases in the scene and start the pre-caching process.
        /// </summary>
        /// <param name="avatar">Avatar that was enabled</param>
        private void Avatar_Enabled(UxrAvatar avatar)
        {
            if (avatar.AvatarMode == UxrAvatarMode.Local && avatar.AvatarController != null)
            {
                if (UxrPointerInputModule.Instance != null && UxrPointerInputModule.Instance.AutoAssignEventCamera)
                {
                    foreach (UxrCanvas canvas in UxrCanvas.AllComponents)
                    {
                        if (canvas.UnityCanvas)
                        {
                            canvas.UnityCanvas.worldCamera = avatar.CameraComponent;
                        }
                    }
                }

                // In multiplayer environments the avatar might be instantiated in Local mode but switched
                // later to UpdateExternally. Don't precache when there is more than 1.

                if (UxrAvatar.AllComponents.Count(a => a.AvatarMode == UxrAvatarMode.Local) == 1)
                {
                    TryPrecaching();
                }
            }
        }

        /// <summary>
        ///     Called when a Unity scene was loaded. It is used to try to automatically set up the canvases in the scene so that
        ///     they can be used with UltimateXR.
        /// </summary>
        /// <param name="scene">Scene that was loaded.</param>
        /// <param name="loadSceneMode">The mode used to load the scene.</param>
        private void SceneManager_SceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
        {
            SetupCanvases();
        }

        /// <summary>
        ///     Called when a Unity scene was unloaded. It is used to try to automatically set up the canvases in the scene so that
        ///     they can be used with UltimateXR.
        /// </summary>
        /// <param name="scene">Scene that was unloaded.</param>
        private void SceneManager_SceneUnloaded(Scene scene)
        {
            SetupCanvases();
        }

        /// <summary>
        ///     Called when a component implementing <see cref="IUxrStateSync" /> is being enabled. We use it to subscribe to the
        ///     <see cref="UxrComponent.StateChanged" /> event to keep track of any state changes in components in UltimateXR.
        /// </summary>
        /// <param name="component">Component that was enabled</param>
        private void StateSync_Registered(IUxrStateSync component)
        {
            component.StateChanged += StateSync_StateChanged;
        }

        /// <summary>
        ///     Called when a component implementing <see cref="IUxrStateSync" /> is being disabled. We use it to unsubscribe from
        ///     the <see cref="UxrComponent.StateChanged" /> event.
        /// </summary>
        /// <param name="component">Component that was disabled</param>
        private void StateSync_Unregistered(IUxrStateSync component)
        {
            component.StateChanged -= StateSync_StateChanged;
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Event trigger for the <see cref="ComponentStateChanged" /> event.
        /// </summary>
        /// <param name="component">Component with the state change</param>
        /// <param name="eventArgs">Event parameters</param>
        private void OnComponentStateChanged(IUxrStateSync component, UxrSyncEventArgs eventArgs)
        {
            if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Verbose)
            {
                Debug.Log($"{UxrConstants.CoreModule} {component.Component.GetPathUnderScene()} state change: {eventArgs}.");
            }

            if (UxrStateSyncImplementer.SyncCallDepth == 1 || !UseTopLevelStateChangesOnly || eventArgs.Options.HasFlag(UxrStateSyncOptions.IgnoreNestingCheck))
            {
                ComponentStateChanged?.Invoke(component, eventArgs);
            }
        }

        /// <summary>
        ///     <see cref="PrecachingStarting" /> event trigger.
        /// </summary>
        private void OnPrecachingStarting()
        {
            PrecachingStarting?.Invoke();
        }

        /// <summary>
        ///     <see cref="PrecachingFinished" /> event trigger.
        /// </summary>
        private void OnPrecachingFinished()
        {
            PrecachingFinished?.Invoke();
        }

        /// <summary>
        ///     <see cref="UxrAvatar.GlobalAvatarMoving" /> event trigger.
        /// </summary>
        /// <param name="sender">The object that originated the movement</param>
        /// <param name="avatar">The avatar that is about to move</param>
        /// <param name="args">Event parameters</param>
        /// <param name="propagateEvents">Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoving" /> events</param>
        private void OnAvatarMoving(object sender, UxrAvatar avatar, UxrAvatarMoveEventArgs args, bool propagateEvents = true)
        {
            if (propagateEvents)
            {
                avatar.RaiseAvatarMoving(sender, args);
            }
        }

        /// <summary>
        ///     <see cref="UxrAvatar.GlobalAvatarMoved" /> event trigger.
        /// </summary>
        /// <param name="sender">The object that originated the movement</param>
        /// <param name="avatar">The avatar that moved</param>
        /// <param name="args">Event parameters</param>
        /// <param name="propagateEvents">Whether to propagate <see cref="UxrAvatar.GlobalAvatarMoved" /> events</param>
        private void OnAvatarMoved(object sender, UxrAvatar avatar, UxrAvatarMoveEventArgs args, bool propagateEvents = true)
        {
            if (propagateEvents)
            {
                avatar.RaiseAvatarMoved(sender, args);
            }
        }

        /// <summary>
        ///     <see cref="AvatarsUpdating" /> event trigger.
        /// </summary>
        private void OnUpdating()
        {
            AvatarsUpdating?.Invoke();
        }

        /// <summary>
        ///     <see cref="AvatarsUpdated" /> event trigger.
        /// </summary>
        private void OnUpdated()
        {
            AvatarsUpdated?.Invoke();
        }

        /// <summary>
        ///     <see cref="StageUpdating" /> event trigger.
        /// </summary>
        private void OnUpdatingStage(UxrUpdateStage stage)
        {
            StageUpdating?.Invoke(stage);
        }

        /// <summary>
        ///     <see cref="StageUpdated" /> event trigger.
        /// </summary>
        private void OnStageUpdated(UxrUpdateStage stage)
        {
            StageUpdated?.Invoke(stage);
        }

        /// <summary>
        ///     <see cref="UxrAvatar.AvatarUpdating">UxrAvatar.AvatarUpdating</see> event trigger.
        /// </summary>
        private void OnAvatarUpdating(UxrAvatarUpdateEventArgs e)
        {
            e.Avatar.RaiseAvatarUpdating(this, e);
        }

        /// <summary>
        ///     <see cref="UxrAvatar.AvatarUpdated">UxrAvatar.AvatarUpdated</see> event trigger.
        /// </summary>
        private void OnAvatarUpdated(UxrAvatarUpdateEventArgs e)
        {
            e.Avatar.RaiseAvatarUpdated(this, e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Performs the post-update: Updates the animation and interaction of all key entities, while sending all related
        ///     events during the process.
        ///     The main updates are:
        ///     <list type="bullet">
        ///         <item>Avatar animation.</item>
        ///         <item>Manipulation mechanics and constraints.</item>
        ///         <item>Other managers in the framework such as the <see cref="UxrWeaponManager" />.</item>
        ///         <item>Inverse kinematics.</item>
        ///     </list>
        /// </summary>
        private void PostUpdate()
        {
            // Avatar bones that are tracked

            OnUpdatingStage(UxrUpdateStage.AvatarUsingTracking);

            UxrAvatar localAvatar = UxrAvatar.LocalAvatar;

            if (IsEnabledController(localAvatar))
            {
                UxrAvatarUpdateEventArgs e = UxrAvatarUpdateEventArgs.GetFromPool(localAvatar, UxrUpdateStage.AvatarUsingTracking);

                OnAvatarUpdating(e);
                ((IUxrAvatarControllerUpdater)localAvatar.AvatarController).UpdateAvatarUsingTrackingDevices();
                OnAvatarUpdated(e);
            }

            OnStageUpdated(UxrUpdateStage.AvatarUsingTracking);

            // Update manipulation. Non-local avatars manipulation will also be updated to sync manipulation events.

            OnUpdatingStage(UxrUpdateStage.Manipulation);

            for (int i = 0; i < UxrAvatar.AllComponents.Count; i++)
            {
                UxrAvatar avatar = UxrAvatar.AllComponents[i];
                if (IsEnabledController(avatar))
                {
                    OnAvatarUpdating(UxrAvatarUpdateEventArgs.GetFromPool(avatar, UxrUpdateStage.Manipulation));
                }
            }

            UxrGrabManager.Instance.UpdateManager();
            UxrWeaponManager.Instance.UpdateManager();

            for (int i = 0; i < UxrAvatar.AllComponents.Count; i++)
            {
                UxrAvatar avatar = UxrAvatar.AllComponents[i];
                if (IsEnabledController(avatar))
                {
                    // We update the manipulation after the grab manager mainly to ensure that the
                    // hand transitions that result from releasing constrained objects work with the correct start.
                    ((IUxrAvatarControllerUpdater)avatar.AvatarController).UpdateAvatarManipulation();
                }
            }

            for (int i = 0; i < UxrAvatar.AllComponents.Count; i++)
            {
                UxrAvatar avatar = UxrAvatar.AllComponents[i];
                if (IsEnabledController(avatar))
                {
                    OnAvatarUpdated(UxrAvatarUpdateEventArgs.GetFromPool(avatar, UxrUpdateStage.Manipulation));
                }
            }

            OnStageUpdated(UxrUpdateStage.Manipulation);

            // Update animation

            OnUpdatingStage(UxrUpdateStage.Animation);

            for (int i = 0; i < UxrAvatar.AllComponents.Count; i++)
            {
                UxrAvatar avatar = UxrAvatar.AllComponents[i];
                if (IsEnabledController(avatar))
                {
                    if (avatar.AvatarMode == UxrAvatarMode.Local)
                    {
                        UxrAvatarUpdateEventArgs e = UxrAvatarUpdateEventArgs.GetFromPool(avatar, UxrUpdateStage.Animation);

                        OnAvatarUpdating(e);
                        ((IUxrAvatarControllerUpdater)avatar.AvatarController).UpdateAvatarAnimation();
                        OnAvatarUpdated(e);
                    }
                    else
                    {
                        // This makes sure that hand poses are updated 
                        UxrAvatarUpdateEventArgs e = UxrAvatarUpdateEventArgs.GetFromPool(avatar, UxrUpdateStage.Animation);

                        OnAvatarUpdating(e);
                        avatar.UpdateHandPoseTransforms();
                        OnAvatarUpdated(e);
                    }
                }
            }

            OnStageUpdated(UxrUpdateStage.Animation);

            // Update post-process. All enabled avatar controllers are updated, not just the local one, so that IK is computed in all.

            OnUpdatingStage(UxrUpdateStage.PostProcess);

            for (int i = 0; i < UxrAvatar.AllComponents.Count; i++)
            {
                UxrAvatar avatar = UxrAvatar.AllComponents[i];
                if (IsEnabledController(avatar))
                {
                    UxrAvatarController avatarController = avatar.AvatarController;
                    UxrAvatarUpdateEventArgs e = UxrAvatarUpdateEventArgs.GetFromPool(avatarController.Avatar, UxrUpdateStage.PostProcess);

                    OnAvatarUpdating(e);
                    ((IUxrAvatarControllerUpdater)avatarController).UpdateAvatarPostProcess();
                    avatarController.Avatar.AvatarRigInfo.UpdateInfo();
                    OnAvatarUpdated(e);
                }
            }

            OnStageUpdated(UxrUpdateStage.PostProcess);
            OnUpdated();
        }

        /// <summary>
        ///     Processes all <see cref="IUxrPrecacheable" /> components in a scene and instantiates all required prefabs in front
        ///     of the avatar camera. The goal is to make sure all their resources are loaded into memory afterwards.<br />
        ///     It also registers initially disabled UltimateXR components so that their Unique ID is known and can receive sync
        ///     messages too.
        /// </summary>
        /// <param name="dynamicInstances">List of loaded instances.</param>
        /// <param name="scene">Scene to get the components from.</param>
        /// <param name="avatar">Current avatar.</param>
        private void AddScenePrecachedInstances(Dictionary<UnityEngine.EntityId, GameObject> dynamicInstances, Scene scene, UxrAvatar avatar)
        {
            for (int rootIndex = 0; rootIndex < scene.rootCount; ++rootIndex)
            {
                MonoBehaviour[] behaviours = scene.GetRootGameObjects()[rootIndex].GetComponentsInChildren<MonoBehaviour>(true);

                for (int behaviourIndex = 0; behaviourIndex < behaviours.Length; ++behaviourIndex)
                {
                    if (behaviours[behaviourIndex] is IUxrPrecacheable precacheable)
                    {
                        foreach (GameObject precachedInstance in precacheable.PrecachedInstances)
                        {
                            if (precachedInstance != null && !dynamicInstances.ContainsKey(precachedInstance.GetEntityId()))
                            {
                                // Instantiate
                                GameObject dynamicInstance = Instantiate(precachedInstance,
                                                                         avatar.CameraTransform.position + avatar.CameraTransform.forward * 5.0f,
                                                                         avatar.CameraTransform.rotation,
                                                                         Instance.transform);

                                dynamicInstances.Add(precachedInstance.GetEntityId(), dynamicInstance);

                                // Avoid sounds
                                AudioSource[] audioSources = dynamicInstance.GetComponentsInChildren<AudioSource>(true);
                                foreach (AudioSource audioSource in audioSources)
                                {
                                    audioSource.enabled = false;
                                }
                            }
                        }
                    }

                    // Ensure registering initially disabled components so that their UniqueId is known and can exchange sync messages too

                    if (behaviours[behaviourIndex] != null && !behaviours[behaviourIndex].enabled && behaviours[behaviourIndex] is IUxrUniqueId unique)
                    {
                        unique.RegisterIfNecessary();
                    }
                }
            }
        }

        /// <summary>
        ///     Destroys the currently loaded pre-cached instances.
        /// </summary>
        private void DestroyPrecachedInstances()
        {
            if (_precacheInstances != null)
            {
                foreach (KeyValuePair<UnityEngine.EntityId, GameObject> dynamicInstancePair in _precacheInstances)
                {
                    if (dynamicInstancePair.Value != null)
                    {
                        Destroy(dynamicInstancePair.Value);
                    }
                }

                _precacheInstances.Clear();
            }
        }

        /// <summary>
        ///     Starts the pre-caching process. If a pre-caching process is currently running, it will be stopped before starting
        ///     again.
        /// </summary>
        private void TryPrecaching()
        {
            if (_precacheCoroutine != null)
            {
                StopCoroutine(_precacheCoroutine);
            }

            if (UsePrecaching)
            {
                _precacheCoroutine = StartCoroutine(PrecacheCoroutine(OnPrecachingStarting, OnPrecachingFinished));
            }
        }

        /// <summary>
        ///     Tries to set up all <see cref="Canvas" /> components currently in the scene so that they can work with UltimateXR
        ///     through the <see cref="UxrCanvas" /> component.
        /// </summary>
        private void SetupCanvases()
        {
            if (UxrPointerInputModule.Instance)
            {
                foreach (Canvas canvas in ComponentExt.GetAllComponentsInOpenScenes<Canvas>(true))
                {
                    if (canvas.renderMode == RenderMode.WorldSpace && canvas.GetComponent<UxrIgnoreCanvas>() == null)
                    {
                        if (!canvas.TryGetComponent<UxrCanvas>(out var canvasXR))
                        {
                            if (UxrPointerInputModule.Instance.AutoEnableOnWorldCanvases)
                            {
                                canvasXR = canvas.gameObject.AddComponent<UxrCanvas>();
                                canvasXR.SetupCanvas(UxrPointerInputModule.Instance);
                            }
                        }

                        if (canvasXR && UxrPointerInputModule.Instance.AutoAssignEventCamera && UxrAvatar.LocalAvatar)
                        {
                            canvas.worldCamera = UxrAvatar.LocalAvatarCamera;
                        }
                    }
                }
            }
        }

        /// <summary>
        ///     Determines whether the <see cref="UxrAvatarController" /> of the avatar is enabled, initialized, and non-null.
        /// </summary>
        /// <param name="avatar">The avatar to evaluate</param>
        /// <returns>
        ///     True if the specified <see cref="UxrAvatarController" /> is not null, is enabled, and is initialized; otherwise,
        ///     false.
        /// </returns>
        private bool IsEnabledController(UxrAvatar avatar)
        {
            UxrAvatarController avatarController = avatar?.AvatarController;
            return avatarController != null && avatarController.enabled && avatarController.Initialized;
        }

        /// <summary>
        ///     Sets the parent transform for the specified <see cref="UxrAvatar" /> object, using special networking code
        ///     if a multiplayer session is active.
        /// </summary>
        /// <param name="avatar">The avatar whose parent transform will be set</param>
        /// <param name="parent">The new parent transform to assign to the avatar. It needs a <see cref="IUxrUniqueId" /> component</param>
        private void SetAvatarParent(UxrAvatar avatar, IUxrUniqueId parent)
        {
            BeginSync(UxrStateSyncOptions.Default ^ UxrStateSyncOptions.Network);

            IUxrNetworkAvatar networkAvatar = avatar.GetComponent<IUxrNetworkAvatar>();

            if (networkAvatar != null && (UxrNetworkManager.IsServer || UxrNetworkManager.IsClient))
            {
                // Parent using networking
                networkAvatar.ChangeParent(avatar.transform, parent?.Transform);
            }
            else
            {
                avatar.transform.SetParent(parent?.Transform);
            }

            EndSyncMethod(SyncParams(avatar, parent));
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     The serialization version used by <see cref="SaveStateChanges" />. Mainly the header, since each component type has
        ///     its own version.
        /// </summary>
        private const int StateSerializationVersion = 0;

        private Coroutine _precacheCoroutine;
        private Dictionary<UnityEngine.EntityId, GameObject> _precacheInstances;
        private Coroutine _teleportCoroutine;
        private Action<bool> _teleportFinishedCallback;

        #endregion
    }
}