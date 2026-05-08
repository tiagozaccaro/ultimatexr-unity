// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ImageExt.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UltimateXR.Extensions.System;
using UltimateXR.Extensions.System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace UltimateXR.Extensions.Unity.Render
{
    /// <summary>
    ///     <see cref="Image" /> extensions.
    /// </summary>
    public static class ImageExt
    {
        #region Public Methods

        /// <summary>
        ///     Replaces the current sprite of the <see cref="Image" /> with one created
        ///     from the given <see cref="Texture2D" />. The generated sprite uses the
        ///     full texture area and a centered pivot, and is assigned as the image's
        ///     <see cref="Image.overrideSprite" />.
        /// </summary>
        /// <param name="image">The target UI <see cref="Image" /> to update</param>
        /// <param name="texture">The source texture to convert into a sprite</param>
        public static void SetTextureOverride(this Image image, Texture2D texture)
        {
            // Create a Sprite from the Texture2D
            Sprite sprite = Sprite.Create(texture,
                                          new Rect(0, 0, texture.width, texture.height),
                                          new Vector2(0.5f, 0.5f) // pivot at center
                                         );

            // Assign it to the UI Image as override
            image.overrideSprite = sprite;
        }

        /// <summary>
        ///     Loads a sprite asynchronously from a base64 encoded string and assigns it to the
        ///     <see cref="Image.overrideSprite" /> property of an <see cref="Image" />.
        /// </summary>
        /// <param name="self">Target <see cref="Image" /></param>
        /// <param name="base64">Base64 encoded string. See <see cref="SpriteExt.ReadSpriteBase64Async" /></param>
        /// <param name="ct">Optional cancellation token, to cancel the operation</param>
        /// <exception cref="ArgumentNullException"><paramref name="base64" /> is null or empty</exception>
        /// <exception cref="OperationCanceledException">Task canceled using <paramref name="ct" /></exception>
        /// <exception cref="FormatException">
        ///     The length of <paramref name="base64" />, ignoring white-space characters, is not
        ///     zero or a multiple of 4
        /// </exception>
        public static async Task OverrideSpriteFromBase64Async(this Image self, string base64, CancellationToken ct = default)
        {
            self.ThrowIfNull(nameof(self));
            self.overrideSprite = await SpriteExt.ReadSpriteBase64Async(self, base64, ct);
        }

        /// <summary>
        ///     Loads a sprite asynchronously from an URI and assigns it to the <see cref="Image.overrideSprite" /> property of an
        ///     <see cref="Image" />.
        /// </summary>
        /// <param name="self">Target image</param>
        /// <param name="uri">File location. See <see cref="FileExt.Read" /></param>
        /// <param name="ct">Optional cancellation token, to cancel the operation</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="uri" /> is null or empty</exception>
        /// <exception cref="OperationCanceledException">Task canceled using <paramref name="ct" /></exception>
        /// <exception cref="FileNotFoundException">The file specified in <paramref name="uri" /> was not found</exception>
        /// <exception cref="NotSupportedException"><paramref name="uri" /> is in an invalid format</exception>
        /// <exception cref="IOException">An I/O error occurred while opening the file</exception>
        /// <exception cref="InvalidOperationException">The stream is currently in use by a previous read operation</exception>
        public static async Task OverrideSpriteFromUriAsync(this Image self, string uri, CancellationToken ct = default)
        {
            self.ThrowIfNull(nameof(self));
            self.overrideSprite = await SpriteExt.ReadSpriteFileAsync(self, uri, ct);
        }

        /// <summary>
        ///     Tries to load a sprite asynchronously from an URI and assign it to the <see cref="Image.overrideSprite" /> property
        ///     of an <see cref="Image" />.
        /// </summary>
        /// <param name="self">Target image</param>
        /// <param name="uri">File location. See <see cref="FileExt.Read" /></param>
        /// <param name="ct">Optional cancellation token, to cancel the operation</param>
        /// <returns>
        ///     Whether the sprite was correctly load and the <see cref="Image" /> had its <see cref="Image.overrideSprite" />
        ///     assigned
        /// </returns>
        public static async Task<bool> TryOverrideSpriteFromUriAsync(this Image self, string uri, CancellationToken ct = default)
        {
            try
            {
                await self.OverrideSpriteFromUriAsync(uri, ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        ///     Tries to load a sprite asynchronously from a base64 encoded string and assign it to the
        ///     <see cref="Image.overrideSprite" /> property of an <see cref="Image" />.
        /// </summary>
        /// <param name="self">Target image</param>
        /// <param name="base64">Base64 encoded string with the image file content</param>
        /// <param name="ct">Optional cancellation token, to cancel the operation</param>
        /// <returns>
        ///     Whether the sprite was correctly load and the <see cref="Image" /> had its <see cref="Image.overrideSprite" />
        ///     assigned
        /// </returns>
        public static async Task<bool> TryOverrideSpriteFromBase64Async(this Image self, string base64, CancellationToken ct)
        {
            try
            {
                await self.OverrideSpriteFromBase64Async(base64, ct);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}