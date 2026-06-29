using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;

namespace DnsChanger.Services;

/// <summary>Information about a network adapter that can have its DNS changed.</summary>
public class NetworkAdapter
{
    public string Name { get; init; } = string.Empty;          // friendly name used by netsh
    public string Description { get; init; } = string.Empty;
    public string CurrentDns { get; init; } = string.Empty;

    public string Display => string.IsNullOrWhiteSpace(Description)
        ? Name
        : $"{Name}  ({Description})";
}

public record DnsResult(bool Ok, string Message);

/// <summary>
/// Reads and changes the DNS servers of Windows network adapters using netsh.
/// Requires the process to run elevated (see app.manifest).
/// </summary>
public static class DnsService
{
    /// <summary>Returns the active (connected) IPv4 adapters.</summary>
    public static List<NetworkAdapter> GetActiveAdapters()
    {
        var result = new List<NetworkAdapter>();

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                continue;

            var props = ni.GetIPProperties();

            // Only list adapters that actually have an IPv4 configuration.
            bool hasIpv4 = props.UnicastAddresses
                .Any(a => a.Address.AddressFamily == AddressFamily.InterNetwork);
            if (!hasIpv4) continue;

            var dns = props.DnsAddresses
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .ToList();

            result.Add(new NetworkAdapter
            {
                Name = ni.Name,
                Description = ni.Description,
                CurrentDns = dns.Count == 0 ? "Automatic (DHCP)" : string.Join(", ", dns),
            });
        }

        return result;
    }

    /// <summary>Sets static DNS servers on the given adapter.</summary>
    public static DnsResult SetDns(string adapter, string primary, string secondary)
    {
        if (string.IsNullOrWhiteSpace(primary))
            return new DnsResult(false, "Primary DNS address is empty.");

        var log = new StringBuilder();
        bool ok = true;

        var p = RunNetsh($"interface ipv4 set dnsservers name=\"{adapter}\" source=static address={primary} register=primary validate=no", log);
        ok &= p;

        if (ok && !string.IsNullOrWhiteSpace(secondary))
        {
            var s = RunNetsh($"interface ipv4 add dnsservers name=\"{adapter}\" address={secondary} index=2 validate=no", log);
            ok &= s;
        }

        FlushDns(log);

        return new DnsResult(ok,
            ok ? $"DNS applied to \"{adapter}\"." : $"Failed to apply DNS.\n{log}");
    }

    /// <summary>Resets the adapter back to automatic (DHCP) DNS.</summary>
    public static DnsResult ClearDns(string adapter)
    {
        var log = new StringBuilder();
        var ok = RunNetsh($"interface ipv4 set dnsservers name=\"{adapter}\" source=dhcp", log);
        FlushDns(log);

        return new DnsResult(ok,
            ok ? $"\"{adapter}\" reset to automatic (DHCP)." : $"Failed to reset DNS.\n{log}");
    }

    private static void FlushDns(StringBuilder log) => Run("ipconfig", "/flushdns", log);

    private static bool RunNetsh(string args, StringBuilder log) => Run("netsh", args, log);

    private static bool Run(string fileName, string arguments, StringBuilder log)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc is null)
            {
                log.AppendLine($"Could not start {fileName}.");
                return false;
            }

            string stdout = proc.StandardOutput.ReadToEnd();
            string stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (!string.IsNullOrWhiteSpace(stdout)) log.AppendLine(stdout.Trim());
            if (!string.IsNullOrWhiteSpace(stderr)) log.AppendLine(stderr.Trim());

            return proc.ExitCode == 0;
        }
        catch (Exception ex)
        {
            log.AppendLine(ex.Message);
            return false;
        }
    }
}
