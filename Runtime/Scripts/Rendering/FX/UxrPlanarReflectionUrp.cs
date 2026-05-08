// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPlanarReflectionUrp.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Avatar;
using UltimateXR.Core.Components;
using UltimateXR.Devices;
using UltimateXR.Extensions.Unity;
using UltimateXR.Extensions.Unity.Math;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.XR;

#if ULTIMATEXR_UNITY_URP
using UnityEngine.Rendering.Universal;
#endif

namespace UltimateXR.Rendering.FX
{
    /// <summary>
    ///     Component that renders a planar reflection image of the scene on an object, using the URP pipeline:
    ///     <list type="bullet">
    ///         <item>
    ///             If either Mirror Transform or Mirror Renderer ar not set, it will try to get these components on the same
    ///             GameObjects where the planar reflection is.
    ///         </item>
    ///         <item>The mirror normal is determined by the -forward axis of Mirror Transform.</item>
    ///         <item>
    ///             For some reason the reflection will not work if the clear skybox is not set on the camera.
    ///             Unity doesn't seem to compute the projection matrices correctly.
    ///         </item>
    ///         <item>
    ///             The mirror renderer should have a material compatible with the URP planar reflection. They can be found
    ///             in the UltimateXR/FX/ category.
    ///         </item>
    ///     </list>
    /// </summary>
    public class UxrPlanarReflectionUrp : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        // Inspector

        [SerializeField] private bool      _forceClearSkyBox;
        [SerializeField] private Transform _mirrorTransform;
        [SerializeField] private Renderer  _mirrorRenderer;
        [SerializeField] private Material  _materialWhenDisabled;
        [SerializeField] private bool      _disablePixelLights = true;
        [SerializeField] private int       _textureSize        = 1024;
        [SerializeField] private float     _clipPlaneOffset;
        [SerializeField] private LayerMask _reflectLayers = -1;

        #endregion

        #region Unity

        /// <summary>
        ///     Stores the current material so that a "cheap" material can be assigned when disabled and the original material
        ///     re-assigned when enabled back again.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _mirrorTransform = ReferenceEquals(_mirrorTransform, null) ? transform : _mirrorTransform;
            _mirrorRenderer  = ReferenceEquals(_mirrorRenderer,  null) ? GetComponent<Renderer>() : _mirrorRenderer;

