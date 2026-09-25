using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using JL.Core.Frontend;
using JL.Core.Utilities;
using JL.Core.Utilities.ObjectPool;

namespace JL.Core.Dicts.EPWING.Yomichan;

internal static class EpwingYomichanUtils
{
    private static readonly ImageInfo s_missingImageInfo = new("", 0, 0, 0, 0);

    internal static string[]? GetDefinitions(JsonElement jsonElement, Dict dict, ref List<ImageInfo>? imageInfos,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache)
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
                YomichanContent<string?> objContent = GetDefinitionsFromJsonObject(
                    definitionElement, dict, ref imageInfos, imageInfoCache);

                if (objContent.Tag is "img")
                {
                    if (objContent.Content is not null)
                    {
                        ImageInfo? imageInfo = GetImageInfo(objContent.Content, imageInfoCache);
                        if (imageInfo is not null)
                        {
                            imageInfos ??= [];
                            imageInfos.Add(imageInfo);
                        }
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

    internal static ImageInfo? GetImageInfo(string imagePath, ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        if (imageInfoCache.TryGetValue(imagePath, out ImageInfo? imageInfo))
        {
            return ReferenceEquals(imageInfo, s_missingImageInfo) ? null : imageInfo;
        }

        imageInfo = FrontendManager.Frontend.GetImageInfo(imagePath);
        if (!imageInfoCache.TryAdd(imagePath, imageInfo ?? s_missingImageInfo)
            && imageInfoCache.TryGetValue(imagePath, out ImageInfo? cachedImageInfo))
        {
            imageInfo = ReferenceEquals(cachedImageInfo, s_missingImageInfo) ? null : cachedImageInfo;
        }

        return imageInfo;
    }

    private static bool IsSmallImage(JsonElement jsonElement)
    {
        if (!jsonElement.TryGetProperty("height", out JsonElement heightProperty)
            || !jsonElement.TryGetProperty("width", out JsonElement widthProperty))
        {
            return false;
        }

        double maximumSize = jsonElement.TryGetProperty("sizeUnits", out JsonElement sizeUnitsProperty)
            && sizeUnitsProperty.ValueEquals("em")
                ? 1
                : 16;

        return heightProperty.GetDouble() <= maximumSize && widthProperty.GetDouble() <= maximumSize;
    }

    internal static bool IsSmallImageWithMissingDimension(string imagePath, double height, double width,
        bool heightSpecified, bool widthSpecified, bool sizeUnitsEm, ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        if (heightSpecified && widthSpecified)
        {
            return false;
        }

        double maximumSize = sizeUnitsEm ? 1 : 16;
        if ((heightSpecified && height > maximumSize) || (widthSpecified && width > maximumSize))
        {
            return false;
        }

        ImageInfo? imageInfo = GetImageInfo(imagePath, imageInfoCache);
        return imageInfo is not null && imageInfo.PixelWidth <= 16 && imageInfo.PixelHeight <= 16;
    }

    private static bool IsSmallImageWithMissingDimension(JsonElement jsonElement, string imagePath,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        bool heightSpecified = jsonElement.TryGetProperty("height", out JsonElement heightProperty);
        bool widthSpecified = jsonElement.TryGetProperty("width", out JsonElement widthProperty);
        if (heightSpecified && widthSpecified)
        {
            return false;
        }

        double specifiedDimension = 0;
        if (heightSpecified || widthSpecified)
        {
            JsonElement specifiedDimensionProperty = heightSpecified ? heightProperty : widthProperty;
            if (specifiedDimensionProperty.ValueKind is not JsonValueKind.Number
                || !specifiedDimensionProperty.TryGetDouble(out specifiedDimension))
            {
                return false;
            }
        }

        bool sizeUnitsEm = jsonElement.TryGetProperty("sizeUnits", out JsonElement sizeUnitsProperty)
            && sizeUnitsProperty.ValueEquals("em");
        return IsSmallImageWithMissingDimension(imagePath, heightSpecified ? specifiedDimension : 0,
            widthSpecified ? specifiedDimension : 0, heightSpecified, widthSpecified, sizeUnitsEm, imageInfoCache);
    }

    internal static string GetListMarker(string marker, int index)
    {
        // Add support for decimal-leading-zero, alphabetic, Roman and Japanese counter styles if a dictionary uses them.
        // The same applies to inherit, unset and initial.
        if (marker.Length > 1 && (marker[0] is '"' or '\'') && marker[^1] == marker[0])
        {
            return marker[1..^1];
        }

        if (marker.Length > 0 && char.IsAsciiLetter(marker[0]))
        {
            marker = $"{index}.";
        }

        return marker;
    }

    internal static void AppendTableCell(StringBuilder stringBuilder, string? content, int colSpan, int rowSpan,
        List<int> rowSpans, ref int columnIndex)
    {
        while (columnIndex < rowSpans.Count && rowSpans[columnIndex] > 0)
        {
            _ = stringBuilder.Append(" | ");
            ++columnIndex;
        }

        _ = stringBuilder.Append(" | ");
        if (content is not null)
        {
            _ = stringBuilder.Append(content.AsSpan().TrimStart());
        }

        int endColumn = columnIndex + colSpan;
        if (rowSpan > 1)
        {
            while (rowSpans.Count < endColumn)
            {
                rowSpans.Add(0);
            }

            int i = columnIndex;
            while (i < endColumn)
            {
                rowSpans[i] = rowSpan;
                ++i;
            }
        }

        int remainingColumns = colSpan - 1;
        while (remainingColumns > 0)
        {
            _ = stringBuilder.Append(" | ");
            --remainingColumns;
        }

        columnIndex = endColumn;
    }

    internal static void AdvanceTableRow(StringBuilder stringBuilder, List<int> rowSpans, int columnIndex)
    {
        int lastSpannedColumn = rowSpans.Count - 1;
        while (lastSpannedColumn >= columnIndex && rowSpans[lastSpannedColumn] is 0)
        {
            --lastSpannedColumn;
        }

        while (columnIndex <= lastSpannedColumn)
        {
            _ = stringBuilder.Append(" | ");
            ++columnIndex;
        }

        int i = 0;
        while (i < rowSpans.Count)
        {
            if (rowSpans[i] > 0)
            {
                --rowSpans[i];
            }

            ++i;
        }

        while (rowSpans.Count > 0 && rowSpans[^1] is 0)
        {
            rowSpans.RemoveAt(rowSpans.Count - 1);
        }
    }

    private static void AppendDefinitionsFromJsonArray(StringBuilder stringBuilder, JsonElement jsonElement, Dict dict,
        ref List<ImageInfo>? imageInfos, bool isOrderedList, string? inheritedMarker, ref int orderedListIndex, ref string? lastTag,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache, List<int>? tableRowSpans, bool isTableRow, ref int tableColumnIndex)
    {
        foreach (JsonElement definitionElement in jsonElement.EnumerateArray())
        {
            if (definitionElement.ValueKind is JsonValueKind.String)
            {
                _ = stringBuilder.Append(definitionElement.GetString());
                lastTag = null;
            }
            else if (definitionElement.ValueKind is JsonValueKind.Array)
            {
                AppendDefinitionsFromJsonArray(stringBuilder, definitionElement, dict, ref imageInfos,
                    isOrderedList, inheritedMarker, ref orderedListIndex, ref lastTag, imageInfoCache,
                    tableRowSpans, isTableRow, ref tableColumnIndex);
            }
            else if (definitionElement.ValueKind is JsonValueKind.Object)
            {
                YomichanContent<string?> contentResult = GetDefinitionsFromJsonObject(
                    definitionElement, dict, ref imageInfos, imageInfoCache, tableRowSpans);
                if (isTableRow && (contentResult.Tag is "th" or "td"))
                {
                    int colSpan = definitionElement.TryGetProperty("colSpan", out JsonElement colSpanElement)
                        && colSpanElement.ValueKind is JsonValueKind.Number
                        && colSpanElement.TryGetInt32(out int colSpanValue) && colSpanValue > 0
                        ? colSpanValue
                        : 1;
                    int rowSpan = definitionElement.TryGetProperty("rowSpan", out JsonElement rowSpanElement)
                        && rowSpanElement.ValueKind is JsonValueKind.Number
                        && rowSpanElement.TryGetInt32(out int rowSpanValue) && rowSpanValue > 0
                        ? rowSpanValue
                        : 1;
                    Debug.Assert(tableRowSpans is not null);
                    AppendTableCell(stringBuilder, contentResult.Content, colSpan, rowSpan, tableRowSpans, ref tableColumnIndex);
                }
                else
                {
                    AppendDefinitionContent(stringBuilder, contentResult, ref imageInfos, isOrderedList, inheritedMarker,
                        ref orderedListIndex, ref lastTag, imageInfoCache);
                }
            }
        }
    }

    private static void AppendDefinitionContent(StringBuilder stringBuilder, YomichanContent<string?> contentResult,
        ref List<ImageInfo>? imageInfos, bool isOrderedList, string? inheritedMarker, ref int orderedListIndex, ref string? lastTag,
        ConcurrentDictionary<string, ImageInfo> imageInfoCache)
    {
        string? content = contentResult.Content;
        if (content is not null)
        {
            switch (contentResult.Tag)
            {
                case "span":
                    _ = stringBuilder.Append(content);
                    if (contentResult.AppendWhitespace)
                    {
                        _ = stringBuilder.Append(' ');
                    }
                    break;

                case "a":
                case "ruby":
                    _ = stringBuilder.Append(content);
                    break;

                case "rp":
                    // Already handled by the "rt" case.
                    break;

                case "rt":
                    _ = stringBuilder.Append('[').Append(content).Append(']');
                    break;

                case "li":
                {
                    content = content.TrimStart();
                    ++orderedListIndex;

                    string? marker = contentResult.Marker ?? inheritedMarker;
                    if (marker is "none")
                    {
                        _ = stringBuilder.Append('\n').Append(content);
                        break;
                    }

                    if (marker is not null)
                    {
                        marker = GetListMarker(marker, orderedListIndex);
                    }

                    marker ??= isOrderedList ? $"{orderedListIndex}." : "•";
                    if (marker.Length is 0)
                    {
                        _ = stringBuilder.Append('\n').Append(content);
                        break;
                    }

                    if (content.StartsWith('•') || content.StartsWith(marker, StringComparison.Ordinal))
                    {
                        _ = stringBuilder.Append('\n').Append(marker).Append('\n').Append(content);
                    }
                    else
                    {
                        _ = stringBuilder.Append('\n').Append(marker).Append(' ').Append(content);
                    }
                    break;
                }

                case "ul":
                case "ol":
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                    break;

                case "th":
                case "td":
                    _ = stringBuilder.Append(" | ").Append(content.AsSpan().TrimStart());
                    break;

                case "tr":
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart()).Append(" |");
                    break;

                case "img":
                {
                    ImageInfo? imageInfo = GetImageInfo(content, imageInfoCache);
                    if (imageInfo is not null)
                    {
                        imageInfos ??= [];
                        imageInfos.Add(imageInfo);
                    }
                    break;
                }

                case "div":
                    if (lastTag is "div" && stringBuilder.Length > 0 && stringBuilder[^1] is '\n')
                    {
                        _ = stringBuilder.Append(content.AsSpan().Trim()).Append('\n');
                    }
                    else
                    {
                        _ = stringBuilder.Append('\n').Append(content.AsSpan().Trim()).Append('\n');
                    }
                    break;

                // "summary" or "details" or "table" or "thead" or "tbody" or "tfoot"
                default:
                    _ = stringBuilder.Append('\n').Append(content.AsSpan().TrimStart());
                    break;
            }

            lastTag = contentResult.Tag;
        }
        else if (contentResult.Tag is "br")
        {
            _ = stringBuilder.Append('\n');
            lastTag = contentResult.Tag;
        }
    }

    private static YomichanContent<string?> GetDefinitionsFromJsonObject(JsonElement jsonElement, Dict dict,
        ref List<ImageInfo>? imagePaths, ConcurrentDictionary<string, ImageInfo> imageInfoCache, List<int>? tableRowSpans = null)
    {
        string? marker = jsonElement.TryGetProperty("style", out JsonElement styleElement) && styleElement.TryGetProperty("listStyleType", out JsonElement listStyleTypeElement)
            ? listStyleTypeElement.GetString()
            : null;

        if (marker is not null)
        {
            if (marker is "disc")
            {
                marker = "•";
            }
            else if (marker is "circle")
            {
                marker = "◦";
            }
            else if (marker is "square")
            {
                marker = "▪";
            }
        }

        if (jsonElement.TryGetProperty("content", out JsonElement contentElement))
        {
            string? tag = null;
            if (jsonElement.TryGetProperty("tag", out JsonElement tagElement))
            {
                tag = tagElement.GetString();
            }

            if (tableRowSpans is { Count: > 0 } && (tag is "thead" or "tbody" or "tfoot"))
            {
                // Row spans do not cross row groups.
                tableRowSpans.Clear();
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

                // Heuristic for Japanese-English dictionaries whose CSS is stored in a separate file and thus cannot be parsed currently
                bool appendWhitespace = tag is "span"
                    && (jsonElement.TryGetProperty("style", out styleElement)
                        ? styleElement.TryGetProperty("marginRight", out _)
                        : jsonElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("class", out _) && contentText.Length > 0 && char.IsAscii(contentText[0]));

                return new YomichanContent<string?>(tag, contentText, appendWhitespace, marker);
            }

            if (contentElement.ValueKind is JsonValueKind.Array)
            {
                bool emptyContentArray = tag is "th" && contentElement.GetArrayLength() is 0;
                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();

                int orderedListIndex = 0;
                string? lastTag = null;
                int tableColumnIndex = 0;
                List<int>? childTableRowSpans = null;
                if (tag is "table")
                {
                    childTableRowSpans = [];
                }
                else if (tag is "thead" or "tbody" or "tfoot" or "tr")
                {
                    childTableRowSpans = tableRowSpans;
                }
                AppendDefinitionsFromJsonArray(sb, contentElement, dict, ref imagePaths, tag is "ol",
                    marker, ref orderedListIndex, ref lastTag, imageInfoCache,
                    childTableRowSpans, tag is "tr" && childTableRowSpans is not null, ref tableColumnIndex);
                if (tag is "tr" && childTableRowSpans is not null)
                {
                    AdvanceTableRow(sb, childTableRowSpans, tableColumnIndex);
                }
                else if (tableRowSpans is { Count: > 0 } && (tag is "thead" or "tbody" or "tfoot"))
                {
                    tableRowSpans.Clear();
                }

                string? content = null;
                if (sb.Length > 0)
                {
                    content = sb.ToString();
                }

                ObjectPoolManager.StringBuilderPool.Return(sb);
                if (tag is "th" && emptyContentArray)
                {
                    content = "×";
                }

                bool appendWhitespace = tag is "span" && content is not null
                    && (jsonElement.TryGetProperty("style", out styleElement)
                        ? styleElement.TryGetProperty("marginRight", out _)
                        : jsonElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("class", out _) && content.Length > 0 && char.IsAscii(content[0]));

                return new YomichanContent<string?>(tag, content, appendWhitespace, marker);
            }

            if (contentElement.ValueKind is JsonValueKind.Object)
            {
                List<int>? childTableRowSpans = null;
                if (tag is "table")
                {
                    childTableRowSpans = [];
                }
                else if (tag is "thead" or "tbody" or "tfoot" or "tr")
                {
                    childTableRowSpans = tableRowSpans;
                }
                YomichanContent<string?> childContent = GetDefinitionsFromJsonObject(contentElement, dict, ref imagePaths,
                    imageInfoCache, childTableRowSpans);
                string? content;
                if (tag is "tr" && childTableRowSpans is not null && (childContent.Tag is "th" or "td"))
                {
                    StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                    int colSpan = contentElement.TryGetProperty("colSpan", out JsonElement colSpanElement)
                        && colSpanElement.ValueKind is JsonValueKind.Number
                        && colSpanElement.TryGetInt32(out int colSpanValue) && colSpanValue > 0
                        ? colSpanValue
                        : 1;
                    int rowSpan = contentElement.TryGetProperty("rowSpan", out JsonElement rowSpanElement)
                        && rowSpanElement.ValueKind is JsonValueKind.Number
                        && rowSpanElement.TryGetInt32(out int rowSpanValue) && rowSpanValue > 0
                        ? rowSpanValue
                        : 1;
                    int tableColumnIndex = 0;
                    AppendTableCell(sb, childContent.Content, colSpan, rowSpan, childTableRowSpans, ref tableColumnIndex);
                    AdvanceTableRow(sb, childTableRowSpans, tableColumnIndex);
                    content = sb.Length > 0 ? sb.ToString() : null;
                    ObjectPoolManager.StringBuilderPool.Return(sb);
                }
                else if (childContent.Tag is "br")
                {
                    content = "\n";
                }
                else if (childContent.Content is null || childContent.Tag is "rp")
                {
                    content = null;
                }
                else if (childContent.Tag is "span")
                {
                    content = childContent.AppendWhitespace ? childContent.Content + " " : childContent.Content;
                }
                else if (childContent.Tag is "a" or "ruby")
                {
                    content = childContent.Content;
                }
                else
                {
                    StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                    int orderedListIndex = 0;
                    string? lastTag = null;
                    AppendDefinitionContent(sb, childContent, ref imagePaths, tag is "ol", marker,
                        ref orderedListIndex, ref lastTag, imageInfoCache);
                    content = sb.Length > 0 ? sb.ToString() : null;
                    ObjectPoolManager.StringBuilderPool.Return(sb);
                }

                if (tableRowSpans is { Count: > 0 } && (tag is "thead" or "tbody" or "tfoot"))
                {
                    tableRowSpans.Clear();
                }

                bool appendWhitespace = tag is "span" && content is not null
                    && (jsonElement.TryGetProperty("style", out styleElement)
                        ? styleElement.TryGetProperty("marginRight", out _)
                        : jsonElement.TryGetProperty("data", out JsonElement dataElement) && dataElement.TryGetProperty("class", out _) && content.Length > 0 && char.IsAscii(content[0]));

                return new YomichanContent<string?>(tag, content, appendWhitespace, marker);
            }
        }
        else if (jsonElement.TryGetProperty("tag", out JsonElement tagElement))
        {
            string? tag = tagElement.GetString();
            if (tableRowSpans is { Count: > 0 } && (tag is "thead" or "tbody" or "tfoot"))
            {
                tableRowSpans.Clear();
            }

            if (tag is "tr" && tableRowSpans is { Count: > 0 })
            {
                StringBuilder sb = ObjectPoolManager.StringBuilderPool.Get();
                AdvanceTableRow(sb, tableRowSpans, 0);
                string content = sb.ToString();
                ObjectPoolManager.StringBuilderPool.Return(sb);
                return new YomichanContent<string?>(tag, content, false, null);
            }

            if (tag is "th")
            {
                return new YomichanContent<string?>("th", "×", false, null);
            }

            if (tag is "td" && tableRowSpans is not null)
            {
                return new YomichanContent<string?>("td", null, false, null);
            }

            if (tag is "br")
            {
                return new YomichanContent<string?>("br", null, false, null);
            }

            if (tag is "img" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
            {
                if (IsSmallImage(jsonElement))
                {
                    //if (jsonElement.TryGetProperty("alt", out JsonElement altElement))
                    //{
                    //    string? altText = altElement.GetString();
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
                string fullImagePath = PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath));
                return IsSmallImageWithMissingDimension(jsonElement, fullImagePath, imageInfoCache)
                    ? default
                    : new YomichanContent<string?>("img", fullImagePath, false, null);
            }

            if (jsonElement.TryGetProperty("title", out JsonElement titleJsonElement))
            {
                return new YomichanContent<string?>(tag, titleJsonElement.GetString(), false, null);
            }
        }
        else if (jsonElement.TryGetProperty("type", out JsonElement typeJsonElement))
        {
            string? type = typeJsonElement.GetString();
            if (type is "text" && jsonElement.TryGetProperty("text", out JsonElement textElement))
            {
                return new YomichanContent<string?>("span", textElement.GetString(), false, null);
            }

            if (type is "image" && jsonElement.TryGetProperty("path", out JsonElement imagePathJsonElement))
            {
                if (IsSmallImage(jsonElement))
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
                string fullImagePath = PathUtils.GetPortablePath(Path.Join(dict.Path, imagePath));
                return IsSmallImageWithMissingDimension(jsonElement, fullImagePath, imageInfoCache)
                    ? default
                    : new YomichanContent<string?>("img", fullImagePath, false, null);
            }
        }

        return default;
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
