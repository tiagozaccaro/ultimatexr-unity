// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrSdkManager.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;

namespace UltimateXR.Editor.Sdks
{
    /// <summary>
    ///     Static class that will store all SDK locators through auto-registration. Each <see cref="UxrSdkLocator" />
    ///     implementation will register itself through this class.
    /// </summary>
    public static partial class UxrSdkManager
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the global list of registered SDK locators. SDK locators will auto-register every time Unity is installed or
        ///     the project is updated.
        /// </summary>
        public static IReadOnlyList<UxrSdkLocator> SDKLocators => s_locators;

        #endregion

        #region Public Methods

        /// <summary>
        ///     Registers a new SDK locator if it is not already registered.
        ///     The locator then is used to update the project symbols adding the necessary symbols if the SDK was found or
        ///     removing them if it wasn't.
        /// </summary>
        /// <param name="locator">SDK locator interface</param>
        public static void RegisterLocator(UxrSdkLocator locator)
        {
            if (s_locators == null)
            {
                s_locators = new List<UxrSdkLocator>();
            }

            // Check if it was already registered

            bool locatorAlreadyRegistered = false;

            foreach (UxrSdkLocator registeredLocator in s_locators)
            {
                if (string.Equals(registeredLocator.Name, locator.Name, StringComparison.Ordinal))
                {
                    locatorAlreadyRegistered = true;
                    break;
                }
            }

            // Register if not found

            if (locatorAlreadyRegistered == false)
            {
                s_locators.Add(locator);
            }

            // Try to locate SDK and set up symbols

            locator.TryLocate();

            if (!locator.IsPackage)
            {
                SetupSymbols(locator);
            }
        }

