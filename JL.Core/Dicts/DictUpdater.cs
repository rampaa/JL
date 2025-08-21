using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Core.WordClass;

namespace JL.Core.Dicts;

public static class DictUpdater
{
    internal static async Task<bool> DownloadBuiltInDict(string dictPath, Uri dictDownloadUri, string dictName,
        bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || noPrompt || Utils.Frontend.ShowYesNoDialog($"Do you want to download the latest version of {dictName}?",
                    isUpdate ? "Update dictionary?" : "Download dictionary?"))
            {
                using HttpRequestMessage request = new(HttpMethod.Get, dictDownloadUri);

                string fullDictPath = Path.GetFullPath(dictPath, Utils.ApplicationPath);
                if (File.Exists(fullDictPath))
                {
                    request.Headers.IfModifiedSince = File.GetLastWriteTime(fullDictPath);
                }

                if (!noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"This may take a while. Please don't shut down the program until {dictName} is downloaded.", "Info");
                }

                using HttpResponseMessage response = await NetworkUtils.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    string tempDictPath = GetTempPath(fullDictPath);
                    await using (responseStream.ConfigureAwait(false))
                    {
                        await DecompressGzipStream(responseStream, tempDictPath).ConfigureAwait(false);
                    }

                    File.Move(tempDictPath, fullDictPath, true);

                    if (!noPrompt)
                    {
                        Utils.Frontend.ShowOkDialog($"{dictName} has been downloaded successfully.", "Info");
                    }

