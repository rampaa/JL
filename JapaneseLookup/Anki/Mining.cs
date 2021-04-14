using System;
using System.Collections.Generic;

namespace JapaneseLookup.Anki
{
    public static class Mining
    {
        // TODO: Customizable fields
        public static async void Mine(string word, string reading, string gloss, string context)
        {
            var deckName = "JLDeck";
            var modelName = "Basic";

            var front = word;
            var back = $"{reading}<br>{gloss}<br>{context}<br>";
            var fields = new Dictionary<string, string> {{"Front", front}, {"Back", back}};

            Dictionary<string, object> options = null;
            string[] tags = {"JL"};
            Dictionary<string, object>[] audio =
            {
                new()
                {
                    {
                        "url",
                        $"http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={word}&kana={reading}"
                    },
                    {
                        "filename",
                        $"JL_{word}_{reading}.mp3"
                    },
                    {
                        "skipHash",
                        null
                    },
                    {
                        "fields",
                        new[] {"Back"}
                    },
                }
            };
            Dictionary<string, object>[] video = null;
            Dictionary<string, object>[] picture = null;

            var result =
                await AnkiConnect.AddNoteToDeck(
                    new Note(deckName, modelName, fields, options, tags, audio, video, picture));
            if (result == null)
            {
                Console.WriteLine($"Mining failed for {word}");
            }
        }
    }
}