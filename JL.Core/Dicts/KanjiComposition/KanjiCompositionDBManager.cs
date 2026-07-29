using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
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
            LoggerManager.Logger.Error("Failed to create connection for {ReadOnlyConnectionString}.", s_readOnlyDBConnectionString);
            return null;
        }

        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;
        _ = command.Parameters.AddWithValue("@kanji", kanji);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        _ = dataReader.Read();

        // The "record" table is created as WITHOUT ROWID because we don't need a numeric primary key.
        // As a result, dataReader.GetStream cannot use its fast SqliteBlob path.
        // We therefore read the BLOBs directly instead of using GetNullableValueFromBlobStream.
        return MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(0));
    }
}
