// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSyncObject.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core.Settings;
using UltimateXR.Core.StateSync;
using UltimateXR.Core.Unique;
using UltimateXR.Networking;
using UnityEngine;

namespace UltimateXR.Core.Components
{
    /// <summary>
    ///     <see cref="UxrComponent" /> component that can be used for several purposes:
    ///     <list type="bullet">
    ///         <item>
    ///             Reference a GameObject in multiplayer environments when there is no other <see cref="UxrComponent" /> on
    ///             it. Since <see cref="UxrComponent" /> is supported as a parameter in synchronized methods (methods that use
    ///             <see cref="UxrComponent.BeginSync" /> and <see cref="UxrComponent.EndSyncMethod" /> for multiplayer
    ///             or replays) it can be used to access the <see cref="UxrComponent.gameObject" /> it is on.<br />
    ///             See also <see cref="UxrComponent.UniqueId" /> and <see cref="UxrUniqueIdImplementer.TryGetComponentById" />
    ///             .
    ///         </item>
    ///         <item>
    ///             Save the component enabled/disabled state and the GameObject active state in replays and game
    ///             saves using <see cref="UxrManager.SaveStateChanges" />.
    ///         </item>
    ///         <item>Automatically synchronize the object's Transform in multiplayer with interpolation/extrapolation support.</item>
    ///     </list>
    /// </summary>
    public partial class UxrSyncObject : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private bool              _syncTransform;
        [SerializeField] private UxrTransformSpace _transformSpace = UxrTransformSpace.World;
        [SerializeField] private bool              _syncActiveAndEnabled;
        [SerializeField] private bool              _syncWhileDisabled;
        [SerializeField] private bool              _syncTransformNetwork;
        [SerializeField] private float             _netPositionDistanceThreshold = 0.001f;
        [SerializeField] private float             _netScaleDeltaThreshold       = 0.005f;
        [SerializeField] private float             _netRotationDegreesThreshold  = 0.01f;
        [SerializeField] private bool              _overrideDefaultNetSyncIntervalSeconds;
        [SerializeField] private float             _netSyncIntervalSecondsOverride = 0.1f;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets the avatar transform to compute transforms in avatar space.
        /// </summary>
        public Transform AvatarTransform
        {
            get
            {
                if (_avatarTransform == null)
                {
                    FindAvatar();
                }

                return _avatarTransform;
            }
        }

        /// <summary>
        ///     Gets or sets whether the transform should be synchronized. This means it will be saved by <see cref="UxrManager" />
        ///     during <see cref="UxrManager.SaveStateChanges" />. This can be used for game saves and replays.
        /// </summary>
        public bool SyncTransform
        {
            get => _syncTransform;
            set => _syncTransform = value;
        }

        /// <summary>
        ///     Gets or sets the transform space used for synchronization.
        /// </summary>
        public UxrTransformSpace TransformSpace
        {
            get => _transformSpace;
            set => _transformSpace = value;
        }

        /// <summary>
        ///     Gets or sets whether to synchronize the active and enabled states.
        /// </summary>
        public bool SyncActiveAndEnabled
        {
            get => _syncActiveAndEnabled;
            set => _syncActiveAndEnabled = value;
        }

        /// <summary>
        ///     Gets or sets whether to synchronize while the component/object are not active/enabled.
        /// </summary>
        public bool SyncWhileDisabled
        {
            get => _syncWhileDisabled;
            set => _syncWhileDisabled = value;
        }

        /// <summary>
        ///     Gets or sets whether the transform should be synchronized over the network in multiplayer sessions.
        /// </summary>
        public bool SyncTransformNetwork
        {
            get => _syncTransformNetwork;
            set => _syncTransformNetwork = value;
        }

        /// <summary>
        ///     Gets or sets the minimum distance the object needs to travel to trigger a network position synchronization.
        /// </summary>
        public float NetPositionDistanceThreshold
        {
            get => _netPositionDistanceThreshold;
            set => _netPositionDistanceThreshold = value;
        }

