// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrLookAtLocalAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UltimateXR.Extensions.Unity;
using UnityEngine;

namespace UltimateXR.Animation.Transforms
{
    /// <summary>
    ///     Component that allows to continuously orientate an object looking at the local avatar camera.
    ///     If there is no local avatar, it will use the first enabled camera.
    /// </summary>
    public class UxrLookAtLocalAvatar : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private bool _allowRotateAroundY = true;
        [SerializeField] private bool _allowRotateAroundX = true;
        [SerializeField] private bool _invertedForwardAxis;
        [SerializeField] private bool _onlyOnce;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Should the lookAt alter the rotation around the vertical axis?
        /// </summary>
        public bool AllowRotateAroundVerticalAxis
        {
            get => _allowRotateAroundY;
            set => _allowRotateAroundY = value;
        }

        /// <summary>
        ///     Should the lookAt alter the rotation around the horizontal axis?
        /// </summary>
        public bool AllowRotateAroundHorizontalAxis
        {
            get => _allowRotateAroundX;
            set => _allowRotateAroundX = value;
        }

        /// <summary>
        ///     If true, the target's forward axis will try to point at the opposite direction where the
        ///     avatar is. By default this is false, meaning the forward vector will try to point at
        ///     the avatar.
        /// </summary>
        public bool InvertedForwardAxis
        {
            get => _invertedForwardAxis;
            set => _invertedForwardAxis = value;
        }

        /// <summary>
        ///     If true, will only perform the lookat the first time it is called. Useful for explosions
        ///     or similar effects in VR.
        /// </summary>
        public bool OnlyOnce
        {
            get => _onlyOnce;
            set => _onlyOnce = true;
        }

        /// <summary>
        ///     Gets or sets an override transform that will be used, if non-null, instead of the local avatar camera.
        /// </summary>
        public Transform OverrideTargetTransform { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Makes an object look at the local avatar a single time.
        /// </summary>
        /// <param name="gameObject">The object that will look at the local avatar</param>
        /// <param name="allowRotateAroundVerticalAxis">
        ///     Should the lookAt alter the rotation around the vertical axis?
        /// </param>
        /// <param name="allowRotateAroundHorizontalAxis">
        ///     Should the lookAt alter the rotation around the horizontal axis?
        /// </param>
        /// <param name="invertedForwardAxis">
        ///     If true, the target's forward axis will try to point at the opposite direction where the avatar is. By default this
        ///     is false, meaning the forward vector will try to point at the avatar
        /// </param>
        public static void MakeLookAtOnlyOnce(GameObject gameObject, bool allowRotateAroundVerticalAxis, bool allowRotateAroundHorizontalAxis, bool invertedForwardAxis)
        {
            PerformLookAt(gameObject.transform, null, allowRotateAroundVerticalAxis, allowRotateAroundHorizontalAxis, invertedForwardAxis);
        }

        /// <summary>
        ///     Removes an UxrLookAtLocalAvatar component if it exists.
        /// </summary>
        /// <param name="gameObject">The GameObject to remove the component from</param>
        public static void RemoveLookAt(GameObject gameObject)
        {
            if (gameObject)
            {
                UxrLookAtLocalAvatar lookAtComponent = gameObject.GetComponent<UxrLookAtLocalAvatar>();

                if (lookAtComponent)
                {
                    Destroy(lookAtComponent);
                }
            }
        }

        /// <summary>
        ///     Makes an object look at the local avatar continuously over time.
        /// </summary>
        /// <param name="sourceObject">The object that will look at the local avatar</param>
        /// <param name="allowRotateAroundVerticalAxis">
        ///     Should the lookAt alter the rotation around the vertical axis?
        /// </param>
        /// <param name="allowRotateAroundHorizontalAxis">
        ///     Should the lookAt alter the rotation around the horizontal axis?
        /// </param>
        /// <param name="invertedForwardAxis">
        ///     If true, the target's forward axis will try to point at the opposite direction where the avatar is. By default this
        ///     is false, meaning the forward vector will try to point at the avatar
        /// </param>
        /// <returns>The look-at component</returns>
        public UxrLookAtLocalAvatar MakeLookAt(GameObject sourceObject, bool allowRotateAroundVerticalAxis, bool allowRotateAroundHorizontalAxis, bool invertedForwardAxis)
        {
            UxrLookAtLocalAvatar lookAtComponent = sourceObject.GetOrAddComponent<UxrLookAtLocalAvatar>();

            lookAtComponent._allowRotateAroundY  = allowRotateAroundVerticalAxis;
            lookAtComponent._allowRotateAroundX  = allowRotateAroundHorizontalAxis;
            lookAtComponent._invertedForwardAxis = invertedForwardAxis;

            return lookAtComponent;
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Subscribes to events.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrManager.AvatarsUpdated += UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrManager.AvatarsUpdated -= UxrManager_AvatarsUpdated;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called after avatars are updated. Performs look at.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            if (_repeat)
            {
                PerformLookAt(transform, OverrideTargetTransform, _allowRotateAroundY, _allowRotateAroundX, _invertedForwardAxis);

                if (_onlyOnce)
                {
                    _repeat = false;
                }
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Performs look at.
        /// </summary>
        /// <param name="transform">The Transform that will look at the local avatar</param>
        /// <param name="overrideTargetTransform">If non-null, it will be used as lookAt target instead of local avatar camera</param>
        /// <param name="allowRotateAroundVerticalAxis">
        ///     Should the lookAt alter the rotation around the vertical axis?
        /// </param>
        /// <param name="allowRotateAroundHorizontalAxis">
        ///     Should the lookAt alter the rotation around the horizontal axis?
        /// </param>
        /// <param name="invertedForwardAxis">
        ///     If true, the target's forward axis will try to point at the opposite direction where the avatar is. By default this
        ///     is false, meaning the forward vector will try to point at the avatar
        /// </param>
        private static void PerformLookAt(Transform transform, Transform overrideTargetTransform, bool allowRotateAroundVerticalAxis, bool allowRotateAroundHorizontalAxis, bool invertedForwardAxis)
        {
            Transform targetTransform = overrideTargetTransform ?? UxrAvatar.LocalOrFirstEnabledCamera?.transform;

            if (targetTransform == null)
            {
                return;
            }

            Vector3 lookAt = targetTransform.position - transform.position;

            if (allowRotateAroundHorizontalAxis == false)
            {
                lookAt.y = 0.0f;
            }

            if (allowRotateAroundVerticalAxis == false)
            {
                lookAt = Vector3.ProjectOnPlane(lookAt, transform.right);
            }

            if (lookAt != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(invertedForwardAxis ? -lookAt : lookAt);
            }
        }

        #endregion

        #region Private Types & Data

        private bool _repeat = true;

        #endregion
    }
}