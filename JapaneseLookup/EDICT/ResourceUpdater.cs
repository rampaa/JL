using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace JapaneseLookup.EDICT
{
    public static class ResourceUpdater
    {
        public static async void UpdateJMdict()
        {
            if (MessageBox.Show("Do you want to update JMdict?", "", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz");
                request.IfModifiedSince = File.GetLastWriteTime(Path.Join(ConfigManager.ApplicationPath, "Resources/JMdict.xml"));
                try
                {
                    request.GetResponse();
                    string downloadName = Path.Join(ConfigManager.ApplicationPath, "Resources/JMdict_e.gz");
                    MessageBox.Show("This may take a while. Please don't shut down the program until JMdict is updated.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    using WebClient client = new();
                    await Task.Run(() => client.DownloadFile("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz", downloadName));
                    await Task.Run(() => GzipDecompressor(new FileInfo(downloadName), Path.Join(ConfigManager.ApplicationPath, "Resources/JMdict.xml")));
                    File.Delete(downloadName);
                    MessageBox.Show("JMdict has been updated successfully.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }

                catch (WebException e)
                {
                    if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("JMdict is up to date.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    else
                        MessageBox.Show("Unexpected error while updating.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        public static async void UpdateJMnedict()
        {
            if (MessageBox.Show("Do you want to update JMnedict?", "", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz");
                request.IfModifiedSince = File.GetLastWriteTime(Path.Join(ConfigManager.ApplicationPath, "Resources/JMnedict.xml"));
                try
                {
                    request.GetResponse();
                    MessageBox.Show("This may take a while. Please don't shut down the program until JMnedict is updated.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    string downloadName = Path.Join(ConfigManager.ApplicationPath, "Resources/JMnedict.xml.gz");
                    using WebClient client = new();
                    await Task.Run(() => client.DownloadFile("http://ftp.edrdg.org/pub/Nihongo/JMnedict.xml.gz", downloadName));
                    await Task.Run(() => GzipDecompressor(new FileInfo(downloadName), Path.Join(ConfigManager.ApplicationPath, "Resources/JMnedict.xml")));
                    File.Delete(downloadName);
                    MessageBox.Show("JMnedict has been updated successfully.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }

                catch (WebException e)
                {
                    if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("JMnedict is up to date.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    else
                        MessageBox.Show("Unexpected error while updating.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        public static async void UpdateKanjidic()
        {
            if (MessageBox.Show("Do you want to update KANJIDIC2?", "", MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.Yes, MessageBoxOptions.DefaultDesktopOnly) == MessageBoxResult.Yes)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz");
                request.IfModifiedSince = File.GetLastWriteTime(Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml"));
                try
                {
                    request.GetResponse();
                    MessageBox.Show("This may take a while. Please don't shut down the program until KANJIDIC2 is updated.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    string downloadName = Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml.gz");
                    using WebClient client = new();
                    await Task.Run(() => client.DownloadFile("http://www.edrdg.org/kanjidic/kanjidic2.xml.gz", downloadName));
                    await Task.Run(() => GzipDecompressor(new FileInfo(downloadName), Path.Join(ConfigManager.ApplicationPath, "Resources/kanjidic2.xml")));
                    File.Delete(downloadName);
                    MessageBox.Show("KANJIDIC2 has been updated successfully.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }

                catch (WebException e)
                {
                    if (e.Response != null && ((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                        MessageBox.Show("KANJIDIC2 is up to date.", "", MessageBoxButton.OK, MessageBoxImage.Information, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                    else
                        MessageBox.Show("Unexpected error while updating.", "", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
                }
            }
        }

        private static void GzipDecompressor(FileInfo fileToDecompress, string filePath)
        {
            using FileStream originalFileStream = fileToDecompress.OpenRead();
            using FileStream decompressedFileStream = File.Create(filePath);
            using GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(decompressedFileStream);
        }
    }
}
