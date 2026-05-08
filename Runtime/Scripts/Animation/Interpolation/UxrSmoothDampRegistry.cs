// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothDampRegistry.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Core.StateSave;

namespace UltimateXR.Animation.Interpolation
{
    /// <summary>
    ///     Registry that tracks all active smooth-damp interpolators during replay playback.
    ///     <list type="bullet">
    ///         <item>
    ///             Step 1 (<see cref="Register" />) is called from the interpolation code when a variable has new frame data
    ///             and <see cref="UxrVarInterpolator.SetTarget" /> was called on its interpolator.
    ///         </item>
    ///         <item>
    ///             Step 2 (<see cref="ProcessAll" />) is called every frame from the playback frame ended event to apply
    ///             smooth damp convergence on all active interpolators, ensuring smooth motion even when no new frame data
    ///             is available.
    ///         </item>
    ///     </list>
    /// </summary>
    public static partial class UxrSmoothDampRegistry
    {
        #region Public Methods

        /// <summary>
        ///     Registers or updates an interpolator entry.
        /// </summary>
        /// <param name="component">The state-save component that owns the variable</param>
        /// <param name="varName">The variable name that uniquely identifies the interpolator within the component</param>
        /// <param name="interpolator">The interpolator performing smooth damp</param>
        /// <param name="applyResult">Action to apply the smooth-damped result back (e.g., set position on a transform)</param>
        public static void Register(IUxrStateSave component, string varName, UxrVarInterpolator interpolator, Action<object> applyResult)
        {
            var key = (component, varName);

            if (s_active.TryGetValue(key, out var entry))
            {
                // Already tracked. Reset idle counter since we got new data.
                entry.IdleFrameCount = 0;
                entry.ApplyResult    = applyResult;
            }
            else
            {
                s_active[key] = new ActiveEntry
                                {
                                    Interpolator   = interpolator,
                                    ApplyResult    = applyResult,
                                    IdleFrameCount = 0
                                };
            }
        }

        /// <summary>
        ///     Runs smooth damp on all active interpolators and applies results.
        ///     Called every frame from the playback frame ended event.
        ///     Releases interpolators that have been idle (no new target data) for enough frames.
        /// </summary>
        /// <param name="deltaTime">Time elapsed since the last frame</param>
        public static void ProcessAll(float deltaTime)
        {
            s_toRemove.Clear();

            foreach (var kvp in s_active)
            {
                var entry  = kvp.Value;
                var result = entry.Interpolator.ApplySmoothDampPostProcess(deltaTime);

                entry.ApplyResult?.Invoke(result);
                entry.IdleFrameCount++;

                if (entry.IdleFrameCount >= ReleaseAfterIdleFrames)
                {
                    s_toRemove.Add(kvp.Key);
                }
            }

            for (int i = 0; i < s_toRemove.Count; i++)
            {
                s_active.Remove(s_toRemove[i]);
            }
        }

        /// <summary>
        ///     Clears all tracked interpolators and restarts their smooth damp.
        ///     Should be called on seek or playback stop to avoid stale values chasing new targets.
        /// </summary>
        public static void Clear()
        {
            foreach (var kvp in s_active)
            {
                kvp.Value.Interpolator.RestartSmoothDamp();
            }

            s_active.Clear();
        }

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Number of frames without new target data after which an interpolator is released from the registry.
        ///     For SmoothDamp=0.3 with exponential decay at 72fps, 99% convergence takes ~26 frames.
        ///     Using 60 frames (~1 second) provides a generous margin for any SmoothDamp value.
        /// </summary>
        private const int ReleaseAfterIdleFrames = 60;

        private static readonly Dictionary<(IUxrStateSave, string), ActiveEntry> s_active   = new Dictionary<(IUxrStateSave, string), ActiveEntry>();
        private static readonly List<(IUxrStateSave, string)>                    s_toRemove = new List<(IUxrStateSave, string)>();

        #endregion
    }
}