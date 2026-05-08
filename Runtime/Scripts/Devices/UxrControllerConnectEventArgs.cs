// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrControllerConnectEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;

namespace UltimateXR.Devices
{
    /// <summary>
    ///     XR Controller connection/disconnection event arguments.
    /// </summary>
    public class UxrControllerConnectEventArgs : UxrDeviceConnectEventArgs
    {
        #region Public Types & Data

        /// <summary>
        ///     The name of the device that was connected/disconnected.
        /// </summary>
        public string DeviceName { get; }

        /// <summary>
        ///     Whether the controller supports handedness, meaning it is capable of distinguishing between being held by the left
        ///     or
        ///     right hand.
        /// </summary>
        public bool SupportsHandedness { get; }

        /// <summary>
        ///     Only valid if <see cref="SupportsHandedness" /> is true. Specifies the hand (left or right) associated with the XR
        ///     controller.
        /// </summary>
        public UxrHandSide HandSide { get; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="isConnected">Whether the device was connected (true) or disconnected (false)</param>
        /// <param name="deviceName">The name of the device</param>
        /// <param name="supportsHandedness">Whether it supports handedness</param>
        /// <param name="handSide">If handedness is supported, which hand it is</param>
        public UxrControllerConnectEventArgs(bool isConnected, string deviceName, bool supportsHandedness, UxrHandSide handSide) : base(isConnected)
        {
            DeviceName         = deviceName;
            SupportsHandedness = supportsHandedness;
            HandSide           = handSide;
        }

        #endregion
    }
}