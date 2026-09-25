using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace JL.Core.Utilities.Database;

internal static partial class SqliteNativeMethods
{
    [LibraryImport("e_sqlite3", EntryPoint = "sqlite3_column_text16")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial nint GetColumnText16(nint statement, int index);

    [LibraryImport("e_sqlite3", EntryPoint = "sqlite3_column_bytes16")]
    [DefaultDllImportSearchPaths(DllImportSearchPath.SafeDirectories)]
    [UnmanagedCallConv(CallConvs = [typeof(CallConvCdecl)])]
    internal static partial int GetColumnBytes16(nint statement, int index);
}
