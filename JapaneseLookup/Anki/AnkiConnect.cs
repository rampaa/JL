using System;
using System.Collections.Generic;
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

        private static async Task<Response> Send(Request req)
        {
            try
            {
                // couldn't get this to work
                // var response = await Client.PostAsJsonAsync(Uri, req);

                // AnkiConnect doesn't like null values
                var payload = new StringContent(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions {IgnoreNullValues = true}));
                Console.WriteLine("Sending: " + await payload.ReadAsStringAsync());

                var postResponse = await Client.PostAsync(Uri, payload);

                var json = await postResponse.Content.ReadFromJsonAsync<Response>();
                Console.WriteLine("json result: " + json.result);

                // TODO: Dedicated error logging/display mechanism
                if (json.error == null) return json;
                switch (json.error.ToString())
                {
                    case "cannot create note because it is a duplicate":
                        Console.WriteLine("error: duplicate note");
                        break;
                    default:
                        Console.WriteLine("error: unspecified, see below");
                        Console.WriteLine(json.error);
                        break;
                }

                return null;
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Communication error: Is Anki open?");
                Console.WriteLine(e);
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}