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
using JapaneseLookup.EDICT;
using JapaneseLookup.EPWING;
using JapaneseLookup.KANJIDIC;

namespace JapaneseLookup
{
    public static class ConfigManager
    {
        public static readonly string ApplicationPath = Directory.GetCurrentDirectory();
        private static readonly List<string> JapaneseFonts = FindJapaneseFonts().OrderBy(font => font).ToList();

        // TODO: Make these configurable too
        private static readonly Dictionary<string, string> FrequencyLists = new()
        {
            { "VN", "Resources/freqlist_vns.json" },
            { "Novel", "Resources/freqlist_novels.json" },
            { "Narou", "Resources/freqlist_narou.json" },
            { "None", "" }
        };

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
        //public static bool fixedWidth = false;
        //public static bool fixedHeight = false;
        public static Key MiningModeKey = Key.M;
        public static Key PlayAudioKey = Key.P;
        public static Key KanjiModeKey = Key.K;
        public static Key ShowPreferencesWindowKey = Key.L;
        public static Key ShowAddNameWindowKey = Key.N;
        public static Key ShowAddWordWindowKey = Key.W;
        public static Key SearchWithBrowserKey = Key.S;
        public static Key TransparentModeKey = Key.T;
        public static Key SteppedBacklogBackwardsKey = Key.Left;
        public static Key SteppedBacklogForwardsKey = Key.Right;

        // consider making this dictionary specific
        public static bool NewlineBetweenDefinitions = false;
        public static int MaxResults = 99;

        public static void ApplyPreferences(MainWindow mainWindow)
        {
            MaxSearchLength = int.Parse(ConfigurationManager.AppSettings.Get("MaxSearchLength") ??
                                        throw new InvalidOperationException());
            FrequencyList = ConfigurationManager.AppSettings.Get("FrequencyList");
            AnkiConnectUri = ConfigurationManager.AppSettings.Get("AnkiConnectUri");
            // UseJMnedict = bool.Parse(ConfigurationManager.AppSettings.Get("UseJMnedict") ??
            //                          throw new InvalidOperationException());
            KanjiMode = bool.Parse(ConfigurationManager.AppSettings.Get("KanjiMode") ??
                                   throw new InvalidOperationException());

            ForceSync = bool.Parse(ConfigurationManager.AppSettings.Get("ForceAnkiSync") ??
                                   throw new InvalidOperationException());
            LookupRate = int.Parse(ConfigurationManager.AppSettings.Get("LookupRate") ??
                                   throw new InvalidOperationException());

            MainWindowHeight = int.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight")!);
            MainWindowWidth = int.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth")!);

            FoundSpellingColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupPrimarySpellingColor"));
            ReadingsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupReadingColor"));
            ROrthographyInfoColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupROrthographyInfoColor"));
            AlternativeSpellingsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupAlternativeSpellingColor"));
            AOrthographyInfoColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupAOrthographyInfoColor"));
            DefinitionsColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDefinitionColor"));
            FrequencyColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupFrequencyColor"));
            ProcessColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupDeconjugationInfoColor"));
            SeparatorColor = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupSeparatorColor"));

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

            MainWindowTextColor = (SolidColorBrush) new BrushConverter().ConvertFrom(
                ConfigurationManager.AppSettings.Get("MainWindowTextColor"));
            MainWindowBacklogTextColor = (SolidColorBrush) new BrushConverter().ConvertFrom(
                ConfigurationManager.AppSettings.Get("MainWindowBacklogTextColor"));

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

            mainWindow.OpacitySlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowOpacity"));
            mainWindow.FontSizeSlider.Value = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowFontSize"));
            mainWindow.MainTextBox.FontFamily = new FontFamily(ConfigurationManager.AppSettings.Get("MainWindowFont"));
            mainWindow.MainTextBox.FontSize = mainWindow.FontSizeSlider.Value;
            mainWindow.Background =
                (SolidColorBrush) new BrushConverter().ConvertFrom(
                    ConfigurationManager.AppSettings.Get("MainWindowBackgroundColor"));
            mainWindow.Background.Opacity = mainWindow.OpacitySlider.Value / 100;
            mainWindow.MainTextBox.Foreground = MainWindowTextColor;
            mainWindow.Height = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowHeight"));
            mainWindow.Width = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowWidth"));
            mainWindow.Top = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowTopPosition"));
            mainWindow.Left = double.Parse(ConfigurationManager.AppSettings.Get("MainWindowLeftPosition"));

            var popupWindow = PopupWindow.Instance;
            popupWindow.Background = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));
            popupWindow.Background.Opacity = double.Parse(ConfigurationManager.AppSettings.Get("PopupOpacity")) / 100;
            popupWindow.MaxHeight = double.Parse(ConfigurationManager.AppSettings.Get("PopupMaxHeight"));
            popupWindow.MaxWidth = double.Parse(ConfigurationManager.AppSettings.Get("PopupMaxWidth"));

