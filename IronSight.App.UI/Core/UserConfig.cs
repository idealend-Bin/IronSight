using System;
using System.IO;
using System.Text.Json;

namespace IronSight.App.UI.Core
{
    /// <summary>
    /// 用户配置模型 - 对应设置页面的所有持久化项
    /// </summary>
    public class UserConfig
    {
        public string Language { get; set; } = "简体中文";
        public bool IsDarkMode { get; set; } = true;
        public int SamplingIntervalMs { get; set; } = 1000;
        public bool IsAutoStart { get; set; } = false;
        public bool AlwaysOnTop { get; set; } = false;
    }

    /// <summary>
    /// 负责配置文件的加载、保存与路径管理
    /// </summary>
    public class ConfigService
    {
        private readonly string _configPath;
        private UserConfig _current;

        public UserConfig Current => _current;
        
        /// <summary>
        /// 获取当前配置对象（与Current属性相同，提供更明确的命名）
        /// </summary>
        public UserConfig CurrentConfig => _current;

        public ConfigService()
        {
            // 将配置存在 AppData/Roaming/IronSight 目录下
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string configDir = Path.Combine(appData, "IronSight");

            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            _configPath = Path.Combine(configDir, "settings.json");
            _current = Load();
        }

        private UserConfig Load()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    string json = File.ReadAllText(_configPath);
                    return JsonSerializer.Deserialize<UserConfig>(json) ?? new UserConfig();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] 加载失败: {ex.Message}");
            }
            return new UserConfig();
        }

        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_current, options);
                File.WriteAllText(_configPath, json);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Config] 保存失败: {ex.Message}");
            }
        }

        public void Reset()
        {
            _current = new UserConfig();
            Save();
        }
        
        /// <summary>
        /// 重置为默认配置（与Reset方法相同，提供更明确的命名）
        /// </summary>
        public void ResetToDefault()
        {
            _current = new UserConfig();
            Save();
        }
    }
}