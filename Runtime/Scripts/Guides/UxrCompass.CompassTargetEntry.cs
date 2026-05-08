// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrCompass.CompassTargetEntry.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Guides
{
    public partial class UxrCompass
    {
        #region Private Types & Data

        /// <summary>
        ///     Represents a target entry used by the compass to guide the user towards a specific Transform or world position.
        /// </summary>
        private class CompassTargetEntry
        {
            #region Public Types & Data

            /// <summary>
            ///     Gets whether this entry has a target.
            /// </summary>
            public bool HasTarget => TargetTransform != null || RawPosition.HasValue;

            #endregion

            #region Public Types & Data

            public Transform             TargetTransform;
            public UxrCompassTargetHint  TargetHint;
            public Vector3?              RawPosition;
            public UxrCompassDisplayMode DisplayMode;
            public float                 IconScale = 1.0f;
            public bool                  IsTemporary;
            public float                 StartTime;
            public float                 OnScreenStartTime;

            #endregion
        }

        #endregion
    }
}