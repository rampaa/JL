using System.Configuration;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using JL.GUI;
using JL.Utilities;

namespace JL
{
    public static class ConfigManager
    {
        public static string AnkiConnectUri { get; set; } = "http://localhost:8765";
        public static int MaxSearchLength { get; set; } = 37;
        public static string FrequencyListName { get; set; } = "VN";

        public static double MainWindowHeight { get; set; } = 200;
        public static double MainWindowWidth { get; set; } = 800;

        public static bool HighlightLongestMatch { get; set; } = false;
        public static bool LookupOnSelectOnly { get; set; } = false;
        public static bool RequireLookupKeyPress { get; set; } = false;
        public static bool KanjiMode { get; set; } = false;
        public static bool InactiveLookupMode { get; set; } = false;
        public static bool ForceSyncAnki { get; set; } = false;
        public static bool AllowDuplicateCards { get; set; } = false;
        public static int LookupRate { get; set; } = 0;

        public static Brush MainWindowTextColor { get; set; } = Brushes.White;
        public static Brush MainWindowBacklogTextColor { get; set; } = Brushes.Bisque;
        public static Brush PrimarySpellingColor { get; set; } = Brushes.Chocolate;
        public static Brush ReadingsColor { get; set; } = Brushes.Goldenrod;
        public static Brush DefinitionsColor { get; set; } = Brushes.White;
        public static Brush DeconjugationInfoColor { get; set; } = Brushes.White;
        public static Brush FrequencyColor { get; set; } = Brushes.White;
        public static Brush AlternativeSpellingsColor { get; set; } = Brushes.White;
        public static Brush SeparatorColor { get; set; } = Brushes.White;
        public static Brush PopupBackgroundColor { get; set; } = Brushes.Black;
        public static Brush DictTypeColor { get; set; } = Brushes.LightBlue;
        public static Brush HighlightColor { get; set; } = Brushes.AliceBlue;

        public static KeyGesture MiningModeKeyGesture { get; set; } = new(Key.M, ModifierKeys.Windows);
        public static KeyGesture PlayAudioKeyGesture { get; set; } = new(Key.P, ModifierKeys.Windows);
        public static KeyGesture KanjiModeKeyGesture { get; set; } = new(Key.K, ModifierKeys.Windows);

        public static KeyGesture ShowManageDictionariesWindowKeyGesture { get; set; } =
            new(Key.D, ModifierKeys.Windows);

        public static KeyGesture ShowPreferencesWindowKeyGesture { get; set; } = new(Key.L, ModifierKeys.Windows);
        public static KeyGesture ShowAddNameWindowKeyGesture { get; set; } = new(Key.N, ModifierKeys.Windows);
        public static KeyGesture ShowAddWordWindowKeyGesture { get; set; } = new(Key.W, ModifierKeys.Windows);
        public static KeyGesture SearchWithBrowserKeyGesture { get; set; } = new(Key.S, ModifierKeys.Windows);
        public static KeyGesture MousePassThroughModeKeyGesture { get; set; } = new(Key.T, ModifierKeys.Windows);
        public static KeyGesture SteppedBacklogBackwardsKeyGesture { get; set; } = new(Key.Left, ModifierKeys.Windows);
        public static KeyGesture SteppedBacklogForwardsKeyGesture { get; set; } = new(Key.Right, ModifierKeys.Windows);
        public static KeyGesture InactiveLookupModeKeyGesture { get; set; } = new(Key.Q, ModifierKeys.Windows);
        public static KeyGesture MotivationKeyGesture { get; set; } = new(Key.O, ModifierKeys.Windows);
        public static KeyGesture ClosePopupKeyGesture { get; set; } = new(Key.Escape, ModifierKeys.Windows);
        public static ModifierKeys LookupKey { get; set; } = ModifierKeys.Alt;
        public static int PrimarySpellingFontSize { get; set; } = 21;
        public static int ReadingsFontSize { get; set; } = 19;
        public static int DefinitionsFontSize { get; set; } = 17;
        public static int DeconjugationInfoFontSize { get; set; } = 17;
        public static int FrequencyFontSize { get; set; } = 17;
        public static int AlternativeSpellingsFontSize { get; set; } = 17;
        public static int DictTypeFontSize { get; set; } = 15;

