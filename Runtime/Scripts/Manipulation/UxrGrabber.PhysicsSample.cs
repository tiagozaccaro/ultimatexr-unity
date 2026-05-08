// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrGrabber.PhysicsSample.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Manipulation
{
    public partial class UxrGrabber
    {
        #region Private Types & Data

        /// <summary>
        ///     Stores physics data of a frame to perform smooth throw computations.
        /// </summary>
        private struct PhysicsSample
        {
            #region Public Types & Data

            /// <summary>
            ///     Gets the sampled center of mass world position.
            /// </summary>
            public Vector3 CenterOfMass { get; private set; }

            /// <summary>
            ///     Gets the sampled fingertip world position.
            /// </summary>
            public Vector3 Tip { get; private set; }

            /// <summary>
            ///     Gets the sampled rotation.
            /// </summary>
            public Quaternion Rotation { get; private set; }

            /// <summary>
            ///     Gets the elapsed time in seconds with respect to the previous sample.
            /// </summary>
            public float DeltaTime { get; private set; }

            /// <summary>
            ///     Gets the sampled angular speed in Euler angles.
            /// </summary>
            public Vector3 EulerSpeed { get; private set; }

            /// <summary>
            ///     Gets the sampled linear velocity in world space.
            /// </summary>
            public Vector3 Velocity { get; private set; }

            /// <summary>
            ///     Gets the sampled total velocity in world space. It is the result of combining the linear velocity plus the velocity
            ///     due to the throw axis angular speed. This should be used to compute throw release velocity.
            /// </summary>
            public Vector3 TotalVelocity { get; private set; }

            /// <summary>
            ///     Gets or sets the sample age in seconds.
            /// </summary>
            public float Age { get; set; }

            #endregion

            #region Public Methods

            /// <summary>
            ///     Initializes the sample.
            /// </summary>
            /// <param name="sampledTransform">
            ///     Transform from the grabbed object if there is one currently being grabbed, otherwise
            ///     from the grabber
            /// </param>
            /// <param name="centerOfMass">World position of the throwing center of mass</param>
            /// <param name="tip">World position of the fingertip approximation, to account for angular velocity in the throw</param>
            /// <param name="deltaTime">Time in seconds since the last frame</param>
            public void Initialize(Transform sampledTransform, Vector3 centerOfMass, Vector3 tip, float deltaTime)
            {
                Age           = 0.0f;
                Rotation      = sampledTransform.rotation;
                DeltaTime     = deltaTime;
                CenterOfMass  = centerOfMass;
                Tip           = tip;
                EulerSpeed    = Vector3.zero;
                Velocity      = Vector3.zero;
                TotalVelocity = Velocity;
            }

            /// <summary>
            ///     Computes the angular and linear velocities using data from the last physics sample and the current transform state.
            /// </summary>
            /// <param name="lastSample">
            ///     The physics sample containing data from the previous frame, used to calculate changes in position and rotation.
            /// </param>
            /// <param name="sampledTransform">
            ///     The transform representing the current state of the grabbed object or the grabber.
            ///     Used to calculate the rotation and position changes relative to the last sample.
            /// </param>
            public void ComputeVelocities(PhysicsSample lastSample, Transform sampledTransform)
            {
                if (DeltaTime <= Mathf.Epsilon)
                {
                    EulerSpeed    = Vector3.zero;
                    Velocity      = Vector3.zero;
                    TotalVelocity = Velocity;
                    return;
                }

                // Angular

                Quaternion relative = Quaternion.Inverse(lastSample.Rotation) * Rotation;
                relative.ToAngleAxis(out float angle, out Vector3 axis);

                if (angle > 180.0f)
                {
                    angle -= 360.0f;
                }

                if (axis.sqrMagnitude > Mathf.Epsilon)
                {
                    Vector3 worldAxis = lastSample.Rotation * axis.normalized;
                    EulerSpeed = angle * worldAxis / DeltaTime;
                }
                else
                {
                    EulerSpeed = Vector3.zero;
                }

                // Linear. TODO: Improve using a mix of linear and angular components?

                Velocity      = (Tip - lastSample.Tip) / DeltaTime;
                TotalVelocity = Velocity;
            }

            #endregion
        }

        #endregion
    }
}