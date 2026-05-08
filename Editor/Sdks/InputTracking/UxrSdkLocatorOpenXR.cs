// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSdkLocatorOpenXR.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;
using UnityEditor;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
#endif

namespace UltimateXR.Editor.Sdks.InputTracking
{
    /// <summary>
    ///     SDK Locator for the OpenXR SDK.
    /// </summary>
    public sealed class UxrSdkLocatorOpenXR : UxrSdkLocator
    {
        #region Public Overrides UxrSdkLocator

        /// <inheritdoc />
        public override SupportType Support => SupportType.InputTracking;

        /// <inheritdoc />
        public override string PackageName => "com.unity.xr.openxr";

        /// <inheritdoc />
        public override string Name => UxrConstants.SdkOpenXR;

        /// <inheritdoc />
        public override string MinimumUnityVersion => "2021.3";

        /// <inheritdoc />
        public override string[] AvailableSymbols
        {
            get
            {
                if (CurrentState == State.Available)
                {
                    if (CurrentVersion == 0)
                    {
                        return new[] { "ULTIMATEXR_UNITY_XR_OPENXR" };
                    }
                }

                return new string[0];
            }
        }

        /// <inheritdoc />
        public override string[] AllSymbols
        {
            get { return new[] { "ULTIMATEXR_UNITY_XR_OPENXR" }; }
        }

        /// <inheritdoc />
        public override bool CanBeUpdated => false;

        /// <inheritdoc />
        public override void TryLocate()
        {
#if UNITY_2021_3_OR_NEWER

            // UltimateXR assembly sets up define for package com.unity.xr.openxr
#if ULTIMATEXR_UNITY_XR_OPENXR
            CurrentVersion = 0;
            CurrentState   = State.Available;
#else
            CurrentState = State.NotInstalled;
#endif

#else
            CurrentState = State.NeedsHigherUnityVersion
#endif
        }

        /// <inheritdoc />
        public override void TryGet()
        {
#if UNITY_EDITOR
            AddRequest request = Client.Add("com.unity.xr.openxr");
#endif
        }

        /// <inheritdoc />
        public override void TryUpdate()
        {

        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Auto-registers the locator each time Unity is launched or the project folder is updated.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void RegisterLocator()
        {
            UxrSdkManager.RegisterLocator(new UxrSdkLocatorOpenXR());
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Allows to remove dependencies from the project in case the user removed SDK folders manually.
        /// </summary>
        [MenuItem(UxrConstants.Editor.MenuPathSdksInputTracking + "Remove Symbols for OpenXR", priority = UxrConstants.Editor.PriorityMenuPathSdksInputTracking)]
        private static void RemoveSymbols()
        {
            UxrSdkManager.RemoveSymbols(new UxrSdkLocatorOpenXR());
        }

        #endregion
    }
}