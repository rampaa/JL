using System.Globalization;
using System.Text.Json;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Utilities;

namespace JL.Core.Freqs;

public static class FreqUtils
{
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, Freq> FreqDicts { get; } = new();

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new(3)
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_vns.json"),
                true, 1, 57273)
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_narou.json"),
                false, 2, 75588)
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)",
                Path.Join(Utils.ResourcesPath, "freqlist_novels.json"),
                false, 3, 114348)
        }
    };

    internal static string GetDBPath(string dbName)
    {
        return string.Create(CultureInfo.InvariantCulture, $"{Path.Join(Utils.ResourcesPath, dbName)} Frequency Dictionary.sqlite");
    }

    public static async Task LoadFrequencies()
    {
        FreqsReady = false;

        bool freqCleared = false;
        bool freqRemoved = false;

        List<Task> tasks = new();

        foreach (Freq freq in FreqDicts.Values.ToList())
        {
            switch (freq.Type)
            {
                case FreqType.Nazeka:
                    if (freq is { Active: true, Contents.Count: 0 })
                    {
                        Task nazekaFreqTask = Task.Run(async () =>
                        {
                            try
                            {
                                await FrequencyNazekaLoader.Load(freq).ConfigureAwait(false);
                                freq.Size = freq.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {freq.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(nazekaFreqTask);
                    }

                    else if (freq is { Active: false, Contents.Count: > 0 })
                    {
                        freq.Contents.Clear();
                        freq.Contents.TrimExcess();
                        freqCleared = true;
                    }

                    break;

                case FreqType.Yomichan:
                case FreqType.YomichanKanji:
                    if (freq is { Active: true, Contents.Count: 0 })
                    {
                        Task yomichanFreqTask = Task.Run(async () =>
                        {
                            try
                            {
                                await FrequencyYomichanLoader.Load(freq).ConfigureAwait(false);
                                freq.Size = freq.Contents.Count;
                            }

                            catch (Exception ex)
                            {
                                Utils.Frontend.Alert(AlertLevel.Error, string.Create(CultureInfo.InvariantCulture, $"Couldn't import {freq.Name}"));
                                Utils.Logger.Error(ex, "Couldn't import {FreqName}", freq.Type);
                                _ = FreqDicts.Remove(freq.Name);
                                freqRemoved = true;
                            }
                        });

                        tasks.Add(yomichanFreqTask);
                    }

                    else if (freq is { Active: false, Contents.Count: > 0 })
                    {
                        freq.Contents.Clear();
                        freq.Contents.TrimExcess();
                        freqCleared = true;
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
                        freq.Contents = freq.Size is not 0
                            ? new Dictionary<string, IList<FrequencyRecord>>(freq.Size)
                            : freq.Type switch
                            {
                                FreqType.Yomichan => new Dictionary<string, IList<FrequencyRecord>>(1504512),
                                FreqType.YomichanKanji => new Dictionary<string, IList<FrequencyRecord>>(169623),
                                FreqType.Nazeka => new Dictionary<string, IList<FrequencyRecord>>(114348),
                                _ => new Dictionary<string, IList<FrequencyRecord>>(500000)
                            };

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
