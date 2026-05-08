// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrDontSyncAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Core.Unique
{
    /// <summary>
    ///     Attribute that marks a component type so that it is excluded from UltimateXR state
    ///     persistence and synchronization systems.
    /// </summary>
    /// <remarks>
    ///     When applied to a component, it prevents the type from participating in:
    ///     <list type="bullet">
    ///         <item>
    ///             StateSave (component serialize/deserialize).
    ///         </item>
    ///         <item>
    ///             StateSync (property/method synchronization).
    ///         </item>
    ///     </list>
    ///     
    ///     Unlike <see cref="UxrDontRegisterAttribute"/>, the component is still included in static
    ///     enumeration properties such as <c>AllComponents</c> and <c>EnabledComponents</c>.
    ///     This attribute is particularly useful for:
    ///     <list type="bullet">
    ///         <item>
    ///             Components containing transient or runtime-only data that should not be persisted.
    ///         </item>
    ///         <item>
    ///             Components whose state is managed manually or through custom synchronization logic.
    ///         </item>
    ///         <item>
    ///             Visual, helper, or secondary systems that do not represent authoritative state.
    ///         </item>
    ///     </list>
    ///     
    ///     The attribute is evaluated at the type level and typically cached internally to avoid
    ///     repeated reflection costs during runtime.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class UxrDontSyncAttribute : Attribute
    {
    }
}
