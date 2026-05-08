// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrController3DModel.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using UltimateXR.Avatar;
using UltimateXR.Avatar.Rig;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UltimateXR.Extensions.System;
using UltimateXR.Extensions.Unity;
using UnityEngine;

namespace UltimateXR.Devices.Visualization
{
    /// <summary>
    ///     Represents the 3D model of a VR controller. It allows to graphically render the current position/orientation and
    ///     input state of the device.
    /// </summary>
    public partial class UxrController3DModel : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private bool              _needsBothHands;
        [SerializeField] private UxrHandSide       _handSide;
        [SerializeField] private UxrControllerHand _controllerHand;
        [SerializeField] private UxrControllerHand _controllerHandLeft;
        [SerializeField] private UxrControllerHand _controllerHandRight;
        [SerializeField] private Transform         _forward;
        [SerializeField] private List<UxrElement>  _controllerElements = new List<UxrElement>();

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets whether the controller requires two hands to hold it.
        /// </summary>
        public bool NeedsBothHands => _needsBothHands;

        /// <summary>
        ///     Gets the hand required to hold the controller, if <see cref="NeedsBothHands" /> is false.
        /// </summary>
        public UxrHandSide HandSide => _handSide;

        /// <summary>
        ///     Gets the forward transform as it is currently in the scene. It can be different from the actual forward tracking
        ///     when we use grab mechanics because the hand transform can be modified by the grab manager and the controller
        ///     usually hangs from the hand hierarchy.
        ///     If you need to know the forward controller transform using the information of tracking sensors without any
        ///     intervention by external elements like the grabbing mechanics use <see cref="ForwardTrackingRotation" />.
        /// </summary>
        public Transform Forward => _forward;

        /// <summary>
        ///     Gets the rotation that represents the controller's forward orientation. We use this mainly to be able to align
        ///     certain mechanics no matter the controller that is currently active. A gun in a game needs to be aligned to the
        ///     controller, teleport mechanics, etc.
        /// </summary>
        public Quaternion ForwardTrackingRotation
        {
            get
            {
                IUxrControllerTracking controllerTracking = _avatar != null ? _avatar.FirstControllerTracking : null;

                if (controllerTracking == null)
                {
                    return _forward.rotation;
                }

                Quaternion relativeRotation = Quaternion.Inverse(transform.rotation) * _forward.transform.rotation;
                Quaternion sensorRotation   = _handSide == UxrHandSide.Left ? controllerTracking.SensorLeftRot : controllerTracking.SensorRightRot;

                return sensorRotation * relativeRotation;
            }
        }

        /// <summary>
        ///     Gets or sets the hand interacting with the controller when the controller is used with only one hand.
        /// </summary>
        public UxrControllerHand ControllerHand
        {
            get => _controllerHand;
            set
            {
                if (value == null && _controllerHand != null)
                {
                    ResetInteraction();
                }

                _controllerHand = value;
            }
        }

        /// <summary>
        ///     Gets or sets the left hand interacting with the controller, when the controller can be held using both
        ///     hands.
        /// </summary>
        public UxrControllerHand ControllerHandLeft
        {
            get => _controllerHandLeft;
            set
            {
                if (value == null && _controllerHandLeft != null)
                {
                    ResetInteraction();
                }

                _controllerHandLeft = value;
            }
        }

        /// <summary>
        ///     Gets or sets the right hand interacting with the controller, when the controller can be held using both
        ///     hands.
        /// </summary>
        public UxrControllerHand ControllerHandRight
        {
            get => _controllerHandRight;
            set
            {
                if (value == null && _controllerHandRight != null)
                {
                    ResetInteraction();
                }

                _controllerHandRight = value;
            }
        }

        /// <summary>
        ///     Gets or sets whether the controller is visible.
        /// </summary>
        public bool IsControllerVisible
        {
            get => _isControllerVisible;
            set
            {
                _isControllerVisible = value;
                gameObject.SetActive(_isControllerVisible);
            }
        }

