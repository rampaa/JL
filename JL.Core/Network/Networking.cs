using System.Runtime.InteropServices;
using System.Text.Json;
using System.Timers;
using JL.Core.Config;
using JL.Core.Dicts;
using JL.Core.Utilities;
using Timer = System.Timers.Timer;

namespace JL.Core.Network;

public static class Networking
{
    public static readonly HttpClient Client = new(new HttpClientHandler
    {
        UseProxy = false,
        CheckCertificateRevocationList = true
    });

    internal const string Jpod101NoAudioMd5Hash = "7E2C2F954EF6051373BA916F000168DC";
    private static readonly Uri s_gitHubApiUrlForLatestJLRelease = new("https://api.github.com/repos/rampaa/JL/releases/latest");
    private static readonly Timer s_updaterTimer = new();
    private static bool s_updatingJL; // false

    public static async Task CheckForJLUpdates(bool isAutoCheck)
    {
        if (s_updatingJL)
        {
            return;
        }

        s_updatingJL = true;
        try
        {
            using HttpRequestMessage gitHubApiRequest = new(HttpMethod.Get, s_gitHubApiUrlForLatestJLRelease);
            gitHubApiRequest.Headers.Add("User-Agent", "JL");

            using HttpResponseMessage gitHubApiResponse = await Client.SendAsync(gitHubApiRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (gitHubApiResponse.IsSuccessStatusCode)
            {
                Stream githubApiResultStream = await gitHubApiResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using (githubApiResultStream.ConfigureAwait(false))
                {
                    JsonDocument jsonDocument = await JsonDocument.ParseAsync(githubApiResultStream).ConfigureAwait(false);
                    JsonElement rootElement = jsonDocument.RootElement;
                    Version latestJLVersion = new(rootElement.GetProperty("tag_name").GetString()!);

                    if (latestJLVersion > Utils.JLVersion)
                    {
                        bool foundRelease = false;
                        string architecture = RuntimeInformation.ProcessArchitecture is Architecture.Arm64
                            ? "arm64"
                            : Environment.Is64BitProcess
                                ? "x64"
                                : "x86";

                        JsonElement assets = jsonDocument.RootElement.GetProperty("assets");

                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            string latestReleaseUrl = asset.GetProperty("browser_download_url").GetString()!;
                            if (latestReleaseUrl.Contains(architecture, StringComparison.Ordinal))
                            {
                                foundRelease = true;

                                if (Utils.Frontend.ShowYesNoDialog(
                                        "A new version of JL is available. Would you like to download it now?", "Update JL?"))
                                {
                                    Utils.Frontend.ShowOkDialog(
                                        "This may take a while. Please don't manually shut down the program until it's updated.", "Info");

                                    await Utils.Frontend.UpdateJL(new Uri(latestReleaseUrl)).ConfigureAwait(false);
                                }

                                break;
                            }
                        }

                        if (!isAutoCheck && !foundRelease)
                        {
                            Utils.Frontend.ShowOkDialog("JL is up to date", "Info");
                        }
                    }

                    else if (!isAutoCheck)
                    {
                        Utils.Frontend.ShowOkDialog("JL is up to date", "Info");
                    }
                }
            }

            else
            {
                Utils.Logger.Error("Couldn't check for JL updates. GitHub API problem. {StatusCode} {ReasonPhrase}", gitHubApiResponse.StatusCode, gitHubApiResponse.ReasonPhrase);
                Utils.Frontend.Alert(AlertLevel.Error, "Couldn't check for JL updates. GitHub API problem.");
            }
        }
        catch (Exception ex)
        {
            Utils.Logger.Error(ex, "Couldn't check for JL updates");
            Utils.Frontend.Alert(AlertLevel.Warning, "Couldn't check for JL updates");
        }

        s_updatingJL = false;
    }

    internal static void InitializeUpdaterTimer()
    {
        s_updaterTimer.Interval = TimeSpan.FromHours(12).TotalMilliseconds;
        s_updaterTimer.Elapsed += CheckForUpdates;
        s_updaterTimer.AutoReset = true;
        s_updaterTimer.Enabled = true;
    }

    // ReSharper disable once AsyncVoidMethod
    private static async void CheckForUpdates(object? sender, ElapsedEventArgs e)
    {
        await DictUpdater.AutoUpdateBuiltInDicts().ConfigureAwait(false);

        if (CoreConfigManager.Instance.CheckForJLUpdatesOnStartUp)
        {
            await CheckForJLUpdates(true).ConfigureAwait(false);
        }
    }
}
