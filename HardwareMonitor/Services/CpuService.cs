using System;
using System.Management;

namespace HardwareMonitor
{
    public class CpuService
    {
        public CpuData Collect()
        {
            CpuData data = new CpuData();

            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT Name, NumberOfCores, NumberOfLogicalProcessors, " +
                    "MaxClockSpeed, Manufacturer, Architecture FROM Win32_Processor"))
                {
                    foreach (ManagementObject item in s.Get())
                    {
                        data.ProcessorName = item["Name"]?.ToString()?.Trim() ?? "Не определено";
                        data.PhysicalCores = Convert.ToInt32(item["NumberOfCores"] ?? 0);
                        data.LogicalCores = Convert.ToInt32(item["NumberOfLogicalProcessors"] ?? 0);
                        data.FrequencyMHz = Convert.ToInt32(item["MaxClockSpeed"] ?? 0);
                        data.Vendor = item["Manufacturer"]?.ToString() ?? "Не определено";

                        int archCode = Convert.ToInt32(item["Architecture"] ?? 0);
                        data.PlatformType = archCode == 9 ? "x64 (64-бит)" : "x86 (32-бит)";
                        break;
                    }
                }

                data.CurrentLoad = ReadLoad();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Сбой чтения CPU: " + ex.Message, ex);
            }

            return data;
        }

        private double ReadLoad()
        {
            try
            {
                using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                    "SELECT PercentProcessorTime FROM " +
                    "Win32_PerfFormattedData_PerfOS_Processor WHERE Name='_Total'"))
                {
                    foreach (ManagementObject item in s.Get())
                    {
                        return Convert.ToDouble(item["PercentProcessorTime"] ?? 0);
                    }
                }
            }
            catch { }
            return 0;
        }
    }
}