// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Cabinet.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UltimateXR.Manipulation;
using UnityEngine;

namespace UltimateXR.Examples.FullScene.Lab
{
    /// <summary>
    ///     Implements cabinet interaction logic by coordinating a door, handle, and locking pins.
    /// </summary>
    public class Cabinet : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [Tooltip(DoorTooltip)]                          private UxrGrabbableObject _door;
        [SerializeField] [Tooltip(HandleTooltip)]                        private UxrGrabbableObject _handle;
        [SerializeField] [Tooltip(LockingPinsTooltip)]                   private Transform          _lockingPins;
        [SerializeField] [Tooltip(LockingPinsInsertedOffsetTooltip)]     private Vector3            _lockingPinsInsertedOffset     = new Vector3(0.02f, 0.0f, 0.0f);
        [SerializeField] [Tooltip(HandleDegreesToOpenTooltip)]           private float              _handleDegreesToOpen           = -45.0f;
        [SerializeField] [Tooltip(LockingPinsDoorBlockedAngleTooltip)]   private float              _lockingPinsDoorBlockedAngle   = 3.0f;
        [SerializeField] [Tooltip(LockingPinsHandleBlockedAngleTooltip)] private float              _lockingPinsHandleBlockedAngle = -50.0f;

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes initial constraint values used by the cabinet locking behavior.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _door.RotationConstraint = UxrRotationConstraintMode.Locked;
            _handleRotConstraintMax  = _handle.MaxSingleRotationDegrees;
            _doorRotConstraintMin    = _door.MinSingleRotationDegrees;
        }

        /// <summary>
        ///     Subscribes to the avatars updated event so that the manipulation logic is done after all manipulation
        ///     logic has been updated.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrManager.AvatarsUpdated += UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Unsubscribes from the avatars updated event.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrManager.AvatarsUpdated -= UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Caches the initial local position of the locking pins transform.
        /// </summary>
        protected override void Start()
        {
            base.Awake();
            _lockingPinsLocalPosition = _lockingPins.localPosition;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called after UltimateXR has done all the frame updating. Does the manipulation logic.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            // Is it grabbed by the local avatar, and it also was the first one to grab the object?
            bool grabbedByLocalAvatar = _handle.IsBeingGrabbed && UxrGrabManager.Instance.GetGrabbingHands(_handle).First().Avatar.AvatarMode == UxrAvatarMode.Local;

            if (grabbedByLocalAvatar)
            {
                if (_handle.SingleRotationAxisDegrees < _handleDegreesToOpen && _door.RotationConstraint == UxrRotationConstraintMode.Locked)
                {
                    // Door becomes unlocked
                    _door.RotationConstraint = UxrRotationConstraintMode.RestrictLocalRotation;
                }
                else if (_handle.SingleRotationAxisDegrees > _handleDegreesToOpen && _door.RotationConstraint == UxrRotationConstraintMode.RestrictLocalRotation && _door.SingleRotationAxisDegrees <= 0.0f)
                {
                    // Door becomes locked
                    _door.RotationConstraint = UxrRotationConstraintMode.Locked;
                }
                else if (_door.SingleRotationAxisDegrees >= _lockingPinsDoorBlockedAngle)
                {
                    // Door is open and pins are in the way, so don't let the door close unless the pins are below threshold.

                    if (_handle.SingleRotationAxisDegrees > _handleDegreesToOpen)
                    {
                        // Block
                        if (Mathf.Approximately(_door.MinSingleRotationDegrees, _doorRotConstraintMin))
                        {
                            _door.MinSingleRotationDegrees = _lockingPinsDoorBlockedAngle;
                        }
                    }
                    else
                    {
                        // Unblock
                        if (!Mathf.Approximately(_door.MinSingleRotationDegrees, _doorRotConstraintMin))
                        {
                            _door.MinSingleRotationDegrees = _doorRotConstraintMin;
                        }
                    }
                }

                if (_door.SingleRotationAxisDegrees < _lockingPinsDoorBlockedAngle && _door.SingleRotationAxisDegrees > 0.01f)
                {
                    // This is fine-tuning: Door is in a position where the pins cannot fully extend. Reflect it in the handle range.

                    // Block
                    if (Mathf.Approximately(_handle.MaxSingleRotationDegrees, _handleRotConstraintMax))
                    {
                        _handle.MaxSingleRotationDegrees = _lockingPinsHandleBlockedAngle;
                    }
                }
                else
                {
                    // Unblock
                    if (!Mathf.Approximately(_handle.MaxSingleRotationDegrees, _handleRotConstraintMax))
                    {
                        _handle.MaxSingleRotationDegrees = _handleRotConstraintMax;
                    }
                }
            }

            // We cannot use _handle.SingleRotationAxisT. The rotation T is not computed equally when the pins are blocked because
            // we change _handle.MaxSingleRotationDegrees at runtime. We need to compute it here correctly manually.
            float handleRotationT = (_handle.SingleRotationAxisDegrees - _handle.MinSingleRotationDegrees) / (_handleRotConstraintMax - _handle.MinSingleRotationDegrees);
            _lockingPins.localPosition = _lockingPinsLocalPosition + _lockingPinsInsertedOffset * (1.0f - handleRotationT);
        }

        #endregion

        #region Private Types & Data

        private const string DoorTooltip                          = "Cabinet door grabbable object.";
        private const string HandleTooltip                        = "Cabinet handle grabbable object used to unlock the door.";
        private const string LockingPinsTooltip                   = "Transform for the locking pins visual.";
        private const string LockingPinsInsertedOffsetTooltip     = "Local offset applied when locking pins are fully inserted.";
        private const string HandleDegreesToOpenTooltip           = "Handle angle threshold to unlock the door.";
        private const string LockingPinsDoorBlockedAngleTooltip   = "Door angle where locking pins block the door from closing.";
        private const string LockingPinsHandleBlockedAngleTooltip = "Handle angle where locking pins block handle motion.";

        private Vector3 _lockingPinsLocalPosition;
        private float   _handleRotConstraintMax;
        private float   _doorRotConstraintMin;

        #endregion
    }
}