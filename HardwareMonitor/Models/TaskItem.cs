using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace HardwareMonitor
{
    public class TaskItem : INotifyPropertyChanged
    {
        private string _processName;
        private string _state;
        private double _cpuPercent;
        private long _ramUsageKB;
        private int _pid;

        public int Pid
        {
            get => _pid;
            set { _pid = value; Notify(); }
        }

        public string ProcessName
        {
            get => _processName;
            set { _processName = value; Notify(); }
        }

        public string State
        {
            get => _state;
            set { _state = value; Notify(); }
        }

        public double CpuPercent
        {
            get => _cpuPercent;
            set { _cpuPercent = value; Notify(); }
        }

        public long RamUsageKB
        {
            get => _ramUsageKB;
            set { _ramUsageKB = value; Notify(); Notify(nameof(RamMB)); }
        }

        public double RamMB => _ramUsageKB / 1024.0;

        public TaskItem()
        {
            _processName = "—";
            _state = "—";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void Notify([CallerMemberName] string prop = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
    }
}