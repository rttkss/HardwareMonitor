using System.Collections.Generic;

namespace HardwareMonitor
{
    public class RamData
    {
        public long TotalBytes { get; set; }
        public long FreeBytes { get; set; }
        public List<RamStick> Sticks { get; set; }

        public double UsedPercent
        {
            get
            {
                if (TotalBytes <= 0) return 0;
                return (double)(TotalBytes - FreeBytes) / TotalBytes * 100.0;
            }
        }

        public long UsedBytes
        {
            get { return TotalBytes - FreeBytes; }
        }

        public string TotalMBText
        {
            get { return (TotalBytes / 1024 / 1024).ToString() + " МБ"; }
        }

        public string FreeMBText
        {
            get { return (FreeBytes / 1024 / 1024).ToString() + " МБ"; }
        }

        public string UsedMBText
        {
            get { return (UsedBytes / 1024 / 1024).ToString() + " МБ"; }
        }

        public string UsedPercentText
        {
            get { return UsedPercent.ToString("F1") + "%"; }
        }

        public RamData()
        {
            Sticks = new List<RamStick>();
        }
    }

    public class RamStick
    {
        public string Brand { get; set; }
        public long SizeBytes { get; set; }
        public int Frequency { get; set; }

        public RamStick()
        {
            Brand = "Не определено";
        }

        public string SizeText
        {
            get { return (SizeBytes / 1024 / 1024).ToString() + " МБ"; }
        }

        public string FreqText
        {
            get { return Frequency.ToString() + " МГц"; }
        }
    }
}