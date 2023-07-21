using System.Net.Http.Json;
using System.Text.Json;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.Anki;

internal static class AnkiConnect
{
    public static async Task<Response?> AddNoteToDeck(Note note)
    {
        Request req = new("addNote", 6, new Dictionary<string, object> { { "note", note } });
        return await Send(req).ConfigureAwait(false);
    }

    public static async Task<Response?> GetDeckNamesResponse()
    {
        Request req = new("deckNames", 6);
        return await Send(req).ConfigureAwait(false);
    }

    public static async Task<Response?> GetModelNamesResponse()
    {
        Request req = new("modelNames", 6);
        return await Send(req).ConfigureAwait(false);
    }

    public static async Task<Response?> GetModelFieldNamesResponse(string modelName)
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

    public static async Task Sync()
    {
        Request req = new("sync", 6);
        _ = await Send(req).ConfigureAwait(false);
    }

    private static async Task<Response?> Send(Request req)
    {
        try
        {
            // AnkiConnect doesn't like null values
            using StringContent payload = new(JsonSerializer.Serialize(req, Utils.s_defaultJso));
            Utils.Logger.Information("Sending: {Payload}", await payload.ReadAsStringAsync().ConfigureAwait(false));

            using HttpResponseMessage postResponse = await Networking.Client
                .PostAsync(CoreConfig.AnkiConnectUri, payload).ConfigureAwait(false);

            if (postResponse.IsSuccessStatusCode)
            {
                Response? json = await postResponse.Content.ReadFromJsonAsync<Response>().ConfigureAwait(false);
                Utils.Logger.Information("json result: {JsonResult}", json?.Result ?? "null");

                if (json?.Error is null)
                {
                    return json;
                }

                Utils.Frontend.Alert(AlertLevel.Error, json.Error.ToString()!);
                Utils.Logger.Error("{JsonError}", json.Error.ToString());
            }

            return null;
        }
        catch (HttpRequestException ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Communication error: Is Anki open?");
            Utils.Logger.Error(ex, "Communication error: Is Anki open?");
            return null;
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Communication error: Unknown error");
            Utils.Logger.Error(ex, "Communication error: Unknown error");
            return null;
        }
    }
}
