using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using JapaneseLookup.GUI;

namespace JapaneseLookup.Utilities
{
    public static class Utils
    {
        public static readonly Logger Logger = new LoggerConfiguration().WriteTo.File("Logs/log.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileTimeLimit: TimeSpan.FromDays(90),
                shared: true)
            .CreateLogger();

        public static IEnumerable<string> UnicodeIterator(this string s)
        {
            for (int i = 0; i < s.Length; ++i)
            {
                if (char.IsHighSurrogate(s, i)
                    && s.Length > i + 1
                    && char.IsLowSurrogate(s, i + 1))
                {
                    yield return char.ConvertFromUtf32(char.ConvertToUtf32(s, i));
                    ++i;
                }
                else
                {
                    yield return s[i].ToString();
                }
            }
        }

        public static List<ComboBoxItem> FindJapaneseFonts()
        {
            List<ComboBoxItem> japaneseFonts = new();

            foreach (FontFamily fontFamily in Fonts.SystemFontFamilies)
            {
                ComboBoxItem comboBoxItem = new();

                comboBoxItem.Content = fontFamily.Source;
                comboBoxItem.FontFamily = fontFamily;
                comboBoxItem.Foreground = Brushes.White;

                if (fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("ja-jp")))
                {
                    japaneseFonts.Add(comboBoxItem);
                }

                else if (fontFamily.FamilyNames.Keys != null && fontFamily.FamilyNames.Keys.Count == 1 &&
                         fontFamily.FamilyNames.ContainsKey(XmlLanguage.GetLanguage("en-US")))
                {
                    bool foundGlyph = false;
                    foreach (var typeFace in fontFamily.GetTypefaces())
                    {
                        if (typeFace.TryGetGlyphTypeface(out var glyphTypeFace))
                        {
                            if (glyphTypeFace.CharacterToGlyphMap.ContainsKey(20685))
                            {
                                japaneseFonts.Add(comboBoxItem);
                                foundGlyph = true;
                                break;
                            }
                        }
                    }

                    if (!foundGlyph)
                    {
                        comboBoxItem.Foreground = Brushes.DimGray;
                        japaneseFonts.Add(comboBoxItem);
                    }
                }
                else
                {
                    comboBoxItem.Foreground = Brushes.DimGray;
                    japaneseFonts.Add(comboBoxItem);
                }
            }

            return japaneseFonts;
        }

        public static bool KeyGestureComparer(KeyEventArgs e, KeyGesture keyGesture)
        {
            if (keyGesture == null)
                return false;

            if (keyGesture.Modifiers.Equals(ModifierKeys.Windows))
                return keyGesture.Key == e.Key && (Keyboard.Modifiers & ModifierKeys.Windows) == 0;
            else
                return keyGesture.Matches(null, e);
        }

        public static string KeyGestureToString(KeyGesture keyGesture)
        {
            StringBuilder keyGestureStringBuilder = new();

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Control))
            {
                keyGestureStringBuilder.Append("Ctrl+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                keyGestureStringBuilder.Append("Shift+");
            }

            if (keyGesture.Modifiers.HasFlag(ModifierKeys.Alt))
            {
                keyGestureStringBuilder.Append("Alt+");
            }

            keyGestureStringBuilder.Append(keyGesture.Key.ToString());

            return keyGestureStringBuilder.ToString();
        }

        public static void Try(Action a, object variable, string key)
        {
            try
            {
                a();
            }
            catch
            {
                Configuration config =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                if (ConfigurationManager.AppSettings.Get(key) == null)
                    config.AppSettings.Settings.Add(key, variable.ToString());
                else
                    config.AppSettings.Settings[key].Value = variable.ToString();

                config.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
        }

        public static KeyGesture KeyGestureSetter(string keyGestureName, KeyGesture keyGesture)
        {
            string rawKeyGesture = ConfigurationManager.AppSettings.Get(keyGestureName);

            if (rawKeyGesture != null)
            {
                KeyGestureConverter keyGestureConverter = new();
                if (!rawKeyGesture!.StartsWith("Ctrl+") && !rawKeyGesture.StartsWith("Shift+") &&
                    !rawKeyGesture.StartsWith("Alt+"))
                    return (KeyGesture)keyGestureConverter.ConvertFromString("Win+" + rawKeyGesture);
                else
                    return (KeyGesture)keyGestureConverter.ConvertFromString(rawKeyGesture);
            }

            else
            {
                Configuration config =
                    ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                config.AppSettings.Settings.Add(keyGestureName, KeyGestureToString(keyGesture));
                config.Save(ConfigurationSaveMode.Modified);

                return keyGesture;
            }
        }

        public static void AddToConfig(string key, string value)
        {
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings.Add(key, value);
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void KeyGestureSaver(string key, string rawKeyGesture)
        {
            Configuration config =
                ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (rawKeyGesture.StartsWith("Win+"))
                config.AppSettings.Settings[key].Value = rawKeyGesture[4..];
            else
                config.AppSettings.Settings[key].Value = rawKeyGesture;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }

        public static void Alert(AlertLevel alertLevel, string message)
        {
            Application.Current.Dispatcher.Invoke((Action)async delegate
            {
                List<AlertWindow> alertWindowList = Application.Current.Windows.OfType<AlertWindow>().ToList();

                AlertWindow alertWindow = new();

                alertWindow.Left = SystemParameters.WorkArea.Width - alertWindow.Width - 30;
                alertWindow.Top = alertWindowList.Count * ((alertWindowList.LastOrDefault()?.ActualHeight ?? 0) + 2) + 30;

                alertWindow.DisplayAlert(alertLevel, message);
                alertWindow.Show();
                await Task.Delay(4004);
                alertWindow.Close();
            });
        }
    }
}