// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrCameraWallFade.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Attributes;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Components.Composite;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Render;
using UltimateXR.Locomotion;
using UltimateXR.Manipulation;
using UnityEngine;

namespace UltimateXR.CameraUtils
{
    /// <summary>
    ///     Component added to a Camera that fades the view to a color when the user's head intersects geometry,
    ///     preventing visual clipping and peeking through walls.
    ///     It is also queried by <see cref="UxrLocomotion" /> components to determine whether movement
    ///     is allowed to prevent cheating through walls.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class UxrCameraWallFade : UxrAvatarComponent<UxrCameraWallFade>
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [StylishFoldout("General")]       private GeneralParameters          _generalParameters     = new GeneralParameters();
        [SerializeField] [StylishFoldout("Portal")]        private PortalParameters           _portalParameters      = new PortalParameters();
        [SerializeField] [StylishFoldout("Out-of-bounds")] private OutOfBoundsSpaceParameters _outOfBoundsParameters = new OutOfBoundsSpaceParameters();

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Called when the avatar starts intersecting with geometry.
        /// </summary>
        public static event Action<UxrCameraWallFade> Entered;

        /// <summary>
        ///     Called when the avatar starts being close enough to geometry to trigger the fade.
        /// </summary>
        public static event Action<UxrCameraWallFade> FadeEntered;

        /// <summary>
        ///     Called when the avatar exits geometry.
        /// </summary>
        public static event Action<UxrCameraWallFade> Exited;

        /// <summary>
        ///     Called when an avatar with an active wall fade moves away enough from geometry to stop the fade.
        /// </summary>
        public static event Action<UxrCameraWallFade> FadeExited;

        /// <summary>
        ///     Called every update while the avatar is within the fade zone due to proximity to or intersection with geometry.
        /// </summary>
        /// <remarks>
        ///     This event is raised continuously while fading is active. It is not invoked when the fade ends;
        ///     use <see cref="FadeExited" /> to handle fade exit.
        /// </remarks>
        public static event EventHandler<UxrWallFadeEventArgs> FadeUpdated;

        /// <summary>
        ///     Gets whether the camera is currently within the fade zone caused by intersecting or being near geometry.
        /// </summary>
        public bool IsInsideFade => IsAvatarInsideFade(Avatar);

        /// <summary>
        ///     Gets or sets the layers considered blocking geometry for the wall fade checks.
        /// </summary>
        public LayerMask CollisionLayers
        {
            get => _generalParameters._collisionLayers;
            set => _generalParameters._collisionLayers = value;
        }

        /// <summary>
        ///     Gets or sets the distance from geometry where the fade starts increasing.
        /// </summary>
        public float FadeFarDistance
        {
            get => _generalParameters._fadeFarDistance;
            set => _generalParameters._fadeFarDistance = value;
        }

        /// <summary>
        ///     Gets or sets the distance from geometry where the fade reaches full intensity.
        /// </summary>
        public float FadeNearDistance
        {
            get => _generalParameters._fadeNearDistance;
            set => _generalParameters._fadeNearDistance = value;
        }

        /// <summary>
        ///     Gets or sets the radius used to approximate the user's head.
        /// </summary>
        public float HeadRadius
        {
            get => _generalParameters._headRadius;
            set => _generalParameters._headRadius = value;
        }

        /// <summary>
        ///     Gets or sets whether wall fade checks should ignore trigger colliders.
        /// </summary>
        public bool IgnoreTriggerColliders
        {
            get => _generalParameters._ignoreTriggerColliders;
            set => _generalParameters._ignoreTriggerColliders = value;
        }

        /// <summary>
        ///     Gets or sets whether dynamic objects should be ignored by wall fade checks.
        /// </summary>
        public bool IgnoreDynamicObjects
        {
            get => _generalParameters._ignoreDynamicObjects;
            set => _generalParameters._ignoreDynamicObjects = value;
        }

        /// <summary>
        ///     Gets or sets whether grabbed objects should be ignored by wall fade checks.
        /// </summary>
        public bool IgnoreGrabbedObjects
        {
            get => _generalParameters._ignoreGrabbedObjects;
            set => _generalParameters._ignoreGrabbedObjects = value;
        }

        /// <summary>
        ///     Gets or sets the radius of the sphere used to compute the portal opening back to the safe space.
        /// </summary>
        public float PortalHoleRadius
        {
            get => _portalParameters._portalHoleRadius;
            set => _portalParameters._portalHoleRadius = value;
        }

        /// <summary>
        ///     Gets or sets the minimum radius allowed for the portal opening.
        /// </summary>
        public float PortalHoleRadiusMin
        {
            get => _portalParameters._portalHoleRadiusMin;
            set => _portalParameters._portalHoleRadiusMin = value;
        }

        /// <summary>
        ///     Gets or sets the softness of the portal edge transition.
        /// </summary>
        public float PortalHoleEdgeSoftness
        {
            get => _portalParameters._portalHoleEdgeSoftness;
            set => _portalParameters._portalHoleEdgeSoftness = value;
        }

        /// <summary>
        ///     Gets or sets the world-space size of each secondary floor and ceiling grid cell.
        /// </summary>
        public float FloorGridTileSize
        {
            get => _outOfBoundsParameters._floorGridTileSize;
            set => _outOfBoundsParameters._floorGridTileSize = value;
        }

        /// <summary>
        ///     Gets or sets the derivative-based anti-aliasing for the grid lines.
        /// </summary>
        public float FloorGridAntialiasing
        {
            get => _outOfBoundsParameters._floorGridAntialiasing;
            set => _outOfBoundsParameters._floorGridAntialiasing = value;
        }