        /// <summary>
        ///     Gets or sets the minimum scale change the object needs to have to trigger a network scale synchronization.
        /// </summary>
        public float NetScaleDeltaThreshold
        {
            get => _netScaleDeltaThreshold;
            set => _netScaleDeltaThreshold = value;
        }

        /// <summary>
        ///     Gets or sets the minimum rotation change the object needs to have in degrees to trigger a network rotation
        ///     synchronization.
        /// </summary>
        public float NetRotationDegreesThreshold
        {
            get => _netRotationDegreesThreshold;
            set => _netRotationDegreesThreshold = value;
        }

        /// <summary>
        ///     Determines whether the default network synchronization interval should be overridden with a custom value.
        /// </summary>
        public bool OverrideDefaultNetSyncIntervalSeconds
        {
            get => _overrideDefaultNetSyncIntervalSeconds;
            set => _overrideDefaultNetSyncIntervalSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the custom network transform synchronization interval, in seconds, overriding the default interval (
        ///     <see cref="UxrGlobalSettings.SyncNetworkTransformIntervalSeconds" />).
        /// </summary>
        public float NetSyncIntervalSecondsOverride
        {
            get => _netSyncIntervalSecondsOverride;
            set => _netSyncIntervalSecondsOverride = value;
        }

        /// <summary>
        ///     Gets or sets the avatar that provides the authoritative network state for this synchronized object.
        /// </summary>
        /// <remarks>
        ///     When this property is <c>null</c>, the object is server-authoritative and only the server instance,
        ///     identified through <see cref="UxrNetworkManager.IsServer" />, can synchronize it.
        /// </remarks>
        public UxrAvatar NetAuthority
        {
            get => _netAuthority;
            set
            {
                if (_awakeFinished)
                {
                    BeginSync(UxrStateSyncOptions.Network);
                }
                
                _netAuthority = value;

                if (_awakeFinished)
                {
                    EndSyncProperty(value);
                }
            }
        }

        /// <summary>
        ///     Gets whether the current instance has authority to synchronize this object.
        /// </summary>
        /// <remarks>
        ///     If <see cref="NetAuthority" /> is assigned, authority belongs to the instance running that avatar as
        ///     <see cref="UxrAvatar.LocalAvatar" />. If <see cref="NetAuthority" /> is <c>null</c>, authority belongs
        ///     to the server instance.
        /// </remarks>
        public bool IsNetAuthority
        {
            get
            {
                if (NetAuthority != null)
                {
                    return NetAuthority == UxrAvatar.LocalAvatar;
                }
                
                return UxrNetworkManager.IsServer;
            }
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            NetAuthority = GetComponentInParent<UxrAvatar>();
            _awakeFinished = true;
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            SaveCurrentTransform();
        }

        /// <summary>
        ///     Handles authoritative network transform synchronization.
        /// </summary>
        /// <remarks>
        ///     The authoritative instance sends local transform changes at the configured synchronization interval.
        ///     Non-authoritative instances consume buffered network snapshots and interpolate or briefly extrapolate them
        ///     for smooth remote motion. Replays are handled separately through <see cref="UxrComponent" /> by overriding
        ///     <see cref="RequiresTransformSerialization" />.
        /// </remarks>
        private void Update()
        {
            if (!_syncTransformNetwork)
            {
                return;
            }

            if (!IsNetAuthority)
            {
                InterpolateFromBuffer();
                return;
            }

            _timer += Time.deltaTime;

            if (_timer > NetSyncIntervalSeconds)
            {
                _timer = 0f;

                TransformDirtyFlags dirtyFlags = GetTransformDirtyFlags();
                if (dirtyFlags != TransformDirtyFlags.None)
                {
                    GetCurrentTransformValues(out Vector3 position, out Quaternion rotation);
                    Vector3 scale = transform.localScale;

                    DispatchSyncTransformNetwork(dirtyFlags, position, rotation, scale);
                    SaveCurrentTransform();
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Will try to find the avatar transform if it exists, to support transforms in avatar space.
        /// </summary>
        private void FindAvatar()
        {
            _avatarTransform = GetComponentInParent<UxrAvatar>()?.transform;
        }

        /// <summary>
        ///     Reads the current position and rotation values according to the configured <see cref="_transformSpace" />.
        /// </summary>
        private void GetCurrentTransformValues(out Vector3 position, out Quaternion rotation)
        {
            switch (_transformSpace)
            {
                case UxrTransformSpace.Local:
                    position = transform.localPosition;
                    rotation = transform.localRotation;
                    break;

                case UxrTransformSpace.Avatar:

                    if (AvatarTransform != null)
                    {
                        position = AvatarTransform.InverseTransformPoint(transform.position);
                        rotation = Quaternion.Inverse(AvatarTransform.rotation) * transform.rotation;
                    }
                    else
                    {
                        position = transform.position;
                        rotation = transform.rotation;
                    }
                    break;

                case UxrTransformSpace.World:
                default:
                    position = transform.position;
                    rotation = transform.rotation;
                    break;
            }
        }

        /// <summary>
        ///     Gets the latest complete synchronized transform state available locally.
        /// </summary>
        /// <remarks>
        ///     Partial network updates are completed using the most recent buffered snapshot. If the buffer is empty,
        ///     the current transform is used as the fallback state.
        /// </remarks>
        /// <param name="position">The latest synchronized position, or the current position if no snapshot exists.</param>
        /// <param name="rotation">The latest synchronized rotation, or the current rotation if no snapshot exists.</param>
        /// <param name="scale">The latest synchronized scale, or the current scale if no snapshot exists.</param>
        private void GetLastBufferedOrCurrent(out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            int bufferCount = _buffer.Count;
            if (bufferCount > 0)
            {
                TransformSyncData last = _buffer[bufferCount - 1];
                position = last.Position;
                rotation = last.Rotation;
                scale    = last.Scale;
                return;
            }

            GetCurrentTransformValues(out position, out rotation);
            scale = transform.localScale;
        }

        /// <summary>
        ///     Adds a synchronized transform snapshot to the interpolation buffer.
        /// </summary>
        /// <remarks>
        ///     The buffer stores complete position, rotation, and scale states so non-authoritative instances can
        ///     interpolate between received network updates instead of snapping directly to the newest value.
        /// </remarks>
        /// <param name="position">The synchronized position to store.</param>
        /// <param name="rotation">The synchronized rotation to store.</param>
        /// <param name="scale">The synchronized scale to store.</param>
        private void AddSnapshotToBuffer(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            _buffer.Add(new TransformSyncData(position, rotation, scale, Time.time));

            if (_buffer.Count > MaxBufferSize)
            {
                _buffer.RemoveAt(0);
            }
        }

        /// <summary>
        ///     Applies buffered network snapshots using interpolation or short extrapolation.
        /// </summary>
        /// <remarks>
        ///     This is used by non-authoritative instances. The object is rendered slightly behind the latest received
        ///     network time so that two snapshots are usually available for interpolation. If the render time is ahead of
        ///     the available snapshots, the method may extrapolate briefly from the last two snapshots. If extrapolation is
        ///     not possible or exceeds the allowed limit, the transform snaps to the latest synchronized state.
        /// </remarks>
        private void InterpolateFromBuffer()
        {
            if (_buffer.Count < 2)
            {
                return;
            }

            float syncInterval = NetSyncIntervalSeconds;
            float renderTime   = Time.time - syncInterval * InterpolationDelayMultiplier;

            // Search for the pair of snapshots that surrounds renderTime.
            for (int i = 0; i < _buffer.Count - 1; i++)
            {
                if (_buffer[i].Time <= renderTime && _buffer[i + 1].Time >= renderTime)
                {
                    float      t              = Mathf.InverseLerp(_buffer[i].Time, _buffer[i    + 1].Time, renderTime);
                    Vector3    targetPosition = Vector3.Lerp(_buffer[i].Position, _buffer[i     + 1].Position, t);
                    Quaternion targetRotation = Quaternion.Slerp(_buffer[i].Rotation, _buffer[i + 1].Rotation, t);
                    Vector3    targetScale    = Vector3.Lerp(_buffer[i].Scale, _buffer[i        + 1].Scale, t);

                    // Discard snapshots that are no longer needed (before the pair we used).
                    if (i > 0)
                    {
                        _buffer.RemoveRange(0, i);
                    }

                    SetTargetTransform(targetPosition, targetRotation, targetScale);
                    return;
                }
            }

            // Render time is ahead of all snapshots: extrapolate from the last two if possible.
            TransformSyncData latest              = _buffer[^1];
            float             extrapolationLength = Time.time - latest.Time;

            if (extrapolationLength < ExtrapolationLimitSeconds && _buffer.Count >= 2)
            {
                TransformSyncData prev = _buffer[^2];
                float             dt   = latest.Time - prev.Time;

                if (dt > 0.0001f)
                {
                    float      t          = extrapolationLength               / dt;
                    Vector3    velocity   = (latest.Position - prev.Position) / dt;
                    Vector3    predicted  = latest.Position + velocity * extrapolationLength;
                    Quaternion predictedR = Quaternion.SlerpUnclamped(prev.Rotation, latest.Rotation, 1f + t);
                    Vector3    predictedS = Vector3.Lerp(latest.Scale, latest.Scale                      + (latest.Scale - prev.Scale), extrapolationLength / dt);

                    SetTargetTransform(predicted, predictedR, predictedS);
                    return;
                }
            }

            // Fallback: snap to the latest known state.
            SetTargetTransform(latest.Position, latest.Rotation, latest.Scale);
        }

        /// <summary>
        ///     Applies a synchronized transform state using the configured transform space.
        /// </summary>
        /// <remarks>
        ///     After applying the transform, the current values are saved as the local baseline so interpolated remote
        ///     motion is not detected as a local change on the next update.
        /// </remarks>
        /// <param name="targetPosition">The synchronized position to apply.</param>
        /// <param name="targetRotation">The synchronized rotation to apply.</param>
        /// <param name="targetScale">The synchronized scale to apply.</param>
        private void SetTargetTransform(Vector3 targetPosition, Quaternion targetRotation, Vector3 targetScale)
        {
            switch (_transformSpace)
            {
                case UxrTransformSpace.Local:

                    transform.localPosition = targetPosition;
                    transform.localRotation = targetRotation;
                    break;

                case UxrTransformSpace.Avatar:

                    if (AvatarTransform != null)
                    {
                        transform.position = AvatarTransform.TransformPoint(targetPosition);
                        transform.rotation = AvatarTransform.rotation * targetRotation;
                    }
                    else
                    {
                        transform.position = targetPosition;
                        transform.rotation = targetRotation;
                    }
                    break;

                case UxrTransformSpace.World:

                default:

                    transform.position = targetPosition;
                    transform.rotation = targetRotation;
                    break;
            }

            transform.localScale = targetScale;

            SaveCurrentTransform();
        }

        /// <summary>
        ///     Saves the current transform values as the local baseline for change detection.
        /// </summary>
        private void SaveCurrentTransform()
        {
            GetCurrentTransformValues(out _lastPosition, out _lastRotation);
            _lastScale = transform.localScale;
        }

        /// <summary>
        ///     Determines which transform components changed compared to the last saved local baseline.
        /// </summary>
        /// <returns>
        ///     A <see cref="TransformDirtyFlags" /> value indicating which transform components changed locally.
        /// </returns>
        private TransformDirtyFlags GetTransformDirtyFlags()
        {
            GetCurrentTransformValues(out Vector3 currentPosition, out Quaternion currentRotation);
            Vector3 currentScale = transform.localScale;

            TransformDirtyFlags flags = TransformDirtyFlags.None;

            if (Vector3.SqrMagnitude(_lastPosition - currentPosition) > PositionEpsilonSqr)
            {
                flags |= TransformDirtyFlags.Position;
            }

            if (Quaternion.Angle(_lastRotation, currentRotation) > NetRotationDegreesThreshold)
            {
                flags |= TransformDirtyFlags.Rotation;
            }

            if (Vector3.SqrMagnitude(_lastScale - currentScale) > ScaleEpsilonSqr)
            {
                flags |= TransformDirtyFlags.Scale;
            }

            return flags;
        }

        /// <summary>
        ///     Sends the smallest synchronized method signature for the changed transform components.
        /// </summary>
        /// <remarks>
        ///     Unchanged components are omitted from the network call. Receivers rebuild a complete snapshot by combining
        ///     the received values with the latest synchronized values they already have.
        /// </remarks>
        private void DispatchSyncTransformNetwork(TransformDirtyFlags dirtyFlags, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // 3 fields
            if (dirtyFlags == TransformDirtyFlags.All)
            {
                SyncTransformNetworkPRS(position, rotation, scale);
                return;
            }

            // 2 fields
            if (dirtyFlags == (TransformDirtyFlags.Position | TransformDirtyFlags.Rotation))
            {
                SyncTransformNetworkPR(position, rotation);
                return;
            }

            if (dirtyFlags == (TransformDirtyFlags.Position | TransformDirtyFlags.Scale))
            {
                SyncTransformNetworkPS(position, scale);
                return;
            }

            if (dirtyFlags == (TransformDirtyFlags.Rotation | TransformDirtyFlags.Scale))
            {
                SyncTransformNetworkRS(rotation, scale);
                return;
            }

            // 1 field
            if (dirtyFlags == TransformDirtyFlags.Position)
            {
                SyncTransformNetworkP(position);
                return;
            }

            if (dirtyFlags == TransformDirtyFlags.Rotation)
            {
                SyncTransformNetworkR(rotation);
                return;
            }

            if (dirtyFlags == TransformDirtyFlags.Scale)
            {
                SyncTransformNetworkS(scale);
            }

            // None: do nothing
        }

        /// <summary>
        ///     Synchronizes the position, rotation, and scale of an object over the network.
        /// </summary>
        /// <param name="position">The position of the object in world space to be synchronized.</param>
        /// <param name="rotation">The rotation of the object in world space to be synchronized.</param>
        /// <param name="scale">The scale of the object in world space to be synchronized.</param>
        private void SyncTransformNetworkPRS(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                AddSnapshotToBuffer(position, rotation, scale);
            }

            EndSyncMethod(SyncParams(position, rotation, scale));
        }

        /// <summary>
        ///     Synchronizes the position and rotation of the object over the network by adding a snapshot to the buffer or
        ///     initiating a sync process, depending on the network role.
        /// </summary>
        /// <param name="position">The position of the object to synchronize.</param>
        /// <param name="rotation">The rotation of the object to synchronize.</param>
        private void SyncTransformNetworkPR(Vector3 position, Quaternion rotation)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out _, out _, out Vector3 lastScale);
                AddSnapshotToBuffer(position, rotation, lastScale);
            }

            EndSyncMethod(SyncParams(position, rotation));
        }

        /// <summary>
        ///     Synchronizes the transform's position and scale data across the network.
        /// </summary>
        /// <param name="position">The position of the object to synchronize.</param>
        /// <param name="scale">The scale of the object to synchronize.</param>
        private void SyncTransformNetworkPS(Vector3 position, Vector3 scale)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out _, out Quaternion lastRotation, out _);
                AddSnapshotToBuffer(position, lastRotation, scale);
            }

            EndSyncMethod(SyncParams(position, scale));
        }

        /// <summary>
        ///     Synchronizes the rotation and scale values of the object over the network.
        /// </summary>
        /// <param name="rotation">The rotation of the object to synchronize.</param>
        /// <param name="scale">The scale of the object to synchronize.</param>
        private void SyncTransformNetworkRS(Quaternion rotation, Vector3 scale)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out Vector3 lastPosition, out _, out _);
                AddSnapshotToBuffer(lastPosition, rotation, scale);
            }

            EndSyncMethod(SyncParams(rotation, scale));
        }

        /// <summary>
        ///     Synchronizes the transform position across the network.
        /// </summary>
        /// <param name="position">The synchronized position.</param>
        private void SyncTransformNetworkP(Vector3 position)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out _, out Quaternion lastRotation, out Vector3 lastScale);
                AddSnapshotToBuffer(position, lastRotation, lastScale);
            }

            EndSyncMethod(SyncParams(position));
        }

        /// <summary>
        ///     Synchronizes the rotation of the object over the network.
        /// </summary>
        /// <param name="rotation">The current rotation to be synchronized.</param>
        private void SyncTransformNetworkR(Quaternion rotation)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out Vector3 lastPosition, out _, out Vector3 lastScale);
                AddSnapshotToBuffer(lastPosition, rotation, lastScale);
            }

            EndSyncMethod(SyncParams(rotation));
        }

        /// <summary>
        ///     Synchronizes the transform scale across the network.
        /// </summary>
        /// <param name="scale">The scale to be synchronized.</param>
        private void SyncTransformNetworkS(Vector3 scale)
        {
            BeginSync(UxrStateSyncOptions.Network);

            if (!IsNetAuthority)
            {
                GetLastBufferedOrCurrent(out Vector3 lastPosition, out Quaternion lastRotation, out _);
                AddSnapshotToBuffer(lastPosition, lastRotation, scale);
            }

            EndSyncMethod(SyncParams(scale));
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Gets the synchronization interval in seconds.
        /// </summary>
        private float NetSyncIntervalSeconds => OverrideDefaultNetSyncIntervalSeconds ? NetSyncIntervalSecondsOverride : UxrGlobalSettings.Instance.SyncNetworkTransformIntervalSeconds;

        // Thresholds

        private float PositionEpsilonSqr => NetPositionDistanceThreshold * NetPositionDistanceThreshold;
        private float ScaleEpsilonSqr    => NetScaleDeltaThreshold       * NetScaleDeltaThreshold;

        /// <summary>
        ///     Maximum number of transform snapshots retained in the interpolation buffer.
        ///     <para>
        ///         This buffer is used on non-authoritative instances to interpolate between recently received network updates and,
        ///         if necessary, extrapolate briefly beyond the last known state.
        ///     </para>
        ///     <para>
        ///         The value is intentionally small to keep memory usage low and to prevent interpolation from relying on stale
        ///         transform states. A modest history is enough because network sync updates are expected to arrive at a
        ///         regular interval.
        ///     </para>
        /// </summary>
        private const int MaxBufferSize = 10;

        /// <summary>
        ///     Multiplier applied to the network sync interval when calculating the render/interpolation delay.
        ///     <para>
        ///         A value of <c>2</c> means the non-authoritative instance attempts to render the object using a point in time that is roughly
        ///         two sync intervals behind the present. This increases the likelihood that at least two buffered snapshots
        ///         will be available for smooth interpolation instead of forcing a snap to the latest update.
        ///     </para>
        ///     <para>
        ///         In practice, this trades a small amount of visual latency for much smoother motion under normal network
        ///         conditions.
        ///     </para>
        /// </summary>
        private const float InterpolationDelayMultiplier = 2f;

        /// <summary>
        ///     Maximum extrapolation time, in seconds, allowed after the most recent buffered snapshot.
        ///     <para>
        ///         If a non-authoritative instance receives no newer network data for a short period, it may continue predicting motion by
        ///         extrapolating from the last known snapshots. Once this time limit is exceeded, the transform is snapped to
        ///         the most recent known state to avoid visually unstable long-range prediction.
        ///     </para>
        ///     <para>
        ///         This value intentionally limits prediction to a brief window so minor packet delays feel smooth without
        ///         allowing prolonged drift from the authoritative network state.
        ///     </para>
        /// </summary>
        private const float ExtrapolationLimitSeconds = 0.25f;

        private readonly List<TransformSyncData> _buffer = new List<TransformSyncData>();

        private bool       _awakeFinished;
        private float      _timer;
        private Vector3    _lastPosition;
        private Quaternion _lastRotation;
        private Vector3    _lastScale;
        private Transform  _avatarTransform;
        private UxrAvatar  _netAuthority;

        #endregion
    }
}