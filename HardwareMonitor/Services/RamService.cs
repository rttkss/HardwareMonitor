using System;
using System.Management;

namespace HardwareMonitor
{
    public class RamService
    {
        public RamData Collect()
        {
            RamData data = new RamData();

            try
            {
                ManagementObjectSearcher osSearcher = new ManagementObjectSearcher(
                    "SELECT TotalVisibleMemorySize, FreePhysicalMemory FROM Win32_OperatingSystem");

                foreach (ManagementObject item in osSearcher.Get())
                {
                    long totalKB = Convert.ToInt64(item["TotalVisibleMemorySize"] ?? 0);
                    long freeKB = Convert.ToInt64(item["FreePhysicalMemory"] ?? 0);
                    data.TotalBytes = totalKB * 1024;
                    data.FreeBytes = freeKB * 1024;
                    break;
                }

                osSearcher.Dispose();
            }
            catch
            {
                // если WMI не отвечает
            }

            try
            {
                ManagementObjectSearcher memSearcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Capacity, Speed FROM Win32_PhysicalMemory");

                foreach (ManagementObject item in memSearcher.Get())
                {
                    RamStick stick = new RamStick();

                    string mfr = item["Manufacturer"] as string;
                    if (mfr != null)
                    {
                        stick.Brand = mfr.Trim();
                    }

                    object cap = item["Capacity"];
                    if (cap != null)
                    {
                        stick.SizeBytes = Convert.ToInt64(cap);
                    }

                    object spd = item["Speed"];
                    if (spd != null)
                    {
                        stick.Frequency = Convert.ToInt32(spd);
                    }

                    data.Sticks.Add(stick);
                }

                memSearcher.Dispose();
            }
            catch
            {
                // пропускаем
            }

            return data;
        }
    }
}