        public static int PopupMaxWidth { get; set; } = 700;
        public static int PopupMaxHeight { get; set; } = 520;
        public static bool PopupDynamicHeight { get; set; } = true;
        public static bool PopupDynamicWidth { get; set; } = true;
        public static bool PopupFocusOnLookup { get; set; } = false;
        public static int PopupXOffset { get; set; } = 10;
        public static int PopupYOffset { get; set; } = 20;
        public static bool PopupFlipX { get; set; } = true;
        public static bool PopupFlipY { get; set; } = true;
        public static FontFamily PopupFont { get; set; } = new("Meiryo");

        // consider making this dictionary specific
        public static bool NewlineBetweenDefinitions { get; set; } = false;
        public static bool CheckForJLUpdatesOnStartUp { get; set; } = true;

        public static void ApplyPreferences()
        {
            string tempStr = ConfigurationManager.AppSettings.Get("FrequencyListName");

            if (tempStr == null)
            {
                tempStr = "VN";
                Utils.AddToConfig("FrequencyListName", tempStr);
            }

            FrequencyListName = tempStr;

            tempStr = ConfigurationManager.AppSettings.Get("AnkiConnectUri");
            if (tempStr == null)
            {
                tempStr = "http://localhost:8765";
                Utils.AddToConfig("AnkiConnectUri", "http://localhost:8765");
            }

            AnkiConnectUri = tempStr;

            Utils.Try(
                () => HighlightLongestMatch =
                    bool.Parse(ConfigurationManager.AppSettings.Get("HighlightLongestMatch")!),
                HighlightLongestMatch, "HighlightLongestMatch");

            Utils.Try(() => CheckForJLUpdatesOnStartUp =
                    bool.Parse(ConfigurationManager.AppSettings.Get("CheckForJLUpdatesOnStartUp")!),
                CheckForJLUpdatesOnStartUp, "CheckForJLUpdatesOnStartUp");

            Utils.Try(() => MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength")!),
                MaxSearchLength, "MaxSearchLength");
            Utils.Try(() => KanjiMode = bool.Parse(ConfigurationManager.AppSettings.Get("KanjiMode")!), KanjiMode,
                "KanjiMode");
            Utils.Try(() => ForceSyncAnki = bool.Parse(ConfigurationManager.AppSettings.Get("ForceSyncAnki")!),
                ForceSyncAnki, "ForceSyncAnki");
            Utils.Try(
                () => AllowDuplicateCards = bool.Parse(ConfigurationManager.AppSettings.Get("AllowDuplicateCards")!),
                AllowDuplicateCards, "AllowDuplicateCards");
            Utils.Try(() => LookupRate = int.Parse(ConfigurationManager.AppSettings.Get("LookupRate")!), LookupRate,
                "LookupRate");

            Utils.Try(() => LookupKey = (ModifierKeys)new ModifierKeysConverter()
                    .ConvertFromString(ConfigurationManager.AppSettings.Get("LookupKey")!)!,
                LookupKey, "LookupKey");

            // MAKE SURE YOU FREEZE ANY NEW COLOR OBJECTS YOU ADD
            // OR THE PROGRAM WILL CRASH AND BURN
            Utils.Try(() =>
                    MainWindowTextColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowTextColor")),
                MainWindowTextColor, "MainWindowTextColor");
            MainWindowTextColor.Freeze();

            Utils.Try(() =>
                    MainWindowBacklogTextColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBacklogTextColor")),
                MainWindowBacklogTextColor, "MainWindowBacklogTextColor");
            MainWindowBacklogTextColor.Freeze();

            Utils.Try(() =>
                    PrimarySpellingColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("PrimarySpellingColor")),
                PrimarySpellingColor, "PrimarySpellingColor");
            PrimarySpellingColor.Freeze();

            Utils.Try(() =>
                    ReadingsColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("ReadingsColor")),
                ReadingsColor, "ReadingsColor");
            ReadingsColor.Freeze();

