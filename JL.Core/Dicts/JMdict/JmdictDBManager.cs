using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;
internal static class JmdictDBManager
{
    public const int Version = 1;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};");
        connection.Open();
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
                antonyms TEXT
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                record_id INTEGER NOT NULL,
                search_key TEXT NOT NULL,
                PRIMARY KEY (record_id, search_key),
                FOREIGN KEY (record_id) REFERENCES record (id) ON DELETE CASCADE
            ) WITHOUT ROWID, STRICT;
            """;
        _ = command.ExecuteNonQuery();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = string.Create(CultureInfo.InvariantCulture, $"PRAGMA user_version = {Version};");
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        _ = command.ExecuteNonQuery();
    }

    public static void InsertRecordsToDB(Dict dict)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadWrite");
        connection.Open();
        using DbTransaction transaction = connection.BeginTransaction();

        Dictionary<JmdictRecord, List<string>> recordToKeysDict = [];
        foreach ((string key, IList<IDictRecord> records) in dict.Contents)
        {
            int recordCount = records.Count;
            for (int i = 0; i < recordCount; i++)
            {
                JmdictRecord record = (JmdictRecord)records[i];
                if (recordToKeysDict.TryGetValue(record, out List<string>? keys))
                {
                    keys.Add(key);
                }
                else
                {
                    recordToKeysDict[record] = [key];
                }
            }
        }

        ulong id = 1;
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
            _ = insertRecordCommand.Parameters.AddWithValue("@primary_spelling_orthography_info", record.PrimarySpellingOrthographyInfo is not null ? JsonSerializer.Serialize(record.PrimarySpellingOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings", record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@alternative_spellings_orthography_info", record.AlternativeSpellingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@readings", record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@readings_orthography_info", record.ReadingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.ReadingsOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@reading_restrictions", record.ReadingRestrictions is not null ? JsonSerializer.Serialize(record.ReadingRestrictions, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary", JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@glossary_info", record.DefinitionInfo is not null ? JsonSerializer.Serialize(record.DefinitionInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@part_of_speech", JsonSerializer.Serialize(record.WordClasses, Utils.s_jsoNotIgnoringNull));
            _ = insertRecordCommand.Parameters.AddWithValue("@spelling_restrictions", record.SpellingRestrictions is not null ? JsonSerializer.Serialize(record.SpellingRestrictions, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@fields", record.Fields is not null ? JsonSerializer.Serialize(record.Fields, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@misc", record.Misc is not null ? JsonSerializer.Serialize(record.Misc, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@dialects", record.Dialects is not null ? JsonSerializer.Serialize(record.Dialects, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@loanword_etymology", record.LoanwordEtymology is not null ? JsonSerializer.Serialize(record.LoanwordEtymology, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@cross_references", record.RelatedTerms is not null ? JsonSerializer.Serialize(record.RelatedTerms, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.Parameters.AddWithValue("@antonyms", record.Antonyms is not null ? JsonSerializer.Serialize(record.Antonyms, Utils.s_jsoNotIgnoringNull) : DBNull.Value);
            _ = insertRecordCommand.ExecuteNonQuery();

            int keyCount = keys.Count;
            for (int i = 0; i < keyCount; i++)
            {
                using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
                insertSearchKeyCommand.CommandText =
                    """
                    INSERT INTO record_search_key(record_id, search_key)
                    VALUES (@record_id, @search_key)
                    """;

                _ = insertSearchKeyCommand.Parameters.AddWithValue("@record_id", id);
                _ = insertSearchKeyCommand.Parameters.AddWithValue("@search_key", keys[i]);

                _ = insertSearchKeyCommand.ExecuteNonQuery();
            }

            ++id;
        }

        using SqliteCommand createIndexCommand = connection.CreateCommand();
        createIndexCommand.CommandText = "CREATE INDEX IF NOT EXISTS ix_record_search_key_search_key ON record_search_key(search_key);";
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

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dbName)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        StringBuilder queryBuilder = new(
            """
            SELECT rsk.search_key AS searchKey,
                   r.edict_id AS id,
                   r.primary_spelling AS primarySpelling,
                   r.primary_spelling_orthography_info AS primarySpellingOrthographyInfo,
                   r.spelling_restrictions AS spellingRestrictions,
                   r.alternative_spellings AS alternativeSpellings,
                   r.alternative_spellings_orthography_info AS alternativeSpellingsOrthographyInfo,
                   r.readings AS readings,
                   r.readings_orthography_info AS readingsOrthographyInfo,
                   r.reading_restrictions AS readingRestrictions,
                   r.glossary AS definitions,
                   r.glossary_info AS definitionInfo,
                   r.part_of_speech AS wordClasses,
                   r.fields AS fields,
                   r.misc AS misc,
                   r.dialects AS dialects,
                   r.loanword_etymology AS loanwordEtymology,
                   r.cross_references AS relatedTerms,
                   r.antonyms AS antonyms
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        int termCount = terms.Count;
        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        _ = queryBuilder.Append(')');

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = queryBuilder.ToString();
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < termCount; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (dataReader.HasRows)
        {
            Dictionary<string, IList<IDictRecord>> results = new(StringComparer.Ordinal);
            while (dataReader.Read())
            {
                JmdictRecord record = GetRecord(dataReader);

                string searchKey = dataReader.GetString(nameof(searchKey));
                if (results.TryGetValue(searchKey, out IList<IDictRecord>? result))
                {
                    result.Add(record);
                }

                else
                {
                    results[searchKey] = [record];
                }
            }

            return results;
        }

        return null;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = new($"Data Source={DBUtils.GetDictDBPath(dict.Name)};Mode=ReadOnly");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT json_group_array(rsk.search_key) AS searchKeys,
                   r.edict_id AS id,
                   r.primary_spelling AS primarySpelling,
                   r.primary_spelling_orthography_info AS primarySpellingOrthographyInfo,
                   r.spelling_restrictions AS spellingRestrictions,
                   r.alternative_spellings AS alternativeSpellings,
                   r.alternative_spellings_orthography_info AS alternativeSpellingsOrthographyInfo,
                   r.readings AS readings,
                   r.readings_orthography_info AS readingsOrthographyInfo,
                   r.reading_restrictions AS readingRestrictions,
                   r.glossary AS definitions,
                   r.glossary_info AS definitionInfo,
                   r.part_of_speech AS wordClasses,
                   r.fields AS fields,
                   r.misc AS misc,
                   r.dialects AS dialects,
                   r.loanword_etymology AS loanwordEtymology,
                   r.cross_references AS relatedTerms,
                   r.antonyms AS antonyms
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            JmdictRecord record = GetRecord(dataReader);

            string[] searchKeys = JsonSerializer.Deserialize<string[]>(dataReader.GetString(nameof(searchKeys)), Utils.s_jsoNotIgnoringNull)!;
            for (int i = 0; i < searchKeys.Length; i++)
            {
                string searchKey = searchKeys[i];
                if (dict.Contents.TryGetValue(searchKey, out IList<IDictRecord>? result))
                {
                    result.Add(record);
                }
                else
                {
                    dict.Contents[searchKey] = [record];
                }
            }
        }

        foreach ((string key, IList<IDictRecord> recordList) in dict.Contents)
        {
            dict.Contents[key] = recordList.ToArray();
        }

        dict.Contents = dict.Contents.ToFrozenDictionary(StringComparer.Ordinal);
    }

    private static JmdictRecord GetRecord(SqliteDataReader dataReader)
    {
        int id = dataReader.GetInt32(nameof(id));
        string primarySpelling = dataReader.GetString(nameof(primarySpelling));

        string[]? primarySpellingOrthographyInfo = null;
        if (dataReader[nameof(primarySpellingOrthographyInfo)] is string primarySpellingOrthographyInfoFromDB)
        {
            primarySpellingOrthographyInfo = JsonSerializer.Deserialize<string[]>(primarySpellingOrthographyInfoFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? spellingRestrictions = null;
        if (dataReader[nameof(spellingRestrictions)] is string spellingRestrictionsFromDB)
        {
            spellingRestrictions = JsonSerializer.Deserialize<string[]?[]>(spellingRestrictionsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? alternativeSpellings = null;
        if (dataReader[nameof(alternativeSpellings)] is string alternativeSpellingsFromDB)
        {
            alternativeSpellings = JsonSerializer.Deserialize<string[]>(alternativeSpellingsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? alternativeSpellingsOrthographyInfo = null;
        if (dataReader[nameof(alternativeSpellingsOrthographyInfo)] is string alternativeSpellingsOrthographyInfoFromDB)
        {
            alternativeSpellingsOrthographyInfo = JsonSerializer.Deserialize<string[]?[]>(alternativeSpellingsOrthographyInfoFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]? readings = null;
        if (dataReader[nameof(readings)] is string readingsFromDB)
        {
            readings = JsonSerializer.Deserialize<string[]>(readingsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? readingRestrictions = null;
        if (dataReader[nameof(readingRestrictions)] is string readingRestrictionsFromDB)
        {
            readingRestrictions = JsonSerializer.Deserialize<string[]?[]>(readingRestrictionsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? readingsOrthographyInfo = null;
        if (dataReader[nameof(readingsOrthographyInfo)] is string readingsOrthographyInfoFromDB)
        {
            readingsOrthographyInfo = JsonSerializer.Deserialize<string[]?[]>(readingsOrthographyInfoFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[][] definitions = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(nameof(definitions)), Utils.s_jsoNotIgnoringNull)!;
        string[][] wordClasses = JsonSerializer.Deserialize<string[][]>(dataReader.GetString(nameof(wordClasses)), Utils.s_jsoNotIgnoringNull)!;

        string[]?[]? fields = null;
        if (dataReader[nameof(fields)] is string fieldsFromDB)
        {
            fields = JsonSerializer.Deserialize<string[]?[]>(fieldsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? misc = null;
        if (dataReader[nameof(misc)] is string miscFromDB)
        {
            misc = JsonSerializer.Deserialize<string[]?[]>(miscFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string?[]? definitionInfo = null;
        if (dataReader[nameof(definitionInfo)] is string definitionInfoFromDB)
        {
            definitionInfo = JsonSerializer.Deserialize<string?[]>(definitionInfoFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? dialects = null;
        if (dataReader[nameof(dialects)] is string dialectsFromDB)
        {
            dialects = JsonSerializer.Deserialize<string[]?[]>(dialectsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        LoanwordSource[]?[]? loanwordEtymology = null;
        if (dataReader[nameof(loanwordEtymology)] is string loanwordEtymologyFromDB)
        {
            loanwordEtymology = JsonSerializer.Deserialize<LoanwordSource[]?[]>(loanwordEtymologyFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? relatedTerms = null;
        if (dataReader[nameof(relatedTerms)] is string relatedTermsFromDB)
        {
            relatedTerms = JsonSerializer.Deserialize<string[]?[]>(relatedTermsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        string[]?[]? antonyms = null;
        if (dataReader[nameof(antonyms)] is string antonymsFromDB)
        {
            antonyms = JsonSerializer.Deserialize<string[]?[]>(antonymsFromDB, Utils.s_jsoNotIgnoringNull);
        }

        return new JmdictRecord(id, primarySpelling, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, definitions, wordClasses, spellingRestrictions, readingRestrictions, fields, misc, definitionInfo, dialects, loanwordEtymology, relatedTerms, antonyms);
    }
}