            if (_mirrorRenderer != null)
            {
                _materials = _mirrorRenderer.materials;
            }
        }

        /// <summary>
        ///     Frees resources.
        /// </summary>
        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            foreach (KeyValuePair<Camera, Camera> camPair in _reflectionCameras)
            {
                if (camPair.Value != null)
                {
                    DestroyImmediate(camPair.Value.gameObject);
                }
            }

            if (_reflectionTextureLeft)
            {
                DestroyImmediate(_reflectionTextureLeft);
                _reflectionTextureLeft = null;
            }

            if (_reflectionTextureRight)
            {
                DestroyImmediate(_reflectionTextureRight);
                _reflectionTextureRight = null;
            }

            if (_renderCameraObject)
            {
                Destroy(_renderCameraObject);
                _renderCameraObject = null;
            }

            _reflectionCameras.Clear();
        }

        /// <summary>
        ///     Subscribes to the URP camera rendering callback.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            if (_mirrorRenderer != null && _materials != null)
            {
                _mirrorRenderer.materials = _materials;
            }

            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering += RenderPipelineManager_BeginReflectionCameraRendering;
            RenderPipelineManager.endCameraRendering   += RenderPipelineManager_EndReflectionCameraRendering;
        }

        /// <summary>
        ///     Unsubscribes from the URP camera rendering callback.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_BeginCameraRendering;
            RenderPipelineManager.beginCameraRendering -= RenderPipelineManager_BeginReflectionCameraRendering;
            RenderPipelineManager.endCameraRendering   -= RenderPipelineManager_EndReflectionCameraRendering;

            foreach (KeyValuePair<Camera, Camera> camPair in _reflectionCameras)
            {
                if (camPair.Value != null)
                {
                    camPair.Value.enabled = false;
                }
            }

            if (s_reflectionRequestCamera != null)
            {
                GL.invertCulling        = false;
                s_reflectionRequestCamera = null;
            }

            if (_mirrorRenderer != null && _materialWhenDisabled)
            {
                _mirrorRenderer.sharedMaterial = _materialWhenDisabled;
            }
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called before a camera starts rendering. It is used to enable inverted culling when rendering the
        ///     reflection camera using render requests in newer URP versions.
        /// </summary>
        private static void RenderPipelineManager_BeginReflectionCameraRendering(ScriptableRenderContext context, Camera camera)
        {
#if ULTIMATEXR_UNITY_URP && UNITY_6000_2_OR_NEWER
            if (camera == s_reflectionRequestCamera)
            {
                GL.invertCulling = true;
            }
#endif
        }

        /// <summary>
        ///     Called after a camera finishes rendering. It is used to restore culling after rendering the reflection
        ///     camera using render requests in newer URP versions.
        /// </summary>
        private static void RenderPipelineManager_EndReflectionCameraRendering(ScriptableRenderContext context, Camera camera)
        {
#if ULTIMATEXR_UNITY_URP && UNITY_6000_2_OR_NEWER
            if (camera == s_reflectionRequestCamera)
            {
                GL.invertCulling = false;
            }
#endif
        }

        /// <summary>
        ///     Called by Unity when the rendering starts. It is used in this component to render the reflection.
        /// </summary>
        private void RenderPipelineManager_BeginCameraRendering(ScriptableRenderContext context, Camera renderCamera)
        {
            // Avoid other cameras except for the main one.

            if (UxrAvatar.LocalAvatarCamera != renderCamera || !_mirrorRenderer || !_mirrorRenderer.sharedMaterial)
            {
                return;
            }

            // Avoid recursive rendering

            if (s_insideRendering)
            {
                return;
            }

            s_insideRendering = true;

            CreateResources(renderCamera, out Camera reflectionCamera);

            // Lower quality for reflection

            int oldPixelLightCount = QualitySettings.pixelLightCount;
            if (_disablePixelLights)
            {
                QualitySettings.pixelLightCount = 0;
            }

            CopyCameraData(renderCamera, reflectionCamera);

            // Update parameters

            reflectionCamera.cullingMask = ~(1 << 4) & _reflectLayers.value;
            
            if (_materials != null)
            {
                foreach (Material m in _materials)
                {
                    if (m.HasProperty(s_varReflectionTexLeft))
                    {
                        m.SetTexture(s_varReflectionTexLeft, _reflectionTextureLeft);
                    }

                    if (m.HasProperty(s_varReflectionTexRight))
                    {
                        m.SetTexture(s_varReflectionTexRight, _reflectionTextureRight);
                    }

                    m.SetFloat(s_varReflectionMaxLodBias, _reflectionTextureLeft.width == 0 ? 0.0f : Mathf.Log(_reflectionTextureLeft.width, 2.0f));
                }
            }

            // Render

            reflectionCamera.enabled = true;

            reflectionCamera.targetTexture = _reflectionTextureLeft;
            RenderReflection(context, renderCamera, reflectionCamera, true, true, _mirrorTransform.position, -_mirrorTransform.forward);
            reflectionCamera.targetTexture = _reflectionTextureRight;
            RenderReflection(context, renderCamera, reflectionCamera, true, false, _mirrorTransform.position, -_mirrorTransform.forward);

            reflectionCamera.enabled = false;

            // Restore quality

            if (_disablePixelLights)
            {
                QualitySettings.pixelLightCount = oldPixelLightCount;
            }

            s_insideRendering = false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Renders the reflection.
        /// </summary>
        /// <param name="context">Render context of the scriptable render pipeline</param>
        /// <param name="renderCamera">Main camera</param>
        /// <param name="reflectionCamera">Camera that will render reflection</param>
        /// <param name="stereo">Is stereo mode active?</param>
        /// <param name="isLeft">Is it the left eye in stereo mode?</param>
        /// <param name="pos">Reflection plane position</param>
        /// <param name="normal">Reflection plane normal</param>
        private void RenderReflection(ScriptableRenderContext context, Camera renderCamera, Camera reflectionCamera, bool stereo, bool isLeft, Vector3 pos, Vector3 normal)
        {
            reflectionCamera.ResetWorldToCameraMatrix();
            reflectionCamera.ResetCullingMatrix();

            // Reflect camera using reflection plane

            float   d               = -Vector3.Dot(normal, pos) - _clipPlaneOffset;
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);

            Matrix4x4 reflection = Matrix4x4Ext.GetReflectionMatrix(reflectionPlane);
            Matrix4x4 projection = renderCamera.projectionMatrix;

            if (stereo && UxrTrackingDevice.GetHeadsetDevice(out InputDevice headsetDevice))
            {
                headsetDevice.TryGetFeatureValue(CommonUsages.leftEyePosition,  out Vector3 leftEye);
                headsetDevice.TryGetFeatureValue(CommonUsages.rightEyePosition, out Vector3 rightEye);
                float ipd = Vector3.Distance(leftEye, rightEye);

                if (isLeft)
                {
                    reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position - 0.5f * ipd * renderCamera.transform.right, renderCamera.transform.rotation);
                    projection = renderCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Left);
                }
                else
                {
                    reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position + 0.5f * ipd * renderCamera.transform.right, renderCamera.transform.rotation);
                    projection = renderCamera.GetStereoProjectionMatrix(Camera.StereoscopicEye.Right);
                }
            }
            else
            {
                reflectionCamera.transform.SetPositionAndRotation(renderCamera.transform.position, renderCamera.transform.rotation);
            }

            // World->ReflectionCamera matrix

            reflectionCamera.worldToCameraMatrix *= reflection;

            // Create the projection matrix. The near plane will be our reflection plane so that we will clip everything on the other side.

            Vector4 clipPlane = GetCameraSpacePlane(reflectionCamera, _clipPlaneOffset, pos, normal, 1.0f);
            projection                        = projection.GetObliqueMatrix(clipPlane);
            reflectionCamera.projectionMatrix = projection;
            reflectionCamera.cullingMatrix    = reflectionCamera.projectionMatrix * reflectionCamera.worldToCameraMatrix;

            // Render

