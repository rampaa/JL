using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EDICT.JMnedict;
internal class JmnedictDBManager
{
    public static async Task CreateJmnedictDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        await connection.OpenAsync().ConfigureAwait(false);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                jmnedict_id INTEGER NOT NULL,
                primary_spelling TEXT NOT NULL,
                readings TEXT,
                alternative_spellings TEXT,
                glossary TEXT NOT NULL,
                name_types TEXT NOT NULL,
                cross_references TEXT
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                record_id INTEGER NOT NULL,
                search_key TEXT NOT NULL,
                PRIMARY KEY (record_id, search_key),
                FOREIGN KEY (record_id) REFERENCES record (id) ON DELETE CASCADE
            ) STRICT;
            """;

        _ = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static async Task InsertToJmnedictDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        await connection.OpenAsync().ConfigureAwait(false);
        using DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        int id = 1;
        HashSet<JmnedictRecord> jmnedictRecords = dict.Contents.Values.SelectMany(v => v).Select(v => (JmnedictRecord)v).ToHashSet();
        foreach (JmnedictRecord record in jmnedictRecords)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();

            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, jmnedict_id, primary_spelling, readings, alternative_spellings, glossary, name_types)
                VALUES (@id, @jmnedict_id, @primary_spelling, @readings, @alternative_spellings, @glossary, @name_types)
                """;

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@jmnedict_id", record.Id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@readings", record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jsoWithIndentation) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoWithIndentation) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoWithIndentation));
            _ = insertRecordCommand.Parameters.AddWithValue("@name_types", JsonSerializer.Serialize(record.NameTypes, Utils.s_jsoWithIndentation));

            _ = await insertRecordCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            using SqliteCommand insertPrimarySpellingCommand = connection.CreateCommand();
            insertPrimarySpellingCommand.CommandText =
                    """
                        INSERT INTO record_search_key(record_id, search_key)
                        VALUES (@record_id, @search_key)
                        """;
            _ = insertPrimarySpellingCommand.Parameters.AddWithValue("@record_id", id);
            _ = insertPrimarySpellingCommand.Parameters.AddWithValue("@search_key", JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling));
            _ = await insertPrimarySpellingCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            if (record.Readings is not null)
            {
                HashSet<string> uniqueReadingsInHiragana = record.Readings.Select(JapaneseUtils.KatakanaToHiragana).ToHashSet();
                foreach (string readingInHiragana in uniqueReadingsInHiragana)
                {
                    using SqliteCommand insertReadingCommand = connection.CreateCommand();

                    insertReadingCommand.CommandText =
                        """
                            INSERT INTO record_search_key(record_id, search_key)
                            VALUES (@record_id, @search_key)
                            """;

                    _ = insertReadingCommand.Parameters.AddWithValue("@record_id", id);
                    _ = insertReadingCommand.Parameters.AddWithValue("@search_key", readingInHiragana);

                    _ = await insertReadingCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
                }
            }

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();

        createIndexCommand.CommandText =
            """
            CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);
            """;

        _ = await createIndexCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = await analyzeCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = await vacuumCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
    }

    public static bool GetRecordsFromJmnedictDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.jmnedict_id as id, r.primary_spelling AS primarySpelling, r.readings AS readings, r.alternative_spellings as alternativeSpellings, r.glossary AS definitions, r.name_types AS nameTypes
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            int id = (int)(long)dataReader["id"];
            string primarySpelling = (string)dataReader["primarySpelling"];

            object readingsFromDB = dataReader["readings"];
            string[]? readings = readingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)readingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object alternativeSpellingsFromDB = dataReader["alternativeSpellings"];
            string[]? alternativeSpellings = alternativeSpellingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)alternativeSpellingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            string[][] definitions = JsonSerializer.Deserialize<string[][]>((string)dataReader["definitions"])!;
            string[][] nameTypes = JsonSerializer.Deserialize<string[][]>((string)dataReader["nameTypes"])!;

            records.Add(new JmnedictRecord(id, primarySpelling, alternativeSpellings, readings, definitions, nameTypes));
        }

        if (records.Count > 0)
        {
            value = records;
            return true;
        }

        value = null;
        return false;
    }
}
