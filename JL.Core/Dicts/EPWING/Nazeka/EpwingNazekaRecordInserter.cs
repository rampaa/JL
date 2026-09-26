using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.EPWING.Nazeka.EpwingNazekaDBManager;

namespace JL.Core.Dicts.EPWING.Nazeka;

internal sealed class EpwingNazekaRecordInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public EpwingNazekaRecordInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(
            _connectionHandle,
            $"""
            INSERT INTO {Record} ({RowId}, {PrimarySpelling}, {Reading}, {AlternativeSpellings}, {Glossary}, {EpwingNazekaDBManager.ImageInfo})
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

    public void Insert(long rowId, string primarySpelling, string? reading, byte[]? alternativeSpellings,
        ReadOnlySpan<byte> definitions, byte[]? imageInfo)
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

        result = alternativeSpellings is not null
            ? raw.sqlite3_bind_blob(_statement, 4, alternativeSpellings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 4);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_blob(_statement, 5, definitions);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = imageInfo is not null
            ? raw.sqlite3_bind_blob(_statement, 6, imageInfo.AsSpan())
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