        /// <summary>
        ///     Gets or sets whether the hand, if present, is visible. In setups where both hands are used, it targets visibility
        ///     of both hands.
        /// </summary>
        public bool IsHandVisible
        {
            get => _isHandVisible;
            set
            {
                _isHandVisible = value;

                if (_controllerHand != null)
                {
                    _controllerHand.gameObject.SetActive(_isHandVisible);
                }

                if (_controllerHandLeft != null)
                {
                    _controllerHandLeft.gameObject.SetActive(_isHandVisible);
                }

                if (_controllerHandRight != null)
                {
                    _controllerHandRight.gameObject.SetActive(_isHandVisible);
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Updates the current visual state using the given input.
        /// </summary>
        /// <param name="controllerInput">The input device to update the controller with</param>
        /// <param name="onlyIfControllerHand">Whether to update the visual state only if a controller hand is currently assigned</param>
        public void UpdateFromInput(UxrControllerInput controllerInput, bool onlyIfControllerHand = false)
        {
            if (controllerInput == null)
            {
                return;
            }

            // Initialize finger contacts

            foreach (UxrFingerType fingerType in _fingerTypeEnums)
            {
                _lastFingerContacts[fingerType]      = _fingerContacts[fingerType];
                _lastFingerContactsLeft[fingerType]  = _fingerContactsLeft[fingerType];
                _lastFingerContactsRight[fingerType] = _fingerContactsRight[fingerType];
                _fingerContacts[fingerType]          = -1;
                _fingerContactsLeft[fingerType]      = -1;
                _fingerContactsRight[fingerType]     = -1;
            }

            // Iterate through all elements

            for (int i = 0; i < _controllerElements.Count; i++)
            {
                UxrElement element = _controllerElements[i];

                if (element.ElementObject == null || element.Finger == UxrFingerType.None)
                {
                    continue;
                }

                // Update controller element

                bool            contact          = false;
                UxrInputButtons controllerButton = UxrControllerInput.ControllerElementToButton(element.Element);

                switch (element.ElementType)
                {
                    case UxrElementType.Button:

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                            element.ElementObject.transform.localPosition = element.InitialLocalPos;
                        }
                        else
                        {
                            if (controllerInput.GetButtonsPress(element.HandSide, controllerButton, true))
                            {
                                element.ElementObject.transform.localPosition = element.InitialLocalPos                              +
                                                                                element.LocalOffsetX * element.ButtonPressedOffset.x +
                                                                                element.LocalOffsetY * element.ButtonPressedOffset.y +
                                                                                element.LocalOffsetZ * element.ButtonPressedOffset.z;
                            }
                            else
                            {
                                element.ElementObject.transform.localPosition = element.InitialLocalPos;
                            }
                        }

                        break;

                    case UxrElementType.Input1DRotate:

                        float inputRotateValue = 0.0f;

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                        }
                        else
                        {
                            inputRotateValue = controllerInput.GetInput1D(element.HandSide, UxrControllerInput.ControllerElementToInput1D(element.Element), true);
                        }

                        Vector3 euler = element.Input1DPressedOffsetAngle * inputRotateValue;
                        element.ElementObject.transform.localRotation = element.InitialLocalRot * Quaternion.Euler(euler);

                        contact = contact || inputRotateValue > 0.01f;
                        break;

                    case UxrElementType.Input1DPush:

                        float inputPushValue = 0.0f;

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                        }
                        else
                        {
                            inputPushValue = controllerInput.GetInput1D(element.HandSide, UxrControllerInput.ControllerElementToInput1D(element.Element), true);
                        }

                        Vector3 offset = element.Input1DPressedOffset * inputPushValue;
                        element.ElementObject.transform.localPosition = element.InitialLocalPos + element.LocalOffsetX * offset.x + element.LocalOffsetY * offset.y + element.LocalOffsetZ * offset.z;
                        contact                                       = contact || inputPushValue > 0.01f;
                        break;

                    case UxrElementType.Input2DJoystick:

                        Vector2 inputValueJoystick = Vector2.zero;

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                        }
                        else
                        {
                            inputValueJoystick = controllerInput.GetInput2D(element.HandSide, UxrControllerInput.ControllerElementToInput2D(element.Element), true);
                        }

                        Vector3 euler1 = Vector3.Lerp(-element.Input2DFirstAxisOffsetAngle,  element.Input2DFirstAxisOffsetAngle,  (inputValueJoystick.x + 1.0f) * 0.5f);
                        Vector3 euler2 = Vector3.Lerp(-element.Input2DSecondAxisOffsetAngle, element.Input2DSecondAxisOffsetAngle, (inputValueJoystick.y + 1.0f) * 0.5f);
                        element.ElementObject.transform.localRotation = Quaternion.Euler(euler2) * Quaternion.Euler(euler1) * element.InitialLocalRot;
                        contact                                       = contact || inputValueJoystick != Vector2.zero;
                        break;

                    case UxrElementType.Input2DTouch:

                        Vector2 inputValueTouch = controllerInput.GetInput2D(element.HandSide, UxrControllerInput.ControllerElementToInput2D(element.Element), true);

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                        }
                        else
                        {
                            inputValueTouch = controllerInput.GetInput2D(element.HandSide, UxrControllerInput.ControllerElementToInput2D(element.Element), true);
                        }

                        Vector3 offset1 = Vector3.Lerp(-element.Input2DFirstAxisOffset,  element.Input2DFirstAxisOffset,  (inputValueTouch.x + 1.0f) * 0.5f);
                        Vector3 offset2 = Vector3.Lerp(-element.Input2DSecondAxisOffset, element.Input2DSecondAxisOffset, (inputValueTouch.y + 1.0f) * 0.5f);

                        if (element.FingerContactPoint != null)
                        {
                            element.FingerContactPoint.transform.localPosition = element.FingerContactInitialLocalPos      +
                                                                                 element.LocalFingerPosOffsetX * offset1.x + element.LocalFingerPosOffsetY * offset1.y + element.LocalFingerPosOffsetZ * offset1.z +
                                                                                 element.LocalFingerPosOffsetX * offset2.x + element.LocalFingerPosOffsetY * offset2.y + element.LocalFingerPosOffsetZ * offset2.z;
                        }

                        contact = contact || inputValueTouch != Vector2.zero;
                        break;

                    case UxrElementType.DPad:

                        bool dpadLeft  = false;
                        bool dpadRight = false;
                        bool dpadUp    = false;
                        bool dpadDown  = false;

                        if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                        {
                        }
                        else
                        {
                            dpadLeft  = controllerInput.GetButtonsPress(element.HandSide, UxrInputButtons.DPadLeft,  true);
                            dpadRight = controllerInput.GetButtonsPress(element.HandSide, UxrInputButtons.DPadRight, true);
                            dpadUp    = controllerInput.GetButtonsPress(element.HandSide, UxrInputButtons.DPadUp,    true);
                            dpadDown  = controllerInput.GetButtonsPress(element.HandSide, UxrInputButtons.DPadDown,  true);
                        }

                        Vector3 dpadEuler1 = dpadLeft  ? -element.DpadFirstAxisOffsetAngle :
                                             dpadRight ? element.DpadFirstAxisOffsetAngle : Vector3.zero;
                        Vector3 dpadEuler2 = dpadUp   ? -element.DpadSecondAxisOffsetAngle :
                                             dpadDown ? element.DpadSecondAxisOffsetAngle : Vector3.zero;
                        Vector3 dpadOffset1 = dpadLeft  ? -element.DpadFirstAxisOffset :
                                              dpadRight ? element.DpadFirstAxisOffset : Vector3.zero;
                        Vector3 dpadOffset2 = dpadUp   ? -element.DpadSecondAxisOffset :
                                              dpadDown ? element.DpadSecondAxisOffset : Vector3.zero;

                        element.ElementObject.transform.localRotation = Quaternion.Euler(dpadEuler2) * Quaternion.Euler(dpadEuler1) * element.InitialLocalRot;

                        element.FingerContactPoint.transform.localPosition = element.FingerContactInitialLocalPos          +
                                                                             element.LocalFingerPosOffsetX * dpadOffset1.x +
                                                                             element.LocalFingerPosOffsetY * dpadOffset1.y +
                                                                             element.LocalFingerPosOffsetZ * dpadOffset1.z +
                                                                             element.LocalFingerPosOffsetX * dpadOffset2.x +
                                                                             element.LocalFingerPosOffsetY * dpadOffset2.y +
                                                                             element.LocalFingerPosOffsetZ * dpadOffset2.z;

                        contact = contact || dpadLeft || dpadRight || dpadUp || dpadDown;
                        break;

                    case UxrElementType.NotSet: break;
                }

