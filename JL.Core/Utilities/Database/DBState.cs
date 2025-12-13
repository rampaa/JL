using System.Runtime.InteropServices;

namespace JL.Core.Utilities.Database;

[StructLayout(LayoutKind.Auto)]
internal readonly ref struct DBState(bool useDB, bool dbExists, bool dbExisted)
{
    public bool UseDB { get; } = useDB;
    public bool DBExists { get; } = dbExists;
    public bool DBExisted { get; } = dbExisted;
}
