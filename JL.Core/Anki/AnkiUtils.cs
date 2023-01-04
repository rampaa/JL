using System.Text.Json;

namespace JL.Core.Anki;

public static class AnkiUtils
{
    public static async Task<List<string>?> GetDeckNames()
    {
        Response? response = await AnkiConnect.GetDeckNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }

    public static async Task<List<string>?> GetModelNames()
    {
        Response? response = await AnkiConnect.GetModelNamesResponse().ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }

    public static async Task<List<string>?> GetFieldNames(string modelName)
    {
        Response? response = await AnkiConnect.GetModelFieldNamesResponse(modelName).ConfigureAwait(false);
        string? resultString = response?.Result?.ToString() ?? null;

        return resultString is not null
            ? JsonSerializer.Deserialize<List<string>>(resultString)
            : null;
    }
}