            DeserializeDicts();

            //Test without async/await.
            // Task.Run(async () => { await LoadDictionaries(); });
            Task.Run(() => { LoadDictionaries(); });
        }

        public static void LoadPreferences(PreferencesWindow preferenceWindow)
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();

            preferenceWindow.MaxSearchLengthNumericUpDown.Value = MaxSearchLength;
            preferenceWindow.AnkiUriTextBox.Text = AnkiConnectUri;
            preferenceWindow.ForceAnkiSyncCheckBox.IsChecked = ForceSync;
            preferenceWindow.LookupRateNumericUpDown.Value = LookupRate;
            // preferenceWindow.UseJMnedictCheckBox.IsChecked = UseJMnedict;
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
            preferenceWindow.PopupBackgroundColorButton.Background = (SolidColorBrush) new BrushConverter()
                .ConvertFrom(ConfigurationManager.AppSettings.Get("PopupBackgroundColor"));
            preferenceWindow.PopupOpacityNumericUpDown.Value = int.Parse(
                ConfigurationManager.AppSettings.Get("PopupOpacity") ?? throw new InvalidOperationException());
            preferenceWindow.PopupSeparatorColorButton.Background = SeparatorColor;
            preferenceWindow.PopupXOffsetNumericUpDown.Value = PopupXOffset;
            preferenceWindow.PopupYOffsetNumericUpDown.Value = PopupYOffset;

            switch (ConfigurationManager.AppSettings.Get("PopupFlip"))
            {
                case "X":
                    preferenceWindow.PopupFlipComboBox.SelectedValue = "X";
                    break;

                case "Y":
                    preferenceWindow.PopupFlipComboBox.SelectedValue = "Y";
                    break;

                case "Both":
                    preferenceWindow.PopupFlipComboBox.SelectedValue = "Both";
                    break;
            }
        }

        public static void SavePreferences(PreferencesWindow preferenceWindow)
        {
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
                    JsonSerializer.Serialize(Dicts.dicts, jso));
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
                        if (!Dicts.dicts.ContainsKey(dict.Type))
                        {
                            dict.Contents = new Dictionary<string, List<IResult>>();
                            Dicts.dicts.Add(dict.Type, dict);
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

        private static List<string> FindJapaneseFonts()
        {
            List<string> japaneseFonts = new();
            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                if (fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
                    japaneseFonts.Add(fontFamily.Source);

                else if (fontFamily.FamilyNames.Keys != null && fontFamily.FamilyNames.Keys.Count == 1 &&
                         fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("en-US")))
                {
                    foreach (var typeFace in fontFamily.GetTypefaces())
                    {
                        if (typeFace.TryGetGlyphTypeface(out var glyphTypeFace))
                        {
                            if (glyphTypeFace.CharacterToGlyphMap.ContainsKey(20685))
                            {
                                japaneseFonts.Add(fontFamily.Source);
                                break;
                            }
                        }
                    }
                }
            }

            return japaneseFonts;
        }

        private static void LoadDictionaries()
        {
            string freqListPath = FrequencyLists[FrequencyList];

            var tasks = new List<Task>();

            foreach ((DictType _, Dict dict) in Dicts.dicts)
            {
                if (!dict.Active)
                    continue;

                switch (dict.Type)
                {
                    case DictType.JMdict:
                        // initial jmdict and freqlist load
                        if (!Dicts.dicts[DictType.JMdict].Contents.Any())
                        {
                            var taskJmdict = Task.Run(() => JMdictLoader.Load(dict.Path)).ContinueWith(_ =>
                            {
                                FrequencyLoader.AddToJMdict($"{FrequencyList}", FrequencyLoader.LoadJson(Path.Join(
                                    ApplicationPath,
                                    freqListPath)).Result);
                            });

                            tasks.Add(taskJmdict);
                        }

                        break;
                    case DictType.JMnedict:
                        // JMnedict
                        if (!Dicts.dicts[DictType.JMnedict].Contents.Any())
                        {
                            var taskJMnedict = Task.Run(() => JMnedictLoader.Load(dict.Path));
                            tasks.Add(taskJMnedict);
                        }

                        break;
                    case DictType.Kanjidic:
                        // KANJIDIC
                        if (!Dicts.dicts[DictType.Kanjidic].Contents.Any())
                        {
                            var taskKanjidict = Task.Run(() => KanjiInfoLoader.Load(dict.Path));
                            tasks.Add(taskKanjidict);
                        }

                        break;
                    case DictType.UnknownEpwing:
                        if (!Dicts.dicts[DictType.UnknownEpwing].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Daijirin:
                        if (!Dicts.dicts[DictType.Daijirin].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Daijisen:
                        if (!Dicts.dicts[DictType.Daijisen].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Kojien:
                        if (!Dicts.dicts[DictType.Kojien].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    case DictType.Meikyou:
                        if (!Dicts.dicts[DictType.Meikyou].Contents.Any())
                        {
                            var taskEpwing = Task.Run(async () =>
                                await EpwingJsonLoader.Loader(dict.Type, dict.Path));
                            tasks.Add(taskEpwing);
                        }

                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            foreach ((DictType _, Dict dict) in Dicts.dicts)
            {
                if (!dict.Active && dict.Contents.Any())
                {
                    dict.Contents.Clear();
                }
            }

            // load new freqlist if necessary
            if (Dicts.dicts[DictType.JMdict]?.Contents.Any() ?? false)
            {
                Dicts.dicts[DictType.JMdict].Contents.TryGetValue("俺", out List<IResult> freqTest1);
                Debug.Assert(freqTest1 != null, nameof(freqTest1) + " != null");

                var freqTest = freqTest1.Cast<JMdictResult>().ToList();

                if (!freqTest[0].FrequencyDict.TryGetValue(FrequencyList, out int _))
                {
                    var taskNewFreqlist = Task.Run(async () =>
                    {
                        FrequencyLoader.AddToJMdict($"{FrequencyList}", await FrequencyLoader.LoadJson(Path.Join(
                            ApplicationPath,
                            freqListPath)));
                    });
                    tasks.Add(taskNewFreqlist);

                    Debug.WriteLine("Banzai! (changed freqlist)");
                }
            }

            // foreach ((DictType _, Dict dict) in Dicts.dicts.Where(d => d.Value.Active))
            // {
            //     Debug.WriteLine("Loading " + dict.Type);
            // }

            foreach (Task task in tasks)
            {
                task.Wait();
            }

            // TODO: doesn't seem to compact when (many?) new dicts are added from the PreferencesWindow
            Debug.WriteLine("Starting compacting GC run");
            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
            GC.GetTotalMemory(true);
            GC.Collect();
        }
    }
}