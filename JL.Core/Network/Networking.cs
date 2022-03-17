using JL.Core.Utilities;

namespace JL.Core.Network;

public static class Networking
{
    public static async Task<byte[]> GetAudioFromJpod101(string foundSpelling, string reading)
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
            // todo mining storemediafile thingy
            return await getResponse.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }
        catch (Exception e)
        {
            Storage.Frontend.Alert(AlertLevel.Error, "Error getting audio from jpod101");
            Utils.Logger.Error(e, "Error getting audio from jpod101");
            return null;
        }
    }

    public static async void CheckForJLUpdates(bool isAutoCheck)
    {
        try
        {
            HttpResponseMessage response = await Storage.Client.GetAsync(Storage.RepoUrl + "releases/latest");
            string responseUri = response.RequestMessage!.RequestUri!.ToString();
            Version latestVersion =
                new(responseUri[(responseUri.LastIndexOf("/", StringComparison.Ordinal) + 1)..]);
            if (latestVersion > Storage.Version)
            {
                if (Storage.Frontend.ShowYesNoDialog(
                        "A new version of JL is available. Would you like to download it now?", ""))
                {
                    Storage.Frontend.ShowOkDialog(
                        "This may take a while. Please don't manually shut down the program until it's updated.", "");

                    await Storage.Frontend.UpdateJL(latestVersion).ConfigureAwait(false);
                }
            }

            else if (!isAutoCheck)
            {
                Storage.Frontend.ShowOkDialog("JL is up to date", "");
            }
        }
        catch
        {
            Utils.Logger.Warning("Couldn't update JL");
            Storage.Frontend.Alert(AlertLevel.Warning, "Couldn't update JL");
        }
    }
}
