using JapaneseLookup.GUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Markup;
using JapaneseLookup.Abstract;
using JapaneseLookup.EPWING;
using JapaneseLookup.KANJIDIC;
using JapaneseLookup.CustomDict;
using JapaneseLookup.Dicts;
using JapaneseLookup.EDICT.JMdict;
using JapaneseLookup.EDICT.JMnedict;
using JapaneseLookup.Frequency;
using System.Xml;

namespace JapaneseLookup
{
    public static class ConfigManager
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();
        private static readonly List<string> JapaneseFonts = Utilities.Utils.FindJapaneseFonts().OrderBy(font => font).ToList();

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

        // TODO: Make these configurable too
        private static readonly Dictionary<string, string> FrequencyLists = new()
        {
            { "VN", "Resources/freqlist_vns.json" },
            { "Novel", "Resources/freqlist_novels.json" },
            { "Narou", "Resources/freqlist_narou.json" },
            { "None", "" }
        };

        public static readonly Dictionary<DictType, Dict> Dicts = new();

        public static string AnkiConnectUri;
        public static int MaxSearchLength;
        public static string FrequencyList;

        // TODO: Don't let KanjiMode be turned on if Kanjidic is not loaded?
        public static bool KanjiMode;
        public static bool ForceSync;
        public static int LookupRate;

        public static double MainWindowHeight;
        public static double MainWindowWidth;

        public static SolidColorBrush MainWindowTextColor;
        public static SolidColorBrush MainWindowBacklogTextColor;
        public static SolidColorBrush FoundSpellingColor;
        public static SolidColorBrush ReadingsColor;
        public static SolidColorBrush ROrthographyInfoColor;
        public static SolidColorBrush DefinitionsColor;
        public static SolidColorBrush ProcessColor;
        public static SolidColorBrush FrequencyColor;
        public static SolidColorBrush AlternativeSpellingsColor;
        public static SolidColorBrush AOrthographyInfoColor;
        public static SolidColorBrush SeparatorColor;
        public static SolidColorBrush PopupBackgroundColor;

        public static int FoundSpellingFontSize;
        public static int ReadingsFontSize;
        public static int ROrthographyInfoFontSize;
        public static int DefinitionsFontSize;
        public static int ProcessFontSize;
        public static int FrequencyFontSize;
        public static int AlternativeSpellingsFontSize;
        public static int AOrthographyInfoFontSize;

        public static int PopupMaxWidth;
        public static int PopupMaxHeight;
        public static int PopupXOffset;
        public static int PopupYOffset;
        public static bool PopupFlipX;
        public static bool PopupFlipY;

        // TODO: hook these up
        //public static bool FixedPopupWidth = false;
        //public static bool FixedPopupHeight = false;
        //public static SolidColorBrush POrthographyInfoColor;
        //public static int POrthographyInfoFontSize;
        public static Brush DictTypeColor = Brushes.LightBlue;
        public static int DictTypeFontSize = 15;
        public static KeyGesture MiningModeKeyGesture;
        public static KeyGesture PlayAudioKeyGesture;
        public static KeyGesture KanjiModeKeyGesture;
        public static KeyGesture ShowPreferencesWindowKeyGesture;
        public static KeyGesture ShowAddNameWindowKeyGesture;
        public static KeyGesture ShowAddWordWindowKeyGesture;
        public static KeyGesture SearchWithBrowserKeyGesture;
        public static KeyGesture TransparentModeKeyGesture;
        public static KeyGesture SteppedBacklogBackwardsKeyGesture;
        public static KeyGesture SteppedBacklogForwardsKeyGesture;

        // consider making this dictionary specific
        // enabling this seems to improve rendering performance by a lot; need to test if it's because
        // a) there's less text on the screen overall
        // b) there's less word-wrapping to do
        public static bool NewlineBetweenDefinitions = false;
        public static int MaxResults = 99;
        public static bool AllowDuplicateCards = false;

