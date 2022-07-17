using System.Configuration;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using JL.Core;
using JL.Windows.GUI;
using JL.Windows.Utilities;

namespace JL.Windows;

public class ConfigManager : CoreConfig
{
    private static ConfigManager? s_instance;

    public static ConfigManager Instance
    {
        get { return s_instance ??= new ConfigManager(); }
    }

    #region General

    private static readonly List<ComboBoxItem> s_japaneseFonts =
        WindowsUtils.FindJapaneseFonts().OrderByDescending(f => f.Foreground!.ToString()).ThenBy(font => font.Content)
            .ToList();

    private static readonly List<ComboBoxItem> s_popupJapaneseFonts =
        s_japaneseFonts.ConvertAll(f => new ComboBoxItem()
        {
            Content = f.Content,
            FontFamily = f.FontFamily,
            Foreground = f.Foreground
        });

    public static bool InactiveLookupMode { get; set; } = false; // todo checkbox?
    public static bool InvisibleMode { get; set; } = false; // todo checkbox?
    public static Brush HighlightColor { get; private set; } = Brushes.AliceBlue;
    public static bool RequireLookupKeyPress { get; private set; } = false;
    public static bool LookupOnSelectOnly { get; private set; } = false;
    public static bool LookupOnLeftClickOnly { get; private set; } = false;

    public static KeyGesture LookupKeyKeyGesture { get; private set; } = new(Key.LeftShift, ModifierKeys.None);
    public static bool HighlightLongestMatch { get; private set; } = false;
    public static bool AutoPlayAudio { get; private set; } = false;
    public static bool CheckForJLUpdatesOnStartUp { get; private set; } = true;
    public static bool AlwaysOnTop { get; set; } = true;
    public static bool DisableHotkeys { get; set; } = false;
    public static bool Focusable { get; private set; } = true;

    #endregion

    #region Textbox

    public static double MainWindowWidth { get; set; } = 800;
    public static double MainWindowHeight { get; set; } = 200;
    public static bool MainWindowDynamicHeight { get; private set; } = false;
    public static bool MainWindowDynamicWidth { get; private set; } = false;
    public static double MainWindowMaxDynamicWidth { get; private set; } = 800;
    public static double MainWindowMaxDynamicHeight { get; private set; } = 269;
    public static Brush MainWindowTextColor { get; private set; } = Brushes.White;
    public static Brush MainWindowBacklogTextColor { get; private set; } = Brushes.Bisque;
    public static bool TextOnlyVisibleOnHover { get; set; } = false;
    public static bool ChangeMainWindowBackgroundOpacityOnUnhover { get; private set; } = false;
    public static double MainWindowBackgroundOpacityOnUnhover { get; private set; } = 0.2; // 0.2-100
    public static bool TextBoxTrimWhiteSpaceCharacters { get; private set; } = true;
    public static bool TextBoxRemoveNewlines { get; private set; } = false;
    public static bool TextBoxIsReadOnly { get; set; } = true;

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
    public static Brush PopupBackgroundColor { get; private set; } = Brushes.Black;
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

    #endregion

    #region Anki
    public static bool AnkiIntegration { get; private set; } = false;
    #endregion

    #region Hotkeys

    public static KeyGesture DisableHotkeysKeyGesture { get; private set; } = new(Key.Pause, ModifierKeys.Windows);
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
    public static KeyGesture MousePassThroughModeKeyGesture { get; private set; } = new(Key.T, ModifierKeys.Windows);
    public static KeyGesture InvisibleToggleModeKeyGesture { get; private set; } = new(Key.I, ModifierKeys.Windows);
    public static KeyGesture SteppedBacklogBackwardsKeyGesture { get; private set; } = new(Key.Left, ModifierKeys.Windows);
    public static KeyGesture SteppedBacklogForwardsKeyGesture { get; private set; } = new(Key.Right, ModifierKeys.Windows);
    public static KeyGesture InactiveLookupModeKeyGesture { get; private set; } = new(Key.Q, ModifierKeys.Windows);
    public static KeyGesture MotivationKeyGesture { get; private set; } = new(Key.O, ModifierKeys.Windows);
    public static KeyGesture ClosePopupKeyGesture { get; private set; } = new(Key.Escape, ModifierKeys.Windows);
    public static KeyGesture ShowStatsKeyGesture { get; private set; } = new(Key.Y, ModifierKeys.Windows);
    public static KeyGesture NextDictKeyGesture { get; private set; } = new(Key.PageDown, ModifierKeys.Windows);
    public static KeyGesture PreviousDictKeyGesture { get; private set; } = new(Key.PageUp, ModifierKeys.Windows);
    public static KeyGesture AlwaysOnTopKeyGesture { get; private set; } = new(Key.R, ModifierKeys.Windows);
    public static KeyGesture TextOnlyVisibleOnHoverKeyGesture { get; private set; } = new(Key.E, ModifierKeys.Windows);
    public static KeyGesture TextBoxIsReadOnlyKeyGesture { get; private set; } = new(Key.U, ModifierKeys.Windows);

    #endregion

    #region Advanced

    public static int MaxSearchLength { get; private set; } = 37;

    public static int MaxNumResultsNotInMiningMode { get; private set; } = 7;

    public static bool Precaching { get; private set; } = false;

    #endregion

