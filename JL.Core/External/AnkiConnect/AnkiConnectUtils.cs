using System.Diagnostics;
using System.Text.Json;
namespace JL.Core.External.AnkiConnect;

public static class AnkiConnectUtils
{
    internal static Dictionary<string, object?> AnkiOptions { get; } = [];
    internal static Dictionary<string, object?> CheckDuplicateOptions { get; } = [];

    public static async ValueTask<string[]?> GetDeckNames()
    {
        Response? response = await AnkiConnectClient.GetDeckNamesResponse().ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] deckNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? deckName = element.GetString();
            Debug.Assert(deckName is not null);
            deckNames[index] = deckName;
            ++index;
        }

        return deckNames;
    }

    public static async ValueTask<string[]?> GetModelNames()
    {
        Response? response = await AnkiConnectClient.GetModelNamesResponse().ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] modelNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? modelName = element.GetString();
            Debug.Assert(modelName is not null);
            modelNames[index] = modelName;
            ++index;
        }

        return modelNames;
    }

    public static async ValueTask<string[]?> GetFieldNames(string modelName)
    {
        Response? response = await AnkiConnectClient.GetModelFieldNamesResponse(modelName).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        string[] fieldNames = new string[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            string? fieldName = element.GetString();
            Debug.Assert(fieldName is not null);
            fieldNames[index] = fieldName;
            ++index;
        }

        return fieldNames;
    }

    internal static async Task<bool?> CanAddNote(Note note)
    {
        Response? response = await AnkiConnectClient.GetCanAddNotesResponse([note]).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        foreach (JsonElement element in result.EnumerateArray())
        {
            return element.GetBoolean();
        }

        return null;
    }

    internal static async ValueTask<bool[]?> CanAddNotes(List<Note> notes)
    {
        Response? response = await AnkiConnectClient.GetCanAddNotesResponse(notes).ConfigureAwait(false);
        if (response is null)
        {
            return null;
        }

        JsonElement result = response.Result;
        bool[] canAddNotesArray = new bool[result.GetArrayLength()];

        int index = 0;
        foreach (JsonElement element in result.EnumerateArray())
        {
            canAddNotesArray[index] = element.GetBoolean();
            ++index;
        }

        return canAddNotesArray;
    }
}
