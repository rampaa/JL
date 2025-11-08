using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using JL.Core.Utilities;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    public static string[]? GetDefinitions(JsonElement jsonElement, Dict dict, List<string> imagePaths)
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

                AppendDefinitionsFromJsonArray(sb, definitionElement, dict, imagePaths);
                if (sb.Length > 0)
                {
                    definition = sb.ToString();
                }

                ObjectPoolManager.StringBuilderPool.Return(sb);
            }
            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                YomichanContent objContent = GetDefinitionsFromJsonObject(definitionElement, dict, imagePaths);
                if (objContent.Tag is "img")
                {
                    if (objContent.Content is not null)
                    {
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

    private static void AppendDefinitionsFromJsonArray(StringBuilder stringBuilder, JsonElement jsonElement, Dict dict, List<string> imagePaths, string? parentTag = null)
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
                AppendDefinitionsFromJsonArray(stringBuilder, definitionElement, dict, imagePaths);
            }
            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                if (first)
                {
                    first = false;
                    parentTag = null;
                }

                YomichanContent contentResult = GetDefinitionsFromJsonObject(definitionElement, dict, imagePaths, parentTag);
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

                        case "img":
                        {
                            imagePaths.Add(contentResult.Content);
                            break;
                        }

                        // "div" or "tr" or "p" or "summary" or "details" or "br" or "rp" or "table" or "thead" or "tbody" or "tfoot"
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

    private static YomichanContent GetDefinitionsFromJsonObject(JsonElement jsonElement, Dict dict, List<string> imagePaths, string? parentTag = null)
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
                    StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

                    AppendDefinitionsFromJsonArray(sb, contentElement, dict, imagePaths, tag);
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
