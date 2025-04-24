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
                JsonValueKind.String => definitionElement.GetString(),
                JsonValueKind.Array => GetDefinitionsFromJsonArray(definitionElement)?.Trim(),
                JsonValueKind.Object => GetDefinitionsFromJsonObject(definitionElement).Content?.Trim(),
                JsonValueKind.Number => definitionElement.GetString(),
                JsonValueKind.True => null,
                JsonValueKind.False => null,
                JsonValueKind.Undefined => null,
                JsonValueKind.Null => null,
                _ => null
            };

            if (definition is not null)
            {
                string trimmedDefinition = definition.Trim();
                if (trimmedDefinition.Length is not 0)
                {
                    definitions.Add(trimmedDefinition.GetPooledString());
                }
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
                _ = stringBuilder.Append(definitionElement.GetString());
            }

            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                string? content = GetDefinitionsFromJsonArray(definitionElement);
                if (content is not null)
                {
                    _ = stringBuilder.Append(content);
                }
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
                        {
                            _ = stringBuilder.Append(contentResult.Content);
                            if (contentResult.AppendWhitespace)
                            {
                                _ = stringBuilder.Append('\t');
                            }
                            break;
                        }

                        case "ruby":
                        {
                            _ = stringBuilder.Append(contentResult.Content);
                            break;
                        }

                        case "rt":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"({contentResult.Content})");
                            break;
                        }

                        case "li":
                        {
                            string content = contentResult.Content.TrimStart();
                            if (!content.AsSpan().StartsWith('•'))
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n• {content}");
                            }
                            else
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{content}");
                            }
                            break;
                        }

                        case "ul":
                        case "ol":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content.Trim()}\n");
                            break;
                        }

                        case "th":
                        case "td":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\t{contentResult.Content.TrimStart()}");
                            break;
                        }

                        // "div" or "a" or "tr" or "p" or "summary" or "details" or "br" or "rp" or "table" or "thead" or "tbody" or "tfoot" or "img"
                        default:
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content.TrimStart()}");
                            break;
                        }
                    }
                }
            }
        }

        return stringBuilder.Length > 0
            ? stringBuilder.ToString()
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
                        contentText = hrefText?.AsSpan().StartsWith("?query=", StringComparison.Ordinal) ?? true
                            ? contentElement.GetString()
                            : $"{contentElement.GetString()}: {hrefText}";
                    }
                    else
                    {
                        contentText = contentElement.GetString();
                    }

                    bool appendWhitespace = tag is "span"
                        && jsonElement.TryGetProperty("style", out JsonElement styleElement)
                        && styleElement.TryGetProperty("marginRight", out _);

                    return new YomichanContent(parentTag ?? tag, contentText, appendWhitespace);
                }

                if (contentElement.ValueKind is JsonValueKind.Array)
                {
                    return new YomichanContent(parentTag ?? tag, GetDefinitionsFromJsonArray(contentElement, tag), false);
                }

                if (contentElement.ValueKind is JsonValueKind.Object)
                {
                    jsonElement = contentElement;
                    parentTag ??= tag;
                    continue;
                }
            }
            else if (jsonElement.TryGetProperty("tag", out JsonElement tagElement) && tagElement.GetString() is "th")
            {
                return new YomichanContent("th", "×", false);
            }

            return default;
        }
    }
}
