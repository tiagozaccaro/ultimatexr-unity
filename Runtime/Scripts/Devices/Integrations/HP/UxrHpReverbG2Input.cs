// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrHpReverbG2Input.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
using UltimateXR.Core;
using UltimateXR.Devices.Integrations.Microsoft;

namespace UltimateXR.Devices.Integrations.HP
{
    /// <summary>
    ///     HP Reverb G2 Input.
    /// </summary>
    public class UxrHpReverbG2Input : UxrWindowsMixedRealityInput
    {
        #region Public Overrides UxrWindowsMixedRealityInput

        /// <inheritdoc />
        public override bool HasControllerElements(UxrHandSide handSide, UxrControllerElements controllerElements)
        {
            uint validElements = (uint)(UxrControllerElements.Joystick  |
                                        UxrControllerElements.Joystick2 |
                                        UxrControllerElements.Grip      |
                                        UxrControllerElements.Trigger   |
                                        UxrControllerElements.Button1   |
                                        UxrControllerElements.Button2   |
                                        UxrControllerElements.Menu      |
                                        UxrControllerElements.DPad);

            return (validElements & (uint)controllerElements) == (uint)controllerElements;
        }

        #endregion

        #region Protected Overrides UxrWindowsMixedRealityInput

        /// <inheritdoc />
        protected override IEnumerable<string> ControllerNames
        {
            get
            {
                yield return "HP Reverb G2 Controller";
                // Uncomment when providing grip/aim pivots in controller prefabs // yield return "HP Reverb G2 Controller OpenXR";
            }
        }

        #endregion
    }
}