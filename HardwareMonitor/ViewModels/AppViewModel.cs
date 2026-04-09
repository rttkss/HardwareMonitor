using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace HardwareMonitor
{
    public class AppViewModel : ViewModelBase
    {
        private readonly CpuService _cpuSvc;
        private readonly RamService _ramSvc;
        private readonly StorageService _storageSvc;
        private readonly TaskService _taskSvc;
        private readonly OsService _osSvc;

        private DispatcherTimer _ticker;
        private const int GraphWidth = 80;
        private bool _isUpdating = false;

        public ObservableCollection<double> LoadHistory { get; set; }
        public ObservableCollection<TaskItem> Tasks { get; set; }

        private CpuData _cpu;
        public CpuData Cpu
        {
            get { return _cpu; }
            set { Set(ref _cpu, value); }
        }

        private RamData _ram;
        public RamData Ram
        {
            get { return _ram; }
            set { Set(ref _ram, value); }
        }

        private StorageData _storage;
        public StorageData Storage
        {
            get { return _storage; }
            set { Set(ref _storage, value); }
        }

        private OsData _os;
        public OsData Os
        {
            get { return _os; }
            set { Set(ref _os, value); }
        }

        private TaskItem _chosenTask;
        public TaskItem ChosenTask
        {
            get { return _chosenTask; }
            set { Set(ref _chosenTask, value); }
        }

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set { Set(ref _statusText, value); }
        }

        private int _totalProcessCount;
        public int TotalProcessCount
        {
            get { return _totalProcessCount; }
            set { Set(ref _totalProcessCount, value); }
        }

        public ICommand CmdRefresh { get; private set; }
        public ICommand CmdSaveReport { get; private set; }
        public ICommand CmdSaveGraph { get; private set; }
        public ICommand CmdEndTask { get; private set; }

        public AppViewModel()
        {
            _cpuSvc = new CpuService();
            _ramSvc = new RamService();
            _storageSvc = new StorageService();
            _taskSvc = new TaskService();
            _osSvc = new OsService();

            LoadHistory = new ObservableCollection<double>();
            Tasks = new ObservableCollection<TaskItem>();

            CmdRefresh = new DelegateCommand(DoRefresh);
            CmdSaveReport = new DelegateCommand(SaveReport);
            CmdSaveGraph = new DelegateCommand(SaveGraphImage);
            CmdEndTask = new DelegateCommand(EndSelectedTask);

            try
            {
                Os = _osSvc.Collect();
            }
            catch
            {
                Os = new OsData();
            }

            StatusText = "Запущено: " + DateTime.Now.ToString("HH:mm:ss");

            DoRefresh();

            _ticker = new DispatcherTimer();
            _ticker.Interval = TimeSpan.FromMilliseconds(2000);
            _ticker.Tick += OnTimerTick;
            _ticker.Start();
        }

        private void OnTimerTick(object sender, EventArgs e)
        {
            DoRefresh();
        }

        private async void DoRefresh()
        {
            if (_isUpdating) return;
            _isUpdating = true;

            try
            {
                // CPU
                try
                {
                    CpuData newCpu = await Task.Run(() => _cpuSvc.Collect());
                    Cpu = newCpu;
                    RaiseChanged("Cpu");
                }
                catch { if (Cpu == null) Cpu = new CpuData(); }

                // RAM
                try
                {
                    RamData newRam = await Task.Run(() => _ramSvc.Collect());
                    Ram = newRam;
                    RaiseChanged("Ram");
                }
                catch { if (Ram == null) Ram = new RamData(); }

                // Storage
                try
                {
                    StorageData newStorage = await Task.Run(() => _storageSvc.Collect());
                    Storage = newStorage;
                    RaiseChanged("Storage");
                }
                catch { if (Storage == null) Storage = new StorageData(); }

                // Processes
                await RefreshProcesses();

                // Graph
                PushLoadHistory();
                RenderGraph();

                StatusText = "Обновлено: " + DateTime.Now.ToString("HH:mm:ss") +
                             " | Процессов: " + TotalProcessCount;
            }
            finally
            {
                _isUpdating = false;
            }
        }

        private async Task RefreshProcesses()
        {
            // Запоминаем выбранный PID (не имя — PID уникален)
            int prevPid = -1;
            if (ChosenTask != null)
            {
                prevPid = ChosenTask.Pid;
            }

            try
            {
                List<TaskItem> fresh = await Task.Run(() => _taskSvc.Snapshot());

                TotalProcessCount = fresh.Count;

                // Строим словарь новых по PID
                Dictionary<int, TaskItem> freshByPid = new Dictionary<int, TaskItem>();
                foreach (TaskItem t in fresh)
                {
                    if (!freshByPid.ContainsKey(t.Pid))
                    {
                        freshByPid[t.Pid] = t;
                    }
                }

                // Обновляем существующие и удаляем мёртвые
                for (int i = Tasks.Count - 1; i >= 0; i--)
                {
                    int pid = Tasks[i].Pid;

                    if (freshByPid.ContainsKey(pid))
                    {
                        // Обновляем данные
                        TaskItem src = freshByPid[pid];
                        Tasks[i].CpuPercent = src.CpuPercent;
                        Tasks[i].RamUsageKB = src.RamUsageKB;
                        Tasks[i].State = src.State;
                        Tasks[i].ProcessName = src.ProcessName;

                        // Убираем из словаря — уже обработан
                        freshByPid.Remove(pid);
                    }
                    else
                    {
                        // Процесс завершился — удаляем
                        Tasks.RemoveAt(i);
                    }
                }

                // Добавляем новые процессы (которых не было в списке)
                foreach (TaskItem newItem in freshByPid.Values)
                {
                    Tasks.Add(newItem);
                }

                // Восстанавливаем выбор
                if (prevPid >= 0 && ChosenTask == null)
                {
                    foreach (TaskItem t in Tasks)
                    {
                        if (t.Pid == prevPid)
                        {
                            ChosenTask = t;
                            break;
                        }
                    }
                }
            }
            catch
            {
                // пропускаем
            }
        }

        private void PushLoadHistory()
        {
            if (Cpu == null) return;

            LoadHistory.Add(Cpu.CurrentLoad);

            while (LoadHistory.Count > GraphWidth)
            {
                LoadHistory.RemoveAt(0);
            }
        }

        private void RenderGraph()
        {
            System.Windows.Controls.Canvas canvas = null;

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                canvas = Application.Current.MainWindow.FindName("GraphArea")
                    as System.Windows.Controls.Canvas;
            }

            if (canvas == null || LoadHistory.Count < 2) return;

            canvas.Children.Clear();

            double w = canvas.ActualWidth;
            double h = canvas.ActualHeight;

            if (w <= 0 || h <= 0) return;

            double step = w / GraphWidth;

            for (int pct = 25; pct <= 75; pct += 25)
            {
                double ly = h - (pct / 100.0 * h);
                Line guide = new Line();
                guide.X1 = 0;
                guide.X2 = w;
                guide.Y1 = ly;
                guide.Y2 = ly;
                guide.Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255));
                guide.StrokeThickness = 1;
                canvas.Children.Add(guide);
            }

            Polyline line = new Polyline();
            line.Stroke = new SolidColorBrush(Color.FromRgb(0, 200, 255));
            line.StrokeThickness = 2;

            Polygon fill = new Polygon();
            fill.Fill = new SolidColorBrush(Color.FromArgb(50, 0, 200, 255));

            for (int i = 0; i < LoadHistory.Count; i++)
            {
                double x = i * step;
                double y = h - (LoadHistory[i] / 100.0 * h);
                line.Points.Add(new Point(x, y));
                fill.Points.Add(new Point(x, y));
            }

            fill.Points.Add(new Point((LoadHistory.Count - 1) * step, h));
            fill.Points.Add(new Point(0, h));

            canvas.Children.Add(fill);
            canvas.Children.Add(line);
        }

        private void EndSelectedTask()
        {
            if (ChosenTask == null)
            {
                MessageBox.Show("Сначала выберите процесс.", "Нет выбора",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string name = ChosenTask.ProcessName;
            int pid = ChosenTask.Pid;

            MessageBoxResult answer = MessageBox.Show(
                "Завершить процесс «" + name + "» (PID: " + pid + ")?",
                "Подтвердите",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (answer != MessageBoxResult.Yes) return;

            try
            {
                Process proc = Process.GetProcessById(pid);
                proc.Kill();
                proc.Dispose();

                ChosenTask = null;
                StatusText = "Процесс «" + name + "» (PID: " + pid + ") завершён";
                DoRefresh();
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Процесс уже завершён.", "Информация",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                DoRefresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message, "Сбой",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void EndTaskByDoubleClick(TaskItem item)
        {
            if (item == null) return;
            ChosenTask = item;
            EndSelectedTask();
        }

        // =============================================
        //  ЭКСПОРТ ОТЧЁТА: TXT + JSON + CSV
        // =============================================
        private void SaveReport()
        {
            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "Текстовый файл (*.txt)|*.txt|JSON файл (*.json)|*.json|CSV файл (*.csv)|*.csv|Все форматы сразу|*.*";
            dlg.FileName = "Report_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (dlg.ShowDialog() != true) return;

            string path = dlg.FileName;
            string ext = System.IO.Path.GetExtension(path).ToLower();
            string nameNoExt = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(path),
                System.IO.Path.GetFileNameWithoutExtension(path));

            if (dlg.FilterIndex == 4)
            {
                SaveAsTxt(nameNoExt + ".txt");
                SaveAsJson(nameNoExt + ".json");
                SaveAsCsv(nameNoExt + ".csv");
                StatusText = "Сохранено: .txt + .json + .csv";
                return;
            }

            if (ext == ".json")
            {
                SaveAsJson(path);
                StatusText = "JSON сохранён: " + path;
            }
            else if (ext == ".csv")
            {
                SaveAsCsv(path);
                StatusText = "CSV сохранён: " + path;
            }
            else
            {
                SaveAsTxt(path);
                StatusText = "TXT сохранён: " + path;
            }
        }

        private void SaveAsTxt(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("========================================");
            sb.AppendLine("      ОТЧЁТ О СИСТЕМЕ");
            sb.AppendLine("  " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"));
            sb.AppendLine("========================================");
            sb.AppendLine();

            sb.AppendLine("--- ОПЕРАЦИОННАЯ СИСТЕМА ---");
            if (Os != null)
            {
                sb.AppendLine("  Название:      " + Os.SystemName);
                sb.AppendLine("  Сборка:        " + Os.Build);
                sb.AppendLine("  Разрядность:   " + Os.Bitness);
                sb.AppendLine("  Компьютер:     " + Os.MachineName);
                sb.AppendLine("  Пользователь:  " + Os.Account);
            }
            sb.AppendLine();

            sb.AppendLine("--- ПРОЦЕССОР ---");
            if (Cpu != null)
            {
                sb.AppendLine("  Модель:        " + Cpu.ProcessorName);
                sb.AppendLine("  Вендор:        " + Cpu.Vendor);
                sb.AppendLine("  Платформа:     " + Cpu.PlatformType);
                sb.AppendLine("  Ядра/Потоки:   " + Cpu.PhysicalCores + "/" + Cpu.LogicalCores);
                sb.AppendLine("  Частота:       " + Cpu.FrequencyGHz);
                sb.AppendLine("  Нагрузка:      " + Cpu.CurrentLoad.ToString("F1") + "%");
            }
            sb.AppendLine();

            sb.AppendLine("--- ОПЕРАТИВНАЯ ПАМЯТЬ ---");
            if (Ram != null)
            {
                sb.AppendLine("  Всего:         " + Ram.TotalMBText);
                sb.AppendLine("  Свободно:      " + Ram.FreeMBText);
                sb.AppendLine("  Использовано:  " + Ram.UsedMBText);
                sb.AppendLine("  Занято:        " + Ram.UsedPercentText);

                if (Ram.Sticks != null && Ram.Sticks.Count > 0)
                {
                    sb.AppendLine("  Планок:        " + Ram.Sticks.Count);
                    for (int i = 0; i < Ram.Sticks.Count; i++)
                    {
                        sb.AppendLine("    [" + (i + 1) + "] " +
                            Ram.Sticks[i].Brand + " - " +
                            Ram.Sticks[i].SizeText + ", " +
                            Ram.Sticks[i].FreqText);
                    }
                }
            }
            sb.AppendLine();

            sb.AppendLine("--- НАКОПИТЕЛИ ---");
            if (Storage != null && Storage.Partitions != null)
            {
                foreach (Partition p in Storage.Partitions)
                {
                    sb.AppendLine("  " + p.Letter +
                        "  Всего: " + (p.CapacityBytes / 1024 / 1024 / 1024) + " ГБ" +
                        "  Свободно: " + (p.FreeBytes / 1024 / 1024 / 1024) + " ГБ" +
                        "  Занято: " + p.OccupiedPercent.ToString("F1") + "%" +
                        "  [" + p.FsType + "]");
                }
            }
            sb.AppendLine();

            sb.AppendLine("--- ПРОЦЕССЫ (" + Tasks.Count + " всего) ---");
            foreach (TaskItem t in Tasks)
            {
                sb.AppendLine("  [PID:" + t.Pid + "] " + t.ProcessName +
                    "  CPU: " + t.CpuPercent.ToString("F1") + "%" +
                    "  RAM: " + t.RamMB.ToString("F1") + " МБ" +
                    "  [" + t.State + "]");
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void SaveAsJson(string path)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("{");
            sb.AppendLine("  \"reportDate\": \"" + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + "\",");

            sb.AppendLine("  \"os\": {");
            sb.AppendLine("    \"name\": \"" + Esc(Os?.SystemName) + "\",");
            sb.AppendLine("    \"build\": \"" + Esc(Os?.Build) + "\",");
            sb.AppendLine("    \"bitness\": \"" + Esc(Os?.Bitness) + "\",");
            sb.AppendLine("    \"computer\": \"" + Esc(Os?.MachineName) + "\",");
            sb.AppendLine("    \"user\": \"" + Esc(Os?.Account) + "\"");
            sb.AppendLine("  },");

            sb.AppendLine("  \"cpu\": {");
            sb.AppendLine("    \"model\": \"" + Esc(Cpu?.ProcessorName) + "\",");
            sb.AppendLine("    \"vendor\": \"" + Esc(Cpu?.Vendor) + "\",");
            sb.AppendLine("    \"platform\": \"" + Esc(Cpu?.PlatformType) + "\",");
            sb.AppendLine("    \"cores\": " + (Cpu?.PhysicalCores ?? 0) + ",");
            sb.AppendLine("    \"threads\": " + (Cpu?.LogicalCores ?? 0) + ",");
            sb.AppendLine("    \"frequencyMHz\": " + (Cpu?.FrequencyMHz ?? 0) + ",");
            sb.AppendLine("    \"loadPercent\": " + (Cpu?.CurrentLoad ?? 0).ToString("F1").Replace(",", "."));
            sb.AppendLine("  },");

            sb.AppendLine("  \"ram\": {");
            sb.AppendLine("    \"totalBytes\": " + (Ram?.TotalBytes ?? 0) + ",");
            sb.AppendLine("    \"freeBytes\": " + (Ram?.FreeBytes ?? 0) + ",");
            sb.AppendLine("    \"usedPercent\": " + (Ram?.UsedPercent ?? 0).ToString("F1").Replace(",", ".") + ",");
            sb.AppendLine("    \"modules\": [");
            if (Ram?.Sticks != null)
            {
                for (int i = 0; i < Ram.Sticks.Count; i++)
                {
                    string comma = (i < Ram.Sticks.Count - 1) ? "," : "";
                    sb.AppendLine("      {");
                    sb.AppendLine("        \"brand\": \"" + Esc(Ram.Sticks[i].Brand) + "\",");
                    sb.AppendLine("        \"sizeBytes\": " + Ram.Sticks[i].SizeBytes + ",");
                    sb.AppendLine("        \"frequencyMHz\": " + Ram.Sticks[i].Frequency);
                    sb.AppendLine("      }" + comma);
                }
            }
            sb.AppendLine("    ]");
            sb.AppendLine("  },");

            sb.AppendLine("  \"disks\": [");
            if (Storage?.Partitions != null)
            {
                for (int i = 0; i < Storage.Partitions.Count; i++)
                {
                    Partition p = Storage.Partitions[i];
                    string comma = (i < Storage.Partitions.Count - 1) ? "," : "";
                    sb.AppendLine("    {");
                    sb.AppendLine("      \"letter\": \"" + Esc(p.Letter) + "\",");
                    sb.AppendLine("      \"capacityBytes\": " + p.CapacityBytes + ",");
                    sb.AppendLine("      \"freeBytes\": " + p.FreeBytes + ",");
                    sb.AppendLine("      \"usedPercent\": " + p.OccupiedPercent.ToString("F1").Replace(",", ".") + ",");
                    sb.AppendLine("      \"fileSystem\": \"" + Esc(p.FsType) + "\"");
                    sb.AppendLine("    }" + comma);
                }
            }
            sb.AppendLine("  ],");

            sb.AppendLine("  \"processes\": [");
            int cnt = 0;
            int total = Tasks.Count;
            foreach (TaskItem t in Tasks)
            {
                cnt++;
                string comma = (cnt < total) ? "," : "";
                sb.AppendLine("    {");
                sb.AppendLine("      \"name\": \"" + Esc(t.ProcessName) + "\",");
                sb.AppendLine("      \"pid\": " + t.Pid + ",");
                sb.AppendLine("      \"cpuPercent\": " + t.CpuPercent.ToString("F1").Replace(",", ".") + ",");
                sb.AppendLine("      \"ramMB\": " + t.RamMB.ToString("F1").Replace(",", ".") + ",");
                sb.AppendLine("      \"state\": \"" + Esc(t.State) + "\"");
                sb.AppendLine("    }" + comma);
            }
            sb.AppendLine("  ]");
            sb.AppendLine("}");

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private void SaveAsCsv(string path)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Раздел;Параметр;Значение");
            sb.AppendLine("ОС;Название;" + CsvSafe(Os?.SystemName));
            sb.AppendLine("ОС;Сборка;" + CsvSafe(Os?.Build));
            sb.AppendLine("ОС;Разрядность;" + CsvSafe(Os?.Bitness));
            sb.AppendLine("ОС;Компьютер;" + CsvSafe(Os?.MachineName));
            sb.AppendLine("ОС;Пользователь;" + CsvSafe(Os?.Account));

            sb.AppendLine("CPU;Модель;" + CsvSafe(Cpu?.ProcessorName));
            sb.AppendLine("CPU;Вендор;" + CsvSafe(Cpu?.Vendor));
            sb.AppendLine("CPU;Ядра;" + (Cpu?.PhysicalCores ?? 0));
            sb.AppendLine("CPU;Потоки;" + (Cpu?.LogicalCores ?? 0));
            sb.AppendLine("CPU;Частота МГц;" + (Cpu?.FrequencyMHz ?? 0));
            sb.AppendLine("CPU;Нагрузка %;" + (Cpu?.CurrentLoad ?? 0).ToString("F1"));

            sb.AppendLine("RAM;Всего МБ;" + (Ram != null ? (Ram.TotalBytes / 1024 / 1024).ToString() : "0"));
            sb.AppendLine("RAM;Свободно МБ;" + (Ram != null ? (Ram.FreeBytes / 1024 / 1024).ToString() : "0"));
            sb.AppendLine("RAM;Занято %;" + (Ram?.UsedPercent ?? 0).ToString("F1"));

            if (Ram?.Sticks != null)
            {
                for (int i = 0; i < Ram.Sticks.Count; i++)
                {
                    sb.AppendLine("RAM Модуль " + (i + 1) + ";Производитель;" + CsvSafe(Ram.Sticks[i].Brand));
                    sb.AppendLine("RAM Модуль " + (i + 1) + ";Размер МБ;" + (Ram.Sticks[i].SizeBytes / 1024 / 1024));
                    sb.AppendLine("RAM Модуль " + (i + 1) + ";Частота МГц;" + Ram.Sticks[i].Frequency);
                }
            }

            if (Storage?.Partitions != null)
            {
                foreach (Partition p in Storage.Partitions)
                {
                    sb.AppendLine("Диск " + p.Letter + ";Всего ГБ;" + (p.CapacityBytes / 1024 / 1024 / 1024));
                    sb.AppendLine("Диск " + p.Letter + ";Свободно ГБ;" + (p.FreeBytes / 1024 / 1024 / 1024));
                    sb.AppendLine("Диск " + p.Letter + ";Занято %;" + p.OccupiedPercent.ToString("F1"));
                    sb.AppendLine("Диск " + p.Letter + ";Файловая система;" + CsvSafe(p.FsType));
                }
            }

            sb.AppendLine();
            sb.AppendLine("Процесс;PID;CPU %;RAM МБ;Статус");
            foreach (TaskItem t in Tasks)
            {
                sb.AppendLine(CsvSafe(t.ProcessName) + ";" + t.Pid + ";" +
                    t.CpuPercent.ToString("F1") + ";" +
                    t.RamMB.ToString("F1") + ";" +
                    CsvSafe(t.State));
            }

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        }

        private string Esc(string s)
        {
            if (s == null) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private string CsvSafe(string s)
        {
            if (s == null) return "";
            if (s.Contains(";") || s.Contains("\"") || s.Contains("\n"))
            {
                return "\"" + s.Replace("\"", "\"\"") + "\"";
            }
            return s;
        }

        private void SaveGraphImage()
        {
            System.Windows.Controls.Canvas canvas = null;

            if (Application.Current != null && Application.Current.MainWindow != null)
            {
                canvas = Application.Current.MainWindow.FindName("GraphArea")
                    as System.Windows.Controls.Canvas;
            }

            if (canvas == null)
            {
                MessageBox.Show("График не найден.", "Ошибка",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.Filter = "PNG (*.png)|*.png";
            dlg.FileName = "CpuGraph_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";

            if (dlg.ShowDialog() != true) return;

            System.Windows.Media.Imaging.RenderTargetBitmap bmp =
                new System.Windows.Media.Imaging.RenderTargetBitmap(
                    (int)canvas.ActualWidth,
                    (int)canvas.ActualHeight,
                    96, 96,
                    PixelFormats.Pbgra32);

            bmp.Render(canvas);

            System.Windows.Media.Imaging.PngBitmapEncoder enc =
                new System.Windows.Media.Imaging.PngBitmapEncoder();

            enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bmp));

            using (FileStream fs = new FileStream(dlg.FileName, FileMode.Create))
            {
                enc.Save(fs);
            }

            StatusText = "График сохранён: " + dlg.FileName;
        }

        public void Shutdown()
        {
            if (_ticker != null)
            {
                _ticker.Stop();
                _ticker.Tick -= OnTimerTick;
            }
        }
    }
}