using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Freqs.Options;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;

public static class FreqUtils
{
    public static bool FreqsReady { get; private set; } // = false;

    public static readonly Dictionary<string, Freq> FreqDicts = new(StringComparer.OrdinalIgnoreCase);

    internal static Freq[]? WordFreqs { get; private set; }
    internal static Freq[]? KanjiFreqs { get; private set; }

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new(3, StringComparer.OrdinalIgnoreCase)
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)",
                Path.Join(AppInfo.ResourcesPath, "freqlist_vns.json"),
                true, 1, 57273, 35893, new FreqOptions(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)))
        },
        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)",
                Path.Join(AppInfo.ResourcesPath, "freqlist_narou.json"),
                false, 2, 75588, 48528, new FreqOptions(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)))
        },
        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)",
                Path.Join(AppInfo.ResourcesPath, "freqlist_novels.json"),
                false, 3, 114348, 74633, new FreqOptions(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)))
        }
    };

    internal static readonly FreqType[] s_allFreqDicts =
    [
        FreqType.Nazeka,
        FreqType.Yomichan,
        FreqType.YomichanKanji
    ];

    public static async Task LoadFrequencies()
    {
        FreqsReady = false;

        bool freqCleared = false;
        bool rebuildingAnyDB = false;
        ConcurrentBag<string> freqNamesToBeRemoved = [];

        Dictionary<string, string> freqDBPaths = new(StringComparer.Ordinal);

        List<Task> tasks = [];
        Freq[] freqs = FreqDicts.Values.ToArray();

        CheckFreqDicts(freqs);

        foreach (Freq freq in freqs)
        {
            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    LoadNazekaFreq(freq, tasks, freqDBPaths, freqNamesToBeRemoved, ref rebuildingAnyDB, ref freqCleared);
                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    LoadYomichanFreq(freq, tasks, freqDBPaths, freqNamesToBeRemoved, ref rebuildingAnyDB, ref freqCleared);
                    break;

                default:
                {
                    LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(FreqType), nameof(FreqUtils), nameof(LoadFrequencies), freq.Type);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid frequency type: {freq.Type}");
                    break;
                }
            }
        }

        if (freqDBPaths.Count > 0)
        {
            KeyValuePair<string, string>[] tempFreqDBPathKeyValuePairs = new KeyValuePair<string, string>[DBUtils.FreqDBPaths.Count + freqDBPaths.Count];
            int index = 0;
            foreach ((string key, string value) in DBUtils.FreqDBPaths)
            {
                tempFreqDBPathKeyValuePairs[index] = KeyValuePair.Create(key, value);
                ++index;
            }

            foreach ((string key, string value) in freqDBPaths)
            {
                tempFreqDBPathKeyValuePairs[index] = KeyValuePair.Create(key, value);
                ++index;
            }

            DBUtils.FreqDBPaths = tempFreqDBPathKeyValuePairs.ToFrozenDictionary(StringComparer.Ordinal);
        }

        if (tasks.Count > 0 || freqCleared)
        {
            SqliteConnection.ClearAllPools();

            if (tasks.Count > 0)
            {
                if (rebuildingAnyDB)
                {
                    FrontendManager.Frontend.Alert(AlertLevel.Information, "Rebuilding frequency databases because their schemas are out of date...");
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (!freqNamesToBeRemoved.IsEmpty)
                {
                    foreach (string freqName in freqNamesToBeRemoved)
                    {
                        _ = FreqDicts.Remove(freqName);
                        string dbPath = DBUtils.GetFreqDBPath(freqName);
                        if (File.Exists(dbPath))
                        {
                            DBUtils.DeleteDB(dbPath);
                        }
                    }

                    IOrderedEnumerable<Freq> orderedFreqs = FreqDicts.Values.OrderBy(static f => f.Priority);
                    int priority = 1;

                    foreach (Freq freq in orderedFreqs)
                    {
                        freq.Priority = priority;
                        ++priority;
                    }
                }
            }

            Freq[] freqSnapshot = FreqDicts.Values.ToArray();
            CheckFreqDicts(freqSnapshot);

            if (freqSnapshot.All(static f => !f.Updating))
            {
                FrontendManager.Frontend.Alert(AlertLevel.Success, "Finished loading frequency dictionaries");
            }
        }

        FreqsReady = true;
    }

    private static DBState PrepareFreqDB(Freq freq, Dictionary<string, string> freqDBPaths, int dbVersion, ref bool rebuildingAnyDB)
    {
        bool useDB = freq.Options.UseDB.Value;
        string dbPath = DBUtils.GetFreqDBPath(freq.Name);
        string dbJournalPath = $"{dbPath}-journal";
        bool dbExists = File.Exists(dbPath);
        bool dbExisted = dbExists;
        bool dbJournalExists = File.Exists(dbJournalPath);

        if (!freq.Updating)
        {
            if (dbJournalExists)
            {
                if (dbExists)
                {
                    DBUtils.DeleteDB(dbPath);
                    dbExists = false;
                }

                File.Delete(dbJournalPath);
            }
            else if (dbExists && !DBUtils.RecordExists(dbPath))
            {
                DBUtils.DeleteDB(dbPath);
                dbExists = false;
            }
        }

        freq.Ready = false;

        if (useDB && !DBUtils.FreqDBPaths.ContainsKey(freq.Name))
        {
            freqDBPaths.Add(freq.Name, dbPath);
        }

        if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(dbVersion, dbPath))
        {
            DBUtils.DeleteDB(dbPath);
            dbExists = false;
            rebuildingAnyDB = true;
        }

        return new DBState(useDB, dbExists, dbExisted);
    }

    private static void LoadNazekaFreq(Freq freq, List<Task> tasks, Dictionary<string, string> freqDBPaths, ConcurrentBag<string> freqNamesToBeRemoved, ref bool rebuildingAnyDB, ref bool freqCleared)
    {
        DBState dBContext = PrepareFreqDB(freq, freqDBPaths, FreqDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !useDB;

        if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    int size = freq.Size > 0
                        ? freq.Size
                        : 114348;

                    freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(size, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        FreqDBManager.LoadFromDB(freq);
                        freq.Size = freq.Contents.Count;
                    }
                    else
                    {
                        await FrequencyNazekaLoader.Load(freq).ConfigureAwait(false);
                        freq.Size = freq.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            FreqDBManager.CreateDB(freq.Name);
                            FreqDBManager.InsertRecordsToDB(freq);

                            if (useDB)
                            {
                                freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                            }
                        }
                    }

                    freq.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullFreqPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{FreqType}'-'{FreqName}' from '{FullFreqPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                    freqNamesToBeRemoved.Add(freq.Name);
                }
            }));
        }

        else if (freq.Contents.Count > 0 && (!freq.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                FreqDBManager.CreateDB(freq.Name);
                FreqDBManager.InsertRecordsToDB(freq);
            }

            freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
            freq.Ready = true;
            freqCleared = true;
        }

        else if (freq is { Active: true, Contents.Count: 0 } && useDB)
        {
            if (freq.MaxValue is 0)
            {
                FreqDBManager.SetMaxFrequencyValue(freq);
            }

            freq.Ready = true;
        }

        else
        {
            freq.Ready = true;
        }
    }

    private static void LoadYomichanFreq(Freq freq, List<Task> tasks, Dictionary<string, string> freqDBPaths, ConcurrentBag<string> freqNamesToBeRemoved, ref bool rebuildingAnyDB, ref bool freqCleared)
    {
        if (freq.Updating)
        {
            return;
        }

        DBState dBContext = PrepareFreqDB(freq, freqDBPaths, FreqDBManager.Version, ref rebuildingAnyDB);

        bool useDB = dBContext.UseDB;
        bool dbExists = dBContext.DBExists;
        bool loadFromDB = dbExists && !useDB;
        if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
        {
            bool dbExisted = dBContext.DBExisted;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    int size = freq.Size > 0
                        ? freq.Size
                        : freq.Type is FreqType.Yomichan
                            ? 1504512
                            : 169623;

                    freq.Contents = new Dictionary<string, IList<FrequencyRecord>>(size, StringComparer.Ordinal);

                    if (loadFromDB)
                    {
                        FreqDBManager.LoadFromDB(freq);
                        freq.Size = freq.Contents.Count;
                    }
                    else
                    {
                        await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                        freq.Size = freq.Contents.Count;

                        if (!dbExists && (useDB || dbExisted))
                        {
                            FreqDBManager.CreateDB(freq.Name);
                            FreqDBManager.InsertRecordsToDB(freq);

                            if (useDB)
                            {
                                freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
                            }
                        }
                    }

                    freq.Ready = true;
                }

                catch (Exception ex)
                {
                    string fullFreqPath = Path.GetFullPath(freq.Path, AppInfo.ApplicationPath);
                    LoggerManager.Logger.Error(ex, "Couldn't import '{FreqType}'-'{FreqName}' from '{FullFreqPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                    FrontendManager.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                    freqNamesToBeRemoved.Add(freq.Name);
                }
            }));
        }

        else if (freq.Contents.Count > 0 && (!freq.Active || useDB))
        {
            if (useDB && !dbExists)
            {
                FreqDBManager.CreateDB(freq.Name);
                FreqDBManager.InsertRecordsToDB(freq);
            }

            freq.Contents = FrozenDictionary<string, IList<FrequencyRecord>>.Empty;
            freq.Ready = true;

            freqCleared = true;
        }

        else if (freq is { Active: true, Contents.Count: 0 } && useDB)
        {
            if (freq.MaxValue is 0)
            {
                FreqDBManager.SetMaxFrequencyValue(freq);
            }

            freq.Ready = true;
        }

        else
        {
            freq.Ready = true;
        }
    }

    public static async Task CreateDefaultFreqsConfig()
    {
        _ = Directory.CreateDirectory(AppInfo.ConfigPath);

        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "freqs.json"), FileStreamOptionsPresets.s_asyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, s_builtInFreqs, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    public static async Task SerializeFreqs()
    {
        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "freqs.json"), FileStreamOptionsPresets.s_asyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, FreqDicts, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    internal static async Task DeserializeFreqs()
    {
        Dictionary<string, Freq>? deserializedFreqs;

        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "freqs.json"), FileStreamOptionsPresets.s_asyncReadFso);
        await using (fileStream.ConfigureAwait(false))
        {
            deserializedFreqs = await JsonSerializer
                .DeserializeAsync<Dictionary<string, Freq>>(fileStream, JsonOptions.s_jsoWithEnumConverter).ConfigureAwait(false);
        }

        if (deserializedFreqs is not null)
        {
            IOrderedEnumerable<Freq> orderedFreqs = deserializedFreqs.Values.OrderBy(static f => f.Priority);
            int priority = 1;

            foreach (Freq freq in orderedFreqs)
            {
                freq.Priority = priority;
                ++priority;

                freq.Path = PathUtils.GetPortablePath(freq.Path);
                if (freq.Type is FreqType.Yomichan or FreqType.YomichanKanji && freq.Revision is null)
                {
                    await UpdateRevisionInfo(freq).ConfigureAwait(false);
                }

                InitFreqOptions(freq);

                FreqDicts.Add(freq.Name, freq);
            }
        }
        else
        {
            FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/freqs.json");
            throw new SerializationException("Couldn't load Config/freqs.json");
        }
    }

    private static void InitFreqOptions(Freq freq)
    {
        if (AutoUpdateAfterNDaysOption.ValidFreqTypes.Contains(freq.Type))
        {
            freq.Options.AutoUpdateAfterNDays ??= new AutoUpdateAfterNDaysOption(0);
        }
    }

    private static void CheckFreqDicts(Freq[] freqs)
    {
        WordFreqs = freqs.Where(static f => f is { Type: not FreqType.YomichanKanji, Active: true })
            .OrderBy(static f => f.Priority)
            .ToArray();

        if (WordFreqs.Length is 0)
        {
            WordFreqs = null;
        }

        KanjiFreqs = freqs.Where(static f => f is { Type: FreqType.YomichanKanji, Active: true })
            .OrderBy(static f => f.Priority)
            .ToArray();

        if (KanjiFreqs.Length is 0)
        {
            KanjiFreqs = null;
        }
    }

    internal static void AddOrUpdate(IDictionary<string, IList<FrequencyRecord>> contents, string key, FrequencyRecord record)
    {
        if (contents.TryGetValue(key, out IList<FrequencyRecord>? freqResult))
        {
            int index = freqResult.IndexOf(record);
            if (index < 0)
            {
                freqResult.Add(record);
            }
            else
            {
                if (freqResult[index].Frequency > record.Frequency)
                {
                    freqResult[index] = record;
                }
            }
        }
        else
        {
            contents[key] = [record];
        }
    }

    private static async Task UpdateRevisionInfo(Freq freq)
    {
        string indexJsonPath = Path.GetFullPath(Path.Join(freq.Path, "index.json"), AppInfo.ApplicationPath);
        if (File.Exists(indexJsonPath))
        {
            JsonElement jsonElement;

            FileStream fileStream = new(indexJsonPath, FileStreamOptionsPresets.s_asyncReadFso);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            }

            freq.Revision = jsonElement.GetProperty("revision").GetString();
            freq.AutoUpdatable = jsonElement.TryGetProperty("isUpdatable", out JsonElement isUpdatableJsonElement) && isUpdatableJsonElement.GetBoolean();
            if (freq.AutoUpdatable)
            {
                string? indexUrl = jsonElement.GetProperty("indexUrl").GetString();
                Debug.Assert(indexUrl is not null);
                freq.Url = new Uri(indexUrl);
            }
        }
    }
}
