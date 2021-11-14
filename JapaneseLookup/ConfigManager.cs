using JapaneseLookup.Abstract;
using JapaneseLookup.CustomDict;
using JapaneseLookup.Dicts;
using JapaneseLookup.EDICT.JMdict;
using JapaneseLookup.EDICT.JMnedict;
using JapaneseLookup.EPWING;
using JapaneseLookup.Frequency;
using JapaneseLookup.GUI;
using JapaneseLookup.KANJIDIC;
using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;

namespace JapaneseLookup
{
    public static class ConfigManager
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();

        public static readonly HttpClient Client = new();

        private static readonly List<string> JapaneseFonts =
            Utils.FindJapaneseFonts().OrderBy(font => font).ToList();

        public static readonly Dictionary<string, Dict> BuiltInDicts =
            new()
            {
                { "JMdict", new Dict(DictType.JMdict, "Resources\\JMdict.xml", true, 0) },
                { "JMnedict", new Dict(DictType.JMnedict, "Resources\\JMnedict.xml", true, 1) },
                { "Kanjidic", new Dict(DictType.Kanjidic, "Resources\\kanjidic2.xml", true, 2) },
                {
                    "CustomWordDictionary",
                    new Dict(DictType.CustomWordDictionary, "Resources\\custom_words.txt", true, 3)
                },
                {
                    "CustomNameDictionary",
                    new Dict(DictType.CustomNameDictionary, "Resources\\custom_names.txt", true, 4)
                }
            };

        private static readonly Dictionary<string, string> FrequencyLists = new()
        {
            { "VN", "Resources/freqlist_vns.json" },
            { "Novel", "Resources/freqlist_novels.json" },
            { "Narou", "Resources/freqlist_narou.json" },
            { "None", "" }
        };

        public static readonly Dictionary<DictType, Dict> Dicts = new();

        public static string AnkiConnectUri { get; set; } = "http://localhost:8765";
        public static int MaxSearchLength { get; set; } = 37;
        public static string FrequencyListName { get; set; } = "VN";

        public static double MainWindowHeight { get; set; } = 200;
        public static double MainWindowWidth { get; set; } = 800;

        public static bool KanjiMode { get; set; } = false;
        public static bool InactiveLookupMode { get; set; } = false;
        public static bool ForceSyncAnki { get; set; } = false;
        public static int LookupRate { get; set; } = 0;

        public static bool PopupDynamicHeight { get; set; } = true;
        public static bool PopupDynamicWidth { get; set; } = false;

        public static Brush MainWindowTextColor { get; set; } = Brushes.White;
        public static Brush MainWindowBacklogTextColor { get; set; } = Brushes.Bisque;
        public static Brush PrimarySpellingColor { get; set; } = Brushes.Chocolate;
        public static Brush ReadingsColor { get; set; } = Brushes.Goldenrod;
        public static Brush ROrthographyInfoColor { get; set; } = Brushes.Goldenrod;
        public static Brush DefinitionsColor { get; set; } = Brushes.White;
        public static Brush DeconjugationInfoColor { get; set; } = Brushes.White;
        public static Brush FrequencyColor { get; set; } = Brushes.White;
        public static Brush AlternativeSpellingsColor { get; set; } = Brushes.White;
        public static Brush AOrthographyInfoColor { get; set; } = Brushes.White;
        public static Brush SeparatorColor { get; set; } = Brushes.White;
        public static Brush PopupBackgroundColor { get; set; } = Brushes.Black;
        public static Brush POrthographyInfoColor { get; set; } = Brushes.White;
        public static Brush DictTypeColor { get; set; } = Brushes.LightBlue;

        public static KeyGesture MiningModeKeyGesture { get; set; } = new(Key.M, ModifierKeys.Windows);
        public static KeyGesture PlayAudioKeyGesture { get; set; } = new(Key.P, ModifierKeys.Windows);
        public static KeyGesture KanjiModeKeyGesture { get; set; } = new(Key.K, ModifierKeys.Windows);
        public static KeyGesture ShowPreferencesWindowKeyGesture { get; set; } = new(Key.L, ModifierKeys.Windows);
        public static KeyGesture ShowAddNameWindowKeyGesture { get; set; } = new(Key.N, ModifierKeys.Windows);
        public static KeyGesture ShowAddWordWindowKeyGesture { get; set; } = new(Key.W, ModifierKeys.Windows);
        public static KeyGesture SearchWithBrowserKeyGesture { get; set; } = new(Key.S, ModifierKeys.Windows);
        public static KeyGesture MousePassThroughModeKeyGesture { get; set; } = new(Key.T, ModifierKeys.Windows);
        public static KeyGesture SteppedBacklogBackwardsKeyGesture { get; set; } = new(Key.Left, ModifierKeys.Windows);
        public static KeyGesture SteppedBacklogForwardsKeyGesture { get; set; } = new(Key.Right, ModifierKeys.Windows);
        public static KeyGesture InactiveLookupModeKeyGesture { get; set; } = new(Key.Q, ModifierKeys.Windows);

