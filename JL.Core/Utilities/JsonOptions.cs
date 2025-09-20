using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace JL.Core.Utilities;

public static class JsonOptions
{
    public static readonly JsonSerializerOptions DefaultJso = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNull = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    internal static readonly JsonSerializerOptions s_jsoWithEnumConverter = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    //internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNullWithEnumConverter = new()
    //{
    //    RespectNullableAnnotations = true,
    //    RespectRequiredConstructorParameters = true,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    //    Converters =
    //    {
    //        new JsonStringEnumConverter()
    //    }
    //};

    internal static readonly JsonSerializerOptions s_jsoWithEnumConverterAndIndentation = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };
}
