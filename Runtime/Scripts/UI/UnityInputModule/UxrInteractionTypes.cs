// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrInteractionTypes.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Avatar;

namespace UltimateXR.UI.UnityInputModule
{
    /// <summary>
    ///     Enumerates the types of interaction supported. Flags can be combined to support multiple interaction types
    ///     simultaneously.
    /// </summary>
    [Flags]
    public enum UxrInteractionTypes
    {
        /// <summary>
        ///     Interaction using <see cref="UxrFingerTip" /> components attached to the finger tips of an <see cref="UxrAvatar" />
        ///     . Enables touch interaction.
        /// </summary>
        FingerTips = 1,

        /// <summary>
        ///     Interaction using <see cref="UxrLaserPointer" /> components from a distance.
        /// </summary>
        LaserPointers = 1 << 1
    }
}
