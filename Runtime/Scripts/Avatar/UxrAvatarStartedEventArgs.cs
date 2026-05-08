// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarStartedEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Events;

namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Arguments for the avatar-started event.
    /// </summary>
    /// <remarks>
    ///     This event uses <see cref="UxrPooledEventArgs{T}" /> to avoid allocations. Instances are pooled and only guaranteed
    ///     to be valid during the event invocation. Do not store or reuse them outside the handler scope.
    ///     Although instances may remain unchanged briefly depending on pool usage, this behavior is not guaranteed.
    /// </remarks>
    public class UxrAvatarStartedEventArgs : UxrAvatarEventArgs<UxrAvatarStartedEventArgs>
    {
        #region Constructors & Finalizer

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Instances should not be created directly. Use
        ///     <see cref="UxrAvatarEventArgs{T}.GetFromPool(UltimateXR.Avatar.UxrAvatar)" /> to retrieve a pooled instance.
        /// </remarks>
        public UxrAvatarStartedEventArgs()
        {
        }

        #endregion
    }
}