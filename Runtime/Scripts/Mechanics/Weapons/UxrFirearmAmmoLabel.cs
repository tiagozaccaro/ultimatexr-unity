// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UxrFirearmAmmoLabel.cs" company="VRMADA">
//   Copyright (c) VRMADA, All rights reserved.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------
using UltimateXR.Core;
using UltimateXR.Core.Components;
using UnityEngine;
using UnityEngine.UI;
#if ULTIMATEXR_UNITY_TMPRO
using TMPro;
#endif

namespace UltimateXR.Mechanics.Weapons
{
    /// <summary>
    ///     Component that draws the ammo left in a firearm magazine.
    ///     Supports both <see cref="Text" /> and TextMeshProUGUI targets.
    ///     The <see cref="Text" /> path allocates only when the displayed text changes.
    ///     The TextMeshProUGUI path uses a reusable character buffer and does not allocate.
    /// </summary>
    [RequireComponent(typeof(UxrFirearmWeapon))]
    public class UxrFirearmAmmoLabel : UxrComponent
    {
        #region Inspector Properties/Serialized Fields

        [SerializeField] [Tooltip(TextTargetToolTip)]   private Graphic _textTarget;
        [SerializeField] [Tooltip(TriggerIndexToolTip)] private int     _triggerIndex;
        [SerializeField] [Tooltip(ShowCapacityToolTip)] private bool    _showCapacity = true;
        [SerializeField] [Tooltip(DigitsToolTip)]       private int     _digits       = 2;

        #endregion

        #region Unity

        /// <summary>
        ///     Initializes the component.
        /// </summary>
        protected override void Awake()
        {
            base.Awake();

            _firearm = GetComponent<UxrFirearmWeapon>();

            if (_digits < 1)
            {
                _digits = 1;
            }

            CacheTargetComponents();
            Invalidate();
        }

        /// <summary>
        ///     Subscribes to events.
        /// </summary>
        protected override void OnEnable()
        {
            base.OnEnable();

            UxrManager.AvatarsUpdated += UxrManager_AvatarsUpdated;
            RefreshLabel();
        }

        /// <summary>
        ///     Unsubscribes from events.
        /// </summary>
        protected override void OnDisable()
        {
            base.OnDisable();

            UxrManager.AvatarsUpdated -= UxrManager_AvatarsUpdated;
        }

        #endregion

        #region Event Handling Methods

        /// <summary>
        ///     Called after all avatars were updated. Updates the ammo information.
        /// </summary>
        private void UxrManager_AvatarsUpdated()
        {
            RefreshLabel();
        }

        #endregion

        #region Private Methods

        /// <summary>
        ///     Caches the supported text components in the target GameObject.
        /// </summary>
        private void CacheTargetComponents()
        {
            _uiText = null;

#if ULTIMATEXR_UNITY_TMPRO
            _tmpText = null;
#endif

            if (!_textTarget)
            {
                return;
            }

            _uiText = _textTarget.GetComponent<Text>();

#if ULTIMATEXR_UNITY_TMPRO
            _tmpText = _textTarget.GetComponent<TextMeshProUGUI>();
#endif
        }

        /// <summary>
        ///     Forces the next refresh to update the displayed label.
        /// </summary>
        private void Invalidate()
        {
            _lastHasMagazine  = !_lastHasMagazine;
            _lastAmmoLeft     = int.MinValue;
            _lastAmmoCapacity = int.MinValue;
            _lastShowCapacity = !_showCapacity;
        }

        /// <summary>
        ///     Refreshes the ammo label if the visible state changed.
        /// </summary>
        private void RefreshLabel()
        {
            if (!_firearm || (!HasUiTextTarget && !HasTmpTextTarget))
            {
                return;
            }

            bool hasMagazine  = _firearm.HasMagAttached(_triggerIndex);
            int  ammoLeft     = hasMagazine ? _firearm.GetAmmoLeft(_triggerIndex) : 0;
            int  ammoCapacity = hasMagazine ? _firearm.GetAmmoCapacity(_triggerIndex) : 0;

            if (hasMagazine)
            {
                if (ammoLeft < 0)
                {
                    ammoLeft = 0;
                }

                if (ammoCapacity < 0)
                {
                    ammoCapacity = 0;
                }
            }

            if (hasMagazine   == _lastHasMagazine  &&
                ammoLeft      == _lastAmmoLeft     &&
                ammoCapacity  == _lastAmmoCapacity &&
                _showCapacity == _lastShowCapacity)
            {
                return;
            }

            _lastHasMagazine  = hasMagazine;
            _lastAmmoLeft     = ammoLeft;
            _lastAmmoCapacity = ammoCapacity;
            _lastShowCapacity = _showCapacity;

#if ULTIMATEXR_UNITY_TMPRO
            if (_tmpText)
            {
                RefreshTmpLabel(hasMagazine, ammoLeft, ammoCapacity);
                return;
            }
#endif

            if (_uiText)
            {
                RefreshUiTextLabel(hasMagazine, ammoLeft, ammoCapacity);
            }
        }

