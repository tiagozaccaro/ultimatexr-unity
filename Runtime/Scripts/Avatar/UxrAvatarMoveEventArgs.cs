// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarMoveEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;
using UltimateXR.Core.Events;
using UltimateXR.Locomotion;
using UnityEngine;

namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Contains information about an <see cref="UxrAvatar" /> that has moved/rotated. Avatars are moved/rotated
    ///     through <see cref="UxrManager" /> functionality such as:
    ///     <list type="bullet">
    ///         <item>
    ///             <see
    ///                 cref="UxrManager.MoveAvatarTo(UxrAvatar,UnityEngine.Vector3,UnityEngine.Vector3,bool,object)">
    ///                 UxrManager.Instance.MoveAvatarTo
    ///             </see>
    ///         </item>
    ///         <item>
    ///             <see cref="UxrManager.RotateAvatar">UxrManager.Instance.RotateAvatar</see>
    ///         </item>
    ///         <item>
    ///             <see
    ///                 cref="UxrManager.TeleportLocalAvatar">
    ///                 UxrManager.Instance.TeleportLocalAvatar
    ///             </see>
    ///         </item>
    ///     </list>
    ///     These methods will move/rotate the root transform of the avatar. If a user moves or rotates in the real-world, the
    ///     camera transform will be updated, but the root avatar transform will remain fixed. Only moving or teleporting the
    ///     avatar will generate <see cref="UxrAvatarMoveEventArgs" /> events.
    /// </summary>
    /// <remarks>
    ///     This event uses <see cref="UxrPooledEventArgs{T}" /> to avoid allocations. Instances are pooled and only guaranteed
    ///     to be valid during the event invocation. Do not store or reuse them outside the handler scope.
    ///     Although instances may remain unchanged briefly depending on pool usage, this behavior is not guaranteed.
    /// </remarks>
    public class UxrAvatarMoveEventArgs : UxrAvatarEventArgs<UxrAvatarMoveEventArgs>
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the old <see cref="UxrAvatar" /> position.
        /// </summary>
        public Vector3 OldPosition { get; private set; }

        /// <summary>
        ///     Gets the old <see cref="UxrAvatar" /> rotation.
        /// </summary>
        public Quaternion OldRotation { get; private set; }

        /// <summary>
        ///     Gets the new <see cref="UxrAvatar" /> position.
        /// </summary>
        public Vector3 NewPosition { get; private set; }

        /// <summary>
        ///     Gets the new <see cref="UxrAvatar" /> rotation.
        /// </summary>
        public Quaternion NewRotation { get; private set; }

        /// <summary>
        ///     Gets the old <see cref="UxrAvatar" /> forward vector.
        /// </summary>
        public Vector3 OldForward { get; private set; }

        /// <summary>
        ///     Gets the new <see cref="UxrAvatar" /> forward vector.
        /// </summary>
        public Vector3 NewForward { get; private set; }

        /// <summary>
        ///     Gets the old <see cref="UxrAvatar" /> local to world matrix.
        /// </summary>
        public Matrix4x4 OldWorldMatrix { get; private set; }

        /// <summary>
        ///     Gets the new <see cref="UxrAvatar" /> local to world matrix.
        /// </summary>
        public Matrix4x4 NewWorldMatrix { get; private set; }

        /// <summary>
        ///     Gets whether the avatar has changed its position.
        /// </summary>
        public bool HasTranslation { get; private set; }

        /// <summary>
        ///     Gets whether the avatar has changed its rotation.
        /// </summary>
        public bool HasRotation { get; private set; }

        /// <summary>
        ///     Gets the object that originated the avatar movement, if any.
        /// </summary>
        public object Source { get; private set; }

        /// <summary>
        ///     Gets whether the avatar movement was originated by a locomotion component.
        /// </summary>
        public bool IsLocomotion => Source is UxrLocomotion;

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Instances should not be created directly. Use <see cref="GetFromPool" /> to retrieve a pooled instance.
        /// </remarks>
        public UxrAvatarMoveEventArgs()
        {
        }

        #endregion

        #region Public Overrides object

        /// <inheritdoc />
        public override string ToString()
        {
            if (HasTranslation && HasRotation)
            {
                return $"Avatar moved (OldPosition={OldPosition}, OldRotation={OldRotation}, NewPosition={NewPosition}, NewRotation={NewRotation})";
            }

            if (HasTranslation)
            {
                return $"Avatar moved (OldPosition={OldPosition}, NewPosition={NewPosition})";
            }

            return $"Avatar moved (OldRotation={OldPosition}, NewRotation={NewPosition})";
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets a new instance from the pool.
        /// </summary>
        /// <param name="avatar">Avatar moved reference</param>
        /// <param name="oldPosition">Old <see cref="UxrAvatar" /> position</param>
        /// <param name="oldRotation">Old <see cref="UxrAvatar" /> rotation</param>
        /// <param name="newPosition">New <see cref="UxrAvatar" /> position</param>
        /// <param name="newRotation">New <see cref="UxrAvatar" /> rotation</param>
        /// <param name="source">Optional object that originated the movement.</param>
        /// <returns>Instance from the pool</returns>
        public static UxrAvatarMoveEventArgs GetFromPool(UxrAvatar avatar, Vector3 oldPosition, Quaternion oldRotation, Vector3 newPosition, Quaternion newRotation, object source = null)
        {
            UxrAvatarMoveEventArgs e = GetFromPool(avatar);

            e.OldPosition = oldPosition;
            e.OldRotation = oldRotation;
            e.NewPosition = newPosition;
            e.NewRotation = newRotation;
            e.Source      = source;

            e.ComputeInternalData();
            return e;
        }

        /// <summary>
        ///     Reorients and repositions a transform so that it keeps the relative position/orientation to the avatar after the
        ///     position changed event.
        /// </summary>
        /// <param name="transform">Transform to reorient/reposition</param>
        public void ReorientRelativeToAvatar(Transform transform)
        {
            GetKeepRelativeOrientationToAvatar(transform, out Vector3 position, out Quaternion rotation);
            transform.SetPositionAndRotation(position, rotation);
        }

        /// <summary>
        ///     Gets the new position and rotation an object would need to have to keep the same relative position/rotation to
        ///     the avatar after moving.
        /// </summary>
        /// <param name="transform">The transform to get the new position/rotation of</param>
        /// <param name="position">The new position</param>
        /// <param name="rotation">The new rotation</param>
        public void GetKeepRelativeOrientationToAvatar(Transform transform, out Vector3 position, out Quaternion rotation)
        {
            Vector3    relativePos = _oldWorldMatrixInverse.MultiplyPoint(transform.position);
            Quaternion relativeRot = _oldRotationInverse * transform.rotation;

            position = NewWorldMatrix.MultiplyPoint(relativePos);
            rotation = NewRotation * relativeRot;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Computes the helper properties and internal variables.
        /// </summary>
        private void ComputeInternalData()
        {
            OldForward     = OldRotation * Vector3.forward;
            NewForward     = NewRotation * Vector3.forward;
            OldWorldMatrix = Matrix4x4.TRS(OldPosition, OldRotation, Vector3.one);
            NewWorldMatrix = Matrix4x4.TRS(NewPosition, NewRotation, Vector3.one);

            _oldWorldMatrixInverse = OldWorldMatrix.inverse;
            _oldRotationInverse    = Quaternion.Inverse(OldRotation);

            HasTranslation = OldPosition != NewPosition;
            HasRotation    = OldRotation != NewRotation;
        }

        #endregion

        #region Private Types & Data

        private Matrix4x4  _oldWorldMatrixInverse;
        private Quaternion _oldRotationInverse;

        #endregion
    }
}