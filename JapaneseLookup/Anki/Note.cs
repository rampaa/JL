using System.Collections.Generic;

namespace JapaneseLookup
{
    public class Note
    {
        // camelCase property names because AnkiConnect
        #pragma warning disable IDE1006
        public string deckName { get; set; }
        public string modelName { get; set; }
        public Dictionary<string, string> fields { get; set; }
        public string[] tags { get; set; }

        public Note(string deckName, string modelName, Dictionary<string, string> fields, string[] tags)
        {
            this.deckName = deckName;
            this.modelName = modelName;
            this.fields = fields;
            this.tags = tags;
        }
    }
}