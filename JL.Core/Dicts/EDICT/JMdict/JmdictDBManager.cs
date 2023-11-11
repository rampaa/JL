using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.EDICT.JMdict;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EDICT.Jmdict;
internal class JmdictDBManager
{
    public static async Task CreateJmdictDB(string dbName)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};"));
        await connection.OpenAsync().ConfigureAwait(false);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                id INTEGER NOT NULL PRIMARY KEY,
                edict_id INTEGER NOT NULL,
                primary_spelling TEXT NOT NULL,
                primary_spelling_orthography_info TEXT,
                readings TEXT,
                alternative_spellings TEXT,
                alternative_spellings_orthography_info TEXT,
                readings_orthography_info TEXT,
                reading_restrictions TEXT,
                glossary TEXT NOT NULL,
                glossary_info TEXT,
                part_of_speech TEXT NOT NULL,
                spelling_restrictions TEXT,
                fields TEXT,
                misc TEXT,
                dialects TEXT,
                loanword_etymology TEXT,
                cross_references TEXT,
                antonyms TEXT,
                gloss_types TEXT
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

    public static async Task InsertToJmdictDB(Dict dict)
    {
        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dict.Name)};Mode=ReadWrite"));
        await connection.OpenAsync().ConfigureAwait(false);
        using DbTransaction transaction = await connection.BeginTransactionAsync().ConfigureAwait(false);

        Dictionary<JmdictRecord, List<string>> recordToKeysDict = new();
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            for (int i = 0; i < records.Count; i++)
            {
                JmdictRecord record = (JmdictRecord)records[i];
                if (recordToKeysDict.TryGetValue(record, out List<string>? keys))
                {
                    keys.Add(key);
                }
                else
                {
                    recordToKeysDict[record] = new List<string>() { key };
                }
            }
        }

        int id = 1;
        foreach ((JmdictRecord record, List<string> keys) in recordToKeysDict)
        {
            using SqliteCommand insertRecordCommand = connection.CreateCommand();

            insertRecordCommand.CommandText =
                """
                INSERT INTO record (id, edict_id, primary_spelling, primary_spelling_orthography_info, alternative_spellings, alternative_spellings_orthography_info, readings, readings_orthography_info, reading_restrictions, glossary, glossary_info, part_of_speech, spelling_restrictions, fields, misc, dialects, loanword_etymology, cross_references, antonyms)
                VALUES (@id, @edict_id, @primary_spelling, @primary_spelling_orthography_info, @alternative_spellings, @alternative_spellings_orthography_info, @readings, @readings_orthography_info, @reading_restrictions, @glossary, @glossary_info, @part_of_speech, @spelling_restrictions, @fields, @misc, @dialects, @loanword_etymology, @cross_references, @antonyms)
                """;

            insertRecordCommand.Prepare();

            _ = insertRecordCommand.Parameters.AddWithValue("@id", id);
            _ = insertRecordCommand.Parameters.AddWithValue("@edict_id", record.Id);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling", record.PrimarySpelling);
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling_orthography_info", record.PrimarySpellingOrthographyInfo is not null ? JsonSerializer.Serialize(record.PrimarySpellingOrthographyInfo, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings_orthography_info", record.AlternativeSpellingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@readings", record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@readings_orthography_info", record.ReadingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.ReadingsOrthographyInfo, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading_restrictions", record.ReadingRestrictions is not null ? JsonSerializer.Serialize(record.ReadingRestrictions, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoWithIndentationNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary_info", record.DefinitionInfo is not null ? JsonSerializer.Serialize(record.DefinitionInfo, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@part_of_speech", JsonSerializer.Serialize(record.WordClasses, Utils.s_jsoWithIndentationNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling_restrictions", record.SpellingRestrictions is not null ? JsonSerializer.Serialize(record.SpellingRestrictions, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@fields", record.Fields is not null ? JsonSerializer.Serialize(record.Fields, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@misc", record.Misc is not null ? JsonSerializer.Serialize(record.Misc, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@dialects", record.Dialects is not null ? JsonSerializer.Serialize(record.Dialects, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@loanword_etymology", record.LoanwordEtymology is not null ? JsonSerializer.Serialize(record.LoanwordEtymology, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@cross_references", record.RelatedTerms is not null ? JsonSerializer.Serialize(record.RelatedTerms, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@antonyms", record.Antonyms is not null ? JsonSerializer.Serialize(record.Antonyms, Utils.s_jsoWithIndentationNotIgnoringNull) : DBNull.Value);
            _ = await insertRecordCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

            for (int i = 0; i < keys.Count; i++)
            {
                using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
                insertSearchKeyCommand.CommandText =
                    """
                    INSERT INTO record_search_key(record_id, search_key)
                    VALUES (@record_id, @search_key)
                    """;

                _ = insertSearchKeyCommand.Parameters.AddWithValue("@record_id", id);
                _ = insertSearchKeyCommand.Parameters.AddWithValue("@search_key", keys[i]);

                _ = await insertSearchKeyCommand.ExecuteNonQueryAsync().ConfigureAwait(false);
            }

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";
        _ = await createIndexCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        await transaction.CommitAsync().ConfigureAwait(false);

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = await analyzeCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = await vacuumCommand.ExecuteNonQueryAsync().ConfigureAwait(false);

        dict.Ready = true;
    }

    public static bool GetRecordsFromJmdictDB(string dbName, string term, [MaybeNullWhen(false)] out IList<IDictRecord> value)
    {
        List<IDictRecord> records = new();

        using SqliteConnection connection = new(string.Create(CultureInfo.InvariantCulture, $"Data Source={DictUtils.GetDBPath(dbName)};Mode=ReadOnly"));
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.edict_id as id, r.primary_spelling AS primarySpelling, r.primary_spelling_orthography_info AS primarySpellingOrthographyInfo, r.spelling_restrictions AS spellingRestrictions, r.alternative_spellings as alternativeSpellings, r.alternative_spellings_orthography_info AS alternativeSpellingsOrthographyInfo, r.readings AS readings, r.readings_orthography_info AS readingsOrthographyInfo, r.reading_restrictions AS readingRestrictions, r.glossary AS definitions, r.glossary_info AS definitionInfo, r.part_of_speech AS wordClasses, r.fields AS fields, r.misc AS misc, r.dialects AS dialects, r.loanword_etymology AS loanwordEtymology, r.cross_references AS relatedTerms, r.antonyms AS antonyms
            FROM record r
            INNER JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key = @term
            """;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            int id = (int)(long)dataReader["id"];
            string primarySpelling = (string)dataReader["primarySpelling"];

            object primarySpellingOrthographyInfoFromDB = dataReader["primarySpellingOrthographyInfo"];
            string[]? primarySpellingOrthographyInfo = primarySpellingOrthographyInfoFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)primarySpellingOrthographyInfoFromDB, Utils.s_jsoWithIndentation)
                : null;

            object spellingRestrictionsFromDB = dataReader["spellingRestrictions"];
            string[]?[]? spellingRestrictions = spellingRestrictionsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)spellingRestrictionsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object alternativeSpellingsFromDB = dataReader["alternativeSpellings"];
            string[]? alternativeSpellings = alternativeSpellingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)alternativeSpellingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object alternativeSpellingsOrthographyInfoFromDB = dataReader["alternativeSpellingsOrthographyInfo"];
            string[]?[]? alternativeSpellingsOrthographyInfo = alternativeSpellingsOrthographyInfoFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)alternativeSpellingsOrthographyInfoFromDB, Utils.s_jsoWithIndentation)
                : null;

            object readingsFromDB = dataReader["readings"];
            string[]? readings = readingsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]>((string)readingsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object readingRestrictionsFromDB = dataReader["readingRestrictions"];
            string[]?[]? readingRestrictions = readingRestrictionsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)readingRestrictionsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object readingsOrthographyInfoFromDB = dataReader["readingsOrthographyInfo"];
            string[]?[]? readingsOrthographyInfo = readingsOrthographyInfoFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)readingsOrthographyInfoFromDB, Utils.s_jsoWithIndentation)
                : null;

            string[][] definitions = JsonSerializer.Deserialize<string[][]>((string)dataReader["definitions"])!;
            string[][] wordClasses = JsonSerializer.Deserialize<string[][]>((string)dataReader["wordClasses"])!;

            object fieldsFromDB = dataReader["fields"];
            string[]?[]? fields = fieldsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)fieldsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object miscFromDB = dataReader["misc"];
            string[]?[]? misc = fieldsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)fieldsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object definitionInfoFromDB = dataReader["definitionInfo"];
            string?[]? definitionInfo = definitionInfoFromDB is not DBNull
                ? JsonSerializer.Deserialize<string?[]>((string)definitionInfoFromDB, Utils.s_jsoWithIndentation)
                : null;

            object dialectsFromDB = dataReader["dialects"];
            string[]?[]? dialects = dialectsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)dialectsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object loanwordEtymologyFromDB = dataReader["loanwordEtymology"];
            LoanwordSource[]?[]? loanwordEtymology = loanwordEtymologyFromDB is not DBNull
                ? JsonSerializer.Deserialize<LoanwordSource[]?[]>((string)loanwordEtymologyFromDB, Utils.s_jsoWithIndentation)
                : null;

            object relatedTermsFromDB = dataReader["relatedTerms"];
            string[]?[]? relatedTerms = relatedTermsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)relatedTermsFromDB, Utils.s_jsoWithIndentation)
                : null;

            object antonymsFromDB = dataReader["antonyms"];
            string[]?[]? antonyms = antonymsFromDB is not DBNull
                ? JsonSerializer.Deserialize<string[]?[]>((string)antonymsFromDB, Utils.s_jsoWithIndentation)
                : null;

            records.Add(new JmdictRecord(id, primarySpelling, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, definitions, wordClasses, spellingRestrictions, readingRestrictions, fields, misc, definitionInfo, dialects, loanwordEtymology, relatedTerms, antonyms));
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
