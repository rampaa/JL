using System.Buffers;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using JL.Core.Config;
using JL.Core.Utilities;
using JL.Windows.GUI;
using Microsoft.Data.Sqlite;

namespace JL.Windows.Utilities;

internal static class KeyGestureUtils
{
    private static readonly SearchValues<string> s_validModifiers = SearchValues.Create(["Ctrl", "Alt", "Shift"], StringComparison.Ordinal);

    public static readonly OrderedDictionary<string, KeyGesture> GlobalKeyGestureNameToKeyGestureDict = [];

    public static readonly KeyGesture AltF4KeyGesture = new(Key.F4, ModifierKeys.Alt);
    public static readonly KeyGesture CtrlCKeyGesture = new(Key.C, ModifierKeys.Control);

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
        nameof(ConfigManager.ClickAudioButtonKeyGesture),
        nameof(ConfigManager.ClickMiningButtonKeyGesture),
        nameof(ConfigManager.SelectedTextToSpeechKeyGesture),
        nameof(ConfigManager.SearchWithBrowserKeyGesture),
        nameof(ConfigManager.LookupFirstTermKeyGesture),
        nameof(ConfigManager.ConfirmItemSelectionKeyGesture),
        nameof(ConfigManager.MotivationKeyGesture),
        nameof(ConfigManager.NextDictKeyGesture),
        nameof(ConfigManager.PreviousDictKeyGesture),
        nameof(ConfigManager.ToggleVisibilityOfDictionaryTabsInMiningModeKeyGesture),
        nameof(ConfigManager.SelectedTextToSpeechKeyGesture),
        nameof(ConfigManager.SelectNextItemKeyGesture),
        nameof(ConfigManager.SelectPreviousItemKeyGesture),
        nameof(ConfigManager.CaptureTextFromClipboardKeyGesture),
        nameof(ConfigManager.CaptureTextFromWebSocketKeyGesture),
        nameof(ConfigManager.ReconnectToWebSocketServerKeyGesture),
        nameof(ConfigManager.ShowAddNameWindowKeyGesture),
        nameof(ConfigManager.ShowAddWordWindowKeyGesture),
        nameof(ConfigManager.ShowStatsKeyGesture)
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

        return modifierKeys is ModifierKeys.Shift
            ? Task.CompletedTask
            : HandleHotKey(new KeyGesture(key, modifierKeys));
    }

    public static Task HandleHotKey(KeyGesture keyGesture)
    {
        PopupWindow? lastPopup = null;
        MainWindow mainWindow = MainWindow.Instance;
        PopupWindow? currentPopup = mainWindow.FirstPopupWindow;
        while (currentPopup?.IsVisible ?? false)
        {
            lastPopup = currentPopup;
            currentPopup = PopupWindowUtils.PopupWindows[currentPopup.PopupIndex + 1];
        }

        return lastPopup is not null
            ? ReadingSelectionWindow.IsItVisible()
                ? ReadingSelectionWindow.HandleHotKey(keyGesture)
                : MiningSelectionWindow.IsItVisible()
                    ? MiningSelectionWindow.HandleHotKey(keyGesture)
                    : lastPopup.HandleHotKey(keyGesture)
            : mainWindow.HandleHotKey(keyGesture);
    }

    public static bool IsEqual(this KeyGesture sourceKeyGesture, KeyGesture targetKeyGesture)
    {
        return targetKeyGesture.Modifiers is ModifierKeys.Windows
            ? sourceKeyGesture.Key == targetKeyGesture.Key && Keyboard.Modifiers is ModifierKeys.None
            : sourceKeyGesture.Key == targetKeyGesture.Key && sourceKeyGesture.Modifiers == targetKeyGesture.Modifiers;
    }

    public static bool IsPressed(this KeyGesture keyGesture)
    {
        ModifierKeys modifierKeys = Keyboard.Modifiers;

        return keyGesture.Modifiers is ModifierKeys.Windows
            ? Keyboard.IsKeyDown(keyGesture.Key) && modifierKeys is ModifierKeys.None
            : Keyboard.IsKeyDown(keyGesture.Key) && (ModifierAsKeyPress(keyGesture.Key, modifierKeys)
                ? keyGesture.Modifiers is ModifierKeys.None
                : modifierKeys == keyGesture.Modifiers);
    }

    private static bool ModifierAsKeyPress(Key key, ModifierKeys currentlyPressedModifierKeys)
    {
        return (key is Key.LeftCtrl or Key.RightCtrl && currentlyPressedModifierKeys is ModifierKeys.Control)
               || (key is Key.LeftAlt or Key.RightAlt && currentlyPressedModifierKeys is ModifierKeys.Alt)
               || (key is Key.LeftShift or Key.RightShift && currentlyPressedModifierKeys is ModifierKeys.Shift);
    }

    public static string ToFormattedString(this KeyGesture keyGesture)
    {
        if (keyGesture.Key is Key.LeftShift or Key.RightShift
            or Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt)
        {
            return keyGesture.Key.ToString();
        }

        StringBuilder sb = Utils.StringBuilderPool.Get();

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

        string formattedText = sb.Length > 0
            ? sb.ToString()
            : "None";

        Utils.StringBuilderPool.Return(sb);

        return formattedText;
    }

    public static KeyGesture GetKeyGestureFromConfig(SqliteConnection connection, Dictionary<string, string> configs, string keyGestureName, KeyGesture defaultKeyGesture)
    {
        ConfigManager configManager = ConfigManager.Instance;
        if (configs.TryGetValue(keyGestureName, out string? rawKeyGesture))
        {
            KeyGestureConverter keyGestureConverter = new();
            string keyGestureString = rawKeyGesture.ContainsAny(s_validModifiers)
                ? rawKeyGesture
                : $"Win+{rawKeyGesture}";

            KeyGesture? newKeyGesture = (KeyGesture?)keyGestureConverter.ConvertFromInvariantString(keyGestureString);
            Debug.Assert(newKeyGesture is not null);

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
        string value = rawKeyGesture.AsSpan().StartsWith("Win+", StringComparison.Ordinal)
            ? rawKeyGesture[4..]
            : rawKeyGesture;

        ConfigDBManager.UpdateSetting(connection, key, value);
    }
}