        /// <summary>
        ///     Gets or sets the color used for the floor and ceiling near the camera.
        /// </summary>
        public Color FloorNearColor
        {
            get => _outOfBoundsParameters._floorNearColor;
            set => _outOfBoundsParameters._floorNearColor = value;
        }

        /// <summary>
        ///     Gets or sets the color used for the floor and ceiling at far distances.
        /// </summary>
        public Color FloorFarColor
        {
            get => _outOfBoundsParameters._floorFarColor;
            set => _outOfBoundsParameters._floorFarColor = value;
        }

        /// <summary>
        ///     Gets or sets the distance where the floor and ceiling start fading from the near color to the far color.
        /// </summary>
        public float FloorFarStartDistance
        {
            get => _outOfBoundsParameters._floorFarStartDistance;
            set => _outOfBoundsParameters._floorFarStartDistance = value;
        }

        /// <summary>
        ///     Gets or sets the distance where the floor and ceiling finish fading from the near color to the far color.
        /// </summary>
        public float FloorFarEndDistance
        {
            get => _outOfBoundsParameters._floorFarEndDistance;
            set => _outOfBoundsParameters._floorFarEndDistance = value;
        }

        /// <summary>
        ///     Gets or sets the color used for the main floor and ceiling grid lines.
        /// </summary>
        public Color FloorGridMainColor
        {
            get => _outOfBoundsParameters._floorGridMainColor;
            set => _outOfBoundsParameters._floorGridMainColor = value;
        }

        /// <summary>
        ///     Gets or sets the world-space thickness of the main floor and ceiling grid lines.
        /// </summary>
        public float FloorGridMainLineThickness
        {
            get => _outOfBoundsParameters._floorGridMainLineThickness;
            set => _outOfBoundsParameters._floorGridMainLineThickness = value;
        }

        /// <summary>
        ///     Gets or sets the number of secondary grid cells between main grid lines.
        /// </summary>
        public int FloorGridMainLineInterval
        {
            get => _outOfBoundsParameters._floorGridMainLineInterval;
            set => _outOfBoundsParameters._floorGridMainLineInterval = value;
        }

        /// <summary>
        ///     Gets or sets the color used for the secondary floor and ceiling grid lines.
        /// </summary>
        public Color FloorGridSecondaryColor
        {
            get => _outOfBoundsParameters._floorGridSecondaryColor;
            set => _outOfBoundsParameters._floorGridSecondaryColor = value;
        }

        /// <summary>
        ///     Gets or sets the world-space thickness of the secondary floor and ceiling grid lines.
        /// </summary>
        public float FloorGridSecondaryLineThickness
        {
            get => _outOfBoundsParameters._floorGridSecondaryLineThickness;
            set => _outOfBoundsParameters._floorGridSecondaryLineThickness = value;
        }

        /// <summary>
        ///     Gets or sets the distance where floor and ceiling grid lines start fading out.
        /// </summary>
        public float FloorGridFadeStartRadius
        {
            get => _outOfBoundsParameters._floorGridFadeStartRadius;
            set => _outOfBoundsParameters._floorGridFadeStartRadius = value;
        }

        /// <summary>
        ///     Gets or sets the distance where floor and ceiling grid lines become fully hidden.
        /// </summary>
        public float FloorGridFadeEndRadius
        {
            get => _outOfBoundsParameters._floorGridFadeEndRadius;
            set => _outOfBoundsParameters._floorGridFadeEndRadius = value;
        }

        /// <summary>
        ///     Gets or sets the height of the ceiling grid relative to the floor Y position.
        /// </summary>
        public float CeilingHeight
        {
            get => _outOfBoundsParameters._ceilingHeight;
            set => _outOfBoundsParameters._ceilingHeight = value;
        }

        /// <summary>
        ///     Gets or sets the color used in the distance where the floor, ceiling, and grid fade into the horizon.
        /// </summary>
        public Color HorizonColor
        {
            get => _outOfBoundsParameters._horizonColor;
            set => _outOfBoundsParameters._horizonColor = value;
        }

        /// <summary>
        ///     Gets or sets the distance where the floor, ceiling, and grid start fading to the horizon color.
        /// </summary>
        public float HorizonFadeStartDistance
        {
            get => _outOfBoundsParameters._horizonFadeStartDistance;
            set => _outOfBoundsParameters._horizonFadeStartDistance = value;
        }

        /// <summary>
        ///     Gets or sets the distance where the floor, ceiling, and grid have fully faded to the horizon color.
        /// </summary>
        public float HorizonFadeEndDistance
        {
            get => _outOfBoundsParameters._horizonFadeEndDistance;
            set => _outOfBoundsParameters._horizonFadeEndDistance = value;
        }

        /// <summary>
        ///     Gets whether the camera is currently inside geometry. To check whether the camera is within the fade zone too, use
        ///     <see cref="IsInsideFade" />.
        /// </summary>
        public bool IsInsideGeometry { get; private set; }

        /// <summary>
        ///     Gets the world-space position where the camera last transitioned from a valid location into a clipping state,
        ///     used as the reference point to compute exit direction and portal placement.
        /// </summary>
        public Vector3 EntryPos { get; private set; }

