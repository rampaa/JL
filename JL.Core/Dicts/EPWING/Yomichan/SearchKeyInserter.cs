using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.EPWING.Yomichan.EpwingYomichanDBManager;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class SearchKeyInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public SearchKeyInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(
            _connectionHandle,
            $"INSERT INTO {RecordSearchKey}({SearchKey}, {RecordId}) VALUES (?1, ?2);",
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

    public void Insert(long recordId, string searchKey, string? additionalSearchKey)
    {
        int result = raw.sqlite3_bind_int64(_statement, 2, recordId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_text16(_statement, 1, searchKey.AsSpan());
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

        if (additionalSearchKey is not null)
        {
            result = raw.sqlite3_bind_text16(_statement, 1, additionalSearchKey.AsSpan());
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
    }

    public void Insert(long recordId, ReadOnlySpan<string> searchKeys)
    {
        int result = raw.sqlite3_bind_int64(_statement, 2, recordId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        foreach (ReadOnlySpan<char> searchKey in searchKeys)
        {
            result = raw.sqlite3_bind_text16(_statement, 1, searchKey);
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
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}

