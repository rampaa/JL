using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Timers;
using JL.Core.Config;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.Bool;
using Timer = System.Timers.Timer;

namespace JL.Core.Network;

public static class NetworkUtils
{
    public static readonly HttpClient Client = new(new HttpClientHandler
    {
        UseProxy = false,
        CheckCertificateRevocationList = true
    });

    internal static readonly Encoding s_utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    internal const string Jpod101NoAudioMd5Hash = "7E2C2F954EF6051373BA916F000168DC";
    private static readonly Uri s_gitHubApiUrlForLatestJLRelease = new("https://api.github.com/repos/rampaa/JL/releases/latest");
    private static readonly Timer s_updaterTimer = new();
    private static readonly AtomicBool s_updatingJL = new(false);

    public static async Task CheckForJLUpdates(bool isAutoCheck)
    {
        if (!s_updatingJL.TrySetTrue())
        {
            return;
        }

        try
        {
            using HttpRequestMessage gitHubApiRequest = new(HttpMethod.Get, s_gitHubApiUrlForLatestJLRelease);
            gitHubApiRequest.Headers.Add("User-Agent", "JL");

            using HttpResponseMessage gitHubApiResponse = await Client.SendAsync(gitHubApiRequest, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(false);

            if (gitHubApiResponse.IsSuccessStatusCode)
            {
                JsonDocument jsonDocument;
                Stream githubApiResultStream = await gitHubApiResponse.Content.ReadAsStreamAsync().ConfigureAwait(false);
                await using (githubApiResultStream.ConfigureAwait(false))
                {
                    jsonDocument = await JsonDocument.ParseAsync(githubApiResultStream).ConfigureAwait(false);
                }

                using (jsonDocument)
                {
                    JsonElement rootElement = jsonDocument.RootElement;
                    string? tagName = rootElement.GetProperty("tag_name").GetString();
                    Debug.Assert(tagName is not null);

                    Version latestJLVersion = new(tagName);

                    if (latestJLVersion > AppInfo.JLVersion)
                    {
                        bool foundRelease = false;
                        string architecture = RuntimeInformation.ProcessArchitecture is Architecture.Arm64
                            ? "arm64"
                            : AppInfo.Is64BitProcess
                                ? "x64"
                                : "x86";

                        JsonElement assets = jsonDocument.RootElement.GetProperty("assets");

                        foreach (JsonElement asset in assets.EnumerateArray())
                        {
                            string? latestReleaseUrl = asset.GetProperty("browser_download_url").GetString();
                            Debug.Assert(latestReleaseUrl is not null);

                            if (latestReleaseUrl.AsSpan().Contains(architecture, StringComparison.Ordinal))
                            {
                                foundRelease = true;

                                string? changelog = rootElement.GetProperty("body").GetString();
                                changelog = string.IsNullOrWhiteSpace(changelog) ? "" : $"\n\nChangelog:\n{changelog}";

                                if (await FrontendManager.Frontend.ShowYesNoDialogAsync(
                                        string.Create(CultureInfo.InvariantCulture, $"JL v{latestJLVersion} is available.{changelog}\n\nWould you like to download it now?"), "Update JL?").ConfigureAwait(false))
                                {
                                    await FrontendManager.Frontend.ShowOkDialogAsync(
                                        "This may take a while. Please don't manually shut down the program until it's updated.", "Info").ConfigureAwait(false);

                                    await FrontendManager.Frontend.UpdateJL(new Uri(latestReleaseUrl)).ConfigureAwait(false);
                                }

                                break;
                            }
                        }

                        if (!isAutoCheck && !foundRelease)
                        {
                            await FrontendManager.Frontend.ShowOkDialogAsync("JL is up to date", "Info").ConfigureAwait(false);
                        }
                    }

                    else if (!isAutoCheck)
                    {
                        await FrontendManager.Frontend.ShowOkDialogAsync("JL is up to date", "Info").ConfigureAwait(false);
                    }
                }
            }

            else
            {
                LoggerManager.Logger.Error("Couldn't check for JL updates. GitHub API problem. {StatusCode} {ReasonPhrase}", gitHubApiResponse.StatusCode, gitHubApiResponse.ReasonPhrase);
                FrontendManager.Frontend.Alert(AlertLevel.Error, "Couldn't check for JL updates. GitHub API problem.");
            }
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't check for JL updates");
            FrontendManager.Frontend.Alert(AlertLevel.Warning, "Couldn't check for JL updates");
        }
        finally
        {
            s_updatingJL.SetFalse();
        }
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
        await ResourceUpdater.AutoUpdateDicts().ConfigureAwait(false);
        await ResourceUpdater.AutoUpdateFreqDicts().ConfigureAwait(false);

        if (CoreConfigManager.Instance.CheckForJLUpdatesOnStartUp)
        {
            await CheckForJLUpdates(true).ConfigureAwait(false);
        }
    }
}
