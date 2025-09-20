using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiComposition;

internal static class KanjiCompositionDBManager
{
    private static readonly string s_dbPath = Path.Join(AppInfo.ResourcesPath, "Kanji Compositions.sqlite");

    public static string[]? GetRecordsFromDB(string kanji)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(s_dbPath);
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT compositions
            FROM record
            WHERE kanji = @kanji;
            """;

        _ = command.Parameters.AddWithValue("@kanji", kanji);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        _ = dataReader.Read();
        return MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(0));
    }
}
