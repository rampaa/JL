using System.Configuration;
using System.Globalization;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using JL.Windows.GUI;

namespace JL.Windows.Utilities;

internal static class KeyGestureUtils
{
    public static readonly Dictionary<int, KeyGesture> KeyGestureDict = new();
    public static readonly Dictionary<string, int> KeyGestureNameToIntDict = new();

    public static readonly HashSet<Key> ValidKeys = new()
    {
        #pragma warning disable format

        // Function keys
        // The F12 key is reserved for use by the debugger at all times so it cannot be used as a global key
        Key.F1, Key.F2, Key.F3, Key.F4, Key.F5, Key.F6,
        Key.F7, Key.F8, Key.F9, Key.F10, Key.F11,

        Key.F13, Key.F14, Key.F15, Key.F16, Key.F17, Key.F18,
        Key.F19, Key.F20, Key.F21, Key.F22, Key.F23, Key.F24,

        // Numeric keypad keys
        Key.NumPad0, Key.NumPad1, Key.NumPad2, Key.NumPad3,Key.NumPad4,
        Key.NumPad5, Key.NumPad6, Key.NumPad7, Key.NumPad8, Key.NumPad9,

        Key.Multiply, Key.Add, Key.Separator, Key.Subtract, Key.Multiply,
        Key.Decimal, Key.Divide

        #pragma warning restore format
    };

    public static async Task HandleKeyDown(KeyEventArgs e)
    {
        ModifierKeys modifierKeys = Keyboard.Modifiers;
        if (modifierKeys is ModifierKeys.None)
        {
            modifierKeys = ModifierKeys.Windows;
        }

        Key key = e.Key is Key.System
            ? e.SystemKey
            : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin
            || modifierKeys is ModifierKeys.Shift)
        {
            modifierKeys = ModifierKeys.None;
        }

        KeyGesture pressedKeyGesture = new(key, modifierKeys);

        await HandleHotKey(pressedKeyGesture, e).ConfigureAwait(false);
    }

    public static async Task HandleHotKey(KeyGesture keyGesture, KeyEventArgs? e = null)
    {
        PopupWindow? currentPopup = MainWindow.Instance.FirstPopupWindow;
        PopupWindow? lastPopup = null;

        while (currentPopup?.IsVisible ?? false)
        {
            lastPopup = currentPopup;
            currentPopup = currentPopup.ChildPopupWindow;
        }

        if (lastPopup is not null)
        {
            await lastPopup.HandleHotKey(keyGesture).ConfigureAwait(false);
        }

        else
        {
            await MainWindow.Instance.HandleHotKey(keyGesture, e).ConfigureAwait(false);
        }
    }

    public static bool CompareKeyGestures(KeyGesture keyGesture1, KeyGesture keyGesture2)
    {
        if (keyGesture2.Modifiers is ModifierKeys.Windows)
        {
            return keyGesture1.Key == keyGesture2.Key && Keyboard.Modifiers is ModifierKeys.None;
        }

        return keyGesture1.Key == keyGesture2.Key && keyGesture1.Modifiers == keyGesture2.Modifiers;
    }

    public static bool CompareKeyGesture(KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers is ModifierKeys.Windows)
        {
            return Keyboard.IsKeyDown(keyGesture.Key) && Keyboard.Modifiers is ModifierKeys.None;
        }

        return Keyboard.IsKeyDown(keyGesture.Key)
               && (ModifierAsKeyPress(keyGesture.Key)
                   ? keyGesture.Modifiers is ModifierKeys.None
                   : Keyboard.Modifiers == keyGesture.Modifiers);
    }

    private static bool ModifierAsKeyPress(Key key)
    {
        return (key is Key.LeftCtrl or Key.RightCtrl && Keyboard.Modifiers is ModifierKeys.Control)
               || (key is Key.LeftAlt or Key.RightAlt && Keyboard.Modifiers is ModifierKeys.Alt)
               || (key is Key.LeftShift or Key.RightShift && Keyboard.Modifiers is ModifierKeys.Shift);
    }

    public static string KeyGestureToString(KeyGesture keyGesture)
    {
        if (keyGesture.Key is Key.LeftShift or Key.RightShift
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt)
        {
            return keyGesture.Key.ToString();
        }

        StringBuilder sb = new();

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
        {
            _ = sb.Append("Ctrl+");
        }

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
        {
            _ = sb.Append("Alt+");
        }

        if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift) && sb.Length > 0)
        {
            _ = sb.Append("Shift+");
        }

        if (keyGesture.Key is not Key.None)
        {
            _ = sb.Append(keyGesture.Key.ToString());
        }

        return sb.Length > 0
            ? sb.ToString()
            : "None";
    }

    public static KeyGesture SetKeyGesture(string keyGestureName, KeyGesture keyGesture, bool setAsGlobalHotKey = true)
    {
        string? rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

        if (rawKeyGesture is not null)
        {
            KeyGestureConverter keyGestureConverter = new();

            KeyGesture newKeyGesture = rawKeyGesture.Contains("Ctrl", StringComparison.Ordinal)
                                       || rawKeyGesture.Contains("Alt", StringComparison.Ordinal)
                                       || rawKeyGesture.Contains("Shift", StringComparison.Ordinal)
                ? (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture)!
                : (KeyGesture)keyGestureConverter.ConvertFromString(string.Create(CultureInfo.InvariantCulture, $"Win+{rawKeyGesture}"))!;

            if (ConfigManager.GlobalHotKeys && setAsGlobalHotKey)
            {
                WinApi.AddHotKeyToKeyGestureDict(keyGestureName, newKeyGesture);
            }

            return newKeyGesture;
        }

        Configuration config =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Add(keyGestureName, KeyGestureToString(keyGesture));
        config.Save(ConfigurationSaveMode.Modified);

        if (ConfigManager.GlobalHotKeys && setAsGlobalHotKey)
        {
            WinApi.AddHotKeyToKeyGestureDict(keyGestureName, keyGesture);
        }

        return keyGesture;
    }

    public static void SetInputGestureText(MenuItem menuItem, KeyGesture keyGesture)
    {
        string keyGestureString = KeyGestureToString(keyGesture);

        menuItem.InputGestureText = keyGestureString is not "None"
            ? keyGestureString
            : "";
    }
}