        /// <summary>
        ///     Gets the outward-facing normal of the surface used as a reference to determine the direction
        ///     the user should move to exit the clipping state.
        /// </summary>
        public Vector3 ExitNormal { get; private set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Checks whether an avatar is currently clipping through geometry or near enough to trigger the fade.
        ///     The camera object requires having an <see cref="UxrCameraFade" /> in order to work.
        /// </summary>
        /// <param name="avatar">The avatar to check.</param>
        /// <returns>
        ///     Whether the avatar has an <see cref="UxrCameraFade" /> component and it currently detects the avatar is
        ///     clipping through geometry.
        /// </returns>
        /// <remarks>
        ///     This method will also return true right before/after entering geometry, where the fade is performed
        ///     using a margin defined by fadeNearDistance and fadeFarDistance.
        ///     For a "true" inside wall value, use <see cref="IsInsideGeometry" /> instead.
        /// </remarks>
        public static bool IsAvatarInsideFade(UxrAvatar avatar)
        {
            if (avatar == null)
            {
                return false;
            }

            UxrCameraWallFade wallFade = avatar.CameraComponent != null ? avatar.CameraComponent.GetComponent<UxrCameraWallFade>() : null;
            return wallFade && wallFade.enabled && wallFade._quadObject != null && wallFade._quadObject.activeSelf;
        }

        /// <summary>
        ///     Determines whether the specified avatar is currently inside geometry.
        /// </summary>
        /// <param name="avatar">The avatar to check for geometry intersection.</param>
        /// <returns>
        ///     True if the avatar's associated <see cref="UxrCameraWallFade" /> component
        ///     indicates it is inside geometry and the component is active; otherwise, false.
        /// </returns>
        public static bool IsAvatarInsideGeometry(UxrAvatar avatar)
        {
            if (avatar == null)
            {
                return false;
            }

            UxrCameraWallFade wallFade = avatar.CameraComponent != null ? avatar.CameraComponent.GetComponent<UxrCameraWallFade>() : null;
            return wallFade && wallFade.enabled && wallFade.IsInsideGeometry;
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the component.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            CreateCameraQuad();
        }

        /// <summary>
        ///     Eliminates the resources.
        /// </summary>
        protected override void OnDestroy()
        {
            if (_fadeMaterial != null)
            {
                Destroy(_fadeMaterial);
            }
        }

        /// <summary>
        ///     Subscribes to events. It also initializes the component so that whenever it is enabled, it is considered as being
        ///     "outside".
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            ResetState();

            UxrAvatar.GlobalAvatarMoved += UxrAvatar_GlobalAvatarMoved;
            UxrManager.AvatarsUpdated   += UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrAvatar.GlobalAvatarMoved -= UxrAvatar_GlobalAvatarMoved;
            UxrManager.AvatarsUpdated   -= UxrManager_AvatarsUpdated;

            if (_quadObject != null)
            {
                _quadObject.SetActive(false);
            }
        }

