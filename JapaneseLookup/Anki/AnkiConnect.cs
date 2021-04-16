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
        private static readonly Uri Uri = new("http://127.0.0.1:8765");

        public static async Task<Response> GetDeckNames()
        {
            var req = new Request("deckNames", 6);
            return await Send(req);
        }

        public static async Task<Response> AddNoteToDeck(Note note)
        {
            var req = new Request("addNote", 6, new Dictionary<string, object> {{"note", note}});
            return await Send(req);
        }

        public static async Task<Response> GetModelFieldNames(string modelName)
        {
            var req = new Request("modelFieldNames", 6, new Dictionary<string, object> {{"modelName", modelName}});
            return await Send(req);
        }

        private static async Task<Response> Send(Request req)
        {
            try
            {
                // couldn't get this to work
                // var response = await Client.PostAsJsonAsync(Uri, req);

                // AnkiConnect doesn't like null values
                var payload = new StringContent(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions {IgnoreNullValues = true}));
                Debug.WriteLine("Sending: " + await payload.ReadAsStringAsync());

                var postResponse = await Client.PostAsync(Uri, payload);

                var json = await postResponse.Content.ReadFromJsonAsync<Response>();
                Debug.WriteLine("json result: " + json!.result);

                // TODO: Dedicated error logging/display mechanism
                // all console statements need to be converted to that ^
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
                Console.WriteLine(e); // this should be Debug.WriteLine after we're done developing
                return null;
            }
        }
    }
}