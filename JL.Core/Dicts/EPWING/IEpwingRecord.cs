using JL.Core.Dicts.Options;

namespace JL.Core.Dicts.EPWING;

public interface IEpwingRecord : IDictRecord
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public List<string>? Definitions { get; set; }

    string? BuildFormattedDefinition(DictOptions? options);
}