                    return true;
                }

                if (response.StatusCode is HttpStatusCode.NotModified && !noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"{dictName} is up to date.", "Info");
                }

                else
                {
                    Utils.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}",
                        dictName, response.StatusCode);

                    if (!noPrompt)
                    {
                        Utils.Frontend.ShowOkDialog($"Unexpected error while downloading {dictName}.", "Info");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Utils.Frontend.ShowOkDialog($"Unexpected error while downloading {dictName}.", "Info");
            Utils.Logger.Error(ex, "Unexpected error while downloading {DictName}", dictName);

            string tempDictPath = GetTempPath(Path.GetFullPath(dictPath, Utils.ApplicationPath));
            if (File.Exists(tempDictPath))
            {
                File.Delete(tempDictPath);
            }
        }

        return false;
    }

    private static async Task DecompressGzipStream(Stream stream, string filePath)
    {
        FileStream decompressedFileStream = File.Create(filePath);
        await using (decompressedFileStream.ConfigureAwait(false))
        {
            GZipStream decompressionStream = new(stream, CompressionMode.Decompress);
            await using (decompressionStream.ConfigureAwait(false))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream).ConfigureAwait(false);
            }
        }
    }

    internal static async Task<bool> DownloadYomichanDict(Dict dict, bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || noPrompt || Utils.Frontend.ShowYesNoDialog($"Do you want to download the latest version of {dict.Name}?",
                isUpdate ? "Update dictionary?" : "Download dictionary?"))
            {
                using HttpRequestMessage indexRequest = new(HttpMethod.Get, dict.Url);

                string fullDictPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
                if (Directory.Exists(fullDictPath))
                {
                    indexRequest.Headers.IfModifiedSince = File.GetLastWriteTime(Path.Join(fullDictPath, "index.json"));
                }

                using HttpResponseMessage indexResponse = await NetworkUtils.Client.SendAsync(indexRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (indexResponse.StatusCode is HttpStatusCode.NotModified && !noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"{dict.Name} is up to date.", "Info");
                    return false;
                }

                if (!indexResponse.IsSuccessStatusCode)
                {
                    Utils.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}",
                        dict.Name, indexResponse.StatusCode);

                    if (!noPrompt)
                    {
                        Utils.Frontend.ShowOkDialog($"Unexpected error while downloading {dict.Name}.", "Info");
                    }

                    return false;
                }

                if (!noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"This may take a while. Please don't shut down the program until {dict.Name} is downloaded.", "Info");
                }

                JsonElement indexJsonElement = await indexResponse.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                string? newRevision = indexJsonElement.GetProperty("revision").GetString();
                Debug.Assert(newRevision is not null);
                if (dict.Revision == newRevision)
                {
                    Utils.Frontend.ShowOkDialog($"{dict.Name} is up to date.", "Info");
                    return false;
                }

                string? downloadUrl = indexJsonElement.GetProperty("downloadUrl").GetString();
                Debug.Assert(downloadUrl is not null);
                using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
                request.Headers.IfModifiedSince = indexRequest.Headers.IfModifiedSince;

                using HttpResponseMessage response = await NetworkUtils.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.NotModified && !noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"{dict.Name} is up to date.", "Info");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    Utils.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", dict.Name, response.StatusCode);

                    if (!noPrompt)
                    {
                        Utils.Frontend.ShowOkDialog($"Unexpected error while downloading {dict.Name}.", "Info");
                    }

                    return false;
                }

                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                string tempDictPath = GetTempPath(fullDictPath);
                await using (responseStream.ConfigureAwait(false))
                {
                    await DecompressZipStream(responseStream, tempDictPath).ConfigureAwait(false);
                }

                if (Directory.Exists(fullDictPath))
                {
                    Directory.Delete(fullDictPath, true);
                }

                Directory.Move(tempDictPath, fullDictPath);

                if (!noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"{dict.Name} has been downloaded successfully.", "Info");
                }

                return true;
            }
        }

        catch (Exception ex)
        {
            Utils.Frontend.ShowOkDialog($"Unexpected error while downloading {dict.Name}.", "Info");
            Utils.Logger.Error(ex, "Unexpected error while downloading {DictName}", dict.Name);

            string tempDictPath = GetTempPath(Path.GetFullPath(dict.Path, Utils.ApplicationPath));
            if (Directory.Exists(tempDictPath))
            {
                Directory.Delete(tempDictPath, true);
            }
        }

        return false;
    }

    private static async Task DecompressZipStream(Stream stream, string destinationDirectory)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false);
        foreach (ZipArchiveEntry entry in archive.Entries)
        {
            string fullPath = Path.Join(destinationDirectory, entry.FullName);
            if (string.IsNullOrEmpty(entry.Name))
            {
                _ = Directory.CreateDirectory(fullPath);
            }
            else
            {
                string? directoryName = Path.GetDirectoryName(fullPath);
                Debug.Assert(directoryName is not null);
                _ = Directory.CreateDirectory(directoryName);

                Stream entryStream = entry.Open();
                await using (entryStream.ConfigureAwait(false))
                {
                    FileStream outputFileStream = File.Create(fullPath);
                    await using (outputFileStream.ConfigureAwait(false))
                    {
                        await entryStream.CopyToAsync(outputFileStream).ConfigureAwait(false);
                    }
                }
            }
        }
    }

    public static async Task UpdateJmdict(bool isUpdate, bool noPrompt)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];

        if (dict.Updating)
        {
            return;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);

        bool downloaded = await DownloadBuiltInDict(dict.Path,
                uri,
                nameof(DictType.JMdict), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(450000, StringComparer.Ordinal);

            await Task.Run(() => JmdictLoader.Load(dict)).ConfigureAwait(false);

            await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
            await JmdictWordClassUtils.Load().ConfigureAwait(false);

            string dbPath = DBUtils.GetDictDBPath(dict.Name);
            bool useDB = dict.Options.UseDB.Value;
            bool dbExists = File.Exists(dbPath);

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
            }

            if (useDB || dbExists)
            {
                await Task.Run(() =>
                {
                    JmdictDBManager.CreateDB(dict.Name);
                    JmdictDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            }

            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
        }

        dict.Updating = false;
        Utils.ClearStringPoolIfDictsAreReady();
    }

    public static async Task UpdateJmnedict(bool isUpdate, bool noPrompt)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMnedict];

        if (dict.Updating)
        {
            return;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);

        bool downloaded = await DownloadBuiltInDict(dict.Path,
                uri,
                nameof(DictType.JMnedict), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(620000, StringComparer.Ordinal);

            await Task.Run(() => JmnedictLoader.Load(dict)).ConfigureAwait(false);

            string dbPath = DBUtils.GetDictDBPath(dict.Name);
            bool useDB = dict.Options.UseDB.Value;
            bool dbExists = File.Exists(dbPath);

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
            }

            if (useDB || dbExists)
            {
                await Task.Run(() =>
                {
                    JmnedictDBManager.CreateDB(dict.Name);
                    JmnedictDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            }

            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
        }

        dict.Updating = false;
        Utils.ClearStringPoolIfDictsAreReady();
    }

    public static async Task UpdateKanjidic(bool isUpdate, bool noPrompt)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[DictType.Kanjidic];

        if (dict.Updating)
        {
            return;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);

        bool downloaded = await DownloadBuiltInDict(dict.Path,
                uri,
                nameof(DictType.Kanjidic), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);

            await Task.Run(() => KanjidicLoader.Load(dict)).ConfigureAwait(false);

            string dbPath = DBUtils.GetDictDBPath(dict.Name);
            bool useDB = dict.Options.UseDB.Value;
            bool dbExists = File.Exists(dbPath);

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
            }

            if (useDB || dbExists)
            {
                await Task.Run(() =>
                {
                    KanjidicDBManager.CreateDB(dict.Name);
                    KanjidicDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            }

            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
        }

        dict.Updating = false;
        Utils.ClearStringPoolIfDictsAreReady();
    }

    public static async Task UpdateYomichanDict(Dict dict, bool isUpdate, bool noPrompt)
    {
        if (dict.Updating)
        {
            return;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);

        bool downloaded = await DownloadYomichanDict(dict, isUpdate, noPrompt).ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);

            await Task.Run(async () =>
            {
                if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificNameYomichan or DictType.NonspecificKanjiWithWordSchemaYomichan or DictType.NonspecificYomichan)
                {
                    await EpwingYomichanLoader.Load(dict).ConfigureAwait(false);
                }
                else if (dict.Type is DictType.NonspecificKanjiYomichan)
                {
                    await YomichanKanjiLoader.Load(dict).ConfigureAwait(false);
                }
                else if (dict.Type is DictType.PitchAccentYomichan)
                {
                    await YomichanPitchAccentLoader.Load(dict).ConfigureAwait(false);
                }
            }).ConfigureAwait(false);

            string dbPath = DBUtils.GetDictDBPath(dict.Name);
            bool useDB = dict.Options.UseDB.Value;
            bool dbExists = File.Exists(dbPath);

            if (dbExists)
            {
                DBUtils.DeleteDB(dbPath);
            }

            if (useDB || dbExists)
            {
                await Task.Run(() =>
                {
                    if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificNameYomichan or DictType.NonspecificKanjiWithWordSchemaYomichan or DictType.NonspecificYomichan)
                    {
                        EpwingYomichanDBManager.CreateDB(dict.Name);
                        EpwingYomichanDBManager.InsertRecordsToDB(dict);
                    }
                    else if (dict.Type is DictType.NonspecificKanjiYomichan)
                    {
                        YomichanKanjiDBManager.CreateDB(dict.Name);
                        YomichanKanjiDBManager.InsertRecordsToDB(dict);
                    }
                    else if (dict.Type is DictType.PitchAccentYomichan)
                    {
                        YomichanPitchAccentDBManager.CreateDB(dict.Name);
                        YomichanPitchAccentDBManager.InsertRecordsToDB(dict);
                    }
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            }

            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
        }

        dict.Updating = false;
        Utils.ClearStringPoolIfDictsAreReady();
    }

    internal static Task AutoUpdateDicts()
    {
        List<Task> tasks = [];
        foreach (Dict dict in DictUtils.Dicts.Values.ToArray())
        {
            if (!dict.Active || !dict.AutoUpdatable)
            {
                continue;
            }

            Debug.Assert(dict.Options.AutoUpdateAfterNDays is not null);
            int dueDate = dict.Options.AutoUpdateAfterNDays.Value;
            if (dueDate is 0)
            {
                continue;
            }

            string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
            if (DictUtils.YomichanDictTypes.Contains(dict.Type))
            {
                fullPath = Path.Join(fullPath, "index.json");
            }

            bool pathExists = File.Exists(fullPath);
            if (pathExists && (DateTime.Now - File.GetLastWriteTime(fullPath)).Days < dueDate)
            {
                continue;
            }

            Utils.Frontend.Alert(AlertLevel.Information, $"Updating {dict.Type}...");
            tasks.Add(dict.Type is DictType.JMdict
                ? UpdateJmdict(pathExists, true)
                : dict.Type is DictType.JMnedict
                    ? UpdateJmnedict(pathExists, true)
                    : dict.Type is DictType.Kanjidic
                        ? UpdateKanjidic(pathExists, true)
                        : UpdateYomichanDict(dict, pathExists, true));
        }

        return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
    }

    private static string GetTempPath(string path)
    {
        return $"{path}.tmp";
    }

}
