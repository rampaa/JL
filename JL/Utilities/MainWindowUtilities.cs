using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
using System.Text.RegularExpressions;

namespace JL.Utilities
{
    public static class MainWindowUtilities
    {
        public static readonly List<string> Backlog = new();
        public static readonly string FakeFrequency = int.MaxValue.ToString();

        public static readonly Regex JapaneseRegex =
            new(
                @"[\u2e80-\u2eff\u3000-\u303f\u3040-\u309f\u30a0-\u30ff\u31c0-\u31ef\u31f0-\u31ff\u3200-\u32ff\u3300-\u33ff\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff\ufe30-\ufe4f\uff00-\uffef]|[\ud82c-\ud82c][\udc00-\udcff]|[\ud840-\ud869][\udc00-\udedf]|[\ud869-\ud86d][\udf00-\udf3f]|[\ud86e-\ud873][\udc20-\udeaf]|[\ud873-\ud87a][\udeb0-\udfef]|[\ud87e-\ude1f][\udc00-\ude1f]|[\ud880-\ud884][\udc00-\udf4f]");

        // Consider checking for \t, \r, "　", " ", ., !, ?, –, —, ―, ‒, ~, ‥, ♪, ～, ♡, ♥, ☆, ★
        private static readonly List<string> s_japanesePunctuation =
            new()
            {
                "。",
                "！",
                "？",
                "…",
                ".",
                "、",
                "「",
                "」",
                "『",
                "』",
                "（",
                "）",
                "\n"
            };

        public static int FindWordBoundary(string text, int position)
        {
            int endPosition = -1;

            foreach (string punctuation in s_japanesePunctuation)
            {
                int tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

                if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                    endPosition = tempIndex;
            }

            if (endPosition == -1)
                endPosition = text.Length;

            return endPosition;
        }

        public static void InitializeMainWindow()
        {
            if (!File.Exists(Path.Join(ConfigManager.ApplicationPath, "Config/dicts.json")))
                Utils.CreateDefaultDictsConfig();

            if (!File.Exists("Resources/custom_words.txt"))
                File.Create("Resources/custom_words.txt").Dispose();

            if (!File.Exists("Resources/custom_names.txt"))
                File.Create("Resources/custom_names.txt").Dispose();

            Utils.DeserializeDicts().ContinueWith(_ =>
            {
                Storage.LoadDictionaries().ContinueWith(_ =>
                    {
                        Storage.InitializePoS().ContinueWith(_ =>
                        {
                            GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                        }).ConfigureAwait(false);
                    }
                ).ConfigureAwait(false);
            }).ConfigureAwait(false);

            ConfigManager.ApplyPreferences();
        }
    }
}
