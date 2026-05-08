// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrFirstPersonCamera.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections;
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UnityEngine;
using UnityEngine.Rendering;

namespace UltimateXR.CameraUtils
{
    /// <summary>
    ///     Marks a <see cref="Camera" /> as a first-person camera. It temporarily hides the renderers listed in the assigned
    ///     <see cref="UxrAvatar.FirstPersonHiddenRenderers" /> while this camera renders, and updates a global shader
    ///     parameter that lets compatible shaders discard or keep pixels depending on whether the current camera is
    ///     first-person.
    /// </summary>
    /// <remarks>
    ///     Behaviour summary:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>
    ///                 If this component is disabled, it does nothing.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If a camera does not have this component, all renderers are rendered normally and the
    ///                 <c>_UxrRenderFirstPersonEffects</c> shader parameter is disabled for that camera.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 If a camera has this component enabled, the renderers in
    ///                 <see cref="UxrAvatar.FirstPersonHiddenRenderers" /> are disabled just before rendering and
    ///                 restored immediately after.
    ///             </description>
    ///         </item>
    ///         <item>
    ///             <description>
    ///                 While this camera renders, <c>_UxrRenderFirstPersonEffects</c> is enabled so shaders can perform the
    ///                 reverse operation too: rendering pixels only in first-person cameras.
    ///             </description>
    ///         </item>
    ///     </list>
    ///     This makes it possible to hide avatar parts such as the head or glasses only for specific cameras without relying
    ///     on layers, and to render first-person-only effects such as fade quads or wall-fade quads. Works with both the
    ///     built-in render pipeline and scriptable render pipelines (URP/HDRP).
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public sealed class UxrFirstPersonCamera : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private UxrAvatar _avatar;

        #endregion

        #region Public Types & Data

        /// <summary>
        ///     Gets or sets the avatar whose first-person hidden renderers will be disabled while this camera renders.
        /// </summary>
        public UxrAvatar Avatar
        {
            get => _avatar;
            set
            {
                _avatar = value;
                InvalidateCache();
            }
        }

        #endregion

        #region Unity

        /// <summary>
        ///     Subscribes to shared camera rendering callbacks.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (++s_enabledCount == 1)
            {
                Camera.onPreCull    += OnCameraPreCull;
                Camera.onPostRender += OnCameraPostRender;

                RenderPipelineManager.beginCameraRendering += OnSrpBeginCameraRendering;
                RenderPipelineManager.endCameraRendering   += OnSrpEndCameraRendering;
            }