        public static int PrimarySpellingFontSize { get; set; } = 21;
        public static int ReadingsFontSize { get; set; } = 19;
        public static int ROrthographyInfoFontSize { get; set; } = 17;
        public static int DefinitionsFontSize { get; set; } = 17;
        public static int DeconjugationInfoFontSize { get; set; } = 17;
        public static int FrequencyFontSize { get; set; } = 17;
        public static int AlternativeSpellingsFontSize { get; set; } = 17;
        public static int AOrthographyInfoFontSize { get; set; } = 17;
        public static int POrthographyInfoFontSize { get; set; } = 17;
        public static int DictTypeFontSize { get; set; } = 15;

        public static int PopupMaxWidth { get; set; } = 700;
        public static int PopupMaxHeight { get; set; } = 520;
        public static int PopupXOffset { get; set; } = 10;
        public static int PopupYOffset { get; set; } = 20;
        public static bool PopupFlipX { get; set; } = false;
        public static bool PopupFlipY { get; set; } = true;
        public static FontFamily PopupFont { get; set; } = new("Meiryo");

        // consider making this dictionary specific
        // enabling this seems to improve rendering performance by a lot; need to test if it's because
        // a) there's less text on the screen overall
        // b) there's less word-wrapping to do
        public static bool NewlineBetweenDefinitions { get; set; } = false;
        public static int MaxResults { get; set; } = 99;
        public static bool AllowDuplicateCards { get; set; } = false;

