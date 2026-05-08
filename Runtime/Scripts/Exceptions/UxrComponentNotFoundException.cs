// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrComponentNotFoundException.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Core.Unique;

namespace UltimateXR.Exceptions
{
    /// <summary>
    ///     UltimateXR Component not found exception. This exception is normally thrown when a method that uses
    ///     <see cref="UxrUniqueIdImplementer.TryGetComponentById" /> could not find the component.
    ///     <see cref="UxrUniqueIdImplementer.TryGetComponentById" /> itself doesn't throw any exception.
    /// </summary>
    public sealed class UxrComponentNotFoundException : Exception
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the component's unique ID that could not be found.
        /// </summary>
        public Guid UniqueId { get; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the component that was going to be retrieved</param>
        /// <param name="debugInfo">Debug info (if available)</param>
        public UxrComponentNotFoundException(Guid uniqueId, string debugInfo = null) : base(FormatMessage(uniqueId, debugInfo))
        {
            UniqueId = uniqueId;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets a formatted exception message.
        /// </summary>
        /// <param name="uniqueId">The unique ID of the component that was going to be retrieved</param>
        /// <param name="debugInfo">Debug info (if available)</param>
        /// <returns>Exception message</returns>
        private static string FormatMessage(Guid uniqueId, string debugInfo)
        {
            string formattedDebugInfo = string.Empty;

            // If no debug information was included in the exception, try to get it using the debug info providers.

            if (debugInfo == null)
            {
                UxrUniqueIdImplementer.TryGetUniqueIdComponentDebugInfo(uniqueId, out debugInfo);
            }
            
            // Format the message.

            if (!string.IsNullOrEmpty(debugInfo))
            {
                return $"Could not find component with UniqueId {(uniqueId == Guid.Empty ? "empty" : uniqueId)}. Component should be: {debugInfo}.";
            }

            return $"Could not find component with UniqueId {(uniqueId == Guid.Empty ? "empty" : uniqueId)}. Consider enabling debug information (menu Tools->UltimateXR->Global Settings) during development to have this message print the name of the object/component that could not be found.";                
        }

        #endregion
    }
}