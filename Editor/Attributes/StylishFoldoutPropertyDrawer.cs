// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StylishFoldoutPropertyDrawer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Attributes;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Attributes
{
    /// <summary>
    ///     Property drawer for the <see cref="StylishFoldoutAttribute" />.
    /// </summary>
    [CustomPropertyDrawer(typeof(StylishFoldoutAttribute))]
    public class StylishFoldoutPropertyDrawer : PropertyDrawer
    {
        #region Public Overrides PropertyDrawer

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float height = HeaderHeight;

            if (property.isExpanded)
            {
                SerializedProperty iterator = property.Copy();
                SerializedProperty end      = iterator.GetEndProperty();

                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
                {
                    height        += EditorGUI.GetPropertyHeight(iterator, true);
                    height        += EditorGUIUtility.standardVerticalSpacing;
                    enterChildren =  false;
                }

                height += HeaderSpacing;
            }

            return height;
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            StylishFoldoutAttribute attr = (StylishFoldoutAttribute)attribute;

            string key = GetInitializationKey(property);

            if (!s_initializedProperties.Contains(key))
            {
                property.isExpanded = attr.ExpandedByDefault;
                s_initializedProperties.Add(key);
            }

            string title = string.IsNullOrEmpty(attr.Title) ? label.text : attr.Title;

            Rect headerRect = new Rect(position.x, position.y, position.width, HeaderHeight);

            property.isExpanded = DrawStylishFoldout(headerRect, title, property.isExpanded);

            if (!property.isExpanded)
            {
                return;
            }

            EditorGUI.indentLevel++;

            float y = headerRect.yMax + HeaderSpacing;

            SerializedProperty iterator = property.Copy();
            SerializedProperty end      = iterator.GetEndProperty();

            bool enterChildren = true;

            while (iterator.NextVisible(enterChildren) && !SerializedProperty.EqualContents(iterator, end))
            {
                float childHeight = EditorGUI.GetPropertyHeight(iterator, true);
                Rect  childRect   = new Rect(position.x, y, position.width, childHeight);

                EditorGUI.PropertyField(childRect, iterator, true);

                y             += childHeight + EditorGUIUtility.standardVerticalSpacing;
                enterChildren =  false;
            }

            EditorGUI.indentLevel--;
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Gets a unique key used to track initialization state for a serialized property.
        ///     The key combines the inspected object's instance ID and the property's path,
        ///     ensuring that each property on each inspected object is uniquely identified.
        /// </summary>
        /// <param name="property">The serialized property for which to generate the initialization key.</param>
        /// <returns>
        ///     A string uniquely identifying the property within the inspected object.
        /// </returns>
        private static string GetInitializationKey(SerializedProperty property)
        {
            return $"{property.serializedObject.targetObject.GetEntityId()}:{property.propertyPath}";
        }

        /// <summary>
        ///     Draws a stylized foldout header similar to Unity's particle system module headers.
        ///     The entire header area is clickable and toggles the foldout state when pressed.
        /// </summary>
        /// <param name="rect">The rectangle in which the foldout header is drawn.</param>
        /// <param name="title">The title displayed in the foldout header.</param>
        /// <param name="expanded">Whether the foldout is currently expanded.</param>
        /// <returns>
        ///     <see langword="true" /> if the foldout should be expanded; otherwise <see langword="false" />.
        /// </returns>
        private static bool DrawStylishFoldout(Rect rect, string title, bool expanded)
        {
            GUIStyle style = new GUIStyle("ShurikenModuleTitle")
                             {
                                 font          = EditorStyles.label.font,
                                 border        = new RectOffset(15, 7, 4, 4),
                                 fixedHeight   = 22,
                                 contentOffset = new Vector2(20f, -2f)
                             };

            GUI.Box(rect, title, style);

            Rect toggleRect = new Rect(rect.x + 4f, rect.y + 2f, 13f, 13f);

            Event e = Event.current;

            if (e.type == EventType.Repaint)
            {
                EditorStyles.foldout.Draw(toggleRect, false, false, expanded, false);
            }

            if (e.type == EventType.MouseDown && rect.Contains(e.mousePosition))
            {
                expanded = !expanded;
                e.Use();
            }

            return expanded;
        }

        #endregion

        #region Private Types & Data

        private const float HeaderHeight  = 22f;
        private const float HeaderSpacing = 2f;

        private static readonly HashSet<string> s_initializedProperties = new();

        #endregion
    }
}