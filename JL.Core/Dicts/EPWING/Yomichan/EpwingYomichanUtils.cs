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
            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

                AppendDefinitionsFromJsonArray(sb, definitionElement, dict, ref imagePaths, null);
                if (sb.Length > 0)
                {
                    definition = sb.ToString();
                }

                ObjectPoolManager.StringBuilderPool.Return(sb);
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

    private static void AppendDefinitionsFromJsonArray(StringBuilder stringBuilder, JsonElement jsonElement, Dict dict, ref List<string>? imagePaths, string? parentTag)
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
                AppendDefinitionsFromJsonArray(stringBuilder, definitionElement, dict, ref imagePaths, null);
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

                        case "rt":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"[{content}]");
                            break;
                        }

                        case "li":
                        {
                            content = content.TrimStart();
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
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{content.Trim()}\n");
                            break;
                        }

                        case "th":
                        case "td":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $" | {content.TrimStart()}");
                            break;
                        }

                        case "tr":
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{content.TrimStart()} |");
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
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"{content.Trim()}\n");
                            }
                            else
                            {
                                _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{content.Trim()}\n");
                            }

                            break;
                        }

                        // "p" or "summary" or "details" or "br" or "rp" or "table" or "thead" or "tbody" or "tfoot"
                        default:
                        {
                            _ = stringBuilder.Append(CultureInfo.InvariantCulture, $"\n{content.TrimStart()}");
                            break;
                        }
                    }

                    lastTag = contentResult.Tag;
                }
            }
        }
    }

    private static YomichanContent GetDefinitionsFromJsonObject(JsonElement jsonElement, Dict dict, ref List<string>? imagePaths, string? parentTag)
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
                        if (tag is "th" && string.IsNullOrWhiteSpace(contentText))
                        {
                            contentText = "×";
                        }
                    }

                    Debug.Assert(contentText is not null);

                    bool appendWhitespace = tag is "span"
                        && (jsonElement.TryGetProperty("style", out JsonElement styleElement)
                            ? styleElement.TryGetProperty("marginRight", out _)
                            // Heuristic for Japanese-English dictionaries whose CSS is stored in a separate file and thus cannot be parsed currently
                            : jsonElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("class", out _) && char.IsAscii(contentText[0]));

                    return new YomichanContent(parentTag ?? tag, contentText, appendWhitespace);
                }

                if (contentElement.ValueKind is JsonValueKind.Array)
                {
                    StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

                    AppendDefinitionsFromJsonArray(sb, contentElement, dict, ref imagePaths, tag);
                    string? content = null;
                    if (sb.Length > 0)
                    {
                        content = sb.ToString();
                    }

                    ObjectPoolManager.StringBuilderPool.Return(sb);
                    return new YomichanContent(parentTag ?? tag, content, false);
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
                    return new YomichanContent("th", "×", false);
                }

                if (tag is "img" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
                {
                    if (jsonElement.TryGetProperty("height", out JsonElement heightProperty))
                    {
                        if (heightProperty.GetDouble() <= 5)
                        {
                            return default;
                        }
                    }
                    else if (jsonElement.TryGetProperty("width", out JsonElement widthProperty))
                    {
                        if (widthProperty.GetDouble() <= 5)
                        {
                            return default;
                        }
                    }

                    string? imagePath = imagePathJsonElement.GetString();
                    Debug.Assert(imagePath is not null);

                    return new YomichanContent("img", PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath)), false);
                }

                if (jsonElement.TryGetProperty("title", out JsonElement titleJsonElement))
                {
                    return new YomichanContent(parentTag ?? tag, titleJsonElement.GetString(), false);
                }
            }
            else if (jsonElement.TryGetProperty("type", out JsonElement typeJsonElement))
            {
                if (typeJsonElement.GetString() is "image" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
                {
                    if (jsonElement.TryGetProperty("height", out JsonElement heightProperty))
                    {
                        if (heightProperty.GetDouble() <= 5)
                        {
                            return default;
                        }
                    }
                    else if (jsonElement.TryGetProperty("width", out JsonElement widthProperty))
                    {
                        if (widthProperty.GetDouble() <= 5)
                        {
                            return default;
                        }
                    }

                    string? imagePath = imagePathJsonElement.GetString();
                    Debug.Assert(imagePath is not null);
                    return new YomichanContent("img", PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath)), false);
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
