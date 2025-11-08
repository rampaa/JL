namespace JL.Core.Utilities;

public static class FileStreamOptionsPresets
{
    public static readonly FileStreamOptions SyncReadFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.SequentialScan
    };

    internal static readonly FileStreamOptions s_asyncReadFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    internal static readonly FileStreamOptions s_asyncRead64KBufferFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1024 * 64,
        Options = FileOptions.Asynchronous | FileOptions.SequentialScan
    };

    internal static readonly FileStreamOptions s_syncRead64KBufferFso = new()
    {
        Mode = FileMode.Open,
        Access = FileAccess.Read,
        Share = FileShare.Read,
        BufferSize = 1024 * 64,
        Options = FileOptions.SequentialScan
    };

    internal static readonly FileStreamOptions s_asyncCreate64KBufferFso = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        BufferSize = 1024 * 64,
        Options = FileOptions.Asynchronous
    };

    internal static readonly FileStreamOptions s_asyncCreateFso = new()
    {
        Mode = FileMode.Create,
        Access = FileAccess.Write,
        Share = FileShare.None,
        Options = FileOptions.Asynchronous
    };
}
