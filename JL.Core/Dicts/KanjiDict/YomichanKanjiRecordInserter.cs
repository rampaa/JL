using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.KanjiDict.YomichanKanjiDBManager;

namespace JL.Core.Dicts.KanjiDict;

internal sealed class YomichanKanjiRecordInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public YomichanKanjiRecordInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(
            _connectionHandle,
            $"""
            INSERT INTO {Record} ({RowId}, {Kanji}, {OnReadings}, {KunReadings}, {Glossary}, {Stats})
            VALUES (?1, ?2, ?3, ?4, ?5, ?6);
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

    public void Insert(long rowId, string kanji, byte[]? onReadings, byte[]? kunReadings, byte[]? definitions, byte[]? stats)
    {
        int result = raw.sqlite3_bind_int64(_statement, 1, rowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_text16(_statement, 2, kanji.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = onReadings is not null
            ? raw.sqlite3_bind_blob(_statement, 3, onReadings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 3);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = kunReadings is not null
            ? raw.sqlite3_bind_blob(_statement, 4, kunReadings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 4);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = definitions is not null
            ? raw.sqlite3_bind_blob(_statement, 5, definitions.AsSpan())
            : raw.sqlite3_bind_null(_statement, 5);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = stats is not null
            ? raw.sqlite3_bind_blob(_statement, 6, stats.AsSpan())
            : raw.sqlite3_bind_null(_statement, 6);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_step(_statement);
        if (result is not raw.SQLITE_DONE)
        {
            _ = raw.sqlite3_reset(_statement);
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
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
