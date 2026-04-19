using JL.Core.Utilities;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiComposition;

internal static class KanjiCompositionDBManager
{
    private static readonly string s_dbPath = Path.Join(AppInfo.ResourcesPath, "Kanji Compositions.sqlite");

    private const string SingleTermQuery =
        """
        SELECT compositions
        FROM record
        WHERE kanji = @kanji;
        """;

    public static string[]? GetRecordsFromDB(string kanji)
    {
        using SqliteConnection? connection = DBUtils.CreateReadOnlyDBConnection(s_dbPath);
        if (connection is null)
        {
            LoggerManager.Logger.Error("Failed to create a read-only connection to the database for dict: {DBPath}.", s_dbPath);
            // FrontendManager.Frontend.Alert(AlertLevel.Error, $"Failed to create a read-only connection to the database for dict: {s_dbPath}.");
            return null;
        }

        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;
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