        /// <summary>
        ///     Checks if a given SDK is present and available.
        /// </summary>
        /// <typeparam name="T">Type of the SDK locator</typeparam>
        /// <returns>True if installed and available, false if not</returns>
        public static bool IsAvailable<T>() where T : UxrSdkLocator
        {
            if (s_locators != null)
            {
                foreach (UxrSdkLocator locator in s_locators)
                {
                    if (locator.GetType() == typeof(T))
                    {
                        return locator.CurrentState == UxrSdkLocator.State.Available;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Checks if a given SDK is present and available.
        /// </summary>
        /// <param name="name">The SDK name (looks to match any UxrSdkLocator.Name)</param>
        /// <returns>True if installed and available, false if not</returns>
        public static bool IsAvailable(string name)
        {
            if (s_locators != null)
            {
                foreach (UxrSdkLocator locator in s_locators)
                {
                    if (locator.Name == name)
                    {
                        return locator.CurrentState == UxrSdkLocator.State.Available;
                    }
                }
            }

            return false;
        }

        /// <summary>
        ///     Updates the project symbols removing the symbols of the SDK locator given as argument.
        /// </summary>
        /// <param name="locator">The SDK locator to remove the symbols from</param>
        public static void RemoveSymbols(UxrSdkLocator locator)
        {
            if (!locator.IsPackage)
            {
                SetupSymbols(locator, SetupSymbolsMode.ForceRemove);
            }
        }


        /// <summary>
        ///     Checks if currently the project has any symbols defined for the given SDK locator.
        /// </summary>
        /// <param name="locator">The SDK locator to check the symbols for</param>
        public static bool HasAnySymbols(UxrSdkLocator locator)
        {
            string[] targetGroupNames = Enum.GetNames(typeof(BuildTargetGroup));
            int      targetGroupIndex = 0;

            foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                // Get the BuildTargetGroup name through targetGroupNames. targetGroup.ToString() does not work because there are
                // enum entries with the same numerical value

                string targetGroupName = targetGroupNames[targetGroupIndex];
                targetGroupIndex++;

                // Ignore invalid / obsolete target groups

                if (targetGroup == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                var fieldInfo = typeof(BuildTargetGroup).GetField(targetGroupName);

                if (fieldInfo != null && fieldInfo.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }

                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);

                // Get trimmed target symbol list using the new API
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbols);

                for (int currentSymbolIndex = 0; currentSymbolIndex < currentSymbols.Length; ++currentSymbolIndex)
                {
                    currentSymbols[currentSymbolIndex] = currentSymbols[currentSymbolIndex].Trim();
                }

                // Look for symbols
                foreach (string sdkSymbolString in locator.AllSymbols)
                {
                    if (Array.IndexOf(currentSymbols, sdkSymbolString) != -1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Updates the project symbols adding the necessary symbols if the SDK is present or removing them if it is not. Also
        ///     allows removing the symbols if necessary.
        /// </summary>
        /// <param name="locator">The SDK locator</param>
        /// <param name="setupSymbolsMode">
        ///     If <see cref="SetupSymbolsMode.AddOrRemove" /> is specified then it will update the symbols depending on the SDK
        ///     presence (add if SDK is present, remove if it is not present).
        ///     In <see cref="SetupSymbolsMode.ForceRemove" /> mode it will remove all symbols linked to the SDK locator.
        /// </param>
        private static void SetupSymbols(UxrSdkLocator locator, SetupSymbolsMode setupSymbolsMode = SetupSymbolsMode.AddOrRemove)
        {
            string[] targetGroupNames = Enum.GetNames(typeof(BuildTargetGroup));
            int      targetGroupIndex = 0;

            HashSet<string> availableSymbols = new HashSet<string>(locator.AvailableSymbols);
            HashSet<string> allSymbols       = new HashSet<string>(locator.AllSymbols);

            foreach (BuildTargetGroup targetGroup in Enum.GetValues(typeof(BuildTargetGroup)))
            {
                // Get the BuildTargetGroup name through targetGroupNames. targetGroup.ToString() does not work because there are
                // enum entries with the same numerical value

                string targetGroupName = targetGroupNames[targetGroupIndex];
                targetGroupIndex++;

                // Ignore invalid / obsolete target groups

                if (targetGroup == BuildTargetGroup.Unknown)
                {
                    continue;
                }

                var fieldInfo = typeof(BuildTargetGroup).GetField(targetGroupName);

                if (fieldInfo != null && fieldInfo.IsDefined(typeof(ObsoleteAttribute), true))
                {
                    continue;
                }

                NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup);

                // Get current target symbol list using the new API
                PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget, out string[] currentSymbolsArray);

                // Keep original order for non-target symbols, while trimming and removing duplicates/empties
                List<string> currentSymbols = new List<string>(currentSymbolsArray.Length);
                HashSet<string> seenSymbols = new HashSet<string>();

                for (int i = 0; i < currentSymbolsArray.Length; ++i)
                {
                    string symbol = currentSymbolsArray[i]?.Trim();

                    if (string.IsNullOrEmpty(symbol))
                    {
                        continue;
                    }

                    if (seenSymbols.Add(symbol))
                    {
                        currentSymbols.Add(symbol);
                    }
                }

                bool updated = false;

                if (setupSymbolsMode == SetupSymbolsMode.AddOrRemove)
                {
                    // Remove any symbols managed by this locator that are no longer available
                    for (int i = currentSymbols.Count - 1; i >= 0; --i)
                    {
                        string symbol = currentSymbols[i];

                        if (allSymbols.Contains(symbol) && !availableSymbols.Contains(symbol))
                        {
                            currentSymbols.RemoveAt(i);
                            updated = true;
                        }
                    }

                    // Add missing available symbols
                    for (int i = 0; i < locator.AvailableSymbols.Length; ++i)
                    {
                        string symbol = locator.AvailableSymbols[i];

                        if (string.IsNullOrWhiteSpace(symbol))
                        {
                            continue;
                        }

                        symbol = symbol.Trim();

                        if (seenSymbols.Add(symbol))
                        {
                            currentSymbols.Add(symbol);
                            updated = true;
                        }
                    }
                }
                else
                {
                    // Remove all symbols linked to this locator
                    for (int i = currentSymbols.Count - 1; i >= 0; --i)
                    {
                        if (allSymbols.Contains(currentSymbols[i]))
                        {
                            currentSymbols.RemoveAt(i);
                            updated = true;
                        }
                    }
                }

                if (updated)
                {
                    PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, currentSymbols.ToArray());
                }
            }
        }

        #endregion

        #region Private Types & Data

        private static List<UxrSdkLocator> s_locators;

        #endregion
    }
}