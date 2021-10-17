using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using JapaneseLookup.GUI;

namespace JapaneseLookup.Utilities
{
    public static class MainWindowUtilities
    {
        public static readonly List<string> Backlog = new();
        public const string FakeFrequency = "1000000";

        public static readonly Regex JapaneseRegex =
            new(
                @"[\u2e80-\u2eff\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\u31c0-\u31ef\u31f0-\u31ff\u3200-\u32ff\u3300-\u33ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|[\ud82c-\ud82c][\udc00-\udcff]|[\ud840-\ud869][\udc00-\udedf]|[\ud869-\ud86d][\udf00-\udf3f]|[\ud86e-\ud873][\udc20-\udeaf]|[\ud873-\ud87a][\udeb0-\udfef]|[\ud87e-\ude1f][\udc00-\ude1f]|[\ud880-\ud884][\udc00-\udf4f]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> JapanesePunctuation =
            new() { "。", "！", "？", "…", "―", ".", "＆", "、", "「", "」", "『", "』", "（", "）", "\n" };

        public static void MainWindowInitializer()
        {
            // todo
            // init AnkiConnect so that it doesn't block later
            // Task.Run(AnkiConnect.GetDeckNames);
        }

        public static int FindWordBoundary(string text, int position)
        {
            int endPosition = -1;

            foreach (string punctuation in JapanesePunctuation)
            {
                int tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                    endPosition = tempIndex;
            }

            if (endPosition == -1)
                endPosition = text.Length;

            return endPosition;
        }

        public static void ShowAddNameWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            var addNameWindowInstance = AddNameWindow.Instance;
            addNameWindowInstance.SpellingTextBox.Text = mainWindow.MainTextBox.SelectedText;
            addNameWindowInstance.ShowDialog();
        }

        public static void ShowAddWordWindow()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            var addWordWindowInstance = AddWordWindow.Instance;
            addWordWindowInstance.SpellingsTextBox.Text = mainWindow.MainTextBox.SelectedText;
            addWordWindowInstance.ShowDialog();
        }

        public static void ShowPreferencesWindow()
        {
            ConfigManager.LoadPreferences(PreferencesWindow.Instance);
            PreferencesWindow.Instance.ShowDialog();
        }

        public static void SearchWithBrowser()
        {
            var mainWindow = Application.Current.Windows.OfType<MainWindow>().First();
            if (mainWindow.MainTextBox.SelectedText.Length > 0)
                Process.Start(new ProcessStartInfo("cmd",
                        $"/c start https://www.google.com/search?q={mainWindow.MainTextBox.SelectedText}^&hl=ja")
                    { CreateNoWindow = true });
        }
    }
}