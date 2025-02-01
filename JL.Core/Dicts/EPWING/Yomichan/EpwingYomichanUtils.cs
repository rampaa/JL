using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    public static string[]? GetDefinitions(JsonElement jsonElement)
    {
        List<string> definitions = new(jsonElement.GetArrayLength());
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

            if (!string.IsNullOrWhiteSpace(definition))
            {
                definitions.Add(definition.GetPooledString());
            }
        }

        return definitions.TrimToArray();
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
                    switch (contentResult.Tag)
                    {
                        case "span":
                        case "ruby":
                            _ = stringBuilder.Append(contentResult.Content);
                            break;

                        case "rt":
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"({contentResult.Content})");
                            break;

                        case "li":
                            if (!contentResult.Content.StartsWith('•'))
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n• {contentResult.Content}");
                            }
                            else
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content}");
                            }
                            break;

                        case "ul":
                        case "ol":
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content}\n");
                            break;

                        case "th":
                        case "td":
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\t{contentResult.Content}");
                            break;

                        // "div" or "a" or "tr" or "p" or "summary" or "details" or "br" or "rp" or "table" or "thead" or "tbody" or "tfoot" or "img"
                        default:
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content}");
                            break;
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
