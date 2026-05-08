// --------------------------------------------------------------------------------------------------------------------
// <copyright file="StylishFoldoutAttribute.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UnityEngine;

namespace UltimateXR.Attributes
{
    /// <summary>
    ///     Attribute that adds a stylish foldout to a serialized property.
    /// </summary>
    public class StylishFoldoutAttribute : PropertyAttribute
    {
        #region Public Types & Data

        /// <summary>
        ///     The fouldout title.
        /// </summary>
        public string Title { get; }

        /// <summary>
        ///     Whether the foldout is expanded by default.
        /// </summary>
        public bool ExpandedByDefault { get; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="title">The title. Null to use the field name.</param>
        /// <param name="expandedByDefault">Whether the foldout is expanded by default.</param>
        public StylishFoldoutAttribute(string title = null, bool expandedByDefault = true)
        {
            Title             = title;
            ExpandedByDefault = expandedByDefault;
        }

        #endregion
    }
}