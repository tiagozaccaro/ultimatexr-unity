// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrUniqueIdImplementer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UltimateXR.Core.Components;
using UltimateXR.Core.Settings;
using UltimateXR.Extensions.Unity;
using UnityEngine;

namespace UltimateXR.Core.Unique
{
    /// <summary>
    ///     Base class for <see cref="UxrUniqueIdImplementer{T}" />.
    /// </summary>
    public abstract class UxrUniqueIdImplementer
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets all registered components that implement the <see cref="IUxrUniqueId" /> interface.
        ///     This includes all <see cref="UxrComponent" /> components and all user custom classes that implement
        ///     <see cref="IUxrUniqueId" /> using <see cref="UxrUniqueIdImplementer{T}" />.
        /// </summary>
        public static IEnumerable<IUxrUniqueId> AllComponents
        {
            get
            {
                foreach (KeyValuePair<Type, UxrUniqueIdImplementer> pair in s_implementerTypes)
                {
                    foreach (IUxrUniqueId unique in pair.Value.GetAllComponentsInternal())
                    {
                        yield return unique;
                    }
                }
            }
        }

        /// <summary>
        ///     Gets or sets the unique id that was used by <see cref="UxrUniqueIdImplementer{T}.CombineUniqueId" />.
        /// </summary>
        public Guid CombineIdSource { get; set; }

        /// <summary>
        ///     Gets or sets the original unique Id.
        /// </summary>
        public Guid OriginalUniqueId { get; set; }