        /// <summary>
        ///     Refreshes the label using the Unity UI Text path.
        ///     This allocates only when the displayed values change.
        ///     Infinite capacity renders an empty string.
        /// </summary>
        /// <param name="hasMagazine">Whether a magazine is attached.</param>
        /// <param name="ammoLeft">Ammo left.</param>
        /// <param name="ammoCapacity">Magazine capacity.</param>
        private void RefreshUiTextLabel(bool hasMagazine, int ammoLeft, int ammoCapacity)
        {
            if (IsInfiniteCapacity(ammoCapacity))
            {
                _uiText.text = string.Empty;
                return;
            }

            string text;

            if (hasMagazine)
            {
                string ammoLeftString = ammoLeft.ToString().PadLeft(_digits, '0');

                if (_showCapacity)
                {
                    string ammoCapacityString = ammoCapacity.ToString().PadLeft(_digits, '0');
                    text = ammoLeftString + "/" + ammoCapacityString;
                }
                else
                {
                    text = ammoLeftString;
                }
            }
            else
            {
                string noAmmoString = new string('-', _digits);

                if (_showCapacity)
                {
                    text = noAmmoString + "/" + noAmmoString;
                }
                else
                {
                    text = noAmmoString;
                }
            }

            _uiText.text = text;
        }

#if ULTIMATEXR_UNITY_TMPRO
        /// <summary>
        ///     Refreshes the label using the TextMeshProUGUI path.
        ///     Infinite capacity or text that doesn't fit in the character buffer renders an empty string.
        /// </summary>
        /// <param name="hasMagazine">Whether a magazine is attached.</param>
        /// <param name="ammoLeft">Ammo left.</param>
        /// <param name="ammoCapacity">Ammo capacity.</param>
        private void RefreshTmpLabel(bool hasMagazine, int ammoLeft, int ammoCapacity)
        {
            if (IsInfiniteCapacity(ammoCapacity))
            {
                _tmpText.SetText(string.Empty);
                return;
            }

            int charCount = hasMagazine
                                ? GetAmmoTextCharCount(ammoLeft, ammoCapacity, _digits, _showCapacity)
                                : GetNoMagazineTextCharCount(_digits, _showCapacity);

            if (charCount > CharBufferLength)
            {
                _tmpText.SetText(string.Empty);
                return;
            }

            if (hasMagazine)
            {
                WriteAmmoText(_charBuffer, ammoLeft, ammoCapacity, _digits, _showCapacity);
            }
            else
            {
                WriteNoMagazineText(_charBuffer, _digits, _showCapacity);
            }

            _tmpText.SetCharArray(_charBuffer, 0, charCount);
        }

        /// <summary>
        ///     Gets the number of characters required to render ammo information.
        /// </summary>
        /// <param name="ammoLeft">Ammo left.</param>
        /// <param name="ammoCapacity">Ammo capacity.</param>
        /// <param name="minDigits">Minimum number of digits.</param>
        /// <param name="showCapacity">Whether to show capacity.</param>
        /// <returns>The number of required characters.</returns>
        private static int GetAmmoTextCharCount(int ammoLeft, int ammoCapacity, int minDigits, bool showCapacity)
        {
            int charCount = GetRenderedDigitCount(ammoLeft, minDigits);

            if (showCapacity)
            {
                charCount += 1 + GetRenderedDigitCount(ammoCapacity, minDigits);
            }

            return charCount;
        }

        /// <summary>
        ///     Gets the number of characters required to render the no-magazine text.
        /// </summary>
        /// <param name="digits">Minimum number of digits.</param>
        /// <param name="showCapacity">Whether to show capacity.</param>
        /// <returns>The number of required characters.</returns>
        private static int GetNoMagazineTextCharCount(int digits, bool showCapacity)
        {
            return showCapacity ? digits * 2 + 1 : digits;
        }

        /// <summary>
        ///     Writes the ammo text into a character buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="ammoLeft">Ammo left.</param>
        /// <param name="ammoCapacity">Ammo capacity.</param>
        /// <param name="minDigits">Minimum number of digits.</param>
        /// <param name="showCapacity">Whether to show capacity.</param>
        /// <returns>The number of written characters.</returns>
        private static int WriteAmmoText(char[] buffer, int ammoLeft, int ammoCapacity, int minDigits, bool showCapacity)
        {
            int index = 0;

            index = WriteInt(buffer, index, ammoLeft, minDigits);

            if (showCapacity)
            {
                buffer[index++] = '/';
                index           = WriteInt(buffer, index, ammoCapacity, minDigits);
            }

            return index;
        }