        public static async Task ApplyPreferences(MainWindow mainWindow)
        {
            CreateDefaultAppConfig();

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

            Utils.Try(() => MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength")!),
                MaxSearchLength, "MaxSearchLength");
            Utils.Try(() => KanjiMode = bool.Parse(ConfigurationManager.AppSettings.Get("KanjiMode")!), KanjiMode,
                "KanjiMode");
            Utils.Try(() => ForceSyncAnki = bool.Parse(ConfigurationManager.AppSettings.Get("ForceSyncAnki")!),
                ForceSyncAnki, "ForceSyncAnki");
            Utils.Try(() => LookupRate = int.Parse(ConfigurationManager.AppSettings.Get("LookupRate")!), LookupRate,
                "LookupRate");

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
                    ROrthographyInfoColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("ROrthographyInfoColor")),
                ROrthographyInfoColor, "ROrthographyInfoColor");
            ROrthographyInfoColor.Freeze();

            Utils.Try(() =>
                    POrthographyInfoColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("POrthographyInfoColor")),
                POrthographyInfoColor, "POrthographyInfoColor");
            POrthographyInfoColor.Freeze();

            Utils.Try(() =>
                    AlternativeSpellingsColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("AlternativeSpellingsColor")),
                AlternativeSpellingsColor, "AlternativeSpellingsColor");
            AlternativeSpellingsColor.Freeze();

            Utils.Try(() =>
                    AOrthographyInfoColor = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("AOrthographyInfoColor")),
                AOrthographyInfoColor, "AOrthographyInfoColor");
            AOrthographyInfoColor.Freeze();

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
            Utils.Try(() => ROrthographyInfoFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("ROrthographyInfoFontSize")!), ROrthographyInfoFontSize, "ROrthographyInfoFontSize");
            Utils.Try(() => POrthographyInfoFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("POrthographyInfoFontSize")!), POrthographyInfoFontSize, "POrthographyInfoFontSize");
            Utils.Try(() => AlternativeSpellingsFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("AlternativeSpellingsFontSize")!), AlternativeSpellingsFontSize, "AlternativeSpellingsFontSize");
            Utils.Try(() => AOrthographyInfoFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("AOrthographyInfoFontSize")!), AOrthographyInfoFontSize, "AOrthographyInfoFontSize");
            Utils.Try(() => DefinitionsFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DefinitionsFontSize")!), DefinitionsFontSize, "DefinitionsFontSize");
            Utils.Try(() => FrequencyFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("FrequencyFontSize")!), FrequencyFontSize, "FrequencyFontSize");
            Utils.Try(() => DeconjugationInfoFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DeconjugationInfoFontSize")!), DeconjugationInfoFontSize, "DeconjugationInfoFontSize");
            Utils.Try(() => DictTypeFontSize = int.Parse(ConfigurationManager.AppSettings
                .Get("DictTypeFontSize")!), DictTypeFontSize, "DictTypeFontSize");
            Utils.Try(() => PopupXOffset = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupXOffset")!), PopupXOffset, "PopupXOffset");
            Utils.Try(() => PopupYOffset = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupYOffset")!), PopupYOffset, "PopupYOffset");
            Utils.Try(() => PopupMaxWidth = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupMaxWidth")!), PopupMaxWidth, "PopupMaxWidth");
            Utils.Try(() => PopupMaxHeight = int.Parse(ConfigurationManager.AppSettings
                .Get("PopupMaxHeight")!), PopupMaxHeight, "PopupMaxHeight");

            tempStr = ConfigurationManager.AppSettings.Get("PopupFlip");

            if (tempStr == null)
            {
                tempStr = "Y";

                Utils.AddToConfig("PopupFlip", tempStr);

                if (ConfigurationManager.AppSettings.Get("PopupFlipX") == null)
                    Utils.AddToConfig("PopupFlipX", "False");
                if (ConfigurationManager.AppSettings.Get("PopupFlipY") == null)
                    Utils.AddToConfig("PopupFlipY", "True");
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

            MiningModeKeyGesture = Utils.KeyGestureSetter("MiningModeKeyGesture", MiningModeKeyGesture);
            PlayAudioKeyGesture = Utils.KeyGestureSetter("PlayAudioKeyGesture", PlayAudioKeyGesture);
            KanjiModeKeyGesture = Utils.KeyGestureSetter("KanjiModeKeyGesture", KanjiModeKeyGesture);
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

            Utils.Try(() => mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowOpacity")!), mainWindow.OpacitySlider.Value, "MainWindowOpacity");
            Utils.Try(() => mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowFontSize")!), mainWindow.FontSizeSlider.Value, "MainWindowFontSize");

            tempStr = ConfigurationManager.AppSettings.Get("MainWindowFont");

            if (tempStr == null)
            {
                Utils.AddToConfig("MainWindowFont", "Meiryo");
                tempStr = "Meiryo";
            }

            mainWindow.MainTextBox.FontFamily = new FontFamily(tempStr);

            Utils.Try(() =>
                    mainWindow.Background = (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")),
                mainWindow.Background, "MainWindowBackgroundColor");
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value / 100;

            mainWindow.MainTextBox.Foreground = MainWindowTextColor;

            Utils.Try(() => MainWindowHeight = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowHeight")!), MainWindowHeight, "MainWindowHeight");
            mainWindow.Height = MainWindowHeight;

            Utils.Try(() => MainWindowWidth = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowWidth")!), MainWindowWidth, "MainWindowWidth");
            mainWindow.Width = MainWindowWidth;

            Utils.Try(() => mainWindow.Top = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowTopPosition")!), mainWindow.Top, "MainWindowTopPosition");

            Utils.Try(() => mainWindow.Left = double.Parse(ConfigurationManager.AppSettings
                .Get("MainWindowLeftPosition")!), mainWindow.Left, "MainWindowLeftPosition");

            var firstPopupWindow = MainWindow.FirstPopupWindow;
            firstPopupWindow.Background = PopupBackgroundColor;

            tempStr = ConfigurationManager.AppSettings.Get("PopupFont");

            if (tempStr == null)
                Utils.AddToConfig("PopupFont", PopupFont.Source);
            else
                PopupFont = new FontFamily(tempStr);

            firstPopupWindow.FontFamily = PopupFont;

            Utils.Try(() => PopupDynamicHeight = bool.Parse(ConfigurationManager.AppSettings
                .Get("PopupDynamicHeight")!), PopupDynamicHeight, "PopupDynamicHeight");
            Utils.Try(() => PopupDynamicWidth = bool.Parse(ConfigurationManager.AppSettings
                .Get("PopupDynamicWidth")!), PopupDynamicWidth, "PopupDynamicWidth");

            if (PopupDynamicWidth && PopupDynamicHeight)
                firstPopupWindow.SizeToContent = SizeToContent.WidthAndHeight;

            else if (PopupDynamicWidth)
                firstPopupWindow.SizeToContent = SizeToContent.Width;

            else if (PopupDynamicHeight)
                firstPopupWindow.SizeToContent = SizeToContent.Height;

            else
                firstPopupWindow.SizeToContent = SizeToContent.Manual;

            if (!File.Exists(Path.Join(ApplicationPath, "Config/dicts.json")))
                CreateDefaultDictsConfig();

            if (!File.Exists("Resources/custom_words.txt"))
                File.Create("Resources/custom_words.txt");

            if (!File.Exists("Resources/custom_names.txt"))
                File.Create("Resources/custom_names.txt");

            await DeserializeDicts().ConfigureAwait(false);

            // Test without async/await.
            // Task.Run(async () => { await LoadDictionaries(); });
            await LoadDictionaries().ConfigureAwait(false);
        }

        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            CreateDefaultAppConfig();
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();

            preferenceWindow.MiningModeKeyGestureTextBox.Text = Utils.KeyGestureToString(MiningModeKeyGesture);
            preferenceWindow.PlayAudioKeyGestureTextBox.Text = Utils.KeyGestureToString(PlayAudioKeyGesture);
            preferenceWindow.KanjiModeKeyGestureTextBox.Text = Utils.KeyGestureToString(KanjiModeKeyGesture);
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

            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.ForceSyncAnkiCheckBox.IsChecked = ForceSyncAnki;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;
            preferenceWindow.KanjiModeCheckBox.IsChecked = KanjiMode;
            preferenceWindow.FrequencyListComboBox.ItemsSource = FrequencyLists.Keys;
            preferenceWindow.FrequencyListComboBox.SelectedItem = FrequencyListName;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;

            preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
            preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;


            Utils.Try(() => preferenceWindow.TextboxBackgroundColorButton.Background =
                    (SolidColorBrush)new BrushConverter()
                        .ConvertFrom(ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor")),
                preferenceWindow.TextboxBackgroundColorButton.Background, "MainWindowBackgroundColor");

            preferenceWindow.TextboxTextColorButton.Background = MainWindowTextColor;
            preferenceWindow.TextboxBacklogTextColorButton.Background = MainWindowBacklogTextColor;
            preferenceWindow.TextboxFontSizeNumericUpDown.Value = mainWindow.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = mainWindow.OpacitySlider.Value;
            preferenceWindow.MainWindowFontComboBox.ItemsSource = JapaneseFonts;
            preferenceWindow.MainWindowFontComboBox.SelectedItem = mainWindow.MainTextBox.FontFamily.ToString();

            preferenceWindow.PopupFontComboBox.ItemsSource = JapaneseFonts;
            preferenceWindow.PopupFontComboBox.SelectedItem = PopupFont.ToString();

            preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
            preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
            preferenceWindow.PopupDynamicHeightCheckBox.IsChecked = PopupDynamicHeight;
            preferenceWindow.PopupDynamicWidthCheckBox.IsChecked = PopupDynamicWidth;
            preferenceWindow.AlternativeSpellingsColorButton.Background = AlternativeSpellingsColor;
            preferenceWindow.AOrthographyInfoColorButton.Background = AOrthographyInfoColor;
            preferenceWindow.DeconjugationInfoColorButton.Background = DeconjugationInfoColor;
            preferenceWindow.DefinitionsColorButton.Background = DefinitionsColor;
            preferenceWindow.FrequencyColorButton.Background = FrequencyColor;
            preferenceWindow.PrimarySpellingColorButton.Background = PrimarySpellingColor;
            preferenceWindow.ReadingsColorButton.Background = ReadingsColor;
            preferenceWindow.ROrthographyInfoColorButton.Background = ROrthographyInfoColor;
            preferenceWindow.POrthographyInfoColorButton.Background = POrthographyInfoColor;
            preferenceWindow.AlternativeSpellingsFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
            preferenceWindow.AOrthographyInfoFontSizeNumericUpDown.Value = AOrthographyInfoFontSize;
            preferenceWindow.DeconjugationInfoFontSizeNumericUpDown.Value = DeconjugationInfoFontSize;
            preferenceWindow.DictTypeFontSizeNumericUpDown.Value = DictTypeFontSize;
            preferenceWindow.DefinitionsFontSizeNumericUpDown.Value = DefinitionsFontSize;
            preferenceWindow.FrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
            preferenceWindow.PrimarySpellingFontSizeNumericUpDown.Value = PrimarySpellingFontSize;
            preferenceWindow.ReadingsFontSizeNumericUpDown.Value = ReadingsFontSize;
            preferenceWindow.ROrthographyInfoFontSizeNumericUpDown.Value = ROrthographyInfoFontSize;
            preferenceWindow.POrthographyInfoFontSizeNumericUpDown.Value = POrthographyInfoFontSize;

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
            preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
            preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;

            preferenceWindow.PopupFlipComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("PopupFlip");
        }

        public static async Task SavePreferences(PreferencesWindow preferenceWindow)
        {
            Utils.KeyGestureSaver("MiningModeKeyGesture", preferenceWindow.MiningModeKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("PlayAudioKeyGesture", preferenceWindow.PlayAudioKeyGestureTextBox.Text);
            Utils.KeyGestureSaver("KanjiModeKeyGesture", preferenceWindow.KanjiModeKeyGestureTextBox.Text);
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
                preferenceWindow.MainWindowFontComboBox.SelectedItem.ToString();
            config.AppSettings.Settings["PopupFont"].Value =
                preferenceWindow.PopupFontComboBox.SelectedItem.ToString();
            config.AppSettings.Settings["FrequencyListName"].Value =
                preferenceWindow.FrequencyListComboBox.SelectedItem.ToString();

            config.AppSettings.Settings["KanjiMode"].Value =
                preferenceWindow.KanjiModeCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["ForceSyncAnki"].Value =
                preferenceWindow.ForceSyncAnkiCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["LookupRate"].Value =
                preferenceWindow.LookupRateNumericUpDown.Value.ToString();

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
            config.AppSettings.Settings["POrthographyInfoColor"].Value =
                preferenceWindow.POrthographyInfoColorButton.Background.ToString();
            config.AppSettings.Settings["ReadingsColor"].Value =
                preferenceWindow.ReadingsColorButton.Background.ToString();
            config.AppSettings.Settings["ROrthographyInfoColor"].Value =
                preferenceWindow.ROrthographyInfoColorButton.Background.ToString();
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
            config.AppSettings.Settings["POrthographyInfoFontSize"].Value =
                preferenceWindow.POrthographyInfoFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["ROrthographyInfoFontSize"].Value =
                preferenceWindow.ROrthographyInfoFontSizeNumericUpDown.Value.ToString();
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

            config.AppSettings.Settings["PopupXOffset"].Value =
                preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupYOffset"].Value =
                preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupFlip"].Value =
                preferenceWindow.PopupFlipComboBox.SelectedValue.ToString();

            config.AppSettings.Settings["MainWindowHeight"].Value =
                preferenceWindow.MainWindowHeightNumericUpDown.Value.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value =
                preferenceWindow.MainWindowWidthNumericUpDown.Value.ToString();

            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();

            SerializeDicts();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            await ApplyPreferences(mainWindow);
        }

        public static void SaveBeforeClosing(MainWindow mainWindow)
        {
            CreateDefaultAppConfig();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["MainWindowFontSize"].Value = mainWindow.FontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = MainWindowHeight.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = MainWindowWidth.ToString();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();

            config.Save(ConfigurationSaveMode.Modified);
        }

        private static void SerializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                    }
                };

                File.WriteAllTextAsync(Path.Join(ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(ConfigManager.Dicts, jso));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private static async Task DeserializeDicts()
        {
            try
            {
                var jso = new JsonSerializerOptions
                {
                    Converters =
                    {
                        new JsonStringEnumConverter(),
                    }
                };

                Dictionary<DictType, Dict> deserializedDicts = await JsonSerializer.DeserializeAsync<Dictionary<DictType, Dict>>(
                    (new StreamReader(Path.Join(ApplicationPath, "Config/dicts.json"))).BaseStream, jso);

                if (deserializedDicts != null)
                {
                    foreach ((DictType _, Dict dict) in deserializedDicts)
                    {
                        if (!ConfigManager.Dicts.ContainsKey(dict.Type))
                        {
                            dict.Contents = new Dictionary<string, List<IResult>>();
                            ConfigManager.Dicts.Add(dict.Type, dict);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("Couldn't load Config/dicts.json");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private static void CreateDefaultDictsConfig()
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters =
                {
                    new JsonStringEnumConverter(),
                }
            };

            try
            {
                Directory.CreateDirectory(Path.Join(ApplicationPath, "Config"));
                File.WriteAllText(Path.Join(ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(BuiltInDicts, jso));
            }
            catch (Exception e)
            {
                Console.WriteLine("Couldn't write default Dicts config");
                Debug.WriteLine(e);
            }
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

        private static async Task LoadDictionaries()
        {
            var tasks = new List<Task>();


            bool dictRemoved = false;

            foreach ((DictType _, Dict dict) in Dicts)
            {

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        // initial jmdict load
                        if (dict.Active && !Dicts[DictType.JMdict].Contents.Any())
                        {
                            tasks.Add(JMdictLoader.Load(dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.JMdict].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.JMnedict:
                        // JMnedict
                        if (dict.Active && !Dicts[DictType.JMnedict].Contents.Any())
                        {
                            tasks.Add(JMnedictLoader.Load(dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.JMnedict].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Kanjidic:
                        // KANJIDIC
                        if (dict.Active && !Dicts[DictType.Kanjidic].Contents.Any())
                        {
                            tasks.Add(KanjiInfoLoader.Load(dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Kanjidic].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Kenkyuusha:
                        if (dict.Active && !Dicts[DictType.Kenkyuusha].Contents.Any())
                        {
                            tasks.Add(EpwingJsonLoader.Loader(dict.Type, dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Kenkyuusha].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Daijirin:
                        if (dict.Active && !Dicts[DictType.Daijirin].Contents.Any())
                        {
                            tasks.Add(EpwingJsonLoader.Loader(dict.Type, dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Daijirin].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Daijisen:
                        if (dict.Active && !Dicts[DictType.Daijisen].Contents.Any())
                        {
                            tasks.Add(EpwingJsonLoader.Loader(dict.Type, dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Daijisen].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Koujien:
                        if (dict.Active && !Dicts[DictType.Koujien].Contents.Any())
                        {
                            tasks.Add(EpwingJsonLoader.Loader(dict.Type, dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Koujien].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.Meikyou:
                        if (dict.Active && !Dicts[DictType.Meikyou].Contents.Any())
                        {
                            tasks.Add(EpwingJsonLoader.Loader(dict.Type, dict.Path));
                        }

                        else if (!dict.Active && Dicts[DictType.Meikyou].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.CustomWordDictionary:
                        if (dict.Active && !Dicts[DictType.CustomWordDictionary].Contents.Any())
                        {
                            tasks.Add(CustomWordLoader.Load(Dicts[DictType.CustomWordDictionary].Path));
                        }

                        else if (!dict.Active && Dicts[DictType.CustomWordDictionary].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;
                    case DictType.CustomNameDictionary:
                        if (dict.Active && !Dicts[DictType.CustomNameDictionary].Contents.Any())
                        {
                            tasks.Add(CustomNameLoader.Load(Dicts[DictType.CustomNameDictionary].Path));
                        }

                        else if (!dict.Active && Dicts[DictType.CustomNameDictionary].Contents.Any())
                        {
                            dict.Contents.Clear();
                            dictRemoved = true;
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // load new freqlist if necessary
            if (!FrequencyLoader.FreqDicts.ContainsKey(FrequencyListName))
            {
                FrequencyLoader.FreqDicts.Clear();
                dictRemoved = true;

                if (FrequencyListName != "None")
                {
                    FrequencyLoader.FreqDicts.Add(FrequencyListName, new Dictionary<string, List<FrequencyEntry>>());

                    Task taskNewFreqlist = Task.Run(async () =>
                    {
                        FrequencyLoader.BuildFreqDict(await FrequencyLoader
                            .LoadJson(Path.Join(ApplicationPath, FrequencyLists[FrequencyListName])).ConfigureAwait(false));
                    });

                    tasks.Add(taskNewFreqlist);
                }
            }

            if (tasks.Any() || dictRemoved)
            {
                if (tasks.Any())
                {
                    await Task.WhenAll(tasks.ToArray()).ConfigureAwait(false);
                }

                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }
        }
    }
}