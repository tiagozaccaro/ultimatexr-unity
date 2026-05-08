// --------------------------------------------------------------------------------------------------------------------
// <copyright file="WaitResult.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
namespace UltimateXR.Extensions.System.Threading
{
    /// <summary>
    ///     Enumerates the possible results for <see cref="TaskExt.WaitUntilCancelledOrTimeout" />.
    /// </summary>
    public enum WaitResult
    {
        Cancelled,
        Timeout
    }
}