        /// <summary>
        ///     Validates serialized parameter groups.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            _generalParameters     ??= new GeneralParameters();
            _portalParameters      ??= new PortalParameters();
            _outOfBoundsParameters ??= new OutOfBoundsSpaceParameters();
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called whenever an avatar moved. The state is reset so that it is considered "outside".
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event parameters.</param>
        private void UxrAvatar_GlobalAvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            if (ReferenceEquals(e.Avatar, Avatar))
            {
                ResetState();
            }
        }

        /// <summary>
        ///     Called after all avatars have been updated. This is where the component is updated.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            if (Avatar.AvatarMode != UxrAvatarMode.Local)
            {
                if (_quadObject)
                {
                    _quadObject.SetActive(false);
                }

                return;
            }

            UpdateFade();
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Enters the geometry clipping state.
        /// </summary>
        private void OnEntered()
        {
            IsInsideGeometry = true;
            Entered?.Invoke(this);
        }

        /// <summary>
        ///     Enters the fade state.
        /// </summary>
        /// <param name="currentPos">Current camera position.</param>
        /// <param name="cameraDeltaPos">Camera movement from the last valid position to the current position.</param>
        /// <param name="movementHit">Optional sweep hit that detected the entry.</param>
        private void OnFadeEntered(Vector3 currentPos, Vector3 cameraDeltaPos, RaycastHit movementHit)
        {
            EntryPos = _lastValidPos;

            Vector3 exitDirection = _lastValidPos - currentPos;

            if (exitDirection.sqrMagnitude <= MinDirectionSqrMagnitude)
            {
                exitDirection = -cameraDeltaPos;
            }

            if (exitDirection.sqrMagnitude <= MinDirectionSqrMagnitude && movementHit.collider != null)
            {
                exitDirection = movementHit.normal;
            }

            if (exitDirection.sqrMagnitude <= MinDirectionSqrMagnitude)
            {
                exitDirection = -transform.forward;
            }

            ExitNormal = exitDirection.normalized;
            FadeEntered?.Invoke(this);
        }

        /// <summary>
        ///     Exits the geometry clipping state.
        /// </summary>
        private void OnExited()
        {
            IsInsideGeometry = false;
            Exited?.Invoke(this);
        }

        /// <summary>
        ///     Exits the fade state.
        /// </summary>
        private void OnFadeExited()
        {
            FadeExited?.Invoke(this);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Resets the wall fade state.
        /// </summary>
        private void ResetState()
        {
            _lastValidPosInitialized = false;
            _fadeAlpha               = 0.0f;
            IsInsideGeometry         = false;

            if (_quadObject != null)
            {
                _quadObject.SetActive(false);
            }
        }

        /// <summary>
        ///     Updates the component.
        /// </summary>
        private void UpdateFade()
        {
            Vector3 currentPos = transform.position;

            if (!_lastValidPosInitialized && Avatar != null && Avatar.transform.InverseTransformPoint(currentPos).y > CameraInitializationMinY)
            {
                _lastValidPosInitialized = true;
                _lastValidPos            = currentPos;
                _fadeAlpha               = 0.0f;
                IsInsideGeometry         = false;
            }

            if (!_lastValidPosInitialized)
            {
                SetFade(0.0f);
                return;
            }

            Vector3 cameraDeltaPos = currentPos - _lastValidPos;

            bool isOverlappingHead = IsOverlappingBlockingGeometry(currentPos, HeadRadius);
            bool hasMovementHit    = TryGetClosestSweepHit(_lastValidPos, currentPos, HeadRadius, out RaycastHit movementHit);

            if (!IsInsideGeometry)
            {
                if (isOverlappingHead || hasMovementHit)
                {
                    OnEntered();
                }
            }
            else
            {
                if (CanExit(currentPos))
                {
                    OnExited();
                }
            }

            float lastAlpha = _fadeAlpha;
            _fadeAlpha = ComputeFadeAlpha(currentPos);

            if (lastAlpha == 0.0f && _fadeAlpha > 0.0f)
            {
                OnFadeEntered(currentPos, cameraDeltaPos, hasMovementHit ? movementHit : default);
            }
            else if (lastAlpha > 0.0f && _fadeAlpha == 0.0f)
            {
                OnFadeExited();
            }

            if (_fadeAlpha == 0.0f && !IsOverlappingBlockingGeometry(currentPos, HeadRadius))
            {
                _lastValidPos = currentPos;
            }

            UpdateOverlay(currentPos, cameraDeltaPos);
        }

        /// <summary>
        ///     Checks whether the camera can exit the clipping state.
        /// </summary>
        /// <param name="currentPos">Current camera position.</param>
        /// <returns>Whether the camera can exit the clipping state.</returns>
        private bool CanExit(Vector3 currentPos)
        {
            if (IsOverlappingBlockingGeometry(currentPos, HeadRadius))
            {
                return false;
            }

            Vector3 start = EntryPos + ExitNormal * StrictPathSkin;
            return IsSpherePathClear(start, currentPos, HeadRadius);
        }

        /// <summary>
        ///     Computes the current fade alpha.
        /// </summary>
        /// <param name="currentPos">Current camera position.</param>
        /// <returns>Fade alpha.</returns>
        private float ComputeFadeAlpha(Vector3 currentPos)
        {
            if (IsInsideGeometry)
            {
                return 1.0f;
            }

            float closestDistance = GetClosestBlockingColliderDistance(currentPos, FadeFarDistance);

            if (closestDistance < 0.0f)
            {
                return 0.0f;
            }

            if (closestDistance <= FadeNearDistance)
            {
                return 1.0f;
            }

            float interval = FadeFarDistance - FadeNearDistance;

            if (interval <= 0.0f)
            {
                return 1.0f;
            }

            return Mathf.Clamp01(1.0f - (closestDistance - FadeNearDistance) / interval);
        }

        /// <summary>
        ///     Updates the overlay quad and material parameters.
        /// </summary>
        /// <param name="currentPos">Current camera position.</param>
        /// <param name="cameraDeltaPos">Camera movement from the last valid position to the current position.</param>
        private void UpdateOverlay(Vector3 currentPos, Vector3 cameraDeltaPos)
        {
            if (_quadObject == null || _fadeMaterial == null)
            {
                return;
            }

            bool shouldRender = _fadeAlpha > 0.0f;

            _quadObject.SetActive(shouldRender);
            _fadeMaterial.color = HorizonColor.WithAlpha(_fadeAlpha);

            if (!shouldRender)
            {
                _cameraComponent.nearClipPlane = DefaultCameraNear;
                return;
            }

            if (_cameraComponent != null)
            {
                // Near is the closest when looking "inside". If we look towards the exit, we need to increase the near plane to avoid clipping with whatever is behind the wall.
                _cameraComponent.nearClipPlane = Vector3.Dot(_cameraComponent.transform.forward, ExitNormal) < 0.0f ? DefaultCameraNear : cameraDeltaPos.magnitude;

                float quadDistance = _cameraComponent.nearClipPlane + QuadDistanceToNear;
                _quadObject.transform.localPosition = Vector3.forward * quadDistance;

                float fovRad = _cameraComponent.fieldOfView * Mathf.Deg2Rad;
                float height = 2.0f                         * quadDistance * Mathf.Tan(fovRad * 0.5f);
                float width  = height                       * _cameraComponent.aspect;

                float invQuadSize = 1.0f / QuadSize;

                _quadObject.transform.localScale = new Vector3(Mathf.Max(QuadSize, width * invQuadSize), Mathf.Max(QuadSize, height * invQuadSize), 1.0f);
            }

            Vector3 planePos     = EntryPos;
            Vector3 planeNormal  = ExitNormal;
            Vector3 sphereCenter = GetPortalSphereCenter(currentPos, PortalHoleRadiusMin);

            _fadeMaterial.SetVector(s_portalPlanePos,     new Vector4(planePos.x,     planePos.y,     planePos.z,     1.0f));
            _fadeMaterial.SetVector(s_portalPlaneNormal,  new Vector4(planeNormal.x,  planeNormal.y,  planeNormal.z,  0.0f));
            _fadeMaterial.SetVector(s_portalSphereCenter, new Vector4(sphereCenter.x, sphereCenter.y, sphereCenter.z, 1.0f));
            _fadeMaterial.SetFloat(s_portalSphereRadius, PortalHoleRadius);
            _fadeMaterial.SetFloat(s_portalEdgeSoftness, PortalHoleEdgeSoftness);

            UpdateFloorGridMaterialProperties();

            UxrWallFadeEventArgs args = UxrWallFadeEventArgs.GetFromPool();
            args.Avatar                      = Avatar;
            args.FadeAlpha                   = _fadeAlpha;
            args.LastValidCameraPos          = _lastValidPos;
            args.LastValidToCurrentCameraPos = cameraDeltaPos;
            args.ExitNormal                  = ExitNormal;
            args.PortalSphereCenter          = sphereCenter;

            FadeUpdated?.Invoke(this, args);
        }

        /// <summary>
        ///     Updates the floor grid parameters used by the fade material.
        /// </summary>
        /// <summary>
        ///     Updates the floor and ceiling grid parameters used by the fade material.
        /// </summary>
        private void UpdateFloorGridMaterialProperties()
        {
            _fadeMaterial.SetFloat(s_floorPosY,     Avatar.transform.position.y);
            _fadeMaterial.SetFloat(s_ceilingHeight, Mathf.Max(CeilingHeight, 0.0f));

            _fadeMaterial.SetColor(s_floorNearColor, FloorNearColor);
            _fadeMaterial.SetColor(s_floorFarColor,  FloorFarColor);

            _fadeMaterial.SetFloat(s_floorFarStartDistance, Mathf.Max(FloorFarStartDistance, 0.0f));
            _fadeMaterial.SetFloat(s_floorFarEndDistance,   Mathf.Max(FloorFarEndDistance,   FloorFarStartDistance + 0.0001f));

            _fadeMaterial.SetFloat(s_horizonFadeStartDistance, Mathf.Max(HorizonFadeStartDistance, 0.0f));
            _fadeMaterial.SetFloat(s_horizonFadeEndDistance,   Mathf.Max(HorizonFadeEndDistance,   HorizonFadeStartDistance + 0.0001f));

            _fadeMaterial.SetFloat(s_floorGridTileSize,     Mathf.Max(FloorGridTileSize, 0.0001f));
            _fadeMaterial.SetFloat(s_floorGridAntiAliasing, Mathf.Clamp01(FloorGridAntialiasing));

            _fadeMaterial.SetColor(s_floorGridMainColor, FloorGridMainColor);
            _fadeMaterial.SetFloat(s_floorGridMainLineThickness, Mathf.Max(FloorGridMainLineThickness, 0.0001f));
            _fadeMaterial.SetFloat(s_floorGridMainLineInterval,  Mathf.Max(FloorGridMainLineInterval,  1));

            _fadeMaterial.SetColor(s_floorGridSecondaryColor, FloorGridSecondaryColor);
            _fadeMaterial.SetFloat(s_floorGridSecondaryLineThickness, Mathf.Max(FloorGridSecondaryLineThickness, 0.0001f));

            _fadeMaterial.SetFloat(s_floorGridFadeStartRadius, Mathf.Max(FloorGridFadeStartRadius, 0.0f));
            _fadeMaterial.SetFloat(s_floorGridFadeEndRadius,   Mathf.Max(FloorGridFadeEndRadius,   FloorGridFadeStartRadius + 0.0001f));
        }

        /// <summary>
        ///     Sets the current fade state directly.
        /// </summary>
        /// <param name="alpha">Fade alpha.</param>
        private void SetFade(float alpha)
        {
            _fadeAlpha = alpha;

            if (_quadObject != null)
            {
                _quadObject.SetActive(alpha > 0.0f);

                if (alpha <= 0.0f)
                {
                    _cameraComponent.nearClipPlane = DefaultCameraNear;
                }
            }
        }

        /// <summary>
        ///     Computes the world-space center of the portal sphere so that the portal remains anchored
        ///     to the original entry position while only varying with the depth of penetration.
        ///     The depth is clamped so that the resulting portal radius on the entry plane never goes
        ///     below a specified minimum.
        /// </summary>
        /// <param name="currentPos">
        ///     Current camera/world position used to determine how far the user has moved into the geometry.
        /// </param>
        /// <param name="minPortalRadius">
        ///     Minimum allowed radius of the portal on the entry plane. The sphere center will stop moving
        ///     deeper once this radius is reached.
        /// </param>
        /// <returns>
        ///     The computed sphere center projected along the entry/exit direction with depth clamped to
        ///     preserve the minimum portal radius.
        /// </returns>
        private Vector3 GetPortalSphereCenter(Vector3 currentPos, float minPortalRadius)
        {
            Vector3 entryToCurrent = currentPos - EntryPos;

            // Compute penetration depth along the exit direction.
            float depth = Vector3.Dot(entryToCurrent, -ExitNormal);
            depth = Mathf.Max(0.0f, depth);

            // Clamp depth so that the portal radius never goes below minPortalRadius.
            float sphereRadius = PortalHoleRadius;
            float minRadius    = Mathf.Clamp(minPortalRadius, 0.0f, sphereRadius);

            float maxDepth = Mathf.Sqrt(Mathf.Max(0.0f, sphereRadius * sphereRadius - minRadius * minRadius));

            depth = Mathf.Min(depth, maxDepth);

            return EntryPos - ExitNormal * depth;
        }

        /// <summary>
        ///     Creates the quad used to render the fullscreen fade.
        /// </summary>
        private void CreateCameraQuad()
        {
            _cameraComponent = GetComponent<Camera>();

            _quadObject = new GameObject("WallFade");
            _quadObject.transform.SetParent(transform);
            _quadObject.transform.localPosition    = Vector3.forward * (_cameraComponent.nearClipPlane + QuadDistanceToNear);
            _quadObject.transform.localEulerAngles = new Vector3(0.0f, 180.0f, 0.0f);

            Mesh mesh = MeshExt.CreateQuad(QuadSize);

            MeshFilter   meshFilter   = _quadObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _quadObject.AddComponent<MeshRenderer>();

            meshFilter.mesh = mesh;
            _fadeMaterial   = new Material(ShaderExt.OverlayFadeWallClipPortal);

            meshRenderer.sharedMaterial = _fadeMaterial;
            _quadObject.SetActive(false);
        }

        /// <summary>
        ///     Checks whether the given collider is valid or should be ignored.
        /// </summary>
        /// <param name="colliderHit">Collider that was hit.</param>
        /// <returns>Whether the given collider is valid.</returns>
        private bool IsValidCollider(Collider colliderHit)
        {
            if (colliderHit == null)
            {
                return false;
            }

            if (IgnoreDynamicObjects && colliderHit.gameObject.IsDynamic())
            {
                return false;
            }

            if (IgnoreGrabbedObjects)
            {
                UxrGrabbableObject grabbableObject = colliderHit.GetComponentInParent<UxrGrabbableObject>();

                if (grabbableObject && grabbableObject.IsBeingGrabbed)
                {
                    return false;
                }
            }

            return !colliderHit.gameObject.GetComponentInParent<UxrIgnoreWallFade>() &&
                   !colliderHit.gameObject.GetComponentInParent<UxrAvatar>();
        }

        /// <summary>
        ///     Checks whether the current head sphere overlaps blocking geometry.
        /// </summary>
        /// <param name="position">Sphere center.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <returns>Whether the sphere overlaps valid blocking geometry.</returns>
        private bool IsOverlappingBlockingGeometry(Vector3 position, float radius)
        {
            int count = Physics.OverlapSphereNonAlloc(position, radius, s_overlapBuffer, CollisionLayers, GetTriggerInteraction());

            for (int i = 0; i < count; ++i)
            {
                if (IsValidCollider(s_overlapBuffer[i]))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Gets the closest blocking collider distance around a position.
        /// </summary>
        /// <param name="position">Center position.</param>
        /// <param name="radius">Search radius.</param>
        /// <returns>Closest distance, or a negative value if no collider was found.</returns>
        private float GetClosestBlockingColliderDistance(Vector3 position, float radius)
        {
            float closestDistance = float.MaxValue;
            bool  hasHit          = false;

            foreach (Vector3 dir in s_probeDirections)
            {
                int count = Physics.SphereCastNonAlloc(position, HeadRadius, dir, s_hitBuffer, radius, CollisionLayers, GetTriggerInteraction());

                for (int hitIndex = 0; hitIndex < count; ++hitIndex)
                {
                    RaycastHit hit = s_hitBuffer[hitIndex];

                    if (!IsValidCollider(hit.collider))
                    {
                        continue;
                    }

                    if (hit.distance < closestDistance)
                    {
                        closestDistance = hit.distance;
                        hasHit          = true;
                    }
                }
            }

            return hasHit ? closestDistance : -1.0f;
        }

        /// <summary>
        ///     Tries to get the closest valid sphere cast hit between two positions.
        /// </summary>
        /// <param name="from">Start position.</param>
        /// <param name="to">End position.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <param name="closestHit">Closest valid hit.</param>
        /// <returns>Whether a valid hit was found.</returns>
        private bool TryGetClosestSweepHit(Vector3 from, Vector3 to, float radius, out RaycastHit closestHit)
        {
            Vector3 delta    = to - from;
            float   distance = delta.magnitude;

            if (distance <= MinSweepDistance)
            {
                closestHit = default;
                return false;
            }

            Vector3 direction = delta / distance;
            int     count     = Physics.SphereCastNonAlloc(from, radius, direction, s_hitBuffer, distance, CollisionLayers, GetTriggerInteraction());

            return TryGetClosestValidHit(count, out closestHit);
        }

        /// <summary>
        ///     Checks whether a sphere path is clear between two positions.
        /// </summary>
        /// <param name="from">Start position.</param>
        /// <param name="to">End position.</param>
        /// <param name="radius">Sphere radius.</param>
        /// <returns>Whether the sphere path is clear.</returns>
        private bool IsSpherePathClear(Vector3 from, Vector3 to, float radius)
        {
            return !TryGetClosestSweepHit(from, to, radius, out _);
        }

        /// <summary>
        ///     Gets the closest valid hit from the shared hit buffer.
        /// </summary>
        /// <param name="count">Number of valid entries in the shared hit buffer.</param>
        /// <param name="closestHit">Closest valid hit.</param>
        /// <returns>Whether a valid hit was found.</returns>
        private bool TryGetClosestValidHit(int count, out RaycastHit closestHit)
        {
            int   closestIndex    = -1;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < count; ++i)
            {
                RaycastHit hit = s_hitBuffer[i];

                if (!IsValidCollider(hit.collider))
                {
                    continue;
                }

                if (hit.distance < closestDistance)
                {
                    closestIndex    = i;
                    closestDistance = hit.distance;
                }
            }

            if (closestIndex >= 0)
            {
                closestHit = s_hitBuffer[closestIndex];
                return true;
            }

            closestHit = default;
            return false;
        }

        /// <summary>
        ///     Gets the trigger interaction mode to use in physics queries.
        /// </summary>
        /// <returns>The trigger interaction mode.</returns>
        private QueryTriggerInteraction GetTriggerInteraction()
        {
            return IgnoreTriggerColliders ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     General wall fade parameters.
        /// </summary>
        [Serializable]
        private class GeneralParameters
        {
            #region Inspector Properties/Serialized Fields

            [SerializeField] [Tooltip(TooltipCollisionLayers)]        public LayerMask _collisionLayers        = 1; // Default
            [SerializeField] [Tooltip(TooltipFadeFarDistance)]        public float     _fadeFarDistance        = 0.15f;
            [SerializeField] [Tooltip(TooltipFadeNearDistance)]       public float     _fadeNearDistance       = 0.03f;
            [SerializeField] [Tooltip(TooltipHeadRadius)]             public float     _headRadius             = 0.08f;
            [SerializeField] [Tooltip(TooltipIgnoreTriggerColliders)] public bool      _ignoreTriggerColliders = true;
            [SerializeField] [Tooltip(TooltipIgnoreDynamicObjects)]   public bool      _ignoreDynamicObjects   = true;
            [SerializeField] [Tooltip(TooltipIgnoreGrabbedObjects)]   public bool      _ignoreGrabbedObjects   = true;

            #endregion

            #region Private Types & Data

            private const string TooltipCollisionLayers        = "Layers considered blocking geometry for the wall fade checks.";
            private const string TooltipFadeFarDistance        = "Distance from geometry where the fade starts increasing.";
            private const string TooltipFadeNearDistance       = "Distance from geometry where the fade reaches full intensity.";
            private const string TooltipHeadRadius             = "Radius used to approximate the user's head when checking proximity to or intersection with geometry.";
            private const string TooltipIgnoreTriggerColliders = "Whether trigger colliders should be ignored by wall fade checks.";
            private const string TooltipIgnoreDynamicObjects   = "Whether dynamic objects should be ignored by wall fade checks.";
            private const string TooltipIgnoreGrabbedObjects   = "Whether grabbed objects should be ignored by wall fade checks.";

            #endregion
        }

        /// <summary>
        ///     Portal rendering parameters.
        /// </summary>
        [Serializable]
        private class PortalParameters
        {
            #region Inspector Properties/Serialized Fields

            [SerializeField] [Tooltip(TooltipPortalHoleRadius)]       public float _portalHoleRadius       = 0.4f;
            [SerializeField] [Tooltip(TooltipPortalHoleRadiusMin)]    public float _portalHoleRadiusMin    = 0.2f;
            [SerializeField] [Tooltip(TooltipPortalHoleEdgeSoftness)] public float _portalHoleEdgeSoftness = 0.05f;

            #endregion

            #region Private Types & Data

            private const string TooltipPortalHoleRadius       = "Radius of the sphere used to compute the portal opening back to the safe space.";
            private const string TooltipPortalHoleRadiusMin    = "Minimum radius allowed for the portal opening when the user moves deeper into clipped space.";
            private const string TooltipPortalHoleEdgeSoftness = "Softness of the portal edge transition.";

            #endregion
        }

        /// <summary>
        ///     Out-of-bounds floor, ceiling, grid, and horizon parameters.
        /// </summary>
        [Serializable]
        private class OutOfBoundsSpaceParameters
        {
            #region Inspector Properties/Serialized Fields

            [SerializeField] [Tooltip(TooltipFloorGridTileSize)]                     public float _floorGridTileSize               = 0.4f;
            [SerializeField] [Tooltip(TooltipFloorGridAntialiasing)] [Range(0f, 1f)] public float _floorGridAntialiasing           = 0.5f;
            [SerializeField] [Tooltip(TooltipFloorNearColor)]                        public Color _floorNearColor                  = new(0.784f, 0.784f, 0.784f, 1.0f);
            [SerializeField] [Tooltip(TooltipFloorFarColor)]                         public Color _floorFarColor                   = new(0.706f, 0.706f, 0.706f, 1.0f);
            [SerializeField] [Tooltip(TooltipFloorFarStartDistance)]                 public float _floorFarStartDistance           = 5.0f;
            [SerializeField] [Tooltip(TooltipFloorFarEndDistance)]                   public float _floorFarEndDistance             = 40.0f;
            [SerializeField] [Tooltip(TooltipFloorGridMainColor)]                    public Color _floorGridMainColor              = new(0f, 0f, 0f, 1.0f);
            [SerializeField] [Tooltip(TooltipFloorGridMainLineThickness)]            public float _floorGridMainLineThickness      = 0.003f;
            [SerializeField] [Tooltip(TooltipFloorGridMainLineInterval)]             public int   _floorGridMainLineInterval       = 4;
            [SerializeField] [Tooltip(TooltipFloorGridSecondaryColor)]               public Color _floorGridSecondaryColor         = new(0f, 0f, 0f, 0.35f);
            [SerializeField] [Tooltip(TooltipFloorGridSecondaryLineThickness)]       public float _floorGridSecondaryLineThickness = 0.001f;
            [SerializeField] [Tooltip(TooltipFloorGridFadeStartRadius)]              public float _floorGridFadeStartRadius        = 5.0f;
            [SerializeField] [Tooltip(TooltipFloorGridFadeEndRadius)]                public float _floorGridFadeEndRadius          = 25.0f;
            [SerializeField] [Tooltip(TooltipCeilingHeight)]                         public float _ceilingHeight                   = 4.0f;
            [SerializeField] [Tooltip(TooltipHorizonColor)]                          public Color _horizonColor                    = Color.white;
            [SerializeField] [Tooltip(TooltipHorizonFadeStartDistance)]              public float _horizonFadeStartDistance        = 60.0f;
            [SerializeField] [Tooltip(TooltipHorizonFadeEndDistance)]                public float _horizonFadeEndDistance          = 180.0f;

            #endregion

            #region Private Types & Data

            private const string TooltipFloorGridTileSize               = "World-space size of each secondary floor and ceiling grid cell.";
            private const string TooltipFloorGridAntialiasing           = "Controls derivative-based anti-aliasing for the grid lines. Higher values reduce moiré at distance.";
            private const string TooltipFloorNearColor                  = "Color used for the floor and ceiling near the camera.";
            private const string TooltipFloorFarColor                   = "Color used for the floor and ceiling at far distances before fading to the horizon color.";
            private const string TooltipFloorFarStartDistance           = "Distance where the floor and ceiling start fading from the near color to the far color.";
            private const string TooltipFloorFarEndDistance             = "Distance where the floor and ceiling finish fading from the near color to the far color.";
            private const string TooltipFloorGridMainColor              = "Color used for the main floor and ceiling grid lines.";
            private const string TooltipFloorGridMainLineThickness      = "World-space thickness of the main floor and ceiling grid lines.";
            private const string TooltipFloorGridMainLineInterval       = "Number of secondary grid cells between main grid lines.";
            private const string TooltipFloorGridSecondaryColor         = "Color used for the secondary floor and ceiling grid lines.";
            private const string TooltipFloorGridSecondaryLineThickness = "World-space thickness of the secondary floor and ceiling grid lines.";
            private const string TooltipFloorGridFadeStartRadius        = "Distance where floor and ceiling grid lines start fading out.";
            private const string TooltipFloorGridFadeEndRadius          = "Distance where floor and ceiling grid lines become fully hidden.";
            private const string TooltipCeilingHeight                   = "Height of the ceiling grid relative to the floor Y position.";
            private const string TooltipHorizonColor                    = "Color used in the distance where the floor, ceiling, and grid fade into the horizon.";
            private const string TooltipHorizonFadeStartDistance        = "Distance where the floor, ceiling, and grid start fading to the horizon color.";
            private const string TooltipHorizonFadeEndDistance          = "Distance where the floor, ceiling, and grid have fully faded to the horizon color.";

            #endregion
        }

        private const float DefaultCameraNear  = 0.01f;
        private const float QuadSize           = 2.0f;
        private const float QuadDistanceToNear = 0.015f;

        /// <summary>
        ///     Used to avoid initialization being done before the user has the headset in the correct position.
        /// </summary>
        private const float CameraInitializationMinY = 0.2f;

        private const float MinDirectionSqrMagnitude = 0.000001f;
        private const float MinSweepDistance         = 0.0001f;
        private const float StrictPathSkin           = 0.01f;

        private const int MaxHits             = 32;
        private const int MaxOverlapColliders = 32;

        private static readonly RaycastHit[] s_hitBuffer     = new RaycastHit[MaxHits];
        private static readonly Collider[]   s_overlapBuffer = new Collider[MaxOverlapColliders];

        private static readonly int s_portalPlanePos     = Shader.PropertyToID("_PortalPlanePos");
        private static readonly int s_portalSphereCenter = Shader.PropertyToID("_PortalSphereCenter");
        private static readonly int s_portalPlaneNormal  = Shader.PropertyToID("_PortalPlaneNormal");
        private static readonly int s_portalSphereRadius = Shader.PropertyToID("_PortalSphereRadius");
        private static readonly int s_portalEdgeSoftness = Shader.PropertyToID("_PortalEdgeSoftness");

        private static readonly int s_floorPosY                       = Shader.PropertyToID("_FloorPosY");
        private static readonly int s_floorGridTileSize               = Shader.PropertyToID("_FloorGridTileSize");
        private static readonly int s_floorGridAntiAliasing           = Shader.PropertyToID("_FloorGridAntiAliasing");
        private static readonly int s_floorNearColor                  = Shader.PropertyToID("_FloorNearColor");
        private static readonly int s_floorFarColor                   = Shader.PropertyToID("_FloorFarColor");
        private static readonly int s_floorFarStartDistance           = Shader.PropertyToID("_FloorFarStartDistance");
        private static readonly int s_floorFarEndDistance             = Shader.PropertyToID("_FloorFarEndDistance");
        private static readonly int s_floorGridMainColor              = Shader.PropertyToID("_FloorGridMainColor");
        private static readonly int s_floorGridMainLineThickness      = Shader.PropertyToID("_FloorGridMainLineThickness");
        private static readonly int s_floorGridMainLineInterval       = Shader.PropertyToID("_FloorGridMainLineInterval");
        private static readonly int s_floorGridSecondaryColor         = Shader.PropertyToID("_FloorGridSecondaryColor");
        private static readonly int s_floorGridSecondaryLineThickness = Shader.PropertyToID("_FloorGridSecondaryLineThickness");
        private static readonly int s_floorGridFadeStartRadius        = Shader.PropertyToID("_FloorGridFadeStartRadius");
        private static readonly int s_floorGridFadeEndRadius          = Shader.PropertyToID("_FloorGridFadeEndRadius");
        private static readonly int s_ceilingHeight                   = Shader.PropertyToID("_CeilingHeight");
        private static readonly int s_horizonFadeStartDistance        = Shader.PropertyToID("_HorizonFadeStartDistance");
        private static readonly int s_horizonFadeEndDistance          = Shader.PropertyToID("_HorizonFadeEndDistance");

        private static readonly Vector3[] s_probeDirections =
        {
            Vector3.forward,
            Vector3.back,
            Vector3.left,
            Vector3.right,
            Vector3.up,
            Vector3.down,

            new Vector3(1,  0, 1).normalized,
            new Vector3(-1, 0, 1).normalized,
            new Vector3(1,  0, -1).normalized,
            new Vector3(-1, 0, -1).normalized,

            new Vector3(1,  1,  0).normalized,
            new Vector3(-1, 1,  0).normalized,
            new Vector3(1,  -1, 0).normalized,
            new Vector3(-1, -1, 0).normalized,
        };

        private Camera     _cameraComponent;
        private Vector3    _lastValidPos;
        private bool       _lastValidPosInitialized;
        private GameObject _quadObject;
        private Material   _fadeMaterial;
        private float      _fadeAlpha;

        #endregion
    }
}