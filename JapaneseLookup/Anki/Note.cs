using System.Collections.Generic;

namespace JapaneseLookup.Anki
{
    public class Note
    {
        // camelCase property names because AnkiConnect
        #pragma warning disable IDE1006

        public string deckName { get; set; }

        public string modelName { get; set; }

        public Dictionary<string, object> fields { get; set; }

        public Dictionary<string, object> options { get; set; }

        public string[] tags { get; set; }

        public Dictionary<string, object>[] audio { get; set; }

        public Dictionary<string, object>[] video { get; set; }

        public Dictionary<string, object>[] picture { get; set; }

        public Note(
            string deckName,
            string modelName,
            Dictionary<string, object> fields,
            Dictionary<string, object> options,
            string[] tags,
            Dictionary<string, object>[] audio,
            Dictionary<string, object>[] video,
            Dictionary<string, object>[] picture
        )
        {
            this.deckName = deckName;
            this.modelName = modelName;
            this.fields = fields;
            this.options = options;
            this.tags = tags;
            this.audio = audio;
            this.video = video;
            this.picture = picture;
        }
    }
}