            Utils.Try(() =>
                    AlternativeSpellingsColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("AlternativeSpellingsColor")),
                AlternativeSpellingsColor, "AlternativeSpellingsColor");
            AlternativeSpellingsColor.Freeze();

            Utils.Try(() =>
                    DefinitionsColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("DefinitionsColor")),
                DefinitionsColor, "DefinitionsColor");
            DefinitionsColor.Freeze();

            Utils.Try(() =>
                    FrequencyColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("FrequencyColor")),
                FrequencyColor, "FrequencyColor");
            FrequencyColor.Freeze();

            Utils.Try(() =>
                    DeconjugationInfoColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("DeconjugationInfoColor")),
                DeconjugationInfoColor, "DeconjugationInfoColor");
            DeconjugationInfoColor.Freeze();

            Utils.Try(() =>
                    SeparatorColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("SeparatorColor")),
                SeparatorColor, "SeparatorColor");
            SeparatorColor!.Freeze();

            Utils.Try(() =>
                    DictTypeColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("DictTypeColor")),
                DictTypeColor, "DictTypeColor");
            DictTypeColor!.Freeze();

            Utils.Try(() =>
                    HighlightColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("HighlightColor")),
                HighlightColor, "HighlightColor");
            HighlightColor.Freeze();
            MainWindow.Instance.MainTextBox.SelectionBrush = HighlightColor;

            Utils.Try(() =>
                PopupBackgroundColor = (SolidColorBrush)new BrushConverter()
                    .ConvertFrom(ConfigurationManager.AppSettings
                        .Get("PopupBackgroundColor")), PopupBackgroundColor, "PopupBackgroundColor");
            Utils.Try(() => PopupBackgroundColor.Opacity = double.Parse(ConfigurationManager.AppSettings
                .Get("PopupOpacity")!) / 100, 70, "PopupOpacity");
            PopupBackgroundColor.Freeze();

            Utils.Try(() => PrimarySpellingFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("PrimarySpellingFontSize")!), PrimarySpellingFontSize, "PrimarySpellingFontSize");
            Utils.Try(() => ReadingsFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("ReadingsFontSize")!), ReadingsFontSize, "ReadingsFontSize");
            Utils.Try(() => AlternativeSpellingsFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("AlternativeSpellingsFontSize")!), AlternativeSpellingsFontSize, "AlternativeSpellingsFontSize");
            Utils.Try(() => DefinitionsFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DefinitionsFontSize")!), DefinitionsFontSize, "DefinitionsFontSize");
            Utils.Try(() => FrequencyFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("FrequencyFontSize")!), FrequencyFontSize, "FrequencyFontSize");
            Utils.Try(() => DeconjugationInfoFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DeconjugationInfoFontSize")!), DeconjugationInfoFontSize, "DeconjugationInfoFontSize");
            Utils.Try(() => DictTypeFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DictTypeFontSize")!), DictTypeFontSize, "DictTypeFontSize");

            Utils.Try(() => PopupFocusOnLookup = bool.Parse(ConfigurationManager.AppSettings
                .Get("PopupFocusOnLookup")!), PopupFocusOnLookup, "PopupFocusOnLookup");
            Utils.Try(() => PopupXOffset = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupXOffset")!), PopupXOffset, "PopupXOffset");
            Utils.Try(() => PopupYOffset = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupYOffset")!), PopupYOffset, "PopupYOffset");

            Utils.DpiAwareXOffset = PopupXOffset / Utils.Dpi.DpiScaleX;
            Utils.DpiAwareYOffset = PopupYOffset / Utils.Dpi.DpiScaleY;

            Utils.Try(() => PopupMaxWidth = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupMaxWidth")!), PopupMaxWidth, "PopupMaxWidth");
            Utils.Try(() => PopupMaxHeight = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupMaxHeight")!), PopupMaxHeight, "PopupMaxHeight");

            tempStr = ConfigurationManager.AppSettings.Get("PopupFlip");

            if (tempStr == null)
            {
                Utils.AddToConfig("PopupFlip", "Both");
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
                Utils.AddToConfig("LookupMode", "1");
            }

            switch (ConfigurationManager.AppSettings.Get("LookupMode"))
            {
                case "1":
                    RequireLookupKeyPress = false;
                    LookupOnSelectOnly = false;
                    break;

                case "2":
                    RequireLookupKeyPress = true;
                    LookupOnSelectOnly = false;
                    break;

                case "3":
                    RequireLookupKeyPress = false;
                    LookupOnSelectOnly = true;
                    break;

                default:
                    RequireLookupKeyPress = false;
                    LookupOnSelectOnly = false;
                    break;
            }

            MiningModeKeyGesture = Utils.KeyGestureSetter("MiningModeKeyGesture", MiningModeKeyGesture);
            PlayAudioKeyGesture = Utils.KeyGestureSetter("PlayAudioKeyGesture", PlayAudioKeyGesture);
            KanjiModeKeyGesture = Utils.KeyGestureSetter("KanjiModeKeyGesture", KanjiModeKeyGesture);

            ShowManageDictionariesWindowKeyGesture =
                Utils.KeyGestureSetter("ShowManageDictionariesWindowKeyGesture",
                    ShowManageDictionariesWindowKeyGesture);

            ShowPreferencesWindowKeyGesture =
                Utils.KeyGestureSetter("ShowPreferencesWindowKeyGesture", ShowPreferencesWindowKeyGesture);
            ShowAddNameWindowKeyGesture =
                Utils.KeyGestureSetter("ShowAddNameWindowKeyGesture", ShowAddNameWindowKeyGesture);
            ShowAddWordWindowKeyGesture =
                Utils.KeyGestureSetter("ShowAddWordWindowKeyGesture", ShowAddWordWindowKeyGesture);
            SearchWithBrowserKeyGesture =
                Utils.KeyGestureSetter("SearchWithBrowserKeyGesture", SearchWithBrowserKeyGesture);
            MousePassThroughModeKeyGesture =
                Utils.KeyGestureSetter("MousePassThroughModeKeyGesture", MousePassThroughModeKeyGesture);
            SteppedBacklogBackwardsKeyGesture = Utils.KeyGestureSetter("SteppedBacklogBackwardsKeyGesture",
                SteppedBacklogBackwardsKeyGesture);
            SteppedBacklogForwardsKeyGesture =
                Utils.KeyGestureSetter("SteppedBacklogForwardsKeyGesture", SteppedBacklogForwardsKeyGesture);
            InactiveLookupModeKeyGesture =
                Utils.KeyGestureSetter("InactiveLookupModeKeyGesture", InactiveLookupModeKeyGesture);
            MotivationKeyGesture =
                Utils.KeyGestureSetter("MotivationKeyGesture", MotivationKeyGesture);

            ClosePopupKeyGesture = Utils.KeyGestureSetter("ClosePopupKeyGesture", ClosePopupKeyGesture);

            if (Utils.KeyGestureToString(ShowAddNameWindowKeyGesture) == "None")
                MainWindow.Instance.AddNameButton.InputGestureText = "";
            else
                MainWindow.Instance.AddNameButton.InputGestureText =
                    Utils.KeyGestureToString(ShowAddNameWindowKeyGesture);

            if (Utils.KeyGestureToString(ShowAddWordWindowKeyGesture) == "None")
                MainWindow.Instance.AddWordButton.InputGestureText = "";
            else
                MainWindow.Instance.AddWordButton.InputGestureText =
                    Utils.KeyGestureToString(ShowAddWordWindowKeyGesture);

            if (Utils.KeyGestureToString(SearchWithBrowserKeyGesture) == "None")
                MainWindow.Instance.SearchButton.InputGestureText = "";
            else
                MainWindow.Instance.SearchButton.InputGestureText =
                    Utils.KeyGestureToString(SearchWithBrowserKeyGesture);

            if (Utils.KeyGestureToString(ShowPreferencesWindowKeyGesture) == "None")
                MainWindow.Instance.PreferencesButton.InputGestureText = "";
            else
                MainWindow.Instance.PreferencesButton.InputGestureText =
                    Utils.KeyGestureToString(ShowPreferencesWindowKeyGesture);

            if (Utils.KeyGestureToString(ShowManageDictionariesWindowKeyGesture) == "None")
                MainWindow.Instance.ManageDictionariesButton.InputGestureText = "";
            else
                MainWindow.Instance.ManageDictionariesButton.InputGestureText =
                    Utils.KeyGestureToString(ShowManageDictionariesWindowKeyGesture);

            Utils.Try(() => MainWindow.Instance.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowOpacity")!), MainWindow.Instance.OpacitySlider.Value, "MainWindowOpacity");
            Utils.Try(() => MainWindow.Instance.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowFontSize")!), MainWindow.Instance.FontSizeSlider.Value, "MainWindowFontSize");

            tempStr = ConfigurationManager.AppSettings.Get("MainWindowFont");

            if (tempStr == null)
            {
                Utils.AddToConfig("MainWindowFont", "Meiryo");
                tempStr = "Meiryo";
            }

            MainWindow.Instance.MainTextBox.FontFamily = new FontFamily(tempStr);

            Utils.Try(() =>
                    MainWindow.Instance.Background = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")),
                MainWindow.Instance.Background, "MainWindowBackgroundColor");
            MainWindow.Instance.Background.Opacity = MainWindow.Instance.OpacitySlider.Value / 100;

            MainWindow.Instance.MainTextBox.Foreground = MainWindowTextColor;

            Utils.Try(() => MainWindowHeight = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowHeight")!), MainWindowHeight, "MainWindowHeight");
            MainWindow.Instance.Height = MainWindowHeight;

            Utils.Try(() => MainWindowWidth = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowWidth")!), MainWindowWidth, "MainWindowWidth");
            MainWindow.Instance.Width = MainWindowWidth;

            Utils.Try(() => MainWindow.Instance.Top = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowTopPosition")!), MainWindow.Instance.Top, "MainWindowTopPosition");

            Utils.Try(() => MainWindow.Instance.Left = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowLeftPosition")!), MainWindow.Instance.Left, "MainWindowLeftPosition");

            tempStr = ConfigurationManager.AppSettings.Get("PopupFont");

            if (tempStr == null)
                Utils.AddToConfig("PopupFont", PopupFont.Source);
            else
                PopupFont = new FontFamily(tempStr);

            Utils.Try(() => PopupDynamicHeight = bool.Parse(ConfigurationManager.AppSettings
                .Get("PopupDynamicHeight")!), PopupDynamicHeight, "PopupDynamicHeight");
            Utils.Try(() => PopupDynamicWidth = bool.Parse(ConfigurationManager.AppSettings
                .Get("PopupDynamicWidth")!), PopupDynamicWidth, "PopupDynamicWidth");

            foreach (PopupWindow popupWindow in Application.Current.Windows.OfType<PopupWindow>().ToList())
            {
                popupWindow.Background = PopupBackgroundColor;
                popupWindow.MaxHeight = PopupMaxHeight;
                popupWindow.MaxWidth = PopupMaxWidth;
                popupWindow.FontFamily = PopupFont;

                if (PopupDynamicWidth && PopupDynamicHeight)
                    popupWindow.SizeToContent = SizeToContent.WidthAndHeight;

                else if (PopupDynamicWidth)
                    popupWindow.SizeToContent = SizeToContent.Width;

                else if (PopupDynamicHeight)
                    popupWindow.SizeToContent = SizeToContent.Height;

                else
                    popupWindow.SizeToContent = SizeToContent.Manual;
            }

            Storage.LoadFrequency().ConfigureAwait(false);
        }

        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            CreateDefaultAppConfig();

            preferenceWindow.VersionTextBlock.Text = "v" + Storage.Version.ToString();

            preferenceWindow.MiningModeKeyGestureTextBox.Text = Utils.KeyGestureToString(MiningModeKeyGesture);
            preferenceWindow.PlayAudioKeyGestureTextBox.Text = Utils.KeyGestureToString(PlayAudioKeyGesture);
            preferenceWindow.KanjiModeKeyGestureTextBox.Text = Utils.KeyGestureToString(KanjiModeKeyGesture);

            preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text =
                Utils.KeyGestureToString(ShowManageDictionariesWindowKeyGesture);
            preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text =
                Utils.KeyGestureToString(ShowPreferencesWindowKeyGesture);
            preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text =
                Utils.KeyGestureToString(ShowAddNameWindowKeyGesture);
            preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text =
                Utils.KeyGestureToString(ShowAddWordWindowKeyGesture);
            preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text =
                Utils.KeyGestureToString(SearchWithBrowserKeyGesture);
            preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text =
                Utils.KeyGestureToString(MousePassThroughModeKeyGesture);
            preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text =
                Utils.KeyGestureToString(SteppedBacklogBackwardsKeyGesture);
            preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text =
                Utils.KeyGestureToString(SteppedBacklogForwardsKeyGesture);
            preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text =
                Utils.KeyGestureToString(InactiveLookupModeKeyGesture);
            preferenceWindow.MotivationKeyGestureTextBox.Text =
                Utils.KeyGestureToString(MotivationKeyGesture);
            preferenceWindow.ClosePopupKeyGestureTextBox.Text =
                Utils.KeyGestureToString(ClosePopupKeyGesture);

            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = ForceSyncAnki;
            preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked = AllowDuplicateCards;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;
            preferenceWindow.KanjiModeCheckBox.IsChecked = KanjiMode;
            preferenceWindow.HighlightLongestMatchCheckBox.IsChecked = HighlightLongestMatch;
            preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked = CheckForJLUpdatesOnStartUp;
            preferenceWindow.FrequencyListComboBox.ItemsSource = Storage.FrequencyLists.Keys;
            preferenceWindow.FrequencyListComboBox.SelectedItem = FrequencyListName;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;

            preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
            preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;

            preferenceWindow.HighlightColorButton.Background = HighlightColor;

            Utils.Try(() => preferenceWindow.TextboxBackgroundColorButton.Background =
                    (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")),
                preferenceWindow.TextboxBackgroundColorButton.Background, "MainWindowBackgroundColor");

            preferenceWindow.TextboxTextColorButton.Background = MainWindowTextColor;
            preferenceWindow.TextboxBacklogTextColorButton.Background = MainWindowBacklogTextColor;
            preferenceWindow.TextboxFontSizeNumericUpDown.Value = MainWindow.Instance.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = MainWindow.Instance.OpacitySlider.Value;

            preferenceWindow.MainWindowFontComboBox.ItemsSource = UiControls.JapaneseFonts;
            preferenceWindow.MainWindowFontComboBox.SelectedIndex = UiControls.JapaneseFonts.FindIndex(f =>
                f.Content.ToString() == MainWindow.Instance.MainTextBox.FontFamily.Source);

            preferenceWindow.PopupFontComboBox.ItemsSource = UiControls.PopupJapaneseFonts;
            preferenceWindow.PopupFontComboBox.SelectedIndex =
                UiControls.PopupJapaneseFonts.FindIndex(f => f.Content.ToString() == PopupFont.Source);

            preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
            preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
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
            Utils.Try(() => preferenceWindow.PopupBackgroundColorButton.Background =
                    (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor")),
                preferenceWindow.PopupBackgroundColorButton.Background, "PopupBackgroundColor");

            Utils.Try(() => preferenceWindow.PopupOpacityNumericUpDown.Value = int.Parse(
                    ConfigurationManager.AppSettings.Get("PopupOpacity")!),
                preferenceWindow.PopupOpacityNumericUpDown.Value, "PopupOpacity");

            preferenceWindow.SeparatorColorButton.Background = SeparatorColor;
            preferenceWindow.DictTypeColorButton.Background = DictTypeColor;

            preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked = PopupFocusOnLookup;
            preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
            preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;
            preferenceWindow.PopupFlipComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("PopupFlip");

            preferenceWindow.LookupModeComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("LookupMode");
            preferenceWindow.LookupKeyComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("LookupKey");
        }

        public static void SavePreferences(PreferencesWindow preferenceWindow)
        {
            Utils.KeyGestureSaver("MiningModeKeyGesture", preferenceWindow.MiningModeKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("PlayAudioKeyGesture", preferenceWindow.PlayAudioKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("KanjiModeKeyGesture", preferenceWindow.KanjiModeKeyGestureTextBox.Text);

            Utils.KeyGestureSaver("ShowManageDictionariesWindowKeyGesture",
                preferenceWindow.ShowManageDictionariesWindowKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("ShowPreferencesWindowKeyGesture",
                preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("ShowAddNameWindowKeyGesture",
                preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("ShowAddWordWindowKeyGesture",
                preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("SearchWithBrowserKeyGesture",
                preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("MousePassThroughModeKeyGesture",
                preferenceWindow.MousePassThroughModeKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("SteppedBacklogBackwardsKeyGesture",
                preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("SteppedBacklogForwardsKeyGesture",
                preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("InactiveLookupModeKeyGesture",
                preferenceWindow.InactiveLookupModeKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("MotivationKeyGesture",
                preferenceWindow.MotivationKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("ClosePopupKeyGesture",
                preferenceWindow.ClosePopupKeyGestureTextBox.Text);

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["MaxSearchLength"].Value =
                preferenceWindow.MaxSearchLengthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["AnkiConnectUri"].Value =
                preferenceWindow.AnkiUriTextBox.Text;

            config.AppSettings.Settings["MainWindowWidth"].Value =
                preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value =
                preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowBackgroundColor"].Value =
                preferenceWindow.TextboxBackgroundColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowTextColor"].Value =
                preferenceWindow.TextboxTextColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowBacklogTextColor"].Value =
                preferenceWindow.TextboxBacklogTextColorButton.Background.ToString();
            config.AppSettings.Settings["MainWindowFontSize"].Value =
                preferenceWindow.TextboxFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value =
                preferenceWindow.TextboxOpacityNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowFont"].Value =
                preferenceWindow.MainWindowFontComboBox.SelectedValue.ToString();
            config.AppSettings.Settings["PopupFont"].Value =
                preferenceWindow.PopupFontComboBox.SelectedValue.ToString();
            config.AppSettings.Settings["FrequencyListName"].Value =
                preferenceWindow.FrequencyListComboBox.SelectedValue.ToString();

            config.AppSettings.Settings["KanjiMode"].Value =
                preferenceWindow.KanjiModeCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["ForceSyncAnki"].Value =
                preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["AllowDuplicateCards"].Value =
                preferenceWindow.AllowDuplicateCardsCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["LookupRate"].Value =
                preferenceWindow.LookupRateNumericUpDown.Value.ToString();
            config.AppSettings.Settings["HighlightLongestMatch"].Value =
                preferenceWindow.HighlightLongestMatchCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["CheckForJLUpdatesOnStartUp"].Value =
                preferenceWindow.CheckForJLUpdatesOnStartUpCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["HighlightColor"].Value =
                preferenceWindow.HighlightColorButton.Background.ToString();

            config.AppSettings.Settings["PopupMaxWidth"].Value =
                preferenceWindow.PopupMaxWidthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupMaxHeight"].Value =
                preferenceWindow.PopupMaxHeightNumericUpDown.Value.ToString();
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
                preferenceWindow.PopupOpacityNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PrimarySpellingFontSize"].Value =
                preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["ReadingsFontSize"].Value =
                preferenceWindow.ReadingsFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["AlternativeSpellingsFontSize"].Value =
                preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["DefinitionsFontSize"].Value =
                preferenceWindow.DefinitionsFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["FrequencyFontSize"].Value =
                preferenceWindow.FrequencyFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["DeconjugationInfoFontSize"].Value =
                preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["DictTypeFontSize"].Value =
                preferenceWindow.DictTypeFontSizeNumericUpDown.Value.ToString();

            config.AppSettings.Settings["SeparatorColor"].Value =
                preferenceWindow.SeparatorColorButton.Background.ToString();

            config.AppSettings.Settings["DictTypeColor"].Value =
                preferenceWindow.DictTypeColorButton.Background.ToString();

            config.AppSettings.Settings["PopupFocusOnLookup"].Value =
                preferenceWindow.PopupFocusOnLookupCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["PopupXOffset"].Value =
                preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupYOffset"].Value =
                preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupFlip"].Value =
                preferenceWindow.PopupFlipComboBox.SelectedValue.ToString();

            config.AppSettings.Settings["LookupMode"].Value =
                preferenceWindow.LookupModeComboBox.SelectedValue.ToString();

            config.AppSettings.Settings["LookupKey"].Value =
                preferenceWindow.LookupKeyComboBox.SelectedValue.ToString();

            config.AppSettings.Settings["MainWindowHeight"].Value =
                preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value =
                preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString();

            config.AppSettings.Settings["MainWindowTopPosition"].Value = MainWindow.Instance.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = MainWindow.Instance.Left.ToString();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            ApplyPreferences();
        }

        public static void SaveBeforeClosing()
        {
            CreateDefaultAppConfig();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["MainWindowFontSize"].Value =
                MainWindow.Instance.FontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = MainWindow.Instance.OpacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = MainWindowHeight.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = MainWindowWidth.ToString();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = MainWindow.Instance.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = MainWindow.Instance.Left.ToString();

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
}
