// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrLocomotion.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Components.Composite;
using UltimateXR.Manipulation;
using UnityEngine;

namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Base class for locomotion components. Locomotion components enable different ways for an <see cref="UxrAvatar" />
    ///     to move around the scenario.
    /// </summary>
    public abstract class UxrLocomotion : UxrAvatarComponent<UxrLocomotion>, IUxrLocomotionUpdater
    {
        #region Public Types & Data

        /// <summary>
        ///     <para>
        ///         Gets whether the locomotion updates the avatar each frame. An example of smooth locomotion is
        ///         <see cref="UxrSmoothLocomotion" /> where the user moves the avatar identically to an FPS video-game.
        ///         An example of non-smooth locomotion is <see cref="UxrTeleportLocomotion" /> where the avatar is moved only on
        ///         specific occasions.
        ///     </para>
        ///     <para>
        ///         The smooth locomotion concept should not be confused with the ability to move the head around each frame.
        ///         Smooth locomotion refers to the avatar position, which is determined by the avatar's root GameObject.
        ///         It should also not be confused with the ability to perform teleportation smoothly. Even if some
        ///         teleportation locomotion methods can teleport using smooth transitions, it should not be considered as smooth
        ///         locomotion.
        ///     </para>
        ///     <para>
        ///         The smooth locomotion property can be used to determine whether certain operations, such as LOD switching,
        ///         should be processed each frame or only when the avatar position changed.
        ///     </para>
        /// </summary>
        public abstract bool IsSmoothLocomotion { get; }

        /// <summary>
        ///     Gets the colliders used for body collision checks in the locomotion system.
        ///     Can be used to exclude objects from blocking avatar movement.
        /// </summary>
        public virtual IReadOnlyList<Collider> BodyColliders => Array.Empty<Collider>();

        #endregion

        #region Explicit IUxrLocomotionUpdater

        /// <inheritdoc />
        void IUxrLocomotionUpdater.UpdateLocomotion()
        {
            UpdateLocomotion();
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Checks whether a raycast finds anything blocking. It filters out invalid raycasts such as against anything
        ///     part of the avatar or a grabbed object. It also processes <see cref="UxrLocomotionRaycastFilter" /> components that
        ///     were hit depending on <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar to compute the raycast for</param>
        /// <param name="origin">Ray origin</param>
        /// <param name="direction">Ray direction</param>
        /// <param name="maxDistance">Raycast maximum distance</param>
        /// <param name="layerMaskRaycast">Raycast layer mask</param>
        /// <param name="queryTriggerInteraction">Behaviour against trigger colliders</param>
        /// <param name="outputHit">Result blocking raycast</param>
        /// <returns>Whether there is a blocking raycast returned in <paramref name="outputHit" /></returns>
        public static bool HasBlockingRaycastHit(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Vector3 origin, Vector3 direction, float maxDistance, int layerMaskRaycast, QueryTriggerInteraction queryTriggerInteraction, out RaycastHit outputHit)
        {
            int hitCount = Physics.RaycastNonAlloc(origin, direction.normalized, s_raycastHitsBuffer, maxDistance, layerMaskRaycast, queryTriggerInteraction);
            ClampHitCountIfNeeded(hitCount, ref s_raycastHitsBuffer);

            return HasBlockingRaycastHit(castPurpose, avatar, s_raycastHitsBuffer, hitCount, out outputHit);
        }

        /// <summary>
        ///     Checks whether a capsule cast finds anything blocking. It filters out invalid positives such as against
        ///     anything part of the avatar or a grabbed object. It also processes <see cref="UxrLocomotionRaycastFilter" /> that
        ///     were hit depending on <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar to compute the capsule cast for</param>
        /// <param name="point1">The center of the sphere at the start of the capsule</param>
        /// <param name="point2">The center of the sphere at the end of the capsule</param>
        /// <param name="radius">The radius of the capsule</param>
        /// <param name="direction">The direction into which to sweep the capsule</param>
        /// <param name="maxDistance">The max length of the sweep</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a capsule</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers</param>
        /// <param name="outputHit">Result blocking raycast</param>
        /// <returns>Whether there is a blocking raycast returned in <paramref name="outputHit" /></returns>
        public static bool HasBlockingCapsuleCastHit(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Vector3 point1, Vector3 point2, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, out RaycastHit outputHit)
        {
            int hitCount = Physics.CapsuleCastNonAlloc(point1, point2, radius, direction, s_raycastHitsBuffer, maxDistance, layerMask, queryTriggerInteraction);
            ClampHitCountIfNeeded(hitCount, ref s_raycastHitsBuffer);

            return HasBlockingRaycastHit(castPurpose, avatar, s_raycastHitsBuffer, hitCount, out outputHit);
        }

        /// <summary>
        ///     Checks whether a capsule overlap finds anything blocking. It filters out invalid positives such as against
        ///     anything part of the avatar or a grabbed object. It also processes <see cref="UxrLocomotionRaycastFilter" /> that
        ///     were hit depending on <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar to compute the capsule cast for</param>
        /// <param name="point1">The center of the sphere at the start of the capsule</param>
        /// <param name="point2">The center of the sphere at the end of the capsule</param>
        /// <param name="radius">The radius of the capsule</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a capsule</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers</param>
        /// <returns>The blocking collider or null if there is no collider blocking</returns>
        public static Collider HasBlockingCapsuleOverlap(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Vector3 point1, Vector3 point2, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            int hitCount = Physics.OverlapCapsuleNonAlloc(point1, point2, radius, s_colliderHitsBuffer, layerMask, queryTriggerInteraction);
            ClampHitCountIfNeeded(hitCount, ref s_colliderHitsBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = s_colliderHitsBuffer[i];
                if (HasBlockingRaycastHit(castPurpose, avatar, hitCollider))
                {
                    return hitCollider;
                }
            }

            return null;
        }

        /// <summary>
        ///     Checks whether a sphere cast finds anything blocking. It filters out invalid positives such as against
        ///     anything part of the avatar or a grabbed object. It also processes <see cref="UxrLocomotionRaycastFilter" /> that
        ///     were hit depending on <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar to compute the sphere cast for</param>
        /// <param name="point">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="direction">The direction into which to sweep the sphere</param>
        /// <param name="maxDistance">The max length of the sweep</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a capsule</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers</param>
        /// <param name="outputHit">Result blocking raycast</param>
        /// <returns>Whether there is a blocking raycast returned in <paramref name="outputHit" /></returns>
        public static bool HasBlockingSphereCastHit(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Vector3 point, float radius, Vector3 direction, float maxDistance, int layerMask, QueryTriggerInteraction queryTriggerInteraction, out RaycastHit outputHit)
        {
            int hitCount = Physics.SphereCastNonAlloc(point, radius, direction, s_raycastHitsBuffer, maxDistance, layerMask, queryTriggerInteraction);
            ClampHitCountIfNeeded(hitCount, ref s_raycastHitsBuffer);

            return HasBlockingRaycastHit(castPurpose, avatar, s_raycastHitsBuffer, hitCount, out outputHit);
        }

        /// <summary>
        ///     Checks whether a sphere overlap finds anything blocking. It filters out invalid positives such as against
        ///     anything part of the avatar or a grabbed object. It also processes <see cref="UxrLocomotionRaycastFilter" /> that
        ///     were hit depending on <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar to compute the sphere cast for</param>
        /// <param name="point">The center of the sphere</param>
        /// <param name="radius">The radius of the sphere</param>
        /// <param name="layerMask">A layer mask that is used to selectively ignore colliders when casting a capsule</param>
        /// <param name="queryTriggerInteraction">Specifies whether this query should hit Triggers</param>
        /// <returns>The blocking collider or null if there is no collider blocking</returns>
        public static Collider HasBlockingSphereOverlap(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Vector3 point, float radius, int layerMask, QueryTriggerInteraction queryTriggerInteraction)
        {
            int hitCount = Physics.OverlapSphereNonAlloc(point, radius, s_colliderHitsBuffer, layerMask, queryTriggerInteraction);
            ClampHitCountIfNeeded(hitCount, ref s_colliderHitsBuffer);

            for (int i = 0; i < hitCount; i++)
            {
                Collider hitCollider = s_colliderHitsBuffer[i];
                if (HasBlockingRaycastHit(castPurpose, avatar, hitCollider))
                {
                    return hitCollider;
                }
            }

            return null;
        }

        /// <summary>
        ///     Checks whether the given raycast hits find anything blocking.
        ///     This method filters out invalid raycasts such as against anything part the avatar or a grabbed object. It also
        ///     processes <see cref="UxrLocomotionRaycastFilter" /> components that were hit depending on
        ///     <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar the ray-casting was computed for</param>
        /// <param name="inputHits">Set of raycast hits to check</param>
        /// <param name="hitCount">The number of hits contained in <paramref name="inputHits" /></param>
        /// <param name="outputHit">Result blocking raycast</param>
        /// <returns>Whether there is a blocking raycast returned in <paramref name="outputHit" /></returns>
        public static bool HasBlockingRaycastHit(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, RaycastHit[] inputHits, int hitCount, out RaycastHit outputHit)
        {
            bool hasBlockingHit = false;
            outputHit = default;

            if (hitCount > 1)
            {
                Array.Sort(inputHits, 0, hitCount, RaycastHitDistanceComparer.Instance);
            }

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit singleHit = inputHits[i];
                if (!HasBlockingRaycastHit(castPurpose, avatar, singleHit.collider))
                {
                    continue;
                }

                outputHit      = singleHit;
                hasBlockingHit = true;
                break;
            }

            return hasBlockingHit;
        }

        /// <summary>
        ///     Checks whether the given collider is blocking.
        ///     This method filters out invalid colliders such as against anything part the avatar or a grabbed object. It also
        ///     processes <see cref="UxrLocomotionRaycastFilter" /> components that were hit depending on
        ///     <paramref name="castPurpose" />.
        /// </summary>
        /// <param name="castPurpose">The purpose of the cast</param>
        /// <param name="avatar">The avatar the ray-casting was computed for</param>
        /// <param name="hitCollider">The collider to process</param>
        /// <returns>Whether the collider is a blocking</returns>
        public static bool HasBlockingRaycastHit(UxrLocomotionRaycastPurpose castPurpose, UxrAvatar avatar, Collider hitCollider)
        {
            if (hitCollider.GetComponentInParent<UxrAvatar>() == avatar)
            {
                // Filter out colliding against part of the avatar
                return false;
            }

            if (castPurpose != UxrLocomotionRaycastPurpose.Other)
            {
                // Process casting filters if necessary

                UxrLocomotionRaycastFilter filter = hitCollider.GetComponentInParent<UxrLocomotionRaycastFilter>();

                if (filter != null && filter.enabled)
                {
                    if (castPurpose == UxrLocomotionRaycastPurpose.Targeting && !filter.BlockTargeting)
                    {
                        // Filter out colliding against a filter that doesn't block targeting.
                        return false;
                    }

                    if (castPurpose == UxrLocomotionRaycastPurpose.Validation && !filter.BlockValidation)
                    {
                        // Filter out colliding against a filter that doesn't block validation.
                        return false;
                    }
                }
            }

            UxrGrabbableObject grabbableObject = hitCollider.GetComponentInParent<UxrGrabbableObject>();

            if (grabbableObject != null && UxrGrabManager.Instance.IsBeingGrabbedBy(grabbableObject, avatar))
            {
                // Filter out colliding against a grabbed object
                return false;
            }

            return true;
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Logs if there is a missing <see cref="Avatar" /> component upwards in the hierarchy.
        ///     Sets up the collision filtering system if required.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            if (Avatar == null)
            {
                UxrManager.LogMissingAvatarInHierarchyError(this);
            }
        }

        /// <summary>
        ///     Sets up the collision filtering.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            // Subscribe to events to enable collision filtering on grab and disable it on release.
            UxrGrabManager.Instance.ObjectGrabbed  += GrabManager_ObjectGrabbed;
            UxrGrabManager.Instance.ObjectReleased += GrabManager_ObjectPlacedOrReleased;
            UxrGrabManager.Instance.ObjectPlaced   += GrabManager_ObjectPlacedOrReleased;

            // Enable collision filtering for all objects being currently grabbed.
            foreach (UxrGrabbableObject grabbableObject in UxrGrabManager.Instance.GetObjectsBeingGrabbed())
            {
                SetCollisionFilteringEnabled(BodyColliders, grabbableObject, true);
            }
        }

        /// <summary>
        ///     Sets up the collision filtering.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrGrabManager.Instance.ObjectGrabbed  -= GrabManager_ObjectGrabbed;
            UxrGrabManager.Instance.ObjectReleased -= GrabManager_ObjectPlacedOrReleased;
            UxrGrabManager.Instance.ObjectPlaced   -= GrabManager_ObjectPlacedOrReleased;

            // Disable collision filtering for all objects being currently grabbed.
            foreach (UxrGrabbableObject grabbableObject in UxrGrabManager.Instance.GetObjectsBeingGrabbed())
            {
                SetCollisionFilteringEnabled(BodyColliders, grabbableObject, false);
            }

            // If there was a transition active, handle it.
            if (_disableCollisionFilteringGrabbableObject != null)
            {
                StopCoroutine(_disableCollisionFilteringCoroutine);
                SetCollisionFilteringEnabled(BodyColliders, _disableCollisionFilteringGrabbableObject, false);
                _disableCollisionFilteringGrabbableObject = null;
                _disableCollisionFilteringCoroutine       = null;
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Coroutine to disable collision filtering for the specified <see cref="UxrGrabbableObject" />
        ///     after a given place/release transition duration.
        /// </summary>
        /// <param name="grabbableObject">The grabbable object for which collision filtering will be disabled.</param>
        /// <param name="durationSeconds">The duration in seconds to wait before disabling collision filtering.</param>
        /// <returns>An enumerator for coroutine execution.</returns>
        private IEnumerator DisableCollisionFilteringAfterTransitionCoroutine(UxrGrabbableObject grabbableObject, float durationSeconds)
        {
            // Wait for the transition to end.

            _disableCollisionFilteringGrabbableObject = grabbableObject;
            yield return new WaitForSeconds(durationSeconds);

            // Disable collision filtering for the grabbable object.

            SetCollisionFilteringEnabled(BodyColliders, grabbableObject, false);
            _disableCollisionFilteringGrabbableObject = null;
            _disableCollisionFilteringCoroutine       = null;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Handles the event triggered when an object is grabbed. It modifies collider behavior to ignore collisions
        ///     between the grabbed object and the character controller.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="eventArgs">The event arguments containing details about the grabbed object.</param>
        private void GrabManager_ObjectGrabbed(object sender, UxrManipulationEventArgs eventArgs)
        {
            if (!eventArgs.IsGrabbedStateChanged)
            {
                return;
            }

            // Check if there is a transition active before filtering.
            if (eventArgs.GrabbableObject == _disableCollisionFilteringGrabbableObject)
            {
                StopCoroutine(_disableCollisionFilteringCoroutine);
                _disableCollisionFilteringCoroutine       = null;
                _disableCollisionFilteringGrabbableObject = null;
            }

            // Filter collisions with this avatar.
            SetCollisionFilteringEnabled(BodyColliders, eventArgs.GrabbableObject, true);
        }

        /// <summary>
        ///     Handles the event triggered when an object is released by the grab manager, restoring collision interactions
        ///     between the released object and the character controller.
        /// </summary>
        /// <param name="sender">The source object that triggered the release event.</param>
        /// <param name="eventArgs">
        ///     Event arguments containing information about the object being released, including the grabbable
        ///     object instance.
        /// </param>
        private void GrabManager_ObjectPlacedOrReleased(object sender, UxrManipulationEventArgs eventArgs)
        {
            if (!eventArgs.IsGrabbedStateChanged)
            {
                return;
            }

            // Disable collisions with this avatar after the transition duration.
            _disableCollisionFilteringCoroutine = StartCoroutine(DisableCollisionFilteringAfterTransitionCoroutine(eventArgs.GrabbableObject, UxrConstants.SmoothManipulationTransitionSeconds));
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Gets a layer mask representing all layers that the specified layer can collide with,
        ///     based on Unity's Physics Layer Collision Matrix.
        /// </summary>
        /// <param name="layer">
        ///     The layer to query.
        /// </param>
        /// <returns>
        ///     A bitmask containing all layers that are not ignored by the specified layer in the
        ///     Physics collision matrix.
        /// </returns>
        /// <remarks>
        ///     This method reconstructs the effective collision mask using
        ///     <see cref="Physics.GetIgnoreLayerCollision(int, int)" />.
        /// </remarks>
        protected static int GetCollisionMask(int layer)
        {
            int mask = 0;

            for (int i = 0; i < 32; i++)
            {
                if (!Physics.GetIgnoreLayerCollision(layer, i))
                {
                    mask |= 1 << i;
                }
            }

            return mask;
        }

        /// <summary>
        ///     Updates the locomotion and the avatar's position/orientation the component belongs to.
        /// </summary>
        protected abstract void UpdateLocomotion();

        #endregion

        #region Private Methods

        /// <summary>
        ///     Ensures a non-alloc physics query result count does not exceed the current buffer capacity by growing the buffer
        ///     when it is full.
        /// </summary>
        /// <typeparam name="T">The type of elements in the buffer.</typeparam>
        /// <param name="hitCount">The number of hits detected, which may exceed the current buffer size.</param>
        /// <param name="buffer">
        ///     The array buffer used to store hit results. It may be resized if the hit count exceeds its
        ///     capacity.
        /// </param>
        private static void ClampHitCountIfNeeded<T>(int hitCount, ref T[] buffer)
        {
            if (hitCount < buffer.Length)
            {
                return;
            }

            Array.Resize(ref buffer, buffer.Length * 2);
        }

        /// <summary>
        ///     Enables or disables collision filtering between the specified body colliders and the colliders
        ///     of a given grabbable object.
        /// </summary>
        /// <param name="bodyColliders">The list of colliders associated with the avatar's body.</param>
        /// <param name="grabbableObject">The grabbable object whose colliders are being filtered.</param>
        /// <param name="ignoreCollisions">Whether collision filtering should be enabled (true) or disabled (false).</param>
        private void SetCollisionFilteringEnabled(IReadOnlyList<Collider> bodyColliders, UxrGrabbableObject grabbableObject, bool ignoreCollisions)
        {
            foreach (Collider bodyCollider in bodyColliders)
            {
                foreach (Collider grabbableCollider in grabbableObject.GetComponentsInChildren<Collider>(true))
                {
                    Physics.IgnoreCollision(bodyCollider, grabbableCollider, ignoreCollisions);
                }
            }
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Comparer used to sort instances of <see cref="RaycastHit" /> by their distance.
        ///     This is used by the Array.Sort overload method that we need for our no-allocations code.
        /// </summary>
        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            #region Constructors & Finalizer

            private RaycastHitDistanceComparer()
            {
            }

            #endregion

            #region Implicit IComparer<RaycastHit>

            public int Compare(RaycastHit a, RaycastHit b)
            {
                return a.distance.CompareTo(b.distance);
            }

            #endregion

            #region Public Types & Data

            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            #endregion
        }

        private static RaycastHit[] s_raycastHitsBuffer  = new RaycastHit[16];
        private static Collider[]   s_colliderHitsBuffer = new Collider[16];

        private Coroutine          _disableCollisionFilteringCoroutine;
        private UxrGrabbableObject _disableCollisionFilteringGrabbableObject;

        #endregion
    }
}