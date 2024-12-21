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
using Rectangle = System.Drawing.Rectangle;

namespace JL.Windows;

internal sealed class ConfigManager
{
    public static ConfigManager Instance { get; private set; } = new();

    #region General

    public bool InactiveLookupMode { get; set; } // = false;
    public Brush HighlightColor { get; private set; } = Brushes.AliceBlue;
    public bool RequireLookupKeyPress { get; private set; } // = false;
    public bool LookupOnSelectOnly { get; private set; } // = false;
    public bool LookupOnMouseClickOnly { get; private set; } // = false;
    public KeyGesture LookupKeyKeyGesture { get; private set; } = new(Key.LeftShift, ModifierKeys.None);
    public bool HighlightLongestMatch { get; private set; } // = false;
    public bool AutoPlayAudio { get; private set; } // = false;
    public bool Focusable { get; private set; } = true;
    public MouseButton MiningModeMouseButton { get; private set; } = MouseButton.Middle;
    public MouseButton LookupOnClickMouseButton { get; private set; } = MouseButton.Left;

    #endregion

    #region MainWindow

    public double MainWindowWidth { get; set; } = 800;
    public double MainWindowHeight { get; set; } = 200;
    public bool MainWindowDynamicHeight { get; private set; } = true;
    public bool MainWindowDynamicWidth { get; private set; } // = false;
    public Brush MainWindowTextColor { get; private set; } = Brushes.White;
    public Brush MainWindowBacklogTextColor { get; private set; } = Brushes.Bisque;
    public bool AlwaysOnTop { get; set; } = true;
    public bool TextOnlyVisibleOnHover { get; set; } // = false;
    public bool ChangeMainWindowBackgroundOpacityOnUnhover { get; private set; } // = false;
    public double MainWindowBackgroundOpacityOnUnhover { get; private set; } = 0.2; // 0.2-100
    public bool TextBoxIsReadOnly { get; set; } = true;
    public bool OnlyCaptureTextWithJapaneseChars { get; private set; } = true;
    public bool DisableLookupsForNonJapaneseCharsInMainWindow { get; private set; } // = false;
    public bool MainWindowFocusOnHover { get; private set; } // = false;
    public bool SteppedBacklogWithMouseWheel { get; private set; } = true;
    public bool HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar { get; set; } // = false;
    public bool EnableBacklog { get; private set; } = true;
    public bool AutoSaveBacklogBeforeClosing { get; private set; } // = false;
    public bool TextToSpeechOnTextChange { get; private set; } // = false;
    public bool HidePopupsOnTextChange { get; private set; } = true;
    public bool AlwaysShowMainTextBoxCaret { get; set; } // = false;
    public double MainWindowMaxDynamicWidth { get; set; } = 800;
    public double MainWindowMaxDynamicHeight { get; set; } = 269;
    public double MainWindowMinDynamicWidth { get; set; } = 100;
    public double MainWindowMinDynamicHeight { get; set; } = 50;
    private bool TextBoxApplyDropShadowEffect { get; set; } = true;
    private bool HorizontallyCenterMainWindowText { get; set; } // = false;
    public bool DiscardIdenticalText { get; set; } // = false;
    public bool MergeSequentialTextsWhenTheyMatch { get; private set; } // = false;
    public bool AllowPartialMatchingForTextMerge { get; private set; } // = false;
    public double MaxDelayBetweenCopiesForMergingMatchingSequentialTextsInMilliseconds { get; private set; } = 5000;
    public bool TextBoxUseCustomLineHeight { get; private set; } // = false;
    public double TextBoxCustomLineHeight { get; private set; } = 75;
    public bool RepositionMainWindowOnTextChangeByBottomPosition { get; private set; } // = false;
    public double MainWindowFixedBottomPosition { get; private set; } = -2;
    public bool RepositionMainWindowOnTextChangeByRightPosition { get; private set; } // = false;
    public double MainWindowFixedRightPosition { get; private set; } // = 0;
    public Color MainTextBoxDropShadowEffectColor { get; private set; } = Colors.Black;
    public double MainTextBoxDropShadowEffectShadowDepth { get; private set; } = 1.3;
    public int MainTextBoxDropShadowEffectBlurRadius { get; private set; } = 4;
    public int MainTextBoxDropShadowEffectBlurOpacity { get; private set; } = 80;
    public int MainTextBoxDropShadowEffectDirection { get; private set; } = 320;
    private VerticalAlignment MainWindowTextVerticalAlignment { get; set; } = VerticalAlignment.Top;

    #endregion

    #region Popup

    public FontFamily PopupFont { get; private set; } = new("Meiryo");
    public double PopupMaxWidth { get; set; } = 700;
    public double PopupMaxHeight { get; set; } = 520;
    public double PopupMinWidth { get; set; } // = 0;
    public double PopupMinHeight { get; set; } // = 0;
    public bool PopupDynamicHeight { get; private set; } = true;
    public bool PopupDynamicWidth { get; private set; } = true;
    public bool FixedPopupPositioning { get; private set; } // = false;
    public double FixedPopupXPosition { get; set; } // = 0;
    public double FixedPopupYPosition { get; set; } // = 0;
    public bool PopupFocusOnLookup { get; private set; } // = false;
    public bool ShowMiningModeReminder { get; private set; } = true;
    public bool DisableLookupsForNonJapaneseCharsInPopups { get; private set; } = true;
    public Brush PopupBackgroundColor { get; private set; } = new SolidColorBrush(Color.FromRgb(0, 0, 0))
    {
        Opacity = 0.8
    };
    public double PopupXOffset { get; set; } = 10;
    public double PopupYOffset { get; set; } = 20;
    public bool PositionPopupLeftOfCursor { get; private set; } // = false;
    public bool PositionPopupAboveCursor { get; private set; } // = false;
    public bool PopupFlipX { get; private set; } = true;
    public bool PopupFlipY { get; private set; } = true;
    public Brush PrimarySpellingColor { get; private set; } = Brushes.Chocolate;
    public double PrimarySpellingFontSize { get; set; } = 21;
    public Brush ReadingsColor { get; private set; } = Brushes.Goldenrod;
    public double ReadingsFontSize { get; set; } = 19;
    public Brush AlternativeSpellingsColor { get; private set; } = Brushes.LightYellow;
    public double AlternativeSpellingsFontSize { get; set; } = 17;
    public Brush DefinitionsColor { get; private set; } = Brushes.White;
    public double DefinitionsFontSize { get; set; } = 17;
    public Brush FrequencyColor { get; private set; } = Brushes.Yellow;
    public double FrequencyFontSize { get; set; } = 17;
    public Brush DeconjugationInfoColor { get; private set; } = Brushes.LightSteelBlue;
    public double DeconjugationInfoFontSize { get; set; } = 17;
    public Brush DictTypeColor { get; private set; } = Brushes.LightBlue;
    public double DictTypeFontSize { get; set; } = 15;
    public Brush SeparatorColor { get; private set; } = Brushes.White;
    public bool HideDictTabsWithNoResults { get; private set; } = true;
    public bool AutoHidePopupIfMouseIsNotOverIt { get; private set; } // = false;
    public double AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds { get; private set; } = 2000;
    public bool AutoLookupFirstTermWhenTextIsCopiedFromClipboard { get; private set; } // = false;
    public bool AutoLookupFirstTermWhenTextIsCopiedFromWebSocket { get; private set; } // = false;
    public bool AutoLookupFirstTermOnTextChangeOnlyWhenMainWindowIsMinimized { get; private set; } = true;
    public MouseButton MineMouseButton { get; private set; } = MouseButton.Left;
    public MouseButton CopyPrimarySpellingToClipboardMouseButton { get; private set; } = MouseButton.Middle;

