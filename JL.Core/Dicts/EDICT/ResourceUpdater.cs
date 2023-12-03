using System.Globalization;
using System.IO.Compression;
using System.Net;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Dicts.EDICT.JMnedict;
using JL.Core.Dicts.EDICT.KANJIDIC;
using JL.Core.Network;
using JL.Core.Utilities;
using JL.Core.WordClass;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EDICT;

public static class ResourceUpdater
{
    internal static async Task<bool> UpdateResource(string resourcePath, Uri resourceDownloadUri, string resourceName,
        bool isUpdate, bool noPrompt)
    {
        try
        {
            if (!isUpdate || Utils.Frontend.ShowYesNoDialog(string.Create(CultureInfo.InvariantCulture,
                        $"Do you want to download the latest version of {resourceName}?"),
                    "Update dictionary?"))
            {
                using HttpRequestMessage request = new(HttpMethod.Get, resourceDownloadUri);

                string fullPath = Path.GetFullPath(resourcePath, Utils.ApplicationPath);
                if (File.Exists(fullPath))
                {
                    request.Headers.IfModifiedSince = File.GetLastWriteTime(fullPath);
                }

                if (!noPrompt)
                {
                    Utils.Frontend.ShowOkDialog(string.Create(CultureInfo.InvariantCulture,
                            $"This may take a while. Please don't shut down the program until {resourceName} is downloaded."),
                        "Info");
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
                        Utils.Frontend.ShowOkDialog(string.Create(CultureInfo.InvariantCulture,
                                $"{resourceName} has been downloaded successfully."),
                            "Info");
                    }

                    return true;
                }

                if (response.StatusCode is HttpStatusCode.NotModified && !noPrompt)
                {
                    Utils.Frontend.ShowOkDialog(string.Create(CultureInfo.InvariantCulture,
                            $"{resourceName} is up to date."),
                        "Info");
                }

                else
                {
                    Utils.Logger.Error("Unexpected error while downloading {ResourceName}. Status code: {StatusCode}",
                        resourceName, response.StatusCode);

                    if (!noPrompt)
                    {
                        Utils.Frontend.ShowOkDialog(string.Create(CultureInfo.InvariantCulture,
                                $"Unexpected error while downloading {resourceName}."),
                            "Info");
                    }
                }
            }
        }

        catch (Exception ex)
        {
            Utils.Frontend.ShowOkDialog(string.Create(CultureInfo.InvariantCulture,
                    $"Unexpected error while downloading {resourceName}."),
                "Info");

            Utils.Logger.Error(ex, "Unexpected error while downloading {ResourceName}", resourceName);
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

    public static async Task UpdateJmdict()
    {
        DictUtils.UpdatingJmdict = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMdict];
        bool downloaded = await UpdateResource(dict.Path,
                DictUtils.s_jmdictUrl,
                DictType.JMdict.ToString(), true, false)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await JmdictLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

            await JmdictWordClassUtils.Serialize().ConfigureAwait(false);

            DictUtils.WordClassDictionary.Clear();

            await JmdictWordClassUtils.Load().ConfigureAwait(false);

            string dbPath = DictUtils.GetDBPath(dict.Name);
            bool useDB = dict.Options?.UseDB?.Value ?? false;
            bool dbExists = File.Exists(dbPath);

            if (dbExists || useDB)
            {
                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                }

                await Task.Run(() =>
                {
                    JmdictDBManager.CreateDB(dbPath);
                    JmdictDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents.Clear();
                dict.Contents.TrimExcess();
            }
        }

        DictUtils.UpdatingJmdict = false;

        Utils.ClearStringPoolIfDictsAreReady();
    }

    public static async Task UpdateJmnedict()
    {
        DictUtils.UpdatingJmnedict = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.JMnedict];
        bool downloaded = await UpdateResource(dict.Path,
                DictUtils.s_jmnedictUrl,
                DictType.JMnedict.ToString(), true, false)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await JmnedictLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

            string dbPath = DictUtils.GetDBPath(dict.Name);
            bool useDB = dict.Options?.UseDB?.Value ?? false;
            bool dbExists = File.Exists(dbPath);
            if (dbExists || useDB)
            {
                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                }

                await Task.Run(() =>
                {
                    JmnedictDBManager.CreateDB(dbPath);
                    JmnedictDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents.Clear();
                dict.Contents.TrimExcess();
            }
        }

        DictUtils.UpdatingJmnedict = false;

        Utils.ClearStringPoolIfDictsAreReady();
    }

    public static async Task UpdateKanjidic()
    {
        DictUtils.UpdatingKanjidic = true;

        Dict dict = DictUtils.SingleDictTypeDicts[DictType.Kanjidic];
        bool downloaded = await UpdateResource(dict.Path,
                DictUtils.s_kanjidicUrl,
                DictType.Kanjidic.ToString(), true, false)
            .ConfigureAwait(false);

        if (downloaded)
        {
            dict.Contents.Clear();

            await Task.Run(async () => await KanjidicLoader
                .Load(dict).ConfigureAwait(false)).ConfigureAwait(false);

            string dbPath = DictUtils.GetDBPath(dict.Name);
            bool useDB = dict.Options?.UseDB?.Value ?? false;
            bool dbExists = File.Exists(dbPath);

            if (dbExists || useDB)
            {
                if (dbExists)
                {
                    SqliteConnection.ClearAllPools();
                    File.Delete(dbPath);
                }

                await Task.Run(() =>
                {
                    KanjidicDBManager.CreateDB(dbPath);
                    KanjidicDBManager.InsertRecordsToDB(dict);
                }).ConfigureAwait(false);
            }

            if (!dict.Active || useDB)
            {
                dict.Contents.Clear();
                dict.Contents.TrimExcess();
            }
        }

        DictUtils.UpdatingKanjidic = false;

        Utils.ClearStringPoolIfDictsAreReady();
    }
}
