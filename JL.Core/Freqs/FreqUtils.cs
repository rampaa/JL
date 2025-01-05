using System.Collections.Frozen;
using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Freqs;

public static class FreqUtils
{
    public static bool FreqsReady { get; private set; } // = false;
    public static Dictionary<string, Freq> FreqDicts { get; } = new(StringComparer.OrdinalIgnoreCase);

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new(3, StringComparer.OrdinalIgnoreCase)
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_vns.json"),
                true, 1, 57273, 35893, new FreqOptions(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)))
        },
        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_narou.json"),
                false, 2, 75588, 48528, new FreqOptions(new UseDBOption(true), new HigherValueMeansHigherFrequencyOption(false)))
        },
        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_novels.json"),
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
        bool freqRemoved = false;
        bool rebuildingDBs = false;

        Dictionary<string, string> freqDBPathDict = new(StringComparer.Ordinal);

        List<Task> tasks = [];

        foreach (Freq freq in FreqDicts.Values.ToArray())
        {
            bool useDB = freq.Options.UseDB.Value;
            string dbPath = DBUtils.GetFreqDBPath(freq.Name);
            string dbJournalPath = dbPath + "-journal";
            bool dbExists = File.Exists(dbPath);
            bool dbExisted = dbExists;
            bool dbJournalExists = File.Exists(dbJournalPath);

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

            bool loadFromDB;
            freq.Ready = false;

            if (useDB && !DBUtils.FreqDBPaths.ContainsKey(freq.Name))
            {
                freqDBPathDict.Add(freq.Name, dbPath);
            }

            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(FreqDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingDBs = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
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
                                string fullFreqPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{FreqType}'-'{FreqName}' from '{FullFreqPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
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
                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    if (dbExists && DBUtils.CheckIfDBSchemaIsOutOfDate(FreqDBManager.Version, dbPath))
                    {
                        DBUtils.DeleteDB(dbPath);
                        dbExists = false;
                        rebuildingDBs = true;
                    }
                    loadFromDB = dbExists && !useDB;

                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
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
                                string fullFreqPath = Path.GetFullPath(freq.Path, Utils.ApplicationPath);
                                Utils.Logger.Error(ex, "Couldn't import '{FreqType}'-'{FreqName}' from '{FullFreqPath}'", freq.Type.GetDescription(), freq.Name, fullFreqPath);
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (File.Exists(dbPath))
                                {
                                    DBUtils.DeleteDB(dbPath);
                                }
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

                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, freq.Type, "Invalid freq type");
            }
        }

        if (freqDBPathDict.Count > 0)
        {
            Dictionary<string, string> tempFreqDBPathDict = new(DBUtils.FreqDBPaths.Count + freqDBPathDict.Count, StringComparer.Ordinal);

            foreach ((string key, string value) in DBUtils.FreqDBPaths)
            {
                tempFreqDBPathDict[key] = value;
            }

            foreach ((string key, string value) in freqDBPathDict)
            {
                tempFreqDBPathDict[key] = value;
            }

            DBUtils.FreqDBPaths = tempFreqDBPathDict.ToFrozenDictionary(StringComparer.Ordinal);
        }

        if (tasks.Count > 0 || freqCleared)
        {
            SqliteConnection.ClearAllPools();

            if (tasks.Count > 0)
            {
                if (rebuildingDBs)
                {
                    Utils.Frontend.Alert(AlertLevel.Information, "Rebuilding frequency databases because their schemas are out of date...");
                }

                await Task.WhenAll(tasks).ConfigureAwait(false);

                if (freqRemoved)
                {
                    IOrderedEnumerable<Freq> orderedFreqs = FreqDicts.Values.OrderBy(static f => f.Priority);
                    int priority = 1;

                    foreach (Freq freq in orderedFreqs)
                    {
                        freq.Priority = priority;
                        ++priority;
                    }
                }
            }

            Utils.Frontend.Alert(AlertLevel.Success, "Finished loading frequency dictionaries");
        }

        FreqsReady = true;
    }

    public static Task CreateDefaultFreqsConfig()
    {
        _ = Directory.CreateDirectory(Utils.ConfigPath);
        return File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
            JsonSerializer.Serialize(s_builtInFreqs, Utils.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation));
    }

    public static Task SerializeFreqs()
    {
        return File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
            JsonSerializer.Serialize(FreqDicts, Utils.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation));
    }

    internal static async Task DeserializeFreqs()
    {
        FileStream fileStream = File.OpenRead(Path.Join(Utils.ConfigPath, "freqs.json"));
        await using (fileStream.ConfigureAwait(false))
        {
            Dictionary<string, Freq>? deserializedFreqs = await JsonSerializer
                .DeserializeAsync<Dictionary<string, Freq>>(fileStream, Utils.s_jsoWithEnumConverter).ConfigureAwait(false);

            if (deserializedFreqs is not null)
            {
                IOrderedEnumerable<Freq> orderedFreqs = deserializedFreqs.Values.OrderBy(static f => f.Priority);
                int priority = 1;

                foreach (Freq freq in orderedFreqs)
                {
                    freq.Priority = priority;
                    ++priority;

                    freq.Path = Utils.GetPath(freq.Path);

                    FreqDicts.Add(freq.Name, freq);
                }
            }
            else
            {
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/freqs.json");
                throw new SerializationException("Couldn't load Config/freqs.json");
            }
        }
    }
}
