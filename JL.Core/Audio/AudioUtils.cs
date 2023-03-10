using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Audio;
public class AudioUtils
{
    public static async Task<byte[]?> GetAudioFromUrl(Uri url)
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

            Utils.Logger.Information("Error getting audio from {url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error getting audio from {url}", url.OriginalString);
            return null;
        }
    }

    public static async Task<byte[]?> GetAudioFromJsonReturningUrl(Uri url)
    {
        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

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

            Utils.Logger.Information("Error getting audio from {url}", url.OriginalString);
            return null;
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Error getting audio from {url}", url.OriginalString);
            return null;
        }
    }

    public static async Task<byte[]?> GetAudioFromPath(Uri uri)
    {
        if (File.Exists(uri.OriginalString))
        {
            return await File.ReadAllBytesAsync(uri.OriginalString).ConfigureAwait(false);
        }

        Utils.Logger.Information("Error getting audio from {uri}", uri.AbsolutePath);
        return null;
    }

    public static async Task<byte[]?> GetAudioPrioritizedAudio(string foundSpelling, string reading)
    {
        byte[]? audioBytes = null;

        IOrderedEnumerable<KeyValuePair<string, AudioSource>> orderedAudioSorces = Storage.AudioSources.OrderBy(static a => a.Value.Priority);
        foreach ((string uri, AudioSource audioSorce) in orderedAudioSorces)
        {
            if (audioSorce.Active)
            {
                StringBuilder stringBuilder = new StringBuilder(uri)
                    .Replace("://localhost", "://127.0.0.1")
                    .Replace("{Term}", foundSpelling)
                    .Replace("{Reading}", reading);

                Uri normalizedUri = new(stringBuilder.ToString());

                switch (audioSorce.Type)
                {
                    case AudioSourceType.Url:
                        audioBytes = await GetAudioFromUrl(normalizedUri).ConfigureAwait(false);
                        break;
                    case AudioSourceType.UrlJson:
                        audioBytes = await GetAudioFromJsonReturningUrl(normalizedUri).ConfigureAwait(false);
                        break;
                    case AudioSourceType.LocalPath:
                        audioBytes = await GetAudioFromPath(normalizedUri).ConfigureAwait(false);
                        break;
                }

                if (audioBytes is not null)
                {
                    break;
                }
            }
        }

        return audioBytes;
    }
}
