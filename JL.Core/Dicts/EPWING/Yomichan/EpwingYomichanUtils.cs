using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    public static string[]? GetDefinitions(JsonElement jsonElement, Dict dict)
    {
        List<string> definitions = [];
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            string? definition = definitionElement.ValueKind switch
            {
                JsonValueKind.String => definitionElement.GetString()!.Trim(),
                JsonValueKind.Array => GetDefinitionsFromJsonArray(definitionElement),
                JsonValueKind.Object => GetDefinitionsFromJsonObject(definitionElement).Content,
                JsonValueKind.Number => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.True => null,
                JsonValueKind.False => null,
                JsonValueKind.Null => null,
                _ => null
            };

            if (definition is not null)
            {
                if (dict.Type is DictType.Kenkyuusha)
                {
                    definition = definition.Replace("┏", "", StringComparison.Ordinal);
                }

                if (!string.IsNullOrWhiteSpace(definition))
                {
                    definitions.Add(definition.GetPooledString());
                }
            }
        }

        return definitions.TrimListToArray();
    }

    private static string? GetDefinitionsFromJsonArray(JsonElement jsonElement, string? parentTag = null)
    {
        StringBuilder stringBuilder = new();

        bool first = true;
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString()).Append(' ');
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
                    if (contentResult.Tag is null or "span" or "ruby")
                    {
                        _ = stringBuilder.Append(contentResult.Content);
                    }
                    else if (contentResult.Tag is "li" && !contentResult.Content.StartsWith('•'))
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n• {contentResult.Content}");
                    }
                    else if (contentResult.Tag is "rt")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"({contentResult.Content})");
                    }
                    else if (contentResult.Tag is "th" or "td")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\t{contentResult.Content}");
                    }
                    else //if (contentResult.Tag is "div" or "a" or "ul" or "ol" or "tr" or "p" or "h1" or "h2" or "h3" or "h4" or "h5" or "h6")
                    {
                        _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content}");
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
                    string? contentText;
                    if (tag is "a" && jsonElement.TryGetProperty("href", out JsonElement hrefElement))
                    {
                        string? hrefText = hrefElement.GetString();
                        contentText = hrefText?.StartsWith("?query=", StringComparison.Ordinal) ?? true
                            ? contentElement.GetString()
                            : $"{contentElement.GetString()}: {hrefText}";
                    }
                    else
                    {
                        contentText = contentElement.GetString();
                    }

                    return new YomichanContent(parentTag ?? tag, contentText);
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
