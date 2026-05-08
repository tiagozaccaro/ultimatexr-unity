// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrAudioFormat.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Networking
{
    /// <summary>
    ///     Describes a PCM audio format with sample rate and channel count.
    /// </summary>
    public readonly struct UxrAudioFormat
    {
        #region Public Types & Data

        /// <summary>
        ///     Gets the audio sample rate in Hz.
        /// </summary>
        public int SampleRate { get; }

        /// <summary>
        ///     Gets the number of audio channels (1 for mono, 2 for stereo).
        /// </summary>
        public int Channels { get; }

        #endregion

        #region Constructors & Finalizer

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="sampleRate">Audio sample rate in Hz</param>
        /// <param name="channels">Number of audio channels</param>
        public UxrAudioFormat(int sampleRate, int channels)
        {
            SampleRate = sampleRate;
            Channels   = channels;
        }

        #endregion
    }
}
