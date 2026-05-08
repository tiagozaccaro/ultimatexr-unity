// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarEventArgs_1.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core.Events;

namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Base event arguments for avatar-related events.
    /// </summary>
    /// <remarks>
    ///     This event uses <see cref="UxrPooledEventArgs{T}" /> to avoid allocations. Instances are pooled and only guaranteed
    ///     to be valid during the event invocation. Do not store or reuse them outside the handler scope.
    ///     Although instances may remain unchanged briefly depending on pool usage, this behavior is not guaranteed.
    /// </remarks>
    public abstract class UxrAvatarEventArgs<T> : UxrPooledEventArgs<T> where T : UxrAvatarEventArgs<T>, new()
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the Avatar the event belongs to.
        /// </summary>
        public UxrAvatar Avatar { get; private set; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Instances should not be created directly. Use <see cref="GetFromPool" /> to retrieve a pooled instance.
        /// </remarks>
        protected UxrAvatarEventArgs()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Retrieves an instance of <see cref="UxrAvatarEventArgs" /> from the event pool and assigns the specified avatar.
        /// </summary>
        /// <param name="avatar">The avatar to associate with the event arguments instance.</param>
        /// <returns>
        ///     An instance of <see cref="UxrAvatarEventArgs" /> retrieved from the pool and initialized with the specified
        ///     avatar.
        /// </returns>
        public static T GetFromPool(UxrAvatar avatar)
        {
            T e = GetFromPool();
            e.Avatar = avatar;
            return e;
        }

        #endregion
    }
}