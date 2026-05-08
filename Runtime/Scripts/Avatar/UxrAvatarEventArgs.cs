// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Events;

namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Event arguments for avatar-related events.
    /// </summary>
    /// <remarks>
    ///     This event uses <see cref="UxrPooledEventArgs{T}" /> to avoid allocations. Instances are pooled and only guaranteed
    ///     to be valid during the event invocation. Do not store or reuse them outside the handler scope.
    ///     Although instances may remain unchanged briefly depending on pool usage, this behavior is not guaranteed.
    /// </remarks>
    public sealed class UxrAvatarEventArgs : UxrAvatarEventArgs<UxrAvatarEventArgs>
    {
        #region Constructors & Finalizer

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Instances should not be created directly. Use GetFromPool to retrieve a pooled instance.
        /// </remarks>
        public UxrAvatarEventArgs()
        {
        }

        #endregion
    }
}