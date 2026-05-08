// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrPooledEventArgs.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Threading;

namespace UltimateXR.Core.Events
{
    /// <summary>
    ///     Base class for pooled event argument objects of type <typeparamref name="T" />.
    ///     Instances are reused through a thread-safe round-robin pool to minimize runtime allocations.
    /// </summary>
    /// <typeparam name="T">
    ///     Concrete pooled event args type. This should be the derived type itself, following the CRTP pattern:
    ///     <code>sealed class MyEventArgs : UxrPooledEventArgs&lt;MyEventArgs&gt;</code>
    /// </typeparam>
    /// <remarks>
    ///     <see cref="UxrPooledEventArgs{T}" /> uses pooled and recyclable event argument instances to avoid runtime
    ///     allocations. Instances are reused automatically using a fixed-size round-robin pool and do not need to be
    ///     returned explicitly.
    ///     Instances are only guaranteed to remain valid during the event invocation or immediate processing scope in which
    ///     they are used. Do not store, cache, or use them later, because they may be reset and reused by the pool.
    /// </remarks>
    public abstract class UxrPooledEventArgs<T> : EventArgs where T : UxrPooledEventArgs<T>, new()
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the number of instances preallocated for the pool.
        /// </summary>
        public static int PoolCapacity => DefaultPoolSize;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Gets an instance from the pool.
        /// </summary>
        /// <returns>A pooled instance ready for use.</returns>
        public static T GetFromPool()
        {
            EnsureInitialized();

            int index = GetNextPoolIndex();
            T   instance = s_pool[index];

            instance.OnGet();

            return instance;
        }

        #endregion

        #region Event Trigger Methods

        /// <summary>
        ///     Called whenever an instance is taken from the pool.
        ///     Override this method to initialize the instance before use.
        /// </summary>
        protected virtual void OnGet()
        {
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Ensures that the pool is prewarmed only once.
        /// </summary>
        private static void EnsureInitialized()
        {
            if (Volatile.Read(ref s_initialized) != 0)
            {
                return;
            }

            if (Interlocked.CompareExchange(ref s_initialized, 1, 0) == 0)
            {
                T[] pool = new T[DefaultPoolSize];

                for (int i = 0; i < pool.Length; ++i)
                {
                    pool[i] = new T();
                }

                s_pool = pool;
            }
        }

        /// <summary>
        ///     Gets the next pool index using round-robin reuse.
        /// </summary>
        /// <returns>The index of the next instance to use.</returns>
        private static int GetNextPoolIndex()
        {
            int nextIndex = Interlocked.Increment(ref s_nextIndex);
            return (nextIndex & int.MaxValue) % DefaultPoolSize;
        }

        #endregion

        #region Private Types & Data

        private const int DefaultPoolSize = 100;

        private static T[] s_pool;
        private static int s_initialized;
        private static int s_nextIndex = -1;

        #endregion
    }
}