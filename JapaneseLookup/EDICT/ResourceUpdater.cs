using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

namespace JapaneseLookup.EDICT
{
    public static class ResourceUpdater
    {
        public static async Task<bool> UpdateResource(string resourcePath, Uri resourceDownloadUri, string resourceName, bool isUpdate, bool noPrompt)
        {
            if (!isUpdate || MessageBox.Show($"Do you want to download the latest version of {resourceName}?", "", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                HttpRequestMessage request = new(HttpMethod.Get, resourceDownloadUri);

                if (File.Exists(Path.Join(ConfigManager.ApplicationPath, resourcePath)))
                    request.Headers.IfModifiedSince = File.GetLastWriteTime(Path.Join(ConfigManager.ApplicationPath, resourcePath));

                if (!noPrompt)
                    MessageBox.Show($"This may take a while. Please don't shut down the program until {resourceName} is downloaded.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                var response = await ConfigManager.Client.SendAsync(request).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                    await GzipStreamDecompressor(responseStream, Path.Join(ConfigManager.ApplicationPath, resourcePath)).ConfigureAwait(false);

                    if (!noPrompt)
                        MessageBox.Show($"{resourceName} has been downloaded successfully.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);

                    return true;
                }

                else if (response.StatusCode == HttpStatusCode.NotModified && !noPrompt)
                {
                    MessageBox.Show($"{resourceName} is up to date.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }

                else if (!noPrompt)
                {
                    MessageBox.Show($"Unexpected error while downloading {resourceName}.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
            return false;
        }

        private static async Task GzipStreamDecompressor(Stream stream, string filePath)
        {
            using FileStream decompressedFileStream = File.Create(filePath);
            using GZipStream decompressionStream = new(stream, CompressionMode.Decompress);
            await decompressionStream.CopyToAsync(decompressedFileStream).ConfigureAwait(false);
        }
    }
}
