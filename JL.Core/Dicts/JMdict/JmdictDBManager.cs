using System.Collections.Frozen;
using System.Globalization;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.JMdict;

internal static class JmdictDBManager
{
    public const int Version = 10;

    private enum ColumnIndex
    {
        RowId = 0,
        EdictId,
        PrimarySpelling,
        PrimarySpellingOrthographyInfo,
        SpellingRestrictions,
        AlternativeSpellings,
        AlternativeSpellingsOrthographyInfo,
        Readings,
        ReadingsOrthographyInfo,
        ReadingRestrictions,
        Glossary,
        GlossaryInfo,
        WordClassesSharedByAllSenses,
        WordClasses,
        FieldsSharedByAllSenses,
        Fields,
        MiscSharedByAllSenses,
        Misc,
        DialectsSharedByAllSenses,
        Dialects,
        LoanwordEtymology,
        CrossReferences,
        Antonyms,
        SearchKey
    }

    public static void CreateDB(string dbName)
    {
        using SqliteConnection connection = DBUtils.CreateDBConnection(DBUtils.GetDictDBPath(dbName));
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS record
            (
                rowid INTEGER NOT NULL PRIMARY KEY,
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
                FOREIGN KEY (record_id) REFERENCES record (rowid) ON DELETE CASCADE
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

        ulong rowId = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (rowid, edict_id, primary_spelling, primary_spelling_orthography_info, alternative_spellings, alternative_spellings_orthography_info, readings, readings_orthography_info, reading_restrictions, glossary, glossary_info, part_of_speech_shared_by_all_senses, part_of_speech, spelling_restrictions, fields_shared_by_all_senses, fields, misc_shared_by_all_senses, misc, dialects_shared_by_all_senses, dialects, loanword_etymology, cross_references, antonyms)
            VALUES (@rowid, @edict_id, @primary_spelling, @primary_spelling_orthography_info, @alternative_spellings, @alternative_spellings_orthography_info, @readings, @readings_orthography_info, @reading_restrictions, @glossary, @glossary_info, @part_of_speech_shared_by_all_senses, @part_of_speech, @spelling_restrictions, @fields_shared_by_all_senses, @fields, @misc_shared_by_all_senses, @misc, @dialects_shared_by_all_senses, @dialects, @loanword_etymology, @cross_references, @antonyms);
            """;

        SqliteParameter rowidParam = new("@rowid", SqliteType.Integer);
        SqliteParameter edictIdParam = new("@edict_id", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new("@primary_spelling", SqliteType.Text);
        SqliteParameter primarySpellingOrthographyInfoParam = new("@primary_spelling_orthography_info", SqliteType.Blob);
        SqliteParameter alternativeSpellingsParam = new("@alternative_spellings", SqliteType.Blob);
        SqliteParameter alternativeSpellingsOrthographyInfoParam = new("@alternative_spellings_orthography_info", SqliteType.Blob);
        SqliteParameter readingsParam = new("@readings", SqliteType.Blob);
        SqliteParameter readingsOrthographyInfoParam = new("@readings_orthography_info", SqliteType.Blob);
        SqliteParameter readingRestrictionsParam = new("@reading_restrictions", SqliteType.Blob);
        SqliteParameter glossaryParam = new("@glossary", SqliteType.Blob);
        SqliteParameter glossaryInfoParam = new("@glossary_info", SqliteType.Blob);
        SqliteParameter partOfSpeechSharedByAllSensesParam = new("@part_of_speech_shared_by_all_senses", SqliteType.Blob);
        SqliteParameter partOfSpeechParam = new("@part_of_speech", SqliteType.Blob);
        SqliteParameter spellingRestrictionsParam = new("@spelling_restrictions", SqliteType.Blob);
        SqliteParameter fieldsSharedByAllSensesParam = new("@fields_shared_by_all_senses", SqliteType.Blob);
        SqliteParameter fieldsParam = new("@fields", SqliteType.Blob);
        SqliteParameter miscSharedByAllSensesParam = new("@misc_shared_by_all_senses", SqliteType.Blob);
        SqliteParameter miscParam = new("@misc", SqliteType.Blob);
        SqliteParameter dialectsSharedByAllSensesParam = new("@dialects_shared_by_all_senses", SqliteType.Blob);
        SqliteParameter dialectsParam = new("@dialects", SqliteType.Blob);
        SqliteParameter loanwordEtymologyParam = new("@loanword_etymology", SqliteType.Blob);
        SqliteParameter crossReferencesParam = new("@cross_references", SqliteType.Blob);
        SqliteParameter antonymsParam = new("@antonyms", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            edictIdParam,
            primarySpellingParam,
            primarySpellingOrthographyInfoParam,
            alternativeSpellingsParam,
            alternativeSpellingsOrthographyInfoParam,
            readingsParam,
            readingsOrthographyInfoParam,
            readingRestrictionsParam,
            glossaryParam,
            glossaryInfoParam,
            partOfSpeechSharedByAllSensesParam,
            partOfSpeechParam,
            spellingRestrictionsParam,
            fieldsSharedByAllSensesParam,
            fieldsParam,
            miscSharedByAllSensesParam,
            miscParam,
            dialectsSharedByAllSensesParam,
            dialectsParam,
            loanwordEtymologyParam,
            crossReferencesParam,
            antonymsParam
        ]);

        insertRecordCommand.Prepare();

        using SqliteCommand insertSearchKeyCommand = connection.CreateCommand();
        insertSearchKeyCommand.CommandText =
            """
            INSERT INTO record_search_key(record_id, search_key)
            VALUES (@record_id, @search_key);
            """;

        SqliteParameter recordIdParam = new("@record_id", SqliteType.Integer);
        SqliteParameter searchKeyParam = new("@search_key", SqliteType.Text);
        insertSearchKeyCommand.Parameters.AddRange([recordIdParam, searchKeyParam]);
        insertSearchKeyCommand.Prepare();

        foreach ((JmdictRecord record, List<string> keys) in recordToKeysDict)
        {
            rowidParam.Value = rowId;
            edictIdParam.Value = record.Id;
            primarySpellingParam.Value = record.PrimarySpelling;
            primarySpellingOrthographyInfoParam.Value = record.PrimarySpellingOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.PrimarySpellingOrthographyInfo) : DBNull.Value;
            alternativeSpellingsParam.Value = record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : DBNull.Value;
            alternativeSpellingsOrthographyInfoParam.Value = record.AlternativeSpellingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo) : DBNull.Value;
            readingsParam.Value = record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : DBNull.Value;
            readingsOrthographyInfoParam.Value = record.ReadingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.ReadingsOrthographyInfo) : DBNull.Value;
            readingRestrictionsParam.Value = record.ReadingRestrictions is not null ? MessagePackSerializer.Serialize(record.ReadingRestrictions) : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            glossaryInfoParam.Value = record.DefinitionInfo is not null ? MessagePackSerializer.Serialize(record.DefinitionInfo) : DBNull.Value;
            partOfSpeechSharedByAllSensesParam.Value = record.WordClassesSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.WordClassesSharedByAllSenses) : DBNull.Value;
            partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            spellingRestrictionsParam.Value = record.SpellingRestrictions is not null ? MessagePackSerializer.Serialize(record.SpellingRestrictions) : DBNull.Value;
            fieldsSharedByAllSensesParam.Value = record.FieldsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.FieldsSharedByAllSenses) : DBNull.Value;
            fieldsParam.Value = record.Fields is not null ? MessagePackSerializer.Serialize(record.Fields) : DBNull.Value;
            miscSharedByAllSensesParam.Value = record.MiscSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.MiscSharedByAllSenses) : DBNull.Value;
            miscParam.Value = record.Misc is not null ? MessagePackSerializer.Serialize(record.Misc) : DBNull.Value;
            dialectsSharedByAllSensesParam.Value = record.DialectsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.DialectsSharedByAllSenses) : DBNull.Value;
            dialectsParam.Value = record.Dialects is not null ? MessagePackSerializer.Serialize(record.Dialects) : DBNull.Value;
            loanwordEtymologyParam.Value = record.LoanwordEtymology is not null ? MessagePackSerializer.Serialize(record.LoanwordEtymology) : DBNull.Value;
            crossReferencesParam.Value = record.RelatedTerms is not null ? MessagePackSerializer.Serialize(record.RelatedTerms) : DBNull.Value;
            antonymsParam.Value = record.Antonyms is not null ? MessagePackSerializer.Serialize(record.Antonyms) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowId;
            foreach (ref readonly string key in keys.AsReadOnlySpan())
            {
                searchKeyParam.Value = key;
                _ = insertSearchKeyCommand.ExecuteNonQuery();
            }

            ++rowId;
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
            SELECT r.rowid,
                   r.edict_id,
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
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
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
            string searchKey = dataReader.GetString((int)ColumnIndex.SearchKey);
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
            SELECT r.rowid,
                   r.edict_id,
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
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            GROUP BY r.rowid;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            JmdictRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString((int)ColumnIndex.SearchKey), Utils.s_jso).Span;
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
        int edictId = dataReader.GetInt32((int)ColumnIndex.EdictId);
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);
        string[]? primarySpellingOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.PrimarySpellingOrthographyInfo);
        string[]?[]? spellingRestrictions = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.SpellingRestrictions);
        string[]? alternativeSpellings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.AlternativeSpellings);
        string[]?[]? alternativeSpellingsOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.AlternativeSpellingsOrthographyInfo);
        string[]? readings = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.Readings);
        string[]?[]? readingsOrthographyInfo = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.ReadingsOrthographyInfo);
        string[]?[]? readingRestrictions = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.ReadingRestrictions);
        string[][] definitions = dataReader.GetValueFromBlobStream<string[][]>((int)ColumnIndex.Glossary);
        string?[]? definitionInfo = dataReader.GetNullableValueFromBlobStream<string?[]>((int)ColumnIndex.GlossaryInfo);
        string[]? wordClassesSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.WordClassesSharedByAllSenses);
        string[]?[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.WordClasses);
        string[]? fieldsSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.FieldsSharedByAllSenses);
        string[]?[]? fields = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Fields);
        string[]? miscSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.MiscSharedByAllSenses);
        string[]?[]? misc = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Misc);
        string[]? dialectsSharedByAllSenses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.DialectsSharedByAllSenses);
        string[]?[]? dialects = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Dialects);
        LoanwordSource[]?[]? loanwordEtymology = dataReader.GetNullableValueFromBlobStream<LoanwordSource[]?[]>((int)ColumnIndex.LoanwordEtymology);
        string[]?[]? relatedTerms = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.CrossReferences);
        string[]?[]? antonyms = dataReader.GetNullableValueFromBlobStream<string[]?[]>((int)ColumnIndex.Antonyms);

        return new JmdictRecord(edictId, primarySpelling, definitions, wordClasses, wordClassesSharedByAllSenses, primarySpellingOrthographyInfo, alternativeSpellings, alternativeSpellingsOrthographyInfo, readings, readingsOrthographyInfo, spellingRestrictions, readingRestrictions, fields, fieldsSharedByAllSenses, misc, miscSharedByAllSenses, definitionInfo, dialects, dialectsSharedByAllSenses, loanwordEtymology, relatedTerms, antonyms);
    }
}
