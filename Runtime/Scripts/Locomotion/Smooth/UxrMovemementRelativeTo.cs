// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrMovemementRelativeTo.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Locomotion.Smooth
{
    /// <summary>
    ///     Defines the reference point relative to which translation movement is calculated.
    /// </summary>
    public enum UxrMovemementRelativeTo
    {
        /// <summary>
        ///     Movement is relative to the avatar's head. The avatar will move towards where the head is pointing.
        /// </summary>
        Head,

        /// <summary>
        ///     Movement is relative to the avatar's left controller. The avatar will move towards where the left controller is
        ///     pointing.
        /// </summary>
        LeftHand,

        /// <summary>
        ///     Movement is relative to the avatar's right controller. The avatar will move towards where the right controller is
        ///     pointing.
        /// </summary>
        RightHand
    }
}