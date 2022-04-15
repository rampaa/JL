using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Utilities;

namespace JL.Core.Anki
{
    public static class AnkiConnect
    {
        public static async Task<Response?> AddNoteToDeck(Note note)
        {
            Request req = new("addNote", 6, new Dictionary<string, object> { { "note", note } });
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response?> GetDeckNames()
        {
            Request req = new("deckNames", 6);
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response?> GetModelNames()
        {
            Request req = new("modelNames", 6);
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response?> GetModelFieldNames(string modelName)
        {
            Request req = new("modelFieldNames", 6, new Dictionary<string, object> { { "modelName", modelName } });
            return await Send(req).ConfigureAwait(false);
        }

        // public static async Task<Response> StoreMediaFile(string filename, string data)
        // {
        //     Request req = new("storeMediaFile", 6,
        //         new Dictionary<string, object> { { "filename", filename }, { "data", data } });
        //     return await Send(req).ConfigureAwait(false);
        // }

        public static async Task<Response?> Sync()
        {
            Request req = new("sync", 6);
            return await Send(req).ConfigureAwait(false);
        }

        private static async Task<Response?> Send(Request req)
        {
            try
            {
                // AnkiConnect doesn't like null values
                StringContent payload = new(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
                Utils.Logger.Information("Sending: " + await payload.ReadAsStringAsync().ConfigureAwait(false));

                HttpResponseMessage postResponse = await Storage.Client
                    .PostAsync(Storage.Frontend.CoreConfig.AnkiConnectUri, payload)
                    .ConfigureAwait(false);

                Response json = (await postResponse.Content.ReadFromJsonAsync<Response>().ConfigureAwait(false))!;
                Utils.Logger.Information("json result: " + json.Result);

                if (json.Error == null)
                    return json;

                Storage.Frontend.Alert(AlertLevel.Error, json.Error.ToString()!);
                Utils.Logger.Error(json.Error.ToString());
                return null;
            }
            catch (HttpRequestException e)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Communication error: Is Anki open?");
                Utils.Logger.Error(e, "Communication error: Is Anki open?");
                return null;
            }
            catch (Exception e)
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Communication error: Unknown error");
                Utils.Logger.Error(e, "Communication error: Unknown error");
                return null;
            }
        }
    }
}
