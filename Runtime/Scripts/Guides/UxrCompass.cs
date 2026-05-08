// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrCompass.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Attributes;
using UltimateXR.Avatar;
using UltimateXR.CameraUtils;
using UltimateXR.Core.Components.Singleton;
using UltimateXR.Extensions.Unity.Render;
using UnityEngine;

namespace UltimateXR.Guides
{
    /// <summary>
    ///     Compass component that guides the user by providing visual hints indicating where to look or what action to
    ///     perform.
    ///     It displays an arrow in front of the view to help bring the target into sight.
    ///     Once the target is visible, it can optionally display an action icon:
    ///     <list type="bullet">
    ///         <item>Location: Indicates where the user should move next.</item>
    ///         <item>Grab: Indicates that an object should be grabbed.</item>
    ///         <item>Look: Draws attention to a specific object.</item>
    ///         <item>Use: Indicates that an interaction should be performed on an object.</item>
    ///     </list>
    /// </summary>
    /// <remarks>
    ///     As a <see cref="UxrSingleton{T}" />, the compass is globally accessible and unique.
    ///     It can be invoked from anywhere using <see cref="UxrCompass.Instance" />.
    /// </remarks>
    public partial class UxrCompass : UxrSingleton<UxrCompass>
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [StylishFoldout("Compass")] private CompassParameters _icons;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets or sets whether the compass is rendered.
        /// </summary>
        public static bool Enabled
        {
            get => s_enabled;
            set
            {
                s_enabled = value;

                if (!HasInstance)
                {
                    return;
                }

                if (value)
                {
                    return;
                }

                Instance._icons._compassArrowPivot.gameObject.SetActive(false);
                Instance._icons._transitionArrow.gameObject.SetActive(false);
                Instance._icons._rootOnScreenIcons.SetActive(false);
            }
        }

        /// <summary>
        ///     Gets whether the compass is currently focused on an object.
        /// </summary>
        public bool HasTarget => SelectedTarget.HasTarget;

        /// <summary>
        ///     Gets the target's <see cref="Transform" />.
        /// </summary>
        public Transform TargetTransform => SelectedTarget.TargetHint != null ? SelectedTarget.TargetHint.GetTransform(this) : SelectedTarget.TargetTransform;

        /// <summary>
        ///     Gets the target's position.
        /// </summary>
        public Vector3 TargetPosition
        {
            get
            {
                if (SelectedTarget.RawPosition.HasValue)
                {
                    return SelectedTarget.RawPosition.Value;
                }

                return SelectedTarget.TargetTransform != null ? SelectedTarget.TargetTransform.position : Vector3.zero;
            }
        }

        /// <summary>
        ///     Gets or sets the current display mode.
        /// </summary>
        public UxrCompassDisplayMode DisplayMode => SelectedTarget.DisplayMode;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Sets the current target.
        /// </summary>
        /// <param name="target">New target or null to stop</param>
        /// <param name="displayMode">The display mode</param>
        /// <param name="iconScale">The icon size multiplier</param>
        /// <param name="highPriority">Whether the target is high-priority and will override all other target requests</param>
        public void SetTarget(Transform target, UxrCompassDisplayMode displayMode = UxrCompassDisplayMode.OnlyCompass, float iconScale = 1.0f, bool highPriority = false)
        {
            CompassTargetEntry entry = highPriority ? _highPriorityTarget : _defaultTarget;
            entry.DisplayMode       = displayMode;
            entry.StartTime         = Time.unscaledTime;
            entry.OnScreenStartTime = Time.unscaledTime;
            entry.RawPosition       = null;
            entry.TargetHint        = target != null ? target.gameObject.GetComponent<UxrCompassTargetHint>() : null;
            entry.TargetTransform   = target;
            entry.IconScale         = iconScale;
            entry.IsTemporary       = false;
        }