        /// <summary>
        ///     Gets or sets the component that was initialized.
        /// </summary>
        public IUxrUniqueId InitializedComponent { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Generates a new unique Id.
        /// </summary>
        /// <returns>Unique Id</returns>
        public static Guid GetNewUniqueId()
        {
            return Guid.NewGuid();
        }

        /// <summary>
        ///     Finds a component using its unique ID. The component should implement the <see cref="IUxrUniqueId" /> interface,
        ///     such as <see cref="UxrComponent" />.
        /// </summary>
        /// <param name="id">Component's unique ID</param>
        /// <param name="component">Returns the component or null if the ID wasn't found</param>
        /// <returns>Whether the given ID was found and a component is returned</returns>
        public static bool TryGetComponentById(Guid id, out IUxrUniqueId component)
        {
            // Iterate over component types. By default, it will contain the UxrComponent implementer only.

            foreach (KeyValuePair<Type, UxrUniqueIdImplementer> pair in s_implementerTypes)
            {
                if (pair.Value.TryGetComponentByIdInternal(id, out component))
                {
                    return true;
                }
            }

            component = null;
            return false;
        }

        /// <summary>
        ///     Finds a component using its unique ID. The component should implement the <see cref="IUxrUniqueId" /> interface,
        ///     such as <see cref="UxrComponent" />.
        /// </summary>
        /// <param name="id">Component's unique ID</param>
        /// <param name="component">Returns the component or null if the ID wasn't found</param>
        /// <returns>Whether the given ID was found and a component is returned</returns>
        public static bool TryGetComponentById<T>(Guid id, out T component) where T : Component, IUxrUniqueId
        {
            foreach (KeyValuePair<Type, UxrUniqueIdImplementer> pair in s_implementerTypes)
            {
                if (pair.Value.TryGetComponentByIdInternal(id, out IUxrUniqueId unique))
                {
                    component = unique as T;

                    if (component == null)
                    {
                        if (UxrGlobalSettings.Instance.LogLevelCore >= UxrLogLevel.Errors)
                        {
                            Debug.LogError($"{UxrConstants.CoreModule} {nameof(UxrUniqueIdImplementer)}.{nameof(TryGetComponentById)} type mismatch with ID {id}. Expected type is ({typeof(T).Name}) and actual type is ({unique.GetType().Name}).");
                        }

                        return false;
                    }
                }
            }

            component = default;
            return false;
        }

        /// <summary>
        ///     Creates the debug information for a component.
        /// </summary>
        /// <param name="component">Component to get the debug information for</param>
        /// <param name="debugLevel">The debug level</param>
        /// <returns>A string with the debug information, or null for <see cref="UxrUniqueIdDebugLevel.NoDebug" /></returns>
        public static string CreateUniqueIdComponentDebugInfo(IUxrUniqueId component, UxrUniqueIdDebugLevel debugLevel)
        {
            if (component == null)
            {
                return null;
            }

            switch (debugLevel)
            {
                case UxrUniqueIdDebugLevel.NoDebug:                    break;
                case UxrUniqueIdDebugLevel.ObjectName:                 return component.GameObject.name;
                case UxrUniqueIdDebugLevel.ObjectNameAndComponentType: return component.GameObject.name + " (" + component.GetType().Name + ")";
                case UxrUniqueIdDebugLevel.ScenePath:                  return component.Component.GetPathUnderScene();
                case UxrUniqueIdDebugLevel.UniqueScenePath:            return component.Component.GetUniqueScenePath();
            }

            return null;
        }

        /// <summary>
        ///     Gets whether a component type should not be registered in UltimateXR systems.
        /// </summary>
        /// <param name="component">The component to check</param>
        /// <returns>Whether the component type has <see cref="UxrDontRegisterAttribute" /></returns>
        public static bool HasDontRegisterAttribute(IUxrUniqueId component)
        {
            if (component == null)
            {
                return false;
            }

            Type componentType = component.GetType();

            if (s_componentTypeDontRegister.TryGetValue(componentType, out bool dontRegister))
            {
                return dontRegister;
            }

            dontRegister = Attribute.IsDefined(componentType, typeof(UxrDontRegisterAttribute), true);
            s_componentTypeDontRegister[componentType] = dontRegister;
            return dontRegister;
        }

        /// <summary>
        ///     Gets whether a component type should be excluded from StateSync and StateSave systems while still
        ///     remaining registered in unique ID/component registries.
        /// </summary>
        /// <param name="component">The component to check</param>
        /// <returns>Whether the component type has <see cref="UxrDontSyncAttribute" /></returns>
        public static bool HasDontSyncAttribute(IUxrUniqueId component)
        {
            if (component == null)
            {
                return false;
            }

            Type componentType = component.GetType();

            if (s_componentTypeDontSync.TryGetValue(componentType, out bool dontSync))
            {
                return dontSync;
            }

            dontSync = Attribute.IsDefined(componentType, typeof(UxrDontSyncAttribute), true);
            s_componentTypeDontSync[componentType] = dontSync;
            return dontSync;
        }

        /// <summary>
        ///     Tries to get the debug information of a Guid whose <see cref="IUxrUniqueId" /> could not be found, using the
        ///     registered <see cref="IUxrUniqueIdDebugInfoResolver" /> objects.
        /// </summary>
        /// <param name="guid">The guid</param>
        /// <param name="debugInfo">Will contain the debug information if the method returns true</param>
        /// <returns>Whether debug information for the given ID was found</returns>
        public static bool TryGetUniqueIdComponentDebugInfo(Guid guid, out string debugInfo)
        {
            debugInfo = null;

            foreach (IUxrUniqueIdDebugInfoResolver resolver in s_debugInfoResolvers)
            {
                if (resolver != null && resolver.TryResolveUniqueIdDebugInfo(guid, out debugInfo))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        ///     Tries to get a component using the debug information of a Guid whose <see cref="IUxrUniqueId" /> could not be found, using the
        ///     registered <see cref="IUxrUniqueIdComponentResolver" /> objects.
        /// </summary>
        /// <param name="guid">The guid</param>
        /// <param name="debugInfo">The debug information, which may contain the name or full  path</param>
        /// <returns>The component, if it could be found</returns>
        public static IUxrUniqueId TryGetUniqueIdComponentUsingDebugInfo(Guid guid, string debugInfo)
        {
            foreach (IUxrUniqueIdComponentResolver resolver in s_componentResolvers)
            {
                if (resolver != null && !string.IsNullOrEmpty(debugInfo))
                {
                    IUxrUniqueId component = resolver.TryResolveComponentUsingDebugInfo(guid, debugInfo);

                    if (component != null)
                    {
                        return component;
                    }
                }
            }

            return null;
        }

        /// <summary>
        ///     Registers a debug info resolver. A debug info resolver can try to get the debug information, such as the name or
        ///     full path of a component, based on the unique ID when the component instance can not be found using
        ///     TryGetComponentById().
        /// </summary>
        /// <param name="resolver">Debug info resolver</param>
        public static void RegisterDebugInfoResolver(IUxrUniqueIdDebugInfoResolver resolver)
        {
            s_debugInfoResolvers.Add(resolver);
        }

        /// <summary>
        ///     Unregisters a debug info resolver registered using <see cref="RegisterDebugInfoResolver" />.
        /// </summary>
        /// <param name="resolver">Debug info resolver</param>
        public static void UnregisterDebugInfoResolver(IUxrUniqueIdDebugInfoResolver resolver)
        {
            if (s_debugInfoResolvers.Contains(resolver))
            {
                s_debugInfoResolvers.Remove(resolver);
            }
        }

        /// <summary>
        ///     Registers a component resolver. A component resolver may try to resolve a component when the id
        ///     could not be found, given the debug info is present, such as the name or full path of a component.
        /// </summary>
        /// <param name="resolver">Component resolver</param>
        public static void RegisterComponentResolver(IUxrUniqueIdComponentResolver resolver)
        {
            s_componentResolvers.Add(resolver);
        }

        /// <summary>
        ///     Unregisters a component resolver registered using <see cref="RegisterComponentResolver" />.
        /// </summary>
        /// <param name="resolver">Component resolver</param>
        public static void UnregisterComponentResolver(IUxrUniqueIdComponentResolver resolver)
        {
            if (s_componentResolvers.Contains(resolver))
            {
                s_componentResolvers.Remove(resolver);
            }
        }

        #endregion

        #region Protected Methods

        /// <summary>
        ///     Tries to find a component using the unique ID in an <see cref="UxrUniqueIdImplementer{T}" /> implementation.
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="component">Returns the component</param>
        /// <returns></returns>
        protected abstract bool TryGetComponentByIdInternal(Guid id, out IUxrUniqueId component);

        /// <summary>
        ///     Returns all components of the type in an <see cref="UxrUniqueIdImplementer{T}" /> implementation.
        /// </summary>
        /// <returns>Returns all registered components of the type in an <see cref="UxrUniqueIdImplementer{T}" /> implementation</returns>
        protected abstract IEnumerable<IUxrUniqueId> GetAllComponentsInternal();

        /// <summary>
        ///     Registers an implementer type if it wasn't already registered. This allows to statically get components of custom
        ///     types based on their ID using <see cref="TryGetComponentById" />.
        ///     Also registers the implementer so that it can be retrieved using the component as key.
        /// </summary>
        /// <param name="targetComponent">The target component</param>
        /// <param name="implementer">The implementer</param>
        protected void RegisterImplementer<T>(T targetComponent, UxrUniqueIdImplementer<T> implementer) where T : Component, IUxrUniqueId
        {
            if (!s_implementerTypes.ContainsKey(typeof(T)))
            {
                s_implementerTypes.Add(typeof(T), implementer);
            }

            if (!s_allImplementers.ContainsKey(targetComponent))
            {
                s_allImplementers.Add(targetComponent, implementer);
            }
        }

        /// <summary>
        ///     Unregisters the implementer.
        /// </summary>
        /// <param name="targetComponent">The target component</param>
        /// <param name="implementer">The implementer</param>
        /// <typeparam name="T">The component type</typeparam>
        protected void UnregisterImplementer<T>(T targetComponent, UxrUniqueIdImplementer<T> implementer) where T : Component, IUxrUniqueId
        {
            s_allImplementers.Remove(targetComponent);
        }

        #endregion

        #region Protected Types & Data

        /// <summary>
        ///     Static dictionary of path collisions to ensure unique IDs are generated.
        /// </summary>
        protected static readonly Dictionary<Guid, int> s_idCollisions = new Dictionary<Guid, int>();

        #endregion

        #region Private Types & Data

        /// <summary>
        ///     Contains the first implementer registered for each type, including <see cref="UxrComponent" /> and any user custom
        ///     types.
        /// </summary>
        private static readonly Dictionary<Type, UxrUniqueIdImplementer> s_implementerTypes = new Dictionary<Type, UxrUniqueIdImplementer>();

        /// <summary>
        ///     Contains the implementer for each component.
        /// </summary>
        private static readonly Dictionary<IUxrUniqueId, UxrUniqueIdImplementer> s_allImplementers = new Dictionary<IUxrUniqueId, UxrUniqueIdImplementer>();

        /// <summary>
        ///     Contains the registered debug info resolvers. See <see cref="RegisterDebugInfoResolver" />.
        /// </summary>
        private static readonly HashSet<IUxrUniqueIdDebugInfoResolver> s_debugInfoResolvers = new HashSet<IUxrUniqueIdDebugInfoResolver>();

        /// <summary>
        ///     Contains the registered component resolvers. See <see cref="RegisterComponentResolver" />.
        /// </summary>
        private static readonly HashSet<IUxrUniqueIdComponentResolver> s_componentResolvers = new HashSet<IUxrUniqueIdComponentResolver>();

        /// <summary>
        ///     Cache with whether component types are decorated using <see cref="UxrDontRegisterAttribute" />.
        /// </summary>
        private static readonly Dictionary<Type, bool> s_componentTypeDontRegister = new Dictionary<Type, bool>();

        /// <summary>
        ///     Cache with whether component types are decorated using <see cref="UxrDontSyncAttribute" />.
        /// </summary>
        private static readonly Dictionary<Type, bool> s_componentTypeDontSync = new Dictionary<Type, bool>();

        #endregion
    }
}