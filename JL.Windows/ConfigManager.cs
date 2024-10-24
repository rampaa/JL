using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using HandyControl.Data;
using JL.Core.Config;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Windows;

internal static class ConfigManager
{
    #region General

    public static bool InactiveLookupMode { get; set; } // = false;
    public static Brush HighlightColor { get; private set; } = Brushes.AliceBlue;
    public static bool RequireLookupKeyPress { get; private set; } // = false;
    public static bool LookupOnSelectOnly { get; private set; } // = false;
    public static bool LookupOnMouseClickOnly { get; private set; } // = false;
    public static bool AutoAdjustFontSizesOnResolutionChange { get; private set; } // = false;

    public static KeyGesture LookupKeyKeyGesture { get; private set; } = new(Key.LeftShift, ModifierKeys.None);
    public static bool HighlightLongestMatch { get; private set; } // = false;
    public static bool AutoPlayAudio { get; private set; } // = false;
    public static bool DisableHotkeys { get; set; } // = false;
    public static bool Focusable { get; private set; } = true;
    public static MouseButton MiningModeMouseButton { get; private set; } = MouseButton.Middle;
    public static MouseButton LookupOnClickMouseButton { get; private set; } = MouseButton.Left;

    #endregion

    #region MainWindow

    public static double MainWindowWidth { get; set; } = 800;
    public static double MainWindowHeight { get; set; } = 200;
    public static bool MainWindowDynamicHeight { get; private set; } = true;
    public static bool MainWindowDynamicWidth { get; private set; } // = false;
    public static Brush MainWindowTextColor { get; private set; } = Brushes.White;
    public static Brush MainWindowBacklogTextColor { get; private set; } = Brushes.Bisque;
    public static bool AlwaysOnTop { get; set; } = true;
    public static bool TextOnlyVisibleOnHover { get; set; } // = false;
    public static bool ChangeMainWindowBackgroundOpacityOnUnhover { get; private set; } // = false;
    public static double MainWindowBackgroundOpacityOnUnhover { get; private set; } = 0.2; // 0.2-100
    public static bool TextBoxIsReadOnly { get; set; } = true;
    public static bool OnlyCaptureTextWithJapaneseChars { get; private set; } = true;
    public static bool DisableLookupsForNonJapaneseCharsInMainWindow { get; private set; } // = false;
    public static bool MainWindowFocusOnHover { get; private set; } // = false;
    public static bool SteppedBacklogWithMouseWheel { get; private set; } = true;
    public static bool HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar { get; set; } // = false;
    public static bool EnableBacklog { get; private set; } = true;
    public static bool AutoSaveBacklogBeforeClosing { get; private set; } // = false;
    public static bool TextToSpeechOnTextChange { get; private set; } // = false;
    public static bool HidePopupsOnTextChange { get; private set; } = true;
    public static bool AlwaysShowMainTextBoxCaret { get; set; } // = false;
    public static double MainWindowMaxDynamicWidth { get; set; } = 800;
    public static double MainWindowMaxDynamicHeight { get; set; } = 269;
    public static double MainWindowMinDynamicWidth { get; set; } = 100;
    public static double MainWindowMinDynamicHeight { get; set; } = 50;
    private static bool TextBoxApplyDropShadowEffect { get; set; } = true;
    private static bool HorizontallyCenterMainWindowText { get; set; } // = false;
    public static bool MergeSequentialTextsWhenTheyMatch { get; private set; } // = false;
    public static bool AllowPartialMatchingForTextMerge { get; private set; } = true; // = false;
    public static double MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds { get; private set; } = 5000;
    public static bool TextBoxUseCustomLineHeight { get; private set; } // = false;
    public static double TextBoxCustomLineHeight { get; private set; } = 75;
    private static VerticalAlignment MainWindowTextVerticalAlignment { get; set; } = VerticalAlignment.Top;

    #endregion

    #region Popup

    public static FontFamily PopupFont { get; private set; } = new("Meiryo");
    public static double PopupMaxWidth { get; set; } = 700;
    public static double PopupMaxHeight { get; set; } = 520;
    public static double PopupMinWidth { get; set; } // = 0;
    public static double PopupMinHeight { get; set; } // = 0;
    public static bool PopupDynamicHeight { get; private set; } = true;
    public static bool PopupDynamicWidth { get; private set; } = true;
    public static bool FixedPopupPositioning { get; private set; } // = false;
    public static double FixedPopupXPosition { get; set; } // = 0;
    public static double FixedPopupYPosition { get; set; } // = 0;
    public static bool PopupFocusOnLookup { get; private set; } // = false;
    public static bool ShowMiningModeReminder { get; private set; } = true;
    public static bool DisableLookupsForNonJapaneseCharsInPopups { get; private set; } = true;
    public static Brush PopupBackgroundColor { get; private set; } = new SolidColorBrush(Color.FromRgb(0, 0, 0))
    {
        Opacity = 0.8
    };
    public static double PopupXOffset { get; set; } = 10;
    public static double PopupYOffset { get; set; } = 20;
    public static bool PositionPopupLeftOfCursor { get; private set; } // = false;
    public static bool PositionPopupAboveCursor { get; private set; } // = false;
    public static bool PopupFlipX { get; private set; } = true;
    public static bool PopupFlipY { get; private set; } = true;
    public static Brush PrimarySpellingColor { get; private set; } = Brushes.Chocolate;
    public static double PrimarySpellingFontSize { get; set; } = 21;
    public static Brush ReadingsColor { get; private set; } = Brushes.Goldenrod;
    public static double ReadingsFontSize { get; set; } = 19;
    public static Brush AlternativeSpellingsColor { get; private set; } = Brushes.LightYellow;
    public static double AlternativeSpellingsFontSize { get; set; } = 17;
    public static Brush DefinitionsColor { get; private set; } = Brushes.White;
    public static double DefinitionsFontSize { get; set; } = 17;
    public static Brush FrequencyColor { get; private set; } = Brushes.Yellow;
    public static double FrequencyFontSize { get; set; } = 17;
    public static Brush DeconjugationInfoColor { get; private set; } = Brushes.LightSteelBlue;
    public static double DeconjugationInfoFontSize { get; set; } = 17;
    public static Brush DictTypeColor { get; private set; } = Brushes.LightBlue;
    public static double DictTypeFontSize { get; set; } = 15;
    public static Brush SeparatorColor { get; private set; } = Brushes.White;
    public static bool HideDictTabsWithNoResults { get; private set; } = true;
    public static bool AutoHidePopupIfMouseIsNotOverIt { get; private set; } // = false;
    public static double AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds { get; private set; } = 2000;
    public static bool AutoLookupFirstTermWhenTextIsCopiedFromClipboard { get; private set; } // = false;
    public static bool AutoLookupFirstTermWhenTextIsCopiedFromWebSocket { get; private set; } // = false;
    public static bool AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized { get; private set; } = true;
    public static MouseButton MineMouseButton { get; private set; } = MouseButton.Left;
    public static MouseButton CopyPrimarySpellingToClipboardMouseButton { get; private set; } = MouseButton.Middle;

    #endregion

    #region Hotkeys

    public static KeyGesture DisableHotkeysKeyGesture { get; private set; } = new(Key.Pause, ModifierKeys.Alt);
    public static KeyGesture MiningModeKeyGesture { get; private set; } = new(Key.M, ModifierKeys.Alt);
    public static KeyGesture PlayAudioKeyGesture { get; private set; } = new(Key.P, ModifierKeys.Alt);
    public static KeyGesture KanjiModeKeyGesture { get; private set; } = new(Key.K, ModifierKeys.Alt);
    public static KeyGesture ShowManageDictionariesWindowKeyGesture { get; private set; } = new(Key.D, ModifierKeys.Alt);
    public static KeyGesture ShowManageFrequenciesWindowKeyGesture { get; private set; } = new(Key.F, ModifierKeys.Alt);
    public static KeyGesture ShowPreferencesWindowKeyGesture { get; private set; } = new(Key.L, ModifierKeys.Alt);
    public static KeyGesture ShowAddNameWindowKeyGesture { get; private set; } = new(Key.N, ModifierKeys.Alt);
    public static KeyGesture ShowAddWordWindowKeyGesture { get; private set; } = new(Key.W, ModifierKeys.Alt);
    public static KeyGesture SearchWithBrowserKeyGesture { get; private set; } = new(Key.S, ModifierKeys.Alt);
    public static KeyGesture MousePassThroughModeKeyGesture { get; private set; } = new(Key.T, ModifierKeys.Alt);
    public static KeyGesture SteppedBacklogBackwardsKeyGesture { get; private set; } = new(Key.Left, ModifierKeys.Alt);
    public static KeyGesture SteppedBacklogForwardsKeyGesture { get; private set; } = new(Key.Right, ModifierKeys.Alt);
    public static KeyGesture InactiveLookupModeKeyGesture { get; private set; } = new(Key.Q, ModifierKeys.Alt);
    public static KeyGesture MotivationKeyGesture { get; private set; } = new(Key.O, ModifierKeys.Alt);
    public static KeyGesture ClosePopupKeyGesture { get; private set; } = new(Key.Escape, ModifierKeys.Windows);
    public static KeyGesture ShowStatsKeyGesture { get; private set; } = new(Key.Y, ModifierKeys.Alt);
    public static KeyGesture NextDictKeyGesture { get; private set; } = new(Key.PageDown, ModifierKeys.Alt);
    public static KeyGesture PreviousDictKeyGesture { get; private set; } = new(Key.PageUp, ModifierKeys.Alt);
    public static KeyGesture AlwaysOnTopKeyGesture { get; private set; } = new(Key.J, ModifierKeys.Alt);
    public static KeyGesture TextBoxIsReadOnlyKeyGesture { get; private set; } = new(Key.U, ModifierKeys.Alt);
    public static KeyGesture CaptureTextFromClipboardKeyGesture { get; private set; } = new(Key.F10, ModifierKeys.Alt);
    public static KeyGesture CaptureTextFromWebSocketKeyGesture { get; private set; } = new(Key.F11, ModifierKeys.Alt);
    public static KeyGesture ReconnectToWebSocketServerKeyGesture { get; private set; } = new(Key.F9, ModifierKeys.Alt);
    public static KeyGesture DeleteCurrentLineKeyGesture { get; private set; } = new(Key.Delete, ModifierKeys.Alt);
    public static KeyGesture ShowManageAudioSourcesWindowKeyGesture { get; private set; } = new(Key.A, ModifierKeys.Alt);
    public static KeyGesture ToggleMinimizedStateKeyGesture { get; private set; } = new(Key.X, ModifierKeys.Alt);
    public static KeyGesture SelectedTextToSpeechKeyGesture { get; private set; } = new(Key.F6, ModifierKeys.Alt);
    public static KeyGesture ToggleAlwaysShowMainTextBoxCaretKeyGesture { get; private set; } = new(Key.G, ModifierKeys.Alt);
    public static KeyGesture MoveCaretLeftKeyGesture { get; private set; } = new(Key.NumPad4, ModifierKeys.Alt);
    public static KeyGesture MoveCaretRightKeyGesture { get; private set; } = new(Key.NumPad6, ModifierKeys.Alt);
    public static KeyGesture MoveCaretUpKeyGesture { get; private set; } = new(Key.NumPad8, ModifierKeys.Alt);
    public static KeyGesture MoveCaretDownKeyGesture { get; private set; } = new(Key.NumPad2, ModifierKeys.Alt);
    public static KeyGesture LookupTermAtCaretIndexKeyGesture { get; private set; } = new(Key.NumPad5, ModifierKeys.Alt);
    public static KeyGesture LookupFirstTermKeyGesture { get; private set; } = new(Key.D, ModifierKeys.Alt);
    public static KeyGesture LookupSelectedTextKeyGesture { get; private set; } = new(Key.F, ModifierKeys.Alt);
    public static KeyGesture SelectNextLookupResultKeyGesture { get; private set; } = new(Key.Down, ModifierKeys.Alt);
    public static KeyGesture SelectPreviousLookupResultKeyGesture { get; private set; } = new(Key.Up, ModifierKeys.Alt);
    public static KeyGesture MineSelectedLookupResultKeyGesture { get; private set; } = new(Key.D5, ModifierKeys.Alt);

