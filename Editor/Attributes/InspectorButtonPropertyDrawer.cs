// --------------------------------------------------------------------------------------------------------------------
// <copyright file="InspectorButtonPropertyDrawer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UltimateXR.Attributes;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Attributes
{
    /// <summary>
    ///     Custom property drawer for the <see cref="InspectorButtonAttribute" />.
    /// </summary>
    [CustomPropertyDrawer(typeof(InspectorButtonAttribute))]
    public class InspectorButtonPropertyDrawer : PropertyDrawer
    {
        #region Public Overrides PropertyDrawer

        /// <inheritdoc />
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return 2f * base.GetPropertyHeight(property, label);
        }

        #endregion

        #region Unity

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            InspectorButtonAttribute attr = (InspectorButtonAttribute)attribute;

            if (attr != null)
            {
                EditorGUI.BeginProperty(position, label, property);
                EditorGUI.indentLevel++;

                position.y += EditorGUIUtility.standardVerticalSpacing;

                float buttonWidth  = attr.Width;
                float buttonHeight = EditorGUIUtility.singleLineHeight;
                float buttonX      = position.x + (position.width - buttonWidth) * 0.5f;
                float buttonY      = position.y;

                Rect buttonRect = new Rect(buttonX, buttonY, buttonWidth, buttonHeight);

                if (GUI.Button(buttonRect, attr.Label))
                {
                    MethodInfo method = FindMethodInOwnerChain(property, attr.MethodName, out object target);

                    if (method != null)
                    {
                        method.Invoke(target, null);
                    }
                    else
                    {
                        Debug.LogError($"[ButtonField] Method '{attr.MethodName}' not found in owner chain for property '{property.propertyPath}'");
                    }
                }

                EditorGUI.indentLevel--;
                EditorGUI.EndProperty();
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Finds a method in the serialized property's owner chain, starting from the immediate parent object and moving
        ///     up to the root serialized object.
        /// </summary>
        /// <param name="property">The property whose owner chain to search.</param>
        /// <param name="methodName">The method name to search for.</param>
        /// <param name="target">
        ///     When this method returns, contains the object instance that owns the resolved method, or <see langword="null" />
        ///     if no method was found.
        /// </param>
        /// <returns>
        ///     A <see cref="MethodInfo" /> representing the resolved method, or <see langword="null" /> if no match was found.
        /// </returns>
        private static MethodInfo FindMethodInOwnerChain(SerializedProperty property, string methodName, out object target)
        {
            object[] owners = GetOwnerChainOfProperty(property);

            foreach (object owner in owners)
            {
                if (owner == null)
                {
                    continue;
                }

                MethodInfo method = FindPrivateInstanceOnHierarchy(owner, methodName);
                if (method != null)
                {
                    target = owner;
                    return method;
                }
            }

            target = null;
            return null;
        }

        /// <summary>
        ///     Gets the chain of owner objects for a serialized property, starting from the immediate parent object and moving
        ///     up to the root serialized object.
        /// </summary>
        /// <param name="property">The property whose owner chain to retrieve.</param>
        /// <returns>
        ///     An array containing the owner chain from nearest to farthest.
        /// </returns>
        private static object[] GetOwnerChainOfProperty(SerializedProperty property)
        {
            List<object> pathObjects = new List<object>();

            object obj  = property.serializedObject.targetObject;
            string path = property.propertyPath.Replace(".Array.data[", "[");

            string[] elements = path.Split('.');

            pathObjects.Add(obj);

            for (int i = 0; i < elements.Length - 1; i++)
            {
                obj = GetPathElementValue(obj, elements[i]);

                if (obj == null)
                {
                    break;
                }

                pathObjects.Add(obj);
            }

            pathObjects.Reverse();
            return pathObjects.ToArray();
        }

        /// <summary>
        ///     Gets the value of a path element from an object instance.
        /// </summary>
        /// <param name="source">The source object to read from.</param>
        /// <param name="element">
        ///     The path element to resolve. This can be a member name or an indexed collection element such as <c>items[0]</c>.
        /// </param>
        /// <returns>
        ///     The resolved value, or <see langword="null" /> if it could not be found.
        /// </returns>
        private static object GetPathElementValue(object source, string element)
        {
            if (source == null)
            {
                return null;
            }

            int indexStart = element.IndexOf('[');

            if (indexStart >= 0)
            {
                string elementName = element.Substring(0, indexStart);
                int    index       = Convert.ToInt32(element.Substring(indexStart + 1, element.Length - indexStart - 2));

                object collection = GetMemberValue(source, elementName);

                if (collection is IEnumerable enumerable)
                {
                    IEnumerator enumerator           = enumerable.GetEnumerator();
                    using var   disposableEnumerator = enumerator as IDisposable;

                    for (int i = 0; i <= index; i++)
                    {
                        if (!enumerator.MoveNext())
                        {
                            return null;
                        }
                    }

                    return enumerator.Current;
                }

                return null;
            }

            return GetMemberValue(source, element);
        }

        /// <summary>
        ///     Gets the value of a field or property with the given name from an object instance.
        /// </summary>
        /// <param name="source">The source object to read from.</param>
        /// <param name="name">The name of the field or property.</param>
        /// <returns>
        ///     The member value if found; otherwise, <see langword="null" />.
        /// </returns>
        private static object GetMemberValue(object source, string name)
        {
            if (source == null)
            {
                return null;
            }

            for (Type t = source.GetType(); t != null; t = t.BaseType)
            {
                FieldInfo field = t.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (field != null)
                {
                    return field.GetValue(source);
                }

                PropertyInfo property = t.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (property != null)
                {
                    return property.GetValue(source);
                }
            }

            return null;
        }

        /// <summary>
        ///     Finds a private or public instance method on the target object's class hierarchy with the specified method name.
        /// </summary>
        /// <param name="target">The object whose class hierarchy is being searched for the method.</param>
        /// <param name="methodName">The name of the method to search for.</param>
        /// <returns>A <see cref="MethodInfo" /> object representing the method if found; otherwise, null.</returns>
        private static MethodInfo FindPrivateInstanceOnHierarchy(object target, string methodName)
        {
            for (Type t = target.GetType(); t != null; t = t.BaseType)
            {
                MethodInfo mi = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                if (mi != null)
                {
                    return mi;
                }
            }
            return null;
        }

        #endregion
    }
}