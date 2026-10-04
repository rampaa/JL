using System.Diagnostics;
using JL.Core.Utilities.Database;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.PitchAccent.YomichanPitchAccentDBManager;

namespace JL.Core.Dicts.PitchAccent;

internal sealed class PitchAccentVariantSourceReader : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;
    private bool _completed;

    public PitchAccentVariantSourceReader(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;
        const string query = $"SELECT {RowId}, {Spelling}, {Reading} FROM {Record};";
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
        _statement = statement;
    }

    public int Read(PitchAccentVariantSource[] sources, int maxSourceCount)
    {
        if (_completed)
        {
            return 0;
        }

        int sourceCount = 0;
        while (sourceCount < maxSourceCount)
        {
            int result = raw.sqlite3_step(_statement);
            if (result is raw.SQLITE_DONE)
            {
                _completed = true;
                break;
            }

            if (result is not raw.SQLITE_ROW)
            {
                SqliteException.ThrowExceptionForRC(result, _connectionHandle);
            }

            sources[sourceCount] = new PitchAccentVariantSource(raw.sqlite3_column_int64(_statement, 0), GetString(1), raw.sqlite3_column_type(_statement, 2) is raw.SQLITE_NULL ? null : GetString(2));
            ++sourceCount;
        }

        return sourceCount;
    }

    private unsafe string GetString(int index)
    {
        nint statement = _statement.DangerousGetHandle();
        nint text = SqliteNativeMethods.GetColumnText16(statement, index);
        int byteCount = SqliteNativeMethods.GetColumnBytes16(statement, index);
        string value = new((char*)text, 0, byteCount / sizeof(char));
        GC.KeepAlive(_statement);
        return value;
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