    #endregion

    #region Advanced

    public static int MaxSearchLength { get; private set; } = 37;
    public static int MaxNumResultsNotInMiningMode { get; private set; } = 7;
    public static string SearchUrl { get; private set; } = "https://www.google.com/search?q={SearchTerm}&hl=ja";
    public static string BrowserPath { get; private set; } = "";
    public static bool GlobalHotKeys { get; private set; } = true;
    public static bool StopIncreasingTimeStatWhenMinimized { get; private set; } = true;
    public static bool StripPunctuationBeforeCalculatingCharacterCount { get; private set; } = true;
    public static bool MineToFileInsteadOfAnki { get; private set; } // = false;

    #endregion

    private static readonly ComboBoxItem[] s_japaneseFonts = WindowsUtils.FindJapaneseFonts();
    private static readonly ComboBoxItem[] s_popupJapaneseFonts = WindowsUtils.CloneJapaneseFontComboBoxItems(s_japaneseFonts);
    private static SkinType Theme { get; set; } = SkinType.Dark;

    public static void ApplyPreferences()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        CoreConfigManager.ApplyPreferences(connection);

        SkinType theme = ConfigDBManager.GetValueFromConfig(connection, Theme, nameof(Theme), Enum.TryParse);
        if (theme != Theme)
        {
            Theme = theme;
            WindowsUtils.ChangeTheme(Theme);
        }

        if (CoreConfigManager.CaptureTextFromClipboard)
        {
            WinApi.SubscribeToClipboardChanged(MainWindow.Instance.WindowHandle);
        }
        else
        {
            WinApi.UnsubscribeFromClipboardChanged(MainWindow.Instance.WindowHandle);
        }

        bool stripPunctuationBeforeCalculatingCharacterCount = StripPunctuationBeforeCalculatingCharacterCount;
        StripPunctuationBeforeCalculatingCharacterCount = ConfigDBManager.GetValueFromConfig(connection, StripPunctuationBeforeCalculatingCharacterCount, nameof(StripPunctuationBeforeCalculatingCharacterCount), bool.TryParse);
        if (stripPunctuationBeforeCalculatingCharacterCount != StripPunctuationBeforeCalculatingCharacterCount && BacklogUtils.Backlog.Count > 0)
        {
            ulong characterCount = 0;
            ulong lineCount = 0;

            int backlogCount = BacklogUtils.Backlog.Count;
            for (int i = 0; i < backlogCount; i++)
            {
                string text = BacklogUtils.Backlog[i];
                if (StripPunctuationBeforeCalculatingCharacterCount)
                {
                    text = JapaneseUtils.RemovePunctuation(text);
                }

                if (text.Length > 0)
                {
                    ++lineCount;
                    characterCount += (ulong)new StringInfo(text).LengthInTextElements;
                }
            }

            if (StripPunctuationBeforeCalculatingCharacterCount)
            {
                Stats.IncrementStat(StatType.Characters, -(long)(Stats.SessionStats.Characters - characterCount));
                Stats.IncrementStat(StatType.Lines, -(long)(Stats.SessionStats.Lines - lineCount));
            }
            else
            {
                Stats.IncrementStat(StatType.Characters, (long)(characterCount - Stats.SessionStats.Characters));
                Stats.IncrementStat(StatType.Lines, (long)(lineCount - Stats.SessionStats.Lines));
            }
        }

        LookupOnClickMouseButton = ConfigDBManager.GetValueFromConfig(connection, LookupOnClickMouseButton, nameof(LookupOnClickMouseButton), Enum.TryParse);
        MiningModeMouseButton = ConfigDBManager.GetValueFromConfig(connection, MiningModeMouseButton, nameof(MiningModeMouseButton), Enum.TryParse);
        MineMouseButton = ConfigDBManager.GetValueFromConfig(connection, MineMouseButton, nameof(MineMouseButton), Enum.TryParse);
        CopyPrimarySpellingToClipboardMouseButton = ConfigDBManager.GetValueFromConfig(connection, CopyPrimarySpellingToClipboardMouseButton, nameof(CopyPrimarySpellingToClipboardMouseButton), Enum.TryParse);

        MainWindowTextVerticalAlignment = ConfigDBManager.GetValueFromConfig(connection, MainWindowTextVerticalAlignment, nameof(MainWindowTextVerticalAlignment), Enum.TryParse);
        MainWindow.Instance.MainTextBox.VerticalContentAlignment = MainWindowTextVerticalAlignment;

        AutoAdjustFontSizesOnResolutionChange = ConfigDBManager.GetValueFromConfig(connection, AutoAdjustFontSizesOnResolutionChange, nameof(AutoAdjustFontSizesOnResolutionChange), bool.TryParse);
        HighlightLongestMatch = ConfigDBManager.GetValueFromConfig(connection, HighlightLongestMatch, nameof(HighlightLongestMatch), bool.TryParse);
        AutoPlayAudio = ConfigDBManager.GetValueFromConfig(connection, AutoPlayAudio, nameof(AutoPlayAudio), bool.TryParse);
        GlobalHotKeys = ConfigDBManager.GetValueFromConfig(connection, GlobalHotKeys, nameof(GlobalHotKeys), bool.TryParse);
        StopIncreasingTimeStatWhenMinimized = ConfigDBManager.GetValueFromConfig(connection, StopIncreasingTimeStatWhenMinimized, nameof(StopIncreasingTimeStatWhenMinimized), bool.TryParse);
        MineToFileInsteadOfAnki = ConfigDBManager.GetValueFromConfig(connection, MineToFileInsteadOfAnki, nameof(MineToFileInsteadOfAnki), bool.TryParse);
        AlwaysOnTop = ConfigDBManager.GetValueFromConfig(connection, AlwaysOnTop, nameof(AlwaysOnTop), bool.TryParse);
        MainWindow.Instance.Topmost = AlwaysOnTop;

        RequireLookupKeyPress = ConfigDBManager.GetValueFromConfig(connection, RequireLookupKeyPress, nameof(RequireLookupKeyPress), bool.TryParse);
        DisableHotkeys = ConfigDBManager.GetValueFromConfig(connection, DisableHotkeys, nameof(DisableHotkeys), bool.TryParse);

        Focusable = ConfigDBManager.GetValueFromConfig(connection, Focusable, nameof(Focusable), bool.TryParse);
        if (Focusable)
        {
            WinApi.AllowActivation(MainWindow.Instance.WindowHandle);
        }
        else
        {
            WinApi.PreventActivation(MainWindow.Instance.WindowHandle);
        }

