using System.Globalization;
using System.Text.Json;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Freqs.Options;
using JL.Core.Utilities;

namespace JL.Core.Freqs;

public static class FreqUtils
{
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, Freq> FreqDicts { get; } = new();
    internal static readonly string s_dbFolderPath = Path.Join(Utils.ResourcesPath, "Frequency Databases");

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new(3)
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_vns.json"),
                true, 1, 57273, false, new FreqOptions(new UseDBOption(false)))
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_narou.json"),
                false, 2, 75588, false, new FreqOptions(new UseDBOption(false)))
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_novels.json"),
                false, 3, 114348, false, new FreqOptions(new UseDBOption(false)))
        }
    };

    internal static readonly FreqType[] s_freqTypesWithDBSupport = {
        FreqType.Nazeka,
        FreqType.Yomichan,
        FreqType.YomichanKanji
    };

    public static string GetDBPath(string dbName)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Path.Join(s_dbFolderPath, dbName)}.sqlite");
    }

    public static async Task LoadFrequencies()
    {
        FreqsReady = false;

        bool freqCleared = false;
        bool freqRemoved = false;

        List<Task> tasks = new();

        foreach (Freq freq in FreqDicts.Values.ToList())
        {
            bool useDB = freq.Options?.UseDB?.Value ?? false;
            string dbPath = GetDBPath(freq.Name);
            string dbJournalPath = dbPath + "-journal";
            bool dbExists = File.Exists(dbPath);
            bool dbJournalExists = File.Exists(dbJournalPath);

            if (dbJournalExists)
            {
                File.Delete(dbJournalPath);
                if (dbExists)
                {
                    File.Delete(dbPath);
                    dbExists = false;
                }
            }

            bool loadFromDB = dbExists && !useDB;

            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        freq.Ready = false;
                        Task nazekaFreqTask = Task.Run(async () =>
                        {
                            try
                            {
                                freq.Contents = freq.Size > 0
                                    ? new Dictionary<string, IList<FrequencyRecord>>(freq.Size)
                                    : new Dictionary<string, IList<FrequencyRecord>>(114348);

                                if (loadFromDB)
                                {
                                    FreqDBManager.LoadFromDB(freq);
                                }
                                else
                                {
                                    await FrequencyNazekaLoader.Load(freq).ConfigureAwait(false);

                                    if (useDB && !dbExists)
                                    {
                                        FreqDBManager.CreateDB(freq.Name);
                                        FreqDBManager.InsertRecordsToDB(freq);
                                        freq.Contents.Clear();
                                        freq.Contents.TrimExcess();
                                    }
                                }

                                freq.Size = freq.Contents.Count;
                                freq.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {freq.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (dbExists)
                                {
                                    File.Delete(dbPath);
                                }
                            }
                        });

                        tasks.Add(nazekaFreqTask);
                    }

                    else
                    {
                        if (freq.Contents.Count > 0 && (!freq.Active || useDB))
                        {
                            if (useDB && !dbExists)
                            {
                                FreqDBManager.CreateDB(freq.Name);
                                FreqDBManager.InsertRecordsToDB(freq);
                            }

                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freqCleared = true;
                        }

                        freq.Ready = true;
                    }

                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    if (freq is { Active: true, Contents.Count: 0 } && (!useDB || !dbExists))
                    {
                        freq.Ready = false;
                        Task yomichanFreqTask = Task.Run(async () =>
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
                                }
                                else
                                {
                                    await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);

                                    if (useDB && !dbExists)
                                    {
                                        FreqDBManager.CreateDB(freq.Name);
                                        FreqDBManager.InsertRecordsToDB(freq);
                                        freq.Contents.Clear();
                                        freq.Contents.TrimExcess();
                                    }
                                }

                                freq.Size = freq.Contents.Count;
                                freq.Ready = true;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {freq.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;

                                if (dbExists)
                                {
                                    File.Delete(dbPath);
                                }
                            }
                        });

                        tasks.Add(yomichanFreqTask);
                    }

                    else
                    {
                        if (freq.Contents.Count > 0 && (!freq.Active || useDB))
                        {
                            if (useDB && !dbExists)
                            {
                                FreqDBManager.CreateDB(freq.Name);
                                FreqDBManager.InsertRecordsToDB(freq);
                            }

                            freq.Contents.Clear();
                            freq.Contents.TrimExcess();
                            freqCleared = true;
                        }

                        freq.Ready = true;
                    }

                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid freq type");
            }
        }

        if (tasks.Count > 0 || freqCleared)
        {
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

            Utils.Frontend.InvalidateDisplayCache();
            Utils.Frontend.Alert(AlertLevel.Success, "Finished loading frequency dictionaries");
        }

        FreqsReady = true;
    }

    public static async Task CreateDefaultFreqsConfig()
    {
        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(s_builtInFreqs, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write default Freqs config");
            Utils.Logger.Error(ex, "Couldn't write default Freqs config");
        }
    }

    public static async Task SerializeFreqs()
    {
        try
        {
            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(FreqDicts, Utils.s_jsoWithEnumConverterAndIndentation)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "SerializeFreqs failed");
            throw;
        }
    }

    internal static async Task DeserializeFreqs()
    {
        try
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
                    Utils.Logger.Fatal("Couldn't load Config/freqs.json");
                }
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "DeserializeFreqs failed");
            throw;
        }
    }
}
