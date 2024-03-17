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
    public static Dictionary<string, Freq> FreqDicts { get; } = new();

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new(3)
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_vns.json"),
                true, 1, 57273, false, new FreqOptions(new UseDBOption(false), new HigherValueMeansHigherFrequencyOption(false)))
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_narou.json"),
                false, 2, 75588, false, new FreqOptions(new UseDBOption(false), new HigherValueMeansHigherFrequencyOption(false)))
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_novels.json"),
                false, 3, 114348, false, new FreqOptions(new UseDBOption(false), new HigherValueMeansHigherFrequencyOption(false)))
        }
    };

    internal static readonly FreqType[] s_allFreqDicts = {
        FreqType.Nazeka,
        FreqType.Yomichan,
        FreqType.YomichanKanji
    };

    public static async Task LoadFrequencies()
    {
        FreqsReady = false;

        bool freqCleared = false;
        bool freqRemoved = false;

        List<Task> tasks = new();

        foreach (Freq freq in FreqDicts.Values.ToList())
        {
            bool useDB = freq.Options?.UseDB?.Value ?? false;
            string dbPath = DBUtils.GetFreqDBPath(freq.Name);
            string dbJournalPath = dbPath + "-journal";
            bool dbExists = File.Exists(dbPath);
            bool dbExisted = dbExists;
            bool dbJournalExists = File.Exists(dbJournalPath);

            if (dbJournalExists)
            {
                DBUtils.SendOptimizePragmaToAllDBs();
                SqliteConnection.ClearAllPools();
                File.Delete(dbJournalPath);
                if (dbExists)
                {
                    File.Delete(dbPath);
                    dbExists = false;
                }
            }

            bool loadFromDB;
            freq.Ready = false;

            if (useDB)
            {
                _ = DBUtils.s_freqDBPaths.TryAdd(freq.Name, dbPath);
            }

            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    dbExists = DBUtils.DeleteOldDB(dbExists, FreqDBManager.Version, dbPath);
                    loadFromDB = dbExists && !useDB;

                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                freq.Contents = freq.Size > 0
                                    ? new Dictionary<string, IList<FrequencyRecord>>(freq.Size)
                                    : new Dictionary<string, IList<FrequencyRecord>>(114348);

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
                                        freq.Contents.Clear();
                                        freq.Contents.TrimExcess();
                                    }
                                }

                                freq.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (dbExists)
                                {
                                    DBUtils.SendOptimizePragmaToAllDBs();
                                    SqliteConnection.ClearAllPools();
                                    File.Delete(dbPath);
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
                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freq.Ready = true;
                        }
                        else
                        {
                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freq.Ready = true;
                        }

                        freqCleared = true;
                    }

                    else
                    {
                        freq.Ready = true;
                    }
                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    dbExists = DBUtils.DeleteOldDB(dbExists, FreqDBManager.Version, dbPath);
                    loadFromDB = dbExists && !useDB;

                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        tasks.Add(Task.Run(async () =>
                        {
                            try
                            {
                                freq.Contents = freq.Size > 0
                                    ? new Dictionary<string, IList<FrequencyRecord>>(freq.Size)
                                    : freq.Type is FreqType.Yomichan
                                        ? new Dictionary<string, IList<FrequencyRecord>>(1504512)
                                        : new Dictionary<string, IList<FrequencyRecord>>(169623);

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
                                        freq.Contents.Clear();
                                        freq.Contents.TrimExcess();
                                    }
                                }

                                freq.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (dbExists)
                                {
                                    DBUtils.SendOptimizePragmaToAllDBs();
                                    SqliteConnection.ClearAllPools();
                                    File.Delete(dbPath);
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
                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freq.Ready = true;
                        }
                        else
                        {
                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freq.Ready = true;
                        }

                        freqCleared = true;
                    }

                    else
                    {
                        freq.Ready = true;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid freq type");
            }
        }

        if (tasks.Count > 0 || freqCleared)
        {
            SqliteConnection.ClearAllPools();

            if (tasks.Count > 0)
            {
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

    public static async Task CreateDefaultFreqsConfig()
    {
        _ = Directory.CreateDirectory(Utils.ConfigPath);
        await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
            JsonSerializer.Serialize(s_builtInFreqs, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
    }

    public static async Task SerializeFreqs()
    {
        await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
            JsonSerializer.Serialize(FreqDicts, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
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
