// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectTransformUtilityExt.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
using UnityEngine.EventSystems;

namespace UltimateXR.Extensions.Unity.UI
{
    /// <summary>
    ///     <see cref="RectTransformUtility" /> extensions.
    /// </summary>
    public static class RectTransformUtilityExt
    {
        #region Public Methods

        /// <summary>
        ///     Returns the normalized (0–1) position of a pointer relative to a RectTransform,
        ///     where (0,0) is bottom-left and (1,1) is top-right of the rect, regardless of pivot, anchors, or scale.
        ///     The position is clamped to [0,1] even if the pointer is outside the bounds.
        /// </summary>
        /// <param name="rectTransform">The UI element to compute relative position against</param>
        /// <param name="eventData">Pointer event data from the UI event system</param>
        /// <param name="normalizedPosition">
        ///     The output normalized position. It's not clamped between 0 and 1 if the position is
        ///     outside the bounds.
        /// </param>
        /// <returns>True if the pointer was successfully converted to local space (regardless of being inside the bounds)</returns>
        public static bool GetNormalizedPointerPosition(RectTransform rectTransform, PointerEventData eventData, out Vector2 normalizedPosition)
        {
            normalizedPosition = Vector2.zero;

            // Convert screen point to local point
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform,
                                                                        eventData.position,
                                                                        eventData.pressEventCamera,
                                                                        out Vector2 localPoint))
            {
                // Get size of rect
                Rect rect = rectTransform.rect;

                // Convert from local space to normalized space
                float x = Mathf.InverseLerp(rect.xMin, rect.xMax, localPoint.x);
                float y = Mathf.InverseLerp(rect.yMin, rect.yMax, localPoint.y);

                normalizedPosition = new Vector2(x, y);
                return true;
            }

            return false;
        }

        #endregion
    }
}