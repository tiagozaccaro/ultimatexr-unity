#if DISABLED
// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrCameraBoundayGrid.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Avatar;
using UltimateXR.Core.Components;
using UnityEngine;

namespace UltimateXR.CameraUtils
{
    /// <summary>
    ///     VRBoundaryGrid — Auto-initializing VR wall proximity boundary system.
    ///     Drop this script anywhere in your project. No scene setup required.
    ///     Automatically creates a grid overlay when the player's head approaches or clips through a wall.
    ///     REQUIREMENTS:
    ///     - AI Navigation package (Unity 6.2)
    ///     - XR Core Utilities package
    ///     - Add shader "VR/BoundaryGrid" to Edit > Project Settings > Graphics > Always Included Shaders
    /// </summary>
    public class UxrCameraBoundaryGrid : UxrComponent<UxrCameraBoundaryGrid>
    {
        #region Inspector Properties/Serialized Fields

        // -------------------------------------------------------
        // Inspector Settings
        // -------------------------------------------------------

        [Header("Wall Detection")] [Tooltip("Layers considered as walls for boundary detection.")] public LayerMask wallLayers = Physics.DefaultRaycastLayers;

        [Tooltip("Radius around the camera head to detect wall overlaps (inside wall detection).")] public float detectionRadius = 0.3f;

        [Tooltip("Distance at which the grid starts fading in when approaching a wall.")] public float warningDistance = 0.6f;

        [Header("Fade Settings")] [Tooltip("How fast the grid fades in and out.")] public float fadeSpeed = 3.0f;

        [Tooltip("Maximum opacity of the grid overlay (0 = invisible, 1 = fully opaque).")] [Range(0f, 1f)] public float maxAlpha = 0.85f;

        [Header("Grid Appearance")] [Tooltip("Color of the grid lines.")] public Color gridColor = new Color(0f, 0.8f, 1f, 1f);

        [Tooltip("Scale of the grid lines. Higher = smaller cells.")] public float gridScale = 10f;

        [Tooltip("Thickness of the grid lines.")] [Range(0.01f, 0.2f)] public float gridLineWidth = 0.05f;

        [Tooltip("Size of the quad covering the camera FOV.")] public float quadSize = 10f;

        [Header("Debug")] public bool  debugForceShow     = false; // toggle in Inspector at runtime
        public                   float debugAlphaOverride = -1f;   // set 0-1 in Inspector to test gradient        

        #endregion

        #region Public Methods

        // -------------------------------------------------------
        // Public API — optional runtime control
        // -------------------------------------------------------

        /// <summary>Returns true if the camera is currently inside a wall collider.</summary>
        public bool IsInsideWall()
        {
            return Physics.OverlapSphere(_vrCamera.transform.position, detectionRadius, wallLayers).Length > 0;
        }

        /// <summary>Sets the grid line color at runtime.</summary>
        public void SetGridColor(Color color)
        {
            gridColor = color;
            if (_gridMaterial != null)
            {
                _gridMaterial.SetColor("_GridColor", color);
            }
        }

        /// <summary>Sets the grid cell scale at runtime.</summary>
        public void SetGridScale(float scale)
        {
            gridScale = scale;
            if (_gridMaterial != null)
            {
                _gridMaterial.SetFloat("_GridScale", scale);
            }
        }

        /// <summary>Sets the wall detection radius at runtime.</summary>
        public void SetDetectionRadius(float radius)
        {
            detectionRadius = radius;
        }

        /// <summary>Sets the warning fade distance at runtime.</summary>
        public void SetWarningDistance(float distance)
        {
            warningDistance = distance;
        }

        /// <summary>Forces the grid fully visible regardless of wall proximity.</summary>
        public void ForceShow()
        {
            _currentAlpha = maxAlpha;
        }

        /// <summary>Forces the grid hidden regardless of wall proximity.</summary>
        public void ForceHide()
        {
            _currentAlpha = 0f;
        }

        #endregion

        #region Unity

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (_gridMaterial != null)
            {
                Destroy(_gridMaterial);
            }
        }

        // -------------------------------------------------------
        // Lifecycle
        // -------------------------------------------------------

        protected override void OnEnable()
        {
            base.OnEnable();

            UxrAvatar.LocalAvatarStarted += UxrAvatar_LocalAvatarStarted;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            
            UxrAvatar.LocalAvatarStarted -= UxrAvatar_LocalAvatarStarted;
        }