        PopupFocusOnLookup = ConfigDBManager.GetValueFromConfig(connection, PopupFocusOnLookup, nameof(PopupFocusOnLookup), bool.TryParse);
        ShowMiningModeReminder = ConfigDBManager.GetValueFromConfig(connection, ShowMiningModeReminder, nameof(ShowMiningModeReminder), bool.TryParse);
        DisableLookupsForNonJapaneseCharsInPopups = ConfigDBManager.GetValueFromConfig(connection, DisableLookupsForNonJapaneseCharsInPopups, nameof(DisableLookupsForNonJapaneseCharsInPopups), bool.TryParse);
        FixedPopupPositioning = ConfigDBManager.GetValueFromConfig(connection, FixedPopupPositioning, nameof(FixedPopupPositioning), bool.TryParse);
        ChangeMainWindowBackgroundOpacityOnUnhover = ConfigDBManager.GetValueFromConfig(connection, ChangeMainWindowBackgroundOpacityOnUnhover, nameof(ChangeMainWindowBackgroundOpacityOnUnhover), bool.TryParse);
        TextOnlyVisibleOnHover = ConfigDBManager.GetValueFromConfig(connection, TextOnlyVisibleOnHover, nameof(TextOnlyVisibleOnHover), bool.TryParse);
        OnlyCaptureTextWithJapaneseChars = ConfigDBManager.GetValueFromConfig(connection, OnlyCaptureTextWithJapaneseChars, nameof(OnlyCaptureTextWithJapaneseChars), bool.TryParse);
        DisableLookupsForNonJapaneseCharsInMainWindow = ConfigDBManager.GetValueFromConfig(connection, DisableLookupsForNonJapaneseCharsInMainWindow, nameof(DisableLookupsForNonJapaneseCharsInMainWindow), bool.TryParse);
        MainWindowFocusOnHover = ConfigDBManager.GetValueFromConfig(connection, MainWindowFocusOnHover, nameof(MainWindowFocusOnHover), bool.TryParse);
        SteppedBacklogWithMouseWheel = ConfigDBManager.GetValueFromConfig(connection, SteppedBacklogWithMouseWheel, nameof(SteppedBacklogWithMouseWheel), bool.TryParse);
        MainWindowDynamicHeight = ConfigDBManager.GetValueFromConfig(connection, MainWindowDynamicHeight, nameof(MainWindowDynamicHeight), bool.TryParse);
        MainWindowDynamicWidth = ConfigDBManager.GetValueFromConfig(connection, MainWindowDynamicWidth, nameof(MainWindowDynamicWidth), bool.TryParse);
        PopupDynamicHeight = ConfigDBManager.GetValueFromConfig(connection, PopupDynamicHeight, nameof(PopupDynamicHeight), bool.TryParse);
        PopupDynamicWidth = ConfigDBManager.GetValueFromConfig(connection, PopupDynamicWidth, nameof(PopupDynamicWidth), bool.TryParse);
        HideDictTabsWithNoResults = ConfigDBManager.GetValueFromConfig(connection, HideDictTabsWithNoResults, nameof(HideDictTabsWithNoResults), bool.TryParse);
        AutoHidePopupIfMouseIsNotOverIt = ConfigDBManager.GetValueFromConfig(connection, AutoHidePopupIfMouseIsNotOverIt, nameof(AutoHidePopupIfMouseIsNotOverIt), bool.TryParse);
        AutoLookupFirstTermWhenTextIsCopiedFromClipboard = ConfigDBManager.GetValueFromConfig(connection, AutoLookupFirstTermWhenTextIsCopiedFromClipboard, nameof(AutoLookupFirstTermWhenTextIsCopiedFromClipboard), bool.TryParse);
        AutoLookupFirstTermWhenTextIsCopiedFromWebSocket = ConfigDBManager.GetValueFromConfig(connection, AutoLookupFirstTermWhenTextIsCopiedFromWebSocket, nameof(AutoLookupFirstTermWhenTextIsCopiedFromWebSocket), bool.TryParse);
        AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized = ConfigDBManager.GetValueFromConfig(connection, AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized, nameof(AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized), bool.TryParse);

        TextBoxIsReadOnly = ConfigDBManager.GetValueFromConfig(connection, TextBoxIsReadOnly, nameof(TextBoxIsReadOnly), bool.TryParse);
        if (MainWindow.Instance.MainTextBox.IsReadOnly != TextBoxIsReadOnly)
        {
            MainWindow.Instance.MainTextBox.IsReadOnly = TextBoxIsReadOnly;
            MainWindow.Instance.MainTextBox.IsUndoEnabled = !TextBoxIsReadOnly;
            MainWindow.Instance.MainTextBox.AcceptsReturn = !TextBoxIsReadOnly;
            MainWindow.Instance.MainTextBox.AcceptsTab = !TextBoxIsReadOnly;
            MainWindow.Instance.MainTextBox.UndoLimit = TextBoxIsReadOnly ? 0 : -1;
        }

        AlwaysShowMainTextBoxCaret = ConfigDBManager.GetValueFromConfig(connection, AlwaysShowMainTextBoxCaret, nameof(AlwaysShowMainTextBoxCaret), bool.TryParse);
        MainWindow.Instance.MainTextBox.IsReadOnlyCaretVisible = AlwaysShowMainTextBoxCaret;

        HorizontallyCenterMainWindowText = ConfigDBManager.GetValueFromConfig(connection, HorizontallyCenterMainWindowText, nameof(HorizontallyCenterMainWindowText), bool.TryParse);
        MainWindow.Instance.MainTextBox.HorizontalContentAlignment = HorizontallyCenterMainWindowText
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Left;

        EnableBacklog = ConfigDBManager.GetValueFromConfig(connection, EnableBacklog, nameof(EnableBacklog), bool.TryParse);
        if (!EnableBacklog)
        {
            BacklogUtils.Backlog.Clear();
            BacklogUtils.Backlog.TrimExcess();
        }

        AutoSaveBacklogBeforeClosing = ConfigDBManager.GetValueFromConfig(connection, AutoSaveBacklogBeforeClosing, nameof(AutoSaveBacklogBeforeClosing), bool.TryParse);

        TextToSpeechOnTextChange = ConfigDBManager.GetValueFromConfig(connection, TextToSpeechOnTextChange, nameof(TextToSpeechOnTextChange), bool.TryParse);

        HidePopupsOnTextChange = ConfigDBManager.GetValueFromConfig(connection, HidePopupsOnTextChange, nameof(HidePopupsOnTextChange), bool.TryParse);

        MergeSequentialTextsWhenTheyMatch = ConfigDBManager.GetValueFromConfig(connection, MergeSequentialTextsWhenTheyMatch, nameof(MergeSequentialTextsWhenTheyMatch), bool.TryParse);
        AllowPartialMatchingForTextMerge = ConfigDBManager.GetValueFromConfig(connection, AllowPartialMatchingForTextMerge, nameof(AllowPartialMatchingForTextMerge), bool.TryParse);

        HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar = ConfigDBManager.GetValueFromConfig(connection, HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar, nameof(HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar), bool.TryParse);
        MainWindow.Instance.ChangeVisibilityOfTitleBarButtons();

        TextBoxApplyDropShadowEffect = ConfigDBManager.GetValueFromConfig(connection, TextBoxApplyDropShadowEffect, nameof(TextBoxApplyDropShadowEffect), bool.TryParse);
        if (TextBoxApplyDropShadowEffect)
        {
            DropShadowEffect dropShadowEffect = new()
            {
                Direction = 320,
                BlurRadius = 4,
                ShadowDepth = 1.3,
                Opacity = 0.8,
                RenderingBias = RenderingBias.Quality
            };

            dropShadowEffect.Freeze();
            MainWindow.Instance.MainTextBox.Effect = dropShadowEffect;
        }

        else
        {
            MainWindow.Instance.MainTextBox.Effect = null;
        }