    public void ApplyPreferences()
    {
        MainWindow mainWindow = MainWindow.Instance;

        string? tempStr = ConfigurationManager.AppSettings.Get("AnkiConnectUri");
        if (tempStr == null)
        {
            tempStr = "http://localhost:8765";
            WindowsUtils.AddToConfig("AnkiConnectUri", "http://localhost:8765");
        }

        AnkiConnectUri = tempStr;

        WindowsUtils.Try(
            () => HighlightLongestMatch =
                bool.Parse(ConfigurationManager.AppSettings.Get("HighlightLongestMatch")!),
            HighlightLongestMatch, "HighlightLongestMatch");

        WindowsUtils.Try(
            () => AutoPlayAudio =
                bool.Parse(ConfigurationManager.AppSettings.Get("AutoPlayAudio")!),
            AutoPlayAudio, "AutoPlayAudio");

        WindowsUtils.Try(
            () => Precaching =
                bool.Parse(ConfigurationManager.AppSettings.Get(nameof(Precaching))!),
            Precaching, nameof(Precaching));

        WindowsUtils.Try(() => CheckForJLUpdatesOnStartUp =
                bool.Parse(ConfigurationManager.AppSettings.Get("CheckForJLUpdatesOnStartUp")!),
            CheckForJLUpdatesOnStartUp, "CheckForJLUpdatesOnStartUp");

        WindowsUtils.Try(() => AlwaysOnTop =
                bool.Parse(ConfigurationManager.AppSettings.Get("AlwaysOnTop")!),
            AlwaysOnTop, "AlwaysOnTop");
        mainWindow.Topmost = AlwaysOnTop;

        WindowsUtils.Try(() => RequireLookupKeyPress =
                bool.Parse(ConfigurationManager.AppSettings.Get("RequireLookupKeyPress")!),
            RequireLookupKeyPress, "RequireLookupKeyPress");

        WindowsUtils.Try(() => DisableHotkeys =
                bool.Parse(ConfigurationManager.AppSettings.Get("DisableHotkeys")!),
            DisableHotkeys, "DisableHotkeys");

        WindowsUtils.Try(() => Focusable =
                bool.Parse(ConfigurationManager.AppSettings.Get("Focusable")!),
            Focusable, "Focusable");

        WindowsUtils.Try(() => AnkiIntegration =
                bool.Parse(ConfigurationManager.AppSettings.Get("AnkiIntegration")!),
            AnkiIntegration, "AnkiIntegration");

        WindowsUtils.Try(
            () => MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength")!),
            MaxSearchLength, "MaxSearchLength");
        WindowsUtils.Try(() => KanjiMode = bool.Parse(ConfigurationManager.AppSettings.Get("KanjiMode")!),
            KanjiMode,
            "KanjiMode");
        WindowsUtils.Try(() => ForceSyncAnki = bool.Parse(ConfigurationManager.AppSettings.Get("ForceSyncAnki")!),
            ForceSyncAnki, "ForceSyncAnki");
        WindowsUtils.Try(
            () => AllowDuplicateCards =
                bool.Parse(ConfigurationManager.AppSettings.Get("AllowDuplicateCards")!),
            AllowDuplicateCards, "AllowDuplicateCards");
        WindowsUtils.Try(() => LookupRate = int.Parse(ConfigurationManager.AppSettings.Get("LookupRate")!),
            LookupRate,
            "LookupRate");

        // MAKE SURE YOU FREEZE ANY NEW COLOR OBJECTS YOU ADD
        // OR THE PROGRAM WILL CRASH AND BURN
        WindowsUtils.Try(() =>
                MainWindowTextColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowTextColor")!)!,
            MainWindowTextColor, "MainWindowTextColor");
        MainWindowTextColor.Freeze();

        WindowsUtils.Try(() =>
                MainWindowBacklogTextColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBacklogTextColor")!)!,
            MainWindowBacklogTextColor, "MainWindowBacklogTextColor");
        MainWindowBacklogTextColor.Freeze();

        WindowsUtils.Try(() =>
                PrimarySpellingColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("PrimarySpellingColor")!)!,
            PrimarySpellingColor, "PrimarySpellingColor");
        PrimarySpellingColor.Freeze();

