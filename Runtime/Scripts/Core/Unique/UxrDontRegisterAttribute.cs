// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrDontRegisterAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Core.Unique
{
    /// <summary>
    ///     Attribute that marks a component type so that it is ignored by UltimateXR internal registration systems.
    /// </summary>
    /// <remarks>
    ///     When applied to a component, it prevents the type from being included in any automatic registration
    ///     module used by UltimateXR, including (but not limited to):
    ///     <list type="bullet">
    ///         <item>
    ///             StateSave (component serialize/deserialize).
    ///         </item>
    ///         <item>
    ///             StateSync (property/method synchronization).
    ///         </item>
    ///         <item>
    ///             Static enumeration properties such as AllComponents/EnabledComponents.
    ///         </item>
    ///     </list>
    ///     This attribute is particularly useful for components that should not be synchronized or serialized, while still
    ///     remaining discoverable through component enumeration.<br />
    ///     The attribute is evaluated at the type level and typically cached internally to avoid repeated
    ///     reflection costs during runtime.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class UxrDontRegisterAttribute : Attribute
    {
    }
}