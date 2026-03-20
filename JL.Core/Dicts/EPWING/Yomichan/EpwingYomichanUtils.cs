using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    public static string[]? GetDefinitions(JsonElement jsonElement, Dict dict, ref List<string>? imagePaths)
    {
        List<string> definitions = new(jsonElement.GetArrayLength());
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            string? definition = null;
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                definition = definitionElement.GetString();
            }
            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                YomichanContent objContent = GetDefinitionsFromJsonObject(definitionElement, dict, ref imagePaths, null);
                if (objContent.Tag is "img")
                {
                    if (objContent.Content is not null)
                    {
                        imagePaths ??= [];
                        imagePaths.Add(objContent.Content);
                    }
                }
                else
                {
                    definition = objContent.Content;
                }
            }
            // else if (definitionElement.ValueKind is JsonValueKind.Array) {} // Deconjugation info, we don't need it, so we can skip it.

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

    private static void AppendDefinitionsFromJsonArray(StringBuilder stringBuilder, JsonElement jsonElement, Dict dict, ref List<string>? imagePaths, string? parentTag, bool isOrderedList, int orderedListIndex)
    {
        bool first = true;
        string? lastTag = null;

        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString());
                lastTag = null;
            }
            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                AppendDefinitionsFromJsonArray(stringBuilder, definitionElement, dict, ref imagePaths, null, isOrderedList, orderedListIndex);
                lastTag = null;
            }
            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                if (first)
                {
                    first = false;
                    parentTag = null;
                }

                YomichanContent contentResult = GetDefinitionsFromJsonObject(definitionElement, dict, ref imagePaths, parentTag);
                string? content = contentResult.Content;
                if (content is not null)
                {
                    switch (contentResult.Tag)
                    {
                        case "span":
                        {
                            _ = stringBuilder.Append(content);
                            if (contentResult.AppendWhitespace)
                            {
                                _ = stringBuilder.Append(' ');
                            }
                            break;
                        }

                        case "a":
                        case "ruby":
                        {
                            _ = stringBuilder.Append(content);
                            break;
                        }

                        case "rp":
                            // Already handled by the "rt" case.
                            break;

                        case "rt":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"[{content}]");
                            break;
                        }

                        case "li":
                        {
                            content = content.TrimStart();
                            string? marker = contentResult.Marker;
                            if (isOrderedList)
                            {
                                ++orderedListIndex;
                                marker = $"{orderedListIndex}.";
                            }
                            else
                            {
                                marker ??= "•";
                            }

                            if (content.StartsWith('•') || content.StartsWith(marker, StringComparison.Ordinal))
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{marker}\n{content}");
                            }
                            else
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{marker} {content}");
                            }
                            break;
                        }

                        case "ul":
                        case "ol":
                        {
                            _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                            break;
                        }

                        case "th":
                        case "td":
                        {
                            _ = stringBuilder.Append(" | ").Append(content.AsSpan().TrimStart());
                            break;
                        }

                        case "tr":
                        {
                            _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart()).Append(" |");
                            break;
                        }

                        case "img":
                        {
                            imagePaths ??= [];
                            imagePaths.Add(content);
                            break;
                        }

                        case "div":
                        {
                            if (lastTag is "div" && stringBuilder.Length > 0 && stringBuilder[^1] is '\n')
                            {
                                _ = stringBuilder.Append(content.AsSpan().Trim()).Append('\n');
                            }
                            else
                            {
                                _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                            }

                            break;
                        }

                        // "summary" or "details" or "table" or "thead" or "tbody" or "tfoot"
                        default:
                        {
                            _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart());
                            break;
                        }
                    }

                    lastTag = contentResult.Tag;
                }
                else if (contentResult.Tag is "br")
                {
                    _ = stringBuilder.Append('\n');
                    lastTag = contentResult.Tag;
                }
            }
        }
    }

    private static YomichanContent GetDefinitionsFromJsonObject(JsonElement jsonElement, Dict dict, ref List<string>? imagePaths, string? parentTag)
    {
        string? marker;
        while (true)
        {
            marker = jsonElement.TryGetProperty("style", out JsonElement styleElement) && styleElement.TryGetProperty("listStyleType", out JsonElement listStyleTypeElement)
                    ? listStyleTypeElement.GetString()
                    : null;

            marker = marker switch
            {
                "disc" => "•",
                "circle" => "◦",
                "square" => "▪",
                _ => marker
            };

            if (marker?.Length > 2 && marker[0] is '"' && marker[^1] is '"')
            {
                marker = marker[1..^1];
            }

            if (marker is not null
                && (marker.Length is 0
                    || char.IsAsciiLetter(marker[0])))
            {
                marker = null;
            }

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
                        if (tag is "th" && string.IsNullOrWhiteSpace(contentText))
                        {
                            contentText = "×";
                        }
                    }

                    Debug.Assert(contentText is not null);

                    bool appendWhitespace = tag is "span"
                        && (jsonElement.TryGetProperty("style", out styleElement)
                            ? styleElement.TryGetProperty("marginRight", out _)
                            // Heuristic for Japanese-English dictionaries whose CSS is stored in a separate file and thus cannot be parsed currently
                            : jsonElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("class", out _) && char.IsAscii(contentText[0]));

                    return new YomichanContent(parentTag ?? tag, contentText, appendWhitespace, marker);
                }

                if (contentElement.ValueKind is JsonValueKind.Array)
                {
                    StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

                    AppendDefinitionsFromJsonArray(sb, contentElement, dict, ref imagePaths, tag, tag is "ol", 0);
                    string? content = null;
                    if (sb.Length > 0)
                    {
                        content = sb.ToString();
                    }

                    ObjectPoolManager.StringBuilderPool.Return(sb);
                    return new YomichanContent(parentTag ?? tag, content, false, marker);
                }

                if (contentElement.ValueKind is JsonValueKind.Object)
                {
                    jsonElement = contentElement;
                    parentTag ??= tag;
                    continue;
                }
            }
            else if (jsonElement.TryGetProperty("tag", out JsonElement tagElement))
            {
                string? tag = tagElement.GetString();
                if (tag is "th")
                {
                    return new YomichanContent("th", "×", false, null);
                }

                if (tag is "img" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
                {
                    if ((jsonElement.TryGetProperty("height", out JsonElement heightProperty) && heightProperty.GetDouble() <= 5)
                        || (jsonElement.TryGetProperty("width", out JsonElement widthProperty) && widthProperty.GetDouble() <= 5))
                    {
                        //if (jsonElement.TryGetProperty("alt", out JsonElement altelement))
                        //{
                        //    string? altText = altelement.GetString();
                        //    if (!string.IsNullOrEmpty(altText))
                        //    {
                        //        return new YomichanContent("span", $"[{altText}]", false, null);
                        //    }
                        //}
                        //else if (jsonElement.TryGetProperty("description", out JsonElement descElement))
                        //{
                        //    string? descText = descElement.GetString();
                        //    if (!string.IsNullOrEmpty(descText))
                        //    {
                        //        return new YomichanContent("span", $"[{descText}]", false, null);
                        //    }
                        //}

                        return default;
                    }

                    string? imagePath = imagePathJsonElement.GetString();
                    Debug.Assert(imagePath is not null);

                    return new YomichanContent("img", PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath)), false, null);
                }

                if (jsonElement.TryGetProperty("title", out JsonElement titleJsonElement))
                {
                    return new YomichanContent(parentTag ?? tag, titleJsonElement.GetString(), false, null);
                }
            }
            else if (jsonElement.TryGetProperty("type", out JsonElement typeJsonElement))
            {
                string? type = typeJsonElement.GetString();
                if (type is "text" && jsonElement.TryGetProperty("text", out JsonElement textElement))
                {
                    return new YomichanContent("span", textElement.GetString(), false, null);
                }

                if (type is "image" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
                {
                    if ((jsonElement.TryGetProperty("height", out JsonElement heightProperty) && heightProperty.GetDouble() <= 5)
                        || (jsonElement.TryGetProperty("width", out JsonElement widthProperty) && widthProperty.GetDouble() <= 5))
                    {
                        //if (jsonElement.TryGetProperty("alt", out JsonElement altElement))
                        //{
                        //    string? altText = altElement.GetString();
                        //    if (!string.IsNullOrWhiteSpace(altText))
                        //    {
                        //        return new YomichanContent("span", $"[{altText}]", false, null);
                        //    }
                        //}
                        //else if (jsonElement.TryGetProperty("description", out JsonElement descElement))
                        //{
                        //    string? descText = descElement.GetString();
                        //    if (!string.IsNullOrWhiteSpace(descText))
                        //    {
                        //        return new YomichanContent("span", $"[{descText}]", false, null);
                        //    }
                        //}

                        return default;
                    }

                    string? imagePath = imagePathJsonElement.GetString();
                    Debug.Assert(imagePath is not null);
                    return new YomichanContent("img", PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath)), false, null);
                }
            }

            return default;
        }
    }

    public static async Task UpdateRevisionInfo(Dict dict)
    {
        string indexJsonPath = Path.GetFullPath(Path.Join(dict.Path, "index.json"), AppInfo.ApplicationPath);
        if (File.Exists(indexJsonPath))
        {
            JsonElement jsonElement;

            FileStream fileStream = new(indexJsonPath, FileStreamOptionsPresets.s_asyncReadFso);
            await using (fileStream.ConfigureAwait(false))
            {
                jsonElement = await JsonSerializer.DeserializeAsync<JsonElement>(fileStream, JsonOptions.DefaultJso).ConfigureAwait(false);
            }

            dict.Revision = jsonElement.GetProperty("revision").GetString();
            dict.AutoUpdatable = jsonElement.TryGetProperty("isUpdatable", out JsonElement isUpdatableJsonElement) && isUpdatableJsonElement.GetBoolean();
            if (dict.AutoUpdatable)
            {
                string? indexUrl = jsonElement.GetProperty("indexUrl").GetString();
                Debug.Assert(indexUrl is not null);
                dict.Url = new Uri(indexUrl);
            }
        }
    }
}
