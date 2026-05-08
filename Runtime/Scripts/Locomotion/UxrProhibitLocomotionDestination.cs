// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrProhibitLocomotionDestination.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Components;

namespace UltimateXR.Locomotion
{
    /// <summary>
    ///     Marks all objects in this hierarchy and below as invalid locomotion destinations,
    ///     preventing the avatar from moving to them.
    /// </summary>
    /// <remarks>
    ///     A limitation of this component is that, when using
    ///     <see cref="UxrSmoothLocomotion" />, it does not prevent moving onto an object with
    ///     <see cref="UxrProhibitLocomotionDestination" /> when transitioning between adjacent
    ///     floor-level surfaces, stepping down onto it, or falling onto it. However, movement that
    ///     requires stepping up onto an object with this component is correctly prevented.
    ///     This behavior is intentional, as blocking all such cases could leave the avatar immobile
    ///     if it ends up inside an area with this component.
    ///     To reliably prevent movement onto these objects when using
    ///     <see cref="UxrSmoothLocomotion" />, it is recommended to create invisible barriers using colliders.
    /// </remarks>
    public class UxrProhibitLocomotionDestination : UxrComponent
    {
    }
}