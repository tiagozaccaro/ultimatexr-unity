// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrVarInterpolator.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Animation.Interpolation
{
    /// <summary>
    ///     Base for interpolator classes that interpolate with optional smooth damping.
    ///     Child classes provide interpolation for different variable types.
    ///     Supports a single step and a two-step interpolation approach:
    ///     <list type="bullet">
    ///         <item>Step 1: <see cref="SetTarget" /> computes the raw interpolation target from frame data.</item>
    ///         <item>
    ///             Step 2: <see cref="ApplySmoothDampPostProcess" /> runs every frame to smoothly converge
    ///             toward the target. This ensures stable smoothing when <see cref="SetTarget" />
    ///             is invoked multiple times per frame or skipped on certain frames.
    ///         </item>
    ///     </list>
    /// </summary>
    public abstract class UxrVarInterpolator
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets whether to always return the first variable when interpolating.
        /// </summary>
        public bool UseStep { get; set; }

        /// <summary>
        ///     Gets or sets the smoothing value [0.0, 1.0]. 0 means no smoothing.
        /// </summary>
        public float SmoothDamp
        {
            get => _smoothDamp;
            set => _smoothDamp = Mathf.Clamp01(value);
        }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="smoothDamp">Smooth damp value [0.0, 1.0]</param>
        /// <param name="useStep">Whether to use step interpolation, where the interpolation will always return the start value</param>
        protected UxrVarInterpolator(float smoothDamp = 0.0f, bool useStep = false)
        {
            SmoothDamp = smoothDamp;
            UseStep    = useStep;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Interpolates between 2 values.
        /// </summary>
        /// <param name="a">Start value</param>
        /// <param name="b">End value</param>
        /// <param name="t">Interpolation factor [0.0, 1.0]</param>
        /// <returns>Interpolated value</returns>
        /// <remarks>
        ///     The interpolated value will be affected by smoothing if the object was initialized with a smoothDamp value
        ///     greater than 0.
        /// </remarks>
        public abstract object Interpolate(object a, object b, float t);

        /// <summary>
        ///     Step 1 of two-step interpolation: Computes the raw interpolation target from frame data
        ///     without applying smooth damp. The result is stored internally as the current target.
        /// </summary>
        /// <param name="a">Start value</param>
        /// <param name="b">End value</param>
        /// <param name="t">Interpolation factor [0.0, 1.0]</param>
        /// <returns>The raw interpolated target value (no smooth damp applied)</returns>
        public abstract object SetTarget(object a, object b, float t);

        /// <summary>
        ///     Step 2 of two-step interpolation: Applies smooth damp convergence from the last value
        ///     toward the current target. Should be called every frame from the post-process step
        ///     to ensure smooth motion even when no new frame data is available.
        /// </summary>
        /// <param name="deltaTime">Time that has elapsed since the last call</param>
        /// <returns>The smooth-damped value</returns>
        public abstract object ApplySmoothDampPostProcess(float deltaTime);

        /// <summary>
        ///     Resets the "memory" of the smooth damp effect, so that the interpolation will restart from the next time
        ///     <see cref="Interpolate" /> is called.
        /// </summary>
        public void RestartSmoothDamp()
        {
            RequiresSmoothDampRestart = true;
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Clears the smooth damp restart variable.
        /// </summary>
        protected void ClearSmoothDampRestart()
        {
            RequiresSmoothDampRestart = false;
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Gets whether the smooth damp needs to be restarted the next time <see cref="Interpolate" /> is called.
        /// </summary>
        protected bool RequiresSmoothDampRestart { get; private set; } = true;

        #endregion

        #region Private Types & Data

        private float _smoothDamp;

        #endregion
    }
}