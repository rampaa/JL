using System;
using System.Collections.Generic;

namespace JapaneseLookup.Anki
{
    public static class Mining
    {
        // TODO: Customizable fields
        // TODO: HTML + CSS for notes
        public static async void Mine(string word, string reading, string gloss, string context)
        {
            var deckName = "JLDeck";
            var modelName = "JL-Basic";

            var front = word;
            var back = $"{reading}<br>{gloss}<br>{context}<br>";
            var fields = new Dictionary<string, string> {{"Front", front}, {"Back", back}};

            Dictionary<string, object> options = null;
            string[] tags = {"JL"};

            var audioField = "Audio";
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
                        "7e2c2f954ef6051373ba916f000168dc"
                    },
                    {
                        "fields",
                        new[] {audioField}
                    },
                }
            };
            Dictionary<string, object>[] video = null;
            Dictionary<string, object>[] picture = null;

            var note = new Note(deckName, modelName, fields, options, tags, audio, video, picture);
            var response = await AnkiConnect.AddNoteToDeck(note);
            Console.WriteLine(response == null ? $"Mining failed for {word}" : $"Mined {word}");
        }
    }
}