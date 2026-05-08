// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ComponentProcessorWindow.TargetObjects.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Editor.Utilities
{
    public abstract partial class UxrAssetProcessorWindow<T>
    {
        #region Private Types & Data

        /// <summary>
        ///     Enumerates the different potential target object(s) for the assset processor.
        /// </summary>
        protected enum TargetObjects
        {
            /// <summary>
            ///     Processes a single asset.
            /// </summary>
            SingleAsset,

            /// <summary>
            ///     Processes a single folder.
            /// </summary>
            ProjectFolder,

            /// <summary>
            ///     Processes a whole folder and all subfolders recursively.
            /// </summary>
            ProjectFolderAndSubFolders,
        }

        #endregion
    }
}