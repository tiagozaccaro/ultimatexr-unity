// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrConstants.InputControllers.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Core
{
    public partial class UxrConstants
    {
        #region Public Types & Data

        public static class InputControllers
        {
            #region Public Types & Data

            /// <summary>
            ///     Controls the string that appears in controller names when using OpenXR.
            /// </summary>
            public const string OpenXR = "OpenXR";

            /// <summary>
            ///     Controls the duration of each haptic sample sent, and thus the iteration of each loop.
            /// </summary>
            public const float HapticSampleDurationSeconds = 0.1f;

            #endregion
        }

        #endregion
    }
}