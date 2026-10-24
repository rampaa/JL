using System.Runtime.InteropServices;
using JL.Core.Utilities;
using JL.Windows.Interop;
using JL.Windows.SpeechSynthesis.Interop;

namespace JL.Windows.SpeechSynthesis;

internal sealed class AudioResampler : IDisposable
{
    private const int NeedMoreInput = unchecked((int)0xC00D6D72);
    private const uint ProvidesSamples = 0x100;
    private const uint OutputBufferSizeBytes = 65536;
    private const uint BeginStreaming = 0x10000000;
    private const uint StartOfStream = 0x10000003;
    private const uint EndOfStream = 0x10000002;
    private const uint Flush = 0;
    private const uint DrainBufferedAudio = 1;
    private static readonly Guid s_transformInterface = new("BF94C121-5B05-4E6F-8000-BA598961414D");
    private static readonly Type? s_resamplerType = Type.GetTypeFromCLSID(new Guid("F447B69E-1884-4A7E-8055-346F74D6EDB3"));
    private readonly nint _transform;
    private readonly uint _bufferSizeBytes;
    private readonly uint _bufferAlignmentMask;
    private readonly bool _providesSamples;
    private bool _disposed;

    private AudioResampler(nint transform, TransformOutputInfo info)
    {
        _transform = transform;
        _bufferSizeBytes = Math.Max(OutputBufferSizeBytes, info.BufferSizeBytes);
        _bufferAlignmentMask = info.BufferAlignmentBytes > 0 ? info.BufferAlignmentBytes - 1 : 0;
        _providesSamples = (info.Flags & ProvidesSamples) is not 0;
    }

    public static AudioResampler? Create(nint inputFormat, nint outputFormat)
    {
        if (s_resamplerType is null)
        {
            LoggerManager.Logger.Error("The Windows audio resampler is unavailable");
            return null;
        }

        object? instance = null;
        nint unknown = 0;
        nint transform = 0;
        try
        {
            instance = Activator.CreateInstance(s_resamplerType);
            if (instance is null)
            {
                LoggerManager.Logger.Error("Couldn't create the Windows audio resampler");
                return null;
            }

            unknown = Marshal.GetIUnknownForObject(instance);
            if (!MediaFoundation.CheckResult(Marshal.QueryInterface(unknown, in s_transformInterface, out transform), "Accessing the audio resampler")
                || !MediaFoundation.SetTransformType(transform, inputFormat, input: true)
                || !MediaFoundation.SetTransformType(transform, outputFormat, input: false)
                || !MediaFoundation.TryGetTransformOutputInfo(transform, out TransformOutputInfo info)
                || !MediaFoundation.SendTransformMessage(transform, BeginStreaming))
            {
                return null;
            }

            AudioResampler resampler = new(transform, info);
            transform = 0;
            return resampler;
        }
        catch (Exception ex)
        {
            LoggerManager.Logger.Error(ex, "Couldn't create the Windows audio resampler");
            return null;
        }
        finally
        {
            if (transform is not 0)
            {
                _ = Marshal.Release(transform);
            }

            if (unknown is not 0)
            {
                _ = Marshal.Release(unknown);
            }

            if (instance is not null)
            {
                _ = Marshal.ReleaseComObject(instance);
            }
        }
    }

    public bool SubmitInputSample(nint sample) => !_disposed && MediaFoundation.ProcessTransformInput(_transform, sample);

    public bool BeginInput()
    {
        return !_disposed
            && MediaFoundation.SendTransformMessage(_transform, Flush)
            && MediaFoundation.SendTransformMessage(_transform, StartOfStream);
    }

    public bool CompleteInput()
    {
        return !_disposed
            && MediaFoundation.SendTransformMessage(_transform, EndOfStream)
            && MediaFoundation.SendTransformMessage(_transform, DrainBufferedAudio);
    }

    public bool TryReadOutputSample(out nint readySample)
    {
        readySample = 0;
        if (_disposed)
        {
            LoggerManager.Logger.Error("Cannot read a disposed audio resampler");
            return false;
        }

        nint sample = 0;
        TransformOutput output = default;
        try
        {
            if (!_providesSamples)
            {
                if (!MediaFoundation.CheckResult(WinApi.NativeMethods.MFCreateSample(out sample), "Creating a resampled audio chunk"))
                {
                    return false;
                }

                int bufferResult = _bufferAlignmentMask is 0
                    ? WinApi.NativeMethods.MFCreateMemoryBuffer(_bufferSizeBytes, out nint buffer)
                    : WinApi.NativeMethods.MFCreateAlignedMemoryBuffer(_bufferSizeBytes, _bufferAlignmentMask, out buffer);
                if (!MediaFoundation.CheckResult(bufferResult, "Creating a resampled audio buffer"))
                {
                    return false;
                }

                try
                {
                    if (!MediaFoundation.AttachBufferToAudioChunk(sample, buffer))
                    {
                        return false;
                    }
                }
                finally
                {
                    _ = Marshal.Release(buffer);
                }
            }

            output = new TransformOutput(sample);
            int result = MediaFoundation.ProcessTransformOutput(_transform, ref output);
            if (result == NeedMoreInput)
            {
                return true;
            }

            if (!MediaFoundation.CheckResult(result, "Reading resampled audio"))
            {
                return false;
            }

            readySample = output.Sample;
            return true;
        }
        finally
        {
            if (output.Events is not 0)
            {
                _ = Marshal.Release(output.Events);
            }

            if (output.Sample is not 0 && output.Sample != sample && output.Sample != readySample)
            {
                _ = Marshal.Release(output.Sample);
            }

            if (sample is not 0 && sample != readySample)
            {
                _ = Marshal.Release(sample);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _ = Marshal.Release(_transform);
    }
}