        public static void ApplyPreferences(MainWindow mainWindow)
        {
            CreateDefaultAppConfig();

            MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength")!);
            FrequencyList = ConfigurationManager.AppSettings.Get("FrequencyList");
            AnkiConnectUri = ConfigurationManager.AppSettings.Get("AnkiConnectUri");
            KanjiMode = bool.Parse(ConfigurationManager.AppSettings.Get("KanjiMode")!);

            ForceSync = bool.Parse(ConfigurationManager.AppSettings.Get("ForceAnkiSync")!);
            LookupRate = int.Parse(ConfigurationManager.AppSettings.Get("LookupRate")!);

            MainWindowHeight = int.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight")!);
            MainWindowWidth = int.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth")!);

            // MAKE SURE YOU FREEZE ANY NEW COLOR OBJECTS YOU ADD
            // OR THE PROGRAM WILL CRASH AND BURN
            MainWindowTextColor = (SolidColorBrush) new BrushConverter().ConvertFrom(
                ConfigurationManager.AppSettings.Get("MainWindowTextColor"));
            MainWindowTextColor!.Freeze();
            MainWindowBacklogTextColor = (SolidColorBrush) new BrushConverter().ConvertFrom(
                ConfigurationManager.AppSettings.Get("MainWindowBacklogTextColor"));
            MainWindowBacklogTextColor!.Freeze();

            FoundSpellingColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupPrimarySpellingColor"));
            FoundSpellingColor!.Freeze();
            ReadingsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupReadingColor"));
            ReadingsColor!.Freeze();
            ROrthographyInfoColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupROrthographyInfoColor"));
            ROrthographyInfoColor!.Freeze();
            AlternativeSpellingsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupAlternativeSpellingColor"));
            AlternativeSpellingsColor!.Freeze();
            AOrthographyInfoColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupAOrthographyInfoColor"));
            AOrthographyInfoColor!.Freeze();
            DefinitionsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDefinitionColor"));
            DefinitionsColor!.Freeze();
            FrequencyColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupFrequencyColor"));
            FrequencyColor!.Freeze();
            ProcessColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDeconjugationInfoColor"));
            ProcessColor!.Freeze();
            SeparatorColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupSeparatorColor"));
            SeparatorColor!.Freeze();
            PopupBackgroundColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));
            PopupBackgroundColor!.Opacity = double.Parse(ConfigurationManager.AppSettings.Get("PopupOpacity")!) / 100;
            PopupBackgroundColor!.Freeze();

            FoundSpellingFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupPrimarySpellingFontSize")!);
            ReadingsFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupReadingFontSize")!);
            ROrthographyInfoFontSize =
                int.Parse(ConfigurationManager.AppSettings.Get("PopupROrthographyInfoFontSize")!);
            AlternativeSpellingsFontSize =
                int.Parse(ConfigurationManager.AppSettings.Get("PopupAlternativeSpellingFontSize")!);
            AOrthographyInfoFontSize =
                int.Parse(ConfigurationManager.AppSettings.Get("PopupAOrthographyInfoFontSize")!);
            DefinitionsFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupDefinitionFontSize")!);
            FrequencyFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupFrequencyFontSize")!);
            ProcessFontSize = int.Parse(ConfigurationManager.AppSettings.Get("PopupDeconjugationInfoFontSize")!);

            PopupXOffset = int.Parse(ConfigurationManager.AppSettings.Get("PopupXOffset")!);
            PopupYOffset = int.Parse(ConfigurationManager.AppSettings.Get("PopupYOffset")!);

            PopupMaxWidth = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth")!);
            PopupMaxHeight = int.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight")!);

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
            }

            #region KeyGestures

            string rawKeyGesture;
            KeyGestureConverter keyGestureConverter = new();

            rawKeyGesture = ConfigurationManager.AppSettings.Get("MiningModeKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                MiningModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                MiningModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("PlayAudioKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                PlayAudioKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                PlayAudioKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("KanjiModeKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                KanjiModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                KanjiModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("ShowPreferencesWindowKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                ShowPreferencesWindowKeyGesture =
                    (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                ShowPreferencesWindowKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("ShowAddNameWindowKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                ShowAddNameWindowKeyGesture =
                    (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                ShowAddNameWindowKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("ShowAddWordWindowKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                ShowAddWordWindowKeyGesture =
                    (KeyGesture)keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                ShowAddWordWindowKeyGesture = (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("SearchWithBrowserKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                SearchWithBrowserKeyGesture =
                    (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                SearchWithBrowserKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("TransparentModeKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                TransparentModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                TransparentModeKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("SteppedBacklogBackwardsKeyGesture");
            if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                SteppedBacklogBackwardsKeyGesture =
                    (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                SteppedBacklogBackwardsKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            rawKeyGesture = ConfigurationManager.AppSettings.Get("SteppedBacklogForwardsKeyGesture");
            if (!rawKeyGesture.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                !rawKeyGesture.StartsWith("Alt+"))
                SteppedBacklogForwardsKeyGesture =
                    (KeyGesture) keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
            else
                SteppedBacklogForwardsKeyGesture = (KeyGesture) keyGestureConverter.ConvertFromString(rawKeyGesture);

            #endregion

            mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity")!);
            mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize")!);
            mainWindow.MainTextBox.FontFamily = new FontFamily(ConfigurationManager.AppSettings.Get("MainWindowFont")!);
            mainWindow.MainTextBox.FontSize = mainWindow.FontSizeSlider.Value;
            mainWindow.Background =
                (SolidColorBrush) new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            Debug.Assert(mainWindow.Background != null, "mainWindow.Background != null");
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value / 100;
            mainWindow.MainTextBox.Foreground = MainWindowTextColor;
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight")!);
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth")!);
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPosition")!);
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition")!);

            var popupWindow = MainWindow.FirstPopupWindow;
            popupWindow.Background = PopupBackgroundColor;
            Debug.Assert(popupWindow.Background != null, "FirstPopupWindow.Background != null");
            popupWindow.MaxHeight = double.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight")!);
            popupWindow.MaxWidth = double.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth")!);

            if (!File.Exists(Path.Join(ApplicationPath, "Config/dicts.json")))
                CreateDefaultDictsConfig();

            DeserializeDicts();

            //Test without async/await.
            // Task.Run(async () => { await LoadDictionaries(); });
            Task.Run(() => { LoadDictionaries(); });
        }

        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            CreateDefaultAppConfig();
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();

            preferenceWindow.MiningModeKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("MiningModeKeyGesture")!;
            preferenceWindow.PlayAudioKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("PlayAudioKeyGesture")!;
            preferenceWindow.KanjiModeKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("KanjiModeKeyGesture")!;
            preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("ShowPreferencesWindowKeyGesture")!;
            preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("ShowAddNameWindowKeyGesture")!;
            preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("ShowAddWordWindowKeyGesture")!;
            preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("SearchWithBrowserKeyGesture")!;
            preferenceWindow.TransparentModeKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("TransparentModeKeyGesture")!;
            preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("SteppedBacklogBackwardsKeyGesture")!;
            preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text =
                ConfigurationManager.AppSettings.Get("SteppedBacklogForwardsKeyGesture")!;

            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.ForceAnkiSyncCheckBox.IsChecked = ForceSync;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;
            preferenceWindow.KanjiModeCheckBox.IsChecked = KanjiMode;
            preferenceWindow.FrequencyListComboBox.ItemsSource = FrequencyLists.Keys;
            preferenceWindow.FrequencyListComboBox.SelectedItem = FrequencyList;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;

            preferenceWindow.MainWindowHeightNumericUpDown.Value = MainWindowHeight;
            preferenceWindow.MainWindowWidthNumericUpDown.Value = MainWindowWidth;
            preferenceWindow.TextboxBackgroundColorButton.Background =
                (SolidColorBrush) new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            preferenceWindow.TextboxTextColorButton.Background = MainWindowTextColor;
            preferenceWindow.TextboxBacklogTextColorButton.Background = MainWindowBacklogTextColor;
            preferenceWindow.TextboxFontSizeNumericUpDown.Value = mainWindow.FontSizeSlider.Value;
            preferenceWindow.TextboxOpacityNumericUpDown.Value = mainWindow.OpacitySlider.Value;
            preferenceWindow.FontComboBox.ItemsSource = JapaneseFonts;
            preferenceWindow.FontComboBox.SelectedItem = mainWindow.MainTextBox.FontFamily.ToString();

            preferenceWindow.PopupMaxHeightNumericUpDown.Value = PopupMaxHeight;
            preferenceWindow.PopupMaxWidthNumericUpDown.Value = PopupMaxWidth;
            preferenceWindow.PopupAlternativeSpellingColorButton.Background = AlternativeSpellingsColor;
            preferenceWindow.PopupAOrthographyInfoColorButton.Background = AOrthographyInfoColor;
            preferenceWindow.PopupDeconjugationInfoColorButton.Background = ProcessColor;
            preferenceWindow.PopupDefinitionColorButton.Background = DefinitionsColor;
            preferenceWindow.PopupFrequencyColorButton.Background = FrequencyColor;
            preferenceWindow.PopupPrimarySpellingColorButton.Background = FoundSpellingColor;
            preferenceWindow.PopupReadingColorButton.Background = ReadingsColor;
            preferenceWindow.PopupROrthographyInfoColorButton.Background = ROrthographyInfoColor;
            preferenceWindow.PopupAlternativeSpellingFontSizeNumericUpDown.Value = AlternativeSpellingsFontSize;
            preferenceWindow.PopupAOrthographyInfoFontSizeNumericUpDown.Value = AOrthographyInfoFontSize;
            preferenceWindow.PopupDeconjugationInfoFontSizeNumericUpDown.Value = ProcessFontSize;
            preferenceWindow.PopupDefinitionFontSizeNumericUpDown.Value = DefinitionsFontSize;
            preferenceWindow.PopupFrequencyFontSizeNumericUpDown.Value = FrequencyFontSize;
            preferenceWindow.PopupPrimarySpellingFontSizeNumericUpDown.Value = FoundSpellingFontSize;
            preferenceWindow.PopupReadingFontSizeNumericUpDown.Value = ReadingsFontSize;
            preferenceWindow.PopupROrthographyInfoFontSizeNumericUpDown.Value = ROrthographyInfoFontSize;

            // Button background color has to be opaque, so we cannot use PopupBackgroundColor here
            preferenceWindow.PopupBackgroundColorButton.Background = (SolidColorBrush)new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));

            preferenceWindow.PopupOpacityNumericUpDown.Value = int.Parse(
                ConfigurationManager.AppSettings.Get("PopupOpacity") ?? throw new InvalidOperationException());
            preferenceWindow.PopupSeparatorColorButton.Background = SeparatorColor;
            preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
            preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;

            preferenceWindow.PopupFlipComboBox.SelectedValue = ConfigurationManager.AppSettings.Get("PopupFlip");
        }

        public static void SavePreferences(PreferencesWindow preferenceWindow)
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            #region KeyGestures

            string rawKeyGesture;
            rawKeyGesture = preferenceWindow.MiningModeKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["MiningModeKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["MiningModeKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.PlayAudioKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["PlayAudioKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["PlayAudioKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.KanjiModeKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["KanjiModeKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["KanjiModeKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.ShowPreferencesWindowKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["ShowPreferencesWindowKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["ShowPreferencesWindowKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.ShowAddNameWindowKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["ShowAddNameWindowKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["ShowAddNameWindowKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.ShowAddWordWindowKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["ShowAddWordWindowKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["ShowAddWordWindowKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.SearchWithBrowserKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["SearchWithBrowserKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["SearchWithBrowserKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.TransparentModeKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["TransparentModeKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["TransparentModeKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.SteppedBacklogBackwardsKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["SteppedBacklogBackwardsKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["SteppedBacklogBackwardsKeyGesture"].Value = rawKeyGesture;

            rawKeyGesture = preferenceWindow.SteppedBacklogForwardsKeyGestureTextBox.Text;
            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings["SteppedBacklogForwardsKeyGesture"].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings["SteppedBacklogForwardsKeyGesture"].Value = rawKeyGesture;

            #endregion

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
                preferenceWindow.FontComboBox.SelectedItem.ToString();
            config.AppSettings.Settings["FrequencyList"].Value =
                preferenceWindow.FrequencyListComboBox.SelectedItem.ToString();

            config.AppSettings.Settings["KanjiMode"].Value =
                preferenceWindow.KanjiModeCheckBox.IsChecked.ToString();

            config.AppSettings.Settings["ForceAnkiSync"].Value =
                preferenceWindow.ForceAnkiSyncCheckBox.IsChecked.ToString();
            config.AppSettings.Settings["LookupRate"].Value =
                preferenceWindow.LookupRateNumericUpDown.Value.ToString();

            config.AppSettings.Settings["PopupMaxWidth"].Value =
                preferenceWindow.PopupMaxWidthNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupMaxHeight"].Value =
                preferenceWindow.PopupMaxHeightNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupBackgroundColor"].Value =
                preferenceWindow.PopupBackgroundColorButton.Background.ToString();
            config.AppSettings.Settings["PopupPrimarySpellingColor"].Value =
                preferenceWindow.PopupPrimarySpellingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupReadingColor"].Value =
                preferenceWindow.PopupReadingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupAlternativeSpellingColor"].Value =
                preferenceWindow.PopupAlternativeSpellingColorButton.Background.ToString();
            config.AppSettings.Settings["PopupDefinitionColor"].Value =
                preferenceWindow.PopupDefinitionColorButton.Background.ToString();
            config.AppSettings.Settings["PopupFrequencyColor"].Value =
                preferenceWindow.PopupFrequencyColorButton.Background.ToString();
            config.AppSettings.Settings["PopupDeconjugationInfoColor"].Value =
                preferenceWindow.PopupDeconjugationInfoColorButton.Background.ToString();
            config.AppSettings.Settings["PopupOpacity"].Value =
                preferenceWindow.PopupOpacityNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupPrimarySpellingFontSize"].Value =
                preferenceWindow.PopupPrimarySpellingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupReadingFontSize"].Value =
                preferenceWindow.PopupReadingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupAlternativeSpellingFontSize"].Value =
                preferenceWindow.PopupAlternativeSpellingFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupDefinitionFontSize"].Value =
                preferenceWindow.PopupDefinitionFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupFrequencyFontSize"].Value =
                preferenceWindow.PopupFrequencyFontSizeNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupDeconjugationInfoFontSize"].Value =
                preferenceWindow.PopupDeconjugationInfoFontSizeNumericUpDown.Value.ToString();

            config.AppSettings.Settings["PopupSeparatorColor"].Value =
                preferenceWindow.PopupSeparatorColorButton.Background.ToString();

            config.AppSettings.Settings["PopupXOffset"].Value =
                preferenceWindow.PopupXOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupYOffset"].Value =
                preferenceWindow.PopupYOffsetNumericUpDown.Value.ToString();
            config.AppSettings.Settings["PopupFlip"].Value =
                preferenceWindow.PopupFlipComboBox.SelectedValue.ToString();

            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
            config.AppSettings.Settings["MainWindowTopPosition"].Value = mainWindow.Top.ToString();
            config.AppSettings.Settings["MainWindowLeftPosition"].Value = mainWindow.Left.ToString();

            SerializeDicts();

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");

            ApplyPreferences(mainWindow);
        }

        public static void SaveBeforeClosing(MainWindow mainWindow)
        {
            CreateDefaultAppConfig();

            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            config.AppSettings.Settings["MainWindowFontSize"].Value = mainWindow.FontSizeSlider.Value.ToString();
            config.AppSettings.Settings["MainWindowOpacity"].Value = mainWindow.OpacitySlider.Value.ToString();
            config.AppSettings.Settings["MainWindowHeight"].Value = mainWindow.Height.ToString();
            config.AppSettings.Settings["MainWindowWidth"].Value = mainWindow.Width.ToString();
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

                File.WriteAllText(Path.Join(ApplicationPath, "Config/dicts.json"),
                    JsonSerializer.Serialize(ConfigManager.Dicts, jso));
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                throw;
            }
        }

        private static void DeserializeDicts()
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

                Dictionary<DictType, Dict> deserializedDicts = JsonSerializer.Deserialize<Dictionary<DictType, Dict>>(
                    File.ReadAllText(Path.Join(ApplicationPath, "Config/dicts.json")), jso);

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

                if (!File.Exists("Resources\\custom_words.txt"))
                    File.Create("Resources\\custom_words.txt");

                if (!File.Exists("Resources\\custom_names.txt"))
                    File.Create("Resources\\custom_names.txt");
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

                    WriteXmlAdd(writer, "MaxSearchLength", "37");
                    WriteXmlAdd(writer, "FrequencyList", "VN");
                    WriteXmlAdd(writer, "KanjiMode", "false");
                    WriteXmlAdd(writer, "LookupRate", "0");

                    WriteXmlAdd(writer, "MainWindowOpacity", "30");
                    WriteXmlAdd(writer, "MainWindowBackgroundColor", "#FF000000");
                    WriteXmlAdd(writer, "MainWindowTextColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "MainWindowBacklogTextColor", "#FFFFE4C4");
                    WriteXmlAdd(writer, "MainWindowFontSize", "40");
                    WriteXmlAdd(writer, "MainWindowFont", "Meiryo");
                    WriteXmlAdd(writer, "MainWindowHeight", "300");
                    WriteXmlAdd(writer, "MainWindowWidth", "1200");
                    WriteXmlAdd(writer, "MainWindowTopPosition", "100");
                    WriteXmlAdd(writer, "MainWindowLeftPosition", "100");

                    WriteXmlAdd(writer, "PopupBackgroundColor", "#FF000000");
                    WriteXmlAdd(writer, "PopupOpacity", "100");
                    WriteXmlAdd(writer, "PopupPrimarySpellingColor", "#FFD2691E");
                    WriteXmlAdd(writer, "PopupPrimarySpellingFontSize", "21");
                    WriteXmlAdd(writer, "PopupReadingColor", "#FFDAA520");
                    WriteXmlAdd(writer, "PopupROrthographyInfoColor", "#FFDAA520");
                    WriteXmlAdd(writer, "PopupReadingFontSize", "19");
                    WriteXmlAdd(writer, "PopupROrthographyInfoFontSize", "17");
                    WriteXmlAdd(writer, "PopupAlternativeSpellingColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "PopupAOrthographyInfoColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "PopupAlternativeSpellingFontSize", "17");
                    WriteXmlAdd(writer, "PopupAOrthographyInfoFontSize", "17");
                    WriteXmlAdd(writer, "PopupDefinitionColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "PopupDefinitionFontSize", "17");
                    WriteXmlAdd(writer, "PopupFrequencyColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "PopupFrequencyFontSize", "17");
                    WriteXmlAdd(writer, "PopupDeconjugationInfoColor", "#FFFFFFFF");
                    WriteXmlAdd(writer, "PopupDeconjugationInfoFontSize", "17");
                    WriteXmlAdd(writer, "PopupMaxWidth", "700");
                    WriteXmlAdd(writer, "PopupMaxHeight", "520");
                    WriteXmlAdd(writer, "PopupXOffset", "10");
                    WriteXmlAdd(writer, "PopupYOffset", "20");
                    WriteXmlAdd(writer, "PopupFlip", "Y");
                    WriteXmlAdd(writer, "PopupSeparatorColor", "#FFFFFFFF");

                    WriteXmlAdd(writer, "AnkiConnectUri", "http://localhost:8765");
                    WriteXmlAdd(writer, "ForceAnkiSync", "false");

                    WriteXmlAdd(writer, "MiningModeKeyGesture", "M");
                    WriteXmlAdd(writer, "PlayAudioKeyGesture", "P");
                    WriteXmlAdd(writer, "KanjiModeKeyGesture", "K");
                    WriteXmlAdd(writer, "ShowPreferencesWindowKeyGesture", "L");
                    WriteXmlAdd(writer, "ShowAddNameWindowKeyGesture", "N");
                    WriteXmlAdd(writer, "ShowAddWordWindowKeyGesture", "W");
                    WriteXmlAdd(writer, "SearchWithBrowserKeyGesture", "S");
                    WriteXmlAdd(writer, "TransparentModeKeyGesture", "T");
                    WriteXmlAdd(writer, "SteppedBacklogBackwardsKeyGesture", "Left");
                    WriteXmlAdd(writer, "SteppedBacklogForwardsKeyGesture", "Right");

                    writer.WriteEndDocument();
                }

                Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        private static void WriteXmlAdd(XmlWriter writer, string key, string value)
        {
            writer.WriteStartElement("add");
            writer.WriteAttributeString("key", key);
            writer.WriteAttributeString("value", value);
            writer.WriteEndElement();
        }
        private static void LoadDictionaries()
        {
            string freqListPath = FrequencyLists[FrequencyList];

            var tasks = new List<Task>();

            var freqTask = Task.Run(() => FrequencyLoader.BuildFreqDict(FrequencyLoader.LoadJson(Path.Join(ApplicationPath, freqListPath)).Result));
            tasks.Add(freqTask);
            
            foreach ((DictType _, Dict dict) in ConfigManager.Dicts)
            {
                if (!dict.Active)
                    continue;

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        // initial jmdict load
                        if (!Dicts[DictType.JMdict].Contents.Any())
                        {
                            var taskJmdict = Task.Run(() => JMdictLoader.Load(dict.Path));
                            tasks.Add(taskJmdict);
                        }

                        break;
                    case DictType.JMnedict:
                        // JMnedict
                        if (!Dicts[DictType.JMnedict].Contents.Any())
                        {
                            var taskJMnedict = Task.Run(() => JMnedictLoader.Load(dict.Path));
                            tasks.Add(taskJMnedict);
                        }

                        break;
                    case DictType.Kanjidic:
                        // KANJIDIC
                        if (!ConfigManager.Dicts[DictType.Kanjidic].Contents.Any())
                        {
                            var taskKanjidict = Task.Run(() => KanjiInfoLoader.Load(dict.Path));
                            tasks.Add(taskKanjidict);
                        }

                        break;
                    case DictType.UnknownEpwing:
                        if (!ConfigManager.Dicts[DictType.UnknownEpwing].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Daijirin:
                        if (!ConfigManager.Dicts[DictType.Daijirin].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Daijisen:
                        if (!ConfigManager.Dicts[DictType.Daijisen].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Koujien:
                        if (!ConfigManager.Dicts[DictType.Koujien].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Meikyou:
                        if (!ConfigManager.Dicts[DictType.Meikyou].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.CustomWordDictionary:
                        if (!ConfigManager.Dicts[DictType.CustomWordDictionary].Contents.Any())
                        {
                            var taskCustomWordDict = Task.Run(() => CustomWordLoader.Load(
                                ConfigManager.Dicts[DictType.CustomWordDictionary].Path));
                            tasks.Add(taskCustomWordDict);
                        }

                        break;
                    case DictType.CustomNameDictionary:
                        if (!ConfigManager.Dicts[DictType.CustomNameDictionary].Contents.Any())
                        {
                            var taskCustomNameDict = Task.Run(() => CustomNameLoader.Load(
                                ConfigManager.Dicts[DictType.CustomNameDictionary].Path));
                            tasks.Add(taskCustomNameDict);
                        }

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach ((DictType _, Dict dict) in ConfigManager.Dicts)
            {
                if (!dict.Active && dict.Contents.Any())
                {
                    Debug.WriteLine("Clearing " + dict.Type);
                    dict.Contents.Clear();
                }
            }

            // load new freqlist if necessary
            //if (Dicts[DictType.JMdict]?.Contents.Any() ?? false)
            //{
            //    Dicts[DictType.JMdict].Contents.TryGetValue("俺", out List<IResult> freqTest1);
            //    Debug.Assert(freqTest1 != null, nameof(freqTest1) + " != null");

            //    var freqTest = freqTest1.Cast<JMdictResult>().ToList();
            //    // todo get NRE here sometimes
            //    if (!freqTest[0].FrequencyDict.TryGetValue(FrequencyList, out int _))
            //    {
            //        var taskNewFreqlist = Task.Run(async () =>
            //        {
            //            FrequencyLoader.AddToJMdict($"{FrequencyList}", await FrequencyLoader.LoadJson(Path.Join(
            //                ApplicationPath,
            //                freqListPath)));
            //        });
            //        tasks.Add(taskNewFreqlist);

            //        Debug.WriteLine("Banzai! (changed freqlist)");
            //    }
            //}

            Task.WaitAll(tasks.ToArray());

            // TODO: doesn't seem to compact after saving settings sometimes
            Debug.WriteLine("Starting compacting GC run");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.GetTotalMemory(true);
            GC.Collect();
        }
    }
}