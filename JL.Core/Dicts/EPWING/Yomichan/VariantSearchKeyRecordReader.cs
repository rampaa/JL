using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.EPWING.Yomichan.EpwingYomichanDBManager;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal sealed class VariantSearchKeyRecordReader : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public VariantSearchKeyRecordReader(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(
            _connectionHandle,
            $"""
            SELECT {RowId}, {PrimarySpelling}, {Reading}
            FROM {Record}
            WHERE {RowId} > ?1
            ORDER BY {RowId}
            LIMIT {VariantSearchKeyRecordBatchSize};
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

    public int Read(VariantSearchKeyRecord[] variantSearchKeyRecords, long lastVariantSearchKeyRecordRowId,
        out long newLastVariantSearchKeyRecordRowId)
    {
        int result = raw.sqlite3_bind_int64(_statement, 1, lastVariantSearchKeyRecordRowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        int count = 0;
        newLastVariantSearchKeyRecordRowId = lastVariantSearchKeyRecordRowId;

        while ((result = raw.sqlite3_step(_statement)) is raw.SQLITE_ROW)
        {
            long sourceRowId = raw.sqlite3_column_int64(_statement, 0);
            string primarySpelling = raw.sqlite3_column_text(_statement, 1).utf8_to_string();
            string? reading = raw.sqlite3_column_type(_statement, 2) is raw.SQLITE_NULL
                ? null
                : raw.sqlite3_column_text(_statement, 2).utf8_to_string();

            variantSearchKeyRecords[count] = new VariantSearchKeyRecord(sourceRowId, primarySpelling, reading);
            ++count;
            newLastVariantSearchKeyRecordRowId = sourceRowId;
        }

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

        return count;
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