        MaxSearchLength = ConfigDBManager.GetValueFromConfig(connection, MaxSearchLength, nameof(MaxSearchLength), int.TryParse);
        PrimarySpellingFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PrimarySpellingFontSize, nameof(PrimarySpellingFontSize), double.TryParse);
        ReadingsFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, ReadingsFontSize, nameof(ReadingsFontSize), double.TryParse);
        AlternativeSpellingsFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, AlternativeSpellingsFontSize, nameof(AlternativeSpellingsFontSize), double.TryParse);
        DefinitionsFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, DefinitionsFontSize, nameof(DefinitionsFontSize), double.TryParse);
        FrequencyFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, FrequencyFontSize, nameof(FrequencyFontSize), double.TryParse);
        DeconjugationInfoFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, DeconjugationInfoFontSize, nameof(DeconjugationInfoFontSize), double.TryParse);
        DictTypeFontSize = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, DictTypeFontSize, nameof(DictTypeFontSize), double.TryParse);
        MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds, nameof(MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds), double.TryParse);
        MaxNumResultsNotInMiningMode = ConfigDBManager.GetValueFromConfig(connection, MaxNumResultsNotInMiningMode, nameof(MaxNumResultsNotInMiningMode), int.TryParse);

        TextBoxUseCustomLineHeight = ConfigDBManager.GetValueFromConfig(connection, TextBoxUseCustomLineHeight, nameof(TextBoxUseCustomLineHeight), bool.TryParse);
        TextBoxCustomLineHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, TextBoxCustomLineHeight, nameof(TextBoxCustomLineHeight), double.TryParse);
        if (TextBoxUseCustomLineHeight)
        {
            MainWindow.Instance.MainTextBox.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight);
            MainWindow.Instance.MainTextBox.SetValue(TextBlock.LineHeightProperty, TextBoxCustomLineHeight);
        }
        else
        {
            MainWindow.Instance.MainTextBox.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.MaxHeight);
            MainWindow.Instance.MainTextBox.SetValue(TextBlock.LineHeightProperty, double.NaN);
        }

        AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds, nameof(AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds), double.TryParse);
        PopupWindowUtils.PopupAutoHideTimer.Enabled = false;
        PopupWindowUtils.PopupAutoHideTimer.Interval = AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;

        PopupXOffset = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupXOffset, nameof(PopupXOffset), double.TryParse);
        WindowsUtils.DpiAwareXOffset = PopupXOffset * WindowsUtils.Dpi.DpiScaleX;

        PopupYOffset = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupYOffset, nameof(PopupYOffset), double.TryParse);
        WindowsUtils.DpiAwareYOffset = PopupYOffset * WindowsUtils.Dpi.DpiScaleY;

        PopupMaxWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMaxWidth, nameof(PopupMaxWidth), double.TryParse);
        PopupMaxHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMaxHeight, nameof(PopupMaxHeight), double.TryParse);
        PopupMinWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMinWidth, nameof(PopupMinWidth), double.TryParse);
        PopupMinHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMinHeight, nameof(PopupMinHeight), double.TryParse);

        FixedPopupXPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, FixedPopupXPosition, nameof(FixedPopupXPosition), double.TryParse);
        FixedPopupYPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, FixedPopupYPosition, nameof(FixedPopupYPosition), double.TryParse);

        MainWindow.Instance.OpacitySlider.Value = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindow.Instance.OpacitySlider.Value, "MainWindowOpacity", double.TryParse);
        MainWindow.Instance.FontSizeSlider.Value = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindow.Instance.FontSizeSlider.Value, "MainWindowFontSize", double.TryParse);
        MainWindowBackgroundOpacityOnUnhover = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowBackgroundOpacityOnUnhover, nameof(MainWindowBackgroundOpacityOnUnhover), double.TryParse);

        MainWindowHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowHeight, nameof(MainWindowHeight), double.TryParse);
        MainWindowWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowWidth, nameof(MainWindowWidth), double.TryParse);
        MainWindowMaxDynamicWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMaxDynamicWidth, nameof(MainWindowMaxDynamicWidth), double.TryParse);
        MainWindowMaxDynamicHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMaxDynamicHeight, nameof(MainWindowMaxDynamicHeight), double.TryParse);
        MainWindowMinDynamicWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMinDynamicWidth, nameof(MainWindowMinDynamicWidth), double.TryParse);
        MainWindowMinDynamicHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMinDynamicHeight, nameof(MainWindowMinDynamicHeight), double.TryParse);
        WindowsUtils.SetSizeToContent(MainWindowDynamicWidth, MainWindowDynamicHeight, MainWindowMaxDynamicWidth, MainWindowMaxDynamicHeight, MainWindowMinDynamicWidth, MainWindowMinDynamicHeight, MainWindowWidth, MainWindowHeight, MainWindow.Instance);
        MainWindow.Instance.WidthBeforeResolutionChange = MainWindowWidth;
        MainWindow.Instance.HeightBeforeResolutionChange = MainWindowHeight;

        double mainWindowTop = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindow.Instance.Top, "MainWindowTopPosition", double.TryParse);
        double mainWindowLeft = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindow.Instance.Left, "MainWindowLeftPosition", double.TryParse);
        WinApi.MoveWindowToPosition(MainWindow.Instance.WindowHandle, mainWindowLeft, mainWindowTop);

        MainWindow.Instance.TopPositionBeforeResolutionChange = MainWindow.Instance.Top;
        MainWindow.Instance.LeftPositionBeforeResolutionChange = MainWindow.Instance.Left;

        MainWindow.Instance.MainGrid.Opacity = TextOnlyVisibleOnHover && !MainWindow.Instance.IsMouseOver && !PreferencesWindow.IsItVisible() ? 0 : 1;

        // MAKE SURE YOU FREEZE ANY NEW COLOR OBJECTS YOU ADD
        // OR THE PROGRAM WILL CRASH AND BURN
        MainWindowTextColor = GetFrozenBrushFromConfig(connection, MainWindowTextColor, nameof(MainWindowTextColor));
        MainWindowBacklogTextColor = GetFrozenBrushFromConfig(connection, MainWindowBacklogTextColor, nameof(MainWindowBacklogTextColor));

        MainWindow.Instance.MainTextBox.Foreground = !EnableBacklog || MainWindow.Instance.MainTextBox.Text == BacklogUtils.Backlog.LastOrDefault("")
            ? MainWindowTextColor
            : MainWindowBacklogTextColor;

        MainWindow.Instance.MainTextBox.CaretBrush = MainWindowTextColor;

        PrimarySpellingColor = GetFrozenBrushFromConfig(connection, PrimarySpellingColor, nameof(PrimarySpellingColor));
        ReadingsColor = GetFrozenBrushFromConfig(connection, ReadingsColor, nameof(ReadingsColor));
        AlternativeSpellingsColor = GetFrozenBrushFromConfig(connection, AlternativeSpellingsColor, nameof(AlternativeSpellingsColor));
        DefinitionsColor = GetFrozenBrushFromConfig(connection, DefinitionsColor, nameof(DefinitionsColor));
        FrequencyColor = GetFrozenBrushFromConfig(connection, FrequencyColor, nameof(FrequencyColor));
        DeconjugationInfoColor = GetFrozenBrushFromConfig(connection, DeconjugationInfoColor, nameof(DeconjugationInfoColor));

        SeparatorColor = GetFrozenBrushFromConfig(connection, SeparatorColor, nameof(SeparatorColor));

        DictTypeColor = GetFrozenBrushFromConfig(connection, DictTypeColor, nameof(DictTypeColor));

        HighlightColor = GetFrozenBrushFromConfig(connection, HighlightColor, nameof(HighlightColor));
        MainWindow.Instance.MainTextBox.SelectionBrush = HighlightColor;

        PopupBackgroundColor = GetBrushFromConfig(connection, PopupBackgroundColor, nameof(PopupBackgroundColor));
        PopupBackgroundColor.Opacity = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, 80.0, "PopupOpacity", double.TryParse) / 100;
        PopupBackgroundColor.Freeze();

        MainWindow.Instance.Background = GetBrushFromConfig(connection, MainWindow.Instance.Background, "MainWindowBackgroundColor");

        MainWindow.Instance.Background.Opacity = ChangeMainWindowBackgroundOpacityOnUnhover && !MainWindow.Instance.IsMouseOver && !PreferencesWindow.IsItVisible()
            ? MainWindowBackgroundOpacityOnUnhover / 100
            : MainWindow.Instance.OpacitySlider.Value / 100;

        WinApi.UnregisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        KeyGestureUtils.GlobalKeyGestureDict.Clear();
        KeyGestureUtils.GlobalKeyGestureNameToIntDict.Clear();

        DisableHotkeysKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(DisableHotkeysKeyGesture), DisableHotkeysKeyGesture);
        MiningModeKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MiningModeKeyGesture), MiningModeKeyGesture);
        PlayAudioKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(PlayAudioKeyGesture), PlayAudioKeyGesture);
        KanjiModeKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(KanjiModeKeyGesture), KanjiModeKeyGesture);
        LookupKeyKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(LookupKeyKeyGesture), LookupKeyKeyGesture);
        ClosePopupKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ClosePopupKeyGesture), ClosePopupKeyGesture);
        ShowStatsKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowStatsKeyGesture), ShowStatsKeyGesture);
        NextDictKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(NextDictKeyGesture), NextDictKeyGesture);
        PreviousDictKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(PreviousDictKeyGesture), PreviousDictKeyGesture);
        AlwaysOnTopKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(AlwaysOnTopKeyGesture), AlwaysOnTopKeyGesture);
        TextBoxIsReadOnlyKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(TextBoxIsReadOnlyKeyGesture), TextBoxIsReadOnlyKeyGesture);
        ToggleAlwaysShowMainTextBoxCaretKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ToggleAlwaysShowMainTextBoxCaretKeyGesture), ToggleAlwaysShowMainTextBoxCaretKeyGesture);
        MoveCaretLeftKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MoveCaretLeftKeyGesture), MoveCaretLeftKeyGesture);
        MoveCaretRightKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MoveCaretRightKeyGesture), MoveCaretRightKeyGesture);
        MoveCaretUpKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MoveCaretUpKeyGesture), MoveCaretUpKeyGesture);
        MoveCaretDownKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MoveCaretDownKeyGesture), MoveCaretDownKeyGesture);
        LookupTermAtCaretIndexKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(LookupTermAtCaretIndexKeyGesture), LookupTermAtCaretIndexKeyGesture);
        LookupFirstTermKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(LookupFirstTermKeyGesture), LookupFirstTermKeyGesture);
        LookupSelectedTextKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(LookupSelectedTextKeyGesture), LookupSelectedTextKeyGesture);
        SelectNextLookupResultKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SelectNextLookupResultKeyGesture), SelectNextLookupResultKeyGesture);
        SelectPreviousLookupResultKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SelectPreviousLookupResultKeyGesture), SelectPreviousLookupResultKeyGesture);
        MineSelectedLookupResultKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MineSelectedLookupResultKeyGesture), MineSelectedLookupResultKeyGesture);
        CaptureTextFromClipboardKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(CaptureTextFromClipboardKeyGesture), CaptureTextFromClipboardKeyGesture);
        CaptureTextFromWebSocketKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(CaptureTextFromWebSocketKeyGesture), CaptureTextFromWebSocketKeyGesture);
        ReconnectToWebSocketServerKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ReconnectToWebSocketServerKeyGesture), ReconnectToWebSocketServerKeyGesture);
        DeleteCurrentLineKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(DeleteCurrentLineKeyGesture), DeleteCurrentLineKeyGesture);

        ShowPreferencesWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowPreferencesWindowKeyGesture), ShowPreferencesWindowKeyGesture);
        ShowAddNameWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowAddNameWindowKeyGesture), ShowAddNameWindowKeyGesture);
        ShowAddWordWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowAddWordWindowKeyGesture), ShowAddWordWindowKeyGesture);
        SearchWithBrowserKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SearchWithBrowserKeyGesture), SearchWithBrowserKeyGesture);
        MousePassThroughModeKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MousePassThroughModeKeyGesture), MousePassThroughModeKeyGesture);
        SteppedBacklogBackwardsKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SteppedBacklogBackwardsKeyGesture), SteppedBacklogBackwardsKeyGesture);
        SteppedBacklogForwardsKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SteppedBacklogForwardsKeyGesture), SteppedBacklogForwardsKeyGesture);
        InactiveLookupModeKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(InactiveLookupModeKeyGesture), InactiveLookupModeKeyGesture);
        MotivationKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MotivationKeyGesture), MotivationKeyGesture);

        ShowManageDictionariesWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowManageDictionariesWindowKeyGesture),
                ShowManageDictionariesWindowKeyGesture);

        ShowManageFrequenciesWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowManageFrequenciesWindowKeyGesture),
                ShowManageFrequenciesWindowKeyGesture);

        ShowManageAudioSourcesWindowKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ShowManageAudioSourcesWindowKeyGesture),
                ShowManageAudioSourcesWindowKeyGesture);

        ToggleMinimizedStateKeyGesture =
            KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(ToggleMinimizedStateKeyGesture),
                ToggleMinimizedStateKeyGesture);

        SelectedTextToSpeechKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(SelectedTextToSpeechKeyGesture), SelectedTextToSpeechKeyGesture);

        if (GlobalHotKeys && !DisableHotkeys)
        {
            WinApi.RegisterAllGlobalHotKeys(MainWindow.Instance.WindowHandle);
        }

        MainWindow.Instance.AddNameMenuItem.SetInputGestureText(ShowAddNameWindowKeyGesture);
        MainWindow.Instance.AddWordMenuItem.SetInputGestureText(ShowAddWordWindowKeyGesture);
        MainWindow.Instance.SearchMenuItem.SetInputGestureText(SearchWithBrowserKeyGesture);
        MainWindow.Instance.PreferencesMenuItem.SetInputGestureText(ShowPreferencesWindowKeyGesture);
        MainWindow.Instance.ManageDictionariesMenuItem.SetInputGestureText(ShowManageDictionariesWindowKeyGesture);
        MainWindow.Instance.ManageFrequenciesMenuItem.SetInputGestureText(ShowManageFrequenciesWindowKeyGesture);
        MainWindow.Instance.ManageAudioSourcesMenuItem.SetInputGestureText(ShowManageAudioSourcesWindowKeyGesture);
        MainWindow.Instance.StatsMenuItem.SetInputGestureText(ShowStatsKeyGesture);

        {
            string? searchUrlStr = ConfigDBManager.GetSettingValue(connection, nameof(SearchUrl));
            if (searchUrlStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(SearchUrl), SearchUrl);
            }
            else
            {
                searchUrlStr = searchUrlStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost", "://127.0.0.1", StringComparison.Ordinal);

                if (Uri.IsWellFormedUriString(searchUrlStr.Replace("{SearchTerm}", "", StringComparison.Ordinal), UriKind.Absolute))
                {
                    SearchUrl = searchUrlStr;
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save Search URL, invalid URL");
                    WindowsUtils.Alert(AlertLevel.Error, "Couldn't save Search URL, invalid URL");
                }
            }
        }

        {
            string? browserPathStr = ConfigDBManager.GetSettingValue(connection, nameof(BrowserPath));
            if (browserPathStr is null)
            {
                ConfigDBManager.InsertSetting(connection, nameof(BrowserPath), BrowserPath);
            }
            else if (!string.IsNullOrWhiteSpace(browserPathStr) && !Path.IsPathFullyQualified(browserPathStr))
            {
                Utils.Logger.Warning("Couldn't save Browser Path, invalid path");
                WindowsUtils.Alert(AlertLevel.Error, "Couldn't save Browser Path, invalid path");
            }
            else
            {
                BrowserPath = browserPathStr;
            }
        }

        {
            string mainWindowFontStr = ConfigDBManager.GetValueFromConfig(connection, "Meiryo", "MainWindowFont");
            MainWindow.Instance.MainTextBox.FontFamily = new FontFamily(mainWindowFontStr);
        }

        {
            string popupPositionRelativeToCursorStr = ConfigDBManager.GetValueFromConfig(connection, "BottomRight", "PopupPositionRelativeToCursor");
            switch (popupPositionRelativeToCursorStr)
            {
                case "TopLeft":
                    PositionPopupAboveCursor = true;
                    PositionPopupLeftOfCursor = true;
                    break;

                case "TopRight":
                    PositionPopupAboveCursor = true;
                    PositionPopupLeftOfCursor = false;
                    break;

                case "BottomLeft":
                    PositionPopupAboveCursor = false;
                    PositionPopupLeftOfCursor = true;
                    break;

                case "BottomRight":
                    PositionPopupAboveCursor = false;
                    PositionPopupLeftOfCursor = false;
                    break;

                default:
                    PositionPopupAboveCursor = false;
                    PositionPopupLeftOfCursor = false;
                    break;
            }
        }

        {
            string popupFlipStr = ConfigDBManager.GetValueFromConfig(connection, "Both", "PopupFlip");
            switch (popupFlipStr)
            {
                case "X":
                    PopupFlipX = true;
                    PopupFlipY = false;
                    break;

                case "Y":
                    PopupFlipX = false;
                    PopupFlipY = true;
                    break;

                case "Both":
                    PopupFlipX = true;
                    PopupFlipY = true;
                    break;

                default:
                    PopupFlipX = true;
                    PopupFlipY = true;
                    break;
            }
        }

        {
            string lookupModeStr = ConfigDBManager.GetValueFromConfig(connection, "Hover", "LookupMode");
            switch (lookupModeStr)
            {
                case "Hover":
                    LookupOnMouseClickOnly = false;
                    LookupOnSelectOnly = false;
                    break;

                case "Click":
                    LookupOnMouseClickOnly = true;
                    LookupOnSelectOnly = false;
                    break;

                case "Select":
                    LookupOnMouseClickOnly = false;
                    LookupOnSelectOnly = true;
                    break;

                default:
                    LookupOnMouseClickOnly = false;
                    LookupOnSelectOnly = false;
                    break;
            }
        }

        {
            string popupFontStr = ConfigDBManager.GetValueFromConfig(connection, PopupFont.Source, nameof(PopupFont));
            PopupFont = new FontFamily(popupFontStr);
            WindowsUtils.PopupFontTypeFace = new Typeface(popupFontStr);
        }

        PopupWindow? currentPopupWindow = MainWindow.Instance.FirstPopupWindow;
        while (currentPopupWindow is not null)
        {
            currentPopupWindow.Background = PopupBackgroundColor;
            currentPopupWindow.Foreground = DefinitionsColor;
            currentPopupWindow.FontFamily = PopupFont;

            WindowsUtils.SetSizeToContent(PopupDynamicWidth, PopupDynamicHeight, PopupMaxWidth, PopupMaxHeight, PopupMinWidth, PopupMinHeight, currentPopupWindow);

            currentPopupWindow.AddNameMenuItem.SetInputGestureText(ShowAddNameWindowKeyGesture);
            currentPopupWindow.AddWordMenuItem.SetInputGestureText(ShowAddWordWindowKeyGesture);
            currentPopupWindow.SearchMenuItem.SetInputGestureText(SearchWithBrowserKeyGesture);

            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    public static void LoadPreferenceWindow(PreferencesWindow preferenceWindow)
    {
        ConfigDBManager.CreateDB();

        preferenceWindow.JLVersionTextBlock.Text = string.Create(CultureInfo.InvariantCulture, $"v{Utils.JLVersion}");

        preferenceWindow.DisableHotkeysKeyGestureTextBox.Text = DisableHotkeysKeyGesture.ToFormattedString();
        preferenceWindow.MiningModeKeyGestureTextBox.Text = MiningModeKeyGesture.ToFormattedString();
        preferenceWindow.PlayAudioKeyGestureTextBox.Text = PlayAudioKeyGesture.ToFormattedString();
        preferenceWindow.KanjiModeKeyGestureTextBox.Text = KanjiModeKeyGesture.ToFormattedString();
        preferenceWindow.LookupKeyKeyGestureTextBox.Text = LookupKeyKeyGesture.ToFormattedString();

        preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text =
            ShowManageDictionariesWindowKeyGesture.ToFormattedString();
        preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text =
            ShowManageFrequenciesWindowKeyGesture.ToFormattedString();
        preferenceWindow.ShowManageAudioSourcesWindowKeyGestureTextBox.Text =
            ShowManageAudioSourcesWindowKeyGesture.ToFormattedString();
        preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text =
            ShowPreferencesWindowKeyGesture.ToFormattedString();
        preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text =
            ShowAddNameWindowKeyGesture.ToFormattedString();
        preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text =
            ShowAddWordWindowKeyGesture.ToFormattedString();
        preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text =
            SearchWithBrowserKeyGesture.ToFormattedString();
        preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text =
            MousePassThroughModeKeyGesture.ToFormattedString();
        preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text =
            SteppedBacklogBackwardsKeyGesture.ToFormattedString();
        preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text =
            SteppedBacklogForwardsKeyGesture.ToFormattedString();
        preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text =
            InactiveLookupModeKeyGesture.ToFormattedString();
        preferenceWindow.MotivationKeyGestureTextBox.Text =
            MotivationKeyGesture.ToFormattedString();
        preferenceWindow.ClosePopupKeyGestureTextBox.Text =
            ClosePopupKeyGesture.ToFormattedString();
        preferenceWindow.ShowStatsKeyGestureTextBox.Text =
            ShowStatsKeyGesture.ToFormattedString();
        preferenceWindow.NextDictKeyGestureTextBox.Text =
            NextDictKeyGesture.ToFormattedString();
        preferenceWindow.PreviousDictKeyGestureTextBox.Text =
            PreviousDictKeyGesture.ToFormattedString();
        preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text =
            AlwaysOnTopKeyGesture.ToFormattedString();
        preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text =
            TextBoxIsReadOnlyKeyGesture.ToFormattedString();
        preferenceWindow.ToggleAlwaysShowMainTextBoxCaretKeyGestureTextBox.Text =
            ToggleAlwaysShowMainTextBoxCaretKeyGesture.ToFormattedString();
        preferenceWindow.MoveCaretLeftKeyGestureTextBox.Text =
            MoveCaretLeftKeyGesture.ToFormattedString();
        preferenceWindow.MoveCaretRightKeyGestureTextBox.Text =
            MoveCaretRightKeyGesture.ToFormattedString();
        preferenceWindow.MoveCaretUpKeyGestureTextBox.Text =
            MoveCaretUpKeyGesture.ToFormattedString();
        preferenceWindow.MoveCaretDownKeyGestureTextBox.Text =
            MoveCaretDownKeyGesture.ToFormattedString();
        preferenceWindow.LookupTermAtCaretIndexKeyGestureTextBox.Text =
            LookupTermAtCaretIndexKeyGesture.ToFormattedString();
        preferenceWindow.LookupFirstTermKeyGestureTextBox.Text =
            LookupFirstTermKeyGesture.ToFormattedString();
        preferenceWindow.LookupSelectedTextKeyGestureTextBox.Text =
            LookupSelectedTextKeyGesture.ToFormattedString();
        preferenceWindow.SelectNextLookupResultKeyGestureTextBox.Text =
            SelectNextLookupResultKeyGesture.ToFormattedString();
        preferenceWindow.SelectPreviousLookupResultKeyGestureTextBox.Text =
            SelectPreviousLookupResultKeyGesture.ToFormattedString();
        preferenceWindow.MineSelectedLookupResultKeyGestureTextBox.Text =
            MineSelectedLookupResultKeyGesture.ToFormattedString();
        preferenceWindow.CaptureTextFromClipboardKeyGestureTextBox.Text =
            CaptureTextFromClipboardKeyGesture.ToFormattedString();
        preferenceWindow.CaptureTextFromWebSocketKeyGestureTextBox.Text =
            CaptureTextFromWebSocketKeyGesture.ToFormattedString();
        preferenceWindow.ReconnectToWebSocketServerKeyGestureTextBox.Text =
            ReconnectToWebSocketServerKeyGesture.ToFormattedString();
        preferenceWindow.DeleteCurrentLineKeyGestureTextBox.Text =
            DeleteCurrentLineKeyGesture.ToFormattedString();
        preferenceWindow.ToggleMinimizedStateKeyGestureTextBox.Text =
            ToggleMinimizedStateKeyGesture.ToFormattedString();
        preferenceWindow.SelectedTextToSpeechTextBox.Text =
            SelectedTextToSpeechKeyGesture.ToFormattedString();

        WindowsUtils.SetButtonColor(preferenceWindow.HighlightColorButton, HighlightColor);
        WindowsUtils.SetButtonColor(preferenceWindow.MainWindowBackgroundColorButton, MainWindow.Instance.Background.CloneCurrentValue());
        WindowsUtils.SetButtonColor(preferenceWindow.TextBoxTextColorButton, MainWindowTextColor);
        WindowsUtils.SetButtonColor(preferenceWindow.TextBoxBacklogTextColorButton, MainWindowBacklogTextColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DeconjugationInfoColorButton, DeconjugationInfoColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DefinitionsColorButton, DefinitionsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.FrequencyColorButton, FrequencyColor);
        WindowsUtils.SetButtonColor(preferenceWindow.PrimarySpellingColorButton, PrimarySpellingColor);
        WindowsUtils.SetButtonColor(preferenceWindow.ReadingsColorButton, ReadingsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.AlternativeSpellingsColorButton, AlternativeSpellingsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.PopupBackgroundColorButton, PopupBackgroundColor);
        WindowsUtils.SetButtonColor(preferenceWindow.SeparatorColorButton, SeparatorColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DictTypeColorButton, DictTypeColor);

        preferenceWindow.SearchUrlTextBox.Text = SearchUrl;
        preferenceWindow.BrowserPathTextBox.Text = BrowserPath;
        preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
        preferenceWindow.AnkiUriTextBox.Text = CoreConfigManager.AnkiConnectUri.OriginalString;
        preferenceWindow.WebSocketUriTextBox.Text = CoreConfigManager.WebSocketUri.OriginalString;
        preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = CoreConfigManager.ForceSyncAnki;
        preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked = CoreConfigManager.AllowDuplicateCards;
        preferenceWindow.LookupRateNumericUpDown.Value = CoreConfigManager.LookupRate;
        preferenceWindow.KanjiModeCheckBox.IsChecked = CoreConfigManager.KanjiMode;
        preferenceWindow.AutoAdjustFontSizesOnResolutionChange.IsChecked = AutoAdjustFontSizesOnResolutionChange;
        preferenceWindow.HighlightLongestMatchCheckBox.IsChecked = HighlightLongestMatch;
        preferenceWindow.AutoPlayAudioCheckBox.IsChecked = AutoPlayAudio;
        preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked = CoreConfigManager.CheckForJLUpdatesOnStartUp;
        preferenceWindow.GlobalHotKeysCheckBox.IsChecked = GlobalHotKeys;
        preferenceWindow.StopIncreasingTimeStatWhenMinimizedCheckBox.IsChecked = StopIncreasingTimeStatWhenMinimized;
        preferenceWindow.StripPunctuationBeforeCalculatingCharacterCountCheckBox.IsChecked = StripPunctuationBeforeCalculatingCharacterCount;
        preferenceWindow.MineToFileInsteadOfAnkiCheckBox.IsChecked = MineToFileInsteadOfAnki;
        preferenceWindow.AlwaysOnTopCheckBox.IsChecked = AlwaysOnTop;
        preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked = RequireLookupKeyPress;
        preferenceWindow.DisableHotkeysCheckBox.IsChecked = DisableHotkeys;
        preferenceWindow.FocusableCheckBox.IsChecked = Focusable;
        preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked = TextOnlyVisibleOnHover;
        preferenceWindow.AnkiIntegrationCheckBox.IsChecked = CoreConfigManager.AnkiIntegration;
        preferenceWindow.LookupRateNumericUpDown.Value = CoreConfigManager.LookupRate;

        preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked = MainWindowDynamicWidth;
        preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked = MainWindowDynamicHeight;

        preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value = MainWindowMaxDynamicWidth;
        preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value = MainWindowMaxDynamicHeight;
        preferenceWindow.MainWindowMinDynamicWidthNumericUpDown.Value = MainWindowMinDynamicWidth;
        preferenceWindow.MainWindowMinDynamicHeightNumericUpDown.Value = MainWindowMinDynamicHeight;

        preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
        preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;
        preferenceWindow.TextBoxFontSizeNumericUpDown.Value = MainWindow.Instance.FontSizeSlider.Value;
        preferenceWindow.MainWindowOpacityNumericUpDown.Value = MainWindow.Instance.OpacitySlider.Value;

        preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked = ChangeMainWindowBackgroundOpacityOnUnhover;
        preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value = MainWindowBackgroundOpacityOnUnhover;

        preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked = TextBoxIsReadOnly;
        preferenceWindow.AlwaysShowMainTextBoxCaretCheckBox.IsChecked = AlwaysShowMainTextBoxCaret;
        preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked = CoreConfigManager.TextBoxTrimWhiteSpaceCharacters;
        preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked = CoreConfigManager.TextBoxRemoveNewlines;
        preferenceWindow.TextBoxApplyDropShadowEffectCheckBox.IsChecked = TextBoxApplyDropShadowEffect;
        preferenceWindow.CaptureTextFromClipboardCheckBox.IsChecked = CoreConfigManager.CaptureTextFromClipboard;
        preferenceWindow.CaptureTextFromWebSocketCheckBox.IsChecked = CoreConfigManager.CaptureTextFromWebSocket;
        preferenceWindow.AutoReconnectToWebSocketCheckBox.IsChecked = CoreConfigManager.AutoReconnectToWebSocket;
        preferenceWindow.OnlyCaptureTextWithJapaneseCharsCheckBox.IsChecked = OnlyCaptureTextWithJapaneseChars;
        preferenceWindow.DisableLookupsForNonJapaneseCharsInMainWindowCheckBox.IsChecked = DisableLookupsForNonJapaneseCharsInMainWindow;
        preferenceWindow.MainWindowFocusOnHoverCheckBox.IsChecked = MainWindowFocusOnHover;
        preferenceWindow.SteppedBacklogWithMouseWheelCheckBox.IsChecked = SteppedBacklogWithMouseWheel;
        preferenceWindow.EnableBacklogCheckBox.IsChecked = EnableBacklog;
        preferenceWindow.AutoSaveBacklogBeforeClosingCheckBox.IsChecked = AutoSaveBacklogBeforeClosing;
        preferenceWindow.TextToSpeechOnTextChangeCheckBox.IsChecked = TextToSpeechOnTextChange;
        preferenceWindow.HidePopupsOnTextChangeCheckBox.IsChecked = HidePopupsOnTextChange;
        preferenceWindow.MergeSequentialTextsWhenTheyMatchCheckBox.IsChecked = MergeSequentialTextsWhenTheyMatch;
        preferenceWindow.AllowPartialMatchingForTextMergeCheckBox.IsChecked = AllowPartialMatchingForTextMerge;
        preferenceWindow.TextBoxUseCustomLineHeightCheckBox.IsChecked = TextBoxUseCustomLineHeight;
        preferenceWindow.ToggleHideAllTitleBarButtonsWhenMouseIsNotOverTitleBarCheckBox.IsChecked = HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar;
        preferenceWindow.HorizontallyCenterMainWindowTextCheckBox.IsChecked = HorizontallyCenterMainWindowText;
        preferenceWindow.MainWindowFontComboBox.ItemsSource = s_japaneseFonts;
        preferenceWindow.MainWindowFontComboBox.SelectedIndex = Array.FindIndex(s_japaneseFonts, static f =>
            f.Content.ToString() == MainWindow.Instance.MainTextBox.FontFamily.Source);

        if (preferenceWindow.MainWindowFontComboBox.SelectedIndex < 0)
        {
            preferenceWindow.MainWindowFontComboBox.SelectedIndex = 0;
        }

        preferenceWindow.PopupFontComboBox.ItemsSource = s_popupJapaneseFonts;
        preferenceWindow.PopupFontComboBox.SelectedIndex =
            Array.FindIndex(s_popupJapaneseFonts, static f => f.Content.ToString() == PopupFont.Source);

        if (preferenceWindow.PopupFontComboBox.SelectedIndex < 0)
        {
            preferenceWindow.PopupFontComboBox.SelectedIndex = 0;
        }

        preferenceWindow.PopupMaxHeightNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Height;
        preferenceWindow.PopupMaxWidthNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Width;

        preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value = MaxNumResultsNotInMiningMode;

        preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
        preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
        preferenceWindow.PopupMinHeightNumericUpDown.Value = PopupMinHeight;
        preferenceWindow.PopupMinWidthNumericUpDown.Value = PopupMinWidth;
        preferenceWindow.FixedPopupPositioningCheckBox.IsChecked = FixedPopupPositioning;
        preferenceWindow.FixedPopupXPositionNumericUpDown.Value = FixedPopupXPosition;
        preferenceWindow.FixedPopupYPositionNumericUpDown.Value = FixedPopupYPosition;
        preferenceWindow.PopupDynamicHeightCheckBox.IsChecked = PopupDynamicHeight;
        preferenceWindow.PopupDynamicWidthCheckBox.IsChecked = PopupDynamicWidth;

        preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
        preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value = DeconjugationInfoFontSize;
        preferenceWindow.DictTypeFontSizeNumericUpDown.Value = DictTypeFontSize;
        preferenceWindow.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMillisecondsNumericUpDown.Value = MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds;
        preferenceWindow.TextBoxCustomLineHeightNumericUpDown.Value = TextBoxCustomLineHeight;
        preferenceWindow.AutoHidePopupIfMouseIsNotOverItDelayInMillisecondsNumericUpDown.Value = AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;
        preferenceWindow.DefinitionsFontSizeNumericUpDown.Value = DefinitionsFontSize;
        preferenceWindow.FrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
        preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value = PrimarySpellingFontSize;
        preferenceWindow.ReadingsFontSizeNumericUpDown.Value = ReadingsFontSize;
        preferenceWindow.PopupOpacityNumericUpDown.Value = PopupBackgroundColor.Opacity * 100;
        preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked = PopupFocusOnLookup;
        preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
        preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;

        if (preferenceWindow.LookupModeComboBox.SelectedIndex < 0)
        {
            preferenceWindow.LookupModeComboBox.SelectedIndex = 0;
        }

        preferenceWindow.LookupOnClickMouseButtonComboBox.SelectedValue = LookupOnClickMouseButton.ToString();
        preferenceWindow.MiningModeMouseButtonComboBox.SelectedValue = MiningModeMouseButton.ToString();
        preferenceWindow.MineMouseButtonComboBox.SelectedValue = MineMouseButton.ToString();
        preferenceWindow.CopyPrimarySpellingToClipboardMouseButtonComboBox.SelectedValue = CopyPrimarySpellingToClipboardMouseButton.ToString();

        preferenceWindow.ShowMiningModeReminderCheckBox.IsChecked = ShowMiningModeReminder;
        preferenceWindow.DisableLookupsForNonJapaneseCharsInPopupsCheckBox.IsChecked = DisableLookupsForNonJapaneseCharsInPopups;
        preferenceWindow.HideDictTabsWithNoResultsCheckBox.IsChecked = HideDictTabsWithNoResults;
        preferenceWindow.AutoHidePopupIfMouseIsNotOverItCheckBox.IsChecked = AutoHidePopupIfMouseIsNotOverIt;

        preferenceWindow.AutoLookupFirstTermWhenTextIsCopiedFromClipboardCheckBox.IsChecked = AutoLookupFirstTermWhenTextIsCopiedFromClipboard;
        preferenceWindow.AutoLookupFirstTermWhenTextIsCopiedFromWebSocketCheckBox.IsChecked = AutoLookupFirstTermWhenTextIsCopiedFromWebSocket;
        preferenceWindow.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimizedCheckBox.IsChecked = AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized;

        preferenceWindow.ThemeComboBox.SelectedValue = Theme;
        preferenceWindow.MainWindowTextVerticalAlignmentComboBox.SelectedValue = MainWindowTextVerticalAlignment;

        using SqliteConnection connection = ConfigDBManager.CreateReadOnlyDBConnection();
        preferenceWindow.ProfileComboBox.ItemsSource = ProfileDBUtils.GetProfileNames(connection);
        preferenceWindow.ProfileComboBox.SelectedItem = ProfileUtils.CurrentProfileName;
        preferenceWindow.MinimumLogLevelComboBox.SelectedValue = ConfigDBManager.GetSettingValue(connection, "MinimumLogLevel");
        preferenceWindow.PopupPositionRelativeToCursorComboBox.SelectedValue = ConfigDBManager.GetSettingValue(connection, "PopupPositionRelativeToCursor");
        preferenceWindow.PopupFlipComboBox.SelectedValue = ConfigDBManager.GetSettingValue(connection, "PopupFlip");
        preferenceWindow.LookupModeComboBox.SelectedValue = ConfigDBManager.GetSettingValue(connection, "LookupMode");
    }

    public static async Task SavePreferences(PreferencesWindow preferenceWindow)
    {
        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(true))
        {
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(DisableHotkeysKeyGesture), preferenceWindow.DisableHotkeysKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MiningModeKeyGesture), preferenceWindow.MiningModeKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(PlayAudioKeyGesture), preferenceWindow.PlayAudioKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(KanjiModeKeyGesture), preferenceWindow.KanjiModeKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(LookupKeyKeyGesture), preferenceWindow.LookupKeyKeyGestureTextBox.Text);

            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowManageDictionariesWindowKeyGesture),
                preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowManageFrequenciesWindowKeyGesture),
                preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowManageAudioSourcesWindowKeyGesture),
                preferenceWindow.ShowManageAudioSourcesWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowPreferencesWindowKeyGesture),
                preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowAddNameWindowKeyGesture),
                preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowAddWordWindowKeyGesture),
                preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SearchWithBrowserKeyGesture),
                preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MousePassThroughModeKeyGesture),
                preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SteppedBacklogBackwardsKeyGesture),
                preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SteppedBacklogForwardsKeyGesture),
                preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(InactiveLookupModeKeyGesture),
                preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MotivationKeyGesture),
                preferenceWindow.MotivationKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ClosePopupKeyGesture),
                preferenceWindow.ClosePopupKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ShowStatsKeyGesture),
                preferenceWindow.ShowStatsKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(NextDictKeyGesture),
                preferenceWindow.NextDictKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(PreviousDictKeyGesture),
                preferenceWindow.PreviousDictKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(AlwaysOnTopKeyGesture),
                preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(TextBoxIsReadOnlyKeyGesture),
                preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ToggleAlwaysShowMainTextBoxCaretKeyGesture),
                preferenceWindow.ToggleAlwaysShowMainTextBoxCaretKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MoveCaretLeftKeyGesture),
                preferenceWindow.MoveCaretLeftKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MoveCaretRightKeyGesture),
                preferenceWindow.MoveCaretRightKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MoveCaretUpKeyGesture),
                preferenceWindow.MoveCaretUpKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MoveCaretDownKeyGesture),
                preferenceWindow.MoveCaretDownKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(LookupTermAtCaretIndexKeyGesture),
                preferenceWindow.LookupTermAtCaretIndexKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(LookupFirstTermKeyGesture),
                preferenceWindow.LookupFirstTermKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(LookupSelectedTextKeyGesture),
                preferenceWindow.LookupSelectedTextKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SelectNextLookupResultKeyGesture),
                preferenceWindow.SelectNextLookupResultKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SelectPreviousLookupResultKeyGesture),
                preferenceWindow.SelectPreviousLookupResultKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MineSelectedLookupResultKeyGesture),
                preferenceWindow.MineSelectedLookupResultKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(CaptureTextFromClipboardKeyGesture),
                preferenceWindow.CaptureTextFromClipboardKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(CaptureTextFromWebSocketKeyGesture),
                preferenceWindow.CaptureTextFromWebSocketKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ReconnectToWebSocketServerKeyGesture),
                preferenceWindow.ReconnectToWebSocketServerKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(DeleteCurrentLineKeyGesture),
                preferenceWindow.DeleteCurrentLineKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(ToggleMinimizedStateKeyGesture),
                preferenceWindow.ToggleMinimizedStateKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(SelectedTextToSpeechKeyGesture),
                preferenceWindow.SelectedTextToSpeechTextBox.Text);

            ConfigDBManager.UpdateSetting(connection, nameof(SearchUrl), preferenceWindow.SearchUrlTextBox.Text);
            ConfigDBManager.UpdateSetting(connection, nameof(BrowserPath), preferenceWindow.BrowserPathTextBox.Text);

            ConfigDBManager.UpdateSetting(connection, nameof(MaxSearchLength),
                preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.AnkiConnectUri), preferenceWindow.AnkiUriTextBox.Text);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.WebSocketUri), preferenceWindow.WebSocketUriTextBox.Text);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowDynamicWidth),
                preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowDynamicHeight),
                preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked.ToString()!);

            double mainWindowMinDynamicWidth = preferenceWindow.MainWindowMinDynamicWidthNumericUpDown.Value;
            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowMinDynamicWidth), mainWindowMinDynamicWidth.ToString(CultureInfo.InvariantCulture));
            double mainWindowMaxDynamicWidth = Math.Max(preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value, mainWindowMinDynamicWidth);
            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowMaxDynamicWidth), mainWindowMaxDynamicWidth.ToString(CultureInfo.InvariantCulture));


            double mainWindowMinDynamicHeight = preferenceWindow.MainWindowMinDynamicHeightNumericUpDown.Value;
            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowMinDynamicHeight), mainWindowMinDynamicHeight.ToString(CultureInfo.InvariantCulture));
            double mainWindowMaxDynamicHeight = Math.Max(preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value, mainWindowMinDynamicHeight);
            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowMaxDynamicHeight), mainWindowMaxDynamicHeight.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowWidth),
                preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowHeight),
                preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            // We want the opaque color here
            ConfigDBManager.UpdateSetting(connection, "MainWindowBackgroundColor",
                preferenceWindow.MainWindowBackgroundColorButton.Background.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(ChangeMainWindowBackgroundOpacityOnUnhover),
                preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowBackgroundOpacityOnUnhover),
                preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(TextBoxIsReadOnly),
                preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AlwaysShowMainTextBoxCaret),
                preferenceWindow.AlwaysShowMainTextBoxCaretCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.TextBoxTrimWhiteSpaceCharacters),
                preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.TextBoxRemoveNewlines),
                preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(TextBoxApplyDropShadowEffect),
                preferenceWindow.TextBoxApplyDropShadowEffectCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.CaptureTextFromClipboard),
                preferenceWindow.CaptureTextFromClipboardCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.CaptureTextFromWebSocket),
                preferenceWindow.CaptureTextFromWebSocketCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.AutoReconnectToWebSocket),
                preferenceWindow.AutoReconnectToWebSocketCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(OnlyCaptureTextWithJapaneseChars),
                preferenceWindow.OnlyCaptureTextWithJapaneseCharsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DisableLookupsForNonJapaneseCharsInMainWindow),
                preferenceWindow.DisableLookupsForNonJapaneseCharsInMainWindowCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowFocusOnHover),
                preferenceWindow.MainWindowFocusOnHoverCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(SteppedBacklogWithMouseWheel),
                preferenceWindow.SteppedBacklogWithMouseWheelCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(EnableBacklog), preferenceWindow.EnableBacklogCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoSaveBacklogBeforeClosing),
                preferenceWindow.AutoSaveBacklogBeforeClosingCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(TextToSpeechOnTextChange),
                preferenceWindow.TextToSpeechOnTextChangeCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HidePopupsOnTextChange),
                preferenceWindow.HidePopupsOnTextChangeCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MergeSequentialTextsWhenTheyMatch),
                preferenceWindow.MergeSequentialTextsWhenTheyMatchCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AllowPartialMatchingForTextMerge),
                preferenceWindow.AllowPartialMatchingForTextMergeCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(TextBoxUseCustomLineHeight),
                preferenceWindow.TextBoxUseCustomLineHeightCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar),
                preferenceWindow.ToggleHideAllTitleBarButtonsWhenMouseIsNotOverTitleBarCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HorizontallyCenterMainWindowText),
                preferenceWindow.HorizontallyCenterMainWindowTextCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowTextColor), preferenceWindow.TextBoxTextColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowBacklogTextColor),
                preferenceWindow.TextBoxBacklogTextColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "MainWindowFontSize",
                preferenceWindow.TextBoxFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, "MainWindowOpacity",
                preferenceWindow.MainWindowOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(Theme), preferenceWindow.ThemeComboBox.SelectedValue.ToString()!);
            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowTextVerticalAlignment), preferenceWindow.MainWindowTextVerticalAlignmentComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "MinimumLogLevel", preferenceWindow.MinimumLogLevelComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "MainWindowFont", preferenceWindow.MainWindowFontComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(PopupFont), preferenceWindow.PopupFontComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.KanjiMode),
                preferenceWindow.KanjiModeCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.ForceSyncAnki),
                preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.AllowDuplicateCards),
                preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.LookupRate),
                preferenceWindow.LookupRateNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(AutoAdjustFontSizesOnResolutionChange),
                preferenceWindow.AutoAdjustFontSizesOnResolutionChange.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HighlightLongestMatch),
                preferenceWindow.HighlightLongestMatchCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoPlayAudio), preferenceWindow.AutoPlayAudioCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(GlobalHotKeys), preferenceWindow.GlobalHotKeysCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(StopIncreasingTimeStatWhenMinimized),
                preferenceWindow.StopIncreasingTimeStatWhenMinimizedCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(StripPunctuationBeforeCalculatingCharacterCount),
                preferenceWindow.StripPunctuationBeforeCalculatingCharacterCountCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MineToFileInsteadOfAnki),
                preferenceWindow.MineToFileInsteadOfAnkiCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.CheckForJLUpdatesOnStartUp),
                preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AlwaysOnTop), preferenceWindow.AlwaysOnTopCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(RequireLookupKeyPress),
                preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DisableHotkeys), preferenceWindow.DisableHotkeysCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(Focusable), preferenceWindow.FocusableCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(TextOnlyVisibleOnHover),
                preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.AnkiIntegration),
                preferenceWindow.AnkiIntegrationCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HighlightColor),
                preferenceWindow.HighlightColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MaxNumResultsNotInMiningMode),
                preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            double popupMinWidth = preferenceWindow.PopupMinWidthNumericUpDown.Value;
            ConfigDBManager.UpdateSetting(connection, nameof(PopupMinWidth), popupMinWidth.ToString(CultureInfo.InvariantCulture));
            double popupMaxWidth = Math.Max(preferenceWindow.PopupMaxWidthNumericUpDown.Value, popupMinWidth);
            ConfigDBManager.UpdateSetting(connection, nameof(PopupMaxWidth), popupMaxWidth.ToString(CultureInfo.InvariantCulture));

            double popupMinHeight = preferenceWindow.PopupMinHeightNumericUpDown.Value;
            ConfigDBManager.UpdateSetting(connection, nameof(PopupMinHeight), popupMinHeight.ToString(CultureInfo.InvariantCulture));
            double popupMaxHeight = Math.Max(preferenceWindow.PopupMaxHeightNumericUpDown.Value, popupMinHeight);
            ConfigDBManager.UpdateSetting(connection, nameof(PopupMaxHeight), popupMaxHeight.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(FixedPopupPositioning),
                preferenceWindow.FixedPopupPositioningCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(FixedPopupXPosition),
                preferenceWindow.FixedPopupXPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(FixedPopupYPosition),
                preferenceWindow.FixedPopupYPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(PopupDynamicHeight),
                preferenceWindow.PopupDynamicHeightCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(PopupDynamicWidth),
                preferenceWindow.PopupDynamicWidthCheckBox.IsChecked.ToString()!);

            // We want the opaque color here
            ConfigDBManager.UpdateSetting(connection, nameof(PopupBackgroundColor),
                preferenceWindow.PopupBackgroundColorButton.Background.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(PrimarySpellingColor), preferenceWindow.PrimarySpellingColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(ReadingsColor), preferenceWindow.ReadingsColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AlternativeSpellingsColor),
                preferenceWindow.AlternativeSpellingsColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DefinitionsColor), preferenceWindow.DefinitionsColorButton.Tag.ToString()!);
            ConfigDBManager.UpdateSetting(connection, nameof(FrequencyColor), preferenceWindow.FrequencyColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DeconjugationInfoColor),
                preferenceWindow.DeconjugationInfoColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "PopupOpacity",
                preferenceWindow.PopupOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(PrimarySpellingFontSize),
                preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(ReadingsFontSize),
                preferenceWindow.ReadingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(AlternativeSpellingsFontSize),
                preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(DefinitionsFontSize),
                preferenceWindow.DefinitionsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(FrequencyFontSize),
                preferenceWindow.FrequencyFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(DeconjugationInfoFontSize),
                preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(DictTypeFontSize),
                preferenceWindow.DictTypeFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds),
                preferenceWindow.MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMillisecondsNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(TextBoxCustomLineHeight),
                preferenceWindow.TextBoxCustomLineHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(SeparatorColor), preferenceWindow.SeparatorColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DictTypeColor), preferenceWindow.DictTypeColorButton.Tag.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(PopupFocusOnLookup),
                preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(PopupXOffset),
                preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(PopupYOffset),
                preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, "PopupPositionRelativeToCursor", preferenceWindow.PopupPositionRelativeToCursorComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "PopupFlip", preferenceWindow.PopupFlipComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(ShowMiningModeReminder),
                preferenceWindow.ShowMiningModeReminderCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(DisableLookupsForNonJapaneseCharsInPopups),
                preferenceWindow.DisableLookupsForNonJapaneseCharsInPopupsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(HideDictTabsWithNoResults),
                preferenceWindow.HideDictTabsWithNoResultsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoHidePopupIfMouseIsNotOverIt),
                preferenceWindow.AutoHidePopupIfMouseIsNotOverItCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds),
                preferenceWindow.AutoHidePopupIfMouseIsNotOverItDelayInMillisecondsNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(AutoLookupFirstTermWhenTextIsCopiedFromClipboard),
                preferenceWindow.AutoLookupFirstTermWhenTextIsCopiedFromClipboardCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoLookupFirstTermWhenTextIsCopiedFromWebSocket),
                preferenceWindow.AutoLookupFirstTermWhenTextIsCopiedFromWebSocketCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized),
                preferenceWindow.AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimizedCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "LookupMode", preferenceWindow.LookupModeComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(LookupOnClickMouseButton),
                preferenceWindow.LookupOnClickMouseButtonComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MiningModeMouseButton),
                preferenceWindow.MiningModeMouseButtonComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MineMouseButton),
                preferenceWindow.MineMouseButtonComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CopyPrimarySpellingToClipboardMouseButton),
                preferenceWindow.CopyPrimarySpellingToClipboardMouseButtonComboBox.SelectedValue.ToString()!);

            ConfigDBManager.UpdateSetting(connection, "MainWindowTopPosition",
                (MainWindow.Instance.Top * WindowsUtils.Dpi.DpiScaleY).ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, "MainWindowLeftPosition",
                (MainWindow.Instance.Left * WindowsUtils.Dpi.DpiScaleX).ToString(CultureInfo.InvariantCulture));
        }

        ApplyPreferences();

        if (preferenceWindow.SetAnkiConfig)
        {
            await preferenceWindow.SaveMiningSetup().ConfigureAwait(false);
        }
    }

    public static void SaveBeforeClosing()
    {
        ConfigDBManager.CreateDB();
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();

        ConfigDBManager.UpdateSetting(connection, "MainWindowFontSize",
            MainWindow.Instance.FontSizeSlider.Value.ToString(CultureInfo.InvariantCulture));

        ConfigDBManager.UpdateSetting(connection, "MainWindowOpacity",
            MainWindow.Instance.OpacitySlider.Value.ToString(CultureInfo.InvariantCulture));

        double mainWindowHeight = MainWindowHeight > MainWindow.Instance.MinHeight
            ? MainWindowHeight <= SystemParameters.VirtualScreenHeight
                ? MainWindowHeight
                : SystemParameters.VirtualScreenHeight
            : MainWindow.Instance.MinHeight;
        ConfigDBManager.UpdateSetting(connection, nameof(MainWindowHeight), mainWindowHeight.ToString(CultureInfo.InvariantCulture));

        double mainWindowWidth = MainWindowWidth > MainWindow.Instance.MinWidth
            ? MainWindowWidth <= SystemParameters.VirtualScreenWidth
                ? MainWindowWidth
                : SystemParameters.VirtualScreenWidth
            : MainWindow.Instance.MinWidth;
        ConfigDBManager.UpdateSetting(connection, nameof(MainWindowWidth), mainWindowWidth.ToString(CultureInfo.InvariantCulture));

        double mainWindowTopPosition = MainWindow.Instance.Top >= SystemParameters.VirtualScreenTop
            ? MainWindow.Instance.Top + MainWindow.Instance.Height <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight
                ? MainWindow.Instance.Top * WindowsUtils.Dpi.DpiScaleY
                : Math.Max(SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - MainWindow.Instance.Height) * WindowsUtils.Dpi.DpiScaleY
            : WindowsUtils.ActiveScreen.Bounds.Y;
        ConfigDBManager.UpdateSetting(connection, "MainWindowTopPosition", mainWindowTopPosition.ToString(CultureInfo.InvariantCulture));

        double mainWindowLeftPosition = MainWindow.Instance.Left >= SystemParameters.VirtualScreenLeft
            ? MainWindow.Instance.Left + MainWindow.Instance.Width <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
                ? MainWindow.Instance.Left * WindowsUtils.Dpi.DpiScaleX
                : Math.Max(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - MainWindow.Instance.Width) * WindowsUtils.Dpi.DpiScaleX
            : WindowsUtils.ActiveScreen.Bounds.X;
        ConfigDBManager.UpdateSetting(connection, "MainWindowLeftPosition", mainWindowLeftPosition.ToString(CultureInfo.InvariantCulture));

        ConfigDBManager.AnalyzeAndVacuum(connection);
    }

    private static Brush GetBrushFromConfig(SqliteConnection connection, Brush solidColorBrush, string configKey)
    {
        string? configValue = ConfigDBManager.GetSettingValue(connection, configKey);
        if (configValue is not null)
        {
            return WindowsUtils.BrushFromHex(configValue);
        }

        ConfigDBManager.InsertSetting(connection, configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));

        return solidColorBrush.IsFrozen
            ? WindowsUtils.BrushFromHex(solidColorBrush.ToString(CultureInfo.InvariantCulture))
            : solidColorBrush;
    }

    private static Brush GetFrozenBrushFromConfig(SqliteConnection connection, Brush solidColorBrush, string configKey)
    {
        Brush brush = GetBrushFromConfig(connection, solidColorBrush, configKey);
        brush.Freeze();
        return brush;
    }
}
