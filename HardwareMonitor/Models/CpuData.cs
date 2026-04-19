namespace HardwareMonitor
{
    public class CpuData
    {
        public string ProcessorName { get; set; }
        public int PhysicalCores { get; set; }
        public int LogicalCores { get; set; }
        public int FrequencyMHz { get; set; }
        public double CurrentLoad { get; set; }
        public string Vendor { get; set; }
        public string PlatformType { get; set; }

        public CpuData()
        {
            ProcessorName = "Не определено";
            Vendor = "Не определено";
            PlatformType = "Не определено";
        }

        public string FrequencyGHz => $"{FrequencyMHz / 1000.0:F2} ГГц";
        public string LoadText => $"{CurrentLoad:F1}%";
    }
}