using System.Collections.Frozen;
using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JL.Core.Dicts;
using JL.Core.Dicts.EPWING.Yomichan;
using JL.Core.Dicts.Interfaces;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Dicts.KanjiDict;
using JL.Core.Dicts.PitchAccent;
using JL.Core.Freqs;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Utilities.Database;
using JL.Core.Utilities.ObjectPool;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Utilities;

public static class ResourceUpdater
{
    internal static async Task<bool> DownloadBuiltInDict(string fullDictPath, Uri dictDownloadUri, string dictName,
        bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || noPrompt || await FrontendManager.Frontend.ShowYesNoDialogAsync($"Do you want to download the latest version of {dictName}?",
                    isUpdate ? "Update dictionary?" : "Download dictionary?").ConfigureAwait(false))
            {
                using HttpRequestMessage request = new(HttpMethod.Get, dictDownloadUri);
                if (File.Exists(fullDictPath))
                {
                    request.Headers.IfModifiedSince = new DateTimeOffset(File.GetLastWriteTimeUtc(fullDictPath), TimeSpan.Zero);
                }

                if (!noPrompt)
                {
                    await FrontendManager.Frontend.ShowOkDialogAsync($"This may take a while. Please don't shut down the program until {dictName} is downloaded.", "Info").ConfigureAwait(false);
                }

                using HttpResponseMessage response = await NetworkUtils.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    string tempDictPath = PathUtils.GetTempPath(fullDictPath);
                    Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await using (responseStream.ConfigureAwait(false))
                    {
                        await DecompressGzipStream(responseStream, tempDictPath).ConfigureAwait(false);
                    }

                    if (File.Exists(fullDictPath))
                    {
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(GetBackupPath(fullDictPath), fullDictPath);
                    }

                    File.Move(tempDictPath, fullDictPath, false);

                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"{dictName} has been downloaded successfully.", "Info").ConfigureAwait(false);
                    }

                    return true;
                }

                if (response.StatusCode is HttpStatusCode.NotModified)
                {
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"{dictName} is up to date.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Information, $"{dictName} is up to date.");
                    }
                }
                else
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", dictName, response.StatusCode);
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"Unexpected error while downloading {dictName}.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Unexpected error while downloading {dictName}.");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Unexpected error while downloading {DictName}", dictName);
            if (!noPrompt)
            {
                await FrontendManager.Frontend.ShowOkDialogAsync($"Unexpected error while downloading {dictName}.", "Info").ConfigureAwait(false);
            }
            else
            {
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Unexpected error while downloading {dictName}.");
            }

            string tempDictPath = PathUtils.GetTempPath(fullDictPath);
            if (File.Exists(tempDictPath))
            {
                File.Delete(tempDictPath);
            }
        }

        return false;
    }

    private static async Task DecompressGzipStream(Stream stream, string filePath)
    {
        FileStream decompressedFileStream = new(filePath, FileStreamOptionsPresets.s_asyncCreate64KBufferFso);
        await using (decompressedFileStream.ConfigureAwait(false))
        {
            GZipStream decompressionStream = new(stream, CompressionMode.Decompress);
            await using (decompressionStream.ConfigureAwait(false))
            {
                await decompressionStream.CopyToAsync(decompressedFileStream).ConfigureAwait(false);
            }
        }
    }

    private static async Task<bool> DownloadYomichanDict(Uri url, string revision, string name, string fullDictPath, bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || noPrompt || await FrontendManager.Frontend.ShowYesNoDialogAsync($"Do you want to download the latest version of {name}?",
                isUpdate ? "Update dictionary?" : "Download dictionary?").ConfigureAwait(false))
            {
                bool indexJsonExists = false;
                using HttpRequestMessage indexRequest = new(HttpMethod.Get, url);
                if (Directory.Exists(fullDictPath))
                {
                    string indexJsonPath = Path.Join(fullDictPath, "index.json");
                    if (File.Exists(indexJsonPath))
                    {
                        //FileStream fileStream = new(indexJsonPath, FileStreamOptionsPresets.s_asyncReadFso);
                        //await using (fileStream.ConfigureAwait(false))
                        //{
                        //    JsonElement tempIndexJsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
                        //    string? tempRevision = tempIndexJsonElement.GetProperty("revision").GetString();
                        //    Debug.Assert(tempRevision is not null);
                        //    revision = tempRevision;
                        //}

                        indexRequest.Headers.IfModifiedSince = new DateTimeOffset(File.GetLastWriteTimeUtc(indexJsonPath), TimeSpan.Zero);
                        indexJsonExists = true;
                    }
                }

                using HttpResponseMessage indexResponse = await NetworkUtils.Client.SendAsync(indexRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (indexJsonExists && indexResponse.StatusCode is HttpStatusCode.NotModified)
                {
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"{name} is up to date.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Information, $"{name} is up to date.");
                    }

                    return false;
                }

                if (!indexResponse.IsSuccessStatusCode)
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", name, indexResponse.StatusCode);
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"Unexpected error while downloading {name}.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Unexpected error while downloading {name}.");
                    }

                    return false;
                }

                if (!noPrompt)
                {
                    await FrontendManager.Frontend.ShowOkDialogAsync($"This may take a while. Please don't shut down the program until {name} is downloaded.", "Info").ConfigureAwait(false);
                }

                JsonElement indexJsonElement = await indexResponse.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                string? newRevision = indexJsonElement.GetProperty("revision").GetString();
                Debug.Assert(newRevision is not null);
                if (indexJsonExists && revision == newRevision)
                {
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"{name} is up to date.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Information, $"{name} is up to date.");
                    }

                    return false;
                }

                string? downloadUrl = indexJsonElement.GetProperty("downloadUrl").GetString();
                Debug.Assert(downloadUrl is not null);
                using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
                request.Headers.IfModifiedSince = indexRequest.Headers.IfModifiedSince;

                using HttpResponseMessage response = await NetworkUtils.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.NotModified)
                {
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"{name} is up to date.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Information, $"{name} is up to date.");
                    }

                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", name, response.StatusCode);
                    if (!noPrompt)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync($"Unexpected error while downloading {name}.", "Info").ConfigureAwait(false);
                    }
                    else
                    {
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Unexpected error while downloading {name}.");
                    }

                    return false;
                }

                string tempDictPath = PathUtils.GetTempPath(fullDictPath);
                Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using (responseStream.ConfigureAwait(false))
                {
                    ArchiveUtils.DecompressZipStream(responseStream, tempDictPath);
                }

                if (Directory.Exists(fullDictPath))
                {
                    string backupPath = GetBackupPath(fullDictPath);
                    if (Directory.Exists(backupPath))
                    {
                        Directory.Delete(backupPath, true);
                    }

                    Directory.Move(fullDictPath, backupPath);
                }

                Directory.Move(tempDictPath, fullDictPath);

                if (!noPrompt)
                {
                    await FrontendManager.Frontend.ShowOkDialogAsync($"{name} has been downloaded successfully.", "Info").ConfigureAwait(false);
                }

                return true;
            }
        }

        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Unexpected error while downloading {DictName}", name);
            if (!noPrompt)
            {
                await FrontendManager.Frontend.ShowOkDialogAsync($"Unexpected error while downloading {name}.", "Info").ConfigureAwait(false);
            }
            else
            {
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Unexpected error while downloading {name}.");
            }

            string tempDictPath = PathUtils.GetTempPath(fullDictPath);
            if (Directory.Exists(tempDictPath))
            {
                Directory.Delete(tempDictPath, true);
            }
        }

        return false;
    }

    private static async Task<bool> UpdateBuiltInDict(bool isUpdate, bool noPrompt, DictType dictType, string dictTypeName, int size, DictUtils.CreateDB createDB, DictUtils.ImportFromDisk importFromDisk, DictUtils.Load load)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[dictType];
        if (dict.Updating)
        {
            return false;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadBuiltInDict(fullDictPath, uri, dictTypeName, isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            bool useDB = dict.Options.UseDB.Value;
            string dbPath = dict.DBPath;
            bool dbExists = File.Exists(dbPath);
            string backupDBPath = GetBackupPath(dbPath);
            try
            {
                if (useDB)
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    if (dbExists)
                    {
                        SqliteConnection.ClearAllPools();
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(backupDBPath, dbPath);
                    }

                    await Task.Run(async () =>
                    {
                        createDB(dbPath);
                        await importFromDisk(dict).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                    if (File.Exists(backupDBPath))
                    {
                        File.Delete(backupDBPath);
                    }
                }
                else
                {
                    dict.Ready = false;
                    await Task.Run(async () =>
                    {
                        dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);
                        await load(dict).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (dbExists)
                    {
                        DBUtils.DeleteDB(dbPath);
                    }

                    if (!dict.Active)
                    {
                        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    }
                }

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (File.Exists(dictBackupPath))
                {
                    File.Delete(dictBackupPath);
                }

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");

                File.Delete(fullDictPath);
                string dictBackupPath = GetBackupPath(fullDictPath);
                if (File.Exists(dictBackupPath))
                {
                    File.Move(dictBackupPath, fullDictPath, true);
                }

                if (File.Exists(backupDBPath))
                {
                    PathUtils.ReplaceFileAtomicallyOnSameVolume(dbPath, backupDBPath);
                }

                if (!dict.Active)
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
                else if (!useDB)
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);
                            await load(dict).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }
                else if (!File.Exists(dbPath))
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            createDB(dbPath);
                            await importFromDisk(dict).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }

                return false;
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }

        dict.Ready = true;
        dict.Updating = false;
        ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        return false;
    }

    public static async Task<bool> UpdateJmdict(bool isUpdate, bool noPrompt)
    {
        bool updated = await UpdateBuiltInDict(isUpdate, noPrompt, DictType.JMdict, nameof(DictType.JMdict), JmdictLoader.Size, JmdictDBManager.CreateDB, JmdictDBManager.ImportFromDisk, JmdictLoader.Load).ConfigureAwait(false);
        if (updated)
        {
            await JmdictWordClassUtils.Serialize().ConfigureAwait(false);
            await JmdictWordClassUtils.Load().ConfigureAwait(false);

            return true;
        }

        return false;
    }

    public static Task<bool> UpdateJmnedict(bool isUpdate, bool noPrompt)
    {
        return UpdateBuiltInDict(isUpdate, noPrompt, DictType.JMnedict, nameof(DictType.JMnedict), JmnedictLoader.Size, JmnedictDBManager.CreateDB, JmnedictDBManager.ImportFromDisk, JmnedictLoader.Load);
    }

    public static Task<bool> UpdateKanjidic(bool isUpdate, bool noPrompt)
    {
        return UpdateBuiltInDict(isUpdate, noPrompt, DictType.Kanjidic, nameof(DictType.Kanjidic), KanjidicLoader.Size, KanjidicDBManager.CreateDB, KanjidicDBManager.ImportFromDisk, KanjidicLoader.Load);
    }

    private static async Task<bool> UpdateYomichanDict(bool isUpdate, bool noPrompt, DictType dictType, int size, DictUtils.CreateDB createDB, DictUtils.ImportFromDisk importFromDisk, DictUtils.Load load)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[dictType];
        if (dict.Updating)
        {
            return false;
        }

        dict.Updating = true;

        Uri? uri = dict.Url;
        Debug.Assert(uri is not null);
        Debug.Assert(dict.Revision is not null);

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadYomichanDict(uri, dict.Revision, dict.Name, fullDictPath, isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            bool useDB = dict.Options.UseDB.Value;
            string dbPath = dict.DBPath;
            bool dbExists = File.Exists(dbPath);
            string backupDBPath = GetBackupPath(dbPath);
            try
            {
                if (useDB)
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    if (dbExists)
                    {
                        SqliteConnection.ClearAllPools();
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(backupDBPath, dbPath);
                    }

                    await Task.Run(async () =>
                    {
                        createDB(dbPath);
                        await importFromDisk(dict).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (File.Exists(backupDBPath))
                    {
                        File.Delete(backupDBPath);
                    }
                }
                else
                {
                    dict.Ready = false;
                    await Task.Run(async () =>
                    {
                        dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);
                        await load(dict).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (dbExists)
                    {
                        DBUtils.DeleteDB(dbPath);
                    }

                    if (!dict.Active)
                    {
                        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    }
                }

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Delete(dictBackupPath, true);
                }

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {dict.Name}");

                Directory.Delete(fullDictPath, true);
                string dictBackupPath = GetBackupPath(fullDictPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Move(dictBackupPath, fullDictPath);
                }

                if (File.Exists(backupDBPath))
                {
                    PathUtils.ReplaceFileAtomicallyOnSameVolume(dbPath, backupDBPath);
                }

                if (!dict.Active)
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
                else if (!useDB)
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            dict.Contents = new Dictionary<string, IList<IDictRecord>>(size, StringComparer.Ordinal);
                            await load(dict).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }
                else if (!File.Exists(dbPath))
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            createDB(dbPath);
                            await importFromDisk(dict).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }

                return false;
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }

        dict.Ready = true;
        dict.Updating = false;
        ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        return false;
    }

    public static async Task<bool> UpdateYomichanDict(Dict dict, bool isUpdate, bool noPrompt)
    {
        if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificNameYomichan or DictType.NonspecificKanjiWithWordSchemaYomichan or DictType.NonspecificYomichan)
        {
            return await UpdateYomichanDict(isUpdate, noPrompt, dict.Type, EpwingYomichanDBManager.Size, EpwingYomichanDBManager.CreateDB, EpwingYomichanDBManager.ImportFromDisk, EpwingYomichanLoader.Load).ConfigureAwait(false);
        }

        if (dict.Type is DictType.NonspecificKanjiYomichan)
        {
            return await UpdateYomichanDict(isUpdate, noPrompt, dict.Type, YomichanKanjiDBManager.Size, YomichanKanjiDBManager.CreateDB, YomichanKanjiDBManager.ImportFromDisk, YomichanKanjiLoader.Load).ConfigureAwait(false);
        }

        if (dict.Type is DictType.PitchAccentYomichan)
        {
            return await UpdateYomichanDict(isUpdate, noPrompt, dict.Type, YomichanPitchAccentDBManager.Size, YomichanPitchAccentDBManager.CreateDB, YomichanPitchAccentDBManager.ImportFromDisk, YomichanPitchAccentLoader.Load).ConfigureAwait(false);
        }

        Debug.Assert(false);
        return false;
    }

    public static async Task<bool> UpdateYomichanFreqDict(Freq freq, bool isUpdate, bool noPrompt)
    {
        if (freq.Updating)
        {
            return false;
        }

        freq.Updating = true;

        Uri? uri = freq.Url;
        Debug.Assert(uri is not null);
        Debug.Assert(freq.Revision is not null);

        string fullDictPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadYomichanDict(uri, freq.Revision, freq.Name, fullDictPath, isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            bool useDB = freq.Options.UseDB.Value;
            string dbPath = freq.DBPath;
            bool dbExists = File.Exists(dbPath);
            string backupDBPath = GetBackupPath(dbPath);
            try
            {
                if (useDB)
                {
                    freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                    if (dbExists)
                    {
                        SqliteConnection.ClearAllPools();
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(backupDBPath, dbPath);
                    }

                    await Task.Run(async () =>
                    {
                        FreqDBManager.CreateDB(dbPath);
                        await FreqDBManager.ImportYomichanFreqFromDisk(freq).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (File.Exists(backupDBPath))
                    {
                        File.Delete(backupDBPath);
                    }
                }
                else
                {
                    freq.Ready = false;
                    await Task.Run(async () =>
                    {
                        freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(13108, StringComparer.Ordinal);
                        await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                    }).ConfigureAwait(false);

                    if (dbExists)
                    {
                        DBUtils.DeleteDB(dbPath);
                    }

                    if (!freq.Active)
                    {
                        freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                    }
                }

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Delete(dictBackupPath, true);
                }

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {freq.Name}");
                return true;
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", freq.Type.GetDescription(), freq.Name, fullDictPath);
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");

                Directory.Delete(fullDictPath, true);
                string dictBackupPath = GetBackupPath(fullDictPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Move(dictBackupPath, fullDictPath);
                }

                if (File.Exists(backupDBPath))
                {
                    PathUtils.ReplaceFileAtomicallyOnSameVolume(dbPath, backupDBPath);
                }

                if (!freq.Active)
                {
                    freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                }
                else if (!useDB)
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(13108, StringComparer.Ordinal);
                            await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{FreqType}'-'{FreqName}' from '{FullDictPath}'", freq.Type.GetDescription(), freq.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {freq.Name}, deactivating it");
                    }
                }
                else if (!File.Exists(dbPath))
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            FreqDBManager.CreateDB(dbPath);
                            await FreqDBManager.ImportYomichanFreqFromDisk(freq).ConfigureAwait(false);
                        }).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{FreqType}'-'{FreqName}' from '{FullDictPath}'", freq.Type.GetDescription(), freq.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {freq.Name}, deactivating it");
                    }
                }

                return false;
            }
            finally
            {
                freq.Ready = true;
                freq.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }

        freq.Ready = true;
        freq.Updating = false;
        ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        return false;
    }

    internal static Task AutoUpdateDicts()
    {
        List<Task<bool>> tasks = [];
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

            string fullPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
            if (DictUtils.YomichanDictTypes.Contains(dict.Type))
            {
                fullPath = Path.Join(fullPath, "index.json");
            }

            bool pathExists = File.Exists(fullPath);
            if (!pathExists || (DateTime.UtcNow - File.GetLastWriteTimeUtc(fullPath)).Days < dueDate)
            {
                continue;
            }

            FrontendManager.Frontend.Alert(AlertLevel.Information, $"Updating {dict.Name}...");
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

    internal static Task AutoUpdateFreqDicts()
    {
        List<Task<bool>> tasks = [];
        foreach (Freq freq in FreqUtils.FreqDicts.Values.ToArray())
        {
            if (!freq.Active || !freq.AutoUpdatable)
            {
                continue;
            }

            Debug.Assert(freq.Options.AutoUpdateAfterNDays is not null);
            int dueDate = freq.Options.AutoUpdateAfterNDays.Value;
            if (dueDate is 0)
            {
                continue;
            }

            string fullPath = Path.GetFullPath(Path.Join(freq.Path, "index.json"), AppInfo.ApplicationPath);
            bool pathExists = File.Exists(fullPath);
            if (!pathExists || (DateTime.UtcNow - File.GetLastWriteTimeUtc(fullPath)).Days < dueDate)
            {
                continue;
            }

            FrontendManager.Frontend.Alert(AlertLevel.Information, $"Updating {freq.Name}...");
            tasks.Add(UpdateYomichanFreqDict(freq, pathExists, true));
        }

        return tasks.Count > 0 ? Task.WhenAll(tasks) : Task.CompletedTask;
    }

    private static string GetBackupPath(string path)
    {
        return $"{path}.bak";
    }

    internal static void HandleLeftOverFiles(string fullPath)
    {
        string tempFilePath = PathUtils.GetTempPath(fullPath);
        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }

        string backupFilePath = GetBackupPath(fullPath);
        if (File.Exists(backupFilePath))
        {
            if (File.Exists(fullPath))
            {
                File.Delete(backupFilePath);
            }
            else
            {
                File.Move(backupFilePath, fullPath, false);
            }
        }
    }

    internal static void HandleLeftOverFolders(string fullPath)
    {
        string tempFolderPath = PathUtils.GetTempPath(fullPath);
        if (Directory.Exists(tempFolderPath))
        {
            Directory.Delete(tempFolderPath, true);
        }

        string backupFolderPath = GetBackupPath(fullPath);
        if (Directory.Exists(backupFolderPath))
        {
            if (Directory.Exists(fullPath))
            {
                Directory.Delete(backupFolderPath, true);
            }
            else
            {
                Directory.Move(backupFolderPath, fullPath);
            }
        }
    }
}
