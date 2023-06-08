using System.Runtime;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Freqs.FrequencyNazeka;
using JL.Core.Freqs.FrequencyYomichan;
using JL.Core.Utilities;

namespace JL.Core.Freqs;

public static class FreqUtils
{
    public static bool FreqsReady { get; private set; } = false;
    public static Dictionary<string, Freq> FreqDicts { get; internal set; } = new();

    internal static readonly Dictionary<string, Freq> s_builtInFreqs = new()
    {
        {
            "VN (Nazeka)",
            new Freq(FreqType.Nazeka, "VN (Nazeka)", "Resources/freqlist_vns.json", true, 1, 57273)
        },

        {
            "Narou (Nazeka)",
            new Freq(FreqType.Nazeka, "Narou (Nazeka)", "Resources/freqlist_narou.json", false, 2, 75588)
        },

        {
            "Novel (Nazeka)",
            new Freq(FreqType.Nazeka, "Novel (Nazeka)", "Resources/freqlist_novels.json", false, 3, 114348)
        }
    };

    public static async Task LoadFrequencies(bool runGC = true)
    {
        FreqsReady = false;

        List<Task> tasks = new();
        bool freqRemoved = false;

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
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
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
                        freqRemoved = true;
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
                                Utils.Frontend.Alert(AlertLevel.Error, $"Couldn't import {freq.Name}");
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
                        freqRemoved = true;
                    }
                    break;

                default:
                    throw new ArgumentOutOfRangeException(null, "Invalid freq type");
            }
        }

        if (tasks.Count > 0 || freqRemoved)
        {
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }

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

            Utils.Frontend.InvalidateDisplayCache();

            if (runGC)
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
            }
        }

        FreqsReady = true;
    }

    public static void CreateDefaultFreqsConfig()
    {
        var jso = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            File.WriteAllText(Path.Join(Utils.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(s_builtInFreqs, jso));
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
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "freqs.json"),
                JsonSerializer.Serialize(FreqDicts, jso)).ConfigureAwait(false);
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
            var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            Stream freqStream = new StreamReader(Path.Join(Utils.ConfigPath, "freqs.json")).BaseStream;
            await using (freqStream.ConfigureAwait(false))
            {
                Dictionary<string, Freq>? deserializedFreqs = await JsonSerializer
                    .DeserializeAsync<Dictionary<string, Freq>>(freqStream, jso).ConfigureAwait(false);

                if (deserializedFreqs is not null)
                {
                    IOrderedEnumerable<Freq> orderedFreqs = deserializedFreqs.Values.OrderBy(static f => f.Priority);
                    int priority = 1;

                    foreach (Freq freq in orderedFreqs)
                    {
                        freq.Contents = freq.Size is not 0
                            ? new Dictionary<string, List<FrequencyRecord>>(freq.Size)
                            : freq.Type switch
                            {
                                FreqType.Yomichan => new Dictionary<string, List<FrequencyRecord>>(1504512),
                                FreqType.YomichanKanji => new Dictionary<string, List<FrequencyRecord>>(169623),
                                FreqType.Nazeka => new Dictionary<string, List<FrequencyRecord>>(114348),
                                _ => new Dictionary<string, List<FrequencyRecord>>(500000)
                            };

                        freq.Priority = priority;
                        ++priority;

                        string relativePath = Path.GetRelativePath(Utils.ApplicationPath, freq.Path);
                        freq.Path = relativePath.StartsWith('.') ? Path.GetFullPath(relativePath) : relativePath;

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
