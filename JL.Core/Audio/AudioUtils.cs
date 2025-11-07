using System.Collections.Frozen;
using System.Net.Http.Json;
using System.Runtime.Serialization;
using System.Text.Json;
using JL.Core.Frontend;
using JL.Core.Network;
using JL.Core.Statistics;
using JL.Core.Utilities;

namespace JL.Core.Audio;

public static class AudioUtils
{
    internal static readonly AudioResponse s_textToSpeechAudioResponse = new(AudioSourceType.TextToSpeech, "wav", null);

    public static readonly OrderedDictionary<string, AudioSource> AudioSources = new(StringComparer.Ordinal);

    private static readonly OrderedDictionary<string, AudioSource> s_builtInAudioSources = new(1, StringComparer.Ordinal)
    {
        {
            "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji={Term}&kana={Reading}", new AudioSource(AudioSourceType.Url, true, 1)
        }
    };

    private static readonly FrozenDictionary<string, string> s_mediaTypeToExtensionDict = new KeyValuePair<string, string>[]
    {
        new("mpeg", "mp3"),
        new("3gpp", "3gp"),
        new("3gpp2", "3g2"),
        new("vorbis", "ogg"),
        new("vorbis-config", "ogg"),
        new("x-midi", "midi")
    }.ToFrozenDictionary(StringComparer.Ordinal);

    private static async ValueTask<AudioResponse?> GetAudioFromUrl(Uri url)
    {
        try
        {
            using HttpResponseMessage response = await NetworkUtils.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                string audioFormat = response.Content.Headers.ContentType?.MediaType?.Split('/').LastOrDefault("mp3") ?? "mp3";
                if (s_mediaTypeToExtensionDict.TryGetValue(audioFormat, out string? fileSuffix))
                {
                    audioFormat = fileSuffix;
                }

                byte[] audioData = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                return HashUtils.GetMd5String(audioData) is NetworkUtils.Jpod101NoAudioMd5Hash
                    ? null
                    : new AudioResponse(AudioSourceType.Url, audioFormat, audioData);
            }

            LoggerManager.Logger.Information("Error getting audio from {Url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error getting audio from {Url}", url.OriginalString);
            return null;
        }
    }

    private static async ValueTask<AudioResponse?> GetAudioFromJsonReturningUrl(Uri url)
    {
        try
        {
            using HttpResponseMessage response = await NetworkUtils.Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

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
                                urlStr = urlStr.Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase);
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

            LoggerManager.Logger.Information("Error getting audio from {Url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Error getting audio from {Url}", url.OriginalString);
            return null;
        }
    }

    private static async ValueTask<AudioResponse?> GetAudioFromPath(Uri uri)
    {
        string fullPath = Path.GetFullPath(uri.LocalPath, AppInfo.ApplicationPath);
        if (File.Exists(fullPath))
        {
            string audioFormat = Path.GetExtension(fullPath)[1..];
            byte[] audioData = await File.ReadAllBytesAsync(fullPath).ConfigureAwait(false);
            return new AudioResponse(AudioSourceType.LocalPath, audioFormat, audioData);
        }

        LoggerManager.Logger.Information("Error getting audio from {LocalPath}", uri.LocalPath);
        return null;
    }

    internal static async ValueTask<AudioResponse?> GetPrioritizedAudio(string spelling, string reading)
    {
        AudioResponse? audioResponse = null;
        foreach ((string uri, AudioSource audioSource) in AudioSources)
        {
            if (audioSource.Active)
            {
                switch (audioSource.Type)
                {
                    case AudioSourceType.Url:
                    case AudioSourceType.UrlJson:
                    {
                        string normalizedUriStr = uri
                            .Replace("://localhost", "://127.0.0.1", StringComparison.OrdinalIgnoreCase)
                            .Replace("{Term}", spelling, StringComparison.OrdinalIgnoreCase)
                            .Replace("{Reading}", reading, StringComparison.OrdinalIgnoreCase);

                        Uri normalizedUri = new(normalizedUriStr);
                        audioResponse = audioSource.Type is AudioSourceType.Url
                            ? await GetAudioFromUrl(normalizedUri).ConfigureAwait(false)
                            : await GetAudioFromJsonReturningUrl(normalizedUri).ConfigureAwait(false);

                        break;
                    }

                    case AudioSourceType.LocalPath:
                    {
                        string normalizedUriStr = uri
                            .Replace("{Term}", spelling, StringComparison.OrdinalIgnoreCase)
                            .Replace("{Reading}", reading, StringComparison.OrdinalIgnoreCase);

                        normalizedUriStr = Path.GetFullPath(normalizedUriStr, AppInfo.ApplicationPath);

                        Uri normalizedUri = new(normalizedUriStr);
                        audioResponse = await GetAudioFromPath(normalizedUri).ConfigureAwait(false);

                        break;
                    }

                    case AudioSourceType.TextToSpeech:
                        await FrontendManager.Frontend.TextToSpeech(uri, reading).ConfigureAwait(false);
                        return s_textToSpeechAudioResponse;

                    default:
                        LoggerManager.Logger.Error("Invalid {TypeName} ({ClassName}.{MethodName}): {Value}", nameof(AudioSourceType), nameof(AudioUtils), nameof(GetPrioritizedAudio), audioSource.Type);
                        FrontendManager.Frontend.Alert(AlertLevel.Error, $"Invalid audio source type: {audioSource.Type}");
                        break;
                }

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
        reading ??= foundSpelling;

        AudioResponse? audioResponse = await GetPrioritizedAudio(foundSpelling, reading).ConfigureAwait(false);
        if (audioResponse?.AudioData is not null)
        {
            FrontendManager.Frontend.StopTextToSpeech();
            await FrontendManager.Frontend.PlayAudio(audioResponse.AudioData, audioResponse.AudioFormat).ConfigureAwait(false);
            StatsUtils.IncrementStat(StatType.TimesPlayedAudio);
        }
    }

    public static async Task SerializeAudioSources()
    {
        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "AudioSourceConfig.json"), FileStreamOptionsPresets.AsyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, AudioSources, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    public static async Task CreateDefaultAudioSourceConfig()
    {
        _ = Directory.CreateDirectory(AppInfo.ConfigPath);

        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "AudioSourceConfig.json"), FileStreamOptionsPresets.AsyncCreateFso);
        await using (fileStream.ConfigureAwait(false))
        {
            await JsonSerializer.SerializeAsync(fileStream, s_builtInAudioSources, JsonOptions.s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation).ConfigureAwait(false);
        }
    }

    internal static async Task DeserializeAudioSources()
    {
        FileStream fileStream = new(Path.Join(AppInfo.ConfigPath, "AudioSourceConfig.json"), FileStreamOptionsPresets.AsyncReadFso);
        await using (fileStream.ConfigureAwait(false))
        {
            Dictionary<string, AudioSource>? deserializedAudioSources = await JsonSerializer
                .DeserializeAsync<Dictionary<string, AudioSource>>(fileStream, JsonOptions.s_jsoWithEnumConverter)
                .ConfigureAwait(false);

            if (deserializedAudioSources is not null)
            {
                IOrderedEnumerable<KeyValuePair<string, AudioSource>> audioSources = deserializedAudioSources.OrderBy(static d => d.Value.Priority);
                int priority = 1;

                foreach ((string key, AudioSource audioSource) in audioSources)
                {
                    audioSource.Priority = priority;
                    AudioSources.Add(key, audioSource);

                    ++priority;
                }
            }
            else
            {
                FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't load Config/AudioSourceConfig.json");
                throw new SerializationException("Couldn't load Config/AudioSourceConfig.json");
            }
        }
    }
}
