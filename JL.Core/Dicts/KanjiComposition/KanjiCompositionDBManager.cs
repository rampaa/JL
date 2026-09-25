using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiComposition;

internal static class KanjiCompositionDBManager
{
    private const string DBName = "Kanji Compositions.sqlite";
    private static readonly string s_dbPath = Path.Join(AppInfo.ResourcesPath, DBName);
    private static readonly string s_readOnlyDBConnectionString = DBUtils.GetReadOnlyConnectionString(s_dbPath);

    private const string SingleTermQuery =
        """
        SELECT compositions
        FROM record
        WHERE kanji = @kanji;
        """;

    //public static void AnalyzeAndVacuum()
    //{
    //    using SqliteConnection connection = DBUtils.CreateDBConnection(s_dbPath);
    //    using SqliteCommand command = connection.CreateCommand();
    //
    //    command.CommandText = $"ANALYZE;";
    //    _ = command.ExecuteNonQuery();
    //
    //    command.CommandText = $"VACUUM;";
    //    _ = command.ExecuteNonQuery();
    //}

    public static string[]? GetRecordsFromDB(string kanji)
    {
        using SqliteConnection? connection = DBUtils.CreateDBConnectionForReadOnlyConnectionString(s_readOnlyDBConnectionString);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}", s_readOnlyDBConnectionString);
            return null;
        }

        using SqliteRecordReader reader = new(connection, SingleTermQuery);
        reader.Bind(1, kanji);

        // The "record" table is created as WITHOUT ROWID because we don't need a numeric primary key.
        // As a result, SqliteBlob cannot be used to read its BLOBs.
        return reader.Read()
            ? reader.Deserialize<string[]>(0)
            : null;
    }
}
