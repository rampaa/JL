using System.Collections.Frozen;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using JL.Core.Config;
using JL.Windows.GUI;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Utilities;

internal static class KeyGestureUtils
{
    public static readonly OrderedDictionary<string, KeyGesture> GlobalKeyGestureNameToKeyGestureDict = [];

    public static readonly KeyGesture AltF4KeyGesture = new(Key.F4, ModifierKeys.Alt);

    public static readonly FrozenSet<Key> ValidGlobalKeys = FrozenSet.ToFrozenSet(
    [
        #pragma warning disable format

        // Function keys
        // The F12 key is reserved for use by the debugger at all times, so it cannot be used as a global key
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
    ]);

    public static readonly string[] NamesOfKeyGesturesThatCanBeUsedWhileJLIsMinimized =
    [
        nameof(ConfigManager.ToggleMinimizedStateKeyGesture),
        nameof(ConfigManager.ClosePopupKeyGesture),
        nameof(ConfigManager.DisableHotkeysKeyGesture),
        nameof(ConfigManager.PlayAudioKeyGesture),
        nameof(ConfigManager.SelectedTextToSpeechKeyGesture),
        nameof(ConfigManager.SearchWithBrowserKeyGesture),
        nameof(ConfigManager.LookupFirstTermKeyGesture),
        nameof(ConfigManager.MineSelectedLookupResultKeyGesture),
        nameof(ConfigManager.MotivationKeyGesture),
        nameof(ConfigManager.NextDictKeyGesture),
        nameof(ConfigManager.PreviousDictKeyGesture),
        nameof(ConfigManager.SelectedTextToSpeechKeyGesture),
        nameof(ConfigManager.SelectNextLookupResultKeyGesture),
        nameof(ConfigManager.SelectPreviousLookupResultKeyGesture),
        nameof(ConfigManager.CaptureTextFromClipboardKeyGesture),
        nameof(ConfigManager.CaptureTextFromWebSocketKeyGesture),
        nameof(ConfigManager.ReconnectToWebSocketServerKeyGesture),
        nameof(ConfigManager.ShowAddNameWindowKeyGesture),
        nameof(ConfigManager.ShowAddWordWindowKeyGesture)
    ];

    public static Task HandleKeyDown(KeyEventArgs e)
    {
        Key key = e.Key is Key.System
            ? e.SystemKey
            : e.Key;

        if (key is Key.LWin or Key.RWin)
        {
            return Task.CompletedTask;
        }

        ModifierKeys modifierKeys = Keyboard.Modifiers;
        if (modifierKeys is ModifierKeys.None)
        {
            modifierKeys = ModifierKeys.Windows;
        }
        else if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt or Key.LeftShift or Key.RightShift)
        {
            modifierKeys = ModifierKeys.None;
        }

        if (modifierKeys is ModifierKeys.Shift)
        {
            return Task.CompletedTask;
        }

        KeyGesture pressedKeyGesture = new(key, modifierKeys);

        return HandleHotKey(pressedKeyGesture, e);
    }

    public static Task HandleHotKey(KeyGesture keyGesture, KeyEventArgs? e = null)
    {
        PopupWindow? lastPopup = null;
        MainWindow mainWindow = MainWindow.Instance;
        PopupWindow? currentPopup = mainWindow.FirstPopupWindow;
        while (currentPopup?.IsVisible ?? false)
        {
            lastPopup = currentPopup;
            currentPopup = currentPopup.ChildPopupWindow;
        }

        return lastPopup is not null
            ? lastPopup.HandleHotKey(keyGesture, e)
            : mainWindow.HandleHotKey(keyGesture, e);
    }

    public static bool IsEqual(this KeyGesture sourceKeyGesture, KeyGesture targetKeyGesture)
    {
        return targetKeyGesture.Modifiers is ModifierKeys.Windows
            ? sourceKeyGesture.Key == targetKeyGesture.Key && Keyboard.Modifiers is ModifierKeys.None
            : sourceKeyGesture.Key == targetKeyGesture.Key && sourceKeyGesture.Modifiers == targetKeyGesture.Modifiers;
    }

    public static bool IsPressed(this KeyGesture keyGesture)
    {
        return keyGesture.Modifiers is ModifierKeys.Windows
            ? Keyboard.IsKeyDown(keyGesture.Key) && Keyboard.Modifiers is ModifierKeys.None
            : Keyboard.IsKeyDown(keyGesture.Key)
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

    public static string ToFormattedString(this KeyGesture keyGesture)
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

    public static KeyGesture GetKeyGestureFromConfig(SqliteConnection connection, string keyGestureName, KeyGesture defaultKeyGesture)
    {
        ConfigManager configManager = ConfigManager.Instance;
        string? rawKeyGesture = ConfigDBManager.GetSettingValue(connection, keyGestureName);
        if (rawKeyGesture is not null)
        {
            KeyGestureConverter keyGestureConverter = new();

            string keyGestureString = rawKeyGesture.Contains("Ctrl", StringComparison.Ordinal)
                                      || rawKeyGesture.Contains("Alt", StringComparison.Ordinal)
                                      || rawKeyGesture.Contains("Shift", StringComparison.Ordinal)
                ? rawKeyGesture
                : $"Win+{rawKeyGesture}";

            KeyGesture newKeyGesture = (KeyGesture)keyGestureConverter.ConvertFromInvariantString(keyGestureString)!;
            if (configManager.GlobalHotKeys)
            {
                WinApi.AddHotKeyToGlobalKeyGestureDict(keyGestureName, newKeyGesture);
            }

            return newKeyGesture;
        }

        ConfigDBManager.InsertSetting(connection, keyGestureName, defaultKeyGesture.ToFormattedString());
        if (configManager.GlobalHotKeys)
        {
            WinApi.AddHotKeyToGlobalKeyGestureDict(keyGestureName, defaultKeyGesture);
        }

        return defaultKeyGesture;
    }

    public static void SetInputGestureText(this MenuItem menuItem, KeyGesture keyGesture)
    {
        string formattedString = keyGesture.ToFormattedString();

        menuItem.InputGestureText = formattedString is not "None"
            ? formattedString
            : "";
    }

    public static void UpdateKeyGesture(SqliteConnection connection, string key, string rawKeyGesture)
    {
        string value = rawKeyGesture.StartsWith("Win+", StringComparison.Ordinal)
            ? rawKeyGesture[4..]
            : rawKeyGesture;

        ConfigDBManager.UpdateSetting(connection, key, value);
    }
}
