// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WristConnectionRays.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UnityEngine;

namespace UltimateXR.Mechanics.CyborgAvatar
{
    /// <summary>
    ///     Component that drives the two devices that connect the Cyborg wrist to the arm.
    /// </summary>
    public partial class WristConnectionRays : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] private float               _gradientPosStart1 = 0.15f;
        [SerializeField] private float               _gradientPosStart2 = 0.2f;
        [SerializeField] private float               _gradientPosEnd1   = 0.8f;
        [SerializeField] private float               _gradientPosEnd2   = 0.85f;
        [SerializeField] private Material            _rayMaterial;
        [SerializeField] private bool                _useMaterialNoiseParameters;
        [SerializeField] private Transform           _src;
        [SerializeField] private Transform           _dst;
        [SerializeField] private List<RayProperties> _rays;

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the component.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _positions = new []
            {
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 0.0f, 0.0f)
            };
            
            _colorGradient = new Gradient();

            _colorKeys = new[]
                         {
                             new GradientColorKey(Color.white, 0.0f),
                             new GradientColorKey(Color.white, _gradientPosStart1),
                             new GradientColorKey(Color.white, _gradientPosStart2),
                             new GradientColorKey(Color.white, _gradientPosEnd1),
                             new GradientColorKey(Color.white, _gradientPosEnd2),
                             new GradientColorKey(Color.white, 1.0f)
                         };

            _alphaKeys = new[]
                         {
                             new GradientAlphaKey(0.0f, 0.0f),
                             new GradientAlphaKey(0.0f, _gradientPosStart1),
                             new GradientAlphaKey(1.0f, _gradientPosStart2),
                             new GradientAlphaKey(1.0f, _gradientPosEnd1),
                             new GradientAlphaKey(0.0f, _gradientPosEnd2),
                             new GradientAlphaKey(0.0f, 1.0f)
                         };
            
            _colorGradient.colorKeys = _colorKeys;
            _colorGradient.alphaKeys = _alphaKeys;
        }

        /// <summary>
        ///     Subscribes to avatar update event.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrManager.AvatarsUpdated += UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Unsubscribes from avatar update events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrManager.AvatarsUpdated -= UxrManager_AvatarsUpdated;
        }

        /// <summary>
        ///     Initializes the component.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            Create(_src.position, _dst.position);
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Updates the component.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            if (_src != null && _dst != null)
            {
                UpdateRays(_src.position, _dst.position);
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Creates the connections.
        /// </summary>
        /// <param name="src">Source position</param>
        /// <param name="dst">Destination position</param>
        private void Create(Vector3 src, Vector3 dst)
        {
            foreach (RayProperties ray in _rays)
            {
                ray.GameObject = new GameObject("Ray");
                ray.GameObject.transform.SetParent(transform, true);
                ray.GameObject.transform.localPosition = Vector3.zero;
                ray.GameObject.transform.localRotation = Quaternion.identity;

                ray.LineRenderer          = ray.GameObject.AddComponent<LineRenderer>();
                ray.LineRenderer.material = _rayMaterial;

                if (_useMaterialNoiseParameters)
                {
                    ray.LineRenderer.material.SetFloat(DistortTimeStartVarName, Random.value * 10000.0f);
                }

                ray.LineRenderer.textureMode = LineTextureMode.Stretch;
                ray.OffsetXY                 = Random.insideUnitCircle;
            }

            UpdateRays(src, dst);
        }

        /// <summary>
        ///     Updates the connection rays.
        /// </summary>
        /// <param name="src">Source position</param>
        /// <param name="dst">End position</param>
        private void UpdateRays(Vector3 src, Vector3 dst)
        {
            foreach (RayProperties ray in _rays)
            {
                if (ray.GameObject == null)
                {
                    continue;
                }

                ray.GameObject.transform.position = src;
                ray.GameObject.transform.LookAt(dst);

                float rayLength = Vector3.Distance(src, dst) / ray.LineRenderer.transform.lossyScale.z;

                _positions[1].z = rayLength * _gradientPosStart1;
                _positions[2].z = rayLength * _gradientPosStart2;
                _positions[3].z = rayLength * _gradientPosEnd1;
                _positions[4].z = rayLength * _gradientPosEnd2;
                _positions[5].z = rayLength;

                Vector3 offset = (ray.GameObject.transform.right * ray.OffsetXY.x + ray.GameObject.transform.up * ray.OffsetXY.y).normalized * ray.Offset;

                for (int pos = 0; pos < _positions.Length; ++pos)
                {
                    _positions[pos] = ray.LineRenderer.transform.InverseTransformPoint(ray.GameObject.transform.TransformPoint(_positions[pos]) + offset);
                }

                ray.LineRenderer.useWorldSpace = false;
                ray.LineRenderer.positionCount = 6;
                ray.LineRenderer.SetPositions(_positions);
                ray.LineRenderer.startWidth     = ray.Thickness;
                ray.LineRenderer.endWidth       = ray.Thickness;
                ray.LineRenderer.material.color = ray.Color;

                if (ray.LineRenderer.material.mainTexture != null)
                {
                    ray.LineRenderer.material.mainTextureScale = new Vector2(rayLength / ray.Thickness / (ray.LineRenderer.material.mainTexture.width / (float)ray.LineRenderer.material.mainTexture.height), 1.0f);
                }

                _colorKeys[0].color.a = 0.0f;
                _colorKeys[1].color.a = _gradientPosStart1;
                _colorKeys[2].color.a = _gradientPosStart2;
                _colorKeys[3].color.a = _gradientPosEnd1;
                _colorKeys[4].color.a = _gradientPosEnd2;
                _colorKeys[5].color.a = 1.0f;

                _alphaKeys[0].time = 0.0f;
                _alphaKeys[1].time = _gradientPosStart1;
                _alphaKeys[2].time = _gradientPosStart2;
                _alphaKeys[3].time = _gradientPosEnd1;
                _alphaKeys[4].time = _gradientPosEnd2;
                _alphaKeys[5].time = 1.0f;

                _colorGradient.colorKeys = _colorKeys;
                _colorGradient.alphaKeys = _alphaKeys;

                ray.LineRenderer.colorGradient = _colorGradient;
            }
        }

        #endregion

        #region Private Types & Data

        private const string DistortTimeStartVarName = "_DistortTimeStart";

        private Vector3[]          _positions     = new Vector3[6];
        private Gradient           _colorGradient = new Gradient();
        private GradientColorKey[] _colorKeys     = new GradientColorKey[6];
        private GradientAlphaKey[] _alphaKeys     = new GradientAlphaKey[6];

        #endregion
    }
}