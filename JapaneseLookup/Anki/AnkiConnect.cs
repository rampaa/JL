using JapaneseLookup.Utilities;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JapaneseLookup.Anki
{
    public static class AnkiConnect
    {
        public static async Task<Response> AddNoteToDeck(Note note)
        {
            var req = new Request("addNote", 6, new Dictionary<string, object> { { "note", note } });
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response> GetDeckNames()
        {
            var req = new Request("deckNames", 6);
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response> GetModelNames()
        {
            var req = new Request("modelNames", 6);
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response> GetModelFieldNames(string modelName)
        {
            var req = new Request("modelFieldNames", 6, new Dictionary<string, object> { { "modelName", modelName } });
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response> StoreMediaFile(string filename, string data)
        {
            var req = new Request("storeMediaFile", 6,
                new Dictionary<string, object> { { "filename", filename }, { "data", data } });
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<Response> Sync()
        {
            var req = new Request("sync", 6);
            return await Send(req).ConfigureAwait(false);
        }

        public static async Task<string> GetAudio(string foundSpelling, string reading)
        {
            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );
            var getResponse = await ConfigManager.Client.GetAsync(uri).ConfigureAwait(false);

            //  var filename = "JL_audio" + foundSpelling + "_" + reading + ".mp3";

            var base64 = Convert.ToBase64String(await getResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false));
            return base64;
            //  await StoreMediaFile(filename, data);
        }

        public static async Task<bool> CheckAudioField(long noteId, string audioFieldName)
        {
            var req = new Request("notesInfo", 6,
                new Dictionary<string, object> { { "notes", new List<long> { noteId } } });

            Response response = await Send(req).ConfigureAwait(false);
            if (response != null)
            {
                var fields =
                    JsonSerializer.Deserialize<List<NotesInfoResult>>(
                        response.Result.ToString()!)![0].Fields;

                return fields[audioFieldName]["value"].ToString() != "";
            }
            else
            {
                Utils.Alert(AlertLevel.Error, "Error checking audio field");
                Utils.Logger.Error("Error checking audio field");
                return false;
            }
        }

        private static async Task<Response> Send(Request req)
        {
            try
            {
                // AnkiConnect doesn't like null values
                var payload = new StringContent(JsonSerializer.Serialize(req,
                    new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull }));
                Utils.Logger.Information("Sending: " + await payload.ReadAsStringAsync().ConfigureAwait(false));

                var postResponse = await ConfigManager.Client.PostAsync(ConfigManager.AnkiConnectUri, payload)
                    .ConfigureAwait(false);

                var json = await postResponse.Content.ReadFromJsonAsync<Response>().ConfigureAwait(false);
                Utils.Logger.Information("json result: " + json!.Result);

                if (json!.Error == null) return json;

                Utils.Alert(AlertLevel.Error, json.Error.ToString());
                Utils.Logger.Error(json.Error.ToString());
                return null;
            }
            catch (HttpRequestException e)
            {
                Utils.Alert(AlertLevel.Error, "Communication error: Is Anki open?");
                Utils.Logger.Error(e, "Communication error: Is Anki open?");
                return null;
            }
            catch (Exception e)
            {
                Utils.Alert(AlertLevel.Error, "Communication error: Unknown error");
                Utils.Logger.Error(e, "Communication error: Unknown error");
                return null;
            }
        }
    }
}