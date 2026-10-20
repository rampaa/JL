using System.Buffers.Binary;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Security.Cryptography;
using JL.Core.Utilities;

namespace JL.Core.Deconjugation;

internal static class DeconjugationRulesBinarySerializer
{
    // Change this when the binary layout or rule compilation changes.
    private const int Version = 2;
    private const int SourceHashOffset = sizeof(int);
    private const int PayloadHashOffset = SourceHashOffset + SHA256.HashSizeInBytes;
    private const int HeaderLength = PayloadHashOffset + SHA256.HashSizeInBytes;

    internal static async Task<DeconjugationRules?> Deserialize(string path, byte[] sourceHash)
    {
        DeleteTemporaryFile(path);
        if (!File.Exists(path))
        {
            return null;
        }

        try
        {
            byte[] data = await File.ReadAllBytesAsync(path).ConfigureAwait(false);
            return Deserialize(data, sourceHash);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LoggerManager.Logger.Debug(ex, "Couldn't read deconjugation rules from '{Path}'", path);
            return null;
        }
    }

    internal static DeconjugationRules? Deserialize(ReadOnlySpan<byte> data, ReadOnlySpan<byte> sourceHash)
    {
        if (data.Length < HeaderLength || sourceHash.Length != SHA256.HashSizeInBytes
            || BinaryPrimitives.ReadInt32LittleEndian(data) != Version
            || !sourceHash.SequenceEqual(data.Slice(SourceHashOffset, SHA256.HashSizeInBytes)))
        {
            return null;
        }

        ReadOnlySpan<byte> payload = data[HeaderLength..];
        Span<byte> payloadHash = stackalloc byte[SHA256.HashSizeInBytes];
        _ = SHA256.HashData(payload, payloadHash);
        if (!payloadHash.SequenceEqual(data.Slice(PayloadHashOffset, SHA256.HashSizeInBytes)))
        {
            return null;
        }

        DeconjugationRulesBinaryReader reader = new(payload);
        if (!reader.TryReadCount(sizeof(int), out int stringCount))
        {
            return null;
        }

        string[] strings = new string[stringCount];
        for (int i = 0; i < strings.Length; ++i)
        {
            if (!reader.TryReadString(out strings[i]))
            {
                return null;
            }
        }

        const int ruleLength = 1 + (sizeof(int) * 5);
        if (!reader.TryReadCount(ruleLength, out int ruleCount))
        {
            return null;
        }

        VirtualRule[] rules = new VirtualRule[ruleCount];
        for (int i = 0; i < rules.Length; ++i)
        {
            if (!reader.TryReadRuleType(out RuleType type)
                || !reader.TryReadIndex(strings.Length, out int decEnd)
                || !reader.TryReadIndex(strings.Length, out int conEnd)
                || !reader.TryReadIndex(strings.Length, out int decTag)
                || !reader.TryReadIndex(strings.Length, out int conTag)
                || !reader.TryReadIndex(strings.Length, out int detail))
            {
                return null;
            }

            rules[i] = new VirtualRule(type, strings[decEnd], strings[conEnd], strings[decTag], strings[conTag], strings[detail]);
        }

        if (!reader.TryReadCount(sizeof(int), out int ruleArrayCount))
        {
            return null;
        }

        VirtualRule[][] ruleArrays = new VirtualRule[ruleArrayCount][];
        int[] maximumConEndLengths = new int[ruleArrayCount];
        bool[] hasEmptyConEnds = new bool[ruleArrayCount];
        for (int i = 0; i < ruleArrays.Length; ++i)
        {
            if (!reader.TryReadCount(sizeof(int), out int count))
            {
                return null;
            }

            VirtualRule[] array = new VirtualRule[count];
            int maximumConEndLength = 0;
            bool hasEmptyConEnd = false;
            for (int j = 0; j < array.Length; ++j)
            {
                if (!reader.TryReadIndex(rules.Length, out int index))
                {
                    return null;
                }

                array[j] = rules[index];
                int conEndLength = rules[index].ConEnd.Length;
                maximumConEndLength = Math.Max(maximumConEndLength, conEndLength);
                hasEmptyConEnd |= conEndLength is 0;
            }

            if (hasEmptyConEnd && maximumConEndLength > 0)
            {
                return null;
            }

            ruleArrays[i] = array;
            maximumConEndLengths[i] = maximumConEndLength;
            hasEmptyConEnds[i] = hasEmptyConEnd;
        }

        if (!reader.TryReadCount(sizeof(ushort) * 2, out int characterCount) || characterCount > ushort.MaxValue)
        {
            return null;
        }

        ushort[] characterCodes = new ushort[char.MaxValue + 1];
        bool[] usedCharacterCodes = new bool[characterCount + 1];
        for (int i = 0; i < characterCount; ++i)
        {
            if (!reader.TryReadUInt16(out ushort character) || !reader.TryReadUInt16(out ushort code)
                || code is 0 || code > characterCount || characterCodes[character] is not 0 || usedCharacterCodes[code])
            {
                return null;
            }

            characterCodes[character] = code;
            usedCharacterCodes[code] = true;
        }

        if (!reader.TryReadInt32(out int characterCodeCount) || characterCodeCount != characterCount + 1
            || !reader.TryReadCount(sizeof(int) * 4, out int matcherCount) || matcherCount is 0)
        {
            return null;
        }

        RuleSuffixMatcher? initialMatcher = null;
        Dictionary<string, RuleSuffixMatcher> matchersByConTag = new(matcherCount - 1, StringComparer.Ordinal);
        for (int i = 0; i < matcherCount; ++i)
        {
            string tag = "";
            if (i > 0)
            {
                if (!reader.TryReadIndex(strings.Length, out int tagIndex))
                {
                    return null;
                }

                tag = strings[tagIndex];
            }

            if (!reader.TryReadCount(sizeof(int), out int nodeCount) || nodeCount is 0
                || nodeCount > Array.MaxLength / characterCodeCount
                || !reader.TryReadCount(sizeof(int) * 2, out int transitionCount) || transitionCount != nodeCount - 1)
            {
                return null;
            }

            int[] nextNodeIndices = new int[nodeCount * characterCodeCount];
            int[] suffixLengthsByNodeIndex = new int[nodeCount];
            int previousPosition = -1;
            // Transitions are stored in table order, and each node is created after its parent.
            for (int j = 0; j < transitionCount; ++j)
            {
                if (!reader.TryReadIndex(nextNodeIndices.Length, out int position)
                    || !reader.TryReadIndex(nodeCount, out int nextNodeIndex)
                    || position <= previousPosition || position % characterCodeCount is 0
                    || nextNodeIndex <= position / characterCodeCount || suffixLengthsByNodeIndex[nextNodeIndex] is not 0)
                {
                    return null;
                }

                int parentNodeIndex = position / characterCodeCount;
                if (parentNodeIndex > 0 && suffixLengthsByNodeIndex[parentNodeIndex] is 0)
                {
                    return null;
                }

                nextNodeIndices[position] = nextNodeIndex;
                suffixLengthsByNodeIndex[nextNodeIndex] = suffixLengthsByNodeIndex[parentNodeIndex] + 1;
                previousPosition = position;
            }

            VirtualRule[][] matchingRulesByNodeIndex = new VirtualRule[nodeCount][];
            for (int j = 0; j < nodeCount; ++j)
            {
                if (!reader.TryReadIndex(ruleArrays.Length, out int arrayIndex)
                    || hasEmptyConEnds[arrayIndex] || maximumConEndLengths[arrayIndex] > suffixLengthsByNodeIndex[j])
                {
                    return null;
                }

                matchingRulesByNodeIndex[j] = ruleArrays[arrayIndex];
            }

            if (!reader.TryReadIndex(ruleArrays.Length, out int emptyEndingRulesIndex)
                || maximumConEndLengths[emptyEndingRulesIndex] > 0 || matchingRulesByNodeIndex[0].Length is not 0)
            {
                return null;
            }

            RuleSuffixMatcher matcher = new(characterCodes, characterCodeCount, nextNodeIndices, matchingRulesByNodeIndex, ruleArrays[emptyEndingRulesIndex]);
            if (i is 0)
            {
                initialMatcher = matcher;
            }
            else if (!matchersByConTag.TryAdd(tag, matcher))
            {
                return null;
            }
        }

        Debug.Assert(initialMatcher is not null);
        return reader.IsComplete ? new DeconjugationRules(initialMatcher, matchersByConTag.ToFrozenDictionary(StringComparer.Ordinal)) : null;
    }

