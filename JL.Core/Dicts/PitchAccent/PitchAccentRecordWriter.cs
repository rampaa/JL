using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.PitchAccent.YomichanPitchAccentDBManager;

namespace JL.Core.Dicts.PitchAccent;

internal sealed class PitchAccentRecordWriter : IDisposable
{
#pragma warning disable CA2213 // sqlite3_finalize releases the prepared statements.
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _insertRecordStatement;
    private readonly sqlite3_stmt _insertSearchKeyStatement;
#pragma warning restore CA2213
    private long _boundSearchKeyRecordId = -1;

    public PitchAccentRecordWriter(SqliteConnection connection)
    {
        const string insertRecordQuery =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Reading}, {Position})
            VALUES (?1, ?2, ?3, ?4);
            """;

        const string insertSearchKeyQuery =
            $"""
            INSERT INTO {RecordSearchKey} ({RecordId}, {SearchKey})
            VALUES (?1, ?2);
            """;

        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        _insertRecordStatement = Prepare(insertRecordQuery);
        try
        {
            _insertSearchKeyStatement = Prepare(insertSearchKeyQuery);
        }
        catch
        {
            _ = raw.sqlite3_finalize(_insertRecordStatement);
            throw;
        }
    }

    private sqlite3_stmt Prepare(string query)
    {
        int result = raw.sqlite3_prepare_v3(_connectionHandle, query, raw.SQLITE_PREPARE_PERSISTENT, out sqlite3_stmt? statement);
        if (result is not raw.SQLITE_OK)
        {
            if (statement is not null)
            {
                _ = raw.sqlite3_finalize(statement);
            }

            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        Debug.Assert(statement is not null);
        return statement;
    }

    public void InsertRecord(long rowId, string spelling, string? reading, byte position)
    {
        int result = raw.sqlite3_bind_int64(_insertRecordStatement, 1, rowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_text16(_insertRecordStatement, 2, spelling.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = reading is not null
            ? raw.sqlite3_bind_text16(_insertRecordStatement, 3, reading.AsSpan())
            : raw.sqlite3_bind_null(_insertRecordStatement, 3);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_int(_insertRecordStatement, 4, position);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_step(_insertRecordStatement);
        if (result is not raw.SQLITE_DONE)
        {
            _ = raw.sqlite3_reset(_insertRecordStatement);
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_reset(_insertRecordStatement);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }
    }

    public void InsertSearchKey(long recordId, string searchKey)
    {
        Debug.Assert(recordId > 0);
        if (_boundSearchKeyRecordId != recordId)
        {
            int bindResult = raw.sqlite3_bind_int64(_insertSearchKeyStatement, 1, recordId);
            if (bindResult is not raw.SQLITE_OK)
            {
                SqliteException.ThrowExceptionForRC(bindResult, _connectionHandle);
            }

            _boundSearchKeyRecordId = recordId;
        }

        int result = raw.sqlite3_bind_text16(_insertSearchKeyStatement, 2, searchKey.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_step(_insertSearchKeyStatement);
        if (result is not raw.SQLITE_DONE)
        {
            _ = raw.sqlite3_reset(_insertSearchKeyStatement);
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_reset(_insertSearchKeyStatement);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_insertSearchKeyStatement);
        _ = raw.sqlite3_finalize(_insertRecordStatement);
    }
}
