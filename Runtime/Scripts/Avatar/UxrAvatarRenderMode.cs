// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAvatarRenderModes.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Avatar
{
    /// <summary>
    ///     Defines the different avatar rendering modes, controlling the visibility of the avatar and input controllers.
    /// </summary>
    public enum UxrAvatarRenderMode
    {
        /// <summary>
        ///     Nothing is rendered. All avatar systems remain active, allowing full interaction (for example, collision or
        ///     grabbing)
        ///     even though no visual elements are visible. Commonly used in mixed reality scenarios.
        /// </summary>
        None = 0,

        /// <summary>
        ///     Only the input controller models are rendered instead of the avatar. Optionally, virtual hands holding the
        ///     controllers can also be shown using <see cref="UxrAvatar.ShowControllerHands" />.
        /// </summary>
        Controllers = 1,

        /// <summary>
        ///     Both the controller models and the full avatar are rendered. The avatar renderers are controlled by
        ///     <see cref="UxrAvatar.AvatarRenderers" />.
        ///     This mode assumes that avatar geometry (such as hands or arms) does not visually interfere with the controllers.
        ///     If overlap occurs, consider using <see cref="ControllersAndPartialAvatar" /> instead.
        /// </summary>
        ControllersAndAvatar = 2,

        /// <summary>
        ///     Both the controller models and the avatar are rendered. A subset of the avatar renderers is selectively hidden to
        ///     prevent visual intersection with the controllers. The renderers to hide are specified using
        ///     <see cref="UxrAvatar.PartialAvatarHiddenRenderers" />, which filters the original set of avatar renderers defined
        ///     in <see cref="UxrAvatar.AvatarRenderers" />.
        /// </summary>
        ControllersAndPartialAvatar = 3,

        /// <summary>
        ///     Only the avatar is rendered. No controller models are visible. This is the default mode.
        /// </summary>
        Avatar = 4,
    }
}