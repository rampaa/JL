using System.Text.Json;

namespace JL.Core.Mining.Anki;

public static class AnkiUtils
{
    public static async ValueTask<List<string>?> GetDeckNames()
    {
        Response? response = await AnkiConnect.GetDeckNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }

    public static async ValueTask<List<string>?> GetModelNames()
    {
        Response? response = await AnkiConnect.GetModelNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }

    public static async ValueTask<List<string>?> GetFieldNames(string modelName)
    {
        Response? response = await AnkiConnect.GetModelFieldNamesResponse(modelName).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }

    internal static async Task<bool?> CanAddNote(Note note)
    {
        Response? response = await AnkiConnect.GetCanAddNotesResponse(note).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<bool>>(resultString)![0]
            : null;
    }
}
