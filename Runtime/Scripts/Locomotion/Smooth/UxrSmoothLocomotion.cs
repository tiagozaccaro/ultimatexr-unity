// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothLocomotion.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using UltimateXR.Animation.Interpolation;
using UltimateXR.Attributes;
using UltimateXR.Avatar;
using UltimateXR.Avatar.Controllers;
using UltimateXR.CameraUtils;
using UltimateXR.Core;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Math;
using UnityEditor;
using UnityEngine;
using Time = UnityEngine.Time;

namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Base class for smooth locomotion using a <see cref="CharacterController" />. Handles common behavior like movement,
    ///     rotation, gravity, and input updates, and is intended to allow both VR and non-VR implementations.
    ///     Derived classes can adapt it for different control styles or platforms.
    /// </summary>
    public abstract class UxrSmoothLocomotion : UxrLocomotion
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [StylishFoldout("General")]  private GeneralParameters  _generalParameters;
        [SerializeField] [StylishFoldout("Movement")] private MovementParameters _movementParameters;
        [SerializeField] [StylishFoldout("Rotation")] private RotationParameters _rotationParameters;
        [SerializeField] [StylishFoldout("Gravity")]  private GravityParameters  _gravityParameters;
        [SerializeField] [StylishFoldout("Collider")] private ColliderParameters _colliderParameters;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets the current speed the avatar is moving horizontally.
        /// </summary>
        public float CurrentHorizontalSpeed => _horizontalVelocity.magnitude;

        /// <summary>
        ///     The normalized current horizontal speed of the locomotion system, represented as a value between 0 and 1.
        ///     Calculated based on the ratio of the current horizontal speed to the maximum possible speed.
        /// </summary>
        public float CurrentNormalizedHorizontalSpeed => MaxSpeed > 0.0001f ? Mathf.Clamp01(CurrentHorizontalSpeed / MaxSpeed) : 0f;

        /// <summary>
        ///     The current list of components that implement the <see cref="IUxrSmoothLocomotionInput" /> interface, used
        ///     by the smooth locomotion system to process input for movement.
        /// </summary>
        public IReadOnlyList<IUxrSmoothLocomotionInput> LocomotionInputs => _locomotionInputs;

        /// <summary>
        ///     Gets or sets whether translation movement is allowed.
        /// </summary>
        public bool IsTranslationAllowed { get; set; } = true;

        /// <summary>
        ///     Gets or sets the <see cref="CharacterController" /> component that drives the locomotion system.
        /// </summary>
        public CharacterController CharacterController
        {
            get => _generalParameters._characterController;
            set => _generalParameters._characterController = value;
        }

        /// <summary>
        ///     Gets or sets the maximum horizontal movement speed.
        /// </summary>
        public float MaxSpeed
        {
            get => _movementParameters._maxSpeed;
            set => _movementParameters._maxSpeed = value;
        }

        /// <summary>
        ///     Gets or sets whether to use acceleration/deceleration for movement.
        /// </summary>
        public bool UseAcceleration
        {
            get => _movementParameters._useAcceleration;
            set => _movementParameters._useAcceleration = value;
        }

        /// <summary>
        ///     Gets or sets the acceleration used to reach the target movement speed.
        /// </summary>
        public float Acceleration
        {
            get => _movementParameters._acceleration;
            set => _movementParameters._acceleration = value;
        }

        /// <summary>
        ///     Gets or sets the deceleration used to reduce movement speed.
        /// </summary>
        public float Deceleration
        {
            get => _movementParameters._deceleration;
            set => _movementParameters._deceleration = value;
        }

        /// <summary>
        ///     Gets or sets the movement speed multiplier applied while sprinting.
        /// </summary>
        public float SprintModifier
        {
            get => _movementParameters._sprintModifier;
            set => _movementParameters._sprintModifier = value;
        }

        /// <summary>
        ///     Gets or sets the input dead-zone threshold.
        /// </summary>
        public float MovementDeadzone
        {
            get => _movementParameters._deadzone;
            set => _movementParameters._deadzone = value;
        }

        /// <summary>
        ///     Gets or sets the snap back distance threshold.
        /// </summary>
        public float SnapBackDistanceThreshold
        {
            get => _movementParameters._snapBackDistanceThreshold;
            set => _movementParameters._snapBackDistanceThreshold = value;
        }

        /// <summary>
        ///     Gets or sets the current turning mode.
        /// </summary>
        public UxrTurnType TurnType
        {
            get => _rotationParameters._turnType;
            set => _rotationParameters._turnType = value;
        }

        /// <summary>
        ///     Gets or sets the joystick dead-zone required to trigger a turn.
        /// </summary>
        public float TurnDeadzone
        {
            get => _rotationParameters._turnDeadzone;
            set => _rotationParameters._turnDeadzone = value;
        }

        /// <summary>
        ///     Gets or sets the seconds between turns if the turn input keeps being pressed.
        /// </summary>
        public float TurnCooldown
        {
            get => _rotationParameters._turnCooldown;
            set => _rotationParameters._turnCooldown = value;
        }

        /// <summary>
        ///     Gets or sets the turn step angle in degrees.
        /// </summary>
        public float TurnStepDegrees
        {
            get => _rotationParameters._turnStepDegrees;
            set => _rotationParameters._turnStepDegrees = value;
        }

        /// <summary>
        ///     Gets or sets the fade color used when the turn type is <see cref="UxrTurnType.Fade" />.
        /// </summary>
        public Color FadeTurnColor
        {
            get => _rotationParameters._fadeTurnColor;
            set => _rotationParameters._fadeTurnColor = value;
        }

        /// <summary>
        ///     Gets or sets the fade duration in seconds used when the turn type is <see cref="UxrTurnType.Fade" />.
        /// </summary>
        public float FadeTurnSeconds
        {
            get => _rotationParameters._fadeTurnSeconds;
            set => _rotationParameters._fadeTurnSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the interpolation duration in seconds used when the turn type is
        ///     <see cref="UxrTurnType.Interpolate" />.
        /// </summary>
        public float InterpolateTurnSeconds
        {
            get => _rotationParameters._interpolateTurnSeconds;
            set => _rotationParameters._interpolateTurnSeconds = value;
        }

        /// <summary>
        ///     Gets or sets the turn speed in degrees per second used when the turn type is <see cref="UxrTurnType.Smooth" />.
        /// </summary>
        public float SmoothTurnSpeedDeg
        {
            get => _rotationParameters._smoothTurnSpeedDeg;
            set => _rotationParameters._smoothTurnSpeedDeg = value;
        }

        /// <summary>
        ///     Gets or sets the smoothing factor used for rotation interpolation.
        /// </summary>
        public float SmoothRotation
        {
            get => _rotationParameters._smoothRotation;
            set => _rotationParameters._smoothRotation = value;
        }

        /// <summary>
        ///     Gravity when falling.
        /// </summary>
        public float Gravity
        {
            get => _gravityParameters._gravity;
            set => _gravityParameters._gravity = value;
        }

        /// <summary>
        ///     Gets or sets the downward force applied while grounded to keep the character attached to the ground.
        /// </summary>
        public float GroundedStickForce
        {
            get => _gravityParameters._groundedStickForce;
            set => _gravityParameters._groundedStickForce = value;
        }

        /// <summary>
        ///     Gets or sets the maximum downward falling speed.
        /// </summary>
        public float TerminalVelocity
        {
            get => _gravityParameters._terminalVelocity;
            set => _gravityParameters._terminalVelocity = value;
        }

        /// <summary>
        ///     Gets or sets the minimum allowed collider height.
        /// </summary>
        public float ColliderMinHeight
        {
            get => _colliderParameters._minHeight;
            set => _colliderParameters._minHeight = value;
        }

        /// <summary>
        ///     Gets or sets the maximum allowed collider height.
        /// </summary>
        public float ColliderMaxHeight
        {
            get => _colliderParameters._maxHeight;
            set => _colliderParameters._maxHeight = value;
        }

        /// <summary>
        ///     Gets or sets the radius of the avatar's capsule collider.
        /// </summary>
        public float ColliderRadius
        {
            get => _colliderParameters._radius;
            set => _colliderParameters._radius = value;
        }

        /// <summary>
        ///     Gets or sets the distance from the eyes to the top of the head.
        ///     This is used to adjust the top of the capsule collider.
        /// </summary>
        public float EyeToTopDistance
        {
            get => _colliderParameters._eyeToTopDistance;
            set => _colliderParameters._eyeToTopDistance = value;
        }

        /// <summary>
        ///     Gets or sets the avatar's capsule collider vertical offset with respect to the center.
        /// </summary>
        public float ColliderCenterYOffset
        {
            get => _colliderParameters._centerYOffset;
            set => _colliderParameters._centerYOffset = value;
        }

        #endregion

        #region Public Overrides UxrLocomotion

        /// <inheritdoc />
        public override IReadOnlyList<Collider> BodyColliders
        {
            get
            {
                _bodyColliders ??= new Collider[] { CharacterController };
                return _bodyColliders;
            }
        }

        /// <inheritdoc />
        public override bool IsSmoothLocomotion => true;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Registers a locomotion input instance for smooth locomotion. There is also an UnregisterInput method.
        ///     This allows you to enable/disable inputs at runtime.
        /// </summary>
        /// <param name="locomotionInput">
        ///     The locomotion input instance implementing the <see cref="IUxrSmoothLocomotionInput" /> interface to be registered.
        ///     If the input is already registered, the method will ignore it.
        /// </param>
        public void RegisterInput(IUxrSmoothLocomotionInput locomotionInput)
        {
            if (_locomotionInputs.Contains(locomotionInput))
            {
                return;
            }
            _locomotionInputs.Add(locomotionInput);
        }

        /// <summary>
        ///     Unregisters a locomotion input from the smooth locomotion system, removing it from the internal list of inputs.
        /// </summary>
        /// <param name="locomotionInput">
        ///     The locomotion input to be unregistered. If the input is part of the internal list,
        ///     it will be removed; otherwise, no action will be performed.
        /// </param>
        public void UnregisterInput(IUxrSmoothLocomotionInput locomotionInput)
        {
            if (_locomotionInputs.Contains(locomotionInput))
            {
                _locomotionInputs.Remove(locomotionInput);
            }
        }

        /// <summary>
        ///     Moves the avatar to a specified target position over a given duration using smooth locomotion.
        /// </summary>
        /// <param name="targetPosition">
        ///     The desired position to move the avatar to, specified in world space.
        /// </param>
        /// <param name="duration">
        ///     The duration in seconds over which the avatar will move to the target position. The default value is 0.5 seconds.
        /// </param>
        /// <param name="options">The options</param>
        public void MoveAvatarTo(Vector3 targetPosition, UxrSmoothMoveToOptions options = UxrSmoothMoveToOptions.Default, float duration = UxrConstants.Locomotion.DiscreteTranslationSeconds)
        {
            if (!IsTranslationAllowed && !options.HasFlag(UxrSmoothMoveToOptions.Force))
            {
                return;
            }

            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
            }

            _moveCoroutine = StartCoroutine(MoveAvatarCoroutine(targetPosition, duration));
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            _isMidRotation = false;

            _oldAvatarPosition = Avatar.transform.position;
            _oldAvatarRotation = Avatar.transform.rotation;

            TargetLocalCamRotation    = CameraController.localRotation;
            TargetLocalAvatarRotation = AvatarRoot.localRotation;

            UxrAvatar.GlobalAvatarMoving += UxrAvatar_GlobalAvatarMoving;
            UxrAvatar.GlobalAvatarMoved  += UxrAvatar_GlobalAvatarMoved;
        }

        /// <inheritdoc />
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrAvatar.GlobalAvatarMoving -= UxrAvatar_GlobalAvatarMoving;
            UxrAvatar.GlobalAvatarMoved  -= UxrAvatar_GlobalAvatarMoved;
        }

        /// <inheritdoc />
        protected override void Start()
        {
            base.Start();

            foreach (IUxrSmoothLocomotionInput locomotionInput in GetDefaultInputs())
            {
                RegisterInput(locomotionInput);
            }
        }

        /// <summary>
        ///     Updates the character controller dimensions and center position based on the head's current position.
        /// </summary>
        /// <remarks>
        ///     Adjusts the character controller's radius and height to maintain appropriate proportions for the avatar
        ///     based on the head's y-coordinate. Ensures the center of the character controller aligns with the avatar's
        ///     head position horizontally and calculates the center's vertical position relative to the head's height
        ///     and center offset.
        /// </remarks>
        protected virtual void LateUpdate()
        {
            TryAdjustCharacterControllerCollider();
        }

        /// <summary>
        ///     Used to find a <see cref="UnityEngine.CharacterController" /> component attached to the avatar if there is none.
        ///     When assigned, it will fit the collider dimensions and center.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            // Initialize fields if they are not initialized yet. This is both for TryAdjustCharacterControllerCollider() and
            // because child OnValidate() may query parent properties to initialize child values.

            _generalParameters  ??= new GeneralParameters();
            _movementParameters ??= new MovementParameters();
            _rotationParameters ??= new RotationParameters();
            _gravityParameters  ??= new GravityParameters();
            _colliderParameters ??= new ColliderParameters();

            if (CharacterController == null)
            {
                CharacterController = transform.SafeGetComponentInParent<CharacterController>();

                // Only here, because otherwise it can be frustrating if it changes automatically whenever the user changes something.
                TryAdjustCharacterControllerCollider();
            }
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Coroutine that moves the avatar smoothly to a target position over a specified duration.
        /// </summary>
        /// <param name="targetPosition">
        ///     The target position where the avatar will be moved.
        /// </param>
        /// <param name="duration">
        ///     The time in seconds over which the movement will occur. Default is 0.5 seconds.
        /// </param>
        /// <returns>
        ///     An enumerator used to control the coroutine's execution.
        /// </returns>
        private IEnumerator MoveAvatarCoroutine(Vector3 targetPosition, float duration)
        {
            float   timer    = 0.0f;
            Vector3 startPos = Avatar.transform.position;

            bool wasEnabled = CharacterController.enabled;
            CharacterController.enabled = false;

            Vector3 targetDirection = Avatar.ProjectedCameraForward;

            while (timer < duration)
            {
                timer += Time.deltaTime;
                float t = timer / duration;

                UxrManager.Instance.MoveAvatarTo(Avatar, Vector3.Lerp(startPos, targetPosition, t), targetDirection, source: this);
                yield return null;
            }

            UxrManager.Instance.MoveAvatarTo(Avatar, targetPosition, targetDirection, source: this);
            CharacterController.enabled = wasEnabled;
            _moveCoroutine              = null;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called when any avatar is about to move globally. We use it to disable the Character
        ///     Controller and re-enable it again after it moved. This is only required if the source
        ///     of the movement was external and not this component itself.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing information</param>
        private void UxrAvatar_GlobalAvatarMoving(object sender, UxrAvatarMoveEventArgs e)
        {
            if (e.Avatar == Avatar && !ReferenceEquals(sender, this) && CharacterController != null)
            {
                _wasCCEnabledOnGlobalAvatarMoving = CharacterController.enabled;
                CharacterController.enabled = false;
            }
        }

        /// <summary>
        ///     Handles the event triggered when the avatar has completed its movement globally. We use it
        ///     to re-enable the Character Controller after the movement has finished. This is only required
        ///     if the source of the movement was external and not this component itself.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing information</param>
        private void UxrAvatar_GlobalAvatarMoved(object sender, UxrAvatarMoveEventArgs e)
        {
            if (e.Avatar == Avatar && !ReferenceEquals(sender, this) && CharacterController != null)
            {
                CharacterController.enabled = _wasCCEnabledOnGlobalAvatarMoving;
            }
        }

        #endregion

        #region Protected Overrides UxrLocomotion

        /// <summary>
        ///     Gathers input and updates the physics parameters.
        /// </summary>
        protected override void UpdateLocomotion()
        {
            UpdateLocomotionInputs();
            InterpolateLocomotion();
            CheckParentAvatarDestination();
            CheckRaiseAvatarMoved();
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Retrieves the default set of smooth locomotion inputs for a derived class. By default, for virtual reality,
        ///     it will return the specific input for VR controllers. For the display system (mobile, desktop, etc.),
        ///     it will return keyboard and mouse, touch screen, and gamepad.
        /// </summary>
        /// <returns>
        ///     A collection of input implementations that are used to handle smooth locomotion behavior.
        /// </returns>
        protected abstract IEnumerable<IUxrSmoothLocomotionInput> GetDefaultInputs();

        /// <summary>
        ///     Gets the basis transform for locomotion movement, which determines the forward direction
        ///     depending on the defined reference.
        /// </summary>
        /// <returns>
        ///     The transform used as the movement basis. Returns the head transform by default,
        ///     unless the relative reference is set to the left hand or right hand in other implementations.
        /// </returns>
        protected virtual Transform GetMovementBasis()
        {
            return CameraController;
        }

        /// <summary>
        ///     Retrieves and processes the various locomotion input states. It updates the state of the locomotion system
        ///     accordingly and triggers appropriate actions.
        /// </summary>
        protected virtual void UpdateLocomotionInputs()
        {
            IsSprinting = UxrSmoothLocomotionInputExtensions.IsSprintInput(_locomotionInputs);

            ProcessMovementInput();
            ProcessRotationInput();

            if (UxrSmoothLocomotionInputExtensions.IsJumpInput(_locomotionInputs))
            {
                PerformJump();
            }
        }

        /// <summary>
        ///     Handles smooth movement of the body based on the given input, applying acceleration, gravity, and constraints.
        /// </summary>
        /// <param name="movementInput">
        ///     The input data describing the desired movement direction and speed. It also includes flags like whether
        ///     to use acceleration for smoothing the movement.
        /// </param>
        protected virtual void MoveBody(UxrMovementInput movementInput)
        {
            if (!IsTranslationAllowed || !CharacterController.enabled || UxrCameraWallFade.IsAvatarInsideGeometry(Avatar))
            {
                return;
            }

            Transform basis     = GetMovementBasis();
            Vector3   forward   = basis.forward;
            Vector3   right     = basis.right;
            Vector2   moveInput = movementInput.Input;

            // Movement on a horizontal plane (prevents looking up/down from affecting the vertical component).
            forward.y = 0f;
            right.y   = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 desiredDir = forward * moveInput.y + right * moveInput.x;
            desiredDir = Vector3.ClampMagnitude(desiredDir, 1f);

            Vector3 desiredVel = desiredDir * MaxSpeed;

            if (movementInput.UseAcceleration && UseAcceleration)
            {
                float usedAccel = desiredVel.sqrMagnitude >= _horizontalVelocity.sqrMagnitude ? Acceleration : Deceleration;
                _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, desiredVel, usedAccel * Time.deltaTime);
            }
            else
            {
                _horizontalVelocity = desiredVel;
            }

            ApplyGravity();

            Vector3 motion = (_horizontalVelocity + Vector3.up * _verticalSpeed) * (Time.deltaTime * (IsSprinting ? SprintModifier : 1f));

            // Constrain if necessary
            ValidateMovementBounds(ref motion);

            // Check if step movement is allowed
            float originalStepOffset = CharacterController.stepOffset;
            if (motion != Vector3.zero)
            {
                ValidateStepMovementDestination(motion);
            }

            // Apply final motion
            CharacterController.Move(motion);

            // Restored original step movement offset
            CharacterController.stepOffset = originalStepOffset;
        }

        /// <summary>
        ///     Rotates the avatar based on a two-dimensional input specifying yaw and pitch changes.
        /// </summary>
        /// <param name="rotationInput">
        ///     Two-dimensional input where the x-component represents yaw rotation, and the y-component
        ///     represents pitch rotation. Positive x rotates the head to the right, and positive y rotates it upwards.
        /// </param>
        /// <remarks>
        ///     For the yaw rotation, the avatar is rotated using AvatarRoot. And for pitch rotation, the head is rotated
        ///     using the camera controller (the camera parent).
        /// </remarks>
        protected virtual void RotateAvatar(Vector2 rotationInput)
        {
            if (TurnType == UxrTurnType.NotAllowed)
            {
                return;
            }

            if (TurnType == UxrTurnType.Smooth)
            {
                // Yaw delta
                float angleYawDelta = rotationInput.x * SmoothTurnSpeedDeg * Time.deltaTime;
                TargetLocalAvatarRotation = Quaternion.AngleAxis(angleYawDelta, Vector3.up) * TargetLocalAvatarRotation;

                // Pitch delta
                float anglePitchDelta = -rotationInput.y * SmoothTurnSpeedDeg * Time.deltaTime;

                // Current pitch from the current target local rotation
                Vector3 localForward = TargetLocalCamRotation * Vector3.forward;
                float   currentPitch = Vector3.SignedAngle(Vector3.forward, localForward, Vector3.right);

                // Apply delta and clamp
                float targetPitch = currentPitch + anglePitchDelta;
                ValidateCameraPitch(ref targetPitch);

                // Rebuild pitch-only local rotation
                TargetLocalCamRotation = Quaternion.AngleAxis(targetPitch, Vector3.right);

                return;
            }

            if (rotationInput.x < TurnDeadzone && rotationInput.x > -TurnDeadzone)
            {
                // Allow without cooldown if user depresses to press back again.
                _cooldown = -1.0f;
            }
            else
            {
                _cooldown -= Time.deltaTime;
                if (_cooldown > 0f)
                {
                    return;
                }
            }

            bool  rotate  = false;
            float degrees = 0.0f;

            if (rotationInput.x >= TurnDeadzone)
            {
                rotate    = true;
                degrees   = TurnStepDegrees;
                _cooldown = TurnCooldown;
            }
            else if (rotationInput.x <= -TurnDeadzone)
            {
                rotate    = true;
                degrees   = -TurnStepDegrees;
                _cooldown = TurnCooldown;
            }

            if (rotate)
            {
                if (TurnType == UxrTurnType.Fade)
                {
                    UxrManager.Instance.TeleportFadeColor = FadeTurnColor;
                }

                _isMidRotation = true;
                UxrManager.Instance.RotateLocalAvatar(degrees,
                                                      TurnType,
                                                      TurnSeconds,
                                                      finishedCallback: _ =>
                                                                        {
                                                                            _isMidRotation            = false;
                                                                            TargetLocalCamRotation    = CameraController.localRotation;
                                                                            TargetLocalAvatarRotation = AvatarRoot.localRotation;
                                                                        },
                                                      source: this);
            }
        }

        /// <summary>
        ///     Validates and clamps the provided camera pitch value to ensure it remains within the allowed range.
        /// </summary>
        /// <param name="pitch">
        ///     The camera pitch angle to be validated and potentially adjusted. The value will be modified to stay within
        ///     the maximum camera pitch limits defined by the locomotion system.
        /// </param>
        protected virtual void ValidateCameraPitch(ref float pitch)
        {
        }

        /// <summary>
        ///     Validates the movement bounds to ensure that the motion does not exceed the defined constraints.
        /// </summary>
        /// <param name="motion">The motion vector to validate and potentially modify based on the movement constraints.</param>
        protected virtual void ValidateMovementBounds(ref Vector3 motion)
        {
        }

        /// <summary>
        ///     Smoothly interpolates locomotion-related transforms, including the avatar root and head,
        ///     to ensure fluid transitions during movement.
        /// </summary>
        protected virtual void InterpolateLocomotion()
        {
            if (_isMidRotation)
            {
                // Do not interpolate during non-immediate rotations (fade/interpolate).
                return;
            }

            // Apply pitch to the head

            if (!TargetLocalCamRotation.EqualsUsingAngle(CameraController.localRotation, 0.01f))
            {
                CameraController.localRotation = UxrInterpolator.SmoothDampRotation(CameraController.localRotation, TargetLocalCamRotation, SmoothRotation);
            }

            // Apply yaw to the avatar root

            if (!TargetLocalAvatarRotation.EqualsUsingAngle(Avatar.transform.localRotation, 0.01f))
            {
                AvatarRoot.localRotation = UxrInterpolator.SmoothDampRotation(AvatarRoot.localRotation, TargetLocalAvatarRotation, SmoothRotation);
            }
        }

        /// <summary>
        ///     Initiates a jump action.
        /// </summary>
        protected virtual void PerformJump()
        {
            // TODO: implement jumping
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     In editor, adds a <see cref="UnityEngine.CharacterController" /> to the avatar.
        /// </summary>
        private void AddCharacterController()
        {
#if UNITY_EDITOR
            GameObject root = Avatar?.gameObject;

            if (root != null)
            {
                CharacterController = Undo.AddComponent<CharacterController>(root);
                TryAdjustCharacterControllerCollider();
            }
#endif
        }

        /// <summary>
        ///     Adjusts the <see cref="CharacterController" /> capsule so that it follows the user's head position.
        /// </summary>
        /// <remarks>
        ///     The capsule center follows the head on the horizontal plane (X/Z) while remaining vertically centered.
        ///     During play mode, the method includes safety checks:
        ///     <list type="bullet">
        ///         <item>
        ///             If the avatar is currently inside a wall-fade state (see <see cref="UxrCameraWallFade" />),
        ///             the collider is not updated, preserving the last valid configuration until the user returns
        ///             to a safe area.
        ///         </item>
        ///         <item>
        ///             If smooth locomotion input is detected and the head has drifted away from the collider beyond
        ///             a threshold, the avatar is snapped back to the last valid collider position before applying movement.
        ///         </item>
        ///         <item>
        ///             Before applying any change, the candidate capsule (center, height, radius) is validated against
        ///             the environment. If the new configuration overlaps geometry, the method computes the closest
        ///             valid capsule along the path from the current to the desired center, preventing penetration while
        ///             still allowing partial adjustment. In this case the head will drift away from the collider.
        ///         </item>
        ///     </list>
        /// </remarks>
        private void TryAdjustCharacterControllerCollider()
        {
            if (!Avatar || !CameraComponent || _generalParameters == null || _colliderParameters == null || !CharacterController || _moveCoroutine != null)
            {
                return;
            }

            if (Application.isPlaying && UxrCameraWallFade.IsAvatarInsideFade(Avatar))
            {
                return;
            }

            float eyeHeight = Application.isPlaying ? Avatar.CurrentCameraEyeLevel : DefaultEyeHeight;

            if (eyeHeight <= 0.0f)
            {
                eyeHeight = Mathf.Lerp(ColliderMinHeight, ColliderMaxHeight, 0.5f);
            }

            eyeHeight = Mathf.Clamp(eyeHeight, ColliderMinHeight, ColliderMaxHeight);

            float totalHeight = eyeHeight + EyeToTopDistance;
            float radius      = ColliderRadius;

            Vector3 headLocal     = CameraComponent.transform.localPosition;
            Vector3 desiredCenter = new Vector3(headLocal.x, totalHeight * 0.5f + ColliderCenterYOffset, headLocal.z);

            if (Application.isPlaying)
            {
                UxrMovementInput input = UxrSmoothLocomotionInputExtensions.GetMovementInput(_locomotionInputs);

                Vector3 currentCenterXZ = CharacterController.center;
                currentCenterXZ.y = 0.0f;

                Vector3 desiredCenterXZ = desiredCenter;
                desiredCenterXZ.y = 0.0f;

                float sqrDistanceToCollider = (desiredCenterXZ - currentCenterXZ).sqrMagnitude;

                if (input.Input.sqrMagnitude > 0.0001f && sqrDistanceToCollider > SnapBackDistanceThreshold * SnapBackDistanceThreshold)
                {
                    // Snap tracking space/head back to the last valid collider position.
                    SnapTrackingSpaceBackToCharacterController();

                    // Recompute desired center after the snap.
                    desiredCenter = new Vector3(headLocal.x, totalHeight * 0.5f + ColliderCenterYOffset, headLocal.z);
                }
                else if (WouldCharacterControllerOverlap(desiredCenter, totalHeight, radius))
                {
                    desiredCenter = GetClosestValidCharacterControllerCenter(CharacterController.center, desiredCenter, totalHeight, radius);
                }
            }

            CharacterController.radius = radius;
            CharacterController.height = totalHeight;
            CharacterController.center = desiredCenter;

#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }

        /// <summary>
        ///     Checks if the character controller would overlap with any colliders in the scene.
        /// </summary>
        /// <param name="center"> The local center position of the character controller. </param>
        /// <param name="height"> The height of the character controller capsule. </param>
        /// <param name="radius"> The radius of the character controller capsule. </param>
        /// <returns>
        ///     Returns true if the character controller overlaps with any colliders, otherwise false.
        /// </returns>
        private bool WouldCharacterControllerOverlap(Vector3 center, float height, float radius)
        {
            GetCharacterControllerCapsulePoints(center, height, radius, out Vector3 point1, out Vector3 point2);

            return HasBlockingCapsuleOverlap(UxrLocomotionRaycastPurpose.Validation,
                                             Avatar,
                                             point1,
                                             point2,
                                             radius,
                                             GetCollisionMask(CharacterController.gameObject.layer),
                                             QueryTriggerInteraction.Ignore) != null;
        }

        /// <summary>
        ///     Computes the closest valid <see cref="CharacterController" /> center along the path between a current
        ///     valid center and a desired target center.
        /// </summary>
        /// <param name="fromCenter">The starting center, assumed to be valid (non-overlapping).</param>
        /// <param name="toCenter">The desired target center that may result in overlap.</param>
        /// <param name="height">The capsule height used for validation.</param>
        /// <param name="radius">The capsule radius used for validation.</param>
        /// <returns>
        ///     The furthest center along the segment from <paramref name="fromCenter" /> to <paramref name="toCenter" />
        ///     that does not overlap geometry.
        /// </returns>
        private Vector3 GetClosestValidCharacterControllerCenter(Vector3 fromCenter, Vector3 toCenter, float height, float radius)
        {
            Vector3 delta = toCenter - fromCenter;

            if (delta.sqrMagnitude <= Mathf.Epsilon)
            {
                return fromCenter;
            }

            if (WouldCharacterControllerOverlap(fromCenter, height, radius))
            {
                return CharacterController.center;
            }

            GetCharacterControllerCapsulePoints(fromCenter, height, radius, out Vector3 point1, out Vector3 point2);

            Vector3 direction   = delta.normalized;
            float   maxDistance = delta.magnitude;

            if (HasBlockingCapsuleCastHit(UxrLocomotionRaycastPurpose.Validation, Avatar, point1, point2, radius, direction, maxDistance, GetCollisionMask(CharacterController.gameObject.layer), QueryTriggerInteraction.Ignore, out RaycastHit hit))
            {
                float safeDistance = Mathf.Max(0.0f, hit.distance - CharacterController.skinWidth);
                return fromCenter + direction * safeDistance;
            }

            if (!WouldCharacterControllerOverlap(toCenter, height, radius))
            {
                return toCenter;
            }

            return fromCenter;
        }

        /// <summary>
        ///     Repositions the avatar so that the head aligns with the current
        ///     <see cref="CharacterController" /> center, restoring the last valid collision state.
        /// </summary>
        /// <remarks>
        ///     This method snaps the user back to the collision-safe position when artificial locomotion
        ///     starts after physical head movement caused a desynchronization between the HMD and the
        ///     collision capsule.
        /// </remarks>
        private void SnapTrackingSpaceBackToCharacterController()
        {
            if (!Avatar || !CharacterController || !CameraComponent)
            {
                return;
            }

            Vector3 targetPosition = CharacterController.transform.TransformPoint(CharacterController.center);
            targetPosition.y = Avatar.transform.position.y;

            MoveAvatarTo(targetPosition);
        }

        /// <summary>
        ///     Computes the world-space capsule endpoints used by a <see cref="CharacterController" />
        ///     for a given local center, height, and radius.
        /// </summary>
        /// <param name="center">The capsule center in local space (CharacterController.center).</param>
        /// <param name="height">The capsule height.</param>
        /// <param name="radius">The capsule radius.</param>
        /// <param name="point1">Output top sphere center in world space.</param>
        /// <param name="point2">Output bottom sphere center in world space.</param>
        /// <remarks>
        ///     The capsule is aligned along the transform's up axis. The segment between <paramref name="point1" />
        ///     and <paramref name="point2" /> defines the cylindrical part of the capsule, with spheres of
        ///     radius <paramref name="radius" /> at both ends.
        /// </remarks>
        private void GetCharacterControllerCapsulePoints(Vector3 center, float height, float radius, out Vector3 point1, out Vector3 point2)
        {
            Transform t = CharacterController.transform;

            // Convert local center to world
            Vector3 worldCenter = t.TransformPoint(center);

            // Ensure valid capsule (height must be at least diameter)
            float clampedHeight = Mathf.Max(height, radius * 2.0f);

            // Half-distance between the sphere centers
            float halfSegment = clampedHeight * 0.5f - radius;

            Vector3 up = t.up;

            point1 = worldCenter + up * halfSegment; // Top sphere center
            point2 = worldCenter - up * halfSegment; // Bottom sphere center
        }

        /// <summary>
        ///     Handles avatar parenting based on the surface it is currently standing on.
        /// </summary>
        /// <remarks>
        ///     This method checks if the avatar is positioned on a surface that has a component
        ///     of type <see cref="UxrParentAvatarDestination" />. If such a component is found and it specifies a parent avatar,
        ///     the avatar's transform is set as a child of the surface's transform. If not, the avatar remains unparented.
        ///     This ensures the avatar properly follows moving platforms or dynamic surfaces.
        /// </remarks>
        private void CheckParentAvatarDestination()
        {
            bool      foundCurrentParent = false;
            Transform currentParent      = Avatar.transform.parent;

            Vector3      rayOrigin = CharacterController.transform.position;
            RaycastHit[] hits      = new RaycastHit[10];

            if (Physics.RaycastNonAlloc(rayOrigin, Vector3.down, hits, 0.2f) > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider)
                    {
                        UxrParentAvatarDestination parentAvatarDestination = hit.collider.GetComponent<UxrParentAvatarDestination>();
                        if (parentAvatarDestination && parentAvatarDestination.ParentAvatar)
                        {
                            Avatar.transform.SetParent(hit.collider.transform);
                            foundCurrentParent = true;
                        }
                        else if (currentParent != null && hit.collider.transform == currentParent)
                        {
                            foundCurrentParent = true;
                        }
                    }
                }
            }

            if (currentParent != null && !foundCurrentParent)
            {
                Avatar.transform.SetParent(null);
            }
        }

        /// <summary>
        ///     Checks if the avatar has moved by comparing the current avatar position and rotation to the previous values.
        ///     If movement is detected, raises the <c>AvatarMoved</c> event and updates the cached position and rotation.
        /// </summary>
        private void CheckRaiseAvatarMoved()
        {
            if (Vector3.Distance(Avatar.transform.position, _oldAvatarPosition) > 0.01f)
            {
                UxrAvatarMoveEventArgs moveEventArgs = UxrAvatarMoveEventArgs.GetFromPool(Avatar, _oldAvatarPosition, _oldAvatarRotation, Avatar.transform.position, Avatar.transform.rotation, this);
                Avatar.RaiseAvatarMoved(this, moveEventArgs);

                // Cache avatar position and rotation
                _oldAvatarPosition = Avatar.transform.position;
                _oldAvatarRotation = Avatar.transform.rotation;
            }
        }

        /// <summary>
        ///     Validates the step movement destination to ensure it does not interfere with prohibited locomotion areas.
        /// </summary>
        /// <param name="motion">The motion vector indicating the intended movement direction and distance.</param>
        private void ValidateStepMovementDestination(Vector3 motion)
        {
            Vector3 displacement = motion;
            displacement.y = 0f;
            float   checkHeight = CharacterController.radius             + 0.01f;
            Vector3 rayOrigin   = CharacterController.transform.position + displacement + Vector3.up * checkHeight;

            RaycastHit[] hits = new RaycastHit[10];
            if (Physics.SphereCastNonAlloc(rayOrigin, CharacterController.radius * 1.1f, displacement.normalized, hits, 0.01f) > 0)
            {
                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider && hit.collider.GetComponentInParent<UxrProhibitLocomotionDestination>())
                    {
                        CharacterController.stepOffset = 0f;
                        break;
                    }
                }
            }
        }

        /// <summary>
        ///     Processes user movement input and applies it to the locomotion system.
        /// </summary>
        private void ProcessMovementInput()
        {
            UxrMovementInput movementInput = UxrSmoothLocomotionInputExtensions.GetMovementInput(_locomotionInputs);

            if (movementInput.Input.magnitude < MovementDeadzone)
            {
                movementInput.Input = Vector2.zero;
            }

            MoveBody(movementInput);
        }

        /// <summary>
        ///     Processes the rotation input by retrieving rotation values and applying them to control the avatar's head rotation.
        /// </summary>
        private void ProcessRotationInput()
        {
            Vector2 rotationInput = UxrSmoothLocomotionInputExtensions.GetRotationInput(_locomotionInputs);

            RotateAvatar(rotationInput);
        }

        /// <summary>
        ///     Applies gravity effects to the vertical velocity of the character. Adjusts the vertical velocity
        ///     depending on whether the character is grounded or in the air. If grounded, applies a force to
        ///     stick the character to the ground. If in the air, increases the downward velocity until the terminal
        ///     velocity is reached.
        /// </summary>
        private void ApplyGravity()
        {
            if (CharacterController.isGrounded)
            {
                if (_verticalSpeed < 0f)
                {
                    _verticalSpeed = GroundedStickForce;
                }
            }
            else
            {
                _verticalSpeed += Gravity * Time.deltaTime;
                if (_verticalSpeed < TerminalVelocity)
                {
                    _verticalSpeed = TerminalVelocity;
                }
            }
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Returns the default eye height of the avatar. 0 if it doesn't have a specific default eye height.
        ///     This is used for non-VR implementations.
        /// </summary>
        protected virtual float DefaultEyeHeight
        {
            get
            {
                float eyeHeight = 0.0f;

                if (Avatar.AvatarController is UxrStandardAvatarController standardAvatarController)
                {
                    eyeHeight = Avatar.AvatarRig.HasAnyUpperBodyIKReference() && standardAvatarController.UseBodyIK ? standardAvatarController.BodyIKSettings.EyesBaseHeight : 0.0f;
                }

                return eyeHeight;
            }
        }

        protected bool       IsComponentFieldsInitialized => _generalParameters != null;
        protected Transform  AvatarRoot                   => Avatar.transform;
        protected Transform  CameraController             => Avatar?.CameraController;
        protected Camera     CameraComponent              => Avatar.CameraComponent;
        protected bool       IsSprinting                  { get; private set; }
        protected Quaternion TargetLocalCamRotation       { get; set; }
        protected Quaternion TargetLocalAvatarRotation    { get; set; }

        #endregion

        #region Private Types & Data

        [Serializable]
        private class GeneralParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(                                                                                                                                         CharacterColliderToolTip)]                         public CharacterController _characterController;
            [ShowIf(nameof(_characterController), null)] [InspectorButton("Add Character Controller", nameof(AddCharacterController))] [Tooltip(              ButtonGenerateControllerToolTip)] [SerializeField] public bool                _buttonGenerateCharacterController;
            [HideIf(nameof(_characterController), null)] [InspectorButton("Fit Collider",             nameof(TryAdjustCharacterControllerCollider))] [Tooltip(ButtonAdjustColliderToolTip)] [SerializeField]     public bool                _buttonAdjustCollider;

            #endregion

            #region Public Types & Data

            public const string CharacterColliderToolTip        = "Specifies the Unity CharacterController component that drives the locomotion system.";
            public const string ButtonGenerateControllerToolTip = "Adds a CharacterController on the root of the avatar.";
            public const string ButtonAdjustColliderToolTip     = "Adjusts the CharacterController collider to fit the parameters in the Collider section.";

            #endregion
        }

        [Serializable]
        private class MovementParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(                                         MaxSpeedToolTip)]                  public float _maxSpeed                  = 3.5f;
            [Tooltip(                                         UseAccelerationToolTip)]           public bool  _useAcceleration           = true;
            [ShowIf(nameof(_useAcceleration), true)] [Tooltip(AccelerationToolTip)]              public float _acceleration              = 30.0f;
            [ShowIf(nameof(_useAcceleration), true)] [Tooltip(DecelerationToolTip)]              public float _deceleration              = 40.0f;
            [Tooltip(                                         SprintModifierToolTip)]            public float _sprintModifier            = 2.0f;
            [Tooltip(                                         DeadzoneToolTip)]                  public float _deadzone                  = 0.15f;
            [Tooltip(                                         SnapBackDistanceThresholdToolTip)] public float _snapBackDistanceThreshold = 0.20f;

            #endregion

            #region Public Types & Data

            public const string MaxSpeedToolTip                  = "Maximum horizontal movement speed.";
            public const string UseAccelerationToolTip           = "Smooths movement by making speed build up and slow down gradually instead of changing instantly.";
            public const string AccelerationToolTip              = "How quickly movement speed increases until reaching the target speed.";
            public const string DecelerationToolTip              = "How quickly movement speed decreases when movement input is reduced or released.";
            public const string SprintModifierToolTip            = "Extra speed multiplier applied while sprinting.";
            public const string DeadzoneToolTip                  = "Minimum movement input required before movement starts. Helps ignore small unwanted stick or input drift.";
            public const string SnapBackDistanceThresholdToolTip = "Minimum distance required to snap back to the valid position when the user tries to move but the collider is stuck.";

            #endregion
        }

        [Serializable]
        private class RotationParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(                                                                        TurnTypeToolTip)]               public UxrTurnType _turnType               = UxrTurnType.Snap;
            [HideIf(nameof(_turnType), UxrTurnType.NotAllowed, UxrTurnType.Smooth)] [Tooltip(TurnDeadzoneToolTip)]           public float       _turnDeadzone           = 0.15f;
            [HideIf(nameof(_turnType), UxrTurnType.NotAllowed, UxrTurnType.Smooth)] [Tooltip(TurnCooldownToolTip)]           public float       _turnCooldown           = 0.25f;
            [HideIf(nameof(_turnType), UxrTurnType.NotAllowed, UxrTurnType.Smooth)] [Tooltip(TurnStepDegreesToolTip)]        public float       _turnStepDegrees        = 45f;
            [ShowIf(nameof(_turnType), UxrTurnType.Fade)] [Tooltip(                          FadeTurnColorToolTip)]          public Color       _fadeTurnColor          = Color.black;
            [ShowIf(nameof(_turnType), UxrTurnType.Fade)] [Tooltip(                          FadeTurnSecondsToolTip)]        public float       _fadeTurnSeconds        = UxrConstants.Locomotion.DiscreteTurnSeconds;
            [ShowIf(nameof(_turnType), UxrTurnType.Interpolate)] [Tooltip(                   InterpolateTurnSecondsToolTip)] public float       _interpolateTurnSeconds = UxrConstants.Locomotion.DiscreteTurnSeconds;
            [ShowIf(nameof(_turnType), UxrTurnType.Smooth)] [Tooltip(                        SmoothTurnSpeedDegToolTip)]     public float       _smoothTurnSpeedDeg     = 90.0f;
            [Range(0.0f, 1.0f)] [Tooltip(                                                    SmoothRotationToolTip)]         public float       _smoothRotation         = 0.1f;

            #endregion

            #region Public Types & Data

            public const string TurnTypeToolTip               = "Turning mode used by locomotion. Depending on the mode, turning can be blocked, smooth, snap-based, faded or interpolated.";
            public const string TurnDeadzoneToolTip           = "Minimum horizontal rotation input required before a discrete turn is triggered.";
            public const string TurnCooldownToolTip           = "Time to wait before another discrete turn can happen while the turn input is still held.";
            public const string TurnStepDegreesToolTip        = "Angle applied on each discrete turn.";
            public const string FadeTurnColorToolTip          = "Screen color used during fade turns.";
            public const string FadeTurnSecondsToolTip        = "Duration of the fade effect used when turning in Fade mode.";
            public const string InterpolateTurnSecondsToolTip = "Duration of the rotation transition used when turning in Interpolate mode.";
            public const string SmoothTurnSpeedDegToolTip     = "Continuous turning speed in degrees per second when using Smooth mode.";
            public const string SmoothRotationToolTip         = "How smoothly avatar and camera rotation move toward their target orientation. Lower values feel more responsive.";

            #endregion
        }

        [Serializable]
        private class GravityParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(GravityToolTip)]            public float _gravity            = -9.81f;
            [Tooltip(GroundedStickForceToolTip)] public float _groundedStickForce = -2.0f;
            [Tooltip(TerminalVelocityToolTip)]   public float _terminalVelocity   = -20.0f;

            #endregion

            #region Public Types & Data

            public const string GravityToolTip            = "Downward acceleration applied while falling.";
            public const string GroundedStickForceToolTip = "Small downward force applied while grounded to help keep the character attached to the floor.";
            public const string TerminalVelocityToolTip   = "Maximum falling speed.";

            #endregion
        }

        [Serializable]
        private class ColliderParameters
        {
            #region Inspector Properties/Serialized Fields

            [Tooltip(MinHeightToolTip)]        public float _minHeight        = 1.2f;
            [Tooltip(MaxHeightToolTip)]        public float _maxHeight        = 2.2f;
            [Tooltip(RadiusToolTip)]           public float _radius           = 0.2f;
            [Tooltip(EyeToTopDistanceToolTip)] public float _eyeToTopDistance = 0.08f;
            [Tooltip(CenterYOffsetToolTip)]    public float _centerYOffset;

            #endregion

            #region Public Types & Data

            public const string MinHeightToolTip        = "Minimum height allowed for the character collider when it is adjusted to the avatar.";
            public const string MaxHeightToolTip        = "Maximum height allowed for the character collider when it is adjusted to the avatar.";
            public const string RadiusToolTip           = "Radius of the avatar capsule collider.";
            public const string EyeToTopDistanceToolTip = "Distance from the eyes to the top of the head. This is used to adjust the top of the capsule collider.";
            public const string CenterYOffsetToolTip    = "Vertical offset added to the collider center. Useful to fine-tune how the capsule aligns with the avatar.";

            #endregion
        }

        /// <summary>
        ///     Gets the turn transition in seconds depending on <see cref="TurnType" />.
        /// </summary>
        private float TurnSeconds
        {
            get
            {
                return TurnType switch
                       {
                           UxrTurnType.Fade        => FadeTurnSeconds,
                           UxrTurnType.Interpolate => InterpolateTurnSeconds,
                           _                       => 0.0f
                       };
            }
        }

        private readonly List<IUxrSmoothLocomotionInput> _locomotionInputs = new List<IUxrSmoothLocomotionInput>();

        private float      _cooldown;
        private Vector3    _oldAvatarPosition;
        private Quaternion _oldAvatarRotation;
        private Vector3    _horizontalVelocity;
        private float      _verticalSpeed;
        private bool       _isMidRotation;
        private Coroutine  _moveCoroutine;
        private Collider[] _bodyColliders;
        private bool       _wasCCEnabledOnGlobalAvatarMoving;

        #endregion
    }
}