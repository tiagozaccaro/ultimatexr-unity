// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RectTransformExt.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UnityEngine;
#if ULTIMATEXR_USE_UNITYINPUTSYSTEM_SDK
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Utilities;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;
#endif

namespace UltimateXR.Extensions.Unity.UI
{
    public static class RectTransformExt
    {
        /// <summary>
        ///     Returns true if the given RectTransform is currently being pressed or was clicked/tapped this frame by mouse or
        ///     touch.
        ///     Works with both legacy and new Input Systems depending on the ULTIMATEXR_USE_UNITYINPUTSYSTEM_SDK define.
        /// </summary>
        public static bool IsPressed(this RectTransform rectTransform, Camera uiCamera)
        {
            if (rectTransform == null || uiCamera == null)
            {
                return false;
            }

#if ULTIMATEXR_USE_UNITYINPUTSYSTEM_SDK
            // New input system
            // Mouse
            if (Mouse.current != null)
            {
                bool mouseActive = Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame;

                if (mouseActive)
                {
                    Vector2 pos = Mouse.current.position.ReadValue();
                    if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, uiCamera))
                    {
                        return true;
                    }
                }
            }

            // Touch
            if (Touchscreen.current != null)
            {
                ReadOnlyArray<TouchControl> touches = Touchscreen.current.touches;

                foreach (TouchControl touch in touches)
                {
                    TouchPhase phase = touch.phase.ReadValue();

                    if (phase is TouchPhase.Began or TouchPhase.Moved or TouchPhase.Stationary)
                    {
                        Vector2 pos = touch.position.ReadValue();
                        if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, uiCamera))
                        {
                            return true;
                        }
                    }
                }
            }
#else
            // Mouse
            if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            {
                Vector2 pos = Input.mousePosition;
                if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, pos, uiCamera))
                {
                    return true;
                }
            }

            // Touch
            for (int i = 0; i < Input.touchCount; i++)
            {
                Touch touch = Input.GetTouch(i);
                if (touch.phase == TouchPhase.Began || touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    if (RectTransformUtility.RectangleContainsScreenPoint(rectTransform, touch.position, uiCamera))
                    {
                        return true;
                    }
                }
            }
#endif

            return false;
        }
    }
}