        /// <summary>
        ///     Writes the "no magazine" text into a character buffer.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="digits">Minimum number of digits.</param>
        /// <param name="showCapacity">Whether to show capacity.</param>
        /// <returns>The number of written characters.</returns>
        private static int WriteNoMagazineText(char[] buffer, int digits, bool showCapacity)
        {
            int index = 0;

            for (int i = 0; i < digits; ++i)
            {
                buffer[index++] = '-';
            }

            if (showCapacity)
            {
                buffer[index++] = '/';

                for (int i = 0; i < digits; ++i)
                {
                    buffer[index++] = '-';
                }
            }

            return index;
        }

        /// <summary>
        ///     Writes a non-negative integer into a character buffer using a minimum number of digits.
        /// </summary>
        /// <param name="buffer">Destination buffer.</param>
        /// <param name="index">Write position.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="minDigits">Minimum number of digits.</param>
        /// <returns>The next write position after the written digits.</returns>
        private static int WriteInt(char[] buffer, int index, int value, int minDigits)
        {
            if (value < 0)
            {
                value = 0;
            }

            int digitCount  = GetDecimalDigitCount(value);
            int totalDigits = digitCount > minDigits ? digitCount : minDigits;
            int endIndex    = index + totalDigits - 1;

            for (int i = 0; i < totalDigits; ++i)
            {
                buffer[index + i] = '0';
            }

            do
            {
                buffer[endIndex--] =  (char)('0' + value % 10);
                value              /= 10;
            }
            while (value > 0);

            return index + totalDigits;
        }
#endif

        /// <summary>
        ///     Gets whether the given capacity represents infinite capacity.
        /// </summary>
        /// <param name="capacity">Capacity.</param>
        /// <returns>Whether the capacity represents infinity.</returns>
        private static bool IsInfiniteCapacity(int capacity)
        {
            return capacity == int.MaxValue;
        }

        /// <summary>
        ///     Gets the number of decimal digits required to represent a non-negative integer.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <returns>Digit count.</returns>
        private static int GetDecimalDigitCount(int value)
        {
            int digitCount = 1;

            while (value >= 10)
            {
                value /= 10;
                ++digitCount;
            }

            return digitCount;
        }

        /// <summary>
        ///     Gets the number of digits that will actually be rendered for a value using a minimum number of digits.
        /// </summary>
        /// <param name="value">Value.</param>
        /// <param name="minDigits">Minimum number of digits.</param>
        /// <returns>The number of rendered digits.</returns>
        private static int GetRenderedDigitCount(int value, int minDigits)
        {
            int digitCount = GetDecimalDigitCount(value);
            return digitCount > minDigits ? digitCount : minDigits;
        }

        #endregion

        #region Private Properties

        /// <summary>
        ///     Gets whether the target has a Unity UI Text component.
        /// </summary>
        private bool HasUiTextTarget => _uiText != null;

#if ULTIMATEXR_UNITY_TMPRO
        /// <summary>
        ///     Gets whether the target has a TextMeshProUGUI component.
        /// </summary>
        private bool HasTmpTextTarget => _tmpText != null;
#else
        /// <summary>
        ///     Gets whether the target has a TextMeshProUGUI component.
        /// </summary>
        private bool HasTmpTextTarget => false;
#endif

        #endregion

        #region Private Types & Data

        private const int CharBufferLength = 32;

        private UxrFirearmWeapon _firearm;
        private Text             _uiText;

#if ULTIMATEXR_UNITY_TMPRO
        private          TextMeshProUGUI _tmpText;
        private readonly char[]          _charBuffer = new char[CharBufferLength];
#endif

        private bool _lastHasMagazine;
        private bool _lastShowCapacity;
        private int  _lastAmmoLeft;
        private int  _lastAmmoCapacity;

        #endregion

        #region Tooltip Strings

        private const string TextTargetToolTip   = "Graphic containing the UI text component used to display the ammo label. It can use a Text component or, if TextMeshPro support is enabled, a TextMeshProUGUI component.";
        private const string TriggerIndexToolTip = "Trigger index in the firearm used to query the magazine and ammo information.";
        private const string ShowCapacityToolTip = "Whether to display the ammo capacity together with the remaining ammo using the format current/capacity.";
        private const string DigitsToolTip       = "Minimum number of digits used when rendering numeric ammo values. Shorter values are left-padded with zeros.";

        #endregion
    }
}