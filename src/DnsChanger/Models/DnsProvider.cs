using System.Text.Json.Serialization;

namespace DnsChanger.Models;

/// <summary>
/// A named DNS configuration (a primary and an optional secondary server).
/// </summary>
public class DnsProvider
{
    public string Name { get; set; } = string.Empty;

    public string Primary { get; set; } = string.Empty;

    public string Secondary { get; set; } = string.Empty;

    /// <summary>True for user-added entries, false for the built-in presets.</summary>
    public bool IsCustom { get; set; }

    /// <summary>Optional short note shown in the UI (e.g. "Ad blocking").</summary>
    public string Note { get; set; } = string.Empty;

    [JsonIgnore]
    public string DisplayServers =>
        string.IsNullOrWhiteSpace(Secondary) ? Primary : $"{Primary}  •  {Secondary}";

    public DnsProvider Clone() => new()
    {
        Name = Name,
        Primary = Primary,
        Secondary = Secondary,
        IsCustom = IsCustom,
        Note = Note,
    };
}
