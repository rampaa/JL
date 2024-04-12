using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    public static string[]? GetDefinitions(JsonElement jsonElement)
    {
        List<string> definitions = [];
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            string? definition = null;
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                definition = definitionElement.GetString()!.Trim();
            }

            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                definition = GetDefinitionsFromJsonArray(definitionElement);
            }

            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                definition = GetDefinitionsFromJsonObject(definitionElement).Content;
            }

            if (definition is not null)
            {
                definitions.Add(definition.GetPooledString());
            }
        }

        return definitions.TrimStringListToStringArray();
    }

    private static string? GetDefinitionsFromJsonArray(JsonElement jsonElement, string? parentTag = null)
    {
        StringBuilder stringBuilder = new();

        bool first = true;
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString());
            }

            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                _ = stringBuilder.Append(GetDefinitionsFromJsonArray(definitionElement));
            }

            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                if (first)
                {
                    first = false;
                    parentTag = null;
                }

                YomichanContent contentResult = GetDefinitionsFromJsonObject(definitionElement, parentTag);
                if (contentResult.Content is not null)
                {
                    if (contentResult.Tag is null or "span")
                    {
                        _ = stringBuilder.Append(contentResult.Content);
                    }

                    else if (contentResult.Tag is "ruby" or "rt")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" ({contentResult.Content}) ");
                    }

                    else //if (contentResult.Tag is "div" or "a" or "li" or "ul" or "ol" or "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content.TrimStart('\n')}");
                    }
                }
            }
        }

        return stringBuilder.Length > 0
            ? stringBuilder.ToString().Trim()
            : null;
    }

    private static YomichanContent GetDefinitionsFromJsonObject(JsonElement jsonElement, string? parentTag = null)
    {
        while (true)
        {
            if (jsonElement.TryGetProperty("content", out JsonElement contentElement))
            {
                string? tag = null;
                if (jsonElement.TryGetProperty("tag", out JsonElement tagElement))
                {
                    tag = tagElement.GetString();
                }

                if (contentElement.ValueKind is JsonValueKind.String)
                {
                    return new YomichanContent(parentTag ?? tag, contentElement.GetString());
                }

                if (contentElement.ValueKind is JsonValueKind.Array)
                {
                    return new YomichanContent(parentTag ?? tag, GetDefinitionsFromJsonArray(contentElement, tag));
                }

                if (contentElement.ValueKind is JsonValueKind.Object)
                {
                    jsonElement = contentElement;
                    parentTag ??= tag;
                    continue;
                }
            }

            return default;
        }
    }
}
