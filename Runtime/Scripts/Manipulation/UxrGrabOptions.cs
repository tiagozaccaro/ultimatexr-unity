// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGrabOptions.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Manipulation
{
    /// <summary>
    ///     Enumerates the different options when using
    ///     <see
    ///         cref="UxrGrabManager.GrabObject(UltimateXR.Manipulation.UxrGrabber,UltimateXR.Manipulation.UxrGrabbableObject,int,bool)" />
    ///     .
    /// </summary>
    [Flags]
    public enum UxrGrabOptions
    {
        /// <summary>
        ///     No options.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Will check each frame whether the object got out of reach due to the object being constrained, or because it was
        ///     grabbed procedurally from a distance that is too far. The distance is controlled by
        ///     <see cref="UxrGrabbableObject.LockedGrabReleaseDistance" />.
        /// </summary>
        CheckMaxGrabDistance = 1 << 0,

        /// <summary>
        ///     Use all options.
        /// </summary>
        All = 0x7FFFFFFF
    }
}