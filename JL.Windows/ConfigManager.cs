using System.Configuration;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Xml;
using HandyControl.Data;
using JL.Core;
using JL.Core.Network;
using JL.Core.Profile;
using JL.Core.Statistics;
using JL.Core.Utilities;
using JL.Windows.GUI;
using JL.Windows.Utilities;

namespace JL.Windows;

internal static class ConfigManager
{
    #region General
    public static bool InactiveLookupMode { get; set; } = false;
    public static Brush HighlightColor { get; private set; } = Brushes.AliceBlue;
    public static bool RequireLookupKeyPress { get; private set; } = false;
    public static bool LookupOnSelectOnly { get; private set; } = false;
    public static bool LookupOnMouseClickOnly { get; private set; } = false;
    public static bool AutoAdjustFontSizesOnResolutionChange { get; private set; } = true;

    public static KeyGesture LookupKeyKeyGesture { get; private set; } = new(Key.LeftShift, ModifierKeys.None);
    public static bool HighlightLongestMatch { get; private set; } = false;
    public static bool AutoPlayAudio { get; private set; } = false;
    public static bool CheckForJLUpdatesOnStartUp { get; private set; } = true;
    public static bool DisableHotkeys { get; set; } = false;
    public static bool Focusable { get; private set; } = true;
    public static MouseButton MiningModeMouseButton { get; private set; } = MouseButton.Middle;
    public static MouseButton LookupOnClickMouseButton { get; private set; } = MouseButton.Left;

    #endregion

    #region MainWindow

    public static double MainWindowWidth { get; set; } = 800;
    public static double MainWindowHeight { get; set; } = 200;
    public static bool MainWindowDynamicHeight { get; private set; } = true;
    public static bool MainWindowDynamicWidth { get; private set; } = false;
    public static double MainWindowMaxDynamicWidth { get; private set; } = 800;
    public static double MainWindowMaxDynamicHeight { get; private set; } = 269;
    public static Brush MainWindowTextColor { get; private set; } = Brushes.White;
    public static Brush MainWindowBacklogTextColor { get; private set; } = Brushes.Bisque;
    public static bool AlwaysOnTop { get; set; } = true;
    public static bool TextOnlyVisibleOnHover { get; set; } = false;
    public static bool ChangeMainWindowBackgroundOpacityOnUnhover { get; private set; } = false;
    public static double MainWindowBackgroundOpacityOnUnhover { get; private set; } = 0.2; // 0.2-100
    public static bool TextBoxTrimWhiteSpaceCharacters { get; private set; } = true;
    public static bool TextBoxRemoveNewlines { get; private set; } = false;
    public static bool TextBoxIsReadOnly { get; set; } = true;
    private static bool TextBoxApplyDropShadowEffect { get; set; } = true;
    public static bool CaptureTextFromClipboard { get; set; } = true;
    public static bool OnlyCaptureTextWithJapaneseChars { get; private set; } = true;
    public static bool DisableLookupsForNonJapaneseCharsInMainWindow { get; private set; } = false;
    public static bool MainWindowFocusOnHover { get; private set; } = false;
    public static bool SteppedBacklogWithMouseWheel { get; private set; } = true;
    public static bool HorizontallyCenterMainWindowText { get; private set; } = false;
    public static bool HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar { get; set; } = false;
    public static bool EnableBacklog { get; private set; } = true;
    public static bool AutoSaveBacklogBeforeClosing { get; private set; } = false;
    public static bool TextToSpeechOnTextChange { get; private set; } = false;
    public static bool HidePopupsOnTextChange { get; private set; } = true;
    public static bool AlwaysShowMainTextBoxCaret { get; set; } = false;

    #endregion

    #region Popup

    public static FontFamily PopupFont { get; private set; } = new("Meiryo");
    public static int PopupMaxWidth { get; set; } = 700;
    public static int PopupMaxHeight { get; set; } = 520;
    public static bool PopupDynamicHeight { get; private set; } = true;
    public static bool PopupDynamicWidth { get; private set; } = true;
    public static bool FixedPopupPositioning { get; private set; } = false;
    public static int FixedPopupXPosition { get; set; } = 0;
    public static int FixedPopupYPosition { get; set; } = 0;
    public static bool PopupFocusOnLookup { get; private set; } = false;
    public static bool ShowMiningModeReminder { get; private set; } = true;
    public static bool DisableLookupsForNonJapaneseCharsInPopups { get; private set; } = true;
    public static Brush PopupBackgroundColor { get; private set; } = new SolidColorBrush(Color.FromRgb(0, 0, 0)) { Opacity = 0.7 };
    public static int PopupXOffset { get; set; } = 10;
    public static int PopupYOffset { get; set; } = 20;
    public static bool PopupFlipX { get; private set; } = true;
    public static bool PopupFlipY { get; private set; } = true;
    public static Brush PrimarySpellingColor { get; private set; } = Brushes.Chocolate;
    public static int PrimarySpellingFontSize { get; set; } = 21;
    public static Brush ReadingsColor { get; private set; } = Brushes.Goldenrod;
    public static int ReadingsFontSize { get; set; } = 19;
    public static Brush AlternativeSpellingsColor { get; private set; } = Brushes.White;
    public static int AlternativeSpellingsFontSize { get; set; } = 17;
    public static Brush DefinitionsColor { get; private set; } = Brushes.White;
    public static int DefinitionsFontSize { get; set; } = 17;
    public static Brush FrequencyColor { get; private set; } = Brushes.White;
    public static int FrequencyFontSize { get; set; } = 17;
    public static Brush DeconjugationInfoColor { get; private set; } = Brushes.White;
    public static int DeconjugationInfoFontSize { get; set; } = 17;
    public static Brush DictTypeColor { get; private set; } = Brushes.LightBlue;
    public static int DictTypeFontSize { get; set; } = 15;
    public static Brush SeparatorColor { get; private set; } = Brushes.White;
    public static bool HideDictTabsWithNoResults { get; private set; } = true;
    public static bool AutoHidePopupIfMouseIsNotOverIt { get; private set; } = false;
    public static int AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds { get; private set; } = 2000;
    public static MouseButton MineMouseButton { get; private set; } = MouseButton.Left;
    public static MouseButton CopyPrimarySpellingToClipboardMouseButton { get; private set; } = MouseButton.Middle;

    #endregion

    #region Hotkeys

    public static KeyGesture DisableHotkeysKeyGesture { get; private set; } = new(Key.Pause, ModifierKeys.Alt);
    public static KeyGesture MiningModeKeyGesture { get; private set; } = new(Key.M, ModifierKeys.Windows);
    public static KeyGesture PlayAudioKeyGesture { get; private set; } = new(Key.P, ModifierKeys.Windows);
    public static KeyGesture KanjiModeKeyGesture { get; private set; } = new(Key.K, ModifierKeys.Windows);

    public static KeyGesture ShowManageDictionariesWindowKeyGesture { get; private set; } =
        new(Key.D, ModifierKeys.Windows);

    public static KeyGesture ShowManageFrequenciesWindowKeyGesture { get; private set; } =
        new(Key.F, ModifierKeys.Windows);

    public static KeyGesture ShowPreferencesWindowKeyGesture { get; private set; } = new(Key.L, ModifierKeys.Windows);
    public static KeyGesture ShowAddNameWindowKeyGesture { get; private set; } = new(Key.N, ModifierKeys.Windows);
    public static KeyGesture ShowAddWordWindowKeyGesture { get; private set; } = new(Key.W, ModifierKeys.Windows);
    public static KeyGesture SearchWithBrowserKeyGesture { get; private set; } = new(Key.S, ModifierKeys.Windows);
    public static KeyGesture MousePassThroughModeKeyGesture { get; private set; } = new(Key.T, ModifierKeys.Alt);
    public static KeyGesture SteppedBacklogBackwardsKeyGesture { get; private set; } = new(Key.Left, ModifierKeys.Windows);
    public static KeyGesture SteppedBacklogForwardsKeyGesture { get; private set; } = new(Key.Right, ModifierKeys.Windows);
    public static KeyGesture InactiveLookupModeKeyGesture { get; private set; } = new(Key.Q, ModifierKeys.Windows);
    public static KeyGesture MotivationKeyGesture { get; private set; } = new(Key.O, ModifierKeys.Windows);
    public static KeyGesture ClosePopupKeyGesture { get; private set; } = new(Key.Escape, ModifierKeys.Windows);
    public static KeyGesture ShowStatsKeyGesture { get; private set; } = new(Key.Y, ModifierKeys.Windows);
    public static KeyGesture NextDictKeyGesture { get; private set; } = new(Key.PageDown, ModifierKeys.Windows);
    public static KeyGesture PreviousDictKeyGesture { get; private set; } = new(Key.PageUp, ModifierKeys.Windows);
    public static KeyGesture AlwaysOnTopKeyGesture { get; private set; } = new(Key.R, ModifierKeys.Alt);
    public static KeyGesture TextBoxIsReadOnlyKeyGesture { get; private set; } = new(Key.U, ModifierKeys.Windows);
    public static KeyGesture CaptureTextFromClipboardKeyGesture { get; private set; } = new(Key.F10, ModifierKeys.Windows);
    public static KeyGesture CaptureTextFromWebSocketKeyGesture { get; private set; } = new(Key.F11, ModifierKeys.Windows);
    public static KeyGesture ReconnectToWebSocketServerKeyGesture { get; private set; } = new(Key.F9, ModifierKeys.Windows);
    public static KeyGesture DeleteCurrentLineKeyGesture { get; private set; } = new(Key.Delete, ModifierKeys.Windows);
    public static KeyGesture ShowManageAudioSourcesWindowKeyGesture { get; private set; } = new(Key.Z, ModifierKeys.Windows);
    public static KeyGesture ToggleMinimizedStateKeyGesture { get; private set; } = new(Key.X, ModifierKeys.Alt);
    public static KeyGesture SelectedTextToSpeechKeyGesture { get; private set; } = new(Key.F6, ModifierKeys.Windows);
    public static KeyGesture ToggleAlwaysShowMainTextBoxCaretKeyGesture { get; private set; } = new(Key.G, ModifierKeys.Windows);
    public static KeyGesture MoveCaretLeftKeyGesture { get; private set; } = new(Key.NumPad4, ModifierKeys.Control);
    public static KeyGesture MoveCaretRightKeyGesture { get; private set; } = new(Key.NumPad6, ModifierKeys.Control);
    public static KeyGesture MoveCaretUpKeyGesture { get; private set; } = new(Key.NumPad8, ModifierKeys.Control);
    public static KeyGesture MoveCaretDownKeyGesture { get; private set; } = new(Key.NumPad2, ModifierKeys.Control);
    public static KeyGesture LookupTermAtCaretIndexKeyGesture { get; private set; } = new(Key.NumPad5, ModifierKeys.Control);
    public static KeyGesture SelectNextLookupResultKeyGesture { get; private set; } = new(Key.Down, ModifierKeys.Control);
    public static KeyGesture SelectPreviousLookupResultKeyGesture { get; private set; } = new(Key.Up, ModifierKeys.Control);
    public static KeyGesture MineSelectedLookupResultKeyGesture { get; private set; } = new(Key.Enter, ModifierKeys.Control);

