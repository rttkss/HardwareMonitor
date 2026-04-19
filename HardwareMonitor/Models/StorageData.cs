using System.Collections.Generic;

namespace HardwareMonitor
{
    public class StorageData
    {
        public List<DriveUnit> Drives { get; set; }
        public List<Partition> Partitions { get; set; }

        public StorageData()
        {
            Drives = new List<DriveUnit>();
            Partitions = new List<Partition>();
        }
    }

    public class DriveUnit
    {
        public string ModelName { get; set; }
        public long TotalBytes { get; set; }
        public string Type { get; set; }

        public DriveUnit()
        {
            ModelName = "Не определено";
            Type = "Не определено";
        }

        public string SizeText => $"{TotalBytes / 1024.0 / 1024 / 1024:F1} ГБ";
    }

    public class Partition
    {
        public string Letter { get; set; }
        public long CapacityBytes { get; set; }
        public long FreeBytes { get; set; }
        public string FsType { get; set; }

        public double OccupiedPercent
        {
            get
            {
                if (CapacityBytes <= 0) return 0;
                return (double)(CapacityBytes - FreeBytes) / CapacityBytes * 100.0;
            }
        }

        public long UsedBytes => CapacityBytes - FreeBytes;

        public Partition()
        {
            Letter = "?";
            FsType = "Не определено";
        }
    }
}