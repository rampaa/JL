namespace JL.Core.Utilities.Database;

internal ref struct DBState(bool useDB, bool dbExists, bool dbExisted)
{
    public bool UseDB { get; } = useDB;
    public bool DBExists { get; set; } = dbExists;
    public bool DBExisted { get; } = dbExisted;
}
