using System.Diagnostics;
using MessagePack;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace JL.Core.Utilities.Database;

internal readonly ref struct SqliteRecordReader
{
    private readonly SqliteConnection _connection;
    private readonly sqlite3_stmt _statement;

    public SqliteRecordReader(SqliteConnection connection, string query)
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

    public void Bind(int index, string value)
    {
        int result = raw.sqlite3_bind_text16(_statement, index, value.AsSpan());
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
        return checked((int)raw.sqlite3_column_int64(_statement, index));
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

    public byte[] GetBytes(int index)
    {
        return raw.sqlite3_column_blob(_statement, index).ToArray();
    }

    public T Deserialize<T>(int index)
    {
        return MessagePackSerializer.Deserialize<T>(GetBytes(index));
    }

    public T Deserialize<T>(string tableName, string columnName, long rowId)
    {
        using SqliteBlob stream = new(_connection, tableName, columnName, rowId, true);
        return MessagePackSerializer.Deserialize<T>(stream);
    }

    public T? DeserializeNullable<T>(int index, string tableName, string columnName, long rowId) where T : class
    {
        return IsNull(index)
            ? null
            : Deserialize<T>(tableName, columnName, rowId);
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
