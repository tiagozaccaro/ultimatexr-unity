// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAssetProcessorWindow.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UltimateXR.Core;
using UltimateXR.Editor.Extensions;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Utilities
{
    /// <summary>
    ///     Base editor window to create tools that process a type of asset on a selection or even the whole project.
    /// </summary>
    public abstract partial class UxrAssetProcessorWindow<T> : EditorWindow where T : Object
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private TargetObjects _targetObjects = TargetObjects.ProjectFolder;
        [SerializeField] private T             _targetSingleObject;
        [SerializeField] private string        _startPath       = "";
        [SerializeField] private bool          _ignoreUxrAssets = true;
        [SerializeField] private UxrLogOptions _uxrLogOptions   = UxrLogOptions.Processed;
        [SerializeField] private bool          _onlyCheck;

        #endregion

        #region Unity

        /// <summary>
        ///     Draws the inspector
        /// </summary>
        private void OnGUI()
        {
            if (!string.IsNullOrEmpty(HelpBoxMessage))
            {
                EditorGUILayout.HelpBox(HelpBoxMessage, HelpBoxMessageType);
            }

            bool buttonEnabled       = false;
            bool showIgnoreUxrOption = CanProcessUltimateXRAssets;

            TargetObjectsType = (TargetObjects)EditorGUILayout.EnumPopup(ContentTargetObjects, TargetObjectsType);

            if (TargetObjectsType == TargetObjects.SingleAsset)
            {
                _targetSingleObject = (T)EditorGUILayout.ObjectField(ContentTargetSingleObject, _targetSingleObject, typeof(T), false);
                buttonEnabled       = _targetSingleObject != null;
            }
            else if (TargetObjectsType is TargetObjects.ProjectFolder or TargetObjects.ProjectFolderAndSubFolders)
            {
                EditorGUILayout.BeginHorizontal();

                StartPath = EditorGUILayout.TextField(ContentPathStart, StartPath);

                if (GUILayout.Button(ContentChooseFolder, GUILayout.ExpandWidth(false)) && UxrEditorUtils.OpenFolderPanel(out string path))
                {
                    StartPath = path;
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();

                showIgnoreUxrOption = false;
                buttonEnabled       = true;
            }

            if (showIgnoreUxrOption)
            {
                EditorGUI.BeginChangeCheck();
                _ignoreUxrAssets = EditorGUILayout.Toggle(ContentIgnoreUxrAssets, _ignoreUxrAssets);

                if (EditorGUI.EndChangeCheck() && !_ignoreUxrAssets && !string.IsNullOrEmpty(DontIgnoreUxrAssetsWarningMessage))
                {
                    EditorUtility.DisplayDialog(UxrConstants.Editor.Warning, DontIgnoreUxrAssetsWarningMessage, UxrConstants.Editor.Ok);
                }
            }

            _uxrLogOptions = (UxrLogOptions)EditorGUILayout.EnumFlagsField(ContentLogOptions, _uxrLogOptions);
            _onlyCheck     = EditorGUILayout.Toggle(ContentOnlyCheck, _onlyCheck);

            // Draw processor GUI if necessary

            OnProcessorGUI();

            // Bottom part

            GUILayout.Space(30);
            GUI.enabled = buttonEnabled && ProcessButtonEnabled;

            if (UxrEditorUtils.CenteredButton(new GUIContent(_onlyCheck ? "Check" : ProcessButtonText)))
            {
                if (OnProcessStarting())
                {
                    ProcessAllAssets();

                    OnProcessEnded();

                    if (TargetObjectsType != TargetObjects.SingleAsset)
                    {
                        ShowResultsDialog(_processedAssetsCount.Sum(c => c.Value));
                    }
                }
            }

            GUI.enabled = true;
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Overridable method called right before starting processing.
        /// </summary>
        /// <remarks>When overriden, the base needs to be called</remarks>
        protected virtual bool OnProcessStarting()
        {
            _processedAssetsCount = new Dictionary<string, int>();

            return true;
        }

        /// <summary>
        ///     Overridable method called right after processing finished.
        /// </summary>
        /// <remarks>When overriden, the base needs to be called</remarks>
        protected virtual void OnProcessEnded()
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        /// <summary>
        ///     Draws the specific processor GUI.
        /// </summary>
        /// <remarks>When overriden, the base needs to be called</remarks>
        protected virtual void OnProcessorGUI()
        {
            if (ViewPreviewProcessing)
            {
                OnPreviewAssetGUI();
            }
        }

        protected virtual void OnAssetPreviewGUI(T asset, string assetPath)
        {
        }

        private void OnPreviewAssetGUI()
        {
            // Preview Area
            EditorGUILayout.BeginHorizontal();
            _viewPreview = EditorGUILayout.Toggle(ContentViewPreview, _viewPreview);
            if (!_viewPreview)
            {
                EditorGUILayout.EndHorizontal();
                return;
            }
            if (TargetObjectsType != TargetObjects.SingleAsset)
            {
                EditorGUILayout.Space();
                if (GUILayout.Button("Update Preview", GUILayout.Width(100)))
                {
                    UpdatePreviewData();
                }
            }
            EditorGUILayout.EndHorizontal();

            // Single Asset Preview
            if (TargetObjectsType == TargetObjects.SingleAsset)
            {
                if (_targetSingleObject == null)
                {
                    return;
                }

                OnAssetPreviewGUI(_targetSingleObject, AssetDatabase.GetAssetPath(_targetSingleObject));
                return;
            }

            // Multiple Assets Preview
            if (_previewAssets.Count == 0)
            {
                return;
            }

            // Upper navigation bar + paged list
            IReadOnlyList<KeyValuePair<T, string>> previewList = _previewAssets.ToList();
            EditorGUILayoutExt.PaginatedScrollView(previewList,
                                                   ref _currentPage,
                                                   ref _previewScrollPosition,
                                                   ItemsPerPage,
                                                   pair => OnAssetPreviewGUI(pair.Key, pair.Value));
        }

        #endregion

        #region Protected Methods

        protected abstract void ProcessAsset(T asset, string assetPath, bool onlyCheck, out bool isChanged, out bool forceNoLog);


        protected virtual void ShowResultsDialog(int assetCount)
        {
            string action = _onlyCheck ? "Found" : "Processed";
            EditorUtility.DisplayDialog("Finished", $"{action} {assetCount} assets in path: {_startPath}", UxrConstants.Editor.Ok);
        }

        protected virtual void UpdatePreviewData()
        {
            _previewAssets.Clear();
            if (string.IsNullOrEmpty(StartPath))
            {
                return;
            }

            string[] searchFolders = { StartPath };
            bool     recursive     = TargetObjectsType == TargetObjects.ProjectFolderAndSubFolders;

            string   filter = $"t:{typeof(T).Name}";
            string[] guids  = AssetDatabase.FindAssets(filter, searchFolders);

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);

                if (!recursive && !string.IsNullOrEmpty(StartPath))
                {
                    string assetFolder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (assetFolder != StartPath.TrimEnd('/'))
                    {
                        continue;
                    }
                }

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                _previewAssets.Add(asset, path);
            }
        }

        #endregion

        #region Private Methods

        private void ProcessAllAssets()
        {
            if (TargetObjectsType == TargetObjects.SingleAsset)
            {
                CurrentTargetAsset = _targetSingleObject;
                AssetProcessor(CurrentTargetAsset, AssetDatabase.GetAssetPath(CurrentTargetAsset), _onlyCheck);
                return;
            }

            string[] searchFolders = string.IsNullOrEmpty(StartPath) ? null : new[] { StartPath };
            bool     recursive     = TargetObjectsType == TargetObjects.ProjectFolderAndSubFolders;

            string   filter = $"t:{typeof(T).Name}";
            string[] guids  = AssetDatabase.FindAssets(filter, searchFolders);

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (!recursive && !string.IsNullOrEmpty(StartPath))
                {
                    string assetFolder = Path.GetDirectoryName(path)?.Replace('\\', '/');
                    if (assetFolder != StartPath.TrimEnd('/'))
                    {
                        continue;
                    }
                }

                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null)
                {
                    continue;
                }

                EditorUtility.DisplayProgressBar(ProcessButtonText, $"Processing {path}", (float)i / guids.Length);

                CurrentTargetAsset = asset;
                AssetProcessor(CurrentTargetAsset, path, _onlyCheck);
            }
        }

        private bool AssetProcessor(T asset, string assetPath, bool onlyCheck)
        {
            ProcessAsset(asset, assetPath, onlyCheck, out bool isChanged, out bool ignoreLog);

            if (!ignoreLog)
            {
                LogProcessing(asset, assetPath, isChanged);
            }

            return isChanged;
        }

        private void LogProcessing(T asset, string assetPath, bool isChanged)
        {
            string action = _onlyCheck ? "Found" : "Processed";
            string processAction = isChanged  ? action :
                                   _onlyCheck ? "Found to ignore" : "Ignored";
            bool shouldLog = (isChanged && _uxrLogOptions.HasFlag(UxrLogOptions.Processed)) || (!isChanged && _uxrLogOptions.HasFlag(UxrLogOptions.Ignored));

            if (asset != null)
            {
                if (isChanged)
                {
                    if (!_processedAssetsCount.TryAdd(assetPath, 1))
                    {
                        _processedAssetsCount[assetPath]++;
                    }
                }

                if (shouldLog)
                {
                    Debug.Log($"{processAction} asset '{asset.name}' in path: {assetPath}");
                }
            }
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Gets whether the component processor can process components from assets in UltimateXR folders.
        /// </summary>
        protected virtual bool CanProcessUltimateXRAssets => false;

        /// <summary>
        ///     Gets the message to show in the help box. Null or empty for no message.
        /// </summary>
        protected virtual string HelpBoxMessage => string.Empty;

        /// <summary>
        ///     Gets the type of message to show in the help box.
        /// </summary>
        protected virtual MessageType HelpBoxMessageType => MessageType.Info;

        /// <summary>
        ///     Gets the message to show in the help box. Null or empty for no message.
        /// </summary>
        protected virtual string DontIgnoreUxrAssetsWarningMessage => "All assets in UltimateXR come with a predefined configuration. Changing it may have unwanted results";

        /// <summary>
        ///     Gets the text to show on the process button.
        /// </summary>
        protected virtual string ProcessButtonText => "Process";

        /// <summary>
        ///     Gets whether the process button is available.
        /// </summary>
        protected virtual bool ProcessButtonEnabled => true;

        protected virtual bool ViewPreviewProcessing => true;

        protected string StartPath
        {
            get => _startPath;
            set
            {
                if (_startPath != value)
                {
                    _startPath = value;
                    UpdatePreviewData();
                }
            }
        }

        /// <summary>
        ///     Gets the current asset being processed.
        /// </summary>
        protected T CurrentTargetAsset { get; private set; }


        protected TargetObjects TargetObjectsType
        {
            get => _targetObjects;
            private set
            {
                if (_targetObjects != value)
                {
                    _targetObjects = value;
                    UpdatePreviewData();
                }
            }
        }

        #endregion

        #region Private Types & Data

        private GUIContent ContentTargetObjects      => new GUIContent("Target Objects",           "The objects to change: objects in the current scene or prefabs in the whole project");
        private GUIContent ContentTargetSingleObject => new GUIContent("Object To Process",        "The object to process");
        private GUIContent ContentPathStart          => new GUIContent("Path Start",               "If empty, it will process the whole /Assets folder. Use Assets/Application/Prefabs/ to start from this folder for example");
        private GUIContent ContentChooseFolder       => new GUIContent("...",                      "Selects the root folder to process");
        private GUIContent ContentIgnoreUxrAssets    => new GUIContent("Ignore UltimateXR assets", "Ignores processing assets in UltimateXR folders");
        private GUIContent ContentLogOptions         => new GUIContent("Log Options",              "Whether to log components that were processed and components that were not processed (ignored)");
        private GUIContent ContentOnlyCheck          => new GUIContent("Only Log, Don't Modify",   "Scared to proceed and make changes? This option will not make any modifications and instead will only log on the console which objects would be changed");
        private GUIContent ContentViewPreview        => new GUIContent("View Preview",             "Whether to show a preview of the assets being processed");

        /// <summary>
        ///     Gets whether to ignore components in assets in UltimateXR folders.
        /// </summary>
        private bool IgnoreUxrAssets => !CanProcessUltimateXRAssets || _ignoreUxrAssets;

        private const    int                   ItemsPerPage   = 100;
        private readonly Dictionary<T, string> _previewAssets = new Dictionary<T, string>();

        private Dictionary<string, int> _processedAssetsCount = new Dictionary<string, int>();

        private Vector2 _previewScrollPosition;
        private bool    _viewPreview = true;
        private int     _currentPage;

        #endregion
    }
}