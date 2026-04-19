using System;
using System.Management;

namespace HardwareMonitor
{
    public class OsService
    {
        public OsData Collect()
        {
            OsData data = new OsData();

            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Caption, Version, OSArchitecture, CSName FROM Win32_OperatingSystem"))
                {
                    foreach (ManagementObject item in s.Get())
                    {
                        data.SystemName = item["Caption"]?.ToString() ?? "—";
                        data.Build = item["Version"]?.ToString() ?? "—";
                        data.Bitness = item["OSArchitecture"]?.ToString() ?? "—";
                        data.MachineName = item["CSName"]?.ToString() ?? "—";
                        break;
                    }
                }

                data.Account = Environment.UserName;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Сбой чтения ОС: " + ex.Message, ex);
            }

            return data;
        }
    }
}