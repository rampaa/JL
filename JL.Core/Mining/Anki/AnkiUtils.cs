using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Mining.Anki;

public static class AnkiUtils
{
    public static async ValueTask<List<string>?> GetDeckNames()
    {
        Response? response = await AnkiConnect.GetDeckNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString, Utils.s_jso)
            : null;
    }

    public static async ValueTask<List<string>?> GetModelNames()
    {
        Response? response = await AnkiConnect.GetModelNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString, Utils.s_jso)
            : null;
    }

    public static async ValueTask<ReadOnlyMemory<string>> GetFieldNames(string modelName)
    {
        Response? response = await AnkiConnect.GetModelFieldNamesResponse(modelName).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<ReadOnlyMemory<string>>(resultString, Utils.s_jso)
            : ReadOnlyMemory<string>.Empty;
    }

    internal static async Task<bool?> CanAddNote(Note note)
    {
        Response? response = await AnkiConnect.GetCanAddNotesResponse([note]).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<bool>(resultString.AsSpan(1, resultString.Length - 2), Utils.s_jso)
            : null;
    }

    internal static async ValueTask<ReadOnlyMemory<bool>> CanAddNotes(List<Note> notes)
    {
        Response? response = await AnkiConnect.GetCanAddNotesResponse(notes).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<ReadOnlyMemory<bool>>(resultString, Utils.s_jso)
            : ReadOnlyMemory<bool>.Empty;
    }
}
