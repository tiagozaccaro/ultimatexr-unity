// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ThreadSafePlayModeTracker.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UltimateXR.Core.Threading
{
    /// <summary>
    ///     Provides a thread-safe way to query the current Play Mode or runtime state of the Unity application.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         This class maintains a cached copy of <see cref="Application.isPlaying" /> that can be safely accessed
    ///         from background threads without touching Unity's main-thread APIs.
    ///     </para>
    ///     <para>
    ///         In the Editor, the value automatically updates when entering or exiting Play Mode. In Player builds,
    ///         it initializes once at startup and updates when the application quits.
    ///     </para>
    ///     <para>
    ///         Useful for worker threads, async tasks, or background systems that cannot directly call Unity APIs.
    ///     </para>
    /// </remarks>
#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static class ThreadSafePlayModeTracker
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets a cached, thread-safe indication of whether the Unity application is currently playing.
        /// </summary>
        /// <remarks>
        ///     Unlike <see cref="Application.isPlaying" />, this property can be accessed from any thread safely.
        ///     The value is updated automatically from the main thread whenever Play Mode changes.
        /// </remarks>
        public static bool IsPlaying
        {
            get
            {
                lock (s_lock)
                {
                    return s_isPlaying;
                }
            }
            private set
            {
                lock (s_lock)
                {
                    s_isPlaying = value;
                }
            }
        }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Static constructor ensures initialization when the Unity Editor loads scripts or the Player starts.
        /// </summary>
#if UNITY_EDITOR
        static ThreadSafePlayModeTracker()
#else
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void ThreadSafePlayModeTrackerInitialize()
#endif
        {
            // Initialize value immediately
            IsPlaying = Application.isPlaying;

#if UNITY_EDITOR
            // Subscribe to Play Mode state changes to update the cached value
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // Also refresh during editor updates (covers domain reloads or manual script recompiles)
            EditorApplication.update += () => IsPlaying = Application.isPlaying;
#else
            // In player builds, track play/quit transitions
            Application.quitting += OnApplicationQuit;
#endif
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Forces an immediate refresh of the cached <see cref="IsPlaying" /> value from the main thread.
        /// </summary>
        /// <remarks>
        ///     This method should be called only from the main thread if you suspect desynchronization.
        ///     In normal operation, automatic updates keep the value current.
        /// </remarks>
        public static void RefreshNow()
        {
            if (!Application.isPlaying && !Application.isEditor)
            {
                return;
            }

            IsPlaying = Application.isPlaying;
        }

        #endregion

        #region Unity

#if !UNITY_EDITOR
        /// <summary>
        ///     Called automatically when the Player application is quitting.
        /// </summary>
        private static void OnApplicationQuit()
        {
            IsPlaying = false;
        }
#endif

        #endregion

        #region Event Handling Methods

#if UNITY_EDITOR
        /// <summary>
        ///     Called whenever the Editor changes Play Mode state.
        /// </summary>
        /// <param name="stateChange">The new state of Play Mode.</param>
        private static void OnPlayModeStateChanged(PlayModeStateChange stateChange)
        {
            IsPlaying = stateChange == PlayModeStateChange.EnteredPlayMode;
        }
#endif

        #endregion

        #region Private Types & Data

        private static readonly object s_lock = new();
        private static volatile bool   s_isPlaying;

        #endregion
    }
}