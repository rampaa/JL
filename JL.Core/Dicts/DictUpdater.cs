using System.Collections.Frozen;
using System.IO.Compression;
using System.Net;
using JL.Core.Dicts.JMdict;
using JL.Core.Dicts.JMnedict;
using JL.Core.Dicts.KANJIDIC;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts;

public static class DictUpdater
{
    internal static async Task<bool> UpdateDict(string dictPath, Uri dictDownloadUri, string dictName,
        bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || noPrompt || Utils.Frontend.ShowYesNoDialog($"Do you want to download the latest version of {dictName}?",
                    isUpdate ? "Update dictionary?" : "Download dictionary?"))
            {
                using HttpRequestMessage request = new(HttpMethod.Get, dictDownloadUri);

                string fullPath = Path.GetFullPath(dictPath, Utils.ApplicationPath);
                if (File.Exists(fullPath))
                {
                    request.Headers.IfModifiedSince = File.GetLastWriteTime(fullPath);
                }

                if (!noPrompt)
                {
                    Utils.Frontend.ShowOkDialog($"This may take a while. Please don't shut down the program until {dictName} is downloaded.", "Info");
                }

                using HttpResponseMessage response = await Networking.Client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    Stream responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await using (responseStream.ConfigureAwait(false))
                    {
                        await DecompressGzipStream(responseStream, fullPath).ConfigureAwait(false);
                    }

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

    public static async Task UpdateJmdict(bool isUpdate, bool noPrompt)
    {
        DictUtils.UpdatingJmdict = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        bool downloaded = await UpdateDict(dict.Path,
                DictUtils.s_jmdictUrl,
                DictType.JMdict.ToString(), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(450000, StringComparer.Ordinal);

            await Task.Run(async () => await JmdictLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

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

            Utils.ClearStringPoolIfDictsAreReady();
            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, "Finished updating JMdict");
        }

        DictUtils.UpdatingJmdict = false;
    }

    public static async Task UpdateJmnedict(bool isUpdate, bool noPrompt)
    {
        DictUtils.UpdatingJmnedict = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMnedict];
        bool downloaded = await UpdateDict(dict.Path,
                DictUtils.s_jmnedictUrl,
                DictType.JMnedict.ToString(), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(620000, StringComparer.Ordinal);

            await Task.Run(async () => await JmnedictLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

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

            Utils.ClearStringPoolIfDictsAreReady();
            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, "Finished updating JMnedict");
        }

        DictUtils.UpdatingJmnedict = false;
    }

    public static async Task UpdateKanjidic(bool isUpdate, bool noPrompt)
    {
        DictUtils.UpdatingKanjidic = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.Kanjidic];
        bool downloaded = await UpdateDict(dict.Path,
                DictUtils.s_kanjidicUrl,
                DictType.Kanjidic.ToString(), isUpdate, noPrompt)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Ready = false;
            dict.Contents = new Dictionary<string, IList<IDictRecord>>(13108, StringComparer.Ordinal);

            await Task.Run(async () => await KanjidicLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

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

            Utils.ClearStringPoolIfDictsAreReady();
            dict.Ready = true;
            Utils.Frontend.Alert(AlertLevel.Success, "Finished updating KANJIDIC2");
        }

        DictUtils.UpdatingKanjidic = false;
    }

    internal static async Task AutoUpdateBuiltInDicts()
    {
        DictType[] dicts =
        [
            DictType.JMdict,
            DictType.JMnedict,
            DictType.Kanjidic
        ];

        for (int i = 0; i < dicts.Length; i++)
        {
            Dict dict = DictUtils.SingleDictTypeDicts[dicts[i]];
            if (!dict.Active)
            {
                continue;
            }

            int dueDate = dict.Options.AutoUpdateAfterNDays!.Value;
            if (dueDate is 0)
            {
                continue;
            }

            string fullPath = Path.GetFullPath(dict.Path, Utils.ApplicationPath);
            bool pathExists = File.Exists(fullPath);
            if (pathExists && (DateTime.Now - File.GetLastWriteTime(fullPath)).Days < dueDate)
            {
                continue;
            }

            Utils.Frontend.Alert(AlertLevel.Information, $"Updating {dict.Type}...");
            if (dict.Type is DictType.JMdict)
            {
                await UpdateJmdict(pathExists, true).ConfigureAwait(false);
            }
            else if (dict.Type is DictType.JMnedict)
            {
                await UpdateJmnedict(pathExists, true).ConfigureAwait(false);
            }
            else
            {
                await UpdateKanjidic(pathExists, true).ConfigureAwait(false);
            }
        }
    }

}
