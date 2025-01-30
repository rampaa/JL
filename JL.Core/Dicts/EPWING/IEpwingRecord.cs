using JL.Core.Dicts.Interfaces;

namespace JL.Core.Dicts.EPWING;

internal interface IEpwingRecord : IDictRecordWithSingleReading
{
    public string[] Definitions { get; }
}
