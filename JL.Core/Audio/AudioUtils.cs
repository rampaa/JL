using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Audio;

internal static class AudioUtils
{
    private static async Task<byte[]?> GetAudioFromUrl(Uri url)
    {
        try
        {
            HttpResponseMessage response = await Storage.Client.GetAsync(url).ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                byte[] audioBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

                return Utils.GetMd5String(audioBytes) is Storage.Jpod101NoAudioMd5Hash
                    ? null
                    : audioBytes;
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

    private static async Task<byte[]?> GetAudioFromJsonReturningUrl(Uri url)
    {
        try
        {
            HttpResponseMessage response = await Storage.Client.GetAsync(url).ConfigureAwait(false);

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
                                urlStr = urlStr.Replace("://localhost", "://127.0.0.1");
                                if (Uri.TryCreate(urlStr, UriKind.Absolute, out Uri? resultUrl))
                                {
                                    byte[]? audioBytes = await GetAudioFromUrl(resultUrl).ConfigureAwait(false);

                                    if (audioBytes is not null)
                                    {
                                        return audioBytes;
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

    private static async Task<byte[]?> GetAudioFromPath(Uri uri)
    {
        if (File.Exists(uri.LocalPath))
        {
            return await File.ReadAllBytesAsync(uri.LocalPath).ConfigureAwait(false);
        }

        Utils.Logger.Information("Error getting audio from {LocalPath}", uri.LocalPath);
        return null;
    }

    public static async Task<byte[]?> GetAudioPrioritizedAudio(string foundSpelling, string reading)
    {
        byte[]? audioBytes = null;

        IOrderedEnumerable<KeyValuePair<string, AudioSource>> orderedAudioSources = Storage.AudioSources.OrderBy(static a => a.Value.Priority);
        foreach ((string uri, AudioSource audioSource) in orderedAudioSources)
        {
            if (audioSource.Active)
            {
                StringBuilder stringBuilder = new StringBuilder(uri)
                    .Replace("://localhost", "://127.0.0.1")
                    .Replace("{Term}", foundSpelling)
                    .Replace("{Reading}", reading);

                Uri normalizedUri = new(stringBuilder.ToString());

                audioBytes = audioSource.Type switch
                {
                    AudioSourceType.Url => await GetAudioFromUrl(normalizedUri).ConfigureAwait(false),
                    AudioSourceType.UrlJson => await GetAudioFromJsonReturningUrl(normalizedUri).ConfigureAwait(false),
                    AudioSourceType.LocalPath => await GetAudioFromPath(normalizedUri).ConfigureAwait(false),
                    _ => audioBytes
                };

                if (audioBytes is not null)
                {
                    break;
                }
            }
        }

        return audioBytes;
    }
}
