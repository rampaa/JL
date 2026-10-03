using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Freqs.FreqDBManager;

namespace JL.Core.Freqs;

internal sealed class FrequencyRecordWriter : IDisposable
{
#pragma warning disable CA2213 // sqlite3_finalize releases the prepared statements.
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _insertRecordStatement;
    private readonly sqlite3_stmt _insertSearchKeyStatement;
#pragma warning restore CA2213
    private long _boundSearchKeyRecordId = -1;

    public FrequencyRecordWriter(SqliteConnection connection)
    {
        const string insertRecordQuery =
            $"""
            INSERT INTO {Record} ({RowId}, {Spelling}, {Frequency})
            VALUES (?1, ?2, ?3);
            """;

        const string insertSearchKeyQuery =
            $"""
            INSERT INTO {RecordSearchKey} ({SearchKey}, {RecordId})
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

    public void InsertRecord(long rowId, string spelling, int frequency)
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

        result = raw.sqlite3_bind_int(_insertRecordStatement, 3, frequency);
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

        int result = raw.sqlite3_bind_text16(_insertSearchKeyStatement, 1, searchKey.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        if (_boundSearchKeyRecordId != recordId)
        {
            result = raw.sqlite3_bind_int64(_insertSearchKeyStatement, 2, recordId);
            if (result is not raw.SQLITE_OK)
            {
                SqliteException.ThrowExceptionForRC(result, _connectionHandle);
            }

            _boundSearchKeyRecordId = recordId;
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
