using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Mining.Anki;

public static class AnkiUtils
{
    public static async ValueTask<string[]?> GetDeckNames()
    {
        Response? response = await AnkiConnect.GetDeckNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<string[]>(resultString, Utils.s_jsoNotIgnoringNull)
            : null;
    }

    public static async ValueTask<string[]?> GetModelNames()
    {
        Response? response = await AnkiConnect.GetModelNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<string[]?>(resultString, Utils.s_jsoNotIgnoringNull)
            : null;
    }

    public static async ValueTask<string[]?> GetFieldNames(string modelName)
    {
        Response? response = await AnkiConnect.GetModelFieldNamesResponse(modelName).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<string[]>(resultString, Utils.s_jsoNotIgnoringNull)
            : null;
    }

    internal static async Task<bool?> CanAddNote(Note note)
    {
        Response? response = await AnkiConnect.GetCanAddNotesResponse(note).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<bool[]>(resultString)![0]
            : null;
    }
}
