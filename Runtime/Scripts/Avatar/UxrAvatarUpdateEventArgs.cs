// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarUpdateEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;
using UltimateXR.Core.Events;

namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Contains information about an avatar update event.
    /// </summary>
    /// <remarks>
    ///     This event uses <see cref="UxrPooledEventArgs{T}" /> to avoid allocations. Instances are pooled and only guaranteed
    ///     to be valid during the event invocation. Do not store or reuse them outside the handler scope.
    ///     Although instances may remain unchanged briefly depending on pool usage, this behavior is not guaranteed.
    /// </remarks>
    public class UxrAvatarUpdateEventArgs : UxrAvatarEventArgs<UxrAvatarUpdateEventArgs>
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the update stage the update event belongs to.
        /// </summary>
        public UxrUpdateStage UpdateStage { get; private set; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Default constructor.
        /// </summary>
        /// <remarks>
        ///     Instances should not be created directly. Use <see cref="GetFromPool" /> to retrieve a pooled instance.
        /// </remarks>
        public UxrAvatarUpdateEventArgs()
        {
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Returns an instance from the pool.
        /// </summary>
        /// <param name="avatar">Avatar the event describes</param>
        /// <param name="updateStage">Update stage the event belongs to</param>
        /// <returns>Instance from the pool</returns>
        public static UxrAvatarUpdateEventArgs GetFromPool(UxrAvatar avatar, UxrUpdateStage updateStage)
        {
            UxrAvatarUpdateEventArgs e = GetFromPool(avatar);
            e.UpdateStage = updateStage;
            return e;
        }

        #endregion
    }
}