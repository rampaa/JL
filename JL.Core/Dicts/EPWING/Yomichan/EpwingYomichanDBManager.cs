using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Dicts.Interfaces;
using JL.Core.Utilities;
using MessagePack;
using Microsoft.Data.Sqlite;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanDBManager
{
    public const int Version = 21;

    private const string SingleTermQuery =
        """
        SELECT r.rowid, r.primary_spelling, r.reading, r.glossary, r.part_of_speech, r.glossary_tags, r.image_paths
        FROM record r
        JOIN record_search_key rsk ON r.rowid = rsk.record_id
        WHERE rsk.search_key = @term;
        """;

    public static string GetQuery(string parameter)
    {
        return
            $"""
            SELECT r.rowid, r.primary_spelling, r.reading, r.glossary, r.part_of_speech, r.glossary_tags, r.image_paths, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            WHERE rsk.search_key IN {parameter}
            """;
    }

    public static string GetQuery(int termCount)
    {
        StringBuilder queryBuilder = Utils.StringBuilderPool.Get().Append(
            """
            SELECT r.rowid, r.primary_spelling, r.reading, r.glossary, r.part_of_speech, r.glossary_tags, r.image_paths, rsk.search_key
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            WHERE rsk.search_key IN (@1
            """);

        for (int i = 1; i < termCount; i++)
        {
            _ = queryBuilder.Append(CultureInfo.InvariantCulture, $", @{i + 1}");
        }

        string query = queryBuilder.Append(");").ToString();
        Utils.StringBuilderPool.Return(queryBuilder);
        return query;
    }

    private enum ColumnIndex
    {
        // ReSharper disable once UnusedMember.Local
        RowId = 0,
        PrimarySpelling,
        Reading,
        Glossary,
        PartOfSpeech,
        GlossaryTags,
        ImagePaths,
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
                primary_spelling TEXT NOT NULL,
                reading TEXT,
                glossary BLOB NOT NULL,
                part_of_speech BLOB,
                glossary_tags BLOB,
                image_paths BLOB
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
        int totalRecordCount = 0;
        ICollection<IList<IDictRecord>> dictRecordValues = dict.Contents.Values;
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            totalRecordCount += dictRecords.Count;
        }

        HashSet<EpwingYomichanRecord> yomichanWordRecords = new(totalRecordCount);
        foreach (IList<IDictRecord> dictRecords in dictRecordValues)
        {
            int dictRecordsCount = dictRecords.Count;
            for (int i = 0; i < dictRecordsCount; i++)
            {
                _ = yomichanWordRecords.Add((EpwingYomichanRecord)dictRecords[i]);
            }
        }

        ulong rowid = 1;

        using SqliteConnection connection = DBUtils.CreateReadWriteDBConnection(DBUtils.GetDictDBPath(dict.Name));
        DBUtils.SetSynchronousModeToNormal(connection);
        using SqliteTransaction transaction = connection.BeginTransaction();

        using SqliteCommand insertRecordCommand = connection.CreateCommand();
        insertRecordCommand.CommandText =
            """
            INSERT INTO record (rowid, primary_spelling, reading, glossary, part_of_speech, glossary_tags, image_paths)
            VALUES (@rowid, @primary_spelling, @reading, @glossary, @part_of_speech, @glossary_tags, @image_paths);
            """;

        SqliteParameter rowidParam = new("@rowid", SqliteType.Integer);
        SqliteParameter primarySpellingParam = new("@primary_spelling", SqliteType.Text);
        SqliteParameter readingParam = new("@reading", SqliteType.Text);
        SqliteParameter glossaryParam = new("@glossary", SqliteType.Blob);
        SqliteParameter partOfSpeechParam = new("@part_of_speech", SqliteType.Blob);
        SqliteParameter glossaryTagsParam = new("@glossary_tags", SqliteType.Blob);
        SqliteParameter imagePathsParam = new("@image_paths", SqliteType.Blob);
        insertRecordCommand.Parameters.AddRange([
            rowidParam,
            primarySpellingParam,
            readingParam,
            glossaryParam,
            partOfSpeechParam,
            glossaryTagsParam,
            imagePathsParam
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

        foreach (EpwingYomichanRecord record in yomichanWordRecords)
        {
            rowidParam.Value = rowid;
            primarySpellingParam.Value = record.PrimarySpelling;
            readingParam.Value = record.Reading is not null ? record.Reading : DBNull.Value;
            glossaryParam.Value = MessagePackSerializer.Serialize(record.Definitions);
            partOfSpeechParam.Value = record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : DBNull.Value;
            glossaryTagsParam.Value = record.DefinitionTags is not null ? MessagePackSerializer.Serialize(record.DefinitionTags) : DBNull.Value;
            imagePathsParam.Value = record.ImagePaths is not null ? MessagePackSerializer.Serialize(record.ImagePaths) : DBNull.Value;
            _ = insertRecordCommand.ExecuteNonQuery();

            recordIdParam.Value = rowid;
            string primarySpellingInHiragana = JapaneseUtils.KatakanaToHiragana(record.PrimarySpelling);
            searchKeyParam.Value = primarySpellingInHiragana;
            _ = insertSearchKeyCommand.ExecuteNonQuery();

            if (record.Reading is not null)
            {
                string readingInHiragana = JapaneseUtils.KatakanaToHiragana(record.Reading);
                if (readingInHiragana != primarySpellingInHiragana)
                {
                    searchKeyParam.Value = readingInHiragana;
                    _ = insertSearchKeyCommand.ExecuteNonQuery();
                }
            }

            ++rowid;
        }

        transaction.Commit();

        using SqliteCommand analyzeCommand = connection.CreateCommand();
        analyzeCommand.CommandText = "ANALYZE;";
        _ = analyzeCommand.ExecuteNonQuery();

        using SqliteCommand vacuumCommand = connection.CreateCommand();
        vacuumCommand.CommandText = "VACUUM;";
        _ = vacuumCommand.ExecuteNonQuery();
    }

    public static Dictionary<string, IList<IDictRecord>>? GetRecordsFromDB(string dbName, ReadOnlySpan<string> terms, string query)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

#pragma warning disable CA2100 // Review SQL queries for security vulnerabilities
        command.CommandText = query;
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
            EpwingYomichanRecord record = GetRecord(dataReader);
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

    public static List<IDictRecord>? GetRecordsFromDB(string dbName, string term)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dbName));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText = SingleTermQuery;

        _ = command.Parameters.AddWithValue("@term", term);

        using SqliteDataReader dataReader = command.ExecuteReader();
        if (!dataReader.HasRows)
        {
            return null;
        }

        List<IDictRecord> results = [];
        while (dataReader.Read())
        {
            results.Add(GetRecord(dataReader));
        }
        return results;
    }

    public static void LoadFromDB(Dict dict)
    {
        using SqliteConnection connection = DBUtils.CreateReadOnlyDBConnection(DBUtils.GetDictDBPath(dict.Name));
        DBUtils.EnableMemoryMapping(connection);
        using SqliteCommand command = connection.CreateCommand();

        command.CommandText =
            """
            SELECT r.rowid, r.primary_spelling, r.reading, r.glossary, r.part_of_speech, r.glossary_tags, r.image_paths, json_group_array(rsk.search_key)
            FROM record r
            JOIN record_search_key rsk ON r.rowid = rsk.record_id
            GROUP BY r.rowid;
            """;

        using SqliteDataReader dataReader = command.ExecuteReader();
        while (dataReader.Read())
        {
            EpwingYomichanRecord record = GetRecord(dataReader);
            ReadOnlySpan<string> searchKeys = JsonSerializer.Deserialize<ReadOnlyMemory<string>>(dataReader.GetString((int)ColumnIndex.SearchKey), Utils.Jso).Span;
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

        dict.Contents = dict.Contents.ToFrozenDictionary(static entry => entry.Key, static IList<IDictRecord> (entry) => entry.Value.ToArray(), StringComparer.Ordinal);
    }

    private static EpwingYomichanRecord GetRecord(SqliteDataReader dataReader)
    {
        string primarySpelling = dataReader.GetString((int)ColumnIndex.PrimarySpelling);

        const int readingIndex = (int)ColumnIndex.Reading;
        string? reading = !dataReader.IsDBNull(readingIndex)
            ? dataReader.GetString(readingIndex)
            : null;

        string[] definitions = dataReader.GetValueFromBlobStream<string[]>((int)ColumnIndex.Glossary);
        string[]? wordClasses = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.PartOfSpeech);
        string[]? definitionTags = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.GlossaryTags);
        string[]? imagePaths = dataReader.GetNullableValueFromBlobStream<string[]>((int)ColumnIndex.ImagePaths);

        return new EpwingYomichanRecord(primarySpelling, reading, definitions, wordClasses, definitionTags, imagePaths);
    }
}
