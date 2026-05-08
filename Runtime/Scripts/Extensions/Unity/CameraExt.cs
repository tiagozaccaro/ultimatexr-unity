// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CameraExt.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;

namespace UltimateXR.Extensions.Unity
{
    public static class CameraExt
    {
        #region Public Methods

        /// <summary>
        ///     Copies the settings of the source <see cref="Camera" /> component to target <see cref="Camera" />
        /// </summary>
        /// <param name="source">The source <see cref="Camera" /> whose properties will be copied</param>
        /// <param name="target">The target <see cref="Camera" /> component</param>
        /// <remarks>
        ///     This method only copies a selection of common camera properties.
        ///     Additional properties and components (e.g., AudioListener, post-processing effects)
        ///     must be copied separately if needed.
        /// </remarks>
        public static void CopySettingsTo(this Camera source, Camera target)
        {
            target.fieldOfView           = source.fieldOfView;
            target.clearFlags            = source.clearFlags;
            target.backgroundColor       = source.backgroundColor;
            target.cullingMask           = source.cullingMask;
            target.orthographic          = source.orthographic;
            target.orthographicSize      = source.orthographicSize;
            target.nearClipPlane         = source.nearClipPlane;
            target.farClipPlane          = source.farClipPlane;
            target.depth                 = source.depth;
            target.renderingPath         = source.renderingPath;
            target.allowHDR              = source.allowHDR;
            target.allowMSAA             = source.allowMSAA;
            target.targetTexture         = source.targetTexture;
            target.usePhysicalProperties = source.usePhysicalProperties;
            target.sensorSize            = source.sensorSize;
            target.focalLength           = source.focalLength;
        }

        /// <summary>
        ///     Converts a world position into screen space, applies a pixel offset,
        ///     and converts it back to world space at the same depth.
        ///     This ensures the offset is visually consistent on screen regardless
        ///     of the distance between the camera and the object.
        /// </summary>
        /// <param name="cam">
        ///     The camera used for the world-to-screen and screen-to-world conversions.
        /// </param>
        /// <param name="worldPos">
        ///     The original world position of the object.
        /// </param>
        /// <param name="pixelOffset">
        ///     The offset to apply in screen-space pixels.
        ///     Positive X moves right, positive Y moves up on the screen.
        /// </param>
        /// <returns>
        ///     A new world position shifted by the specified screen-space offset.
        /// </returns>
        public static Vector3 WorldPosWithScreenOffset(this Camera cam, Vector3 worldPos, Vector2 pixelOffset)
        {
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);

            screenPos.x += pixelOffset.x;
            screenPos.y += pixelOffset.y;

            return cam.ScreenToWorldPoint(screenPos);
        }

        /// <summary>
        ///     Captures the given camera's view into a <see cref="Texture2D" />.
        /// </summary>
        /// <param name="cam">Camera to capture from</param>
        /// <param name="width">Output width in pixels</param>
        /// <param name="height">Output height in pixels</param>
        /// <returns>Texture with the capture</returns>
        /// <remarks>The returned texture should be disposed using Object.Destroy() after use</remarks>
        public static Texture2D CaptureToPng(this Camera cam, int width, int height)
        {
            // Create a temporary RenderTexture
            RenderTexture rt = new RenderTexture(width, height, 24);
            cam.targetTexture = rt;

            // Render the camera’s view
            cam.Render();

            // Read pixels into a Texture2D
            RenderTexture.active = rt;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            tex.Apply();

            // Cleanup
            cam.targetTexture    = null;
            RenderTexture.active = null;
            Object.Destroy(rt);

            return tex;
        }

        #endregion
    }
}