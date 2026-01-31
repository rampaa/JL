using System.IO.Compression;

namespace JL.Core.Utilities;

public static class ArchiveUtils
{
    public static void DecompressZipStream(Stream stream, string destinationDirectory)
    {
        using ZipArchive archive = new(stream, ZipArchiveMode.Read, false);
        archive.ExtractToDirectory(destinationDirectory);

        // TODO: Uncomment this for .NET 10
        //ZipArchive archive = new(stream, ZipArchiveMode.Read, false);
        //await using (archive.ConfigureAwait(false))
        //{
        //    await archive.ExtractToDirectoryAsync(destinationDirectory).ConfigureAwait(false);
        //}
    }
}
