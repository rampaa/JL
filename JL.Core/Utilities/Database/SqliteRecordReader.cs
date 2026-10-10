using System.Buffers;
using System.Diagnostics;
using MessagePack;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace JL.Core.Utilities.Database;

internal readonly ref struct SqliteRecordReader
{
    private readonly SqliteConnection _connection;
    private readonly sqlite3_stmt _statement;

    public SqliteRecordReader(SqliteConnection connection, ReadOnlySpan<byte> query)
    {
        _connection = connection;
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);

        int result = raw.sqlite3_prepare_v3(connectionHandle, query, 0, out sqlite3_stmt? statement);
        if (result is not raw.SQLITE_OK)
        {
            if (statement is not null)
            {
                _ = raw.sqlite3_finalize(statement);
            }

            SqliteException.ThrowExceptionForRC(result, connectionHandle);
        }

        Debug.Assert(statement is not null);
        _statement = statement;
    }

    public void Bind(int index, ReadOnlySpan<char> value)
    {
        int result = raw.sqlite3_bind_text16(_statement, index, value);
        if (result is not raw.SQLITE_OK)
        {
            sqlite3? connectionHandle = _connection.Handle;
            Debug.Assert(connectionHandle is not null);
            SqliteException.ThrowExceptionForRC(result, connectionHandle);
        }
    }

    public void Bind(int index, int value)
    {
        int result = raw.sqlite3_bind_int(_statement, index, value);
        if (result is not raw.SQLITE_OK)
        {
            sqlite3? connectionHandle = _connection.Handle;
            Debug.Assert(connectionHandle is not null);
            SqliteException.ThrowExceptionForRC(result, connectionHandle);
        }
    }

    public bool Read()
    {
        int result = raw.sqlite3_step(_statement);
        if (result is raw.SQLITE_ROW)
        {
            return true;
        }

        if (result is raw.SQLITE_DONE)
        {
            return false;
        }

        sqlite3? connectionHandle = _connection.Handle;
        Debug.Assert(connectionHandle is not null);
        SqliteException.ThrowExceptionForRC(result, connectionHandle);
        return false;
    }

    public bool IsNull(int index)
    {
        return raw.sqlite3_column_type(_statement, index) is raw.SQLITE_NULL;
    }

    public int GetInt32(int index)
    {
        return raw.sqlite3_column_int(_statement, index);
    }

    public long GetInt64(int index)
    {
        return raw.sqlite3_column_int64(_statement, index);
    }

    public double GetDouble(int index)
    {
        return raw.sqlite3_column_double(_statement, index);
    }

    public unsafe string GetString(int index)
    {
        nint statement = _statement.DangerousGetHandle();
        nint text = SqliteNativeMethods.GetColumnText16(statement, index);
        int byteCount = SqliteNativeMethods.GetColumnBytes16(statement, index);
        string value = new((char*)text, 0, byteCount / sizeof(char));
        GC.KeepAlive(_statement);
        return value;
    }

    // The span belongs to SQLite. Use it before Read/Dispose or converting this column to another format.
    public unsafe ReadOnlySpan<char> GetStringSpan(int index)
    {
        nint statement = _statement.DangerousGetHandle();
        nint text = SqliteNativeMethods.GetColumnText16(statement, index);
        int byteCount = SqliteNativeMethods.GetColumnBytes16(statement, index);
        ReadOnlySpan<char> value = new((char*)text, byteCount / sizeof(char));
        GC.KeepAlive(_statement);
        return value;
    }

    public T Deserialize<T>(int index)
    {
        ReadOnlySpan<byte> source = raw.sqlite3_column_blob(_statement, index);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(source.Length);
        try
        {
            source.CopyTo(buffer);
            return MessagePackSerializer.Deserialize<T>(buffer.AsMemory(0, source.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public T? DeserializeNullable<T>(int index) where T : class
    {
        return IsNull(index)
            ? null
            : Deserialize<T>(index);
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
