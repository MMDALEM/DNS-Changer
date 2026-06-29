using System.IO;
using System.Text.Json;
using DnsChanger.Models;

namespace DnsChanger.Services;

/// <summary>
/// Loads and saves the list of DNS providers to %AppData%\DnsChanger\providers.json.
/// On first run it seeds the file with the built-in presets.
/// </summary>
public static class StorageService
{
    private static readonly string Folder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DnsChanger");

    private static readonly string FilePath = Path.Combine(Folder, "providers.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static List<DnsProvider> Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                var json = File.ReadAllText(FilePath);
                var list = JsonSerializer.Deserialize<List<DnsProvider>>(json);
                if (list is { Count: > 0 })
                    return MergeWithDefaults(list);
            }
        }
        catch
        {
            // Corrupt or unreadable file – fall back to defaults below.
        }

        var defaults = Defaults();
        Save(defaults);
        return defaults;
    }

    public static void Save(List<DnsProvider> providers)
    {
        try
        {
            Directory.CreateDirectory(Folder);
            var json = JsonSerializer.Serialize(providers, JsonOptions);
            File.WriteAllText(FilePath, json);
        }
        catch
        {
            // Saving is best-effort; ignore IO errors so the app keeps working.
        }
    }

    /// <summary>
    /// Keeps the user's custom entries and refreshes the built-in presets so new
    /// app versions can ship additional well-known providers.
    /// </summary>
    private static List<DnsProvider> MergeWithDefaults(List<DnsProvider> saved)
    {
        var custom = saved.Where(p => p.IsCustom).ToList();
        var merged = Defaults();
        merged.AddRange(custom);
        return merged;
    }

    /// <summary>The well-known DNS providers shipped with the app.</summary>
    public static List<DnsProvider> Defaults() =>
    [
        // ---- International ----
        new() { Name = "Cloudflare",                Primary = "1.1.1.1",        Secondary = "1.0.0.1",          Note = "Fast & private" },
        new() { Name = "Cloudflare (Malware)",      Primary = "1.1.1.2",        Secondary = "1.0.0.2",          Note = "Blocks malware" },
        new() { Name = "Cloudflare (Family)",       Primary = "1.1.1.3",        Secondary = "1.0.0.3",          Note = "Malware + adult" },
        new() { Name = "Google Public DNS",         Primary = "8.8.8.8",        Secondary = "8.8.4.4",          Note = "Reliable" },
        new() { Name = "Quad9",                     Primary = "9.9.9.9",        Secondary = "149.112.112.112",  Note = "Security focused" },
        new() { Name = "OpenDNS",                   Primary = "208.67.222.222", Secondary = "208.67.220.220",   Note = "Cisco" },
        new() { Name = "OpenDNS Family Shield",     Primary = "208.67.222.123", Secondary = "208.67.220.123",   Note = "Adult content filter" },
        new() { Name = "AdGuard DNS",               Primary = "94.140.14.14",   Secondary = "94.140.15.15",     Note = "Ad blocking" },
        new() { Name = "AdGuard Family",            Primary = "94.140.14.15",   Secondary = "94.140.15.16",     Note = "Ads + adult filter" },
        new() { Name = "CleanBrowsing",             Primary = "185.228.168.9",  Secondary = "185.228.169.9",    Note = "Security filter" },
        new() { Name = "Comodo Secure DNS",         Primary = "8.26.56.26",     Secondary = "8.20.247.20" },
        new() { Name = "Level3",                    Primary = "4.2.2.4",        Secondary = "4.2.2.2" },
        new() { Name = "DNS.WATCH",                 Primary = "84.200.69.80",   Secondary = "84.200.70.40",     Note = "No logging" },
        new() { Name = "Yandex DNS",                Primary = "77.88.8.8",      Secondary = "77.88.8.1" },
        new() { Name = "Verisign",                  Primary = "64.6.64.6",      Secondary = "64.6.65.6" },

        // ---- Iran (anti-sanction / local) ----
        new() { Name = "Shecan",                    Primary = "178.22.122.100", Secondary = "185.51.200.2",     Note = "Iran • anti-sanction" },
        new() { Name = "403.online",               Primary = "10.202.10.202",  Secondary = "10.202.10.102",    Note = "Iran • anti-sanction" },
        new() { Name = "Electro",                   Primary = "78.157.42.100",  Secondary = "78.157.42.101",    Note = "Iran • anti-sanction" },
        new() { Name = "Begzar",                    Primary = "185.55.226.26",  Secondary = "185.55.225.25",    Note = "Iran • anti-sanction" },
        new() { Name = "RadarGame",                 Primary = "10.202.10.10",   Secondary = "10.202.10.11",     Note = "Iran • gaming" },
        new() { Name = "Shelter",                   Primary = "94.103.125.157", Secondary = "94.103.125.158",   Note = "Iran • anti-sanction" },
        new() { Name = "Pishgaman",                 Primary = "5.202.100.100",  Secondary = "5.202.100.101",    Note = "Iran" },
    ];
}
