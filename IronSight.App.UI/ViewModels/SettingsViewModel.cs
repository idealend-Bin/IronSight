using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IronSight.App.UI.Core;
using IronSight.Interop.Native.Network;
using IronSight.Interop.Services;

namespace IronSight.App.UI.ViewModels
{
    public class SettingsViewModel : BaseViewModel
    {
        private readonly NetworkService _networkService;
        private readonly ConfigService _configService;

        // 绑定到 UI 的属性
        private string _selectedLanguage;
        private bool _isDarkMode;
        private double _samplingInterval;
        private bool _isAutoStart;

        public SettingsViewModel(NetworkService networkService, ConfigService configService)
        {
            _networkService = networkService;
            _configService = configService;

            // 加载初始设置
            LoadSettings();

            // 初始化命令
            SaveCommand = new RelayCommand(ExecuteSave);
            ResetCommand = new RelayCommand(ExecuteReset);
        }

        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set => SetProperty(ref _selectedLanguage, value);
        }

        public bool IsDarkMode
        {
            get => _isDarkMode;
            set
            {
                if (SetProperty(ref _isDarkMode, value))
                {
                    ApplyTheme();
                }
            }
        }

        public double SamplingInterval
        {
            get => _samplingInterval;
            set
            {
                if (SetProperty(ref _samplingInterval, value))
                {
                    // 实时调整 ElementRuntime 的采样频率
                    _networkService.UpdateTickRate(TimeSpan.FromMilliseconds(value));
                }
            }
        }

        public bool IsAutoStart
        {
            get => _isAutoStart;
            set => SetProperty(ref _isAutoStart, value);
        }

        public ICommand SaveCommand { get; }
        public ICommand ResetCommand { get; }

        private void LoadSettings()
        {
            var config = _configService.CurrentConfig;
            _selectedLanguage = config.Language;
            _isDarkMode = config.IsDarkMode;
            _samplingInterval = config.SamplingIntervalMs;
            _isAutoStart = config.IsAutoStart;
        }

        private void ExecuteSave()
        {
            var config = _configService.CurrentConfig;
            config.Language = SelectedLanguage;
            config.IsDarkMode = IsDarkMode;
            config.SamplingIntervalMs = (int)SamplingInterval;
            config.IsAutoStart = IsAutoStart;

            _configService.Save();

            // 可以在这里触发一个全局通知，比如“设置已保存”
            Console.WriteLine("[Settings] 配置已成功持久化到磁盘");
        }

        private void ExecuteReset()
        {
            _configService.ResetToDefault();
            LoadSettings();
            // 强制刷新 UI 绑定
            OnPropertyChanged(string.Empty);
        }

        private void ApplyTheme()
        {
            // 这里调用你的主题切换逻辑
            // 比如：AppThemeManager.SetTheme(IsDarkMode ? Themes.Dark : Themes.Light);
        }
    }

}