using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.KanjiComposition;

internal static class KanjiCompositionDBManager
{
    private static readonly string s_dbPath = Path.Join(Utils.ResourcesPath, "Kanji Compositions.sqlite");

    public static string[]? GetRecordsFromDB(string kanji)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(s_dbPath);
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

    //private enum ColumnIndex
    //{
    //    Kanji = 0,
    //    Compositions
    //}

    //public static FrozenDictionary<string, string[]> LoadFromDB()
    //{
    //    return ReadRecords().ToFrozenDictionary(StringComparer.Ordinal);
    //}

    //private static IEnumerable<KeyValuePair<string, string[]>> ReadRecords()
    //{
    //    using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(s_dbPath);
    //    using SqliteCommand command = connection.CreateCommand();

    //    command.CommandText = "SELECT kanji, compositions FROM record;";

    //    using SqliteDataReader dataReader = command.ExecuteReader();
    //    while (dataReader.Read())
    //    {
    //        string kanji = dataReader.GetString((int)ColumnIndex.Kanji);
    //        string[] compositions = MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>((int)ColumnIndex.Compositions));

    //        yield return new KeyValuePair<string, string[]>(kanji, compositions);
    //    }
    //}

    //public static void CreateDB()
    //{
    //    using SqliteConnection connection = DBUtils.CreateDBConnection(s_dbPath);
    //    using SqliteCommand command = connection.CreateCommand();

    //    command.CommandText =
    //        """
    //        CREATE TABLE IF NOT EXISTS record
    //        (
    //            kanji TEXT NOT NULL PRIMARY KEY,
    //            compositions BLOB
    //        ) WITHOUT ROWID, STRICT;
    //        """;
    //    _ = command.ExecuteNonQuery();
    //}

    //public static void InsertRecordsToDB(IDictionary<string, string[]> dict)
    //{
    //    using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(s_dbPath);
    //    using SqliteTransaction transaction = connection.BeginTransaction();

    //    using SqliteCommand insertRecordCommand = connection.CreateCommand();
    //    insertRecordCommand.CommandText =
    //        """
    //        INSERT INTO record (kanji, compositions)
    //        VALUES (@kanji, @compositions);
    //        """;

    //    SqliteParameter kanjiParam = new("@kanji", SqliteType.Text);
    //    SqliteParameter compositionsParam = new("@compositions", SqliteType.Blob);
    //    insertRecordCommand.Parameters.AddRange([kanjiParam, compositionsParam]);
    //    insertRecordCommand.Prepare();

    //    foreach ((string kanji, string[] compositions) in dict)
    //    {
    //        kanjiParam.Value = kanji;
    //        compositionsParam.Value = MessagePackSerializer.Serialize(compositions);
    //        _ = insertRecordCommand.ExecuteNonQuery();
    //    }

    //    transaction.Commit();

    //    using SqliteCommand analyzeCommand = connection.CreateCommand();
    //    analyzeCommand.CommandText = "ANALYZE;";
    //    _ = analyzeCommand.ExecuteNonQuery();

    //    using SqliteCommand vacuumCommand = connection.CreateCommand();
    //    vacuumCommand.CommandText = "VACUUM;";
    //    _ = vacuumCommand.ExecuteNonQuery();
    //}
}
