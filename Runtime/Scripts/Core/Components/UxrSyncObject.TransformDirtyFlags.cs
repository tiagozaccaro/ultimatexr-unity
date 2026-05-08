// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSyncObject.TransformDirtyFlags.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;

namespace UltimateXR.Core.Components
{
    public partial class UxrSyncObject
    {
        #region Private Types & Data

        /// <summary>
        ///     Represents the state of a Transform object that might have been modified from the last network
        ///     synchronization or internal state update. It is used to determine which aspects of a Transform
        ///     (position, rotation, or scale) require changes to be serialized or propagated.
        /// </summary>
        [Flags]
        private enum TransformDirtyFlags : byte
        {
            /// <summary>
            ///     Represents a state where no changes have been detected in the Transform object. This indicates
            ///     that position, rotation, and scale are all unmodified and do not require any serialization
            ///     or propagation during network synchronization or internal state updates.
            /// </summary>
            None = 0,

            /// <summary>
            ///     Indicates that the position of the Transform object has changed.
            /// </summary>
            Position = 1 << 0,

            /// <summary>
            ///     Indicates that the rotation of a Transform object has been modified.
            /// </summary>
            Rotation = 1 << 1,

            /// <summary>
            ///     Indicates that the scale of the Transform object has been modified.
            /// </summary>
            Scale = 1 << 2,

            /// <summary>
            ///     Indicates that all aspects of a Transform object (position, rotation, and scale)
            ///     have been modified.
            /// </summary>
            All = Position | Rotation | Scale
        }

        #endregion
    }
}