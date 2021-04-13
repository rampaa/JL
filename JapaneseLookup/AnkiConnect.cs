using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace JapaneseLookup
{
    public static class AnkiConnect
    {
        private static readonly HttpClient Client = new();
        private static readonly Uri Uri = new("http://127.0.0.1:8765");

        public static async Task<Response> Send(Request req)
        {
            try
            {
                // couldn't get this to work
                // var response = await Client.PostAsJsonAsync(Uri, req);
                
                //AnkiConnect doesn't like "params":null, so we strip it
                var payload = new StringContent(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions {IgnoreNullValues = true}));
                Console.WriteLine("Sending: " + await payload.ReadAsStringAsync());
                var response = await Client.PostAsync(Uri, payload);

                var json = await response.Content.ReadFromJsonAsync<Response>();
                Console.WriteLine("json result: " + json.result);

                // TODO: we need a dedicated error logging/display mechanism
                if (json.error != null)
                {
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
                }

                return json;
            }
            catch (HttpRequestException)
            {
                Console.WriteLine("Open Anki you noob");
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