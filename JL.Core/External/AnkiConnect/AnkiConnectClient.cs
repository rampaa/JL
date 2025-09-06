using System.Diagnostics;
using System.Net.Http.Json;
using JL.Core.Config;
using JL.Core.Network;
using JL.Core.Utilities;

namespace JL.Core.External.AnkiConnect;

internal static class AnkiConnectClient
{
    public static ValueTask<Response?> AddNoteToDeck(Note note)
    {
        RequestWithParameters<Note> req = new("addNote", 6, new Dictionary<string, Note>(1, StringComparer.Ordinal)
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
        RequestWithParameters<string> req = new("modelFieldNames", 6, new Dictionary<string, string>(1, StringComparer.Ordinal)
        {
            {
                "modelName", modelName
            }
        });
        return Send(req);
    }

    public static ValueTask<Response?> GetCanAddNotesResponse(List<Note> notes)
    {
        RequestWithParameters<List<Note>> req = new("canAddNotes", 6, new Dictionary<string, List<Note>>(1, StringComparer.Ordinal)
        {
            {
                "notes", notes
            }
        });

        return Send(req);
    }

    //public static ValueTask<Response?> StoreMediaFile(string filename, string data)
    //{
    //    Request<string> req = new("storeMediaFile", 6, new Dictionary<string, string>(2, StringComparer.Ordinal)
    //    {
    //        {
    //            "filename", filename
    //        },
    //        {
    //            "data", data
    //        }
    //    });

    //    return Send(req);
    //}

    public static async Task Sync()
    {
        Request req = new("sync", 6);
        _ = await Send(req).ConfigureAwait(false);
    }

    private static async ValueTask<Response?> Send<T>(T request) where T : Request
    {
        try
        {
            // AnkiConnect doesn't like null values
            using JsonContent content = JsonContent.Create(request, options: JsonOptions.s_jsoIgnoringWhenWritingNull);

            // AnkiConnect expects the content to be buffered
            await content.LoadIntoBufferAsync().ConfigureAwait(false);

            using HttpResponseMessage postResponse = await NetworkUtils.Client.PostAsync(CoreConfigManager.Instance.AnkiConnectUri, content).ConfigureAwait(false);

            if (!postResponse.IsSuccessStatusCode)
            {
                return null;
            }

            Response? response = await postResponse.Content.ReadFromJsonAsync<Response>().ConfigureAwait(false);
            Debug.Assert(response is not null);
            if (response.Error is null)
            {
                return response;
            }

            Utils.Frontend.Alert(AlertLevel.Error, response.Error);
            Utils.Logger.Error("{JsonError}", response.Error);

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
