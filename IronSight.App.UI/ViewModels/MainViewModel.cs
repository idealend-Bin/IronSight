using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using IronSight.App.UI.Core;
using IronSight.Interop.Core;
using IronSight.Interop.Events;
using IronSight.Interop.Native.Memory;
using IronSight.Interop.Services;

namespace IronSight.App.UI.ViewModels
{
    public class MainViewModel : BaseViewModel, IDisposable
    {
        private readonly SystemMonitorService _systemMonitor;
        private readonly ClipboardService _clipboardService;

        private string _statusMessage = "System Ready";
        private double _cpuUsage;
        private double _diskReadRate;
        private double _diskWriteRate;

        // Navigation / View State
        private object _currentView; // For simplicity, could be ViewModel or simple Enum/Index

        public ObservableCollection<ClipboardItem> ClipboardHistory { get; } = new ObservableCollection<ClipboardItem>();

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public double CpuUsage
        {
            get => _cpuUsage;
            set => SetProperty(ref _cpuUsage, value);
        }

        public double DiskReadRate
        {
            get => _diskReadRate;
            set => SetProperty(ref _diskReadRate, value);
        }

        public double DiskWriteRate
        {
            get => _diskWriteRate;
            set => SetProperty(ref _diskWriteRate, value);
        }

        public ICommand CleanMemoryCommand { get; }
        public ICommand CopyClipboardItemCommand { get; }

        public MainViewModel()
        {
            CleanMemoryCommand = new RelayCommand(ExecuteCleanMemory);
            CopyClipboardItemCommand = new RelayCommand<ClipboardItem>(ExecuteCopyClipboardItem);

            // Initialize Services
            LoggerService.Initialize(); // Initialize Logging

            _systemMonitor = new SystemMonitorService();
            _systemMonitor.StatsUpdated += OnSystemStatsUpdated;
            _systemMonitor.Start();

            _clipboardService = new ClipboardService();
            _clipboardService.ClipboardChanged += OnClipboardChanged;
            _clipboardService.Start();
        }

        private void OnSystemStatsUpdated(object? sender, SystemStatsEventArgs e)
        {
            // Marshal to UI Thread
            Application.Current.Dispatcher.Invoke(() =>
            {
                CpuUsage = e.CpuUsage;
                DiskReadRate = e.DiskReadRate / 1024.0 / 1024.0; // MB/s
                DiskWriteRate = e.DiskWriteRate / 1024.0 / 1024.0; // MB/s
            });
        }

        private void OnClipboardChanged(object? sender, EventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        string text = Clipboard.GetText();
                        if (!string.IsNullOrWhiteSpace(text))
                        {
                            // Check for duplicates (simple check)
                            // Move to top if exists?
                            var existing = null as ClipboardItem;
                            foreach (var item in ClipboardHistory) { if (item.Content == text) { existing = item; break; } }

                            if (existing != null)
                            {
                                ClipboardHistory.Move(ClipboardHistory.IndexOf(existing), 0);
                                existing.Timestamp = DateTime.Now; // Update Time
                            }
                            else
                            {
                                ClipboardHistory.Insert(0, new ClipboardItem { Content = text, Timestamp = DateTime.Now });
                                if (ClipboardHistory.Count > 50) ClipboardHistory.RemoveAt(ClipboardHistory.Count - 1);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggerService.Log(LogLevel.Warn, $"Clipboard access failed: {ex.Message}");
                }
            });
        }

        private void ExecuteCleanMemory()
        {
            StatusMessage = "Optimizing Memory...";
            Task.Run(() =>
            {
                var result = MemoryMethods.CleanSystemMemory(); // Assume this exists from previous codebase
                Application.Current.Dispatcher.Invoke(() =>
                {
                    double mb = result.TotalBytesReleased / (1024.0 * 1024.0);
                    StatusMessage = $"Freed {mb:F2} MB RAM.";
                });
            });
        }

        private void ExecuteCopyClipboardItem(ClipboardItem item)
        {
            if (item != null)
            {
                try
                {
                    // Prevent loop? 
                    // Actually setting clipboard will trigger 'OnClipboardChanged'.
                    // We might want to ignore our own set.
                    // But for now, moving it to top is fine.
                    Clipboard.SetText(item.Content);
                }
                catch (Exception ex)
                {
                    StatusMessage = "Failed to copy: " + ex.Message;
                }
            }
        }


        public void Dispose()
        {
            _systemMonitor?.Dispose();
            _clipboardService?.Dispose();
        }
    }

    public class ClipboardItem
    {
        public string Content { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public string DisplayText => Content.Length > 50 ? Content.Substring(0, 50).Replace("\n", " ") + "..." : Content.Replace("\n", " ");
    }
}