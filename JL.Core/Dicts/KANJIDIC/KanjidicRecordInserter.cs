using System.Diagnostics;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.KANJIDIC.KanjidicDBManager;

namespace JL.Core.Dicts.KANJIDIC;

internal sealed class KanjidicRecordInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public KanjidicRecordInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(_connectionHandle,
            $"""
            INSERT INTO {Record} ({Kanji}, {OnReadings}, {KunReadings}, {NanoriReadings}, {RadicalNames}, {Glossary}, {StrokeCount}, {Grade}, {Frequency})
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9);
            """,
            raw.SQLITE_PREPARE_PERSISTENT, out sqlite3_stmt? statement);
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

    public void Insert(string kanji, byte[]? onReadings, byte[]? kunReadings, byte[]? nanoriReadings,
        byte[]? radicalNames, byte[]? definitions, byte strokeCount, byte grade, int frequency)
    {
        int result = raw.sqlite3_bind_text16(_statement, 1, kanji.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = onReadings is not null
            ? raw.sqlite3_bind_blob(_statement, 2, onReadings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 2);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = kunReadings is not null
            ? raw.sqlite3_bind_blob(_statement, 3, kunReadings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 3);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = nanoriReadings is not null
            ? raw.sqlite3_bind_blob(_statement, 4, nanoriReadings.AsSpan())
            : raw.sqlite3_bind_null(_statement, 4);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = radicalNames is not null
            ? raw.sqlite3_bind_blob(_statement, 5, radicalNames.AsSpan())
            : raw.sqlite3_bind_null(_statement, 5);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = definitions is not null
            ? raw.sqlite3_bind_blob(_statement, 6, definitions.AsSpan())
            : raw.sqlite3_bind_null(_statement, 6);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_int(_statement, 7, strokeCount);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_int(_statement, 8, grade);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_int(_statement, 9, frequency);
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
