using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using JL.Core.Config;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.Mining.Anki;

internal static class AnkiConnect
{
    public static ValueTask<Response?> AddNoteToDeck(Note note)
    {
        Request req = new("addNote", 6, new Dictionary<string, object>(1, StringComparer.Ordinal)
        {
            {
                "note", note
            }
        });
        return Send(req);
    }

    public static ValueTask<Response?> GetDeckNamesResponse()
    {
        Request req = new("deckNames", 6);
        return Send(req);
    }

    public static ValueTask<Response?> GetModelNamesResponse()
    {
        Request req = new("modelNames", 6);
        return Send(req);
    }

    public static ValueTask<Response?> GetModelFieldNamesResponse(string modelName)
    {
        Request req = new("modelFieldNames", 6, new Dictionary<string, object>(1, StringComparer.Ordinal)
        {
            {
                "modelName", modelName
            }
        });
        return Send(req);
    }

    public static ValueTask<Response?> GetCanAddNotesResponse(List<Note> notes)
    {
        Request req = new("canAddNotes", 6, new Dictionary<string, object>(1, StringComparer.Ordinal)
        {
            {
                "notes", notes
            }
        });

        return Send(req);
    }

    // public static ValueTask<Response> StoreMediaFile(string filename, string data)
    // {
    //     Request req = new("storeMediaFile", 6,
    //         new Dictionary<string, object> { { "filename", filename }, { "data", data } });
    //     return Send(req);
    // }

    public static async Task Sync()
    {
        Request req = new("sync", 6);
        _ = await Send(req).ConfigureAwait(false);
    }

    private static async ValueTask<Response?> Send(Request req)
    {
        try
        {
            // AnkiConnect doesn't like null values
            using StringContent payload = new(JsonSerializer.Serialize(req, Utils.s_jsoIgnoringWhenWritingNull));
            Utils.Logger.Information("Sending: {Payload}", await payload.ReadAsStringAsync().ConfigureAwait(false));

            using HttpResponseMessage postResponse = await NetworkUtils.Client
                .PostAsync(CoreConfigManager.Instance.AnkiConnectUri, payload).ConfigureAwait(false);

            if (!postResponse.IsSuccessStatusCode)
            {
                return null;
            }

            Response? json = await postResponse.Content.ReadFromJsonAsync<Response>().ConfigureAwait(false);
            Utils.Logger.Information("json result: {JsonResult}", json?.Result ?? "null");

            if (json?.Error is null)
            {
                return json;
            }

            string? error = json.Error.ToString();
            Debug.Assert(error is not null);

            Utils.Frontend.Alert(AlertLevel.Error, error);
            Utils.Logger.Error("{JsonError}", error);

            return null;
        }
        catch (HttpRequestException ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't connect to AnkiConnect. Please ensure Anki is open and AnkiConnect is installed.");
            Utils.Logger.Error(ex, "Couldn't connect to AnkiConnect. Please ensure Anki is open and AnkiConnect is installed.");
            return null;
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't connect to AnkiConnect");
            Utils.Logger.Error(ex, "Couldn't connect to AnkiConnect");
            return null;
        }
    }
}
