using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace HardwareMonitor
{
    public class TaskService
    {
        private readonly Dictionary<int, TimeSpan> _prevTimes = new Dictionary<int, TimeSpan>();
        private DateTime _lastSample = DateTime.UtcNow;

        public List<TaskItem> Snapshot()
        {
            List<TaskItem> result = new List<TaskItem>();

            DateTime now = DateTime.UtcNow;
            double gap = Math.Max((now - _lastSample).TotalMilliseconds, 1);
            _lastSample = now;

            int cpuCount = Environment.ProcessorCount;

            // Очищаем старые PID которых уже нет
            HashSet<int> alivePids = new HashSet<int>();

            Process[] allProcs = null;
            try
            {
                allProcs = Process.GetProcesses();
            }
            catch
            {
                return result;
            }

            foreach (Process p in allProcs)
            {
                try
                {
                    int pid = p.Id;
                    alivePids.Add(pid);

                    TimeSpan cur = p.TotalProcessorTime;
                    double usage = 0;

                    if (_prevTimes.ContainsKey(pid))
                    {
                        TimeSpan prev = _prevTimes[pid];
                        usage = (cur - prev).TotalMilliseconds / (cpuCount * gap) * 100;
                    }

                    _prevTimes[pid] = cur;

                    string procName = p.ProcessName;
                    bool responding = true;

                    try
                    {
                        responding = p.Responding;
                    }
                    catch
                    {
                        // некоторые процессы не дают доступ
                    }

                    long memBytes = 0;
                    try
                    {
                        memBytes = p.WorkingSet64;
                    }
                    catch
                    {
                        // пропускаем
                    }

                    result.Add(new TaskItem
                    {
                        Pid = pid,
                        ProcessName = procName,
                        State = responding ? "Работает" : "Завис",
                        CpuPercent = Math.Round(Math.Min(usage, 100), 1),
                        RamUsageKB = memBytes / 1024
                    });
                }
                catch
                {
                    // пропускаем защищённые процессы
                }
                finally
                {
                    try { p.Dispose(); } catch { }
                }
            }

            // Удаляем из истории мёртвые PID
            List<int> deadPids = new List<int>();
            foreach (int pid in _prevTimes.Keys)
            {
                if (!alivePids.Contains(pid))
                {
                    deadPids.Add(pid);
                }
            }
            foreach (int pid in deadPids)
            {
                _prevTimes.Remove(pid);
            }

            // Сортировка по памяти
            result.Sort((a, b) => b.RamUsageKB.CompareTo(a.RamUsageKB));

            return result;
        }
    }
}