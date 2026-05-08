// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IUxrNetworkAvatar.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Avatar;
using UnityEngine;

namespace UltimateXR.Networking
{
    /// <summary>
    ///     Interface for network avatar components. Network avatar components are responsible for setting the avatar in the
    ///     correct mode (local/external) and sending/receiving global component state changes.
    /// </summary>
    public interface IUxrNetworkAvatar
    {
        #region Public Types & Data

        /// <summary>
        ///     Event called right after the avatar was spawned.
        /// </summary>
        event Action AvatarSpawned;

        /// <summary>
        ///     Event called right after the avatar was despawned.
        /// </summary>
        event Action AvatarDespawned;

        /// <summary>
        ///     Gets whether the avatar networking component was initialized and its properties are set.
        /// </summary>
        public bool IsInitialized { get; }

        /// <summary>
        ///     Gets whether this avatar is the avatar controller by the user (true) or a remote avatar (false).
        /// </summary>
        bool IsLocal { get; }

        /// <summary>
        ///     Gets the avatar component.
        /// </summary>
        UxrAvatar Avatar { get; }

        /// <summary>
        ///     Gets whether the implementation uses dummy network transforms to synchronize avatar transforms.
        /// </summary>
        bool UsesDummyNetworkTransforms { get; }

        /// <summary>
        ///     Gets or sets the avatar name.
        /// </summary>
        string AvatarName { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Changes the parent of the specified child Transform to the specified parent Transform.
        /// </summary>
        /// <param name="child">The Transform of the child GameObject to reparent</param>
        /// <param name="parent">The Transform of the target parent GameObject</param>
        /// <returns>Returns true if the parent was successfully changed. Otherwise, false.</returns>
        bool ChangeParent(Transform child, Transform parent);

        #endregion
    }
}