#if ULTIMATEXR_UNITY_URP
#if UNITY_6000_2_OR_NEWER
            RenderTexture destination = reflectionCamera.targetTexture;

            if (destination != null)
            {
                UniversalRenderPipeline.SingleCameraRequest request = new UniversalRenderPipeline.SingleCameraRequest
                {
                    destination = destination
                };

                if (RenderPipeline.SupportsRenderRequest(reflectionCamera, request))
                {
                    Camera previousReflectionRequestCamera = s_reflectionRequestCamera;
                    s_reflectionRequestCamera = reflectionCamera;

                    try
                    {
                        RenderPipeline.SubmitRenderRequest(reflectionCamera, request);
                    }
                    finally
                    {
                        GL.invertCulling        = false;
                        s_reflectionRequestCamera = previousReflectionRequestCamera;
                    }
                }
            }
#else
#pragma warning disable CS0618
            GL.invertCulling = true;
            UniversalRenderPipeline.RenderSingleCamera(context, reflectionCamera);
            GL.invertCulling = false;
#pragma warning restore CS0618
#endif
#endif

            reflectionCamera.ResetWorldToCameraMatrix();
            reflectionCamera.ResetCullingMatrix();
        }

        /// <summary>
        ///     Copies data from one camera to another.
        /// </summary>
        /// <param name="src">Source data</param>
        /// <param name="dest">Destination data</param>
        private void CopyCameraData(Camera src, Camera dest)
        {
            if (dest == null)
            {
                return;
            }

            if (_forceClearSkyBox == false)
            {
                dest.clearFlags      = src.clearFlags;
                dest.backgroundColor = src.backgroundColor;

                if (src.clearFlags == CameraClearFlags.Skybox)
                {
                    Skybox srcSky = src.GetComponent(typeof(Skybox)) as Skybox;
                    Skybox dstSky = dest.GetComponent(typeof(Skybox)) as Skybox;

                    if (dstSky)
                    {
                        if (!srcSky || !srcSky.material)
                        {
                            dstSky.enabled = false;
                        }
                        else
                        {
                            dstSky.enabled  = true;
                            dstSky.material = srcSky.material;
                        }
                    }
                }
            }

            dest.farClipPlane  = src.farClipPlane;
            dest.nearClipPlane = src.nearClipPlane;
            dest.orthographic  = src.orthographic;

            if (XRSettings.enabled == false)
            {
                dest.fieldOfView = src.fieldOfView;
            }

            dest.aspect           = src.aspect;
            dest.orthographicSize = src.orthographicSize;
        }

        /// <summary>
        ///     Creates the internal resources if necessary.
        /// </summary>
        /// <param name="currentCamera">Render camera</param>
        /// <param name="reflectionCamera">Reflection camera</param>
        private void CreateResources(Camera currentCamera, out Camera reflectionCamera)
        {
            reflectionCamera = null;

            // Render textures

            if (_oldReflectionTextureSize != _textureSize)
            {
                CreateRenderTexture(ref _reflectionTextureLeft);
                CreateRenderTexture(ref _reflectionTextureRight);
                _oldReflectionTextureSize = _textureSize;
            }

            if (_reflectionTextureLeft == null)
            {
                CreateRenderTexture(ref _reflectionTextureLeft);
            }

            if (_reflectionTextureRight == null)
            {
                CreateRenderTexture(ref _reflectionTextureRight);
            }

            // Reflection camera

            _reflectionCameras.TryGetValue(currentCamera, out reflectionCamera);

            if (!reflectionCamera)
            {
                if (_renderCameraObject != null)
                {
                    reflectionCamera = _renderCameraObject.GetComponent<Camera>();
                }
                else
                {
                    _renderCameraObject = new GameObject($"{nameof(UxrPlanarReflectionUrp)} Camera", typeof(Camera), typeof(Skybox));
                    reflectionCamera    = _renderCameraObject.GetComponent<Camera>();
                }
                
                if (XRSettings.enabled == false)
                {
                    reflectionCamera.fieldOfView = 60.0f;
                }

                reflectionCamera.transform.SetPositionAndRotation(transform);
                reflectionCamera.enabled = true;
                //go.hideFlags                      = HideFlags.HideAndDontSave;
                _reflectionCameras[currentCamera] = reflectionCamera;

                if (_forceClearSkyBox)
                {
                    reflectionCamera.clearFlags = CameraClearFlags.Skybox;
                }

                reflectionCamera.enabled = false;
            }
        }

        /// <summary>
        ///     Creates a render texture.
        /// </summary>
        /// <param name="texture">Texture to create</param>
        private void CreateRenderTexture(ref RenderTexture texture)
        {
            if (texture)
            {
                DestroyImmediate(texture);
            }

            texture = new RenderTexture(_textureSize, _textureSize, 16);

            texture.name             = $"{nameof(UxrPlanarReflectionUrp)} Reflection";
            texture.isPowerOfTwo     = true;
            texture.hideFlags        = HideFlags.DontSave;
            texture.filterMode       = FilterMode.Trilinear;
            texture.autoGenerateMips = true;
            texture.useMipMap        = true; // Mip mapping can be used in shaders for blurring
        }

        /// <summary>
        ///     Given a plane point and normal in world coordinates, computes the plane in camera space.
        /// </summary>
        /// <param name="cam">Camera</param>
        /// <param name="offset">Clip plane offset</param>
        /// <param name="position">Point in plane</param>
        /// <param name="normal">Plane normal</param>
        /// <param name="sideSign">Plane side of the camera</param>
        /// <returns>Plane in camera space</returns>
        private Vector4 GetCameraSpacePlane(Camera cam, float offset, Vector3 position, Vector3 normal, float sideSign)
        {
            Vector3   offsetPos = position + normal * offset;
            Matrix4x4 m         = cam.worldToCameraMatrix;
            Vector3   cpos      = m.MultiplyPoint(offsetPos);
            Vector3   cnormal   = m.MultiplyVector(normal).normalized * sideSign;
            return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
        }

        #endregion

        #region Private Types & Data

        // Constants

        private static readonly int s_varReflectionTexLeft    = Shader.PropertyToID("_ReflectionTexLeft");
        private static readonly int s_varReflectionTexRight   = Shader.PropertyToID("_ReflectionTexRight");
        private static readonly int s_varReflectionMaxLodBias = Shader.PropertyToID("_ReflectionMaxLODBias");

        // Static

        private static   bool                       s_insideRendering;
        private static   Camera                     s_reflectionRequestCamera;
        private readonly Dictionary<Camera, Camera> _reflectionCameras = new Dictionary<Camera, Camera>();

        // Internal

        private GameObject    _renderCameraObject;
        private RenderTexture _reflectionTextureLeft;
        private RenderTexture _reflectionTextureRight;
        private int           _oldReflectionTextureSize;
        private Material[]    _materials;

        #endregion
    }
}