        /// <summary>
        ///     Sets the current target. When the object gets into sight, it will show the icon described by
        ///     <paramref name="displayMode" /> during a limited amount of time (<see cref="TemporaryDurationSeconds" />). The
        ///     timer is reset each time the object gets out of sight.
        /// </summary>
        /// <param name="target">New target or null to stop</param>
        /// <param name="displayMode">The display mode</param>
        /// <param name="iconScale">The icon size multiplier</param>
        /// <param name="highPriority">Whether the target is high-priority and will override all other target requests</param>
        public void SetTargetTemporary(Transform target, UxrCompassDisplayMode displayMode = UxrCompassDisplayMode.OnlyCompass, float iconScale = 1.0f, bool highPriority = false)
        {
            SetTarget(target, displayMode, iconScale, highPriority);
            (highPriority ? _highPriorityTarget : _defaultTarget).IsTemporary = true;
        }

        /// <summary>
        ///     Sets the current target.
        /// </summary>
        /// <param name="position">The target position</param>
        /// <param name="displayMode">The display mode</param>
        /// <param name="iconScale">The icon size multiplier</param>
        /// <param name="highPriority">Whether the target is high-priority and will override all other target requests</param>
        public void SetTarget(Vector3 position, UxrCompassDisplayMode displayMode = UxrCompassDisplayMode.OnlyCompass, float iconScale = 1.0f, bool highPriority = false)
        {
            CompassTargetEntry entry = highPriority ? _highPriorityTarget : _defaultTarget;
            entry.DisplayMode       = displayMode;
            entry.TargetTransform   = null;
            entry.StartTime         = Time.unscaledTime;
            entry.OnScreenStartTime = Time.unscaledTime;
            entry.RawPosition       = position;
            entry.TargetHint        = null;
            entry.IconScale         = iconScale;
            entry.IsTemporary       = false;
        }

