using System.Management;
using System.Security.Cryptography;
using System.Text;

namespace Avryd.Core.Auth;

public static class HardwareId
{
    public static string Get()
    {
        try
        {
            var parts = new List<string>();

            // CPU ID
            using (var searcher = new ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor"))
            {
                foreach (ManagementObject obj in searcher.Get())
                    parts.Add(obj["ProcessorId"]?.ToString() ?? "");
            }

            // Motherboard serial
            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BaseBoard"))
            {
                foreach (ManagementObject obj in searcher.Get())
                    parts.Add(obj["SerialNumber"]?.ToString() ?? "");
            }

            // BIOS serial
            using (var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS"))
            {
                foreach (ManagementObject obj in searcher.Get())
                    parts.Add(obj["SerialNumber"]?.ToString() ?? "");
            }

            var combined = string.Join("|", parts.Where(p => !string.IsNullOrEmpty(p)));
            if (string.IsNullOrEmpty(combined))
                combined = Environment.MachineName + Environment.UserName;

            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined + "Avryd_HW"));
            return Convert.ToHexString(hash)[..32].ToLower();
        }
        catch
        {
            var fallback = Environment.MachineName + Environment.UserName + Environment.ProcessorCount;
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(fallback));
            return Convert.ToHexString(hash)[..32].ToLower();
        }
    }
}
