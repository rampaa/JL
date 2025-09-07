using System.Security.Cryptography;

namespace JL.Core.Utilities;
internal static class HashUtils
{
#pragma warning disable CA5351 // Do Not Use Broken Cryptographic Algorithms
    internal static string GetMd5String(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToHexString(MD5.HashData(bytes).AsReadOnlySpan());
    }
#pragma warning restore CA5351 // Do Not Use Broken Cryptographic Algorithms
}