        /// <summary>
        ///     Sets the current target. When the object gets into sight it will show the icon described by
        ///     <paramref name="displayMode" /> during a limited amount of time (<see cref="TemporaryDurationSeconds" />). The
        ///     timer is reset each time the object gets out of sight.
        /// </summary>
        /// <param name="position">The target position</param>
        /// <param name="displayMode">The display mode</param>
        /// <param name="iconScale">The icon size multiplier</param>
        /// <param name="highPriority">Whether the target is high-priority and will override all other target requests</param>
        public void SetTargetTemporary(Vector3 position, UxrCompassDisplayMode displayMode = UxrCompassDisplayMode.OnlyCompass, float iconScale = 1.0f, bool highPriority = false)
        {
            SetTarget(position, displayMode, iconScale, highPriority);
            (highPriority ? _highPriorityTarget : _defaultTarget).IsTemporary = true;
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the compass.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _icons._compassArrowPivot.gameObject.SetActive(false);
            _icons._transitionArrow.gameObject.SetActive(false);
            _icons._rootOnScreenIcons.SetActive(false);

            _initialIconScales = new Dictionary<MeshRenderer, Vector3>();

            foreach (MeshRenderer iconRenderer in IconRenderers)
            {
                _initialIconScales.Add(iconRenderer, iconRenderer.transform.localScale);
            }

            _defaultTarget      = new CompassTargetEntry();
            _highPriorityTarget = new CompassTargetEntry();
        }

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrCameraWallFade.FadeEntered += UxrCameraWallFade_FadeEntered;
            UxrCameraWallFade.FadeExited  += UxrCameraWallFade_FadeExited;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrCameraWallFade.FadeEntered -= UxrCameraWallFade_FadeEntered;
            UxrCameraWallFade.FadeExited  -= UxrCameraWallFade_FadeExited;
        }

        /// <summary>
        ///     Updates the compass.
        /// </summary>
        protected void Update()
        {
            if (!s_enabled)
            {
                return;
            }

            if (!HasTarget)
            {
                // No object focused anymore
                if (_targetFocused)
                {
                    _targetFocused = false;

                    if (_coroutineArrowTransition != null)
                    {
                        StopCoroutine(_coroutineArrowTransition);
                    }

                    _icons._compassArrowPivot.gameObject.SetActive(false);
                    _icons._transitionArrow.gameObject.SetActive(false);
                    _icons._rootOnScreenIcons.SetActive(false);
                }
            }
            else
            {
                // Object focused. Check if the object is onscreen or offscreen to show compass or bouncing arrow.
                // Also check if we need to trigger the transition arrow when going from offscreen to onscreen.
                if (!_targetFocused)
                {
                    _targetFocused = true;
                }

                if (SelectedTarget.IsTemporary && Time.unscaledTime - SelectedTarget.StartTime > TemporaryDurationSeconds)
                {
                    SetTarget(null, highPriority: SelectedTarget == _highPriorityTarget);
                    return;
                }

                Camera  avatarCamera      = UxrAvatar.LocalAvatarCamera;
                Vector3 targetInCameraPos = avatarCamera.WorldToScreenPoint(TargetPosition);
                float   percentMargin     = 0.20f;
                float   marginWidth       = avatarCamera.pixelWidth  * percentMargin;
                float   marginHeight      = avatarCamera.pixelHeight * percentMargin;

                if (targetInCameraPos.x >= marginWidth                             &&
                    targetInCameraPos.x <= avatarCamera.pixelWidth - marginWidth   &&
                    targetInCameraPos.y >= marginHeight                            &&
                    targetInCameraPos.y <= avatarCamera.pixelHeight - marginHeight &&
                    targetInCameraPos.z > 0.0f)
                {
                    // Object onscreen
                    if (!_icons._rootOnScreenIcons.activeSelf && !_icons._transitionArrow.gameObject.activeSelf)
                    {
                        // Transition offscreen -> onscreen
                        _icons._transitionArrow.gameObject.SetActive(true);

                        if (_coroutineArrowTransition != null)
                        {
                            StopCoroutine(_coroutineArrowTransition);
                        }

                        _coroutineArrowTransition = StartCoroutine(ArrowTransitionCoroutine(_icons._compassArrowRenderer.transform.position, TargetPosition));
                    }

                    _icons._rootOnScreenIcons.transform.position = TargetPosition;
                    _icons._compassArrowPivot.gameObject.SetActive(false);
                    UpdateOnScreenIcon(Time.unscaledTime);
                }
                else
                {
                    // Object offscreen -> show compass
                    _icons._rootOnScreenIcons.gameObject.SetActive(false);
                    _icons._compassArrowPivot.gameObject.SetActive(true);

                    Vector3 direction = avatarCamera.transform.InverseTransformPoint(TargetPosition);
                    direction.z = 0.0f;
                    direction.Normalize();
                    direction = new Vector3(targetInCameraPos.x - avatarCamera.pixelWidth * 0.5f, targetInCameraPos.y - avatarCamera.pixelHeight * 0.5f, 0.0f).normalized;

                    if (targetInCameraPos.z < 0.0f)
                    {
                        direction = -direction;
                    }

                    _icons._compassArrowPivot.transform.SetPositionAndRotation(avatarCamera.transform.position + avatarCamera.transform.forward * _icons._distanceToCamera, Quaternion.LookRotation(avatarCamera.transform.forward, avatarCamera.transform.TransformDirection(direction)));
                }
            }

            Color color = Color.white;
            color.a                                     = (Mathf.Sin(Time.unscaledTime * Mathf.PI * 2.0f * 5.0f) + 1.0f) * 0.5f;
            _icons._compassArrowRenderer.material.color = color;
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Coroutine that transitions between the compass arrow to the arrow that moves to the target when it comes into
        ///     sight.
        /// </summary>
        /// <param name="posStart"></param>
        /// <param name="posEnd"></param>
        /// <returns></returns>
        private IEnumerator ArrowTransitionCoroutine(Vector3 posStart, Vector3 posEnd)
        {
            _icons._transitionArrow.rotation = Quaternion.LookRotation(posEnd - posStart);

            float duration  = 0.2f;
            float startTime = Time.unscaledTime;

            while (Time.unscaledTime - startTime < duration)
            {
                float t = (Time.unscaledTime - startTime) / duration;
                _icons._transitionArrow.transform.position = Vector3.Lerp(posStart, posEnd, t);
                yield return null;
            }

            _icons._transitionArrow.gameObject.SetActive(false);

            // _onScreenStartTime will ensure that the effects will align in a cool way when the transition arrow disappears. The animation curve will always start correctly.
            SelectedTarget.OnScreenStartTime = Time.unscaledTime;
            _icons._rootOnScreenIcons.SetActive(true);
            UpdateOnScreenIcon(Time.unscaledTime);

            _coroutineArrowTransition = null;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Handles the event triggered when the local avatar enters the fade.
        /// </summary>
        /// <param name="wallFade">The wall fade that triggered the event</param>
        private void UxrCameraWallFade_FadeEntered(UxrCameraWallFade wallFade)
        {
            // Don't use this for now because it gets a little bit in the way:
            // Instance.SetTarget(wallFade.LastValidPos + wallFade.ExitNormal * 0.05f, UxrCompassDisplayMode.OnlyCompass, 0.5f, highPriority: true);
        }

        /// <summary>
        ///     Handles the event triggered when the local avatar exits the fade.
        /// </summary>
        /// <param name="wallFade">The wall fade that triggered the event</param>
        private void UxrCameraWallFade_FadeExited(UxrCameraWallFade wallFade)
        {
            // Instance.SetTarget(null, highPriority: true);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the icon.
        /// </summary>
        /// <param name="time">Time in seconds the icon has been on screen</param>
        private void UpdateOnScreenIcon(float time)
        {
            if (UxrAvatar.LocalAvatarCamera == null)
            {
                return;
            }

            float frequency         = 2.0f;
            float timeSinceOnScreen = time - SelectedTarget.OnScreenStartTime;
            float interpolationTime = timeSinceOnScreen * frequency;
            float effectBounceT     = UxrInterpolator.GetInterpolationFactor(interpolationTime, UxrEasing.EaseOutQuad,   UxrLoopMode.PingPong);
            float effectSineT       = UxrInterpolator.GetInterpolationFactor(interpolationTime, UxrEasing.EaseInOutSine, UxrLoopMode.PingPong);

            _icons._rootOnScreenIcons.transform.position = TargetPosition;

            _icons._iconLocationPivot.gameObject.SetActive(DisplayMode == UxrCompassDisplayMode.Location);
            _icons._iconLookPivot.gameObject.SetActive(DisplayMode == UxrCompassDisplayMode.Look && timeSinceOnScreen < TemporaryDurationSeconds);
            _icons._iconGrabPivot.gameObject.SetActive(DisplayMode == UxrCompassDisplayMode.Grab);
            _icons._iconUsePivot.gameObject.SetActive(DisplayMode  == UxrCompassDisplayMode.Use);

            if (DisplayMode == UxrCompassDisplayMode.Location)
            {
                _icons._iconLocationBottom.transform.localPosition = Vector3.up * (effectBounceT * 0.4f);
            }
            else if (DisplayMode == UxrCompassDisplayMode.Grab)
            {
                _icons._iconGrabRenderer.material.color = ColorExt.ColorAlpha(Color.white, effectSineT);
            }
            else if (DisplayMode == UxrCompassDisplayMode.Look)
            {
                _icons._iconLookRenderer.material.color = ColorExt.ColorAlpha(Color.white, effectSineT);
            }
            else if (DisplayMode == UxrCompassDisplayMode.Use)
            {
                _icons._iconUseRenderer.material.color = ColorExt.ColorAlpha(Color.white, effectSineT);
            }

            // Scale visible icon based on size

            _icons._rootOnScreenIcons.transform.localScale = Vector3.one * SelectedTarget.IconScale;

            foreach (KeyValuePair<MeshRenderer, Vector3> iconScale in _initialIconScales)
            {
                if (iconScale.Key.gameObject.activeInHierarchy)
                {
                    float distance = Vector3.Distance(iconScale.Key.transform.position, UxrAvatar.LocalAvatar.CameraPosition);
                    iconScale.Key.transform.localScale = Vector3.Max(iconScale.Value, distance * 0.3f * iconScale.Value);
                }
            }
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Holds serialized material references and color/width settings for the outline effect.
        /// </summary>
        [Serializable]
        private class CompassParameters
        {
            #region Inspector Properties/Serialized Fields

            [SerializeField] [Tooltip(TooltipDistanceToCamera)]     public float        _distanceToCamera = 1.0f;
            [SerializeField] [Tooltip(TooltipRootOnScreenIcons)]    public GameObject   _rootOnScreenIcons;
            [SerializeField] [Tooltip(TooltipIconLocationPivot)]    public Transform    _iconLocationPivot;
            [SerializeField] [Tooltip(TooltipIconLocationBottom)]   public Transform    _iconLocationBottom;
            [SerializeField] [Tooltip(TooltipIconLocationRenderer)] public MeshRenderer _iconLocationRenderer;
            [SerializeField] [Tooltip(TooltipIconLookPivot)]        public Transform    _iconLookPivot;
            [SerializeField] [Tooltip(TooltipIconLookRenderer)]     public MeshRenderer _iconLookRenderer;
            [SerializeField] [Tooltip(TooltipIconGrabPivot)]        public Transform    _iconGrabPivot;
            [SerializeField] [Tooltip(TooltipIconGrabRenderer)]     public MeshRenderer _iconGrabRenderer;
            [SerializeField] [Tooltip(TooltipIconUsePivot)]         public Transform    _iconUsePivot;
            [SerializeField] [Tooltip(TooltipIconUseRenderer)]      public MeshRenderer _iconUseRenderer;
            [SerializeField] [Tooltip(TooltipCompassArrowPivot)]    public Transform    _compassArrowPivot;
            [SerializeField] [Tooltip(TooltipCompassArrowRenderer)] public Renderer     _compassArrowRenderer;
            [SerializeField] [Tooltip(TooltipTransitionArrow)]      public Transform    _transitionArrow;

            #endregion

            #region Private Types & Data

            private const string TooltipDistanceToCamera     = "Distance in meters from the camera at which the compass arrow is placed when the target is offscreen.";
            private const string TooltipRootOnScreenIcons    = "Root GameObject containing all onscreen icon GameObjects shown when the target is in view.";
            private const string TooltipIconLocationPivot    = "Pivot transform for the location icon. Used to position and orient the location indicator.";
            private const string TooltipIconLocationBottom   = "Bottom transform of the location icon. Animated vertically to create a bouncing effect when on screen.";
            private const string TooltipIconLocationRenderer = "Renderer for the location icon mesh.";
            private const string TooltipIconLookPivot        = "Pivot transform for the look icon. Used to position and orient the look indicator.";
            private const string TooltipIconLookRenderer     = "Renderer for the look icon mesh. Its alpha is animated to create a pulsing fade effect when on screen.";
            private const string TooltipIconGrabPivot        = "Pivot transform for the grab icon. Used to position and orient the grab indicator.";
            private const string TooltipIconGrabRenderer     = "Renderer for the grab icon mesh. Its alpha is animated to create a pulsing fade effect when on screen.";
            private const string TooltipIconUsePivot         = "Pivot transform for the use icon. Used to position and orient the use/interaction indicator.";
            private const string TooltipIconUseRenderer      = "Renderer for the use icon mesh. Its alpha is animated to create a pulsing fade effect when on screen.";
            private const string TooltipCompassArrowPivot    = "Pivot transform for the compass arrow. Controls position and orientation of the offscreen direction indicator.";
            private const string TooltipCompassArrowRenderer = "Renderer for the compass arrow mesh. Used to animate the arrow color when pointing offscreen.";
            private const string TooltipTransitionArrow      = "Transform for the transition arrow that animates from the compass position to the target when it enters view.";

            #endregion
        }

        /// <summary>
        ///     Gets the currently selected compass target, determining whether the high-priority target or the default target is
        ///     used.
        /// </summary>
        private CompassTargetEntry SelectedTarget => _highPriorityTarget.HasTarget ? _highPriorityTarget : _defaultTarget;

        /// <summary>
        ///     Gets the icon renderer components.
        /// </summary>
        private IEnumerable<MeshRenderer> IconRenderers
        {
            get
            {
                yield return _icons._iconLocationRenderer;
                yield return _icons._iconLookRenderer;
                yield return _icons._iconGrabRenderer;
                yield return _icons._iconUseRenderer;
            }
        }

        /// <summary>
        ///     Duration in seconds to show the look icon while the target is in view. After that, do not show the look icon unless
        ///     it comes into sight again. It is also used by <see cref="SetTargetTemporary" />.
        /// </summary>
        private const float TemporaryDurationSeconds = 3.0f;

        /// <summary>
        ///     See <see cref="Enabled" />.
        /// </summary>
        private static bool s_enabled = true;

        private bool                              _targetFocused;
        private Coroutine                         _coroutineArrowTransition;
        private Dictionary<MeshRenderer, Vector3> _initialIconScales;

        private CompassTargetEntry _defaultTarget;
        private CompassTargetEntry _highPriorityTarget;

        #endregion
    }
}