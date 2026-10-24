using System.Diagnostics.CodeAnalysis;

namespace JL.Core.Deconjugation;

internal readonly record struct Form(string Text, string LastTag, ProcessNode? Process);
