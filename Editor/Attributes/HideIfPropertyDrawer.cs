// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HideIfPropertyDrawer.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using UltimateXR.Attributes;
using UltimateXR.Core;
using UnityEditor;
using UnityEngine;

namespace UltimateXR.Editor.Attributes
{
    /// <summary>
    ///     Custom property drawer for <see cref="HideIfAttribute" />.
    ///     Controls whether a field is displayed based on the value of another field.
    /// </summary>
    [CustomPropertyDrawer(typeof(HideIfAttribute))]
    public class HideIfPropertyDrawer : ShowIfPropertyDrawer
    {
        #region Protected Methods

        /// <inheritdoc cref="ShowIfPropertyDrawer" />
        protected override bool ShouldShow(object value, object expectedValue)
        {
            return !base.ShouldShow(value, expectedValue);
        }

        #endregion
    }
}