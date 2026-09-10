using Newtonsoft.Json;
using System;
using System.IO;
using System.Windows.Media;

namespace SnakeGame
{
    public class AppSettings
    {
        public int BackgroundStyle { get; set; } = 2;
        public string HeadColorHex { get; set; } = "#90EE90";
        public string BodyColorHex { get; set; } = "#008000";
        public string AccentColorHex { get; set; } = "#0067C0";

        /// <summary>主题名：Windows11 / 深色 / 浅色 / 纯白</summary>
        public string ThemeName { get; set; } = "Windows11";

        public int ResolutionIndex { get; set; } = 3;
        public int DisplayMode { get; set; } = 2;
        public int FpsTarget { get; set; } = 60;
        public bool AntiAlias { get; set; } = true;
        public bool Vsync { get; set; } = true;
        public bool HighFpsEnabled { get; set; }
        public int SelectedHighFpsIndex { get; set; }
        public bool BidirectionalIpc { get; set; }
        public bool SpeedBalance { get; set; } = true;
        public bool CustomBgEnabled { get; set; } = true;
        public string CustomBackgroundPath { get; set; } = "";
        // ★ 窗口几何信息
        public double WindowWidth { get; set; } = 1600;
        public double WindowHeight { get; set; } = 900;
        public double WindowLeft { get; set; } = -1;   // -1 表示未保存
        public double WindowTop { get; set; } = -1;
        public bool WindowMaximized { get; set; } = false;

        private static readonly string FILE =
    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app_settings_wpf.json");

        public static AppSettings Load()
        {
            try
            {
                var raw = SecureStorage.LoadText(FILE);
                if (!string.IsNullOrEmpty(raw))
                    return JsonConvert.DeserializeObject<AppSettings>(raw) ?? new AppSettings();
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                SecureStorage.SaveText(FILE, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch { }
        }

        public Color HeadColor() => ParseHex(HeadColorHex, Colors.LightGreen);
        public Color BodyColor() => ParseHex(BodyColorHex, Colors.Green);
        public Color AccentColor() => ParseHex(AccentColorHex, Color.FromRgb(0, 103, 192));

        /// <summary>根据主题名返回界面背景色</summary>
        public Color ThemeBackground()
        {
            switch (ThemeName)
            {
                case "深色": return Color.FromRgb(32, 32, 36);
                case "浅色": return Color.FromRgb(248, 248, 250);
                case "纯白": return Colors.White;
                default: return Color.FromRgb(245, 245, 247); // Windows11
            }
        }

        /// <summary>根据主题名返回主文字色</summary>
        public Color ThemeTextPrimary()
        {
            switch (ThemeName)
            {
                case "深色": return Color.FromRgb(240, 240, 240);
                default: return Color.FromRgb(22, 22, 22);
            }
        }

        /// <summary>根据主题名返回次要文字色</summary>
        public Color ThemeTextSecondary()
        {
            switch (ThemeName)
            {
                case "深色": return Color.FromRgb(160, 160, 160);
                default: return Color.FromRgb(96, 96, 96);
            }
        }

        /// <summary>根据主题名返回卡片色</summary>
        public Color ThemeCard()
        {
            switch (ThemeName)
            {
                case "深色": return Color.FromArgb(220, 48, 48, 54);
                case "纯白": return Color.FromArgb(255, 255, 255, 255);
                default: return Color.FromArgb(220, 255, 255, 255);
            }
        }

        /// <summary>根据主题名返回边框色</summary>
        public Color ThemeBorder()
        {
            switch (ThemeName)
            {
                case "深色": return Color.FromRgb(80, 80, 88);
                default: return Color.FromRgb(210, 210, 210);
            }
        }

        private static Color ParseHex(string hex, Color fallback)
        {
            try { return (Color)ColorConverter.ConvertFromString(hex); }
            catch { return fallback; }
        }
    }
}