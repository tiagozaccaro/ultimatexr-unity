using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Extensions
{
    public static class EditorGUILayoutExt
    {
        /// <summary>
        ///     Draws a navigation bar + scrollable paginated list for any collection of items.
        /// </summary>
        /// <typeparam name="T">Type of element in the collection.</typeparam>
        /// <param name="items">Full list of items to paginate.</param>
        /// <param name="currentPage">Current page index (0-based). Will be updated by this method.</param>
        /// <param name="scrollPosition">Current scroll position. Will be updated by this method.</param>
        /// <param name="itemsPerPage">How many items to show per page.</param>
        /// <param name="drawItem">Callback invoked for each visible item on the current page.</param>
        /// <param name="labelOverride">
        ///     Optional custom label for the navigation bar.
        ///     If null, defaults to "Page X of Y (N assets)".
        /// </param>
        public static void PaginatedScrollView<T>(
            IReadOnlyList<T>            items,
            ref int                     currentPage,
            ref Vector2                 scrollPosition,
            int                         itemsPerPage,
            Action<T>                   drawItem,
            Func<int, int, int, string> labelOverride = null)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            int totalPages = Mathf.Max(1, Mathf.CeilToInt((float)items.Count / itemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

            // Navigation bar
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

            if (GUILayout.Button("<", GUILayout.Width(30)) && currentPage > 0)
            {
                currentPage--;
                scrollPosition = Vector2.zero;
            }

            GUILayout.FlexibleSpace();

            string label = labelOverride != null
                ? labelOverride(currentPage + 1, totalPages, items.Count)
                : $"Page {currentPage + 1} of {totalPages} ({items.Count} assets)";

            EditorGUILayout.LabelField(label, EditorStyles.centeredGreyMiniLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(">", GUILayout.Width(30)) && currentPage < totalPages - 1)
            {
                currentPage++;
                scrollPosition = Vector2.zero;
            }

            EditorGUILayout.EndHorizontal();

            // Scrollable content
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            IEnumerable<T> pagedItems = items.Skip(currentPage * itemsPerPage).Take(itemsPerPage);

            foreach (T item in pagedItems)
            {
                drawItem(item);
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