                // Is there contact?

                contact = contact || (controllerButton != UxrInputButtons.None && (controllerInput.GetButtonsTouch(element.HandSide, controllerButton, true) || controllerInput.GetButtonsPress(element.HandSide, controllerButton, true)));

                if (onlyIfControllerHand && !IsControllerHandPresent(element.HandSide))
                {
                    contact = false;
                }

                if (!contact)
                {
                    continue;
                }

                // Write contacts

                if (!_needsBothHands)
                {
                    _fingerContacts[element.Finger] = i;
                }
                else
                {
                    switch (element.HandSide)
                    {
                        case UxrHandSide.Left:  _fingerContactsLeft[element.Finger]  = i; break;
                        case UxrHandSide.Right: _fingerContactsRight[element.Finger] = i; break;
                        default:                throw new ArgumentOutOfRangeException();
                    }
                }
            }

            // Look for changes and update contact targets with synchronization support

            foreach (UxrFingerType fingerType in _fingerTypeEnums)
            {
                if (!_needsBothHands)
                {
                    if (_lastFingerContacts[fingerType] != _fingerContacts[fingerType])
                    {
                        ChangeFingerContactTarget(-1, fingerType, _fingerContacts[fingerType]);
                    }
                }
                else
                {
                    if (_lastFingerContactsLeft[fingerType] == _fingerContactsLeft[fingerType])
                    {
                        ChangeFingerContactTarget((int)UxrHandSide.Left, fingerType, _fingerContactsLeft[fingerType]);
                    }

                    if (_lastFingerContactsRight[fingerType] == _fingerContactsRight[fingerType])
                    {
                        ChangeFingerContactTarget((int)UxrHandSide.Right, fingerType, _fingerContactsRight[fingerType]);
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the list of GameObjects that represent the given different controller input elements.
        /// </summary>
        /// <param name="elements">Flags representing the input elements to get the objects of</param>
        /// <returns>List of GameObjects representing the given controller input elements</returns>
        public IEnumerable<GameObject> GetElements(UxrControllerElements elements)
        {
            for (int i = 0; i < UxrControllerInput.SupportedControllerElements.Count; i++)
            {
                UxrControllerElements element = UxrControllerInput.SupportedControllerElements[i];
                if (elements.HasFlag(element) && _hashedElements.TryGetValue(element, out GameObject elementGameObject))
                {
                    yield return elementGameObject;
                }
            }
        }

        /// <summary>
        ///     Gets the list of materials of all objects that represent the given different controller input elements.
        /// </summary>
        /// <param name="elements">Flags representing the input elements to get the materials from</param>
        /// <returns>List of materials used by the objects representing the given controller input elements</returns>
        public IEnumerable<Material> GetElementsMaterials(UxrControllerElements elements)
        {
            for (int i = 0; i < UxrControllerInput.SupportedControllerElements.Count; i++)
            {
                UxrControllerElements element = UxrControllerInput.SupportedControllerElements[i];
                if (elements.HasFlag(element) && _hashedElements.TryGetValue(element, out GameObject elementGameObject))
                {
                    Renderer elementRenderer = elementGameObject.GetComponent<Renderer>();

                    if (elementRenderer != null && elementRenderer.material != null)
                    {
                        yield return elementRenderer.material;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets the list of original shared materials of all objects that represent the given different controller input
        ///     elements. The original materials are the shared materials that the input elements had at the beginning, before any
        ///     modifications.
        /// </summary>
        /// <param name="elements">Flags representing the input elements to get the original shared materials from</param>
        /// <returns>List of original shared materials used by the objects representing the given controller input elements</returns>
        public IEnumerable<Material> GetElementsOriginalMaterials(UxrControllerElements elements)
        {
            for (int i = 0; i < UxrControllerInput.SupportedControllerElements.Count; i++)
            {
                UxrControllerElements element = UxrControllerInput.SupportedControllerElements[i];
                if (elements.HasFlag(element) && _hashedElementsOriginalMaterial.TryGetValue(element, out Material elementMaterial))
                {
                    yield return elementMaterial;
                }
            }
        }

        /// <summary>
        ///     Changes the material of the objects that represent the given different controller input elements.
        /// </summary>
        /// <param name="elements">Flags representing the input elements whose materials will be changed</param>
        /// <param name="material">New material to assign</param>
        public void SetElementsMaterial(UxrControllerElements elements, Material material)
        {
            for (int i = 0; i < UxrControllerInput.SupportedControllerElements.Count; i++)
            {
                UxrControllerElements element = UxrControllerInput.SupportedControllerElements[i];
                if (elements.HasFlag(element) && _hashedElements.TryGetValue(element, out GameObject elementGameObject)
                                              && elementGameObject.TryGetComponent<Renderer>(out Renderer elementRenderer))
                {
                    elementRenderer.material = material;
                }
            }
        }

        /// <summary>
        ///     Restores the materials of the objects that represent the given different controller input elements.
        /// </summary>
        /// <param name="elements">Flags representing the input elements whose materials to restore</param>
        public void RestoreElementsMaterials(UxrControllerElements elements)
        {
            for (int i = 0; i < UxrControllerInput.SupportedControllerElements.Count; i++)
            {
                UxrControllerElements element = UxrControllerInput.SupportedControllerElements[i];
                if (elements.HasFlag(element) && _hashedElements.TryGetValue(element, out GameObject elementGameObject)
                                              && elementGameObject.TryGetComponent<Renderer>(out Renderer elementRenderer))
                {
                    elementRenderer.sharedMaterial = _hashedElementsOriginalMaterial[element];
                }
            }
        }

        /// <summary>
        ///     Changes the current hand to use the controller to the opposite side.
        /// </summary>
        public void SwitchHandedness()
        {
            if (_needsBothHands)
            {
                return;
            }

            _handSide = _handSide == UxrHandSide.Left ? UxrHandSide.Right : UxrHandSide.Left;

            foreach (UxrElement element in _controllerElements)
            {
                element.HandSide = element.HandSide == UxrHandSide.Left ? UxrHandSide.Right : UxrHandSide.Left;
            }
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the component.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            // Initialize data

            UniqueIdChanged += This_UniqueIdChanged;

            _fingerTypeEnums = Enum.GetValues(typeof(UxrFingerType)).Cast<UxrFingerType>().Where(fingerType => fingerType != UxrFingerType.None).ToArray();
            _avatar          = GetComponentInParent<UxrAvatar>();

            foreach (UxrFingerType fingerType in _fingerTypeEnums)
            {
                _lastFingerContacts.Add(fingerType, -1);
                _lastFingerContactsLeft.Add(fingerType, -1);
                _lastFingerContactsRight.Add(fingerType, -1);
                _fingerContacts.Add(fingerType, -1);
                _fingerContactsLeft.Add(fingerType, -1);
                _fingerContactsRight.Add(fingerType, -1);
            }

            for (int i = 0; i < _controllerElements.Count; i++)
            {
                UxrElement element = _controllerElements[i];
                if (element.ElementObject != null)
                {
                    // Initialize initial pos/rot

                    element.InitialLocalPos = element.ElementObject.transform.localPosition;
                    element.InitialLocalRot = element.ElementObject.transform.localRotation;

                    // Initialize original materials and hashed elements

                    if (_hashedElements.ContainsKey(element.Element))
                    {
                        //Debug.LogWarning($"Element {element.Element} was already found in the {nameof(UxrController3DModel)} list of {name}. Ignoring.");
                    }
                    else
                    {
                        // Element
                        _hashedElements.Add(element.Element, element.ElementObject);

                        // Original materials
                        Renderer componentRenderer = element.ElementObject.GetComponent<Renderer>();
                        _hashedElementsOriginalMaterial.Add(element.Element, componentRenderer != null ? componentRenderer.sharedMaterial : null);
                    }

                    element.LocalOffsetX = element.ElementObject.transform.parent.InverseTransformDirection(element.ElementObject.transform.right);
                    element.LocalOffsetY = element.ElementObject.transform.parent.InverseTransformDirection(element.ElementObject.transform.up);
                    element.LocalOffsetZ = element.ElementObject.transform.parent.InverseTransformDirection(element.ElementObject.transform.forward);

                    if (element.FingerContactPoint != null)
                    {
                        element.LocalFingerPosOffsetX = element.FingerContactPoint.transform.parent.InverseTransformDirection(element.ElementObject.transform.right);
                        element.LocalFingerPosOffsetY = element.FingerContactPoint.transform.parent.InverseTransformDirection(element.ElementObject.transform.up);
                        element.LocalFingerPosOffsetZ = element.FingerContactPoint.transform.parent.InverseTransformDirection(element.ElementObject.transform.forward);
                    }
                }

                if (element.ElementObject != null && element.FingerContactPoint != null)
                {
                    element.FingerContactInitialLocalPos = element.FingerContactPoint.transform.localPosition;

                    if (element.FingerContactPoint != element.ElementObject)
                    {
                        element.FingerContactPoint.SetActive(false);
                    }
                }

#if !ULTIMATEXR_NOSYNC

                if (element.ElementObject != null)
                {
                    UxrSyncObject syncObject = element.ElementObject.GetOrAddComponent<UxrSyncObject>();
                    syncObject.ChangeUniqueId(GuidExt.Combine(UniqueId, GetElementUniqueId(i)));
                    syncObject.SyncTransform                         = true;
                    syncObject.SyncTransformNetwork                  = true;
                    syncObject.TransformSpace                        = UxrTransformSpace.Local;
                    syncObject.OverrideDefaultNetSyncIntervalSeconds = true;
                    syncObject.NetSyncIntervalSecondsOverride        = 0.1f;
                }
#endif
            }
        }

        /// <summary>
        ///     Updates the fingers based on the current contact information.
        /// </summary>
        private void LateUpdate()
        {
            // Update fingers

            if (!_needsBothHands)
            {
                if (_controllerHand != null && _fingerContacts != null)
                {
                    foreach (KeyValuePair<UxrFingerType, int> fingerContactPair in _fingerContacts)
                    {
                        int elementIndex = fingerContactPair.Value;
                        _controllerHand.UpdateFinger(fingerContactPair.Key, elementIndex >= 0 ? _controllerElements[elementIndex].FingerContactPoint.transform : null);
                    }
                }
            }
            else
            {
                if (_controllerHandLeft != null && _fingerContactsLeft != null)
                {
                    foreach (KeyValuePair<UxrFingerType, int> fingerContactPair in _fingerContactsLeft)
                    {
                        int elementIndex = fingerContactPair.Value;
                        _controllerHandLeft.UpdateFinger(fingerContactPair.Key, elementIndex >= 0 ? _controllerElements[elementIndex].FingerContactPoint.transform : null);
                    }
                }

                if (_controllerHandRight != null && _fingerContactsRight != null)
                {
                    foreach (KeyValuePair<UxrFingerType, int> fingerContactPair in _fingerContactsRight)
                    {
                        int elementIndex = fingerContactPair.Value;
                        _controllerHandRight.UpdateFinger(fingerContactPair.Key, elementIndex >= 0 ? _controllerElements[elementIndex].FingerContactPoint.transform : null);
                    }
                }
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Handles the event triggered when the unique identifier of the object changes.
        /// </summary>
        /// <param name="oldId">The previous unique identifier.</param>
        /// <param name="newId">The new unique identifier.</param>
        private void This_UniqueIdChanged(Guid oldId, Guid newId)
        {
#if !ULTIMATEXR_NOSYNC
            for (int i = 0; i < _controllerElements.Count; i++)
            {
                UxrElement element = _controllerElements[i];

                if (element.ElementObject != null)
                {
                    UxrSyncObject syncObject = element.ElementObject.GetComponent<UxrSyncObject>();

                    if (syncObject != null)
                    {
                        syncObject.ChangeUniqueId(GuidExt.Combine(newId, GetElementUniqueId(i)));
                    }
                }
            }
#endif
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Builds a unique identifier for a controller element.
        /// </summary>
        /// <param name="elementIndex">Controller element index</param>
        /// <returns>Unique ID</returns>
        private Guid GetElementUniqueId(int elementIndex)
        {
            return ("UxrController3D element " + elementIndex).GetGuid();
        }

        /// <summary>
        ///     Changes the contact target of a finger holding the controller.
        /// </summary>
        /// <param name="hand">
        ///     The hand to change the contact state for. Use -1 for the target hand when the controller can only be grabbed with
        ///     one hand, or 0 (left) 1 (right) when the controller can be grabbed using both hands.
        /// </param>
        /// <param name="fingerType">
        ///     The type of finger to change the contact state for.
        /// </param>
        /// <param name="targetElementIndex">
        ///     The index of the controller element that the finger is contacting or -1 for no contact (release).
        /// </param>
        private void ChangeFingerContactTarget(int hand, UxrFingerType fingerType, int targetElementIndex)
        {
            UxrElement oldElement = _fingerContacts[fingerType] != -1 ? _controllerElements[_fingerContacts[fingerType]] : null;
            UxrElement newElement = targetElementIndex          != -1 ? _controllerElements[targetElementIndex] : null;

            BeginSync();

            // Update finger contact point transform

            if (hand == -1)
            {
                _fingerContacts[fingerType] = targetElementIndex;
            }
            else
            {
                switch (hand)
                {
                    case (int)UxrHandSide.Left:  _fingerContactsLeft[fingerType]  = targetElementIndex; break;
                    case (int)UxrHandSide.Right: _fingerContactsRight[fingerType] = targetElementIndex; break;

                    default: throw new ArgumentOutOfRangeException();
                }
            }

            // Set the contact point visible state if the contact point is specified and different from the element itself.

            if (oldElement != null && oldElement.FingerContactPoint != null && oldElement.FingerContactPoint != oldElement.ElementObject)
            {
                oldElement.FingerContactPoint.SetActive(false);
            }

            if (newElement != null && newElement.FingerContactPoint != null && newElement.FingerContactPoint != newElement.ElementObject)
            {
                bool handVisible = _controllerHand && _controllerHand.gameObject.activeSelf;

                if (_needsBothHands)
                {
                    handVisible = (newElement.HandSide == UxrHandSide.Left  && _controllerHandLeft  != null && _controllerHandLeft.gameObject.activeSelf) ||
                                  (newElement.HandSide == UxrHandSide.Right && _controllerHandRight != null && _controllerHandRight.gameObject.activeSelf);
                }

                newElement.FingerContactPoint.SetActive(targetElementIndex != -1 && !handVisible);
            }

            EndSyncMethod(SyncParams(hand, fingerType, targetElementIndex));
        }

        /// <summary>
        ///     Gets whether the component has a visual hand available for visualization.
        /// </summary>
        /// <param name="handSide">Hand to check for</param>
        /// <returns>Whether there is a visual hand available</returns>
        private bool IsControllerHandPresent(UxrHandSide handSide)
        {
            if (_needsBothHands)
            {
                return handSide == UxrHandSide.Left ? _controllerHandLeft != null : _controllerHandRight != null;
            }

            return _controllerHand != null;
        }

        /// <summary>
        ///     Resets the controller to a state where it's not being interacted with.
        /// </summary>
        private void ResetInteraction()
        {
            foreach (UxrFingerType fingerType in _fingerTypeEnums)
            {
                _lastFingerContacts[fingerType]      = -1;
                _lastFingerContactsLeft[fingerType]  = -1;
                _lastFingerContactsRight[fingerType] = -1;
                _fingerContacts[fingerType]          = -1;
                _fingerContactsLeft[fingerType]      = -1;
                _fingerContactsRight[fingerType]     = -1;
            }

            foreach (UxrElement element in _controllerElements)
            {
                if (element.ElementObject == null)
                {
                    continue;
                }

                // Restore initial pos/rot

                element.ElementObject.transform.localPosition = element.InitialLocalPos;
                element.ElementObject.transform.localRotation = element.InitialLocalRot;

                // Restore original material

                if (_hashedElements.ContainsKey(element.Element))
                {
                    Renderer componentRenderer = element.ElementObject.GetComponent<Renderer>();
                    if (componentRenderer != null && _hashedElementsOriginalMaterial.TryGetValue(element.Element, out Material originalMaterial))
                    {
                        componentRenderer.sharedMaterial = originalMaterial;
                    }
                }

                // Disable visual contact object if it exists

                if (element.FingerContactPoint != null)
                {
                    element.FingerContactPoint.transform.localPosition = element.FingerContactInitialLocalPos;

                    if (element.FingerContactPoint != element.ElementObject)
                    {
                        element.FingerContactPoint.SetActive(false);
                    }
                }
            }
        }

        #endregion

        #region Private Types & Data

        private readonly Dictionary<UxrControllerElements, GameObject> _hashedElements                 = new Dictionary<UxrControllerElements, GameObject>();
        private readonly Dictionary<UxrControllerElements, Material>   _hashedElementsOriginalMaterial = new Dictionary<UxrControllerElements, Material>();
        private readonly Dictionary<UxrFingerType, int>                _lastFingerContacts             = new Dictionary<UxrFingerType, int>();
        private readonly Dictionary<UxrFingerType, int>                _lastFingerContactsLeft         = new Dictionary<UxrFingerType, int>();
        private readonly Dictionary<UxrFingerType, int>                _lastFingerContactsRight        = new Dictionary<UxrFingerType, int>();
        private          Dictionary<UxrFingerType, int>                _fingerContacts                 = new Dictionary<UxrFingerType, int>();
        private          Dictionary<UxrFingerType, int>                _fingerContactsLeft             = new Dictionary<UxrFingerType, int>();
        private          Dictionary<UxrFingerType, int>                _fingerContactsRight            = new Dictionary<UxrFingerType, int>();

        private UxrFingerType[] _fingerTypeEnums;

        private UxrAvatar _avatar;
        private bool      _isControllerVisible = true;
        private bool      _isHandVisible       = true;

        #endregion
    }
}