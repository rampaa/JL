using System.Collections.Frozen;
using System.Data;
using System.Globalization;
using System.Text.Json;
using JL.Core.Utilities;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictDBManager
{
    public const int Version = 2;

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
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

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (id, edict_id, primary_spelling, primary_spelling_orthography_info, alternative_spellings, alternative_spellings_orthography_info, readings, readings_orthography_info, reading_restrictions, glossary, glossary_info, part_of_speech, spelling_restrictions, fields, misc, dialects, loanword_etymology, cross_references, antonyms)
            VALUES (@id, @edict_id, @primary_spelling, @primary_spelling_orthography_info, @alternative_spellings, @alternative_spellings_orthography_info, @readings, @readings_orthography_info, @reading_restrictions, @glossary, @glossary_info, @part_of_speech, @spelling_restrictions, @fields, @misc, @dialects, @loanword_etymology, @cross_references, @antonyms);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@edict_id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling_orthography_info", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@alternative_spellings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@alternative_spellings_orthography_info", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@readings", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@readings_orthography_info", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@reading_restrictions", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@glossary_info", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@part_of_speech", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@spelling_restrictions", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@fields", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@misc", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@dialects", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@loanword_etymology", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@cross_references", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@antonyms", SqliteType.Text);
        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key);
            """;

        _ = insertSearchKeyCommand.Parameters.Add("@record_id", SqliteType.Integer);
        _ = insertSearchKeyCommand.Parameters.Add("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Prepare();

        foreach ((JmdictRecord record, List<string> keys) in recordToKeysDict)
        {
            insertRecordCommand.Parameters["@id"].Value = id;
            insertRecordCommand.Parameters["@edict_id"].Value = record.Id;
            insertRecordCommand.Parameters["@primary_spelling"].Value = record.PrimarySpelling;
            insertRecordCommand.Parameters["@primary_spelling_orthography_info"].Value = record.PrimarySpellingOrthographyInfo is not null ? JsonSerializer.Serialize(record.PrimarySpellingOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@alternative_spellings"].Value = record.AlternativeSpellings is not null ? JsonSerializer.Serialize(record.AlternativeSpellings, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@alternative_spellings_orthography_info"].Value = record.AlternativeSpellingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@readings"].Value = record.Readings is not null ? JsonSerializer.Serialize(record.Readings, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@readings_orthography_info"].Value = record.ReadingsOrthographyInfo is not null ? JsonSerializer.Serialize(record.ReadingsOrthographyInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@reading_restrictions"].Value = record.ReadingRestrictions is not null ? JsonSerializer.Serialize(record.ReadingRestrictions, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@glossary"].Value = JsonSerializer.Serialize(record.Definitions, Utils.s_jsoNotIgnoringNull);
            insertRecordCommand.Parameters["@glossary_info"].Value = record.DefinitionInfo is not null ? JsonSerializer.Serialize(record.DefinitionInfo, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@part_of_speech"].Value = JsonSerializer.Serialize(record.WordClasses, Utils.s_jsoNotIgnoringNull);
            insertRecordCommand.Parameters["@spelling_restrictions"].Value = record.SpellingRestrictions is not null ? JsonSerializer.Serialize(record.SpellingRestrictions, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@fields"].Value = record.Fields is not null ? JsonSerializer.Serialize(record.Fields, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@misc"].Value = record.Misc is not null ? JsonSerializer.Serialize(record.Misc, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@dialects"].Value = record.Dialects is not null ? JsonSerializer.Serialize(record.Dialects, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@loanword_etymology"].Value = record.LoanwordEtymology is not null ? JsonSerializer.Serialize(record.LoanwordEtymology, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@cross_references"].Value = record.RelatedTerms is not null ? JsonSerializer.Serialize(record.RelatedTerms, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            insertRecordCommand.Parameters["@antonyms"].Value = record.Antonyms is not null ? JsonSerializer.Serialize(record.Antonyms, Utils.s_jsoNotIgnoringNull) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            int keyCount = keys.Count;
            for (int i = 0; i < keyCount; i++)
            {
                insertSearchKeyCommand.Parameters["@record_id"].Value = id;
                insertSearchKeyCommand.Parameters["@search_key"].Value = keys[i];
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
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, List<string> terms, string parameter)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
            $"""
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
            WHERE rsk.search_key IN {parameter}
            """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        int termCount = terms.Count;
        for (int i = 0; i < termCount; i++)
        {
            _ = command.Parameters.AddWithValue(string.Create(CultureInfo.InvariantCulture, $"@{i + 1}"), terms[i]);
        }

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

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

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
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
            GROUP BY r.id;
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
