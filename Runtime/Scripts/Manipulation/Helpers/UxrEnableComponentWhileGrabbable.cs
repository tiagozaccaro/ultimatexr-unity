// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrEnableComponentWhileGrabbable.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UltimateXR.Core.Settings;
using UnityEngine;
using UnityEngine.AI;

namespace UltimateXR.Manipulation.Helpers
{
    /// <summary>
    ///     Enables a target component while the associated <see cref="UxrGrabbableObject" /> is in a grabbable state.
    ///     The component will be enabled if both of the following conditions are met:
    ///     <list type="bullet">
    ///         <item>- <see cref="UxrGrabbableObject.enabled" /> is <c>true</c></item>
    ///         <item>- <see cref="UxrGrabbableObject.IsGrabbable" /> returns <c>true</c></item>
    ///     </list>
    ///     This can be used to control rendering, physics, animation, or logic behaviors dynamically when an object
    ///     is interactable in the scene.
    ///     <para>
    ///         The following Unity component types are supported and will be toggled using their built-in <c>enabled</c>
    ///         property:
    ///     </para>
    ///     <list type="bullet">
    ///         <item>
    ///             <see cref="Collider" />
    ///         </item>
    ///         <item>
    ///             <see cref="Renderer" />
    ///         </item>
    ///         <item>
    ///             <see cref="Camera" />
    ///         </item>
    ///         <item>
    ///             <see cref="Light" />
    ///         </item>
    ///         <item>
    ///             <see cref="Animator" />
    ///         </item>
    ///         <item>
    ///             <see cref="AudioSource" />
    ///         </item>
    ///         <item>
    ///             <see cref="NavMeshAgent" />
    ///         </item>
    ///         <item>Any <see cref="Behaviour" /> (e.g., MonoBehaviours)</item>
    ///     </list>
    ///     <para>
    ///         Note: <see cref="ParticleSystem" /> and other components that do not expose an <c>enabled</c> property are not
    ///         supported.
    ///     </para>
    /// </summary>
    public class UxrEnableComponentWhileGrabbable : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private UxrGrabbableObject _grabbableObject;
        [SerializeField] private Component          _targetComponent;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets or sets the grabbable object.
        /// </summary>
        public UxrGrabbableObject GrabbableObject
        {
            get => _grabbableObject;
            set => _grabbableObject = value;
        }

        /// <summary>
        ///     Gets or sets the target component.
        /// </summary>
        public Component TargetComponent
        {
            get => _targetComponent;
            set
            {
                _targetComponent = value;
                UpdateEnabledAssigner();
            }
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the enabled assigner.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            UpdateEnabledAssigner();
        }

        /// <summary>
        ///     Updates the component each frame.
        /// </summary>
        private void Update()
        {
            _enabledAssigner?.Invoke(_grabbableObject != null && _grabbableObject.enabled && _grabbableObject.IsGrabbable);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the _enabledAssigner delegate based on the component type. This delegate will avoid runtime checking per
        ///     frame.
        /// </summary>
        private void UpdateEnabledAssigner()
        {
            _enabledAssigner = null;

            if (_targetComponent == null)
            {
                return;
            }

            switch (_targetComponent)
            {
                case Collider c:         _enabledAssigner = b => c.enabled     = b; break;
                case Renderer r:         _enabledAssigner = b => r.enabled     = b; break;
                case Animator a:         _enabledAssigner = b => a.enabled     = b; break;
                case AudioSource aud:    _enabledAssigner = b => aud.enabled   = b; break;
                case Light li:           _enabledAssigner = b => li.enabled    = b; break;
                case Camera cam:         _enabledAssigner = b => cam.enabled   = b; break;
                case NavMeshAgent agent: _enabledAssigner = b => agent.enabled = b; break;

                case Behaviour behaviour:
                    // fallback for any other Behaviour not listed above
                    _enabledAssigner = b => behaviour.enabled = b; break;

                default:
                    if (UxrGlobalSettings.Instance.LogLevelManipulation >= UxrLogLevel.Errors)
                    {
                        Debug.LogError($"{UxrConstants.ManipulationModule} {nameof(UxrEnableComponentWhileGrabbable)} does not support type {_targetComponent.GetType().Name}. Does it have an enabled property?");
                    }
                    break;
            }
        }

        #endregion

        #region Private Types & Data

        private Action<bool> _enabledAssigner;

        #endregion
    }
}