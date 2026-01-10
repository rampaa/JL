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
using JL.Core.WordClass;

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
                using HttpRequestMessage indexRequest = new(HttpMethod.Get, url);
                if (Directory.Exists(fullDictPath))
                {
                    string indexJsonPath = Path.Join(fullDictPath, "index.json");
                    if (File.Exists(indexJsonPath))
                    {
                        indexRequest.Headers.IfModifiedSince = new DateTimeOffset(File.GetLastWriteTimeUtc(indexJsonPath), TimeSpan.Zero);
                    }
                }

                using HttpResponseMessage indexResponse = await NetworkUtils.Client.SendAsync(indexRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (indexResponse.StatusCode is HttpStatusCode.NotModified)
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
                if (revision == newRevision)
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
                    await DecompressZipStream(responseStream, tempDictPath).ConfigureAwait(false);
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

    private static async Task DecompressZipStream(Stream stream, string destinationDirectory)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false);
        await archive.ExtractToDirectoryAsync(destinationDirectory).ConfigureAwait(false);
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

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadBuiltInDict(fullDictPath, uri, nameof(DictType.JMdict), isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(450000, StringComparer.Ordinal);

            try
            {
                await Task.Run(() => JmdictLoader.Load(dict)).ConfigureAwait(false);

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (File.Exists(dictBackupPath))
                {
                    File.Delete(dictBackupPath);
                }

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

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
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

                if (dict is { Active: true, Options.UseDB.Value: false })
                {
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(450000, StringComparer.Ordinal);
                    try
                    {
                        await Task.Run(() => JmdictLoader.Load(dict)).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }
                else
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }
        else
        {
            dict.Ready = true;
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
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

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadBuiltInDict(fullDictPath, uri, nameof(DictType.JMnedict), isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(620000, StringComparer.Ordinal);

            try
            {
                await Task.Run(() => JmnedictLoader.Load(dict)).ConfigureAwait(false);

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (File.Exists(dictBackupPath))
                {
                    File.Delete(dictBackupPath);
                }

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

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
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

                if (dict is { Active: true, Options.UseDB.Value: false })
                {
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(620000, StringComparer.Ordinal);
                    try
                    {
                        await Task.Run(() => JmnedictLoader.Load(dict)).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }
                else
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }
        else
        {
            dict.Ready = true;
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
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

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadBuiltInDict(fullDictPath, uri, nameof(DictType.Kanjidic), isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);

            try
            {
                await Task.Run(() => KanjidicLoader.Load(dict)).ConfigureAwait(false);

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (File.Exists(dictBackupPath))
                {
                    File.Delete(dictBackupPath);
                }

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

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
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

                if (dict is { Active: true, Options.UseDB.Value: false })
                {
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);
                    try
                    {
                        await Task.Run(() => KanjidicLoader.Load(dict)).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                    }
                }
                else
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }
        else
        {
            dict.Ready = true;
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
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
        Debug.Assert(dict.Revision is not null);

        string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadYomichanDict(uri, dict.Revision, dict.Name, fullDictPath, isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);

            try
            {
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

                string dictBackupPath = GetBackupPath(fullDictPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Delete(dictBackupPath, true);
                }

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

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {dict.Name}");
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

                if (dict is { Active: true, Options.UseDB.Value: false })
                {
                    dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);
                    try
                    {
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
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {dict.Name}, deactivating it");
                        dict.Active = false;
                        dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    }
                }
                else
                {
                    dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
                }
            }
            finally
            {
                dict.Ready = true;
                dict.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }
        else
        {
            dict.Ready = true;
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
    }

    public static async Task UpdateYomichanFreqDict(Freq freq, bool isUpdate, bool noPrompt)
    {
        if (freq.Updating)
        {
            return;
        }

        freq.Updating = true;

        Uri? uri = freq.Url;
        Debug.Assert(uri is not null);
        Debug.Assert(freq.Revision is not null);

        string fullFreqPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
        bool downloaded = await DownloadYomichanDict(uri, freq.Revision, freq.Name, fullFreqPath, isUpdate, noPrompt).ConfigureAwait(false);
        if (downloaded)
        {
            freq.Ready = false;
            freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(13108, StringComparer.Ordinal);

            try
            {
                await Task.Run(() => FrequencyYomichanLoader.Load(freq)).ConfigureAwait(false);

                string dictBackupPath = GetBackupPath(fullFreqPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Delete(dictBackupPath, true);
                }

                string dbPath = DBUtils.GetFreqDBPath(freq.Name);
                bool useDB = freq.Options.UseDB.Value;
                bool dbExists = File.Exists(dbPath);

                if (dbExists)
                {
                    DBUtils.DeleteDB(dbPath);
                }

                if (useDB || dbExists)
                {
                    await Task.Run(() =>
                    {
                        FreqDBManager.CreateDB(freq.Name);
                        FreqDBManager.InsertRecordsToDB(freq);
                    }).ConfigureAwait(false);
                }

                if (!freq.Active || useDB)
                {
                    freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                }

                FrontendManager.Frontend.Alert(AlertLevel.Success, $"Finished updating {freq.Name}");
            }
            catch (Exception ex)
            {
                LoggerManager.Logger.Error(ex, "Couldn't import '{FreqType}'-'{FreqName}' from '{FullFreqPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");

                Directory.Delete(fullFreqPath, true);
                string dictBackupPath = GetBackupPath(fullFreqPath);
                if (Directory.Exists(dictBackupPath))
                {
                    Directory.Move(dictBackupPath, fullFreqPath);
                }

                if (freq is { Active: true, Options.UseDB.Value: false })
                {
                    freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(13108, StringComparer.Ordinal);
                    try
                    {
                        await Task.Run(() => FrequencyYomichanLoader.Load(freq)).ConfigureAwait(false);
                    }
                    catch (Exception innerEx)
                    {
                        LoggerManager.Logger.Error(innerEx, "Couldn't re-import '{FreqType}'-'{FreqName}' from '{FullDictPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't re-import {freq.Name}, deactivating it");
                        freq.Active = false;
                        freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                    }
                }
            }
            finally
            {
                freq.Ready = true;
                freq.Updating = false;
                ObjectPoolManager.ClearStringPoolIfDictsAreReady();
            }
        }
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
        List<Task> tasks = [];
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
