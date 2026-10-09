using System.Security.Cryptography;

namespace JL.Core.Utilities;

internal static class HashUtils
{
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
    internal static bool HasMd5Hash(ReadOnlySpan<byte> bytes, ReadOnlySpan<byte> expectedHash)
    {
        Span<byte> hash = stackalloc byte[MD5.HashSizeInBytes];
        _ = MD5.HashData(bytes, hash);
        return hash.SequenceEqual(expectedHash);
    }
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
}
