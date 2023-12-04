using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EDICT.JMnedict;
internal static class JmnedictDBManager
{
    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                jmnedict_id INTEGER NOT NULL,
                primary_spelling TEXT NOT NULL,
                primary_spelling_in_hiragana TEXT NOT NULL,
                readings TEXT,
                alternative_spellings TEXT,
                glossary TEXT NOT NULL,
                name_types TEXT NOT NULL
            ) STRICT;
            """;

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        int id = 1;
        HashSet<JmnedictRecord> jmnedictRecords = dict.Contents.Values.SelectMany(static v => v).Select(static v => (JmnedictRecord)v).ToHashSet();
        foreach (JmnedictRecord record in jmnedictRecords)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();

            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, jmnedict_id, primary_spelling, primary_spelling_in_hiragana, readings, alternative_spellings, glossary, name_types)
                VALUES (@id, @jmnedict_id, @primary_spelling, @primary_spelling_in_hiragana, @readings, @alternative_spellings, @glossary, @name_types)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@jmnedict_id", record.Id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling_in_hiragana", JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling));
            _ = insertRecordCommand.Parameters.AddWithValue("@readings", record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@name_types", JsonSerializer.Serialize(record.NameTypes, Utils.s_jsoNotIgnoringNull));

            _ = insertRecordCommand.ExecuteNonQuery();

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_primary_spelling_in_hiragana ON record(primary_spelling_in_hiragana);";
        _ = createIndexCommand.ExecuteNonQuery();

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();

        dict.Ready = true;
    }

    public static Dictionary<string, IList<IDictRecord>> GetRecordsFromDB(string dbName, List<string> terms)
    {
        Dictionary<string, IList<IDictRecord>> results = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT r.primary_spelling_in_hiragana AS searchKey,
                   r.jmnedict_id AS id,
                   r.primary_spelling AS primarySpelling,
                   r.readings AS readings,
                   r.alternative_spellings AS alternativeSpellings,
                   r.glossary AS definitions,
                   r.name_types AS nameTypes
            FROM record r
            WHERE r.primary_spelling_in_hiragana = @term1
            """);

        for (int i = 1; i < terms.Count; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $"\nOR r.primary_spelling_in_hiragana = @term{i + 1}");
        }

        command.CommandText = queryBuilder.ToString();

        for (int i = 0; i < terms.Count; i++)
        {
            _ = command.Parameters.AddWithValue($"@term{i + 1}", terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            string searchKey = dataReader.GetString(nameof(searchKey));
            int id = dataReader.GetInt32(nameof(id));
            string primarySpelling = dataReader.GetString(nameof(primarySpelling));

            string[]? readings = null;
            if (dataReader[nameof(readings)] is string readingsFromDB)
            {
                readings = JsonSerializer.Deserialize<string[]>(readingsFromDB, Utils.s_jsoNotIgnoringNull);
            }

            string[]? alternativeSpellings = null;
            if (dataReader[nameof(alternativeSpellings)] is string alternativeSpellingsFromDB)
            {
                alternativeSpellings = JsonSerializer.Deserialize<string[]>(alternativeSpellingsFromDB, Utils.s_jsoNotIgnoringNull);
            }

            string[][] definitions = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;
            string[][] nameTypes = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(nameof(nameTypes)), Utils.s_jsoNotIgnoringNull)!;

            if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
            {
                result.Add(new JmnedictRecord(id, primarySpelling, alternativeSpellings, readings, definitions, nameTypes));
            }

            else
            {
                results[searchKey] = new List<IDictRecord> { new JmnedictRecord(id, primarySpelling, alternativeSpellings, readings, definitions, nameTypes) };
            }
        }

        return results;
    }
}
