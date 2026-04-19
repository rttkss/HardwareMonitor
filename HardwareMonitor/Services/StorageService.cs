using System;
using System.Collections.Generic;
using System.Management;

namespace HardwareMonitor
{
    public class StorageService
    {
        public StorageData Collect()
        {
            StorageData data = new StorageData();

            try
            {
                data.Drives = ScanDrives();
                data.Partitions = ScanPartitions();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Сбой чтения дисков: " + ex.Message, ex);
            }

            return data;
        }

        private List<DriveUnit> ScanDrives()
        {
            List<DriveUnit> list = new List<DriveUnit>();

            using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                "SELECT Model, Size, MediaType FROM Win32_DiskDrive"))
            {
                foreach (ManagementObject d in s.Get())
                {
                    list.Add(new DriveUnit
                    {
                        ModelName = d["Model"]?.ToString() ?? "Не определено",
                        TotalBytes = Convert.ToInt64(d["Size"] ?? 0),
                        Type = d["MediaType"]?.ToString() ?? "Не определено"
                    });
                }
            }

            return list;
        }

        private List<Partition> ScanPartitions()
        {
            List<Partition> list = new List<Partition>();

            using (ManagementObjectSearcher s = new ManagementObjectSearcher(
                "SELECT DeviceID, Size, FreeSpace, FileSystem " +
                "FROM Win32_LogicalDisk WHERE DriveType=3"))
            {
                foreach (ManagementObject v in s.Get())
                {
                    list.Add(new Partition
                    {
                        Letter = v["DeviceID"]?.ToString() ?? "?",
                        CapacityBytes = Convert.ToInt64(v["Size"] ?? 0),
                        FreeBytes = Convert.ToInt64(v["FreeSpace"] ?? 0),
                        FsType = v["FileSystem"]?.ToString() ?? "Не определено"
                    });
                }
            }

            return list;
        }
    }
}