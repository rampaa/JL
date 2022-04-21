using System.Runtime;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Dicts;
using JL.Core.Network;
using Serilog;
using Serilog.Core;

namespace JL.Core.Utilities;

public static class Utils
{
    public static readonly Logger Logger = new LoggerConfiguration().WriteTo!.File("Logs/log.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileTimeLimit: TimeSpan.FromDays(90),
            shared: true)
        .CreateLogger()!;

    public static void CreateDefaultDictsConfig()
    {
        var jso = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter(), }
        };

        try
        {
            Directory.CreateDirectory(Storage.ConfigPath);
            File.WriteAllText(Path.Join(Storage.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(Storage.BuiltInDicts, jso));
        }
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Couldn't write default Dicts config");
            Logger.Error(e, "Couldn't write default Dicts config");
        }
    }

    public static void SerializeDicts()
    {
        try
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                Converters = { new JsonStringEnumConverter(), },
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };

            File.WriteAllTextAsync(Path.Join(Storage.ConfigPath, "dicts.json"),
                JsonSerializer.Serialize(Storage.Dicts, jso));
        }
        catch (Exception e)
        {
            Logger.Fatal(e, "SerializeDicts failed");
            throw;
        }
    }

    private static async Task DeserializeDicts()
    {
        try
        {
            var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter(), } };

            Dictionary<DictType, Dict>? deserializedDicts = await JsonSerializer
                .DeserializeAsync<Dictionary<DictType, Dict>>(
                    new StreamReader(Path.Join(Storage.ConfigPath, "dicts.json")).BaseStream, jso)
                .ConfigureAwait(false);

            if (deserializedDicts != null)
            {
                foreach (Dict dict in deserializedDicts.Values)
                {
                    if (!Storage.Dicts.ContainsKey(dict.Type))
                    {
                        dict.Contents = new Dictionary<string, List<IResult>>();
                        Storage.Dicts.Add(dict.Type, dict);
                    }
                }
            }
            else
            {
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/dicts.json");
                Utils.Logger.Error("Couldn't load Config/dicts.json");
            }
        }
        catch (Exception e)
        {
            Utils.Logger.Fatal(e, "DeserializeDicts failed");
            throw;
        }
    }

    public static string GetMd5String(byte[] bytes)
    {
        byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")!).ComputeHash(bytes);
        string encoded = BitConverter.ToString(hash).Replace("-", string.Empty).ToLower();

        return encoded;
    }

    public static int FindWordBoundary(string text, int position)
    {
        int endPosition = -1;

        for (int i = 0; i < Storage.JapanesePunctuation.Count; i++)
        {
            int tempIndex = text.IndexOf(Storage.JapanesePunctuation[i], position, StringComparison.Ordinal);

            if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                endPosition = tempIndex;
        }

        if (endPosition == -1)
            endPosition = text.Length;

        return endPosition;
    }

    public static string FindSentence(string text, int position)
    {
        List<string> japanesePunctuationLite = new()
        {
            "。",
            "！",
            "？",
            "…",
            ".",
            "\n",
        };

        Dictionary<string, string> japaneseParentheses = new() { { "「", "」" }, { "『", "』" }, { "（", "）" }, };

        int startPosition = -1;
        int endPosition = -1;

        for (int i = 0; i < japanesePunctuationLite.Count; i++)
        {
            string punctuation = japanesePunctuationLite[i];

            int tempIndex = text.LastIndexOf(punctuation, position, StringComparison.Ordinal);

            if (tempIndex > startPosition)
                startPosition = tempIndex;

            tempIndex = text.IndexOf(punctuation, position, StringComparison.Ordinal);

            if (tempIndex != -1 && (endPosition == -1 || tempIndex < endPosition))
                endPosition = tempIndex;
        }

        ++startPosition;

        if (endPosition == -1)
            endPosition = text.Length - 1;

        string sentence = startPosition < endPosition
            ? text[startPosition..(endPosition + 1)].Trim('\n', '\t', '\r', ' ', '　')
            : "";

        if (sentence.Length > 1)
        {
            if (japaneseParentheses.ContainsValue(sentence.First().ToString()))
            {
                sentence = sentence[1..];
            }

            if (japaneseParentheses.ContainsKey(sentence.LastOrDefault().ToString()))
            {
                sentence = sentence[..^1];
            }

            if (japaneseParentheses.TryGetValue(sentence.FirstOrDefault().ToString(), out string? rightParenthesis))
            {
                if (sentence.Last().ToString() == rightParenthesis)
                    sentence = sentence[1..^1];

                else if (!sentence.Contains(rightParenthesis))
                    sentence = sentence[1..];

                else if (sentence.Contains(rightParenthesis))
                {
                    int numberOfLeftParentheses = sentence.Count(p => p == sentence[0]);
                    int numberOfRightParentheses = sentence.Count(p => p == rightParenthesis[0]);

                    if (numberOfLeftParentheses == numberOfRightParentheses + 1)
                        sentence = sentence[1..];
                }
            }

            else if (japaneseParentheses.ContainsValue(sentence.LastOrDefault().ToString()))
            {
                string leftParenthesis = japaneseParentheses.First(p => p.Value == sentence.Last().ToString()).Key;

                if (!sentence.Contains(leftParenthesis))
                    sentence = sentence[..^1];

                else if (sentence.Contains(leftParenthesis))
                {
                    int numberOfLeftParentheses = sentence.Count(p => p == leftParenthesis[0]);
                    int numberOfRightParentheses = sentence.Count(p => p == sentence.Last());

                    if (numberOfRightParentheses == numberOfLeftParentheses + 1)
                        sentence = sentence[..^1];
                }
            }
        }

        return sentence;
    }

    public static async Task GetAndPlayAudioFromJpod101(string foundSpelling, string? reading, float volume)
    {
        Utils.Logger.Information("Attempting to play audio from jpod101: {FoundSpelling} {Reading}", foundSpelling, reading);

        if (string.IsNullOrEmpty(reading))
            reading = foundSpelling;

        byte[]? sound = await Networking.GetAudioFromJpod101(foundSpelling, reading).ConfigureAwait(false);
        if (sound != null)
        {
            if (Utils.GetMd5String(sound) == Storage.Jpod101NoAudioMd5Hash)
            {
                // TODO sound = shortErrorSound
                return;
            }

            Storage.Frontend.PlayAudio(sound, volume);
            Storage.SessionStats.TimesPlayedAudio += 1;
        }
    }

    public static void CoreInitialize()
    {
        if (!File.Exists($"{Storage.ConfigPath}/dicts.json"))
            Utils.CreateDefaultDictsConfig();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_words.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_words.txt").Dispose();

        if (!File.Exists($"{Storage.ResourcesPath}/custom_names.txt"))
            File.Create($"{Storage.ResourcesPath}/custom_names.txt").Dispose();

        Utils.DeserializeDicts().ContinueWith(_ =>
        {
            Storage.LoadDictionaries().ContinueWith(_ =>
                {
                    Storage.InitializePoS().ContinueWith(_ =>
                    {
                        GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, false, true);
                    }).ConfigureAwait(false);
                }
            ).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
