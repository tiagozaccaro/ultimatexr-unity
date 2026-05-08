// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UniqueIdGenerationWindow.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Xml;
using UltimateXR.Core;
using UltimateXR.Core.Unique;
using UltimateXR.Editor.Core;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Utilities.MultiplayerUtils
{
    /// <summary>
    ///     Custom tool window that will make sure that all UXR elements in the project have correct
    ///     <see cref="UniqueId" /> values.
    /// </summary>
    public sealed class UniqueIdGenerationWindow : ComponentProcessorWindow<Component>
    {
        #region Public Methods

        /// <summary>
        ///     Fixes the given component Unique ID information if necessary.
        ///     Delegates to <see cref="UxrUniqueIdAutoFixer.FixComponent" /> which contains the centralized fix logic.
        /// </summary>
        /// <param name="info">Information about the component</param>
        /// <param name="forceRegenerateId">Whether to force the regeneration of the unique ID.</param>
        /// <param name="onlyCheck">
        ///     Whether to only check and return a value telling if the component requires changes but not
        ///     perform any modifications on it
        /// </param>
        /// <returns>Whether the component required changes</returns>
        public static bool FixComponentUniqueIdInformation(UxrComponentInfo<Component> info, bool forceRegenerateId, bool onlyCheck)
        {
            return UxrUniqueIdAutoFixer.FixComponent(info.TargetComponent, forceRegenerateId, onlyCheck);
        }

        #endregion

        #region Event Trigger Methods

        /// <inheritdoc />
        protected override bool OnProcessStarting()
        {
            if (!base.OnProcessStarting())
            {
                return false;
            }

            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, false);

            return true;
        }

        /// <inheritdoc />
        protected override void OnProcessEnded()
        {
            base.OnProcessEnded();

            EditorPrefs.SetBool(UxrConstants.Editor.AutomaticIdGenerationPrefs, true);
        }

        #endregion

        #region Protected Overrides ComponentProcessorWindow<Component>

        /// <inheritdoc />
        protected override bool CanProcessUltimateXRAssets => true;

        /// <inheritdoc />
        protected override string HelpBoxMessage => "This generates missing unique IDs for UltimateXR components. Unique IDs are used to identify objects so that functionality such as multi-user and state saves can work correctly. UltimateXR versions prior to 1.0.0 might have missing IDs since unique ID information was first introduced in 1.0.0. Use this tool to generate missing unique ID information in projects created with older versions.";

        /// <inheritdoc />
        protected override string DontIgnoreUxrAssetsWarningMessage => "All assets in UltimateXR come already with unique ID information. Changing it may have unwanted results";

        /// <inheritdoc />
        protected override string ProcessButtonText => "Generate";

        /// <inheritdoc />
        protected override void ProcessComponent(UxrComponentInfo<Component> info, bool onlyCheck, out bool isChanged, out bool forceNoLog)
        {
            isChanged  = false;
            forceNoLog = true;

            if (info.TargetComponent is IUxrUniqueId)
            {
                forceNoLog = false;
                isChanged  = FixComponentUniqueIdInformation(info, false, onlyCheck);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Menu entry that invokes the tool.
        /// </summary>
        [MenuItem(UxrConstants.Editor.MenuPathNetworking + "Check Unique IDs", priority = UxrConstants.Editor.PriorityMenuPathNetworking + 1)]
        private static void Init()
        {
            UniqueIdGenerationWindow window = (UniqueIdGenerationWindow)GetWindow(typeof(UniqueIdGenerationWindow));
            window.Show();
        }

        #endregion
    }
}