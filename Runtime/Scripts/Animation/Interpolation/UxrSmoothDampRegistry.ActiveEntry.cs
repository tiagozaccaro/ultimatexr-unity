// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSmoothDampRegistry.ActiveEntry.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Animation.Interpolation
{
    public static partial class UxrSmoothDampRegistry
    {
        #region Private Types & Data

        /// <summary>
        ///     Represents a single interpolator entry being tracked in the registry.
        /// </summary>
        private class ActiveEntry
        {
            #region Public Types & Data

            /// <summary>The interpolator performing smooth damp.</summary>
            public UxrVarInterpolator Interpolator { get; set; }

            /// <summary>Action to apply the smooth-damped result back (e.g., set position on a transform).</summary>
            public Action<object> ApplyResult { get; set; }

            /// <summary>Number of frames since the last <see cref="Register" /> call for this entry.</summary>
            public int IdleFrameCount { get; set; }

            #endregion
        }

        #endregion
    }
}