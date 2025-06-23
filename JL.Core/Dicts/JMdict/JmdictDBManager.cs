using System.Collections.Frozen;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictDBManager
{
    public const int Version = 9;

    private const int SearchKeyIndex = 22;

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
                primary_spelling_orthography_info BLOB,
                readings BLOB,
                alternative_spellings BLOB,
                alternative_spellings_orthography_info BLOB,
                readings_orthography_info BLOB,
                reading_restrictions BLOB,
                glossary BLOB NOT NULL,
                glossary_info BLOB,
                part_of_speech_shared_by_all_senses BLOB,
                part_of_speech BLOB,
                spelling_restrictions BLOB,
                fields_shared_by_all_senses BLOB,
                fields BLOB,
                misc_shared_by_all_senses BLOB,
                misc BLOB,
                dialects_shared_by_all_senses BLOB,
                dialects BLOB,
                loanword_etymology BLOB,
                cross_references BLOB,
                antonyms BLOB
            ) STRICT;

            CREATE TABLE IF NOT EXISTS record_search_key
            (
                search_key TEXT NOT NULL,
                record_id INTEGER NOT NULL,
                PRIMARY KEY (search_key, record_id),
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
            int recordsCount = records.Count;
            for (int i = 0; i < recordsCount; i++)
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
            INSERT INTO record (id, edict_id, primary_spelling, primary_spelling_orthography_info, alternative_spellings, alternative_spellings_orthography_info, readings, readings_orthography_info, reading_restrictions, glossary, glossary_info, part_of_speech_shared_by_all_senses, part_of_speech, spelling_restrictions, fields_shared_by_all_senses, fields, misc_shared_by_all_senses, misc, dialects_shared_by_all_senses, dialects, loanword_etymology, cross_references, antonyms)
            VALUES (@id, @edict_id, @primary_spelling, @primary_spelling_orthography_info, @alternative_spellings, @alternative_spellings_orthography_info, @readings, @readings_orthography_info, @reading_restrictions, @glossary, @glossary_info, @part_of_speech_shared_by_all_senses, @part_of_speech, @spelling_restrictions, @fields_shared_by_all_senses, @fields, @misc_shared_by_all_senses, @misc, @dialects_shared_by_all_senses, @dialects, @loanword_etymology, @cross_references, @antonyms);
            """;

        _ = insertRecordCommand.Parameters.Add("@id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@edict_id", SqliteType.Integer);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling", SqliteType.Text);
        _ = insertRecordCommand.Parameters.Add("@primary_spelling_orthography_info", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@alternative_spellings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@alternative_spellings_orthography_info", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@readings", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@readings_orthography_info", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@reading_restrictions", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@glossary", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@glossary_info", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@part_of_speech_shared_by_all_senses", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@part_of_speech", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@spelling_restrictions", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@fields_shared_by_all_senses", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@fields", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@misc_shared_by_all_senses", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@misc", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@dialects_shared_by_all_senses", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@dialects", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@loanword_etymology", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@cross_references", SqliteType.Blob);
        _ = insertRecordCommand.Parameters.Add("@antonyms", SqliteType.Blob);
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
            insertRecordCommand.Parameters["@primary_spelling_orthography_info"].Value = record.PrimarySpellingOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.PrimarySpellingOrthographyInfo) : DBNull.Value;
            insertRecordCommand.Parameters["@alternative_spellings"].Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            insertRecordCommand.Parameters["@alternative_spellings_orthography_info"].Value = record.AlternativeSpellingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo) : DBNull.Value;
            insertRecordCommand.Parameters["@readings"].Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
            insertRecordCommand.Parameters["@readings_orthography_info"].Value = record.ReadingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.ReadingsOrthographyInfo) : DBNull.Value;
            insertRecordCommand.Parameters["@reading_restrictions"].Value = record.ReadingRestrictions is not null ? MessagePackSerializer.Serialize(record.ReadingRestrictions) : DBNull.Value;
            insertRecordCommand.Parameters["@glossary"].Value = MessagePackSerializer.Serialize(record.Definitions);
            insertRecordCommand.Parameters["@glossary_info"].Value = record.DefinitionInfo is not null ? MessagePackSerializer.Serialize(record.DefinitionInfo) : DBNull.Value;
            insertRecordCommand.Parameters["@part_of_speech_shared_by_all_senses"].Value = record.WordClassesSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.WordClassesSharedByAllSenses) : DBNull.Value;
            insertRecordCommand.Parameters["@part_of_speech"].Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            insertRecordCommand.Parameters["@spelling_restrictions"].Value = record.SpellingRestrictions is not null ? MessagePackSerializer.Serialize(record.SpellingRestrictions) : DBNull.Value;
            insertRecordCommand.Parameters["@fields_shared_by_all_senses"].Value = record.FieldsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.FieldsSharedByAllSenses) : DBNull.Value;
            insertRecordCommand.Parameters["@fields"].Value = record.Fields is not null ? MessagePackSerializer.Serialize(record.Fields) : DBNull.Value;
            insertRecordCommand.Parameters["@misc_shared_by_all_senses"].Value = record.MiscSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.MiscSharedByAllSenses) : DBNull.Value;
            insertRecordCommand.Parameters["@misc"].Value = record.Misc is not null ? MessagePackSerializer.Serialize(record.Misc) : DBNull.Value;
            insertRecordCommand.Parameters["@dialects_shared_by_all_senses"].Value = record.DialectsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.DialectsSharedByAllSenses) : DBNull.Value;
            insertRecordCommand.Parameters["@dialects"].Value = record.Dialects is not null ? MessagePackSerializer.Serialize(record.Dialects) : DBNull.Value;
            insertRecordCommand.Parameters["@loanword_etymology"].Value = record.LoanwordEtymology is not null ? MessagePackSerializer.Serialize(record.LoanwordEtymology) : DBNull.Value;
            insertRecordCommand.Parameters["@cross_references"].Value = record.RelatedTerms is not null ? MessagePackSerializer.Serialize(record.RelatedTerms) : DBNull.Value;
            insertRecordCommand.Parameters["@antonyms"].Value = record.Antonyms is not null ? MessagePackSerializer.Serialize(record.Antonyms) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            insertSearchKeyCommand.Parameters["@record_id"].Value = id;
            foreach (ref readonly string key in keys.AsReadOnlySpan())
            {
                insertSearchKeyCommand.Parameters["@search_key"].Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();
            }

            ++id;
        }

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string parameter)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText =
            $"""
            SELECT r.edict_id,
                   r.primary_spelling,
                   r.primary_spelling_orthography_info,
                   r.spelling_restrictions,
                   r.alternative_spellings,
                   r.alternative_spellings_orthography_info,
                   r.readings,
                   r.readings_orthography_info,
                   r.reading_restrictions,
                   r.glossary,
                   r.glossary_info,
                   r.part_of_speech_shared_by_all_senses,
                   r.part_of_speech,
                   r.fields_shared_by_all_senses,
                   r.fields,
                   r.misc_shared_by_all_senses,
                   r.misc,
                   r.dialects_shared_by_all_senses,
                   r.dialects,
                   r.loanword_etymology,
                   r.cross_references,
                   r.antonyms,
                   rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            WHERE rsk.search_key IN {parameter}
            """;
#pragma warning restore CA2100 // Review SQL queries for security vulnerabilities

        for (int i = 0; i < terms.Length; i++)
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
            string searchKey = dataReader.GetString(SearchKeyIndex);
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
            SELECT r.edict_id,
                   r.primary_spelling,
                   r.primary_spelling_orthography_info,
                   r.spelling_restrictions,
                   r.alternative_spellings,
                   r.alternative_spellings_orthography_info,
                   r.readings,
                   r.readings_orthography_info,
                   r.reading_restrictions,
                   r.glossary,
                   r.glossary_info,
                   r.part_of_speech_shared_by_all_senses,
                   r.part_of_speech,
                   r.fields_shared_by_all_senses,
                   r.fields,
                   r.misc_shared_by_all_senses,
                   r.misc,
                   r.dialects_shared_by_all_senses,
                   r.dialects,
                   r.loanword_etymology,
                   r.cross_references,
                   r.antonyms,
                   json_group_array(rsk.search_key)
            FROM record r
            JOIN record_search_key rsk ON r.id = rsk.record_id
            GROUP BY r.id;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            JmdictRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString(SearchKeyIndex), Utils.s_jso).Span;
            foreach (ref readonly string searchKey in searchKeys)
            {
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
        int id = dataReader.GetInt32(0);
        string primarySpelling = dataReader.GetString(1);

        string[]? primarySpellingOrthographyInfo = !dataReader.IsDBNull(2)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(2))
            : null;

        string[]?[]? spellingRestrictions = !dataReader.IsDBNull(3)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(3))
            : null;

        string[]? alternativeSpellings = !dataReader.IsDBNull(4)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(4))
            : null;


        string[]?[]? alternativeSpellingsOrthographyInfo = !dataReader.IsDBNull(5)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(5))
            : null;

        string[]? readings = !dataReader.IsDBNull(6)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(6))
            : null;

        string[]?[]? readingsOrthographyInfo = !dataReader.IsDBNull(7)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(7))
            : null;

        string[]?[]? readingRestrictions = !dataReader.IsDBNull(8)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(8))
            : null;

        string[][]? definitions = MessagePackSerializer.Deserialize<string[][]>(dataReader.GetFieldValue<byte[]>(9));
        Debug.Assert(definitions is not null);

        string?[]? definitionInfo = !dataReader.IsDBNull(10)
            ? MessagePackSerializer.Deserialize<string?[]>(dataReader.GetFieldValue<byte[]>(10))
            : null;

        string[]? wordClassesSharedByAllSenses = !dataReader.IsDBNull(11)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(11))
            : null;

        string[]?[]? wordClasses = !dataReader.IsDBNull(12)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(12))
            : null;

        string[]? fieldsSharedByAllSenses = !dataReader.IsDBNull(13)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(13))
            : null;

        string[]?[]? fields = !dataReader.IsDBNull(14)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(14))
            : null;

        string[]? miscSharedByAllSenses = !dataReader.IsDBNull(15)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(15))
            : null;

        string[]?[]? misc = !dataReader.IsDBNull(16)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(16))
            : null;

        string[]? dialectsSharedByAllSenses = !dataReader.IsDBNull(17)
            ? MessagePackSerializer.Deserialize<string[]>(dataReader.GetFieldValue<byte[]>(17))
            : null;

        string[]?[]? dialects = !dataReader.IsDBNull(18)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(18))
            : null;

        LoanwordSource[]?[]? loanwordEtymology = !dataReader.IsDBNull(19)
            ? MessagePackSerializer.Deserialize<LoanwordSource[]?[]>(dataReader.GetFieldValue<byte[]>(19))
            : null;

        string[]?[]? relatedTerms = !dataReader.IsDBNull(20)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(20))
            : null;

        string[]?[]? antonyms = !dataReader.IsDBNull(21)
            ? MessagePackSerializer.Deserialize<string[]?[]>(dataReader.GetFieldValue<byte[]>(21))
            : null;

        return new JmdictRecord(id, primarySpelling, definitions, wordClasses, wordClassesSharedByAllSenses, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, spellingRestrictions, readingRestrictions, fields, fieldsSharedByAllSenses, misc, miscSharedByAllSenses, definitionInfo, dialects, dialectsSharedByAllSenses, loanwordEtymology, relatedTerms, antonyms);
    }
}
