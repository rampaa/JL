namespace JL.Core.Dicts.EPWING;

public interface IEpwingResult : IResult
{
    public string PrimarySpelling { get; }
    public string? Reading { get; }
    public List<string>? Definitions { get; set; }
}
