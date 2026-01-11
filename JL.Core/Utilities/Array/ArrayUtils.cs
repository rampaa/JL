namespace JL.Core.Utilities.Array;

internal static class ArrayUtils
{
    internal static T[]? ConcatNullableArrays<T>(params ReadOnlySpan<T[]?> arrays)
    {
        int position = 0;
        int length = 0;

        foreach (ref readonly T[]? array in arrays)
        {
            if (array is not null)
            {
                length += array.Length;
            }
        }

        if (length is 0)
        {
            return null;
        }

        T[] concatArray = new T[length];
        foreach (ref readonly T[]? array in arrays)
        {
            if (array is not null)
            {
                array.CopyTo(concatArray.AsSpan(position, array.Length));
                position += array.Length;
            }
        }

        return concatArray;
    }
}