        protected void Update()
        {
            if (!_initialized || _vrCamera == null) return;

            float targetAlpha;

            // Debug overrides
            if (debugForceShow)
                targetAlpha = maxAlpha;
            else if (debugAlphaOverride >= 0f)
                targetAlpha = debugAlphaOverride; // manually scrub alpha in Inspector
            else
                targetAlpha = CalculateTargetAlpha();

            _currentAlpha = Mathf.Lerp(_currentAlpha, targetAlpha, Time.deltaTime * fadeSpeed);
            _gridMaterial.SetFloat("_Alpha", _currentAlpha);
        }

        #endregion

        #region Event Handling Methods

        private void UxrAvatar_LocalAvatarStarted(object sender, UxrAvatarStartedEventArgs e)
        {
            Camera camera = e.Avatar.GetComponentInChildren<Camera>();
            if (camera != null)
            {
                _vrCamera      = camera;
                _floorPosition = new Vector3(e.Avatar.gameObject.transform.position.x, e.Avatar.gameObject.transform.position.y, e.Avatar.gameObject.transform.position.z);

                if (!TryInitializeGridQuad())
                {
                    Debug.LogError("[VRBoundaryGrid] Could not initialize grid quad.");
                    return;
                }

                _initialized = true;
                Debug.Log("[VRBoundaryGrid] Initialized successfully.");              
            }
        }

        #endregion

        #region Private Methods

        // -------------------------------------------------------
        // Auto-Inject — fires on every scene load, zero user setup
        // -------------------------------------------------------

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoInitialize()
        {
            // Prevent duplicates across scene loads
            if (FindFirstObjectByType<UxrCameraBoundaryGrid>() != null)
            {
                return;
            }

            GameObject go = new GameObject("[VRBoundaryGrid]");
            DontDestroyOnLoad(go);
            go.AddComponent<UxrCameraBoundaryGrid>();
        }

        // -------------------------------------------------------
        // Initialization Helpers
        // -------------------------------------------------------

        private bool TryInitializeCamera()
        {
            if (_vrCamera == null)
            {
                Debug.LogError("[VRBoundaryGrid] No camera found.");
                return false;
            }

            return true;
        }

        private bool TryInitializeGridQuad()
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogError($"[VRBoundaryGrid] Shader '{ShaderName}' not found.\n" +
                               "Go to Edit > Project Settings > Graphics > Always Included Shaders and add 'VR/BoundaryGrid'.");
                return false;
            }

            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name             = "[BoundaryGridQuad]";
            quad.transform.parent = _vrCamera.transform;

            // Place at avatar floor position relative to camera
            Vector3 localFloor = _vrCamera.transform.InverseTransformPoint(_floorPosition);
            quad.transform.localPosition = new Vector3(0f, localFloor.y, 0f);
            quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f); // flat/horizontal
            quad.transform.localScale    = new Vector3(quadSize, quadSize, 1f);

            Destroy(quad.GetComponent<Collider>());


            // Build material
            _gridMaterial = new Material(shader);
            _gridMaterial.SetColor("_GridColor", gridColor);
            _gridMaterial.SetFloat("_GridScale", gridScale);
            _gridMaterial.SetFloat("_GridWidth", gridLineWidth);
            _gridMaterial.SetFloat("_Alpha",     0f);

            _gridRenderer          = quad.GetComponent<MeshRenderer>();
            _gridRenderer.material = _gridMaterial;

            // Render on top of everything
            _gridRenderer.sortingOrder = 999;

            return true;
        }

        // -------------------------------------------------------
        // Detection Logic
        // -------------------------------------------------------

        private float CalculateTargetAlpha()
        {
            // Case 1: Head is fully inside a wall — full alpha immediately
            Collider[] overlaps = Physics.OverlapSphere(_vrCamera.transform.position, detectionRadius, wallLayers);
            if (overlaps.Length > 0)
            {
                return maxAlpha;
            }

            // Case 2: Head is approaching a wall — fade based on proximity
            if (Physics.SphereCast(_vrCamera.transform.position,
                                   detectionRadius,
                                   _vrCamera.transform.forward,
                                   out RaycastHit hit,
                                   warningDistance,
                                   wallLayers))
            {
                float proximity = 1f - Mathf.Clamp01(hit.distance / warningDistance);
                return proximity * maxAlpha;
            }

            // Case 3: No wall nearby — fade out
            return 0f;
        }

        #endregion

        #region Private Types & Data

        // Inline shader source — embedded so no .shader file is needed
        private const string ShaderName = "VR/BoundaryGrid";

        // -------------------------------------------------------
        // Private State
        // -------------------------------------------------------

        private Camera       _vrCamera;
        private MeshRenderer _gridRenderer;
        private Material     _gridMaterial;
        private float        _currentAlpha;
        private bool         _initialized = false;
        private Vector3      _floorPosition;

        #endregion
    }
}
#endif