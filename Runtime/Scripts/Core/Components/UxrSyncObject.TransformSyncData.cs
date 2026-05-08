// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSyncObject.TransformSyncData.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Core.Components
{
    public partial class UxrSyncObject
    {
        #region Private Types & Data

        /// <summary>
        ///     Represents the synchronized state of a transform at a specific point in time, used for interpolation,
        ///     extrapolation, or network-based transform synchronization. Stores the position, rotation, scale, and
        ///     a timestamp of when the state was captured.
        /// </summary>
        private readonly struct TransformSyncData
        {
            #region Constructors & Finalizer

            /// <summary>
            ///     Constructor.
            /// </summary>
            /// <param name="position">The position of the transform.</param>
            /// <param name="rotation">The rotation of the transform.</param>
            /// <param name="scale">The scale of the transform.</param>
            /// <param name="time">The timestamp indicating when the state was captured.</param>
            public TransformSyncData(Vector3 position, Quaternion rotation, Vector3 scale, float time)
            {
                Position = position;
                Rotation = rotation;
                Scale    = scale;
                Time     = time;
            }

            #endregion

            #region Public Types & Data

            public readonly Vector3    Position;
            public readonly Quaternion Rotation;
            public readonly Vector3    Scale;
            public readonly float      Time;

            #endregion
        }

        #endregion
    }
}