            _endOfFrameCoroutine = StartCoroutine(EndOfFrameCoroutine());
        }

        /// <summary>
        ///     Unsubscribes from shared camera rendering callbacks and restores renderer state.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            s_enabledCount = Mathf.Max(0, s_enabledCount - 1);

            if (s_enabledCount == 0)
            {
                Camera.onPreCull    -= OnCameraPreCull;
                Camera.onPostRender -= OnCameraPostRender;

                RenderPipelineManager.beginCameraRendering -= OnSrpBeginCameraRendering;
                RenderPipelineManager.endCameraRendering   -= OnSrpEndCameraRendering;

                ResetRenderFirstPersonEffectsState();
            }

            if (_endOfFrameCoroutine != null)
            {
                StopCoroutine(_endOfFrameCoroutine);
                _endOfFrameCoroutine = null;
            }

            RestoreRenderers();
        }

        #endregion

        #region Coroutines

        /// <summary>
        ///     Restores renderer and shader state at the end of the frame as a safety fallback.
        /// </summary>
        /// <returns>
        ///     An enumerator used by Unity to execute the cleanup after rendering.
        /// </returns>
        private IEnumerator EndOfFrameCoroutine()
        {
            WaitForEndOfFrame wait = new WaitForEndOfFrame();

            while (true)
            {
                yield return wait;

                RestoreRenderers();
                ResetRenderFirstPersonEffectsState();
                RebuildCacheIfNeeded();
            }
            // ReSharper disable once IteratorNeverReturns
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called by Scriptable Render Pipelines before a camera is rendered.
        /// </summary>
        /// <param name="context">The render context used by the current Scriptable Render Pipeline.</param>
        /// <param name="targetCamera">The camera that is about to render.</param>
        private static void OnSrpBeginCameraRendering(ScriptableRenderContext context, Camera targetCamera)
        {
            BeginCameraRendering(targetCamera, true, context);
        }

        /// <summary>
        ///     Called by Scriptable Render Pipelines after a camera has rendered.
        /// </summary>
        /// <param name="context">The render context used by the current Scriptable Render Pipeline.</param>
        /// <param name="targetCamera">The camera that finished rendering.</param>
        private static void OnSrpEndCameraRendering(ScriptableRenderContext context, Camera targetCamera)
        {
            EndCameraRendering(targetCamera, true, context);
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Called by the Built-in Render Pipeline before a camera culls and renders the scene.
        /// </summary>
        /// <param name="targetCamera">The camera that is about to render.</param>
        private static void OnCameraPreCull(Camera targetCamera)
        {
            BeginCameraRendering(targetCamera, false, default);
        }

        /// <summary>
        ///     Called by the Built-in Render Pipeline after a camera has rendered the scene.
        /// </summary>
        /// <param name="targetCamera">The camera that finished rendering.</param>
        private static void OnCameraPostRender(Camera targetCamera)
        {
            EndCameraRendering(targetCamera, false, default);
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Enables first-person rendering state if the camera being rendered has an enabled
        ///     <see cref="UxrFirstPersonCamera" /> component.
        /// </summary>
        /// <param name="targetCamera">The camera that is about to render.</param>
        /// <param name="useCommandBuffer">Whether the shader state should be queued into the SRP render context.</param>
        /// <param name="context">The render context used by the current Scriptable Render Pipeline.</param>
        private static void BeginCameraRendering(Camera targetCamera, bool useCommandBuffer, ScriptableRenderContext context)
        {
            bool isFirstPersonCamera = TryGetFirstPersonCamera(targetCamera, out UxrFirstPersonCamera firstPersonCamera);

            s_renderFirstPersonEffectsStack.Push(s_renderFirstPersonEffects);
            SetRenderFirstPersonEffects(isFirstPersonCamera, useCommandBuffer, context);

            if (isFirstPersonCamera)
            {
                firstPersonCamera.RebuildCacheIfNeeded();
                firstPersonCamera.HideRenderers();
            }
        }

        /// <summary>
        ///     Restores first-person rendering state after a camera has rendered.
        /// </summary>
        /// <param name="targetCamera">The camera that finished rendering.</param>
        /// <param name="useCommandBuffer">Whether the shader state should be queued into the SRP render context.</param>
        /// <param name="context">The render context used by the current Scriptable Render Pipeline.</param>
        private static void EndCameraRendering(Camera targetCamera, bool useCommandBuffer, ScriptableRenderContext context)
        {
            if (TryGetFirstPersonCamera(targetCamera, out UxrFirstPersonCamera firstPersonCamera))
            {
                firstPersonCamera.RestoreRenderers();
            }

            bool previousValue = s_renderFirstPersonEffectsStack.Count > 0 && s_renderFirstPersonEffectsStack.Pop();
            SetRenderFirstPersonEffects(previousValue, useCommandBuffer, context);
        }

        /// <summary>
        ///     Checks whether the given camera is an enabled first-person camera.
        /// </summary>
        /// <param name="targetCamera">The camera to check.</param>
        /// <param name="firstPersonCamera">First-person camera component, if found and enabled.</param>
        /// <returns>
        ///     Whether the camera should render first-person effects.
        /// </returns>
        private static bool TryGetFirstPersonCamera(Camera targetCamera, out UxrFirstPersonCamera firstPersonCamera)
        {
            if (targetCamera != null                                &&
                targetCamera.TryGetComponent(out firstPersonCamera) &&
                firstPersonCamera.enabled)
            {
                return true;
            }

            firstPersonCamera = null;
            return false;
        }

        /// <summary>
        ///     Updates the shader flag that gates first-person-only effects.
        /// </summary>
        /// <param name="renderFirstPersonEffects">Whether first-person effects should render.</param>
        /// <param name="useCommandBuffer">Whether the value should be queued into an SRP render context.</param>
        /// <param name="context">Render context used by SRP.</param>
        private static void SetRenderFirstPersonEffects(bool renderFirstPersonEffects, bool useCommandBuffer, ScriptableRenderContext context)
        {
            float value = renderFirstPersonEffects ? 1.0f : 0.0f;

            s_renderFirstPersonEffects = renderFirstPersonEffects;

            if (useCommandBuffer)
            {
                CommandBuffer commandBuffer = new CommandBuffer
                                              {
                                                  name = nameof(UxrFirstPersonCamera)
                                              };

                try
                {
                    commandBuffer.SetGlobalFloat(UxrConstants.Shaders.UxrRenderFirstPersonEffectsId, value);
                    context.ExecuteCommandBuffer(commandBuffer);
                }
                finally
                {
                    commandBuffer.Release();
                }
            }
            else
            {
                Shader.SetGlobalFloat(UxrConstants.Shaders.UxrRenderFirstPersonEffectsId, value);
            }
        }

        /// <summary>
        ///     Restores the default first-person effect shader state.
        /// </summary>
        private static void ResetRenderFirstPersonEffectsState()
        {
            s_renderFirstPersonEffects = false;
            s_renderFirstPersonEffectsStack.Clear();

            Shader.SetGlobalFloat(UxrConstants.Shaders.UxrRenderFirstPersonEffectsId, 0.0f);
        }

        /// <summary>
        ///     Disables the cached avatar renderers that should be hidden from this first-person camera.
        /// </summary>
        private void HideRenderers()
        {
            RestoreRenderers();

            if (_cachedRenderers == null)
            {
                return;
            }

            for (int i = 0; i < _cachedRenderers.Length; ++i)
            {
                Renderer rendererToHide = _cachedRenderers[i];

                bool wasEnabled = rendererToHide != null && rendererToHide.enabled;

                _cachedRendererEnabledStates[i] = wasEnabled;

                if (wasEnabled)
                {
                    rendererToHide.enabled = false;
                    _hiddenCount++;
                }
            }
        }

        /// <summary>
        ///     Restores the avatar renderers that were disabled by <see cref="HideRenderers" />.
        /// </summary>
        private void RestoreRenderers()
        {
            if (_hiddenCount == 0)
            {
                return;
            }

            if (_cachedRenderers != null && _cachedRendererEnabledStates != null)
            {
                for (int i = 0; i < _cachedRenderers.Length; ++i)
                {
                    Renderer rendererToRestore = _cachedRenderers[i];

                    if (rendererToRestore != null && _cachedRendererEnabledStates[i])
                    {
                        rendererToRestore.enabled = true;
                    }

                    _cachedRendererEnabledStates[i] = false;
                }
            }

            _hiddenCount = 0;
        }

        /// <summary>
        ///     Rebuilds the cached renderer array if the avatar or its hidden renderer list has changed.
        /// </summary>
        private void RebuildCacheIfNeeded()
        {
            if (_avatar == null)
            {
                _cachedRenderers             = null;
                _cachedRendererEnabledStates = null;
                _cachedAvatar                = null;
                _cachedVersion               = -1;
                return;
            }

            List<Renderer> sourceList = _avatar.FirstPersonHiddenRenderers;
            int            version    = sourceList?.Count ?? 0;
            bool           dirty      = _cachedAvatar != _avatar || _cachedVersion != version;

            if (!dirty && _cachedRenderers != null)
            {
                for (int i = 0; i < _cachedRenderers.Length; ++i)
                {
                    if (sourceList == null || _cachedRenderers[i] != sourceList[i])
                    {
                        dirty = true;
                        break;
                    }
                }
            }

            if (!dirty)
            {
                return;
            }

            RestoreRenderers();

            _cachedAvatar  = _avatar;
            _cachedVersion = version;

            if (sourceList == null || sourceList.Count == 0)
            {
                _cachedRenderers             = null;
                _cachedRendererEnabledStates = null;
                return;
            }

            if (_cachedRenderers == null || _cachedRenderers.Length != sourceList.Count)
            {
                _cachedRenderers             = new Renderer[sourceList.Count];
                _cachedRendererEnabledStates = new bool[sourceList.Count];
            }

            for (int i = 0; i < sourceList.Count; ++i)
            {
                _cachedRenderers[i]             = sourceList[i];
                _cachedRendererEnabledStates[i] = false;
            }
        }

        /// <summary>
        ///     Forces the cached renderer array to be rebuilt before the next render.
        /// </summary>
        private void InvalidateCache()
        {
            _cachedAvatar  = null;
            _cachedVersion = -1;
        }

        #endregion

        #region Private Types & Data

        private static readonly Stack<bool> s_renderFirstPersonEffectsStack = new Stack<bool>();

        private static int  s_enabledCount;
        private static bool s_renderFirstPersonEffects;

        private Renderer[] _cachedRenderers;
        private bool[]     _cachedRendererEnabledStates;
        private UxrAvatar  _cachedAvatar;
        private int        _cachedVersion = -1;
        private int        _hiddenCount;
        private Coroutine  _endOfFrameCoroutine;

        #endregion
    }
}