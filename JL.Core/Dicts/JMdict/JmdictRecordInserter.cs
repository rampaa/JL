using System.Diagnostics;
using MessagePack;
using Microsoft.Data.Sqlite;
using SQLitePCL;
using static JL.Core.Dicts.JMdict.JmdictDBManager;

namespace JL.Core.Dicts.JMdict;

internal sealed class JmdictRecordInserter : IDisposable
{
    private readonly sqlite3 _connectionHandle;
    private readonly sqlite3_stmt _recordStatement;
    private readonly sqlite3_stmt _searchKeyStatement;

    public JmdictRecordInserter(SqliteConnection connection)
    {
        sqlite3? connectionHandle = connection.Handle;
        Debug.Assert(connectionHandle is not null);
        _connectionHandle = connectionHandle;

        int result = raw.sqlite3_prepare_v3(_connectionHandle,
            $"""
            INSERT INTO {Record} ({RowId}, {EdictId}, {PrimarySpelling}, {PrimarySpellingOrthographyInfo}, {AlternativeSpellings}, {AlternativeSpellingsOrthographyInfo}, {Readings}, {ReadingsOrthographyInfo}, {ReadingRestrictions}, {Glossary}, {GlossaryInfo}, {PartOfSpeechSharedByAllSenses}, {PartOfSpeech}, {SpellingRestrictions}, {FieldsSharedByAllSenses}, {Fields}, {MiscSharedByAllSenses}, {Misc}, {DialectsSharedByAllSenses}, {Dialects}, {LoanwordEtymology}, {CrossReferences}, {Info})
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10, ?11, ?12, ?13, ?14, ?15, ?16, ?17, ?18, ?19, ?20, ?21, ?22, ?23);
            """,
            raw.SQLITE_PREPARE_PERSISTENT, out sqlite3_stmt? recordStatement);
        if (result is not raw.SQLITE_OK)
        {
            if (recordStatement is not null)
            {
                _ = raw.sqlite3_finalize(recordStatement);
            }

            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        Debug.Assert(recordStatement is not null);
        _recordStatement = recordStatement;

        result = raw.sqlite3_prepare_v3(_connectionHandle,
            $"""
            INSERT INTO {RecordSearchKey} ({RecordId}, {SearchKey})
            VALUES (?1, ?2);
            """,
            raw.SQLITE_PREPARE_PERSISTENT, out sqlite3_stmt? searchKeyStatement);
        if (result is not raw.SQLITE_OK)
        {
            if (searchKeyStatement is not null)
            {
                _ = raw.sqlite3_finalize(searchKeyStatement);
            }

            _ = raw.sqlite3_finalize(_recordStatement);
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        Debug.Assert(searchKeyStatement is not null);
        _searchKeyStatement = searchKeyStatement;
    }

    public void InsertRecord(long rowId, JmdictRecord record)
    {
        int result = raw.sqlite3_bind_int64(_recordStatement, 1, rowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_int(_recordStatement, 2, record.Id);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_bind_text16(_recordStatement, 3, record.PrimarySpelling.AsSpan());
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        BindBlob(4, record.PrimarySpellingOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.PrimarySpellingOrthographyInfo) : null);
        BindBlob(5, record.AlternativeSpellings is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellings) : null);
        BindBlob(6, record.AlternativeSpellingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.AlternativeSpellingsOrthographyInfo) : null);
        BindBlob(7, record.Readings is not null ? MessagePackSerializer.Serialize(record.Readings) : null);
        BindBlob(8, record.ReadingsOrthographyInfo is not null ? MessagePackSerializer.Serialize(record.ReadingsOrthographyInfo) : null);
        BindBlob(9, record.ReadingRestrictions is not null ? MessagePackSerializer.Serialize(record.ReadingRestrictions) : null);
        BindBlob(10, MessagePackSerializer.Serialize(record.Definitions));
        BindBlob(11, record.DefinitionInfo is not null ? MessagePackSerializer.Serialize(record.DefinitionInfo) : null);
        BindBlob(12, record.WordClassesSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.WordClassesSharedByAllSenses) : null);
        BindBlob(13, record.WordClasses is not null ? MessagePackSerializer.Serialize(record.WordClasses) : null);
        BindBlob(14, record.SpellingRestrictions is not null ? MessagePackSerializer.Serialize(record.SpellingRestrictions) : null);
        BindBlob(15, record.FieldsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.FieldsSharedByAllSenses) : null);
        BindBlob(16, record.Fields is not null ? MessagePackSerializer.Serialize(record.Fields) : null);
        BindBlob(17, record.MiscSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.MiscSharedByAllSenses) : null);
        BindBlob(18, record.Misc is not null ? MessagePackSerializer.Serialize(record.Misc) : null);
        BindBlob(19, record.DialectsSharedByAllSenses is not null ? MessagePackSerializer.Serialize(record.DialectsSharedByAllSenses) : null);
        BindBlob(20, record.Dialects is not null ? MessagePackSerializer.Serialize(record.Dialects) : null);
        BindBlob(21, record.LoanwordEtymology is not null ? MessagePackSerializer.Serialize(record.LoanwordEtymology) : null);
        BindBlob(22, record.CrossReferences is not null ? MessagePackSerializer.Serialize(record.CrossReferences) : null);
        BindBlob(23, record.Info is not null ? MessagePackSerializer.Serialize(record.Info) : null);

        StepAndReset(_recordStatement);
    }

    public void InsertSearchKeys(long rowId, ReadOnlySpan<string> searchKeys)
    {
        int result = raw.sqlite3_bind_int64(_searchKeyStatement, 1, rowId);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        foreach (string searchKey in searchKeys)
        {
            result = raw.sqlite3_bind_text16(_searchKeyStatement, 2, searchKey.AsSpan());
            if (result is not raw.SQLITE_OK)
            {
                SqliteException.ThrowExceptionForRC(result, _connectionHandle);
            }

            StepAndReset(_searchKeyStatement);
        }
    }

    private void BindBlob(int index, byte[]? value)
    {
        int result = value is not null
            ? raw.sqlite3_bind_blob(_recordStatement, index, value.AsSpan())
            : raw.sqlite3_bind_null(_recordStatement, index);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }
    }

    private void StepAndReset(sqlite3_stmt statement)
    {
        int result = raw.sqlite3_step(statement);
        if (result is not raw.SQLITE_DONE)
        {
            _ = raw.sqlite3_reset(statement);
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }

        result = raw.sqlite3_reset(statement);
        if (result is not raw.SQLITE_OK)
        {
            SqliteException.ThrowExceptionForRC(result, _connectionHandle);
        }
    }

    public void Dispose()
    {
        _ = raw.sqlite3_finalize(_recordStatement);
        _ = raw.sqlite3_finalize(_searchKeyStatement);
    }
}
