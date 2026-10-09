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
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"This may take a while. Please don't shut down the program until {dictName} is downloaded.");
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
                        FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{dictName} has been downloaded successfully.");
                    }

                    return true;
                }

                if (response.StatusCode is HttpStatusCode.NotModified)
                {
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{dictName} is up to date.");
                }
                else
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", dictName, response.StatusCode);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Unexpected error while downloading {dictName}. Check the logs for more details.");
                }
            }
        }

        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Unexpected error while downloading {DictName}", dictName);
            FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Unexpected error while downloading {dictName}. Check the logs for more details.");

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
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{name} is up to date.");
                    return false;
                }

                if (!indexResponse.IsSuccessStatusCode)
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", name, indexResponse.StatusCode);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Unexpected error while downloading {name}. Check the logs for more details.");

                    return false;
                }

                if (!noPrompt)
                {
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"This may take a while. Please don't shut down the program until {name} is downloaded.");
                }

                JsonElement indexJsonElement = await indexResponse.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                string? newRevision = indexJsonElement.GetProperty("revision").GetString();
                Debug.Assert(newRevision is not null);
                if (indexJsonExists && revision == newRevision)
                {
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{name} is up to date.");
                    return false;
                }

                string? downloadUrl = indexJsonElement.GetProperty("downloadUrl").GetString();
                Debug.Assert(downloadUrl is not null);
                using HttpRequestMessage request = new(HttpMethod.Get, downloadUrl);
                request.Headers.IfModifiedSince = indexRequest.Headers.IfModifiedSince;

                using HttpResponseMessage response = await NetworkUtils.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.StatusCode is HttpStatusCode.NotModified)
                {
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{name} is up to date.");
                    return false;
                }

                if (!response.IsSuccessStatusCode)
                {
                    LoggerManager.Logger.Error("Unexpected error while downloading {DictName}. Status code: {StatusCode}", name, response.StatusCode);
                    FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Unexpected error while downloading {name}. Check the logs for more details.");

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
                    FrontendManager.Frontend.Notify(NotificationLevel.Information, $"{name} has been downloaded successfully.");
                }

                return true;
            }
        }

        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Unexpected error while downloading {DictName}", name);
            FrontendManager.Frontend.Notify(NotificationLevel.Error, $"Unexpected error while downloading {name}. Check the logs for more details.");

            string tempDictPath = PathUtils.GetTempPath(fullDictPath);
            if (Directory.Exists(tempDictPath))
            {
                Directory.Delete(tempDictPath, true);
            }
        }

        return false;
    }

    private static async Task<bool> UpdateBuiltInDict(bool isUpdate, bool noPrompt, DictType dictType, string dictTypeName, int size, DictUtils.CreateDB createDB)
    {
        Dict dict = DictUtils.SingleDictTypeDicts[dictType];
        if (dict.Updating)
        {
            return false;
        }

        dict.Updating = true;
        try
        {
            Uri? uri = dict.Url;
            Debug.Assert(uri is not null);
            string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
            bool downloaded = await DownloadBuiltInDict(fullDictPath, uri, dictTypeName, isUpdate, noPrompt).ConfigureAwait(false);
            if (!downloaded)
            {
                return false;
            }

            if (dictType is DictType.JMdict)
            {
                Dictionary<string, string> entities = new(DictUtils.JmdictEntities.Count, StringComparer.Ordinal);
                return await ImportUpdatedDict(dict, fullDictPath, entities, size, createDB,
                    (importedDict, dbPath) => JmdictDBManager.ImportFromDisk(importedDict, dbPath, entities),
                    importedDict => JmdictLoader.Load(importedDict, entities)).ConfigureAwait(false);
            }

            if (dictType is DictType.JMnedict)
            {
                Dictionary<string, string> entities = new(DictUtils.JmnedictEntities.Count, StringComparer.Ordinal);
                return await ImportUpdatedDict(dict, fullDictPath, entities, size, createDB,
                    (importedDict, dbPath) => JmnedictDBManager.ImportFromDisk(importedDict, dbPath, entities),
                    importedDict => JmnedictLoader.Load(importedDict, entities)).ConfigureAwait(false);
            }

            return await ImportUpdatedDict(dict, fullDictPath, null, size, createDB, KanjidicDBManager.ImportFromDisk, KanjidicLoader.Load).ConfigureAwait(false);
        }
        finally
        {
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
    }

    public static async Task<bool> UpdateJmdict(bool isUpdate, bool noPrompt)
    {
        bool updated = await UpdateBuiltInDict(isUpdate, noPrompt, DictType.JMdict, nameof(DictType.JMdict), JmdictLoader.Size, JmdictDBManager.CreateDB).ConfigureAwait(false);
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
        return UpdateBuiltInDict(isUpdate, noPrompt, DictType.JMnedict, nameof(DictType.JMnedict), JmnedictLoader.Size, JmnedictDBManager.CreateDB);
    }

    public static Task<bool> UpdateKanjidic(bool isUpdate, bool noPrompt)
    {
        return UpdateBuiltInDict(isUpdate, noPrompt, DictType.Kanjidic, nameof(DictType.Kanjidic), KanjidicLoader.Size, KanjidicDBManager.CreateDB);
    }

    private static async Task<bool> UpdateYomichanDict(string dictName, bool isUpdate, bool noPrompt, int size, DictUtils.CreateDB createDB, DictUtils.ImportFromDisk importFromDisk, DictUtils.Load load)
    {
        Dict dict = DictUtils.Dicts[dictName];
        if (dict.Updating)
        {
            return false;
        }

        dict.Updating = true;
        try
        {
            Uri? uri = dict.Url;
            Debug.Assert(uri is not null);
            Debug.Assert(dict.Revision is not null);
            string fullDictPath = Path.GetFullPath(dict.Path, AppInfo.ApplicationPath);
            bool downloaded = await DownloadYomichanDict(uri, dict.Revision, dict.Name, fullDictPath, isUpdate, noPrompt).ConfigureAwait(false);
            return downloaded && await ImportUpdatedDict(dict, fullDictPath, null, size, createDB, importFromDisk, load).ConfigureAwait(false);
        }
        finally
        {
            dict.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
    }

    internal static async Task<bool> ImportUpdatedDict(Dict dict, string fullDictPath, Dictionary<string, string>? entities, int size, DictUtils.CreateDB createDB, DictUtils.ImportFromDisk importFromDisk, DictUtils.Load load)
    {
        bool useDB = dict.Options.UseDB.Value;
        bool originalReady = dict.Ready;
        bool importSuccessful = false;
        bool sourceIsFile = dict.Type is DictType.JMdict or DictType.JMnedict or DictType.Kanjidic;
        string tempDBPath = "";

        try
        {
            if (sourceIsFile ? !File.Exists(fullDictPath) : !Directory.Exists(fullDictPath))
            {
                LoggerManager.Logger.Error("The downloaded dictionary source is missing: {FullDictPath}", fullDictPath);
            }
            else
            {
                Dict importedDict = new(dict.Type, dict.Name, dict.Path, dict.Active, dict.Priority, dict.Size, dict.Options,
                    autoUpdatable: dict.AutoUpdatable, url: dict.Url, revision: dict.Revision)
                {
                    Updating = true
                };

                await Task.Run(async () =>
                {
                    if (useDB)
                    {
                        tempDBPath = PathUtils.GetTempPath(dict.DBPath);
                        DeleteTemporaryDB(tempDBPath);
                        createDB(tempDBPath);
                        await importFromDisk(importedDict, tempDBPath).ConfigureAwait(false);
                    }
                    else
                    {
                        DictUtils.InitializeContents(importedDict, size);
                        await load(importedDict).ConfigureAwait(false);
                        importedDict.Size = importedDict.Contents.Count;
                    }
                }).ConfigureAwait(false);

                if (!useDB || PrepareDatabaseForReplacement(tempDBPath, dict.DBPath))
                {
                    dict.Ready = false;
                    if (useDB)
                    {
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(dict.DBPath, tempDBPath);
                    }
                    else if (File.Exists(dict.DBPath))
                    {
                        DBUtils.DeleteDB(dict.DBPath);
                    }

                    if (dict.Type is DictType.JMdict)
                    {
                        Debug.Assert(entities is not null);
                        DictUtils.JmdictEntities = entities;
                    }
                    else if (dict.Type is DictType.JMnedict)
                    {
                        Debug.Assert(entities is not null);
                        DictUtils.JmnedictEntities = entities;
                    }

                    dict.Size = importedDict.Size;
                    dict.MaxSearchKeyLength = importedDict.MaxSearchKeyLength;
                    dict.Revision = importedDict.Revision;
                    dict.Contents = !useDB && dict.Active
                        ? importedDict.Contents
                        : FrozenDictionary<string, IList<IDictRecord>>.Empty;
                    dict.Ready = true;
                    importSuccessful = true;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", dict.Type.GetDescription(), dict.Name, fullDictPath);
        }
        finally
        {
            if (!importSuccessful && tempDBPath.Length > 0)
            {
                TryDeleteTemporaryDB(tempDBPath);
            }
        }

        if (!importSuccessful)
        {
            RestoreSourceBackup(fullDictPath, sourceIsFile);
            dict.Ready = originalReady && (!useDB || File.Exists(dict.DBPath));
            if (!dict.Ready)
            {
                dict.Active = false;
                dict.Contents = FrozenDictionary<string, IList<IDictRecord>>.Empty;
            }

            FrontendManager.Frontend.Notify(NotificationLevel.Error, dict.Ready
                ? $"Couldn't update {dict.Name}; the previous dictionary is still available. Check the logs for more details."
                : $"Couldn't import {dict.Name}, deactivating it. Check the logs for more details.");
            return false;
        }

        DeleteSourceBackup(fullDictPath, sourceIsFile);
        if (dict.MaxSearchKeyLength > DictUtils.MaxSearchKeyLength)
        {
            DictUtils.MaxSearchKeyLength = dict.MaxSearchKeyLength;
        }

        FrontendManager.Frontend.Notify(NotificationLevel.Success, $"Finished updating {dict.Name}");
        return true;
    }

    public static async Task<bool> UpdateYomichanDict(Dict dict, bool isUpdate, bool noPrompt)
    {
        if (dict.Type is DictType.NonspecificWordYomichan or DictType.NonspecificNameYomichan or DictType.NonspecificKanjiWithWordSchemaYomichan or DictType.NonspecificYomichan)
        {
            return await UpdateYomichanDict(dict.Name, isUpdate, noPrompt, EpwingYomichanDBManager.Size, EpwingYomichanDBManager.CreateDB, EpwingYomichanDBManager.ImportFromDisk, EpwingYomichanLoader.Load).ConfigureAwait(false);
        }

        if (dict.Type is DictType.NonspecificKanjiYomichan)
        {
            return await UpdateYomichanDict(dict.Name, isUpdate, noPrompt, YomichanKanjiDBManager.Size, YomichanKanjiDBManager.CreateDB, YomichanKanjiDBManager.ImportFromDisk, YomichanKanjiLoader.Load).ConfigureAwait(false);
        }

        if (dict.Type is DictType.PitchAccentYomichan)
        {
            return await UpdateYomichanDict(dict.Name, isUpdate, noPrompt, YomichanPitchAccentDBManager.Size, YomichanPitchAccentDBManager.CreateDB, YomichanPitchAccentDBManager.ImportFromDisk, YomichanPitchAccentLoader.Load).ConfigureAwait(false);
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
        try
        {
            Uri? uri = freq.Url;
            Debug.Assert(uri is not null);
            Debug.Assert(freq.Revision is not null);
            string fullDictPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
            bool downloaded = await DownloadYomichanDict(uri, freq.Revision, freq.Name, fullDictPath, isUpdate, noPrompt).ConfigureAwait(false);
            return downloaded && await ImportUpdatedFrequency(freq, fullDictPath).ConfigureAwait(false);
        }
        finally
        {
            freq.Updating = false;
            ObjectPoolManager.ClearStringPoolIfDictsAreReady();
        }
    }

    internal static async Task<bool> ImportUpdatedFrequency(Freq freq, string fullDictPath)
    {
        bool useDB = freq.Options.UseDB.Value;
        bool originalReady = freq.Ready;
        bool importSuccessful = false;
        string tempDBPath = "";

        try
        {
            if (!Directory.Exists(fullDictPath))
            {
                LoggerManager.Logger.Error("The downloaded frequency source is missing: {FullDictPath}", fullDictPath);
            }
            else
            {
                Freq importedFreq = new(freq.Type, freq.Name, freq.Path, freq.Active, freq.Priority, freq.Size, 0, freq.Options,
                    freq.AutoUpdatable, freq.Url, freq.Revision)
                {
                    Updating = true
                };

                await Task.Run(async () =>
                {
                    if (useDB)
                    {
                        tempDBPath = PathUtils.GetTempPath(freq.DBPath);
                        DeleteTemporaryDB(tempDBPath);
                        FreqDBManager.CreateDB(tempDBPath);
                        await FreqDBManager.ImportYomichanFreqFromDisk(importedFreq, tempDBPath).ConfigureAwait(false);
                    }
                    else
                    {
                        FreqUtils.InitializeContents(importedFreq, 0);
                        await FrequencyYomichanLoader.Load(importedFreq).ConfigureAwait(false);
                        importedFreq.Size = importedFreq.Contents.Count;
                    }
                }).ConfigureAwait(false);

                if (!useDB || PrepareDatabaseForReplacement(tempDBPath, freq.DBPath))
                {
                    freq.Ready = false;
                    if (useDB)
                    {
                        PathUtils.ReplaceFileAtomicallyOnSameVolume(freq.DBPath, tempDBPath);
                    }
                    else if (File.Exists(freq.DBPath))
                    {
                        DBUtils.DeleteDB(freq.DBPath);
                    }

                    freq.Size = importedFreq.Size;
                    freq.MaxValue = importedFreq.MaxValue;
                    freq.Revision = importedFreq.Revision;
                    freq.Contents = !useDB && freq.Active
                        ? importedFreq.Contents
                        : FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                    freq.Ready = true;
                    importSuccessful = true;
                }
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't import '{DictType}'-'{DictName}' from '{FullDictPath}'", freq.Type.GetDescription(), freq.Name, fullDictPath);
        }
        finally
        {
            if (!importSuccessful && tempDBPath.Length > 0)
            {
                TryDeleteTemporaryDB(tempDBPath);
            }
        }

        if (!importSuccessful)
        {
            RestoreSourceBackup(fullDictPath, false);
            freq.Ready = originalReady && (!useDB || File.Exists(freq.DBPath));
            if (!freq.Ready)
            {
                freq.Active = false;
                freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
            }

            FrontendManager.Frontend.Notify(NotificationLevel.Error, freq.Ready
                ? $"Couldn't update {freq.Name}; the previous frequency dictionary is still available. Check the logs for more details."
                : $"Couldn't import {freq.Name}, deactivating it. Check the logs for more details.");
            return false;
        }

        DeleteSourceBackup(fullDictPath, false);
        FrontendManager.Frontend.Notify(NotificationLevel.Success, $"Finished updating {freq.Name}");
        return true;
    }

    private static bool PrepareDatabaseForReplacement(string tempDBPath, string dbPath)
    {
        SqliteConnection.ClearAllPools();
        using (SqliteConnection connection = new($"Data Source={tempDBPath};Mode=ReadWrite;Pooling=False;"))
        {
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA quick_check;";
            if (command.ExecuteScalar() is not "ok")
            {
                LoggerManager.Logger.Error("Imported database '{DBPath}' failed SQLite's integrity check", tempDBPath);
                return false;
            }

            // The database must be self-contained before moving its main file.
            command.CommandText = "PRAGMA journal_mode = DELETE;";
            if (command.ExecuteScalar() is not "delete")
            {
                LoggerManager.Logger.Error("Couldn't close the imported database's WAL journal: {DBPath}", tempDBPath);
                return false;
            }
        }

        if (File.Exists(dbPath))
        {
            using SqliteConnection connection = new($"Data Source={dbPath};Mode=ReadWrite;Pooling=False;");
            connection.Open();
            using SqliteCommand command = connection.CreateCommand();
            command.CommandText = "PRAGMA journal_mode = DELETE;";
            if (command.ExecuteScalar() is not "delete")
            {
                LoggerManager.Logger.Error("Couldn't close the previous database's WAL journal: {DBPath}", dbPath);
                return false;
            }
        }

        return true;
    }

    private static void DeleteTemporaryDB(string tempDBPath)
    {
        File.Delete(tempDBPath);
        File.Delete(tempDBPath + "-wal");
        File.Delete(tempDBPath + "-shm");
        File.Delete(tempDBPath + "-journal");
    }

    private static void TryDeleteTemporaryDB(string tempDBPath)
    {
        try
        {
            SqliteConnection.ClearAllPools();
            DeleteTemporaryDB(tempDBPath);
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Warning(ex, "Couldn't remove temporary database '{TempDBPath}'", tempDBPath);
        }
    }

    private static void RestoreSourceBackup(string fullPath, bool sourceIsFile)
    {
        try
        {
            string backupPath = GetBackupPath(fullPath);
            if (sourceIsFile)
            {
                File.Delete(fullPath);
                if (File.Exists(backupPath))
                {
                    File.Move(backupPath, fullPath);
                }
            }
            else
            {
                if (Directory.Exists(fullPath))
                {
                    Directory.Delete(fullPath, true);
                }

                if (Directory.Exists(backupPath))
                {
                    Directory.Move(backupPath, fullPath);
                }
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't restore source backup for '{FullPath}'", fullPath);
            FrontendManager.Frontend.Notify(NotificationLevel.Error, "Couldn't restore the previous source files. Check the logs for more details.");
        }
    }

    private static void DeleteSourceBackup(string fullPath, bool sourceIsFile)
    {
        string backupPath = GetBackupPath(fullPath);
        try
        {
            if (sourceIsFile)
            {
                File.Delete(backupPath);
            }
            else if (Directory.Exists(backupPath))
            {
                Directory.Delete(backupPath, true);
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Warning(ex, "Couldn't remove source backup '{BackupPath}'", backupPath);
        }
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

            FrontendManager.Frontend.Notify(NotificationLevel.Information, $"Updating {dict.Name}...");
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

            FrontendManager.Frontend.Notify(NotificationLevel.Information, $"Updating {freq.Name}...");
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
