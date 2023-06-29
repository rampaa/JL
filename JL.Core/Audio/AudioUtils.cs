using System.Net.Http.Json;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Audio;

public static class AudioUtils
{
    public static readonly Dictionary<string, AudioSource> AudioSources = new();

    private static readonly Dictionary<string, AudioSource> s_builtInAudioSources = new()
    {
        {
            "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}", new AudioSource(AudioSourceType.Url, true, 1)
        }
    };

    private static readonly Dictionary<string, string> s_mediaTypeToExtensionDict = new()
    {
        { "mpeg", "mp3" },
        { "3gpp", "3gp" },
        { "3gpp2", "3g2" },
        { "vorbis", "ogg" },
        { "vorbis-config", "ogg" },
        { "x-midi", "midi" }
    };

    private static async Task<AudioResponse?> GetAudioFromUrl(Uri url)
    {
        try
        {
            HttpResponseMessage response = await Networking.Client.GetAsync(url).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string audioFormat = response.Content.Headers.ContentType?.MediaType?.Split('/').LastOrDefault("mp3") ?? "mp3";

                if (s_mediaTypeToExtensionDict.TryGetValue(audioFormat, out string? fileSuffix))
                {
                    audioFormat = fileSuffix;
                }

                byte[] audioData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                return Utils.GetMd5String(audioData) is Networking.Jpod101NoAudioMd5Hash
                    ? null
                    : new AudioResponse(audioFormat, audioData);
            }

            Utils.Logger.Information("Error getting audio from {Url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error getting audio from {Url}", url.OriginalString);
            return null;
        }
    }

    private static async Task<AudioResponse?> GetAudioFromJsonReturningUrl(Uri url)
    {
        try
        {
            HttpResponseMessage response = await Networking.Client.GetAsync(url).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                JsonElement jsonElement = await response.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
                if (jsonElement.TryGetProperty("audioSources", out JsonElement audioSources))
                {
                    foreach (JsonElement audioSource in audioSources.EnumerateArray())
                    {
                        if (audioSource.TryGetProperty("url", out JsonElement audioSourcesJsonElement))
                        {
                            string? urlStr = audioSourcesJsonElement.GetString();

                            if (urlStr is not null)
                            {
                                urlStr = urlStr.Replace("://localhost", "://127.0.0.1", StringComparison.Ordinal);
                                if (Uri.TryCreate(urlStr, UriKind.Absolute, out Uri? resultUrl))
                                {
                                    AudioResponse? audioResponse = await GetAudioFromUrl(resultUrl).ConfigureAwait(false);

                                    if (audioResponse is not null)
                                    {
                                        return audioResponse;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            Utils.Logger.Information("Error getting audio from {Url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error getting audio from {Url}", url.OriginalString);
            return null;
        }
    }

    private static async Task<AudioResponse?> GetAudioFromPath(Uri uri)
    {
        if (File.Exists(uri.LocalPath))
        {
            string audioFormat = Path.GetExtension(uri.LocalPath)[1..];
            byte[] audioData = await File.ReadAllBytesAsync(uri.LocalPath).ConfigureAwait(false);
            return new AudioResponse(audioFormat, audioData);
        }

        Utils.Logger.Information("Error getting audio from {LocalPath}", uri.LocalPath);
        return null;
    }

    internal static async Task<AudioResponse?> GetPrioritizedAudio(string foundSpelling, string reading)
    {
        AudioResponse? audioResponse = null;

        IOrderedEnumerable<KeyValuePair<string, AudioSource>> orderedAudioSources = AudioSources.OrderBy(static a => a.Value.Priority);
        foreach ((string uri, AudioSource audioSource) in orderedAudioSources)
        {
            if (audioSource.Active)
            {
                StringBuilder stringBuilder = new StringBuilder(uri)
                    .Replace("://localhost", "://127.0.0.1")
                    .Replace("{Term}", foundSpelling)
                    .Replace("{Reading}", reading);

                Uri normalizedUri = new(stringBuilder.ToString());

                audioResponse = audioSource.Type switch
                {
                    AudioSourceType.Url => await GetAudioFromUrl(normalizedUri).ConfigureAwait(false),
                    AudioSourceType.UrlJson => await GetAudioFromJsonReturningUrl(normalizedUri).ConfigureAwait(false),
                    AudioSourceType.LocalPath => await GetAudioFromPath(normalizedUri).ConfigureAwait(false),
                    _ => audioResponse
                };

                if (audioResponse is not null)
                {
                    break;
                }
            }
        }

        return audioResponse;
    }

    public static async Task GetAndPlayAudio(string foundSpelling, string? reading)
    {
        if (string.IsNullOrEmpty(reading))
        {
            reading = foundSpelling;
        }

        AudioResponse? audioResponse = await GetPrioritizedAudio(foundSpelling, reading).ConfigureAwait(false);
        if (audioResponse is not null)
        {
            Utils.Frontend.PlayAudio(audioResponse.AudioData, audioResponse.AudioFormat, CoreConfig.AudioVolume / 100f);
            await Stats.IncrementStat(StatType.TimesPlayedAudio).ConfigureAwait(false);
        }
    }

    public static async Task SerializeAudioSources()
    {
        try
        {
            var jso = new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            await File.WriteAllTextAsync(Path.Join(Utils.ConfigPath, "AudioSourceConfig.json"),
                JsonSerializer.Serialize(AudioSources, jso)).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "SerializeAudioSources failed");
            throw;
        }
    }

    internal static void CreateDefaultAudioSourceConfig()
    {
        var jso = new JsonSerializerOptions
        {
            WriteIndented = true,
            Converters = { new JsonStringEnumConverter() }
        };

        try
        {
            _ = Directory.CreateDirectory(Utils.ConfigPath);
            File.WriteAllText(Path.Join(Utils.ConfigPath, "AudioSourceConfig.json"),
                JsonSerializer.Serialize(s_builtInAudioSources, jso));
        }
        catch (Exception ex)
        {
            Utils.Frontend.Alert(AlertLevel.Error, "Couldn't write default audio source config");
            Utils.Logger.Error(ex, "Couldn't write default audio source config");
        }
    }

    internal static async Task DeserializeAudioSources()
    {
        try
        {
            var jso = new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() }, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            Stream audioSourceConfigStream = new StreamReader(Path.Join(Utils.ConfigPath, "AudioSourceConfig.json")).BaseStream;
            await using (audioSourceConfigStream.ConfigureAwait(false))
            {
                Dictionary<string, AudioSource>? deserializedAudioSources = await JsonSerializer
                    .DeserializeAsync<Dictionary<string, AudioSource>>(audioSourceConfigStream, jso).ConfigureAwait(false);

                if (deserializedAudioSources is not null)
                {
                    IOrderedEnumerable<KeyValuePair<string, AudioSource>> audioSources = deserializedAudioSources.OrderBy(static d => d.Value.Priority);
                    int priority = 1;

                    foreach ((string? key, AudioSource? audioSource) in audioSources)
                    {
                        audioSource.Priority = priority;
                        AudioSources.Add(key, audioSource);

                        ++priority;
                    }
                }
                else
                {
                    Utils.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/AudioSourceConfig.json");
                    Utils.Logger.Fatal("Couldn't load Config/AudioSourceConfig.json");
                }
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.Fatal(ex, "DeserializeAudioSources failed");
            throw;
        }
    }
}
