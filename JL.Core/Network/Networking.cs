using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Network;

public static class Networking
{
    public static async Task<byte[]?> GetAudioFromJpod101(string foundSpelling, string reading)
    {
        try
        {
            Uri uri = new(
                "http://assets.languagepod101.com/dictionary/japanese/audiomp3.php?kanji=" +
                foundSpelling +
                "&kana=" +
                reading
            );
            HttpResponseMessage getResponse = await Storage.Client.GetAsync(uri).ConfigureAwait(false);
            return await getResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Error getting audio from jpod101");
            Utils.Logger.Error(e, "Error getting audio from jpod101");
            return null;
        }
    }

    public static async Task CheckForJLUpdates(bool isAutoCheck)
    {
        try
        {
            HttpRequestMessage gitHubApiRequest = new(HttpMethod.Get, Storage.GitHubApiUrlForLatestJLRelease);
            gitHubApiRequest.Headers.Add("User-Agent", "JL");

            HttpResponseMessage gitHubApiResponse = await Storage.Client.SendAsync(gitHubApiRequest).ConfigureAwait(false);

            if (gitHubApiResponse.IsSuccessStatusCode)
            {
                Stream githubApiResultStream = await gitHubApiResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                JsonDocument jsonDocument = await JsonDocument.ParseAsync(githubApiResultStream).ConfigureAwait(false);
                JsonElement rootElement = jsonDocument.RootElement;
                Version latestJLVersion = new(rootElement.GetProperty("tag_name").ToString());

                if (latestJLVersion > Storage.JLVersion)
                {
                    bool foundRelease = false;
                    string architecture = Environment.Is64BitProcess ? "x64" : "x86";
                    JsonElement assets = jsonDocument.RootElement.GetProperty("assets");

                    foreach (JsonElement asset in assets.EnumerateArray())
                    {
                        string latestReleaseUrl = asset.GetProperty("browser_download_url").ToString();

                        // Add OS check?
                        if (latestReleaseUrl.Contains(architecture))
                        {
                            foundRelease = true;

                            if (Storage.Frontend.ShowYesNoDialog(
                                "A new version of JL is available. Would you like to download it now?", "Update JL?"))
                            {
                                Storage.Frontend.ShowOkDialog(
                                    "This may take a while. Please don't manually shut down the program until it's updated.", "Info");

                                await Storage.Frontend.UpdateJL(new Uri(latestReleaseUrl)).ConfigureAwait(false);
                            }
                            break;
                        }
                    }

                    if (!isAutoCheck && !foundRelease)
                    {
                        Storage.Frontend.ShowOkDialog("JL is up to date", "Info");
                    }

                }

                else if (!isAutoCheck)
                {
                    Storage.Frontend.ShowOkDialog("JL is up to date", "Info");
                }
            }

            else
            {
                Utils.Logger.Error("Couldn't update JL. GitHub API problem. {StatusCode} {ReasonPhrase}", gitHubApiResponse.StatusCode, gitHubApiResponse.ReasonPhrase);
                Storage.Frontend.Alert(AlertLevel.Error, "Couldn't update JL");
            }
        }
        catch
        {
            Utils.Logger.Warning("Couldn't update JL");
            Storage.Frontend.Alert(AlertLevel.Warning, "Couldn't update JL");
        }
    }
}
