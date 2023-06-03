using System.Configuration;
using System.Text;
using JL.Windows.GUI;
using System.Windows.Input;
using System.Windows.Controls;

namespace JL.Windows.Utilities;
public static class KeyGestureUtils
{
    public static readonly Dictionary<int, KeyGesture> KeyGestureDict = new();

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

        if (e.Key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LWin or Key.RWin
            || modifierKeys is ModifierKeys.Shift)
        {
            modifierKeys = ModifierKeys.None;
        }

        Key key = e.Key is Key.System
            ? e.SystemKey
            : e.Key;

        KeyGesture pressedKeyGesture = new(key, modifierKeys);

        await HandleHotKey(pressedKeyGesture).ConfigureAwait(false);
    }

    public static async Task HandleHotKey(KeyGesture keyGesture)
    {
        MainWindow mainWindow = MainWindow.Instance;

        if (!mainWindow.IsVisible
            || ManageDictionariesWindow.IsItVisible()
            || ManageFrequenciesWindow.IsItVisible()
            || ManageAudioSourcesWindow.IsItVisible()
            || AddNameWindow.IsItVisible()
            || AddWordWindow.IsItVisible()
            || PreferencesWindow.IsItVisible()
            || StatsWindow.IsItVisible())
        {
            return;
        }

        PopupWindow? lastPopup = null;
        PopupWindow? currentPopup = mainWindow.FirstPopupWindow;

        while (currentPopup is not null)
        {
            if (currentPopup.IsVisible)
            {
                lastPopup = currentPopup;
            }

            else
            {
                break;
            }

            currentPopup = currentPopup.ChildPopupWindow;
        }

        if (lastPopup is not null)
        {
            await lastPopup.HandleHotKey(keyGesture).ConfigureAwait(false);
        }

        else
        {
            await mainWindow.HandleHotKey(keyGesture).ConfigureAwait(false);
        }
    }

    public static bool CompareKeyGestures(KeyGesture keyGesture1, KeyGesture keyGesture2)
    {
        if (keyGesture2.Modifiers is ModifierKeys.Windows)
        {
            return keyGesture2.Key == keyGesture1.Key && Keyboard.Modifiers is 0;
        }

        if (keyGesture2.Modifiers is 0)
        {
            return keyGesture2.Key == keyGesture1.Key;
        }

        return keyGesture1 == keyGesture2;
    }

    public static bool CompareKeyGesture(KeyGesture keyGesture)
    {
        if (keyGesture.Modifiers is ModifierKeys.Windows)
        {
            return Keyboard.IsKeyDown(keyGesture.Key) && (Keyboard.Modifiers & ModifierKeys.Windows) is 0;
        }

        if (keyGesture.Modifiers is 0)
        {
            return Keyboard.IsKeyDown(keyGesture.Key);
        }
        return Keyboard.IsKeyDown(keyGesture.Key) && Keyboard.Modifiers == keyGesture.Modifiers;
    }

    public static string KeyGestureToString(KeyGesture keyGesture)
    {
        StringBuilder keyGestureStringBuilder = new();

        if (keyGesture.Key is Key.LeftShift or Key.RightShift
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt)
        {
            _ = keyGestureStringBuilder.Append(keyGesture.Key.ToString());
        }

        else
        {
            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
            {
                _ = keyGestureStringBuilder.Append("Ctrl+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                _ = keyGestureStringBuilder.Append("Alt+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift) && keyGestureStringBuilder.Length > 0)
            {
                _ = keyGestureStringBuilder.Append("Shift+");
            }

            if (keyGesture.Key is not Key.None)
            {
                _ = keyGestureStringBuilder.Append(keyGesture.Key.ToString());
            }
        }

        return keyGestureStringBuilder.Length > 0
            ? keyGestureStringBuilder.ToString()
            : "None";
    }

    public static KeyGesture SetKeyGesture(string keyGestureName, KeyGesture keyGesture)
    {
        string? rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

        if (rawKeyGesture is not null)
        {
            KeyGestureConverter keyGestureConverter = new();

            KeyGesture newKeyGesture = rawKeyGesture.Contains("Ctrl") || rawKeyGesture.Contains("Alt") || rawKeyGesture.Contains("Shift")
                ? (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture)!
                : (KeyGesture)keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture)!;

            if (ConfigManager.GlobalHotKeys)
            {
                WinApi.AddHotKeyToKeyGestureDict(keyGestureName, newKeyGesture);
            }

            return newKeyGesture;
        }

        Configuration config =
            ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        config.AppSettings.Settings.Add(keyGestureName, KeyGestureToString(keyGesture));
        config.Save(ConfigurationSaveMode.Modified);

        if (ConfigManager.GlobalHotKeys)
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
