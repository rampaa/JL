using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace JapaneseLookup.Anki
{
    public static class AnkiConnect
    {
        private static readonly HttpClient Client = new();

        private static readonly Uri AnkiConnectUri = new(ConfigManager.AnkiConnectUri);

        public static async Task<Response> AddNoteToDeck(Note note)
        {
            var req = new Request("addNote", 6, new Dictionary<string, object> {{"note", note}});
            return await Send(req);
        }

        public static async Task<Response> GetDeckNames()
        {
            var req = new Request("deckNames", 6);
            return await Send(req);
        }

        public static async Task<Response> GetModelNames()
        {
            var req = new Request("modelNames", 6);
            return await Send(req);
        }

        public static async Task<Response> GetModelFieldNames(string modelName)
        {
            var req = new Request("modelFieldNames", 6, new Dictionary<string, object> {{"modelName", modelName}});
            return await Send(req);
        }

        public static async Task<Response> StoreMediaFile(string filename, string data)
        {
            var req = new Request("storeMediaFile", 6,
                new Dictionary<string, object> {{"filename", filename}, {"data", data}});
            return await Send(req);
        }

        public static async Task<string> GetAudio(string foundSpelling, string reading)
        {
            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );
            var getResponse = await Client.GetAsync(uri);

            //  var filename = "JL_" + foundSpelling + "_" + reading + ".mp3";

            var base64 = Convert.ToBase64String(await getResponse.Content.ReadAsByteArrayAsync());
            return base64;
            //  await StoreMediaFile(filename, data);
        }

        private static async Task<Response> Send(Request req)
        {
            try
            {
                // AnkiConnect doesn't like null values
                var payload = new StringContent(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions {IgnoreNullValues = true}));
                Debug.WriteLine("Sending: " + await payload.ReadAsStringAsync());

                var postResponse = await Client.PostAsync(AnkiConnectUri, payload);

                var json = await postResponse.Content.ReadFromJsonAsync<Response>();
                Debug.WriteLine("json result: " + json!.result);

                if (json!.error == null) return json;

                Console.WriteLine(json.error.ToString());
                return null;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Communication error: Is Anki open?");
                Debug.WriteLine(e);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine("Communication error: Unknown error");
                Debug.WriteLine(e);
                return null;
            }
        }
    }
}