    #endregion

    #region Hotkeys

    public KeyGesture DisableHotkeysKeyGesture { get; private set; } = new(Key.Pause, ModifierKeys.Alt);
    public KeyGesture MiningModeKeyGesture { get; private set; } = new(Key.M, ModifierKeys.Alt);
    public KeyGesture PlayAudioKeyGesture { get; private set; } = new(Key.P, ModifierKeys.Alt);
    public KeyGesture ShowManageDictionariesWindowKeyGesture { get; private set; } = new(Key.D, ModifierKeys.Alt);
    public KeyGesture ShowManageFrequenciesWindowKeyGesture { get; private set; } = new(Key.F, ModifierKeys.Alt);
    public KeyGesture ShowPreferencesWindowKeyGesture { get; private set; } = new(Key.L, ModifierKeys.Alt);
    public KeyGesture ShowAddNameWindowKeyGesture { get; private set; } = new(Key.N, ModifierKeys.Alt);
    public KeyGesture ShowAddWordWindowKeyGesture { get; private set; } = new(Key.W, ModifierKeys.Alt);
    public KeyGesture SearchWithBrowserKeyGesture { get; private set; } = new(Key.S, ModifierKeys.Alt);
    public KeyGesture MousePassThroughModeKeyGesture { get; private set; } = new(Key.T, ModifierKeys.Alt);
    public KeyGesture SteppedBacklogBackwardsKeyGesture { get; private set; } = new(Key.Left, ModifierKeys.Alt);
    public KeyGesture SteppedBacklogForwardsKeyGesture { get; private set; } = new(Key.Right, ModifierKeys.Alt);
    public KeyGesture InactiveLookupModeKeyGesture { get; private set; } = new(Key.Q, ModifierKeys.Alt);
    public KeyGesture MotivationKeyGesture { get; private set; } = new(Key.O, ModifierKeys.Alt);
    public KeyGesture ClosePopupKeyGesture { get; private set; } = new(Key.Escape, ModifierKeys.Windows);
    public KeyGesture ShowStatsKeyGesture { get; private set; } = new(Key.Y, ModifierKeys.Alt);
    public KeyGesture NextDictKeyGesture { get; private set; } = new(Key.PageDown, ModifierKeys.Alt);
    public KeyGesture PreviousDictKeyGesture { get; private set; } = new(Key.PageUp, ModifierKeys.Alt);
    public KeyGesture AlwaysOnTopKeyGesture { get; private set; } = new(Key.J, ModifierKeys.Alt);
    public KeyGesture TextBoxIsReadOnlyKeyGesture { get; private set; } = new(Key.U, ModifierKeys.Alt);
    public KeyGesture CaptureTextFromClipboardKeyGesture { get; private set; } = new(Key.F10, ModifierKeys.Alt);
    public KeyGesture CaptureTextFromWebSocketKeyGesture { get; private set; } = new(Key.F11, ModifierKeys.Alt);
    public KeyGesture ReconnectToWebSocketServerKeyGesture { get; private set; } = new(Key.F9, ModifierKeys.Alt);
    public KeyGesture DeleteCurrentLineKeyGesture { get; private set; } = new(Key.Delete, ModifierKeys.Alt);
    public KeyGesture ShowManageAudioSourcesWindowKeyGesture { get; private set; } = new(Key.A, ModifierKeys.Alt);
    public KeyGesture ToggleMinimizedStateKeyGesture { get; private set; } = new(Key.X, ModifierKeys.Alt);
    public KeyGesture SelectedTextToSpeechKeyGesture { get; private set; } = new(Key.F6, ModifierKeys.Alt);
    public KeyGesture ToggleAlwaysShowMainTextBoxCaretKeyGesture { get; private set; } = new(Key.G, ModifierKeys.Alt);
    public KeyGesture MoveCaretLeftKeyGesture { get; private set; } = new(Key.NumPad4, ModifierKeys.Alt);
    public KeyGesture MoveCaretRightKeyGesture { get; private set; } = new(Key.NumPad6, ModifierKeys.Alt);
    public KeyGesture MoveCaretUpKeyGesture { get; private set; } = new(Key.NumPad8, ModifierKeys.Alt);
    public KeyGesture MoveCaretDownKeyGesture { get; private set; } = new(Key.NumPad2, ModifierKeys.Alt);
    public KeyGesture LookupTermAtCaretIndexKeyGesture { get; private set; } = new(Key.NumPad5, ModifierKeys.Alt);
    public KeyGesture LookupFirstTermKeyGesture { get; private set; } = new(Key.D, ModifierKeys.Alt);
    public KeyGesture LookupSelectedTextKeyGesture { get; private set; } = new(Key.F, ModifierKeys.Alt);
    public KeyGesture SelectNextLookupResultKeyGesture { get; private set; } = new(Key.Down, ModifierKeys.Alt);
    public KeyGesture SelectPreviousLookupResultKeyGesture { get; private set; } = new(Key.Up, ModifierKeys.Alt);
    public KeyGesture MineSelectedLookupResultKeyGesture { get; private set; } = new(Key.D5, ModifierKeys.Alt);

