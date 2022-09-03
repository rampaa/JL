using System.Text.Json;

namespace JL.Core.Anki;

public static class AnkiUtils
{
    public static async Task<List<string>?> GetDeckNames()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetDeckNamesResponse().ConfigureAwait(false))?.Result?.ToString()!)!;
        }

        catch
        {
            return null;
        }
    }

    public static async Task<List<string>?> GetModelNames()
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelNamesResponse().ConfigureAwait(false))?.Result?.ToString()!)!;
        }

        catch
        {
            return null;
        }
    }

    public static async Task<List<string>?> GetFieldNames(string modelName)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>((await AnkiConnect.GetModelFieldNamesResponse(modelName).ConfigureAwait(false))?.Result?.ToString()!)!;
        }

        catch
        {
            return null;
        }
    }
}
