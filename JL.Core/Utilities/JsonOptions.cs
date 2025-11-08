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

    public static readonly JsonSerializerOptions Jso64KBuffer = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultBufferSize = 1024 * 64
    };

    internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNull = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNull64KBuffer = new()
    {
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        DefaultBufferSize = 1024 * 64
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

    //internal static readonly JsonSerializerOptions s_jsoWithEnumConverter64KBuffer = new()
    //{
    //    RespectNullableAnnotations = true,
    //    RespectRequiredConstructorParameters = true,
    //    DefaultBufferSize = 1024 * 64,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //    Converters =
    //    {
    //        new JsonStringEnumConverter()
    //    }
    //};

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

    //internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNullWithEnumConverter64K = new()
    //{
    //    RespectNullableAnnotations = true,
    //    RespectRequiredConstructorParameters = true,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //    DefaultBufferSize = 64 * 1024,
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

    //internal static readonly JsonSerializerOptions s_jsoWithEnumConverterAndIndentation64KBuffer = new()
    //{
    //    RespectNullableAnnotations = true,
    //    RespectRequiredConstructorParameters = true,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //    WriteIndented = true,
    //    DefaultBufferSize = 64 * 1024,
    //    Converters =
    //    {
    //        new JsonStringEnumConverter()
    //    }
    //};

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

    //internal static readonly JsonSerializerOptions s_jsoIgnoringWhenWritingNullWithEnumConverterAndIndentation64KBuffer = new()
    //{
    //    RespectNullableAnnotations = true,
    //    RespectRequiredConstructorParameters = true,
    //    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    //    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    //    DefaultBufferSize = 64 * 1024,
    //    WriteIndented = true,
    //    Converters =
    //    {
    //        new JsonStringEnumConverter()
    //    }
    //};
}
