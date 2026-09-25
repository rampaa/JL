using System.Diagnostics;
using JL.Core.Utilities.Database;
using MessagePack;
using Microsoft.Data.Sqlite;
using SQLitePCL;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal readonly ref struct YomichanRecordReader
{
    private readonly SqliteConnection _connection;
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _statement;

    public YomichanRecordReader(SqliteConnection connection, string query)
    {
        _connection = connection;
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(_connectionHandle, query, 0, out sqlite3_stmt? statement);
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

    public void Bind(int index, string value)
    {
        int result = raw.sqlite3_bind_text16(_statement, index, value.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
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

        SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        return false;
    }

    public unsafe string GetString(YomichanColumnIndex column)
    {
        int index = (int)column;
        nint statement = _statement.DangerousGetHandle();
        nint text = SqliteNativeMethods.GetColumnText16(statement, index);
        int byteCount = SqliteNativeMethods.GetColumnBytes16(statement, index);
        string value = new((char*)text, 0, byteCount / sizeof(char));
        GC.KeepAlive(_statement);
        return value;
    }

    public EpwingYomichanRecord GetRecord()
    {
        long rowId = raw.sqlite3_column_int64(_statement, (int)YomichanColumnIndex.RowId);
        string primarySpelling = GetString(YomichanColumnIndex.PrimarySpelling);

        string? reading = raw.sqlite3_column_type(_statement, (int)YomichanColumnIndex.Reading) is raw.SQLITE_NULL
            ? null
            : GetString(YomichanColumnIndex.Reading);

        double popularityScore = raw.sqlite3_column_double(_statement, (int)YomichanColumnIndex.PopularityScore);

        string[] definitions = Deserialize<string[]>(EpwingYomichanDBManager.Glossary, rowId);
        string[]? wordClasses = DeserializeNullable<string[]>(YomichanColumnIndex.PartOfSpeech, EpwingYomichanDBManager.PartOfSpeech, rowId);
        string[]? definitionTags = DeserializeNullable<string[]>(YomichanColumnIndex.GlossaryTags, EpwingYomichanDBManager.GlossaryTags, rowId);
        ImageInfo[]? imageInfos = DeserializeNullable<ImageInfo[]>(YomichanColumnIndex.ImageInfos, EpwingYomichanDBManager.ImageInfos, rowId);

        return new EpwingYomichanRecord(primarySpelling, reading, popularityScore, definitions, wordClasses, definitionTags, imageInfos);
    }

    private T Deserialize<T>(string columnName, long rowId)
    {
        using SqliteBlob stream = new(_connection, EpwingYomichanDBManager.Record, columnName, rowId, true);
        return MessagePackSerializer.Deserialize<T>(stream);
    }

    private T? DeserializeNullable<T>(YomichanColumnIndex column, string columnName, long rowId) where T : class
    {
        return raw.sqlite3_column_type(_statement, (int)column) is raw.SQLITE_NULL
            ? null
            : Deserialize<T>(columnName, rowId);
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_statement);
    }
}
