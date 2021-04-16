using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace JapaneseLookup.EDICT
{
    class JMdictUpdater
    {
        public static void JMDictUpdater()
        {

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz");
            request.IfModifiedSince = File.GetLastWriteTime("../net5.0-windows/Resources/JMdict.xml");
            try
            {
                request.GetResponse();
                using WebClient client = new();
                client.DownloadFile("http://ftp.edrdg.org/pub/Nihongo/JMdict_e.gz", "JMdict_e.gz");
                GzipDecompresser(new FileInfo("JMdict_e.gz"));
                File.Delete("JMdict_e.gz");
            }

            catch (WebException e)
            {
                if (e.Response != null)
                    if (((HttpWebResponse)e.Response).StatusCode == HttpStatusCode.NotModified)
                        System.Diagnostics.Debug.WriteLine("Dictionary is up to date."); // Show this as a popup.

                    else
                        System.Diagnostics.Debug.WriteLine("Unexpected error while updating the dictionary."); // Show this as a popup.
            }
        }

        public static void GzipDecompresser(FileInfo fileToDecompress)
        {
            using FileStream originalFileStream = fileToDecompress.OpenRead();
            string currentFileName = fileToDecompress.FullName;
            string newFileName = "../net5.0-windows/Resources/JMdict.xml";

            using FileStream decompressedFileStream = File.Create(newFileName);
            using GZipStream decompressionStream = new(originalFileStream, CompressionMode.Decompress);
            decompressionStream.CopyTo(decompressedFileStream);
        }
    }
}