    #endregion

    #region Advanced

    public int MaxSearchLength { get; private set; } = 37;
    public int MaxNumResultsNotInMiningMode { get; private set; } = 7;
    public string SearchUrl { get; private set; } = "https://www.google.com/search?q={SearchTerm}&hl=ja";
    public string BrowserPath { get; private set; } = "";
    public bool DisableHotkeys { get; set; } // = false;
    public bool GlobalHotKeys { get; private set; } = true;
    public bool StopIncreasingTimeStatWhenMinimized { get; private set; } = true;
    public bool StripPunctuationBeforeCalculatingCharacterCount { get; private set; } = true;
    public bool MineToFileInsteadOfAnki { get; private set; } // = false;
    public bool AutoAdjustFontSizesOnResolutionChange { get; private set; } // = false;

    #endregion

    private static readonly ComboBoxItem[] s_japaneseFonts = WindowsUtils.FindJapaneseFonts();
    private static readonly ComboBoxItem[] s_popupJapaneseFonts = WindowsUtils.CloneComboBoxItems(s_japaneseFonts);
    private SkinType Theme { get; set; } = SkinType.Dark;

    private ConfigManager()
    {
    }

    public static void ResetConfigs()
    {
        using SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        Instance.SaveBeforeClosing(connection);
        ConfigDBManager.DeleteAllSettingsFromProfile("MainWindowTopPosition", "MainWindowLeftPosition");

        ConfigManager newInstance = new();
        ConfigDBManager.InsertSetting(connection, nameof(Theme), newInstance.Theme.ToString());
        ConfigDBManager.InsertSetting(connection, nameof(StripPunctuationBeforeCalculatingCharacterCount), newInstance.StripPunctuationBeforeCalculatingCharacterCount.ToString());

        newInstance.Theme = Instance.Theme;
        newInstance.StripPunctuationBeforeCalculatingCharacterCount = Instance.StripPunctuationBeforeCalculatingCharacterCount;

        Instance = newInstance;
        CoreConfigManager.CreateNewCoreConfigManager();
        Instance.ApplyPreferences(connection);

        ConfigDBManager.AnalyzeAndVacuum(connection);
    }

    public void ApplyPreferences(SqliteConnection connection)
    {
        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        coreConfigManager.ApplyPreferences(connection);

        MainWindow mainWindow = MainWindow.Instance;

        SkinType theme = ConfigDBManager.GetValueFromConfig(connection, Theme, nameof(Theme), Enum.TryParse);
        if (theme != Theme)
        {
            Theme = theme;
            WindowsUtils.ChangeTheme(Theme);
            mainWindow.UpdateLayout();
        }

        if (coreConfigManager.CaptureTextFromClipboard)
        {
            WinApi.SubscribeToClipboardChanged(mainWindow.WindowHandle);
        }
        else
        {
            WinApi.UnsubscribeFromClipboardChanged(mainWindow.WindowHandle);
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
                StatsUtils.IncrementStat(StatType.Characters, -(long)(StatsUtils.SessionStats.Characters - characterCount));
                StatsUtils.IncrementStat(StatType.Lines, -(long)(StatsUtils.SessionStats.Lines - lineCount));
            }
            else
            {
                StatsUtils.IncrementStat(StatType.Characters, (long)(characterCount - StatsUtils.SessionStats.Characters));
                StatsUtils.IncrementStat(StatType.Lines, (long)(lineCount - StatsUtils.SessionStats.Lines));
            }
        }

        LookupOnClickMouseButton = ConfigDBManager.GetValueFromConfig(connection, LookupOnClickMouseButton, nameof(LookupOnClickMouseButton), Enum.TryParse);
        MiningModeMouseButton = ConfigDBManager.GetValueFromConfig(connection, MiningModeMouseButton, nameof(MiningModeMouseButton), Enum.TryParse);
        MineMouseButton = ConfigDBManager.GetValueFromConfig(connection, MineMouseButton, nameof(MineMouseButton), Enum.TryParse);
        CopyPrimarySpellingToClipboardMouseButton = ConfigDBManager.GetValueFromConfig(connection, CopyPrimarySpellingToClipboardMouseButton, nameof(CopyPrimarySpellingToClipboardMouseButton), Enum.TryParse);

        MainWindowTextVerticalAlignment = ConfigDBManager.GetValueFromConfig(connection, MainWindowTextVerticalAlignment, nameof(MainWindowTextVerticalAlignment), Enum.TryParse);
        mainWindow.MainTextBox.VerticalContentAlignment = MainWindowTextVerticalAlignment;

        AutoAdjustFontSizesOnResolutionChange = ConfigDBManager.GetValueFromConfig(connection, AutoAdjustFontSizesOnResolutionChange, nameof(AutoAdjustFontSizesOnResolutionChange), bool.TryParse);
        HighlightLongestMatch = ConfigDBManager.GetValueFromConfig(connection, HighlightLongestMatch, nameof(HighlightLongestMatch), bool.TryParse);
        AutoPlayAudio = ConfigDBManager.GetValueFromConfig(connection, AutoPlayAudio, nameof(AutoPlayAudio), bool.TryParse);
        GlobalHotKeys = ConfigDBManager.GetValueFromConfig(connection, GlobalHotKeys, nameof(GlobalHotKeys), bool.TryParse);
        StopIncreasingTimeStatWhenMinimized = ConfigDBManager.GetValueFromConfig(connection, StopIncreasingTimeStatWhenMinimized, nameof(StopIncreasingTimeStatWhenMinimized), bool.TryParse);
        MineToFileInsteadOfAnki = ConfigDBManager.GetValueFromConfig(connection, MineToFileInsteadOfAnki, nameof(MineToFileInsteadOfAnki), bool.TryParse);
        AlwaysOnTop = ConfigDBManager.GetValueFromConfig(connection, AlwaysOnTop, nameof(AlwaysOnTop), bool.TryParse);
        mainWindow.Topmost = AlwaysOnTop;

        RequireLookupKeyPress = ConfigDBManager.GetValueFromConfig(connection, RequireLookupKeyPress, nameof(RequireLookupKeyPress), bool.TryParse);
        DisableHotkeys = ConfigDBManager.GetValueFromConfig(connection, DisableHotkeys, nameof(DisableHotkeys), bool.TryParse);

