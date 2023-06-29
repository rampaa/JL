using System.ComponentModel;

namespace JL.Core.Audio;

public enum AudioSourceType
{
    [Description("Local Path")] LocalPath,
    [Description("URL")] Url,
    [Description("URL (JSON)")] UrlJson
}