    #endregion

    #region Advanced

    public static int MaxSearchLength { get; private set; } = 37;
    public static int MaxNumResultsNotInMiningMode { get; private set; } = 7;
    public static bool Precaching { get; private set; } = false;
    public static string SearchUrl { get; private set; } = "https://www.google.com/search?q={SearchTerm}&hl=ja";
    public static string BrowserPath { get; private set; } = "";
    public static bool GlobalHotKeys { get; private set; } = false;
    public static bool StopIncreasingTimeStatWhenMinimized { get; private set; } = true;
    public static bool StripPunctuationBeforeCalculatingCharacterCount { get; private set; } = true;

    #endregion

    public static ExeConfigurationFileMap MappedExeConfiguration { get; set; } = new();
    private static readonly ComboBoxItem[] s_japaneseFonts = WindowsUtils.FindJapaneseFonts();
    private static readonly ComboBoxItem[] s_popupJapaneseFonts = WindowsUtils.CloneJapaneseFontComboBoxItems(s_japaneseFonts);
    private static SkinType s_theme = SkinType.Dark;

    public static void ApplyPreferences()
    {
        Configuration config = ConfigurationManager.OpenMappedExeConfiguration(MappedExeConfiguration, ConfigurationUserLevel.None);
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        {
            string? minimumLogLevelStr = settings.Get("MinimumLogLevel");
            if (minimumLogLevelStr is null)
            {
                AddToConfig(config, "MinimumLogLevel", "Error");
            }
            else
            {
                Utils.LoggingLevelSwitch.MinimumLevel = minimumLogLevelStr switch
                {
                    "Fatal" => Serilog.Events.LogEventLevel.Fatal,
                    "Error" => Serilog.Events.LogEventLevel.Error,
                    "Warning" => Serilog.Events.LogEventLevel.Warning,
                    "Information" => Serilog.Events.LogEventLevel.Information,
                    "Debug" => Serilog.Events.LogEventLevel.Debug,
                    "Verbose" => Serilog.Events.LogEventLevel.Verbose,
                    _ => Serilog.Events.LogEventLevel.Error
                };
            }
        }

        {
            string? themeStr = settings.Get("Theme");
            if (themeStr is null)
            {
                themeStr = "Dark";
                AddToConfig(config, "Theme", themeStr);
            }

            SkinType skinType = themeStr is "Dark" ? SkinType.Dark : SkinType.Default;
            if (s_theme != skinType)
            {
                s_theme = skinType;
                WindowsUtils.ChangeTheme(s_theme);
            }
        }

        MainWindow mainWindow = MainWindow.Instance;

        CaptureTextFromClipboard = GetValueFromConfig(config, CaptureTextFromClipboard, nameof(CaptureTextFromClipboard), bool.TryParse);
        if (CaptureTextFromClipboard)
        {
            WinApi.SubscribeToClipboardChanged(mainWindow.WindowHandle);
        }
        else
        {
            WinApi.UnsubscribeFromClipboardChanged(mainWindow.WindowHandle);
        }

        CoreConfig.CaptureTextFromWebSocket = GetValueFromConfig(config, CoreConfig.CaptureTextFromWebSocket, nameof(CoreConfig.CaptureTextFromWebSocket), bool.TryParse);
        if (!CoreConfig.CaptureTextFromWebSocket && !CaptureTextFromClipboard)
        {
            StatsUtils.StatsStopWatch.Stop();
            StatsUtils.StopStatsTimer();
        }
        else
        {
            StatsUtils.StatsStopWatch.Start();
            StatsUtils.StartStatsTimer();
        }

        LookupOnClickMouseButton = GetValueFromConfig(config, LookupOnClickMouseButton, nameof(LookupOnClickMouseButton), Enum.TryParse);
        MiningModeMouseButton = GetValueFromConfig(config, MiningModeMouseButton, nameof(MiningModeMouseButton), Enum.TryParse);
        MineMouseButton = GetValueFromConfig(config, MineMouseButton, nameof(MineMouseButton), Enum.TryParse);
        CopyPrimarySpellingToClipboardMouseButton = GetValueFromConfig(config, CopyPrimarySpellingToClipboardMouseButton, nameof(CopyPrimarySpellingToClipboardMouseButton), Enum.TryParse);

        AutoAdjustFontSizesOnResolutionChange = GetValueFromConfig(config, AutoAdjustFontSizesOnResolutionChange, nameof(AutoAdjustFontSizesOnResolutionChange), bool.TryParse);
        HighlightLongestMatch = GetValueFromConfig(config, HighlightLongestMatch, nameof(HighlightLongestMatch), bool.TryParse);
        AutoPlayAudio = GetValueFromConfig(config, AutoPlayAudio, nameof(AutoPlayAudio), bool.TryParse);
        Precaching = GetValueFromConfig(config, Precaching, nameof(Precaching), bool.TryParse);
        GlobalHotKeys = GetValueFromConfig(config, GlobalHotKeys, nameof(GlobalHotKeys), bool.TryParse);
        StopIncreasingTimeStatWhenMinimized = GetValueFromConfig(config, StopIncreasingTimeStatWhenMinimized, nameof(StopIncreasingTimeStatWhenMinimized), bool.TryParse);
        StripPunctuationBeforeCalculatingCharacterCount = GetValueFromConfig(config, StripPunctuationBeforeCalculatingCharacterCount, nameof(StripPunctuationBeforeCalculatingCharacterCount), bool.TryParse);
        CheckForJLUpdatesOnStartUp = GetValueFromConfig(config, CheckForJLUpdatesOnStartUp, nameof(CheckForJLUpdatesOnStartUp), bool.TryParse);
        AlwaysOnTop = GetValueFromConfig(config, AlwaysOnTop, nameof(AlwaysOnTop), bool.TryParse);
        mainWindow.Topmost = AlwaysOnTop;

        RequireLookupKeyPress = GetValueFromConfig(config, RequireLookupKeyPress, nameof(RequireLookupKeyPress), bool.TryParse);
        DisableHotkeys = GetValueFromConfig(config, DisableHotkeys, nameof(DisableHotkeys), bool.TryParse);

        Focusable = GetValueFromConfig(config, Focusable, nameof(Focusable), bool.TryParse);
        if (Focusable)
        {
            WinApi.AllowActivation(mainWindow.WindowHandle);
        }
        else
        {
            WinApi.PreventActivation(mainWindow.WindowHandle);
        }

        CoreConfig.AnkiIntegration = GetValueFromConfig(config, CoreConfig.AnkiIntegration, nameof(CoreConfig.AnkiIntegration), bool.TryParse);
        CoreConfig.KanjiMode = GetValueFromConfig(config, CoreConfig.KanjiMode, nameof(CoreConfig.KanjiMode), bool.TryParse);
        CoreConfig.ForceSyncAnki = GetValueFromConfig(config, CoreConfig.ForceSyncAnki, nameof(CoreConfig.ForceSyncAnki), bool.TryParse);
        CoreConfig.AllowDuplicateCards = GetValueFromConfig(config, CoreConfig.AllowDuplicateCards, nameof(CoreConfig.AllowDuplicateCards), bool.TryParse);
        PopupFocusOnLookup = GetValueFromConfig(config, PopupFocusOnLookup, nameof(PopupFocusOnLookup), bool.TryParse);
        ShowMiningModeReminder = GetValueFromConfig(config, ShowMiningModeReminder, nameof(ShowMiningModeReminder), bool.TryParse);
        DisableLookupsForNonJapaneseCharsInPopups = GetValueFromConfig(config, DisableLookupsForNonJapaneseCharsInPopups, nameof(DisableLookupsForNonJapaneseCharsInPopups), bool.TryParse);
        FixedPopupPositioning = GetValueFromConfig(config, FixedPopupPositioning, nameof(FixedPopupPositioning), bool.TryParse);
        ChangeMainWindowBackgroundOpacityOnUnhover = GetValueFromConfig(config, ChangeMainWindowBackgroundOpacityOnUnhover, nameof(ChangeMainWindowBackgroundOpacityOnUnhover), bool.TryParse);
        TextOnlyVisibleOnHover = GetValueFromConfig(config, TextOnlyVisibleOnHover, nameof(TextOnlyVisibleOnHover), bool.TryParse);
        TextBoxTrimWhiteSpaceCharacters = GetValueFromConfig(config, TextBoxTrimWhiteSpaceCharacters, nameof(TextBoxTrimWhiteSpaceCharacters), bool.TryParse);
        TextBoxRemoveNewlines = GetValueFromConfig(config, TextBoxRemoveNewlines, nameof(TextBoxRemoveNewlines), bool.TryParse);
        OnlyCaptureTextWithJapaneseChars = GetValueFromConfig(config, OnlyCaptureTextWithJapaneseChars, nameof(OnlyCaptureTextWithJapaneseChars), bool.TryParse);
        DisableLookupsForNonJapaneseCharsInMainWindow = GetValueFromConfig(config, DisableLookupsForNonJapaneseCharsInMainWindow, nameof(DisableLookupsForNonJapaneseCharsInMainWindow), bool.TryParse);
        MainWindowFocusOnHover = GetValueFromConfig(config, MainWindowFocusOnHover, nameof(MainWindowFocusOnHover), bool.TryParse);
        SteppedBacklogWithMouseWheel = GetValueFromConfig(config, SteppedBacklogWithMouseWheel, nameof(SteppedBacklogWithMouseWheel), bool.TryParse);
        MainWindowDynamicHeight = GetValueFromConfig(config, MainWindowDynamicHeight, nameof(MainWindowDynamicHeight), bool.TryParse);
        MainWindowDynamicWidth = GetValueFromConfig(config, MainWindowDynamicWidth, nameof(MainWindowDynamicWidth), bool.TryParse);
        PopupDynamicHeight = GetValueFromConfig(config, PopupDynamicHeight, nameof(PopupDynamicHeight), bool.TryParse);
        PopupDynamicWidth = GetValueFromConfig(config, PopupDynamicWidth, nameof(PopupDynamicWidth), bool.TryParse);
        HideDictTabsWithNoResults = GetValueFromConfig(config, HideDictTabsWithNoResults, nameof(HideDictTabsWithNoResults), bool.TryParse);
        AutoHidePopupIfMouseIsNotOverIt = GetValueFromConfig(config, AutoHidePopupIfMouseIsNotOverIt, nameof(AutoHidePopupIfMouseIsNotOverIt), bool.TryParse);

        TextBoxIsReadOnly = GetValueFromConfig(config, TextBoxIsReadOnly, nameof(TextBoxIsReadOnly), bool.TryParse);
        if (mainWindow.MainTextBox.IsReadOnly != TextBoxIsReadOnly)
        {
            mainWindow.MainTextBox.IsReadOnly = TextBoxIsReadOnly;
            mainWindow.MainTextBox.IsUndoEnabled = !TextBoxIsReadOnly;
            mainWindow.MainTextBox.UndoLimit = TextBoxIsReadOnly ? 0 : -1;
        }

        AlwaysShowMainTextBoxCaret = GetValueFromConfig(config, AlwaysShowMainTextBoxCaret, nameof(AlwaysShowMainTextBoxCaret), bool.TryParse);
        mainWindow.MainTextBox.IsReadOnlyCaretVisible = AlwaysShowMainTextBoxCaret;

        HorizontallyCenterMainWindowText = GetValueFromConfig(config, HorizontallyCenterMainWindowText, nameof(HorizontallyCenterMainWindowText), bool.TryParse);
        mainWindow.MainTextBox.HorizontalContentAlignment = HorizontallyCenterMainWindowText
            ? HorizontalAlignment.Center
            : HorizontalAlignment.Left;

        EnableBacklog = GetValueFromConfig(config, EnableBacklog, nameof(EnableBacklog), bool.TryParse);
        if (!EnableBacklog)
        {
            BacklogUtils.Backlog.Clear();
            BacklogUtils.Backlog.TrimExcess();
        }

        AutoSaveBacklogBeforeClosing = GetValueFromConfig(config, AutoSaveBacklogBeforeClosing, nameof(AutoSaveBacklogBeforeClosing), bool.TryParse);

        TextToSpeechOnTextChange = GetValueFromConfig(config, TextToSpeechOnTextChange, nameof(TextToSpeechOnTextChange), bool.TryParse);

        HidePopupsOnTextChange = GetValueFromConfig(config, HidePopupsOnTextChange, nameof(HidePopupsOnTextChange), bool.TryParse);

        HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar = GetValueFromConfig(config, HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar, nameof(HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar), bool.TryParse);
        mainWindow.ChangeVisibilityOfTitleBarButtons();

        TextBoxApplyDropShadowEffect = GetValueFromConfig(config, TextBoxApplyDropShadowEffect, nameof(TextBoxApplyDropShadowEffect), bool.TryParse);
        if (TextBoxApplyDropShadowEffect)
        {
            DropShadowEffect dropShadowEffect = new() { Direction = 320, BlurRadius = 4, ShadowDepth = 1.3, Opacity = 0.8, RenderingBias = RenderingBias.Quality };
            dropShadowEffect.Freeze();
            mainWindow.MainTextBox.Effect = dropShadowEffect;
        }

        else
        {
            mainWindow.MainTextBox.Effect = null;
        }

        MaxSearchLength = GetValueFromConfig(config, MaxSearchLength, nameof(MaxSearchLength), int.TryParse);
        CoreConfig.LookupRate = GetValueFromConfig(config, CoreConfig.LookupRate, nameof(CoreConfig.LookupRate), int.TryParse);
        PrimarySpellingFontSize = GetValueFromConfig(config, PrimarySpellingFontSize, nameof(PrimarySpellingFontSize), int.TryParse);
        ReadingsFontSize = GetValueFromConfig(config, ReadingsFontSize, nameof(ReadingsFontSize), int.TryParse);
        AlternativeSpellingsFontSize = GetValueFromConfig(config, AlternativeSpellingsFontSize, nameof(AlternativeSpellingsFontSize), int.TryParse);
        DefinitionsFontSize = GetValueFromConfig(config, DefinitionsFontSize, nameof(DefinitionsFontSize), int.TryParse);
        FrequencyFontSize = GetValueFromConfig(config, FrequencyFontSize, nameof(FrequencyFontSize), int.TryParse);
        DeconjugationInfoFontSize = GetValueFromConfig(config, DeconjugationInfoFontSize, nameof(DeconjugationInfoFontSize), int.TryParse);
        DictTypeFontSize = GetValueFromConfig(config, DictTypeFontSize, nameof(DictTypeFontSize), int.TryParse);
        MaxNumResultsNotInMiningMode = GetValueFromConfig(config, MaxNumResultsNotInMiningMode, nameof(MaxNumResultsNotInMiningMode), int.TryParse);
        CoreConfig.AudioVolume = GetValueFromConfig(config, CoreConfig.AudioVolume, nameof(CoreConfig.AudioVolume), int.TryParse);

        AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds = GetValueFromConfig(config, AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds, nameof(AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds), int.TryParse);
        PopupWindow.PopupAutoHideTimer.Enabled = false;
        PopupWindow.PopupAutoHideTimer.Interval = AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;

        PopupXOffset = GetValueFromConfig(config, PopupXOffset, nameof(PopupXOffset), int.TryParse);
        WindowsUtils.DpiAwareXOffset = PopupXOffset / WindowsUtils.Dpi.DpiScaleX;

        PopupYOffset = GetValueFromConfig(config, PopupYOffset, nameof(PopupYOffset), int.TryParse);
        WindowsUtils.DpiAwareYOffset = PopupYOffset / WindowsUtils.Dpi.DpiScaleY;

        PopupMaxWidth = GetValueFromConfig(config, PopupMaxWidth, nameof(PopupMaxWidth), int.TryParse);
        WindowsUtils.DpiAwarePopupMaxWidth = PopupMaxWidth / WindowsUtils.Dpi.DpiScaleX;

        PopupMaxHeight = GetValueFromConfig(config, PopupMaxHeight, nameof(PopupMaxHeight), int.TryParse);
        WindowsUtils.DpiAwarePopupMaxHeight = PopupMaxHeight / WindowsUtils.Dpi.DpiScaleY;

        FixedPopupXPosition = GetValueFromConfig(config, FixedPopupXPosition, nameof(FixedPopupXPosition), int.TryParse);
        WindowsUtils.DpiAwareFixedPopupXPosition = FixedPopupXPosition / WindowsUtils.Dpi.DpiScaleX;

        FixedPopupYPosition = GetValueFromConfig(config, FixedPopupYPosition, nameof(FixedPopupYPosition), int.TryParse);
        WindowsUtils.DpiAwareFixedPopupYPosition = FixedPopupYPosition / WindowsUtils.Dpi.DpiScaleY;

        mainWindow.OpacitySlider.Value = GetNumberWithDecimalPointFromConfig(config, mainWindow.OpacitySlider.Value, "MainWindowOpacity", double.TryParse);
        mainWindow.FontSizeSlider.Value = GetNumberWithDecimalPointFromConfig(config, mainWindow.FontSizeSlider.Value, "MainWindowFontSize", double.TryParse);
        MainWindowBackgroundOpacityOnUnhover = GetNumberWithDecimalPointFromConfig(config, MainWindowBackgroundOpacityOnUnhover, nameof(MainWindowBackgroundOpacityOnUnhover), double.TryParse);

        MainWindowHeight = GetNumberWithDecimalPointFromConfig(config, MainWindowHeight, nameof(MainWindowHeight), double.TryParse);
        MainWindowWidth = GetNumberWithDecimalPointFromConfig(config, MainWindowWidth, nameof(MainWindowWidth), double.TryParse);
        MainWindowMaxDynamicWidth = GetNumberWithDecimalPointFromConfig(config, MainWindowMaxDynamicWidth, nameof(MainWindowMaxDynamicWidth), double.TryParse);
        MainWindowMaxDynamicHeight = GetNumberWithDecimalPointFromConfig(config, MainWindowMaxDynamicHeight, nameof(MainWindowMaxDynamicHeight), double.TryParse);
        WindowsUtils.SetSizeToContentForMainWindow(MainWindowDynamicWidth, MainWindowDynamicHeight, MainWindowMaxDynamicWidth, MainWindowMaxDynamicHeight, MainWindowWidth, MainWindowHeight, mainWindow);
        mainWindow.WidthBeforeResolutionChange = MainWindowWidth;
        mainWindow.HeightBeforeResolutionChange = MainWindowHeight;

        mainWindow.Top = GetNumberWithDecimalPointFromConfig(config, mainWindow.Top, "MainWindowTopPosition", double.TryParse);
        mainWindow.Left = GetNumberWithDecimalPointFromConfig(config, mainWindow.Left, "MainWindowLeftPosition", double.TryParse);

        mainWindow.TopPositionBeforeResolutionChange = mainWindow.Top;
        mainWindow.LeftPositionBeforeResolutionChange = mainWindow.Left;

        mainWindow.MainGrid.Opacity = TextOnlyVisibleOnHover && !mainWindow.IsMouseOver && !PreferencesWindow.IsItVisible() ? 0 : 1;

        // MAKE SURE YOU FREEZE ANY NEW COLOR OBJECTS YOU ADD
        // OR THE PROGRAM WILL CRASH AND BURN
        MainWindowTextColor = GetFrozenBrushFromConfig(config, MainWindowTextColor, nameof(MainWindowTextColor));
        MainWindowBacklogTextColor = GetFrozenBrushFromConfig(config, MainWindowBacklogTextColor, nameof(MainWindowBacklogTextColor));

        mainWindow.MainTextBox.Foreground = !EnableBacklog || mainWindow.MainTextBox.Text == BacklogUtils.Backlog.LastOrDefault("")
            ? MainWindowTextColor
            : MainWindowBacklogTextColor;

        mainWindow.MainTextBox.CaretBrush = MainWindowTextColor;

        PrimarySpellingColor = GetFrozenBrushFromConfig(config, PrimarySpellingColor, nameof(PrimarySpellingColor));
        ReadingsColor = GetFrozenBrushFromConfig(config, ReadingsColor, nameof(ReadingsColor));
        AlternativeSpellingsColor = GetFrozenBrushFromConfig(config, AlternativeSpellingsColor, nameof(AlternativeSpellingsColor));
        DefinitionsColor = GetFrozenBrushFromConfig(config, DefinitionsColor, nameof(DefinitionsColor));
        FrequencyColor = GetFrozenBrushFromConfig(config, FrequencyColor, nameof(FrequencyColor));
        DeconjugationInfoColor = GetFrozenBrushFromConfig(config, DeconjugationInfoColor, nameof(DeconjugationInfoColor));

        SeparatorColor = GetFrozenBrushFromConfig(config, SeparatorColor, nameof(SeparatorColor));

        DictTypeColor = GetFrozenBrushFromConfig(config, DictTypeColor, nameof(DictTypeColor));

        HighlightColor = GetFrozenBrushFromConfig(config, HighlightColor, nameof(HighlightColor));
        mainWindow.MainTextBox.SelectionBrush = HighlightColor;

        PopupBackgroundColor = GetBrushFromConfig(config, PopupBackgroundColor, nameof(PopupBackgroundColor));
        PopupBackgroundColor.Opacity = GetNumberWithDecimalPointFromConfig(config, 70.0, "PopupOpacity", double.TryParse) / 100;
        PopupBackgroundColor.Freeze();

        mainWindow.Background = GetBrushFromConfig(config, mainWindow.Background, "MainWindowBackgroundColor");

        mainWindow.Background.Opacity = ChangeMainWindowBackgroundOpacityOnUnhover && !mainWindow.IsMouseOver && !PreferencesWindow.IsItVisible()
            ? MainWindowBackgroundOpacityOnUnhover / 100
            : mainWindow.OpacitySlider.Value / 100;

        WinApi.UnregisterAllHotKeys(mainWindow.WindowHandle);
        KeyGestureUtils.KeyGestureDict.Clear();
        KeyGestureUtils.KeyGestureNameToIntDict.Clear();

        DisableHotkeysKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(DisableHotkeysKeyGesture), DisableHotkeysKeyGesture);
        MiningModeKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MiningModeKeyGesture), MiningModeKeyGesture);
        PlayAudioKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(PlayAudioKeyGesture), PlayAudioKeyGesture);
        KanjiModeKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(KanjiModeKeyGesture), KanjiModeKeyGesture);
        LookupKeyKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(LookupKeyKeyGesture), LookupKeyKeyGesture, false);
        ClosePopupKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(ClosePopupKeyGesture), ClosePopupKeyGesture);
        ShowStatsKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(ShowStatsKeyGesture), ShowStatsKeyGesture);
        NextDictKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(NextDictKeyGesture), NextDictKeyGesture);
        PreviousDictKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(PreviousDictKeyGesture), PreviousDictKeyGesture);
        AlwaysOnTopKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(AlwaysOnTopKeyGesture), AlwaysOnTopKeyGesture);
        TextBoxIsReadOnlyKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(TextBoxIsReadOnlyKeyGesture), TextBoxIsReadOnlyKeyGesture);
        ToggleAlwaysShowMainTextBoxCaretKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(ToggleAlwaysShowMainTextBoxCaretKeyGesture), ToggleAlwaysShowMainTextBoxCaretKeyGesture);
        MoveCaretLeftKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MoveCaretLeftKeyGesture), MoveCaretLeftKeyGesture);
        MoveCaretRightKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MoveCaretRightKeyGesture), MoveCaretRightKeyGesture);
        MoveCaretUpKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MoveCaretUpKeyGesture), MoveCaretUpKeyGesture);
        MoveCaretDownKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MoveCaretDownKeyGesture), MoveCaretDownKeyGesture);
        LookupTermAtCaretIndexKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(LookupTermAtCaretIndexKeyGesture), LookupTermAtCaretIndexKeyGesture);
        SelectNextLookupResultKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(SelectNextLookupResultKeyGesture), SelectNextLookupResultKeyGesture);
        SelectPreviousLookupResultKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(SelectPreviousLookupResultKeyGesture), SelectPreviousLookupResultKeyGesture);
        MineSelectedLookupResultKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(MineSelectedLookupResultKeyGesture), MineSelectedLookupResultKeyGesture);
        CaptureTextFromClipboardKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(CaptureTextFromClipboardKeyGesture), CaptureTextFromClipboardKeyGesture);
        CaptureTextFromWebSocketKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(CaptureTextFromWebSocketKeyGesture), CaptureTextFromWebSocketKeyGesture);
        ReconnectToWebSocketServerKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(ReconnectToWebSocketServerKeyGesture), ReconnectToWebSocketServerKeyGesture);
        DeleteCurrentLineKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(DeleteCurrentLineKeyGesture), DeleteCurrentLineKeyGesture);

        ShowPreferencesWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowPreferencesWindowKeyGesture), ShowPreferencesWindowKeyGesture);
        ShowAddNameWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowAddNameWindowKeyGesture), ShowAddNameWindowKeyGesture);
        ShowAddWordWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowAddWordWindowKeyGesture), ShowAddWordWindowKeyGesture);
        SearchWithBrowserKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(SearchWithBrowserKeyGesture), SearchWithBrowserKeyGesture);
        MousePassThroughModeKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(MousePassThroughModeKeyGesture), MousePassThroughModeKeyGesture);
        SteppedBacklogBackwardsKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(SteppedBacklogBackwardsKeyGesture), SteppedBacklogBackwardsKeyGesture);
        SteppedBacklogForwardsKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(SteppedBacklogForwardsKeyGesture), SteppedBacklogForwardsKeyGesture);
        InactiveLookupModeKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(InactiveLookupModeKeyGesture), InactiveLookupModeKeyGesture);
        MotivationKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(MotivationKeyGesture), MotivationKeyGesture);

        ShowManageDictionariesWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowManageDictionariesWindowKeyGesture),
                ShowManageDictionariesWindowKeyGesture);

        ShowManageFrequenciesWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowManageFrequenciesWindowKeyGesture),
                ShowManageFrequenciesWindowKeyGesture);

        ShowManageAudioSourcesWindowKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ShowManageAudioSourcesWindowKeyGesture),
                ShowManageAudioSourcesWindowKeyGesture);

        ToggleMinimizedStateKeyGesture =
            KeyGestureUtils.SetKeyGesture(config, nameof(ToggleMinimizedStateKeyGesture),
                ToggleMinimizedStateKeyGesture);

        SelectedTextToSpeechKeyGesture = KeyGestureUtils.SetKeyGesture(config, nameof(SelectedTextToSpeechKeyGesture), SelectedTextToSpeechKeyGesture);

        if (GlobalHotKeys && !DisableHotkeys)
        {
            WinApi.RegisterAllHotKeys(mainWindow.WindowHandle);
        }

        KeyGestureUtils.SetInputGestureText(mainWindow.AddNameMenuItem, ShowAddNameWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.AddWordMenuItem, ShowAddWordWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.SearchMenuItem, SearchWithBrowserKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.PreferencesMenuItem, ShowPreferencesWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.ManageDictionariesMenuItem, ShowManageDictionariesWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.ManageFrequenciesMenuItem, ShowManageFrequenciesWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.ManageAudioSourcesMenuItem, ShowManageAudioSourcesWindowKeyGesture);
        KeyGestureUtils.SetInputGestureText(mainWindow.StatsMenuItem, ShowStatsKeyGesture);

        {
            string? ankiConnectUriStr = settings.Get(nameof(CoreConfig.AnkiConnectUri));
            if (ankiConnectUriStr is null)
            {
                AddToConfig(config, nameof(CoreConfig.AnkiConnectUri), CoreConfig.AnkiConnectUri.OriginalString);
            }

            else
            {
                ankiConnectUriStr = ankiConnectUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.Ordinal);

                if (Uri.TryCreate(ankiConnectUriStr, UriKind.Absolute, out Uri? ankiConnectUri))
                {
                    CoreConfig.AnkiConnectUri = ankiConnectUri;
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save AnkiConnect server address, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save AnkiConnect server address, invalid URL");
                }
            }
        }

        {
            string? webSocketUriStr = settings.Get(nameof(CoreConfig.WebSocketUri));
            if (webSocketUriStr is null)
            {
                AddToConfig(config, nameof(CoreConfig.WebSocketUri), CoreConfig.WebSocketUri.OriginalString);
            }
            else
            {
                webSocketUriStr = webSocketUriStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost:", "://127.0.0.1:", StringComparison.Ordinal);

                if (Uri.TryCreate(webSocketUriStr, UriKind.Absolute, out Uri? webSocketUri))
                {
                    CoreConfig.WebSocketUri = webSocketUri;
                }
                else
                {
                    Utils.Logger.Warning("Couldn't save WebSocket address, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save WebSocket address, invalid URL");
                }
            }

            WebSocketUtils.HandleWebSocket();
        }

        {
            string? searchUrlStr = settings.Get(nameof(SearchUrl));
            if (searchUrlStr is null)
            {
                AddToConfig(config, nameof(SearchUrl), SearchUrl);
            }
            else
            {
                searchUrlStr = searchUrlStr
                    .Replace("://0.0.0.0:", "://127.0.0.1:", StringComparison.Ordinal)
                    .Replace("://localhost", "://127.0.0.1", StringComparison.Ordinal);

                if (!Uri.IsWellFormedUriString(searchUrlStr.Replace("{SearchTerm}", "", StringComparison.Ordinal), UriKind.Absolute))
                {
                    Utils.Logger.Warning("Couldn't save Search URL, invalid URL");
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save Search URL, invalid URL");
                }
                else
                {
                    SearchUrl = searchUrlStr;
                }
            }
        }

        {
            string? browserPathStr = settings.Get(nameof(BrowserPath));
            if (browserPathStr is null)
            {
                AddToConfig(config, nameof(BrowserPath), BrowserPath);
            }
            else if (!string.IsNullOrWhiteSpace(browserPathStr) && !Path.IsPathFullyQualified(browserPathStr))
            {
                Utils.Logger.Warning("Couldn't save Browser Path, invalid path");
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't save Browser Path, invalid path");
            }
            else
            {
                BrowserPath = browserPathStr;
            }
        }

        {
            string? mainWindowFontStr = settings.Get("MainWindowFont");
            if (mainWindowFontStr is null)
            {
                AddToConfig(config, "MainWindowFont", "Meiryo");
                mainWindowFontStr = "Meiryo";
            }

            mainWindow.MainTextBox.FontFamily = new FontFamily(mainWindowFontStr);
        }

        {
            string? popupFlipStr = settings.Get("PopupFlip");
            if (popupFlipStr is null)
            {
                popupFlipStr = "Both";
                AddToConfig(config, "PopupFlip", popupFlipStr);
            }

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
            string? lookupModeStr = settings.Get("LookupMode");
            if (lookupModeStr is null)
            {
                lookupModeStr = "Hover";
                AddToConfig(config, "LookupMode", lookupModeStr);
            }

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
            string? popupFontStr = settings.Get(nameof(PopupFont));
            if (popupFontStr is null)
            {
                AddToConfig(config, nameof(PopupFont), PopupFont.Source);
            }
            else
            {
                PopupFont = new FontFamily(popupFontStr);
                WindowsUtils.PopupFontTypeFace = new Typeface(popupFontStr);
            }
        }

        PopupWindow? currentPopupWindow = MainWindow.Instance.FirstPopupWindow;
        while (currentPopupWindow is not null)
        {
            currentPopupWindow.Background = PopupBackgroundColor;
            currentPopupWindow.Foreground = DefinitionsColor;
            currentPopupWindow.FontFamily = PopupFont;

            WindowsUtils.SetSizeToContentForPopup(PopupDynamicWidth, PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxWidth, WindowsUtils.DpiAwarePopupMaxHeight, currentPopupWindow);

            KeyGestureUtils.SetInputGestureText(currentPopupWindow.AddNameMenuItem, ShowAddNameWindowKeyGesture);
            KeyGestureUtils.SetInputGestureText(currentPopupWindow.AddWordMenuItem, ShowAddWordWindowKeyGesture);
            KeyGestureUtils.SetInputGestureText(currentPopupWindow.SearchMenuItem, SearchWithBrowserKeyGesture);

            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    public static void LoadPreferences(PreferencesWindow preferenceWindow)
    {
        CreateDefaultAppConfig();

        Configuration config = ConfigurationManager.OpenMappedExeConfiguration(MappedExeConfiguration, ConfigurationUserLevel.None);
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        MainWindow mainWindow = MainWindow.Instance;

        preferenceWindow.ProfileComboBox.ItemsSource = ProfileUtils.Profiles;
        preferenceWindow.ProfileComboBox.SelectedItem = ProfileUtils.CurrentProfile;

        preferenceWindow.JLVersionTextBlock.Text = string.Create(CultureInfo.InvariantCulture, $"v{Utils.JLVersion}");

        preferenceWindow.DisableHotkeysKeyGestureTextBox.Text = KeyGestureUtils.KeyGestureToString(DisableHotkeysKeyGesture);
        preferenceWindow.MiningModeKeyGestureTextBox.Text = KeyGestureUtils.KeyGestureToString(MiningModeKeyGesture);
        preferenceWindow.PlayAudioKeyGestureTextBox.Text = KeyGestureUtils.KeyGestureToString(PlayAudioKeyGesture);
        preferenceWindow.KanjiModeKeyGestureTextBox.Text = KeyGestureUtils.KeyGestureToString(KanjiModeKeyGesture);
        preferenceWindow.LookupKeyKeyGestureTextBox.Text = KeyGestureUtils.KeyGestureToString(LookupKeyKeyGesture);

        preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowManageDictionariesWindowKeyGesture);
        preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowManageFrequenciesWindowKeyGesture);
        preferenceWindow.ShowManageAudioSourcesWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowManageAudioSourcesWindowKeyGesture);
        preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowPreferencesWindowKeyGesture);
        preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowAddNameWindowKeyGesture);
        preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowAddWordWindowKeyGesture);
        preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SearchWithBrowserKeyGesture);
        preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MousePassThroughModeKeyGesture);
        preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SteppedBacklogBackwardsKeyGesture);
        preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SteppedBacklogForwardsKeyGesture);
        preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(InactiveLookupModeKeyGesture);
        preferenceWindow.MotivationKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MotivationKeyGesture);
        preferenceWindow.ClosePopupKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ClosePopupKeyGesture);
        preferenceWindow.ShowStatsKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ShowStatsKeyGesture);
        preferenceWindow.NextDictKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(NextDictKeyGesture);
        preferenceWindow.PreviousDictKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(PreviousDictKeyGesture);
        preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(AlwaysOnTopKeyGesture);
        preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(TextBoxIsReadOnlyKeyGesture);
        preferenceWindow.ToggleAlwaysShowMainTextBoxCaretKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ToggleAlwaysShowMainTextBoxCaretKeyGesture);
        preferenceWindow.MoveCaretLeftKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MoveCaretLeftKeyGesture);
        preferenceWindow.MoveCaretRightKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MoveCaretRightKeyGesture);
        preferenceWindow.MoveCaretUpKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MoveCaretUpKeyGesture);
        preferenceWindow.MoveCaretDownKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MoveCaretDownKeyGesture);
        preferenceWindow.LookupTermAtCaretIndexKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(LookupTermAtCaretIndexKeyGesture);
        preferenceWindow.SelectNextLookupResultKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SelectNextLookupResultKeyGesture);
        preferenceWindow.SelectPreviousLookupResultKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SelectPreviousLookupResultKeyGesture);
        preferenceWindow.MineSelectedLookupResultKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(MineSelectedLookupResultKeyGesture);
        preferenceWindow.CaptureTextFromClipboardKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(CaptureTextFromClipboardKeyGesture);
        preferenceWindow.CaptureTextFromWebSocketKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(CaptureTextFromWebSocketKeyGesture);
        preferenceWindow.ReconnectToWebSocketServerKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ReconnectToWebSocketServerKeyGesture);
        preferenceWindow.DeleteCurrentLineKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(DeleteCurrentLineKeyGesture);
        preferenceWindow.ToggleMinimizedStateKeyGestureTextBox.Text =
            KeyGestureUtils.KeyGestureToString(ToggleMinimizedStateKeyGesture);
        preferenceWindow.SelectedTextToSpeechTextBox.Text =
            KeyGestureUtils.KeyGestureToString(SelectedTextToSpeechKeyGesture);

        WindowsUtils.SetButtonColor(preferenceWindow.HighlightColorButton, HighlightColor);
        WindowsUtils.SetButtonColor(preferenceWindow.MainWindowBackgroundColorButton, mainWindow.Background.CloneCurrentValue());
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
        preferenceWindow.AnkiUriTextBox.Text = CoreConfig.AnkiConnectUri.OriginalString;
        preferenceWindow.WebSocketUriTextBox.Text = CoreConfig.WebSocketUri.OriginalString;
        preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = CoreConfig.ForceSyncAnki;
        preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked = CoreConfig.AllowDuplicateCards;
        preferenceWindow.LookupRateNumericUpDown.Value = CoreConfig.LookupRate;
        preferenceWindow.KanjiModeCheckBox.IsChecked = CoreConfig.KanjiMode;
        preferenceWindow.AutoAdjustFontSizesOnResolutionChange.IsChecked = AutoAdjustFontSizesOnResolutionChange;
        preferenceWindow.HighlightLongestMatchCheckBox.IsChecked = HighlightLongestMatch;
        preferenceWindow.AutoPlayAudioCheckBox.IsChecked = AutoPlayAudio;
        preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked = CheckForJLUpdatesOnStartUp;
        preferenceWindow.PrecachingCheckBox.IsChecked = Precaching;
        preferenceWindow.GlobalHotKeysCheckBox.IsChecked = GlobalHotKeys;
        preferenceWindow.StopIncreasingTimeStatWhenMinimizedCheckBox.IsChecked = StopIncreasingTimeStatWhenMinimized;
        preferenceWindow.StripPunctuationBeforeCalculatingCharacterCountCheckBox.IsChecked = StripPunctuationBeforeCalculatingCharacterCount;
        preferenceWindow.AlwaysOnTopCheckBox.IsChecked = AlwaysOnTop;
        preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked = RequireLookupKeyPress;
        preferenceWindow.DisableHotkeysCheckBox.IsChecked = DisableHotkeys;
        preferenceWindow.FocusableCheckBox.IsChecked = Focusable;
        preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked = TextOnlyVisibleOnHover;
        preferenceWindow.AnkiIntegrationCheckBox.IsChecked = CoreConfig.AnkiIntegration;
        preferenceWindow.LookupRateNumericUpDown.Value = CoreConfig.LookupRate;

        preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked = MainWindowDynamicWidth;
        preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked = MainWindowDynamicHeight;

        preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value = MainWindowMaxDynamicWidth;
        preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value = MainWindowMaxDynamicHeight;

        preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
        preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;
        preferenceWindow.TextBoxFontSizeNumericUpDown.Value = mainWindow.FontSizeSlider.Value;
        preferenceWindow.MainWindowOpacityNumericUpDown.Value = mainWindow.OpacitySlider.Value;

        preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked = ChangeMainWindowBackgroundOpacityOnUnhover;
        preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value = MainWindowBackgroundOpacityOnUnhover;

        preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked = TextBoxIsReadOnly;
        preferenceWindow.AlwaysShowMainTextBoxCaretCheckBox.IsChecked = AlwaysShowMainTextBoxCaret;
        preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked = TextBoxTrimWhiteSpaceCharacters;
        preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked = TextBoxRemoveNewlines;
        preferenceWindow.TextBoxApplyDropShadowEffectCheckBox.IsChecked = TextBoxApplyDropShadowEffect;
        preferenceWindow.CaptureTextFromClipboardCheckBox.IsChecked = CaptureTextFromClipboard;
        preferenceWindow.CaptureTextFromWebSocketCheckBox.IsChecked = CoreConfig.CaptureTextFromWebSocket;
        preferenceWindow.OnlyCaptureTextWithJapaneseCharsCheckBox.IsChecked = OnlyCaptureTextWithJapaneseChars;
        preferenceWindow.DisableLookupsForNonJapaneseCharsInMainWindowCheckBox.IsChecked = DisableLookupsForNonJapaneseCharsInMainWindow;
        preferenceWindow.MainWindowFocusOnHoverCheckBox.IsChecked = MainWindowFocusOnHover;
        preferenceWindow.SteppedBacklogWithMouseWheelCheckBox.IsChecked = SteppedBacklogWithMouseWheel;
        preferenceWindow.EnableBacklogCheckBox.IsChecked = EnableBacklog;
        preferenceWindow.AutoSaveBacklogBeforeClosingCheckBox.IsChecked = AutoSaveBacklogBeforeClosing;
        preferenceWindow.TextToSpeechOnTextChangeCheckBox.IsChecked = TextToSpeechOnTextChange;
        preferenceWindow.HidePopupsOnTextChangeCheckBox.IsChecked = HidePopupsOnTextChange;
        preferenceWindow.ToggleHideAllTitleBarButtonsWhenMouseIsNotOverTitleBarCheckBox.IsChecked = HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar;
        preferenceWindow.HorizontallyCenterMainWindowTextCheckBox.IsChecked = HorizontallyCenterMainWindowText;

        preferenceWindow.ThemeComboBox.SelectedValue = settings.Get("Theme");
        preferenceWindow.MinimumLogLevelComboBox.SelectedValue = settings.Get("MinimumLogLevel");

        preferenceWindow.MainWindowFontComboBox.ItemsSource = s_japaneseFonts;
        preferenceWindow.MainWindowFontComboBox.SelectedIndex = Array.FindIndex(s_japaneseFonts, f =>
            f.Content.ToString() == mainWindow.MainTextBox.FontFamily.Source);

        if (preferenceWindow.MainWindowFontComboBox.SelectedIndex is -1)
        {
            preferenceWindow.MainWindowFontComboBox.SelectedIndex = 0;
        }

        preferenceWindow.PopupFontComboBox.ItemsSource = s_popupJapaneseFonts;
        preferenceWindow.PopupFontComboBox.SelectedIndex =
            Array.FindIndex(s_popupJapaneseFonts, static f => f.Content.ToString() == PopupFont.Source);

        if (preferenceWindow.PopupFontComboBox.SelectedIndex is -1)
        {
            preferenceWindow.PopupFontComboBox.SelectedIndex = 0;
        }

        preferenceWindow.PopupMaxHeightNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Height;
        preferenceWindow.PopupMaxWidthNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Width;

        preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value = MaxNumResultsNotInMiningMode;
        preferenceWindow.AudioVolumeNumericUpDown.Value = CoreConfig.AudioVolume;

        preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
        preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
        preferenceWindow.FixedPopupPositioningCheckBox.IsChecked = FixedPopupPositioning;
        preferenceWindow.FixedPopupXPositionNumericUpDown.Value = FixedPopupXPosition;
        preferenceWindow.FixedPopupYPositionNumericUpDown.Value = FixedPopupYPosition;
        preferenceWindow.PopupDynamicHeightCheckBox.IsChecked = PopupDynamicHeight;
        preferenceWindow.PopupDynamicWidthCheckBox.IsChecked = PopupDynamicWidth;

        preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
        preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value = DeconjugationInfoFontSize;
        preferenceWindow.DictTypeFontSizeNumericUpDown.Value = DictTypeFontSize;
        preferenceWindow.AutoHidePopupIfMouseIsNotOverItDelayInMillisecondsNumericUpDown.Value = AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds;
        preferenceWindow.DefinitionsFontSizeNumericUpDown.Value = DefinitionsFontSize;
        preferenceWindow.FrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
        preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value = PrimarySpellingFontSize;
        preferenceWindow.ReadingsFontSizeNumericUpDown.Value = ReadingsFontSize;
        preferenceWindow.PopupOpacityNumericUpDown.Value = PopupBackgroundColor.Opacity * 100;
        preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked = PopupFocusOnLookup;
        preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
        preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;
        preferenceWindow.PopupFlipComboBox.SelectedValue = settings.Get("PopupFlip");
        preferenceWindow.LookupModeComboBox.SelectedValue = settings.Get("LookupMode");

        if (preferenceWindow.LookupModeComboBox.SelectedIndex is -1)
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
    }

    public static async Task SavePreferences(PreferencesWindow preferenceWindow)
    {
        Configuration config = ConfigurationManager.OpenMappedExeConfiguration(MappedExeConfiguration, ConfigurationUserLevel.None);
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        SaveKeyGesture(config, nameof(DisableHotkeysKeyGesture), preferenceWindow.DisableHotkeysKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MiningModeKeyGesture), preferenceWindow.MiningModeKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(PlayAudioKeyGesture), preferenceWindow.PlayAudioKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(KanjiModeKeyGesture), preferenceWindow.KanjiModeKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(LookupKeyKeyGesture), preferenceWindow.LookupKeyKeyGestureTextBox.Text);

        SaveKeyGesture(config, nameof(ShowManageDictionariesWindowKeyGesture),
            preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowManageFrequenciesWindowKeyGesture),
            preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowManageAudioSourcesWindowKeyGesture),
            preferenceWindow.ShowManageAudioSourcesWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowPreferencesWindowKeyGesture),
            preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowAddNameWindowKeyGesture),
            preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowAddWordWindowKeyGesture),
            preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SearchWithBrowserKeyGesture),
            preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MousePassThroughModeKeyGesture),
            preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SteppedBacklogBackwardsKeyGesture),
            preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SteppedBacklogForwardsKeyGesture),
            preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(InactiveLookupModeKeyGesture),
            preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MotivationKeyGesture),
            preferenceWindow.MotivationKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ClosePopupKeyGesture),
            preferenceWindow.ClosePopupKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ShowStatsKeyGesture),
            preferenceWindow.ShowStatsKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(NextDictKeyGesture),
            preferenceWindow.NextDictKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(PreviousDictKeyGesture),
            preferenceWindow.PreviousDictKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(AlwaysOnTopKeyGesture),
            preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(TextBoxIsReadOnlyKeyGesture),
            preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ToggleAlwaysShowMainTextBoxCaretKeyGesture),
            preferenceWindow.ToggleAlwaysShowMainTextBoxCaretKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MoveCaretLeftKeyGesture),
            preferenceWindow.MoveCaretLeftKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MoveCaretRightKeyGesture),
            preferenceWindow.MoveCaretRightKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MoveCaretUpKeyGesture),
            preferenceWindow.MoveCaretUpKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MoveCaretDownKeyGesture),
            preferenceWindow.MoveCaretDownKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(LookupTermAtCaretIndexKeyGesture),
            preferenceWindow.LookupTermAtCaretIndexKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SelectNextLookupResultKeyGesture),
            preferenceWindow.SelectNextLookupResultKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SelectPreviousLookupResultKeyGesture),
            preferenceWindow.SelectPreviousLookupResultKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(MineSelectedLookupResultKeyGesture),
            preferenceWindow.MineSelectedLookupResultKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(CaptureTextFromClipboardKeyGesture),
            preferenceWindow.CaptureTextFromClipboardKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(CaptureTextFromWebSocketKeyGesture),
            preferenceWindow.CaptureTextFromWebSocketKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ReconnectToWebSocketServerKeyGesture),
            preferenceWindow.ReconnectToWebSocketServerKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(DeleteCurrentLineKeyGesture),
            preferenceWindow.DeleteCurrentLineKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(ToggleMinimizedStateKeyGesture),
            preferenceWindow.ToggleMinimizedStateKeyGestureTextBox.Text);
        SaveKeyGesture(config, nameof(SelectedTextToSpeechKeyGesture),
            preferenceWindow.SelectedTextToSpeechTextBox.Text);

        settings[nameof(SearchUrl)].Value = preferenceWindow.SearchUrlTextBox.Text;

        settings[nameof(BrowserPath)].Value = preferenceWindow.BrowserPathTextBox.Text;

        settings[nameof(MaxSearchLength)].Value =
            preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(CoreConfig.AnkiConnectUri)].Value =
            preferenceWindow.AnkiUriTextBox.Text;

        settings[nameof(CoreConfig.WebSocketUri)].Value =
            preferenceWindow.WebSocketUriTextBox.Text;

        settings[nameof(MainWindowDynamicWidth)].Value =
            preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked.ToString();
        settings[nameof(MainWindowDynamicHeight)].Value =
            preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked.ToString();

        settings[nameof(MainWindowMaxDynamicWidth)].Value =
            preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(MainWindowMaxDynamicHeight)].Value =
            preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        settings[nameof(MainWindowWidth)].Value =
            preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(MainWindowHeight)].Value =
            preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        // We want the opaque color here
        settings["MainWindowBackgroundColor"].Value =
            preferenceWindow.MainWindowBackgroundColorButton.Background.ToString(CultureInfo.InvariantCulture);

        settings[nameof(ChangeMainWindowBackgroundOpacityOnUnhover)].Value =
            preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked.ToString();
        settings[nameof(MainWindowBackgroundOpacityOnUnhover)].Value =
            preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(TextBoxIsReadOnly)].Value =
            preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked.ToString();
        settings[nameof(AlwaysShowMainTextBoxCaret)].Value =
            preferenceWindow.AlwaysShowMainTextBoxCaretCheckBox.IsChecked.ToString();
        settings[nameof(TextBoxTrimWhiteSpaceCharacters)].Value =
            preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked.ToString();
        settings[nameof(TextBoxRemoveNewlines)].Value =
            preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked.ToString();
        settings[nameof(TextBoxApplyDropShadowEffect)].Value =
            preferenceWindow.TextBoxApplyDropShadowEffectCheckBox.IsChecked.ToString();


        settings[nameof(CaptureTextFromClipboard)].Value =
            preferenceWindow.CaptureTextFromClipboardCheckBox.IsChecked.ToString();
        settings[nameof(CoreConfig.CaptureTextFromWebSocket)].Value =
            preferenceWindow.CaptureTextFromWebSocketCheckBox.IsChecked.ToString();

        settings[nameof(OnlyCaptureTextWithJapaneseChars)].Value =
            preferenceWindow.OnlyCaptureTextWithJapaneseCharsCheckBox.IsChecked.ToString();
        settings[nameof(DisableLookupsForNonJapaneseCharsInMainWindow)].Value =
            preferenceWindow.DisableLookupsForNonJapaneseCharsInMainWindowCheckBox.IsChecked.ToString();
        settings[nameof(MainWindowFocusOnHover)].Value =
            preferenceWindow.MainWindowFocusOnHoverCheckBox.IsChecked.ToString();
        settings[nameof(SteppedBacklogWithMouseWheel)].Value =
            preferenceWindow.SteppedBacklogWithMouseWheelCheckBox.IsChecked.ToString();
        settings[nameof(EnableBacklog)].Value =
            preferenceWindow.EnableBacklogCheckBox.IsChecked.ToString();
        settings[nameof(AutoSaveBacklogBeforeClosing)].Value =
            preferenceWindow.AutoSaveBacklogBeforeClosingCheckBox.IsChecked.ToString();
        settings[nameof(TextToSpeechOnTextChange)].Value =
            preferenceWindow.TextToSpeechOnTextChangeCheckBox.IsChecked.ToString();
        settings[nameof(HidePopupsOnTextChange)].Value =
            preferenceWindow.HidePopupsOnTextChangeCheckBox.IsChecked.ToString();
        settings[nameof(HideAllTitleBarButtonsWhenMouseIsNotOverTitleBar)].Value =
            preferenceWindow.ToggleHideAllTitleBarButtonsWhenMouseIsNotOverTitleBarCheckBox.IsChecked.ToString();
        settings[nameof(HorizontallyCenterMainWindowText)].Value =
            preferenceWindow.HorizontallyCenterMainWindowTextCheckBox.IsChecked.ToString();

        settings[nameof(MainWindowTextColor)].Value =
            preferenceWindow.TextBoxTextColorButton.Tag.ToString();
        settings[nameof(MainWindowBacklogTextColor)].Value =
            preferenceWindow.TextBoxBacklogTextColorButton.Tag.ToString();
        settings["MainWindowFontSize"].Value =
            preferenceWindow.TextBoxFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings["MainWindowOpacity"].Value =
            preferenceWindow.MainWindowOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings["Theme"].Value =
            preferenceWindow.ThemeComboBox.SelectedValue.ToString();
        settings["MinimumLogLevel"].Value =
            preferenceWindow.MinimumLogLevelComboBox.SelectedValue.ToString();
        settings["MainWindowFont"].Value =
            preferenceWindow.MainWindowFontComboBox.SelectedValue.ToString();
        settings[nameof(PopupFont)].Value =
            preferenceWindow.PopupFontComboBox.SelectedValue.ToString();

        settings[nameof(CoreConfig.KanjiMode)].Value =
            preferenceWindow.KanjiModeCheckBox.IsChecked.ToString();
        settings[nameof(CoreConfig.ForceSyncAnki)].Value =
            preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString();
        settings[nameof(CoreConfig.AllowDuplicateCards)].Value =
            preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked.ToString();
        settings[nameof(CoreConfig.LookupRate)].Value =
            preferenceWindow.LookupRateNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(AutoAdjustFontSizesOnResolutionChange)].Value =
            preferenceWindow.AutoAdjustFontSizesOnResolutionChange.IsChecked.ToString();
        settings[nameof(HighlightLongestMatch)].Value =
            preferenceWindow.HighlightLongestMatchCheckBox.IsChecked.ToString();
        settings[nameof(AutoPlayAudio)].Value =
            preferenceWindow.AutoPlayAudioCheckBox.IsChecked.ToString();
        settings[nameof(Precaching)].Value =
            preferenceWindow.PrecachingCheckBox.IsChecked.ToString();
        settings[nameof(GlobalHotKeys)].Value =
            preferenceWindow.GlobalHotKeysCheckBox.IsChecked.ToString();
        settings[nameof(StopIncreasingTimeStatWhenMinimized)].Value =
            preferenceWindow.StopIncreasingTimeStatWhenMinimizedCheckBox.IsChecked.ToString();
        settings[nameof(StripPunctuationBeforeCalculatingCharacterCount)].Value =
            preferenceWindow.StripPunctuationBeforeCalculatingCharacterCountCheckBox.IsChecked.ToString();
        settings[nameof(CheckForJLUpdatesOnStartUp)].Value =
            preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked.ToString();

        settings[nameof(AlwaysOnTop)].Value =
            preferenceWindow.AlwaysOnTopCheckBox.IsChecked.ToString();

        settings[nameof(RequireLookupKeyPress)].Value =
            preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked.ToString();

        settings[nameof(DisableHotkeys)].Value =
            preferenceWindow.DisableHotkeysCheckBox.IsChecked.ToString();

        settings[nameof(Focusable)].Value =
            preferenceWindow.FocusableCheckBox.IsChecked.ToString();

        settings[nameof(TextOnlyVisibleOnHover)].Value =
            preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked.ToString();

        settings[nameof(CoreConfig.AnkiIntegration)].Value =
            preferenceWindow.AnkiIntegrationCheckBox.IsChecked.ToString();
        settings[nameof(HighlightColor)].Value =
            preferenceWindow.HighlightColorButton.Tag.ToString();

        settings[nameof(MaxNumResultsNotInMiningMode)].Value =
            preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        settings[nameof(CoreConfig.AudioVolume)].Value =
            preferenceWindow.AudioVolumeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        settings[nameof(PopupMaxWidth)].Value =
            preferenceWindow.PopupMaxWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(PopupMaxHeight)].Value =
            preferenceWindow.PopupMaxHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(FixedPopupPositioning)].Value =
            preferenceWindow.FixedPopupPositioningCheckBox.IsChecked.ToString();
        settings[nameof(FixedPopupXPosition)].Value =
            preferenceWindow.FixedPopupXPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(FixedPopupYPosition)].Value =
            preferenceWindow.FixedPopupYPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(PopupDynamicHeight)].Value =
            preferenceWindow.PopupDynamicHeightCheckBox.IsChecked.ToString();
        settings[nameof(PopupDynamicWidth)].Value =
            preferenceWindow.PopupDynamicWidthCheckBox.IsChecked.ToString();

        // We want the opaque color here
        settings[nameof(PopupBackgroundColor)].Value =
            preferenceWindow.PopupBackgroundColorButton.Background.ToString(CultureInfo.InvariantCulture);

        settings[nameof(PrimarySpellingColor)].Value =
            preferenceWindow.PrimarySpellingColorButton.Tag.ToString();
        settings[nameof(ReadingsColor)].Value =
            preferenceWindow.ReadingsColorButton.Tag.ToString();
        settings[nameof(AlternativeSpellingsColor)].Value =
            preferenceWindow.AlternativeSpellingsColorButton.Tag.ToString();
        settings[nameof(DefinitionsColor)].Value =
            preferenceWindow.DefinitionsColorButton.Tag.ToString();
        settings[nameof(FrequencyColor)].Value =
            preferenceWindow.FrequencyColorButton.Tag.ToString();
        settings[nameof(DeconjugationInfoColor)].Value =
            preferenceWindow.DeconjugationInfoColorButton.Tag.ToString();
        settings["PopupOpacity"].Value =
            preferenceWindow.PopupOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(PrimarySpellingFontSize)].Value =
            preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(ReadingsFontSize)].Value =
            preferenceWindow.ReadingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(AlternativeSpellingsFontSize)].Value =
            preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(DefinitionsFontSize)].Value =
            preferenceWindow.DefinitionsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(FrequencyFontSize)].Value =
            preferenceWindow.FrequencyFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(DeconjugationInfoFontSize)].Value =
            preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(DictTypeFontSize)].Value =
            preferenceWindow.DictTypeFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        settings[nameof(SeparatorColor)].Value =
            preferenceWindow.SeparatorColorButton.Tag.ToString();

        settings[nameof(DictTypeColor)].Value =
            preferenceWindow.DictTypeColorButton.Tag.ToString();

        settings[nameof(PopupFocusOnLookup)].Value =
            preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked.ToString();
        settings[nameof(PopupXOffset)].Value =
            preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings[nameof(PopupYOffset)].Value =
            preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        settings["PopupFlip"].Value =
            preferenceWindow.PopupFlipComboBox.SelectedValue.ToString();

        settings[nameof(ShowMiningModeReminder)].Value =
            preferenceWindow.ShowMiningModeReminderCheckBox.IsChecked.ToString();

        settings[nameof(DisableLookupsForNonJapaneseCharsInPopups)].Value =
            preferenceWindow.DisableLookupsForNonJapaneseCharsInPopupsCheckBox.IsChecked.ToString();

        settings[nameof(HideDictTabsWithNoResults)].Value =
            preferenceWindow.HideDictTabsWithNoResultsCheckBox.IsChecked.ToString();

        settings[nameof(AutoHidePopupIfMouseIsNotOverIt)].Value =
            preferenceWindow.AutoHidePopupIfMouseIsNotOverItCheckBox.IsChecked.ToString();

        settings[nameof(AutoHidePopupIfMouseIsNotOverItDelayInMilliseconds)].Value =
            preferenceWindow.AutoHidePopupIfMouseIsNotOverItDelayInMillisecondsNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        settings["LookupMode"].Value =
            preferenceWindow.LookupModeComboBox.SelectedValue.ToString();

        settings[nameof(LookupOnClickMouseButton)].Value =
            preferenceWindow.LookupOnClickMouseButtonComboBox.SelectedValue.ToString();

        settings[nameof(MiningModeMouseButton)].Value =
            preferenceWindow.MiningModeMouseButtonComboBox.SelectedValue.ToString();

        settings[nameof(MineMouseButton)].Value =
            preferenceWindow.MineMouseButtonComboBox.SelectedValue.ToString();

        settings[nameof(CopyPrimarySpellingToClipboardMouseButton)].Value =
            preferenceWindow.CopyPrimarySpellingToClipboardMouseButtonComboBox.SelectedValue.ToString();

        MainWindow mainWindow = MainWindow.Instance;
        settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString(CultureInfo.InvariantCulture);
        settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString(CultureInfo.InvariantCulture);

        config.Save(ConfigurationSaveMode.Modified);

        ApplyPreferences();

        if (preferenceWindow.SetAnkiConfig)
        {
            await preferenceWindow.SaveMiningSetup().ConfigureAwait(false);
        }
    }

    public static void SaveBeforeClosing()
    {
        CreateDefaultAppConfig();

        Configuration config = ConfigurationManager.OpenMappedExeConfiguration(MappedExeConfiguration, ConfigurationUserLevel.None);
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        MainWindow mainWindow = MainWindow.Instance;

        settings["MainWindowFontSize"].Value = mainWindow.FontSizeSlider.Value.ToString(CultureInfo.InvariantCulture);
        settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString(CultureInfo.InvariantCulture);

        settings[nameof(MainWindowHeight)].Value = MainWindowHeight > mainWindow.MinHeight
            ? MainWindowHeight.ToString(CultureInfo.InvariantCulture)
            : mainWindow.MinHeight.ToString(CultureInfo.InvariantCulture);

        settings[nameof(MainWindowWidth)].Value = MainWindowWidth > mainWindow.MinWidth
            ? MainWindowWidth.ToString(CultureInfo.InvariantCulture)
            : mainWindow.MinWidth.ToString(CultureInfo.InvariantCulture);

        settings["MainWindowTopPosition"].Value = mainWindow.Top >= SystemParameters.VirtualScreenTop
            ? mainWindow.Top.ToString(CultureInfo.InvariantCulture)
            : "0";

        settings["MainWindowLeftPosition"].Value = mainWindow.Left >= SystemParameters.VirtualScreenLeft
            ? mainWindow.Left.ToString(CultureInfo.InvariantCulture)
            : "0";

        // TODO: properties with public setters should be saved here?

        config.Save(ConfigurationSaveMode.Modified);
    }

    private static void CreateDefaultAppConfig()
    {
        if (!File.Exists(ProfileUtils.DefaultProfilePath))
        {
            using (XmlWriter writer = XmlWriter.Create(ProfileUtils.DefaultProfilePath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("appSettings");
                writer.WriteEndDocument();
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.Save(ConfigurationSaveMode.Full);
        }
    }

    private delegate bool TryParseHandler<T>(string value, out T? result);

    private delegate bool TryParseHandlerWithCultureInfo<T>(string value, NumberStyles numberStyles, CultureInfo cultureInfo, out T result);

    private static T GetValueFromConfig<T>(Configuration config, T variable, string configKey, TryParseHandler<T> tryParseHandler) where T : struct
    {
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        string? configValue = settings.Get(configKey);
        if (configValue is not null && tryParseHandler(configValue, out T value))
        {
            return value;
        }

        if (settings.Get(configKey) is null)
        {
            config.AppSettings.Settings.Add(configKey, variable.ToString());
        }
        else
        {
            settings[configKey].Value = variable.ToString();
        }

        config.Save(ConfigurationSaveMode.Modified);

        return variable;
    }

    private static T GetNumberWithDecimalPointFromConfig<T>(Configuration config, T number, string configKey, TryParseHandlerWithCultureInfo<T> tryParseHandler) where T : struct
    {
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        string? configValue = settings.Get(configKey);
        if (configValue is not null && tryParseHandler(configValue, NumberStyles.Number, CultureInfo.InvariantCulture, out T value))
        {
            return value;
        }

        if (settings.Get(configKey) is null)
        {
            config.AppSettings.Settings.Add(configKey, Convert.ToString(number, CultureInfo.InvariantCulture));
        }
        else
        {
            settings[configKey].Value = Convert.ToString(number, CultureInfo.InvariantCulture);
        }

        config.Save(ConfigurationSaveMode.Modified);

        return number;
    }

    private static Brush GetBrushFromConfig(Configuration config, Brush solidColorBrush, string configKey)
    {
        KeyValueConfigurationCollection settings = config.AppSettings.Settings;

        string? configValue = settings.Get(configKey);
        if (configValue is not null)
        {
            Brush? brushFromConfig = WindowsUtils.BrushFromHex(configValue);
            if (brushFromConfig is not null)
            {
                return brushFromConfig;
            }
        }

        if (settings.Get(configKey) is null)
        {
            config.AppSettings.Settings.Add(configKey, solidColorBrush.ToString(CultureInfo.InvariantCulture));
        }
        else
        {
            settings[configKey].Value = solidColorBrush.ToString(CultureInfo.InvariantCulture);
        }

        config.Save(ConfigurationSaveMode.Modified);

        return solidColorBrush.IsFrozen
            ? WindowsUtils.BrushFromHex(solidColorBrush.ToString(CultureInfo.InvariantCulture))!
            : solidColorBrush;
    }

    private static Brush GetFrozenBrushFromConfig(Configuration config, Brush solidColorBrush, string configKey)
    {
        Brush brush = GetBrushFromConfig(config, solidColorBrush, configKey);
        brush.Freeze();
        return brush;
    }

    private static void AddToConfig(Configuration config, string key, string value)
    {
        config.AppSettings.Settings.Add(key, value);
        config.Save(ConfigurationSaveMode.Modified);
    }

    private static void SaveKeyGesture(Configuration config, string key, string rawKeyGesture)
    {
        config.AppSettings.Settings[key].Value = rawKeyGesture.StartsWith("Win+", StringComparison.Ordinal)
            ? rawKeyGesture[4..]
            : rawKeyGesture;

        config.Save(ConfigurationSaveMode.Modified);
    }
}
