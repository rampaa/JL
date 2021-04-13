using System.Collections.Generic;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    public static class Mining
    {
        public static async Task<Response> GetDeckNames()
        {
            // var req = "{\"action\":\"deckNames\",\"version\":6}";
            var req = new Request("deckNames", 6);
            return await AnkiConnect.Send(req);
        }

        public static async Task<Response> AddNoteToDeck(Note note)
        {
            // var req = $"{{\"action\":\"addNote\",\"version\":6,\"params\":{{\"note\":{JsonSerializer.Serialize(note)}}}}}";
            var req = new Request("addNote", 6, new Dictionary<string, object> {{"note", note}});
            return await AnkiConnect.Send(req);
        }
    }
}