        Focusable = ConfigDBManager.GetValueFromConfig(connection, Focusable, nameof(Focusable), bool.TryParse);
        if (Focusable)
        {
            WinApi.AllowActivation(mainWindow.WindowHandle);
        }
        else
        {
            WinApi.PreventActivation(mainWindow.WindowHandle);
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
        if (mainWindow.MainTextBox.IsReadOnly != TextBoxIsReadOnly)
        {
            mainWindow.MainTextBox.IsReadOnly = TextBoxIsReadOnly;
            mainWindow.MainTextBox.IsUndoEnabled = !TextBoxIsReadOnly;
            mainWindow.MainTextBox.AcceptsReturn = !TextBoxIsReadOnly;
            mainWindow.MainTextBox.AcceptsTab = !TextBoxIsReadOnly;
            mainWindow.MainTextBox.UndoLimit = TextBoxIsReadOnly ? 0 : -1;
        }

        AlwaysShowMainTextBoxCaret = ConfigDBManager.GetValueFromConfig(connection, AlwaysShowMainTextBoxCaret, nameof(AlwaysShowMainTextBoxCaret), bool.TryParse);
        mainWindow.MainTextBox.IsReadOnlyCaretVisible = AlwaysShowMainTextBoxCaret;

        HorizontallyCenterMainWindowText = ConfigDBManager.GetValueFromConfig(connection, HorizontallyCenterMainWindowText, nameof(HorizontallyCenterMainWindowText), bool.TryParse);
        mainWindow.MainTextBox.HorizontalContentAlignment = HorizontallyCenterMainWindowText
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

        DiscardIdenticalText = ConfigDBManager.GetValueFromConfig(connection, DiscardIdenticalText, nameof(DiscardIdenticalText), bool.TryParse);
        MergeSequentialTextsWhenTheyMatch = ConfigDBManager.GetValueFromConfig(connection, MergeSequentialTextsWhenTheyMatch, nameof(MergeSequentialTextsWhenTheyMatch), bool.TryParse);
        AllowPartialMatchingForTextMerge = ConfigDBManager.GetValueFromConfig(connection, AllowPartialMatchingForTextMerge, nameof(AllowPartialMatchingForTextMerge), bool.TryParse);

        HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar = ConfigDBManager.GetValueFromConfig(connection, HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar, nameof(HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar), bool.TryParse);
        mainWindow.ChangeVisibilityOfTitleBarButtons();

        MainTextBoxDropShadowEffectShadowDepth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainTextBoxDropShadowEffectShadowDepth, nameof(MainTextBoxDropShadowEffectShadowDepth), double.TryParse);
        MainTextBoxDropShadowEffectDirection = ConfigDBManager.GetValueFromConfig(connection, MainTextBoxDropShadowEffectDirection, nameof(MainTextBoxDropShadowEffectDirection), int.TryParse);
        MainTextBoxDropShadowEffectBlurRadius = ConfigDBManager.GetValueFromConfig(connection, MainTextBoxDropShadowEffectBlurRadius, nameof(MainTextBoxDropShadowEffectBlurRadius), int.TryParse);
        MainTextBoxDropShadowEffectBlurOpacity = ConfigDBManager.GetValueFromConfig(connection, MainTextBoxDropShadowEffectBlurOpacity, nameof(MainTextBoxDropShadowEffectBlurOpacity), int.TryParse);
        TextBoxApplyDropShadowEffect = ConfigDBManager.GetValueFromConfig(connection, TextBoxApplyDropShadowEffect, nameof(TextBoxApplyDropShadowEffect), bool.TryParse);
        MainTextBoxDropShadowEffectColor = ConfigUtils.GetColorFromConfig(connection, MainTextBoxDropShadowEffectColor, nameof(MainTextBoxDropShadowEffectColor));
        if (TextBoxApplyDropShadowEffect)
        {
            DropShadowEffect dropShadowEffect = new()
            {
                Direction = MainTextBoxDropShadowEffectDirection,
                BlurRadius = MainTextBoxDropShadowEffectBlurRadius,
                ShadowDepth = MainTextBoxDropShadowEffectShadowDepth,
                Opacity = MainTextBoxDropShadowEffectBlurOpacity / 100.0,
                Color = MainTextBoxDropShadowEffectColor,
                RenderingBias = RenderingBias.Quality
            };

            dropShadowEffect.Freeze();
            mainWindow.MainTextBox.Effect = dropShadowEffect;
        }

