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
            string? definition = null;
            switch (definitionElement.ValueKind)
            {
                case JsonValueKind.String:
                {
                    definition = definitionElement.GetString();
                    break;
                }

                case JsonValueKind.Array:
                {
                    StringBuilder sb = Utils.StringBuilderPool.Get();

                    AppendDefinitionsFromJsonArray(sb, definitionElement);
                    if (sb.Length > 0)
                    {
                        definition = sb.ToString();
                    }

                    Utils.StringBuilderPool.Return(sb);

                    break;
                }

                case JsonValueKind.Object:
                {
                    YomichanContent objContent = GetDefinitionsFromJsonObject(definitionElement);
                    definition = objContent.Content;
                    break;
                }

                case JsonValueKind.Number:
                case JsonValueKind.Undefined:
                case JsonValueKind.True:
                case JsonValueKind.False:
                case JsonValueKind.Null:
                default:
                    break;
            }

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

    private static void AppendDefinitionsFromJsonArray(StringBuilder stringBuilder, JsonElement jsonElement, string? parentTag = null)
    {
        bool first = true;
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString());
            }
            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                AppendDefinitionsFromJsonArray(stringBuilder, definitionElement);
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

                        case "a":
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
                            if (!content.StartsWith('•'))
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

                        default:
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{contentResult.Content.TrimStart()}");
                            break;
                        }
                    }
                }
            }
        }
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
                    StringBuilder sb = Utils.StringBuilderPool.Get();

                    AppendDefinitionsFromJsonArray(sb, contentElement, tag);
                    string? content = null;
                    if (sb.Length > 0)
                    {
                        content = sb.ToString();
                    }

                    Utils.StringBuilderPool.Return(sb);
                    return new YomichanContent(parentTag ?? tag, content, false);
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
