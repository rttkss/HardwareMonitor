namespace HardwareMonitor
{
    public class OsData
    {
        public string SystemName { get; set; }
        public string Build { get; set; }
        public string Bitness { get; set; }
        public string MachineName { get; set; }
        public string Account { get; set; }

        public OsData()
        {
            SystemName = "—";
            Build = "—";
            Bitness = "—";
            MachineName = "—";
            Account = "—";
        }
    }
}