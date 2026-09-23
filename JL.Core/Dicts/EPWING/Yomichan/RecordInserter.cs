using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.EPWING.Yomichan.EpwingYomichanDBManager;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class RecordInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public RecordInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(
            _connectionHandle,
            $"""
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {PopularityScore}, {Glossary}, {PartOfSpeech}, {GlossaryTags}, {ImageInfos})
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8);
            """,
            raw.SQLITE_PREPARE_PERSISTENT,
            out sqlite3_stmt? statement);

        if (result is not raw.SQLITE_OK)
        {
            if (statement is not null)
            {
                _ = raw.sqlite3_finalize(statement);
            }

            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        Debug.Assert(statement is not null);
        _statement = statement;
    }

    public void Insert(long rowId, in EpwingYomichanImportRecord record)
    {
        Insert(rowId, record.PrimarySpelling, record.Reading, record.PopularityScore,
            record.Definitions, record.WordClasses, record.DefinitionTags, record.ImageInfos);
    }

    public void Insert(long rowId, string primarySpelling, string? reading, double popularityScore,
        ReadOnlySpan<byte> definitions, byte[]? wordClasses, byte[]? definitionTags, byte[]? imageInfos)
    {
        int result = raw.sqlite3_bind_int64(_statement, 1, rowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_text16(_statement, 2, primarySpelling.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = reading is not null
            ? raw.sqlite3_bind_text16(_statement, 3, reading.AsSpan())
            : raw.sqlite3_bind_null(_statement, 3);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_double(_statement, 4, popularityScore);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_blob(_statement, 5, definitions);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = wordClasses is not null
            ? raw.sqlite3_bind_blob(_statement, 6, wordClasses.AsSpan())
            : raw.sqlite3_bind_null(_statement, 6);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = definitionTags is not null
            ? raw.sqlite3_bind_blob(_statement, 7, definitionTags.AsSpan())
            : raw.sqlite3_bind_null(_statement, 7);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = imageInfos is not null
            ? raw.sqlite3_bind_blob(_statement, 8, imageInfos.AsSpan())
            : raw.sqlite3_bind_null(_statement, 8);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_step(_statement);
        if (result is not raw.SQLITE_DONE)
        {
            int stepResult = result;
            _ = raw.sqlite3_reset(_statement);
            SqliteException.ThrowExceptionForRC(stepResult, _connectionHandle);
        }

        result = raw.sqlite3_reset(_statement);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