        else
        {
            mainWindow.MainTextBox.Effect = null;
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
            mainWindow.MainTextBox.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.BlockLineHeight);
            mainWindow.MainTextBox.SetValue(TextBlock.LineHeightProperty, TextBoxCustomLineHeight);
        }
        else
        {
            mainWindow.MainTextBox.SetValue(TextBlock.LineStackingStrategyProperty, LineStackingStrategy.MaxHeight);
            mainWindow.MainTextBox.SetValue(TextBlock.LineHeightProperty, double.NaN);
        }

        AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds, nameof(AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds), double.TryParse);
        PopupWindowUtils.PopupAutoHideTimer.Enabled = false;
        PopupWindowUtils.PopupAutoHideTimer.Interval = AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;

        DpiScale dpi = WindowsUtils.Dpi;

        PopupXOffset = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupXOffset, nameof(PopupXOffset), double.TryParse);
        WindowsUtils.DpiAwareXOffset = PopupXOffset * dpi.DpiScaleX;

        PopupYOffset = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupYOffset, nameof(PopupYOffset), double.TryParse);
        WindowsUtils.DpiAwareYOffset = PopupYOffset * dpi.DpiScaleY;

        PopupMaxWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMaxWidth, nameof(PopupMaxWidth), double.TryParse);
        PopupMaxHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMaxHeight, nameof(PopupMaxHeight), double.TryParse);
        PopupMinWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMinWidth, nameof(PopupMinWidth), double.TryParse);
        PopupMinHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, PopupMinHeight, nameof(PopupMinHeight), double.TryParse);

        FixedPopupXPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, FixedPopupXPosition, nameof(FixedPopupXPosition), double.TryParse);
        FixedPopupYPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, FixedPopupYPosition, nameof(FixedPopupYPosition), double.TryParse);

        mainWindow.OpacitySlider.Value = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, mainWindow.OpacitySlider.Value, "MainWindowOpacity", double.TryParse);
        mainWindow.FontSizeSlider.Value = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, mainWindow.FontSizeSlider.Value, "MainWindowFontSize", double.TryParse);
        MainWindowBackgroundOpacityOnUnhover = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowBackgroundOpacityOnUnhover, nameof(MainWindowBackgroundOpacityOnUnhover), double.TryParse);

        MainWindowHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowHeight, nameof(MainWindowHeight), double.TryParse);
        MainWindowWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowWidth, nameof(MainWindowWidth), double.TryParse);
        MainWindowMaxDynamicWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMaxDynamicWidth, nameof(MainWindowMaxDynamicWidth), double.TryParse);
        MainWindowMaxDynamicHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMaxDynamicHeight, nameof(MainWindowMaxDynamicHeight), double.TryParse);
        MainWindowMinDynamicWidth = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMinDynamicWidth, nameof(MainWindowMinDynamicWidth), double.TryParse);
        MainWindowMinDynamicHeight = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowMinDynamicHeight, nameof(MainWindowMinDynamicHeight), double.TryParse);
        WindowsUtils.SetSizeToContent(MainWindowDynamicWidth, MainWindowDynamicHeight, MainWindowMaxDynamicWidth, MainWindowMaxDynamicHeight, MainWindowMinDynamicWidth, MainWindowMinDynamicHeight, MainWindowWidth, MainWindowHeight, mainWindow);
        mainWindow.WidthBeforeResolutionChange = MainWindowWidth;
        mainWindow.HeightBeforeResolutionChange = MainWindowHeight;

        double mainWindowTop = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, mainWindow.Top, "MainWindowTopPosition", double.TryParse);
        double mainWindowLeft = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, mainWindow.Left, "MainWindowLeftPosition", double.TryParse);
        WinApi.MoveWindowToPosition(mainWindow.WindowHandle, mainWindowLeft, mainWindowTop);

        mainWindow.TopPositionBeforeResolutionChange = mainWindow.Top;
        mainWindow.LeftPositionBeforeResolutionChange = mainWindow.Left;

        RepositionMainWindowOnTextChangeByBottomPosition = ConfigDBManager.GetValueFromConfig(connection, RepositionMainWindowOnTextChangeByBottomPosition, nameof(RepositionMainWindowOnTextChangeByBottomPosition), bool.TryParse);
        RepositionMainWindowOnTextChangeByRightPosition = ConfigDBManager.GetValueFromConfig(connection, RepositionMainWindowOnTextChangeByRightPosition, nameof(RepositionMainWindowOnTextChangeByRightPosition), bool.TryParse);
        MainWindowFixedBottomPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowFixedBottomPosition, nameof(MainWindowFixedBottomPosition), double.TryParse);
        MainWindowFixedRightPosition = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, MainWindowFixedRightPosition, nameof(MainWindowFixedRightPosition), double.TryParse);
        mainWindow.UpdatePosition();

        mainWindow.MainGrid.Opacity = TextOnlyVisibleOnHover && !mainWindow.IsMouseOver && !PreferencesWindow.IsItVisible() ? 0 : 1;

        // MAKE SURE YOU FREEZE ANY NEW FREEZABLE OBJECTS YOU ADD
        // OR THE PROGRAM WILL CRASH AND BURN
        MainWindowTextColor = ConfigUtils.GetFrozenBrushFromConfig(connection, MainWindowTextColor, nameof(MainWindowTextColor));
        MainWindowBacklogTextColor = ConfigUtils.GetFrozenBrushFromConfig(connection, MainWindowBacklogTextColor, nameof(MainWindowBacklogTextColor));

        mainWindow.MainTextBox.Foreground = !EnableBacklog || mainWindow.MainTextBox.Text == BacklogUtils.Backlog.LastOrDefault("")
            ? MainWindowTextColor
            : MainWindowBacklogTextColor;

        mainWindow.MainTextBox.CaretBrush = MainWindowTextColor;

        PrimarySpellingColor = ConfigUtils.GetFrozenBrushFromConfig(connection, PrimarySpellingColor, nameof(PrimarySpellingColor));
        ReadingsColor = ConfigUtils.GetFrozenBrushFromConfig(connection, ReadingsColor, nameof(ReadingsColor));
        AlternativeSpellingsColor = ConfigUtils.GetFrozenBrushFromConfig(connection, AlternativeSpellingsColor, nameof(AlternativeSpellingsColor));
        DefinitionsColor = ConfigUtils.GetFrozenBrushFromConfig(connection, DefinitionsColor, nameof(DefinitionsColor));
        FrequencyColor = ConfigUtils.GetFrozenBrushFromConfig(connection, FrequencyColor, nameof(FrequencyColor));
        DeconjugationInfoColor = ConfigUtils.GetFrozenBrushFromConfig(connection, DeconjugationInfoColor, nameof(DeconjugationInfoColor));

        SeparatorColor = ConfigUtils.GetFrozenBrushFromConfig(connection, SeparatorColor, nameof(SeparatorColor));

        DictTypeColor = ConfigUtils.GetFrozenBrushFromConfig(connection, DictTypeColor, nameof(DictTypeColor));

        HighlightColor = ConfigUtils.GetFrozenBrushFromConfig(connection, HighlightColor, nameof(HighlightColor));
        mainWindow.MainTextBox.SelectionBrush = HighlightColor;

        PopupBackgroundColor = ConfigUtils.GetBrushFromConfig(connection, PopupBackgroundColor, nameof(PopupBackgroundColor));
        PopupBackgroundColor.Opacity = ConfigDBManager.GetNumberWithDecimalPointFromConfig(connection, 80.0, "PopupOpacity", double.TryParse) / 100;
        PopupBackgroundColor.Freeze();

        mainWindow.Background = ConfigUtils.GetBrushFromConfig(connection, mainWindow.Background, "MainWindowBackgroundColor");

        mainWindow.Background.Opacity = ChangeMainWindowBackgroundOpacityOnUnhover && !mainWindow.IsMouseOver && !PreferencesWindow.IsItVisible()
            ? MainWindowBackgroundOpacityOnUnhover / 100
            : mainWindow.OpacitySlider.Value / 100;

        WinApi.UnregisterAllGlobalHotKeys(mainWindow.WindowHandle);
        KeyGestureUtils.GlobalKeyGestureDict.Clear();
        KeyGestureUtils.GlobalKeyGestureNameToIntDict.Clear();

        DisableHotkeysKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(DisableHotkeysKeyGesture), DisableHotkeysKeyGesture);
        MiningModeKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(MiningModeKeyGesture), MiningModeKeyGesture);
        PlayAudioKeyGesture = KeyGestureUtils.GetKeyGestureFromConfig(connection, nameof(PlayAudioKeyGesture), PlayAudioKeyGesture);
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
            WinApi.RegisterAllGlobalHotKeys(mainWindow.WindowHandle);
        }

        mainWindow.AddNameMenuItem.SetInputGestureText(ShowAddNameWindowKeyGesture);
        mainWindow.AddWordMenuItem.SetInputGestureText(ShowAddWordWindowKeyGesture);
        mainWindow.SearchMenuItem.SetInputGestureText(SearchWithBrowserKeyGesture);
        mainWindow.PreferencesMenuItem.SetInputGestureText(ShowPreferencesWindowKeyGesture);
        mainWindow.ManageDictionariesMenuItem.SetInputGestureText(ShowManageDictionariesWindowKeyGesture);
        mainWindow.ManageFrequenciesMenuItem.SetInputGestureText(ShowManageFrequenciesWindowKeyGesture);
        mainWindow.ManageAudioSourcesMenuItem.SetInputGestureText(ShowManageAudioSourcesWindowKeyGesture);
        mainWindow.StatsMenuItem.SetInputGestureText(ShowStatsKeyGesture);

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
                    .Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase);

                if (Uri.IsWellFormedUriString(searchUrlStr.Replace("{SearchTerm}", "", StringComparison.OrdinalIgnoreCase), UriKind.Absolute))
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
            mainWindow.MainTextBox.FontFamily = new FontFamily(mainWindowFontStr);
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
                    throw new ArgumentOutOfRangeException(null, popupPositionRelativeToCursorStr, "Invalid PopupPositionRelativeToCursor");
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
                    throw new ArgumentOutOfRangeException(null, popupFlipStr, "Invalid PopupFlip");
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

        PopupWindow? currentPopupWindow = mainWindow.FirstPopupWindow;
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

    public void LoadPreferenceWindow(PreferencesWindow preferenceWindow)
    {
        preferenceWindow.JLVersionTextBlock.Text = string.Create(CultureInfo.InvariantCulture, $"v{Utils.JLVersion}");
        preferenceWindow.DisableHotkeysKeyGestureTextBox.Text = DisableHotkeysKeyGesture.ToFormattedString();
        preferenceWindow.MiningModeKeyGestureTextBox.Text = MiningModeKeyGesture.ToFormattedString();
        preferenceWindow.PlayAudioKeyGestureTextBox.Text = PlayAudioKeyGesture.ToFormattedString();
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

        MainWindow mainWindow = MainWindow.Instance;

        WindowsUtils.SetButtonColor(preferenceWindow.HighlightColorButton, HighlightColor);
        WindowsUtils.SetButtonColor(preferenceWindow.MainWindowBackgroundColorButton, mainWindow.Background.CloneCurrentValue());
        WindowsUtils.SetButtonColor(preferenceWindow.TextBoxTextColorButton, MainWindowTextColor);
        WindowsUtils.SetButtonColor(preferenceWindow.TextBoxBacklogTextColorButton, MainWindowBacklogTextColor);
        WindowsUtils.SetButtonColor(preferenceWindow.MainTextBoxDropShadowEffectColorButton, MainTextBoxDropShadowEffectColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DeconjugationInfoColorButton, DeconjugationInfoColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DefinitionsColorButton, DefinitionsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.FrequencyColorButton, FrequencyColor);
        WindowsUtils.SetButtonColor(preferenceWindow.PrimarySpellingColorButton, PrimarySpellingColor);
        WindowsUtils.SetButtonColor(preferenceWindow.ReadingsColorButton, ReadingsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.AlternativeSpellingsColorButton, AlternativeSpellingsColor);
        WindowsUtils.SetButtonColor(preferenceWindow.PopupBackgroundColorButton, PopupBackgroundColor);
        WindowsUtils.SetButtonColor(preferenceWindow.SeparatorColorButton, SeparatorColor);
        WindowsUtils.SetButtonColor(preferenceWindow.DictTypeColorButton, DictTypeColor);


        CoreConfigManager coreConfigManager = CoreConfigManager.Instance;
        preferenceWindow.SearchUrlTextBox.Text = SearchUrl;
        preferenceWindow.BrowserPathTextBox.Text = BrowserPath;
        preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
        preferenceWindow.AnkiUriTextBox.Text = coreConfigManager.AnkiConnectUri.OriginalString;
        preferenceWindow.WebSocketUriTextBox.Text = coreConfigManager.WebSocketUri.OriginalString;
        preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = coreConfigManager.ForceSyncAnki;
        preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked = coreConfigManager.AllowDuplicateCards;
        preferenceWindow.CheckForDuplicateCardsCheckBox.IsChecked = coreConfigManager.CheckForDuplicateCards;
        preferenceWindow.AutoAdjustFontSizesOnResolutionChangeCheckBox.IsChecked = AutoAdjustFontSizesOnResolutionChange;
        preferenceWindow.HighlightLongestMatchCheckBox.IsChecked = HighlightLongestMatch;
        preferenceWindow.AutoPlayAudioCheckBox.IsChecked = AutoPlayAudio;
        preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked = coreConfigManager.CheckForJLUpdatesOnStartUp;
        preferenceWindow.TrackTermLookupCountsCheckBox.IsChecked = coreConfigManager.TrackTermLookupCounts;
        preferenceWindow.GlobalHotKeysCheckBox.IsChecked = GlobalHotKeys;
        preferenceWindow.StopIncreasingTimeStatWhenMinimizedCheckBox.IsChecked = StopIncreasingTimeStatWhenMinimized;
        preferenceWindow.StripPunctuationBeforeCalculatingCharacterCountCheckBox.IsChecked = StripPunctuationBeforeCalculatingCharacterCount;
        preferenceWindow.MineToFileInsteadOfAnkiCheckBox.IsChecked = MineToFileInsteadOfAnki;
        preferenceWindow.AlwaysOnTopCheckBox.IsChecked = AlwaysOnTop;
        preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked = RequireLookupKeyPress;
        preferenceWindow.DisableHotkeysCheckBox.IsChecked = DisableHotkeys;
        preferenceWindow.FocusableCheckBox.IsChecked = Focusable;
        preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked = TextOnlyVisibleOnHover;
        preferenceWindow.AnkiIntegrationCheckBox.IsChecked = coreConfigManager.AnkiIntegration;

        preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked = MainWindowDynamicWidth;
        preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked = MainWindowDynamicHeight;

        preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value = MainWindowMaxDynamicWidth;
        preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value = MainWindowMaxDynamicHeight;
        preferenceWindow.MainWindowMinDynamicWidthNumericUpDown.Value = MainWindowMinDynamicWidth;
        preferenceWindow.MainWindowMinDynamicHeightNumericUpDown.Value = MainWindowMinDynamicHeight;

        preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
        preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;
        preferenceWindow.TextBoxFontSizeNumericUpDown.Value = mainWindow.FontSizeSlider.Value;
        preferenceWindow.MainWindowOpacityNumericUpDown.Value = mainWindow.OpacitySlider.Value;

        preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked = ChangeMainWindowBackgroundOpacityOnUnhover;
        preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value = MainWindowBackgroundOpacityOnUnhover;

        preferenceWindow.MainTextBoxDropShadowEffectDirectionNumericUpDown.Value = MainTextBoxDropShadowEffectDirection;
        preferenceWindow.MainTextBoxDropShadowEffectBlurRadiusNumericUpDown.Value = MainTextBoxDropShadowEffectBlurRadius;
        preferenceWindow.MainTextBoxDropShadowEffectShadowDepthNumericUpDown.Value = MainTextBoxDropShadowEffectShadowDepth;
        preferenceWindow.MainTextBoxDropShadowEffectBlurOpacityNumericUpDown.Value = MainTextBoxDropShadowEffectBlurOpacity;

        preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked = TextBoxIsReadOnly;
        preferenceWindow.AlwaysShowMainTextBoxCaretCheckBox.IsChecked = AlwaysShowMainTextBoxCaret;
        preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked = coreConfigManager.TextBoxTrimWhiteSpaceCharacters;
        preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked = coreConfigManager.TextBoxRemoveNewlines;
        preferenceWindow.TextBoxApplyDropShadowEffectCheckBox.IsChecked = TextBoxApplyDropShadowEffect;
        preferenceWindow.CaptureTextFromClipboardCheckBox.IsChecked = coreConfigManager.CaptureTextFromClipboard;
        preferenceWindow.CaptureTextFromWebSocketCheckBox.IsChecked = coreConfigManager.CaptureTextFromWebSocket;
        preferenceWindow.AutoReconnectToWebSocketCheckBox.IsChecked = coreConfigManager.AutoReconnectToWebSocket;
        preferenceWindow.OnlyCaptureTextWithJapaneseCharsCheckBox.IsChecked = OnlyCaptureTextWithJapaneseChars;
        preferenceWindow.DisableLookupsForNonJapaneseCharsInMainWindowCheckBox.IsChecked = DisableLookupsForNonJapaneseCharsInMainWindow;
        preferenceWindow.MainWindowFocusOnHoverCheckBox.IsChecked = MainWindowFocusOnHover;
        preferenceWindow.SteppedBacklogWithMouseWheelCheckBox.IsChecked = SteppedBacklogWithMouseWheel;
        preferenceWindow.EnableBacklogCheckBox.IsChecked = EnableBacklog;
        preferenceWindow.AutoSaveBacklogBeforeClosingCheckBox.IsChecked = AutoSaveBacklogBeforeClosing;
        preferenceWindow.TextToSpeechOnTextChangeCheckBox.IsChecked = TextToSpeechOnTextChange;
        preferenceWindow.HidePopupsOnTextChangeCheckBox.IsChecked = HidePopupsOnTextChange;
        preferenceWindow.DiscardIdenticalTextCheckBox.IsChecked = DiscardIdenticalText;
        preferenceWindow.MergeSequentialTextsWhenTheyMatchCheckBox.IsChecked = MergeSequentialTextsWhenTheyMatch;
        preferenceWindow.AllowPartialMatchingForTextMergeCheckBox.IsChecked = AllowPartialMatchingForTextMerge;
        preferenceWindow.TextBoxUseCustomLineHeightCheckBox.IsChecked = TextBoxUseCustomLineHeight;
        preferenceWindow.ToggleHideAllTitleBarButtonsWhenMouseIsNotOverTitleBarCheckBox.IsChecked = HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar;
        preferenceWindow.HorizontallyCenterMainWindowTextCheckBox.IsChecked = HorizontallyCenterMainWindowText;
        preferenceWindow.MainWindowFontComboBox.ItemsSource = s_japaneseFonts;
        preferenceWindow.MainWindowFontComboBox.SelectedIndex = Array.FindIndex(s_japaneseFonts, f =>
            f.Content.ToString() == mainWindow.MainTextBox.FontFamily.Source);

        if (preferenceWindow.MainWindowFontComboBox.SelectedIndex < 0)
        {
            preferenceWindow.MainWindowFontComboBox.SelectedIndex = 0;
        }

        preferenceWindow.PopupFontComboBox.ItemsSource = s_popupJapaneseFonts;
        preferenceWindow.PopupFontComboBox.SelectedIndex =
            Array.FindIndex(s_popupJapaneseFonts, f => f.Content.ToString() == PopupFont.Source);

        if (preferenceWindow.PopupFontComboBox.SelectedIndex < 0)
        {
            preferenceWindow.PopupFontComboBox.SelectedIndex = 0;
        }

        Rectangle bounds = WindowsUtils.ActiveScreen.Bounds;
        preferenceWindow.PopupMaxHeightNumericUpDown.Maximum = bounds.Height;
        preferenceWindow.PopupMaxWidthNumericUpDown.Maximum = bounds.Width;

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
        preferenceWindow.RepositionMainWindowOnTextChangeByBottomPositionCheckBox.IsChecked = RepositionMainWindowOnTextChangeByBottomPosition;
        preferenceWindow.RepositionMainWindowOnTextChangeByRightPositionCheckBox.IsChecked = RepositionMainWindowOnTextChangeByRightPosition;

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
        preferenceWindow.MainWindowFixedRightPositionNumericUpDown.Value = MainWindowFixedRightPosition;
        preferenceWindow.MainWindowFixedBottomPositionNumericUpDown.Value = MainWindowFixedBottomPosition;

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

    public async Task SavePreferences(PreferencesWindow preferenceWindow)
    {
        SqliteConnection connection = ConfigDBManager.CreateReadWriteDBConnection();
        await using (connection.ConfigureAwait(true))
        {
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(DisableHotkeysKeyGesture), preferenceWindow.DisableHotkeysKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(MiningModeKeyGesture), preferenceWindow.MiningModeKeyGestureTextBox.Text);
            KeyGestureUtils.UpdateKeyGesture(connection, nameof(PlayAudioKeyGesture), preferenceWindow.PlayAudioKeyGestureTextBox.Text);
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

            ConfigDBManager.UpdateSetting(connection, nameof(MainTextBoxDropShadowEffectDirection), preferenceWindow.MainTextBoxDropShadowEffectDirectionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));
            ConfigDBManager.UpdateSetting(connection, nameof(MainTextBoxDropShadowEffectBlurRadius), preferenceWindow.MainTextBoxDropShadowEffectBlurRadiusNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));
            ConfigDBManager.UpdateSetting(connection, nameof(MainTextBoxDropShadowEffectShadowDepth), preferenceWindow.MainTextBoxDropShadowEffectShadowDepthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));
            ConfigDBManager.UpdateSetting(connection, nameof(MainTextBoxDropShadowEffectBlurOpacity), preferenceWindow.MainTextBoxDropShadowEffectBlurOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));
            ConfigDBManager.UpdateSetting(connection, nameof(MainTextBoxDropShadowEffectColor), preferenceWindow.MainTextBoxDropShadowEffectColorButton.Tag.ToString()!);

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

            ConfigDBManager.UpdateSetting(connection, nameof(DiscardIdenticalText),
                preferenceWindow.DiscardIdenticalTextCheckBox.IsChecked.ToString()!);

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

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.ForceSyncAnki),
                preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.AllowDuplicateCards),
                preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.CheckForDuplicateCards),
                preferenceWindow.CheckForDuplicateCardsCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(AutoAdjustFontSizesOnResolutionChange),
                preferenceWindow.AutoAdjustFontSizesOnResolutionChangeCheckBox.IsChecked.ToString()!);

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

            ConfigDBManager.UpdateSetting(connection, nameof(CoreConfigManager.TrackTermLookupCounts),
                preferenceWindow.TrackTermLookupCountsCheckBox.IsChecked.ToString()!);

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

            ConfigDBManager.UpdateSetting(connection, nameof(RepositionMainWindowOnTextChangeByBottomPosition),
                preferenceWindow.RepositionMainWindowOnTextChangeByBottomPositionCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(RepositionMainWindowOnTextChangeByRightPosition),
                preferenceWindow.RepositionMainWindowOnTextChangeByRightPositionCheckBox.IsChecked.ToString()!);

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowFixedBottomPosition),
                preferenceWindow.MainWindowFixedBottomPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, nameof(MainWindowFixedRightPosition),
                preferenceWindow.MainWindowFixedRightPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture));

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

            MainWindow mainWindow = MainWindow.Instance;
            DpiScale dpi = WindowsUtils.Dpi;
            ConfigDBManager.UpdateSetting(connection, "MainWindowTopPosition",
                (mainWindow.Top * dpi.DpiScaleY).ToString(CultureInfo.InvariantCulture));

            ConfigDBManager.UpdateSetting(connection, "MainWindowLeftPosition",
                (mainWindow.Left * dpi.DpiScaleX).ToString(CultureInfo.InvariantCulture));

            ApplyPreferences(connection);
        }

        if (preferenceWindow.SetAnkiConfig)
        {
            await preferenceWindow.SaveMiningSetup().ConfigureAwait(false);
        }
    }

    public void SaveBeforeClosing(SqliteConnection connection)
    {
        MainWindow mainWindow = MainWindow.Instance;
        ConfigDBManager.UpdateSetting(connection, "MainWindowFontSize",
            mainWindow.FontSizeSlider.Value.ToString(CultureInfo.InvariantCulture));

        ConfigDBManager.UpdateSetting(connection, "MainWindowOpacity",
            mainWindow.OpacitySlider.Value.ToString(CultureInfo.InvariantCulture));

        double mainWindowHeight = MainWindowHeight > mainWindow.MinHeight
            ? MainWindowHeight <= SystemParameters.VirtualScreenHeight
                ? MainWindowHeight
                : SystemParameters.VirtualScreenHeight
            : mainWindow.MinHeight;
        ConfigDBManager.UpdateSetting(connection, nameof(MainWindowHeight), mainWindowHeight.ToString(CultureInfo.InvariantCulture));

        double mainWindowWidth = MainWindowWidth > mainWindow.MinWidth
            ? MainWindowWidth <= SystemParameters.VirtualScreenWidth
                ? MainWindowWidth
                : SystemParameters.VirtualScreenWidth
            : mainWindow.MinWidth;
        ConfigDBManager.UpdateSetting(connection, nameof(MainWindowWidth), mainWindowWidth.ToString(CultureInfo.InvariantCulture));

        Rectangle bounds = WindowsUtils.ActiveScreen.Bounds;
        DpiScale dpi = WindowsUtils.Dpi;
        double mainWindowTopPosition = mainWindow.Top >= SystemParameters.VirtualScreenTop
            ? mainWindow.Top + mainWindow.Height <= SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight
                ? mainWindow.Top * dpi.DpiScaleY
                : Math.Max(SystemParameters.VirtualScreenTop, SystemParameters.VirtualScreenTop + SystemParameters.VirtualScreenHeight - mainWindow.Height) * dpi.DpiScaleY
            : bounds.Y;
        ConfigDBManager.UpdateSetting(connection, "MainWindowTopPosition", mainWindowTopPosition.ToString(CultureInfo.InvariantCulture));

        double mainWindowLeftPosition = mainWindow.Left >= SystemParameters.VirtualScreenLeft
            ? mainWindow.Left + mainWindow.Width <= SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth
                ? mainWindow.Left * dpi.DpiScaleX
                : Math.Max(SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenLeft + SystemParameters.VirtualScreenWidth - mainWindow.Width) * dpi.DpiScaleX
            : bounds.X;
        ConfigDBManager.UpdateSetting(connection, "MainWindowLeftPosition", mainWindowLeftPosition.ToString(CultureInfo.InvariantCulture));

        ConfigDBManager.AnalyzeAndVacuum(connection);
    }
}