    internal static void Serialize(string path, DeconjugationRules rules, ReadOnlySpan<byte> sourceHash)
    {
        string temporaryPath = PathUtils.GetTempPath(path);
        bool temporaryFileCreated = false;
        try
        {
            using MemoryStream payload = new();
            using (BinaryWriter writer = new(payload, TextUtils.Utf8NoBom, true))
            {
                WriteTables(writer, rules);
            }

            ReadOnlySpan<byte> data = payload.GetBuffer().AsSpan(0, (int)payload.Length);
            using (FileStream file = new(temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                temporaryFileCreated = true;
                using BinaryWriter writer = new(file, TextUtils.Utf8NoBom);
                writer.Write(Version);
                writer.Write(sourceHash);
                Span<byte> payloadHash = stackalloc byte[SHA256.HashSizeInBytes];
                _ = SHA256.HashData(data, payloadHash);
                writer.Write(payloadHash);
                writer.Write(data);
            }

            PathUtils.ReplaceFileAtomicallyOnSameVolume(path, temporaryPath);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LoggerManager.Logger.Debug(ex, "Couldn't save deconjugation rules to '{Path}'", path);
        }
        finally
        {
            if (temporaryFileCreated)
            {
                DeleteTemporaryFile(path);
            }
        }
    }

    private static void DeleteTemporaryFile(string path)
    {
        try
        {
            File.Delete(PathUtils.GetTempPath(path));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            LoggerManager.Logger.Debug(ex, "Couldn't remove the temporary deconjugation rules file for '{Path}'", path);
        }
    }

    private static void WriteTables(BinaryWriter writer, DeconjugationRules rules)
    {
        List<RuleSuffixMatcher> matchers = new(rules.MatchersByConTag.Count + 1) { rules.InitialMatcher };
        string[] tags = rules.MatchersByConTag.Keys.Order(StringComparer.Ordinal).ToArray();
        foreach (string tag in tags)
        {
            matchers.Add(rules.MatchersByConTag[tag]);
        }

        List<VirtualRule[]> ruleArrays = [];
        Dictionary<VirtualRule[], int> ruleArrayIds = new(ReferenceEqualityComparer.Instance);
        Dictionary<VirtualRule, int> ruleIds = [];
        Dictionary<string, int> stringIds = new(StringComparer.Ordinal);
        foreach (RuleSuffixMatcher matcher in matchers.AsReadOnlySpan())
        {
            foreach (VirtualRule[] array in matcher.MatchingRulesByNodeIndex)
            {
                AddRuleArray(array, ruleArrays, ruleArrayIds, ruleIds, stringIds);
            }

            AddRuleArray(matcher.EmptyEndingRules, ruleArrays, ruleArrayIds, ruleIds, stringIds);
        }

        foreach (string tag in tags)
        {
            _ = stringIds.TryAdd(tag, stringIds.Count);
        }

        string[] strings = new string[stringIds.Count];
        int maximumStringLength = 0;
        foreach ((string value, int index) in stringIds)
        {
            strings[index] = value;
            maximumStringLength = Math.Max(maximumStringLength, value.Length);
        }

        writer.Write(strings.Length);
        byte[] utf8Buffer = strings.Length is 0 ? [] : new byte[TextUtils.Utf8NoBom.GetMaxByteCount(maximumStringLength)];
        foreach (string value in strings)
        {
            int byteCount = TextUtils.Utf8NoBom.GetBytes(value, utf8Buffer);
            writer.Write(byteCount);
            writer.Write(utf8Buffer.AsSpan(0, byteCount));
        }

        VirtualRule[] uniqueRules = new VirtualRule[ruleIds.Count];
        foreach ((VirtualRule rule, int index) in ruleIds)
        {
            uniqueRules[index] = rule;
        }

        writer.Write(uniqueRules.Length);
        foreach (ref readonly VirtualRule rule in uniqueRules.AsSpan())
        {
            writer.Write((byte)rule.Type);
            writer.Write(stringIds[rule.DecEnd]);
            writer.Write(stringIds[rule.ConEnd]);
            writer.Write(stringIds[rule.DecTag]);
            writer.Write(stringIds[rule.ConTag]);
            writer.Write(stringIds[rule.Detail]);
        }

        writer.Write(ruleArrays.Count);
        foreach (VirtualRule[] array in ruleArrays.AsReadOnlySpan())
        {
            writer.Write(array.Length);
            foreach (ref readonly VirtualRule rule in array.AsSpan())
            {
                writer.Write(ruleIds[rule]);
            }
        }

        ushort[] characterCodes = rules.InitialMatcher.CharacterCodes;
        writer.Write(rules.InitialMatcher.CharacterCodeCount - 1);
        for (int character = 0; character < characterCodes.Length; ++character)
        {
            if (characterCodes[character] is not 0)
            {
                writer.Write((ushort)character);
                writer.Write(characterCodes[character]);
            }
        }

        writer.Write(rules.InitialMatcher.CharacterCodeCount);
        writer.Write(matchers.Count);
        for (int i = 0; i < matchers.Count; ++i)
        {
            RuleSuffixMatcher matcher = matchers[i];
            if (i > 0)
            {
                writer.Write(stringIds[tags[i - 1]]);
            }

            writer.Write(matcher.MatchingRulesByNodeIndex.Length);
            // Every node except the root has exactly one incoming transition.
            writer.Write(matcher.MatchingRulesByNodeIndex.Length - 1);
            int[] nextNodeIndices = matcher.NextNodeIndices;
            for (int position = 0; position < nextNodeIndices.Length; ++position)
            {
                if (nextNodeIndices[position] is not 0)
                {
                    writer.Write(position);
                    writer.Write(nextNodeIndices[position]);
                }
            }

            foreach (VirtualRule[] array in matcher.MatchingRulesByNodeIndex)
            {
                writer.Write(ruleArrayIds[array]);
            }

            writer.Write(ruleArrayIds[matcher.EmptyEndingRules]);
        }
    }

    private static void AddRuleArray(VirtualRule[] array, List<VirtualRule[]> ruleArrays, Dictionary<VirtualRule[], int> ruleArrayIds,
        Dictionary<VirtualRule, int> ruleIds, Dictionary<string, int> stringIds)
    {
        if (!ruleArrayIds.TryAdd(array, ruleArrays.Count))
        {
            return;
        }

        ruleArrays.Add(array);
        foreach (ref readonly VirtualRule rule in array.AsSpan())
        {
            if (!ruleIds.TryAdd(rule, ruleIds.Count))
            {
                continue;
            }

            _ = stringIds.TryAdd(rule.DecEnd, stringIds.Count);
            _ = stringIds.TryAdd(rule.ConEnd, stringIds.Count);
            _ = stringIds.TryAdd(rule.DecTag, stringIds.Count);
            _ = stringIds.TryAdd(rule.ConTag, stringIds.Count);
            _ = stringIds.TryAdd(rule.Detail, stringIds.Count);
        }
    }
}
