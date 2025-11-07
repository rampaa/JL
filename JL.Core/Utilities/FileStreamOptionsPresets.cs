namespace JL.Core.Utilities;

public static class FileStreamOptionsPresets
{
    public static readonly FileStreamOptions SyncReadFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1024 * 64,
        Options = FileOptions.SequentialScan
    };

    public static readonly FileStreamOptions AsyncReadFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    public static readonly FileStreamOptions AsyncRead64KBufferFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1024 * 64,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    public static readonly FileStreamOptions SyncRead64KBufferFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1024 * 64,
        Options = FileOptions.SequentialScan
    };

    public static readonly FileStreamOptions AsyncCreate64KBufferFso = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        BufferSize = 1024 * 64,
        Options = FileOptions.Asynchronous
    };

    public static readonly FileStreamOptions AsyncCreateFso = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous
    };
}