        WindowsUtils.Try(() =>
                ReadingsColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("ReadingsColor")!)!,
            ReadingsColor, "ReadingsColor");
        ReadingsColor.Freeze();

        WindowsUtils.Try(() =>
                AlternativeSpellingsColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("AlternativeSpellingsColor")!)!,
            AlternativeSpellingsColor, "AlternativeSpellingsColor");
        AlternativeSpellingsColor.Freeze();

        WindowsUtils.Try(() =>
                DefinitionsColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("DefinitionsColor")!)!,
            DefinitionsColor, "DefinitionsColor");
        DefinitionsColor.Freeze();

        WindowsUtils.Try(() =>
                FrequencyColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("FrequencyColor")!)!,
            FrequencyColor, "FrequencyColor");
        FrequencyColor.Freeze();

        WindowsUtils.Try(() =>
                DeconjugationInfoColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("DeconjugationInfoColor")!)!,
            DeconjugationInfoColor, "DeconjugationInfoColor");
        DeconjugationInfoColor.Freeze();

        WindowsUtils.Try(() =>
                SeparatorColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("SeparatorColor")!)!,
            SeparatorColor, "SeparatorColor");
        SeparatorColor.Freeze();

        WindowsUtils.Try(() =>
                DictTypeColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("DictTypeColor")!)!,
            DictTypeColor, "DictTypeColor");
        DictTypeColor.Freeze();

        WindowsUtils.Try(() =>
                HighlightColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("HighlightColor")!)!,
            HighlightColor, "HighlightColor");
        HighlightColor.Freeze();
        mainWindow.MainTextBox.SelectionBrush = HighlightColor;

        WindowsUtils.Try(() =>
            PopupBackgroundColor = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings
                    .Get("PopupBackgroundColor")!)!, PopupBackgroundColor, "PopupBackgroundColor");
        WindowsUtils.Try(() => PopupBackgroundColor.Opacity = double.Parse(ConfigurationManager.AppSettings
            .Get("PopupOpacity")!, CultureInfo.InvariantCulture) / 100, 70, "PopupOpacity");
        PopupBackgroundColor.Freeze();

        WindowsUtils.Try(() => PrimarySpellingFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("PrimarySpellingFontSize")!), PrimarySpellingFontSize, "PrimarySpellingFontSize");
        WindowsUtils.Try(() => ReadingsFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("ReadingsFontSize")!), ReadingsFontSize, "ReadingsFontSize");
        WindowsUtils.Try(() => AlternativeSpellingsFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("AlternativeSpellingsFontSize")!), AlternativeSpellingsFontSize, "AlternativeSpellingsFontSize");
        WindowsUtils.Try(() => DefinitionsFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("DefinitionsFontSize")!), DefinitionsFontSize, "DefinitionsFontSize");
        WindowsUtils.Try(() => FrequencyFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("FrequencyFontSize")!), FrequencyFontSize, "FrequencyFontSize");
        WindowsUtils.Try(() => DeconjugationInfoFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("DeconjugationInfoFontSize")!), DeconjugationInfoFontSize, "DeconjugationInfoFontSize");
        WindowsUtils.Try(() => DictTypeFontSize = int.Parse(ConfigurationManager.AppSettings
            .Get("DictTypeFontSize")!), DictTypeFontSize, "DictTypeFontSize");

        WindowsUtils.Try(() => MaxNumResultsNotInMiningMode = int.Parse(ConfigurationManager.AppSettings
            .Get(nameof(MaxNumResultsNotInMiningMode))!), MaxNumResultsNotInMiningMode, nameof(MaxNumResultsNotInMiningMode));

        WindowsUtils.Try(() => PopupFocusOnLookup = bool.Parse(ConfigurationManager.AppSettings
            .Get("PopupFocusOnLookup")!), PopupFocusOnLookup, "PopupFocusOnLookup");
        WindowsUtils.Try(() => PopupXOffset = int.Parse(ConfigurationManager.AppSettings
            .Get("PopupXOffset")!), PopupXOffset, "PopupXOffset");
        WindowsUtils.Try(() => PopupYOffset = int.Parse(ConfigurationManager.AppSettings
            .Get("PopupYOffset")!), PopupYOffset, "PopupYOffset");

        WindowsUtils.Try(() => ShowMiningModeReminder = bool.Parse(ConfigurationManager.AppSettings
            .Get("ShowMiningModeReminder")!), ShowMiningModeReminder, "ShowMiningModeReminder");

        WindowsUtils.Try(() => DisableLookupsForNonJapaneseCharsInPopups = bool.Parse(ConfigurationManager.AppSettings
            .Get("DisableLookupsForNonJapaneseCharsInPopups")!), DisableLookupsForNonJapaneseCharsInPopups, "DisableLookupsForNonJapaneseCharsInPopups");

        WindowsUtils.DpiAwareXOffset = PopupXOffset / WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareYOffset = PopupYOffset / WindowsUtils.Dpi.DpiScaleY;

        WindowsUtils.Try(() => PopupMaxWidth = int.Parse(ConfigurationManager.AppSettings
            .Get("PopupMaxWidth")!), PopupMaxWidth, "PopupMaxWidth");
        WindowsUtils.Try(() => PopupMaxHeight = int.Parse(ConfigurationManager.AppSettings
            .Get("PopupMaxHeight")!), PopupMaxHeight, "PopupMaxHeight");
        WindowsUtils.DpiAwarePopupMaxWidth = PopupMaxWidth / WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwarePopupMaxHeight = PopupMaxHeight / WindowsUtils.Dpi.DpiScaleY;

        WindowsUtils.Try(() => FixedPopupPositioning = bool.Parse(ConfigurationManager.AppSettings
            .Get("FixedPopupPositioning")!), FixedPopupPositioning, "FixedPopupPositioning");

        WindowsUtils.Try(() => FixedPopupXPosition = int.Parse(ConfigurationManager.AppSettings
            .Get("FixedPopupXPosition")!), FixedPopupXPosition, "FixedPopupXPosition");
        WindowsUtils.Try(() => FixedPopupYPosition = int.Parse(ConfigurationManager.AppSettings
            .Get("FixedPopupYPosition")!), FixedPopupYPosition, "FixedPopupYPosition");

        WindowsUtils.DpiAwareFixedPopupXPosition = FixedPopupXPosition / WindowsUtils.Dpi.DpiScaleX;
        WindowsUtils.DpiAwareFixedPopupYPosition = FixedPopupYPosition / WindowsUtils.Dpi.DpiScaleY;

        tempStr = ConfigurationManager.AppSettings.Get("PopupFlip");

        if (tempStr == null)
        {
            WindowsUtils.AddToConfig("PopupFlip", "Both");
        }

        switch (ConfigurationManager.AppSettings.Get("PopupFlip"))
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
                PopupFlipX = false;
                PopupFlipY = true;
                break;
        }

        tempStr = ConfigurationManager.AppSettings.Get("LookupMode");

        if (tempStr == null)
        {
            WindowsUtils.AddToConfig("LookupMode", "Hover");
        }

        switch (ConfigurationManager.AppSettings.Get("LookupMode"))
        {
            case "Hover":
                LookupOnLeftClickOnly = false;
                LookupOnSelectOnly = false;
                break;

            case "Click":
                LookupOnLeftClickOnly = true;
                LookupOnSelectOnly = false;
                break;

            case "Select":
                LookupOnLeftClickOnly = false;
                LookupOnSelectOnly = true;
                break;

            default:
                LookupOnLeftClickOnly = false;
                LookupOnSelectOnly = false;
                break;
        }

        DisableHotkeysKeyGesture = WindowsUtils.KeyGestureSetter("DisableHotkeysKeyGesture", DisableHotkeysKeyGesture);
        MiningModeKeyGesture = WindowsUtils.KeyGestureSetter("MiningModeKeyGesture", MiningModeKeyGesture);
        PlayAudioKeyGesture = WindowsUtils.KeyGestureSetter("PlayAudioKeyGesture", PlayAudioKeyGesture);
        KanjiModeKeyGesture = WindowsUtils.KeyGestureSetter("KanjiModeKeyGesture", KanjiModeKeyGesture);
        LookupKeyKeyGesture = WindowsUtils.KeyGestureSetter("LookupKeyKeyGesture", LookupKeyKeyGesture);

        ShowManageDictionariesWindowKeyGesture =
            WindowsUtils.KeyGestureSetter("ShowManageDictionariesWindowKeyGesture",
                ShowManageDictionariesWindowKeyGesture);

        ShowManageFrequenciesWindowKeyGesture =
            WindowsUtils.KeyGestureSetter("ShowManageFrequenciesWindowKeyGesture",
                ShowManageFrequenciesWindowKeyGesture);

        ShowPreferencesWindowKeyGesture =
            WindowsUtils.KeyGestureSetter("ShowPreferencesWindowKeyGesture", ShowPreferencesWindowKeyGesture);
        ShowAddNameWindowKeyGesture =
            WindowsUtils.KeyGestureSetter("ShowAddNameWindowKeyGesture", ShowAddNameWindowKeyGesture);
        ShowAddWordWindowKeyGesture =
            WindowsUtils.KeyGestureSetter("ShowAddWordWindowKeyGesture", ShowAddWordWindowKeyGesture);
        SearchWithBrowserKeyGesture =
            WindowsUtils.KeyGestureSetter("SearchWithBrowserKeyGesture", SearchWithBrowserKeyGesture);
        MousePassThroughModeKeyGesture =
            WindowsUtils.KeyGestureSetter("MousePassThroughModeKeyGesture", MousePassThroughModeKeyGesture);
        InvisibleToggleModeKeyGesture =
            WindowsUtils.KeyGestureSetter("InvisibleToggleModeKeyGesture", InvisibleToggleModeKeyGesture);
        SteppedBacklogBackwardsKeyGesture =
            WindowsUtils.KeyGestureSetter("SteppedBacklogBackwardsKeyGesture", SteppedBacklogBackwardsKeyGesture);
        SteppedBacklogForwardsKeyGesture =
            WindowsUtils.KeyGestureSetter("SteppedBacklogForwardsKeyGesture", SteppedBacklogForwardsKeyGesture);
        InactiveLookupModeKeyGesture =
            WindowsUtils.KeyGestureSetter("InactiveLookupModeKeyGesture", InactiveLookupModeKeyGesture);
        MotivationKeyGesture =
            WindowsUtils.KeyGestureSetter("MotivationKeyGesture", MotivationKeyGesture);

        ClosePopupKeyGesture = WindowsUtils.KeyGestureSetter("ClosePopupKeyGesture", ClosePopupKeyGesture);

        ShowStatsKeyGesture = WindowsUtils.KeyGestureSetter("ShowStatsKeyGesture", ShowStatsKeyGesture);

        NextDictKeyGesture = WindowsUtils.KeyGestureSetter("NextDictKeyGesture", NextDictKeyGesture);
        PreviousDictKeyGesture = WindowsUtils.KeyGestureSetter("PreviousDictKeyGesture", PreviousDictKeyGesture);

        AlwaysOnTopKeyGesture = WindowsUtils.KeyGestureSetter("AlwaysOnTopKeyGesture", AlwaysOnTopKeyGesture);
        TextOnlyVisibleOnHoverKeyGesture = WindowsUtils.KeyGestureSetter("TextOnlyVisibleOnHoverKeyGesture", TextOnlyVisibleOnHoverKeyGesture);
        TextBoxIsReadOnlyKeyGesture = WindowsUtils.KeyGestureSetter("TextBoxIsReadOnlyKeyGesture", TextBoxIsReadOnlyKeyGesture);

        WindowsUtils.SetInputGestureText(mainWindow.AddNameButton, ShowAddNameWindowKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.AddWordButton, ShowAddWordWindowKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.SearchButton, SearchWithBrowserKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.PreferencesButton, ShowPreferencesWindowKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.ManageDictionariesButton, ShowManageDictionariesWindowKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.ManageFrequenciesButton, ShowManageFrequenciesWindowKeyGesture);
        WindowsUtils.SetInputGestureText(mainWindow.StatsButton, ShowStatsKeyGesture);

        WindowsUtils.Try(() => mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager
            .AppSettings.Get("MainWindowOpacity")!, CultureInfo.InvariantCulture), mainWindow.OpacitySlider.Value, "MainWindowOpacity");

        WindowsUtils.Try(() => mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager
            .AppSettings.Get("MainWindowFontSize")!, CultureInfo.InvariantCulture), mainWindow.FontSizeSlider.Value, "MainWindowFontSize");

        tempStr = ConfigurationManager.AppSettings.Get("MainWindowFont");

        if (tempStr == null)
        {
            WindowsUtils.AddToConfig("MainWindowFont", "Meiryo");
            tempStr = "Meiryo";
        }

        mainWindow.MainTextBox.FontFamily = new FontFamily(tempStr);

        WindowsUtils.Try(() =>
                mainWindow.Background = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")!)!,
            mainWindow.Background, "MainWindowBackgroundColor");
        mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value / 100;

        mainWindow.MainTextBox.Foreground = MainWindowTextColor;

        WindowsUtils.Try(() => TextOnlyVisibleOnHover =
            bool.Parse(ConfigurationManager.AppSettings.Get("TextOnlyVisibleOnHover")!),
            TextOnlyVisibleOnHover, "TextOnlyVisibleOnHover");
        mainWindow.MainGrid.Opacity = TextOnlyVisibleOnHover && !mainWindow.IsMouseOver ? 0 : 1;

        WindowsUtils.Try(() => ChangeMainWindowBackgroundOpacityOnUnhover =
            bool.Parse(ConfigurationManager.AppSettings.Get("ChangeMainWindowBackgroundOpacityOnUnhover")!),
            ChangeMainWindowBackgroundOpacityOnUnhover, "ChangeMainWindowBackgroundOpacityOnUnhover");

        WindowsUtils.Try(() => MainWindowBackgroundOpacityOnUnhover =
            double.Parse(ConfigurationManager.AppSettings.Get("MainWindowBackgroundOpacityOnUnhover")!, CultureInfo.InvariantCulture),
            MainWindowBackgroundOpacityOnUnhover, "MainWindowBackgroundOpacityOnUnhover");

        if (ChangeMainWindowBackgroundOpacityOnUnhover && !mainWindow.IsMouseOver)
        {
            mainWindow.Background.Opacity = MainWindowBackgroundOpacityOnUnhover / 100;
        }

        WindowsUtils.Try(() => TextBoxIsReadOnly =
            bool.Parse(ConfigurationManager.AppSettings.Get("TextBoxIsReadOnly")!),
            TextBoxIsReadOnly, "TextBoxIsReadOnly");
        mainWindow.MainTextBox.IsReadOnly = TextBoxIsReadOnly;
        mainWindow.MainTextBox.IsUndoEnabled = !TextBoxIsReadOnly;

        WindowsUtils.Try(() => TextBoxTrimWhiteSpaceCharacters =
            bool.Parse(ConfigurationManager.AppSettings.Get("TextBoxTrimWhiteSpaceCharacters")!),
            TextBoxTrimWhiteSpaceCharacters, "TextBoxTrimWhiteSpaceCharacters");

        WindowsUtils.Try(() => TextBoxRemoveNewlines =
            bool.Parse(ConfigurationManager.AppSettings.Get("TextBoxRemoveNewlines")!),
            TextBoxRemoveNewlines, "TextBoxRemoveNewlines");

        WindowsUtils.Try(() => MainWindowDynamicHeight = bool.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowDynamicHeight")!), MainWindowDynamicHeight, "MainWindowDynamicHeight");
        WindowsUtils.Try(() => MainWindowDynamicWidth = bool.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowDynamicWidth")!), MainWindowDynamicWidth, "MainWindowDynamicWidth");

        WindowsUtils.Try(() => MainWindowHeight = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowHeight")!, CultureInfo.InvariantCulture), MainWindowHeight, "MainWindowHeight");

        WindowsUtils.Try(() => MainWindowWidth = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowWidth")!, CultureInfo.InvariantCulture), MainWindowWidth, "MainWindowWidth");

        WindowsUtils.Try(() => MainWindowMaxDynamicWidth = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowMaxDynamicWidth")!, CultureInfo.InvariantCulture), MainWindowMaxDynamicWidth, "MainWindowMaxDynamicWidth");
        WindowsUtils.Try(() => MainWindowMaxDynamicHeight = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowMaxDynamicHeight")!, CultureInfo.InvariantCulture), MainWindowMaxDynamicHeight, "MainWindowMaxDynamicHeight");

        WindowsUtils.SetSizeToContentForMainWindow(MainWindowDynamicWidth, MainWindowDynamicHeight, MainWindowMaxDynamicWidth, MainWindowMaxDynamicHeight,
            MainWindowWidth, MainWindowHeight, mainWindow);
        mainWindow.WidthBeforeResolutionChange = MainWindowWidth;
        mainWindow.HeightBeforeResolutionChange = MainWindowHeight;

        WindowsUtils.Try(() => mainWindow.Top = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowTopPosition")!, CultureInfo.InvariantCulture), mainWindow.Top, "MainWindowTopPosition");
        mainWindow.TopPositionBeforeResolutionChange = mainWindow.Top;

        WindowsUtils.Try(() => mainWindow.Left = double.Parse(ConfigurationManager.AppSettings
            .Get("MainWindowLeftPosition")!, CultureInfo.InvariantCulture), mainWindow.Left, "MainWindowLeftPosition");
        mainWindow.LeftPositionBeforeResolutionChange = mainWindow.Left;

        tempStr = ConfigurationManager.AppSettings.Get("PopupFont");

        if (tempStr == null)
            WindowsUtils.AddToConfig("PopupFont", PopupFont.Source);
        else
            PopupFont = new FontFamily(tempStr);

        WindowsUtils.Try(() => PopupDynamicHeight = bool.Parse(ConfigurationManager.AppSettings
            .Get("PopupDynamicHeight")!), PopupDynamicHeight, "PopupDynamicHeight");
        WindowsUtils.Try(() => PopupDynamicWidth = bool.Parse(ConfigurationManager.AppSettings
            .Get("PopupDynamicWidth")!), PopupDynamicWidth, "PopupDynamicWidth");

        PopupWindow? currentPopupWindow = mainWindow.FirstPopupWindow;

        while (currentPopupWindow != null)
        {
            currentPopupWindow.Background = PopupBackgroundColor;
            currentPopupWindow.FontFamily = PopupFont;

            WindowsUtils.SetSizeToContentForPopup(PopupDynamicWidth, PopupDynamicHeight, WindowsUtils.DpiAwarePopupMaxHeight, WindowsUtils.DpiAwarePopupMaxWidth, currentPopupWindow);

            WindowsUtils.SetInputGestureText(currentPopupWindow.AddNameButton, ShowAddNameWindowKeyGesture);
            WindowsUtils.SetInputGestureText(currentPopupWindow.AddWordButton, ShowAddWordWindowKeyGesture);
            WindowsUtils.SetInputGestureText(currentPopupWindow.SearchButton, SearchWithBrowserKeyGesture);
            WindowsUtils.SetInputGestureText(currentPopupWindow.StatsButton, ShowStatsKeyGesture);

            currentPopupWindow = currentPopupWindow.ChildPopupWindow;
        }
    }

    public void LoadPreferences(PreferencesWindow preferenceWindow)
    {
        CreateDefaultAppConfig();

        MainWindow mainWindow = MainWindow.Instance;

        preferenceWindow.JLVersionTextBlock.Text = "v" + Storage.JLVersion;

        preferenceWindow.DisableHotkeysKeyGestureTextBox.Text = WindowsUtils.KeyGestureToString(DisableHotkeysKeyGesture);
        preferenceWindow.MiningModeKeyGestureTextBox.Text = WindowsUtils.KeyGestureToString(MiningModeKeyGesture);
        preferenceWindow.PlayAudioKeyGestureTextBox.Text = WindowsUtils.KeyGestureToString(PlayAudioKeyGesture);
        preferenceWindow.KanjiModeKeyGestureTextBox.Text = WindowsUtils.KeyGestureToString(KanjiModeKeyGesture);
        preferenceWindow.LookupKeyKeyGestureTextBox.Text = WindowsUtils.KeyGestureToString(LookupKeyKeyGesture);

        preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowManageDictionariesWindowKeyGesture);
        preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowManageFrequenciesWindowKeyGesture);
        preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowPreferencesWindowKeyGesture);
        preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowAddNameWindowKeyGesture);
        preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowAddWordWindowKeyGesture);
        preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(SearchWithBrowserKeyGesture);
        preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(MousePassThroughModeKeyGesture);
        preferenceWindow.InvisibleToggleModeKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(InvisibleToggleModeKeyGesture);
        preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(SteppedBacklogBackwardsKeyGesture);
        preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(SteppedBacklogForwardsKeyGesture);
        preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(InactiveLookupModeKeyGesture);
        preferenceWindow.MotivationKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(MotivationKeyGesture);
        preferenceWindow.ClosePopupKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ClosePopupKeyGesture);
        preferenceWindow.ShowStatsKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(ShowStatsKeyGesture);
        preferenceWindow.NextDictKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(NextDictKeyGesture);
        preferenceWindow.PreviousDictKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(PreviousDictKeyGesture);

        preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(AlwaysOnTopKeyGesture);
        preferenceWindow.TextOnlyVisibleOnHoverKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(TextOnlyVisibleOnHoverKeyGesture);

        preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text =
            WindowsUtils.KeyGestureToString(TextBoxIsReadOnlyKeyGesture);

        preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
        preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
        preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = ForceSyncAnki;
        preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked = AllowDuplicateCards;
        preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;
        preferenceWindow.KanjiModeCheckBox.IsChecked = KanjiMode;
        preferenceWindow.HighlightLongestMatchCheckBox.IsChecked = HighlightLongestMatch;
        preferenceWindow.AutoPlayAudioCheckBox.IsChecked = AutoPlayAudio;
        preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked = CheckForJLUpdatesOnStartUp;
        preferenceWindow.PrecachingCheckBox.IsChecked = Precaching;
        preferenceWindow.AlwaysOnTopCheckBox.IsChecked = AlwaysOnTop;
        preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked = RequireLookupKeyPress;
        preferenceWindow.DisableHotkeysCheckBox.IsChecked = DisableHotkeys;
        preferenceWindow.FocusableCheckBox.IsChecked = Focusable;
        preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked = TextOnlyVisibleOnHover;
        preferenceWindow.AnkiIntegrationCheckBox.IsChecked = AnkiIntegration;
        preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;

        preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked = MainWindowDynamicWidth;
        preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked = MainWindowDynamicHeight;

        preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value = MainWindowMaxDynamicWidth;
        preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value = MainWindowMaxDynamicHeight;

        preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
        preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;
        preferenceWindow.HighlightColorButton.Background = HighlightColor;

        WindowsUtils.Try(() => preferenceWindow.TextboxBackgroundColorButton.Background =
                (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")!)!,
            preferenceWindow.TextboxBackgroundColorButton.Background, "MainWindowBackgroundColor");

        preferenceWindow.TextboxTextColorButton.Background = MainWindowTextColor;
        preferenceWindow.TextboxBacklogTextColorButton.Background = MainWindowBacklogTextColor;
        preferenceWindow.TextboxFontSizeNumericUpDown.Value = mainWindow.FontSizeSlider.Value;
        preferenceWindow.TextboxOpacityNumericUpDown.Value = mainWindow.OpacitySlider.Value;

        preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked = ChangeMainWindowBackgroundOpacityOnUnhover;
        preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value = MainWindowBackgroundOpacityOnUnhover;

        preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked = TextBoxIsReadOnly;
        preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked = TextBoxTrimWhiteSpaceCharacters;
        preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked = TextBoxRemoveNewlines;

        preferenceWindow.MainWindowFontComboBox.ItemsSource = s_japaneseFonts;
        preferenceWindow.MainWindowFontComboBox.SelectedIndex = s_japaneseFonts.FindIndex(f =>
            f.Content.ToString() == mainWindow.MainTextBox.FontFamily.Source);

        preferenceWindow.PopupFontComboBox.ItemsSource = s_popupJapaneseFonts;
        preferenceWindow.PopupFontComboBox.SelectedIndex =
            s_popupJapaneseFonts.FindIndex(f => f.Content.ToString() == PopupFont.Source);

        preferenceWindow.PopupMaxHeightNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Height;
        preferenceWindow.PopupMaxWidthNumericUpDown.Maximum = WindowsUtils.ActiveScreen.Bounds.Width;

        preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value = MaxNumResultsNotInMiningMode;

        preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
        preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
        preferenceWindow.FixedPopupPositioningCheckBox.IsChecked = FixedPopupPositioning;
        preferenceWindow.FixedPopupXPositionNumericUpDown.Value = FixedPopupXPosition;
        preferenceWindow.FixedPopupYPositionNumericUpDown.Value = FixedPopupYPosition;
        preferenceWindow.PopupDynamicHeightCheckBox.IsChecked = PopupDynamicHeight;
        preferenceWindow.PopupDynamicWidthCheckBox.IsChecked = PopupDynamicWidth;
        preferenceWindow.AlternativeSpellingsColorButton.Background = AlternativeSpellingsColor;
        preferenceWindow.DeconjugationInfoColorButton.Background = DeconjugationInfoColor;
        preferenceWindow.DefinitionsColorButton.Background = DefinitionsColor;
        preferenceWindow.FrequencyColorButton.Background = FrequencyColor;
        preferenceWindow.PrimarySpellingColorButton.Background = PrimarySpellingColor;
        preferenceWindow.ReadingsColorButton.Background = ReadingsColor;
        preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
        preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value = DeconjugationInfoFontSize;
        preferenceWindow.DictTypeFontSizeNumericUpDown.Value = DictTypeFontSize;
        preferenceWindow.DefinitionsFontSizeNumericUpDown.Value = DefinitionsFontSize;
        preferenceWindow.FrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
        preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value = PrimarySpellingFontSize;
        preferenceWindow.ReadingsFontSizeNumericUpDown.Value = ReadingsFontSize;

        // Button background color has to be opaque, so we cannot use PopupBackgroundColor here
        WindowsUtils.Try(() => preferenceWindow.PopupBackgroundColorButton.Background =
                (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor")!)!,
            preferenceWindow.PopupBackgroundColorButton.Background, "PopupBackgroundColor");

        WindowsUtils.Try(() => preferenceWindow.PopupOpacityNumericUpDown.Value = double.Parse(
                ConfigurationManager.AppSettings.Get("PopupOpacity")!, CultureInfo.InvariantCulture),
            preferenceWindow.PopupOpacityNumericUpDown.Value, "PopupOpacity");

        preferenceWindow.SeparatorColorButton.Background = SeparatorColor;

        preferenceWindow.DictTypeColorButton.Background = DictTypeColor;

        preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked = PopupFocusOnLookup;
        preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
        preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;
        preferenceWindow.PopupFlipComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("PopupFlip");

        preferenceWindow.LookupModeComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("LookupMode");

        if (preferenceWindow.LookupModeComboBox.SelectedIndex == -1)
        {
            preferenceWindow.LookupModeComboBox.SelectedIndex = 0;
        }

        preferenceWindow.ShowMiningModeReminderCheckBox.IsChecked = ShowMiningModeReminder;
        preferenceWindow.DisableLookupsForNonJapaneseCharsInPopupsCheckBox.IsChecked = DisableLookupsForNonJapaneseCharsInPopups;
    }

    public async Task SavePreferences(PreferencesWindow preferenceWindow)
    {
        WindowsUtils.KeyGestureSaver("DisableHotkeysKeyGesture", preferenceWindow.DisableHotkeysKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("MiningModeKeyGesture", preferenceWindow.MiningModeKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("PlayAudioKeyGesture", preferenceWindow.PlayAudioKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("KanjiModeKeyGesture", preferenceWindow.KanjiModeKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("LookupKeyKeyGesture", preferenceWindow.LookupKeyKeyGestureTextBox.Text);

        WindowsUtils.KeyGestureSaver("ShowManageDictionariesWindowKeyGesture",
            preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ShowManageFrequenciesWindowKeyGesture",
            preferenceWindow.ShowManageFrequenciesWindowKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ShowPreferencesWindowKeyGesture",
            preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ShowAddNameWindowKeyGesture",
            preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ShowAddWordWindowKeyGesture",
            preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("SearchWithBrowserKeyGesture",
            preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("MousePassThroughModeKeyGesture",
            preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("InvisibleToggleModeKeyGesture",
            preferenceWindow.InvisibleToggleModeKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("SteppedBacklogBackwardsKeyGesture",
            preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("SteppedBacklogForwardsKeyGesture",
            preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("InactiveLookupModeKeyGesture",
            preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("MotivationKeyGesture",
            preferenceWindow.MotivationKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ClosePopupKeyGesture",
            preferenceWindow.ClosePopupKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("ShowStatsKeyGesture",
            preferenceWindow.ShowStatsKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("NextDictKeyGesture",
            preferenceWindow.NextDictKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("PreviousDictKeyGesture",
            preferenceWindow.PreviousDictKeyGestureTextBox.Text);

        WindowsUtils.KeyGestureSaver("AlwaysOnTopKeyGesture",
            preferenceWindow.AlwaysOnTopKeyGestureTextBox.Text);
        WindowsUtils.KeyGestureSaver("TextOnlyVisibleOnHoverKeyGesture",
            preferenceWindow.TextOnlyVisibleOnHoverKeyGestureTextBox.Text);

        WindowsUtils.KeyGestureSaver("TextBoxIsReadOnlyKeyGesture",
            preferenceWindow.TextBoxIsReadOnlyKeyGestureTextBox.Text);

        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        config.AppSettings.Settings["MaxSearchLength"].Value =
            preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["AnkiConnectUri"].Value =
            preferenceWindow.AnkiUriTextBox.Text;

        config.AppSettings.Settings["MainWindowDynamicWidth"].Value =
            preferenceWindow.MainWindowDynamicWidthCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["MainWindowDynamicHeight"].Value =
            preferenceWindow.MainWindowDynamicHeightCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["MainWindowMaxDynamicWidth"].Value =
            preferenceWindow.MainWindowMaxDynamicWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowMaxDynamicHeight"].Value =
            preferenceWindow.MainWindowMaxDynamicHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["MainWindowWidth"].Value =
            preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowHeight"].Value =
            preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowBackgroundColor"].Value =
            preferenceWindow.TextboxBackgroundColorButton.Background.ToString();

        config.AppSettings.Settings["ChangeMainWindowBackgroundOpacityOnUnhover"].Value =
            preferenceWindow.ChangeMainWindowBackgroundOpacityOnUnhoverCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["MainWindowBackgroundOpacityOnUnhover"].Value =
            preferenceWindow.MainWindowBackgroundOpacityOnUnhoverNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["TextBoxIsReadOnly"].Value =
            preferenceWindow.TextBoxIsReadOnlyCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["TextBoxTrimWhiteSpaceCharacters"].Value =
            preferenceWindow.TextBoxTrimWhiteSpaceCharactersCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["TextBoxRemoveNewlines"].Value =
            preferenceWindow.TextBoxRemoveNewlinesCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["MainWindowTextColor"].Value =
            preferenceWindow.TextboxTextColorButton.Background.ToString();
        config.AppSettings.Settings["MainWindowBacklogTextColor"].Value =
            preferenceWindow.TextboxBacklogTextColorButton.Background.ToString();
        config.AppSettings.Settings["MainWindowFontSize"].Value =
            preferenceWindow.TextboxFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowOpacity"].Value =
            preferenceWindow.TextboxOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowFont"].Value =
            preferenceWindow.MainWindowFontComboBox.SelectedValue.ToString();
        config.AppSettings.Settings["PopupFont"].Value =
            preferenceWindow.PopupFontComboBox.SelectedValue.ToString();

        config.AppSettings.Settings["KanjiMode"].Value =
            preferenceWindow.KanjiModeCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["ForceSyncAnki"].Value =
            preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["AllowDuplicateCards"].Value =
            preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["LookupRate"].Value =
            preferenceWindow.LookupRateNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["HighlightLongestMatch"].Value =
            preferenceWindow.HighlightLongestMatchCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["AutoPlayAudio"].Value =
            preferenceWindow.AutoPlayAudioCheckBox.IsChecked.ToString();
        config.AppSettings.Settings[nameof(Precaching)].Value =
            preferenceWindow.PrecachingCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["CheckForJLUpdatesOnStartUp"].Value =
            preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["AlwaysOnTop"].Value =
            preferenceWindow.AlwaysOnTopCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["RequireLookupKeyPress"].Value =
            preferenceWindow.RequireLookupKeyPressCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["DisableHotkeys"].Value =
            preferenceWindow.DisableHotkeysCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["Focusable"].Value =
            preferenceWindow.FocusableCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["TextOnlyVisibleOnHover"].Value =
            preferenceWindow.TextOnlyVisibleOnHoverCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["AnkiIntegration"].Value =
            preferenceWindow.AnkiIntegrationCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["HighlightColor"].Value =
            preferenceWindow.HighlightColorButton.Background.ToString();

        config.AppSettings.Settings[nameof(MaxNumResultsNotInMiningMode)].Value =
            preferenceWindow.MaxNumResultsNotInMiningModeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["PopupMaxWidth"].Value =
            preferenceWindow.PopupMaxWidthNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["PopupMaxHeight"].Value =
            preferenceWindow.PopupMaxHeightNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["FixedPopupPositioning"].Value =
            preferenceWindow.FixedPopupPositioningCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["FixedPopupXPosition"].Value =
            preferenceWindow.FixedPopupXPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["FixedPopupYPosition"].Value =
            preferenceWindow.FixedPopupYPositionNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["PopupDynamicHeight"].Value =
            preferenceWindow.PopupDynamicHeightCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["PopupDynamicWidth"].Value =
            preferenceWindow.PopupDynamicWidthCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["PopupBackgroundColor"].Value =
            preferenceWindow.PopupBackgroundColorButton.Background.ToString();
        config.AppSettings.Settings["PrimarySpellingColor"].Value =
            preferenceWindow.PrimarySpellingColorButton.Background.ToString();
        config.AppSettings.Settings["ReadingsColor"].Value =
            preferenceWindow.ReadingsColorButton.Background.ToString();
        config.AppSettings.Settings["AlternativeSpellingsColor"].Value =
            preferenceWindow.AlternativeSpellingsColorButton.Background.ToString();
        config.AppSettings.Settings["DefinitionsColor"].Value =
            preferenceWindow.DefinitionsColorButton.Background.ToString();
        config.AppSettings.Settings["FrequencyColor"].Value =
            preferenceWindow.FrequencyColorButton.Background.ToString();
        config.AppSettings.Settings["DeconjugationInfoColor"].Value =
            preferenceWindow.DeconjugationInfoColorButton.Background.ToString();
        config.AppSettings.Settings["PopupOpacity"].Value =
            preferenceWindow.PopupOpacityNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["PrimarySpellingFontSize"].Value =
            preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["ReadingsFontSize"].Value =
            preferenceWindow.ReadingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["AlternativeSpellingsFontSize"].Value =
            preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["DefinitionsFontSize"].Value =
            preferenceWindow.DefinitionsFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["FrequencyFontSize"].Value =
            preferenceWindow.FrequencyFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["DeconjugationInfoFontSize"].Value =
            preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["DictTypeFontSize"].Value =
            preferenceWindow.DictTypeFontSizeNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["SeparatorColor"].Value =
            preferenceWindow.SeparatorColorButton.Background.ToString();

        config.AppSettings.Settings["DictTypeColor"].Value =
            preferenceWindow.DictTypeColorButton.Background.ToString();

        config.AppSettings.Settings["PopupFocusOnLookup"].Value =
            preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked.ToString();
        config.AppSettings.Settings["PopupXOffset"].Value =
            preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["PopupYOffset"].Value =
            preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["PopupFlip"].Value =
            preferenceWindow.PopupFlipComboBox.SelectedValue.ToString();

        config.AppSettings.Settings["ShowMiningModeReminder"].Value =
            preferenceWindow.ShowMiningModeReminderCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["DisableLookupsForNonJapaneseCharsInPopups"].Value =
            preferenceWindow.DisableLookupsForNonJapaneseCharsInPopupsCheckBox.IsChecked.ToString();

        config.AppSettings.Settings["LookupMode"].Value =
            preferenceWindow.LookupModeComboBox.SelectedValue.ToString();

        MainWindow mainWindow = MainWindow.Instance;
        config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString(CultureInfo.InvariantCulture);

        config.Save(ConfigurationSaveMode.Modified);
        ConfigurationManager.RefreshSection("appSettings");

        ApplyPreferences();

        if (preferenceWindow.SetAnkiConfig)
        {
            await preferenceWindow.SaveMiningSetup().ConfigureAwait(false);
        }
    }

    public static void SaveBeforeClosing()
    {
        CreateDefaultAppConfig();

        MainWindow mainWindow = MainWindow.Instance;

        Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

        config.AppSettings.Settings["MainWindowFontSize"].Value =
            mainWindow.FontSizeSlider.Value.ToString(CultureInfo.InvariantCulture);
        config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["MainWindowHeight"].Value = MainWindowHeight > mainWindow.MinHeight
            ? MainWindowHeight.ToString(CultureInfo.InvariantCulture)
            : mainWindow.MinHeight.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["MainWindowWidth"].Value = MainWindowWidth > mainWindow.MinWidth
            ? MainWindowWidth.ToString(CultureInfo.InvariantCulture)
            : mainWindow.MinWidth.ToString(CultureInfo.InvariantCulture);

        config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top >= SystemParameters.VirtualScreenTop
            ? mainWindow.Top.ToString(CultureInfo.InvariantCulture)
            : "0";

        config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left >= SystemParameters.VirtualScreenLeft
            ? mainWindow.Left.ToString(CultureInfo.InvariantCulture)
            : "0";

        config.Save(ConfigurationSaveMode.Modified);
    }

    private static void CreateDefaultAppConfig()
    {
        string configPath = System.Reflection.Assembly.GetExecutingAssembly().Location + ".config";
        if (!File.Exists(configPath))
        {
            using (XmlWriter writer = XmlWriter.Create(configPath, new XmlWriterSettings { Indent = true }))
            {
                writer.WriteStartDocument();
                writer.WriteStartElement("configuration");
                writer.WriteStartElement("appSettings");
                writer.WriteEndDocument();
            }

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.Save(ConfigurationSaveMode.Full);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
