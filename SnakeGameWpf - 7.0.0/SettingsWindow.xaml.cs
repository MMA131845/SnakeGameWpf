using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SnakeGame
{
    public partial class SettingsWindow : Window
    {
        private readonly AppSettings _draft;
        private readonly AppSettings _original;

        private static readonly Dictionary<string, string> BackgroundOptions =
            new() { { "0", "纯黑" }, { "1", "纯白" }, { "2", "格子" }, { "3", "星空" } };

        private static readonly (string name, Color color)[] HeadOptions = {
            ("浅绿", Colors.LightGreen), ("黄", Colors.Yellow), ("橙", Colors.Orange),
            ("粉", Colors.Pink), ("青", Colors.Cyan), ("白", Colors.White)
        };
        private static readonly (string name, Color color)[] BodyOptions = {
            ("绿", Colors.Green), ("深绿", Colors.DarkGreen), ("蓝", Colors.Blue),
            ("紫", Colors.Purple), ("棕", Colors.Brown), ("灰", Colors.Gray)
        };
        private static readonly (string name, Color color)[] AccentOptions = {
            ("蓝", Color.FromRgb(0, 103, 192)), ("红", Color.FromRgb(232, 17, 35)),
            ("绿", Color.FromRgb(0, 138, 0)),   ("橙", Color.FromRgb(255, 140, 0)),
            ("紫", Color.FromRgb(107, 105, 214)), ("青", Color.FromRgb(0, 178, 210))
        };

        public SettingsWindow(AppSettings current)
        {
            InitializeComponent();
            _original = current;
            _draft = Clone(current);
            NavList.SelectedIndex = 0;
        }

        private static AppSettings Clone(AppSettings s) => new AppSettings
        {
            BackgroundStyle = s.BackgroundStyle,
            HeadColorHex = s.HeadColorHex,
            BodyColorHex = s.BodyColorHex,
            AccentColorHex = s.AccentColorHex,
            ResolutionIndex = s.ResolutionIndex,
            DisplayMode = s.DisplayMode,
            FpsTarget = s.FpsTarget,
            AntiAlias = s.AntiAlias,
            Vsync = s.Vsync,
            HighFpsEnabled = s.HighFpsEnabled,
            SelectedHighFpsIndex = s.SelectedHighFpsIndex,
            BidirectionalIpc = s.BidirectionalIpc,
            SpeedBalance = s.SpeedBalance,
            CustomBgEnabled = s.CustomBgEnabled,
            CustomBackgroundPath = s.CustomBackgroundPath
        };

        private void NavList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PaneContent == null) return;
            PaneContent.Children.Clear();
            switch (NavList.SelectedIndex)
            {
                case 0: BuildBackgroundPane(); break;
                case 1: BuildHeadColorPane(); break;
                case 2: BuildBodyColorPane(); break;
                case 3: BuildAccentPane(); break;
                case 4: BuildGraphicsPane(); break;
                case 5: BuildExperimentalPane(); break;
            }
        }

        private void BuildBackgroundPane()
        {
            PaneTitle.Text = "背景样式";
            foreach (var kv in BackgroundOptions)
            {
                int val = int.Parse(kv.Key);
                var rb = new RadioButton
                {
                    Content = kv.Value,
                    GroupName = "bg",
                    IsChecked = _draft.BackgroundStyle == val,
                    Margin = new Thickness(0, 6, 0, 6),
                    Tag = val
                };
                rb.Checked += (s, e) => _draft.BackgroundStyle = (int)((RadioButton)s).Tag;
                PaneContent.Children.Add(rb);
            }
        }

        private void BuildHeadColorPane()
        {
            PaneTitle.Text = "蛇头颜色";
            BuildColorList(HeadOptions, _draft.HeadColorHex, c => _draft.HeadColorHex = c);
        }

        private void BuildBodyColorPane()
        {
            PaneTitle.Text = "蛇身颜色";
            BuildColorList(BodyOptions, _draft.BodyColorHex, c => _draft.BodyColorHex = c);
        }

        private void BuildAccentPane()
        {
            PaneTitle.Text = "主题色";
            BuildColorList(AccentOptions, _draft.AccentColorHex, c => _draft.AccentColorHex = c);
        }

        private void BuildColorList((string name, Color color)[] options, string currentHex, Action<string> setter)
        {
            var current = (Color)ColorConverter.ConvertFromString(currentHex);
            foreach (var opt in options)
            {
                var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 6, 0, 6) };
                var swatch = new Border
                {
                    Width = 24,
                    Height = 24,
                    Background = new SolidColorBrush(opt.color),
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(4)
                };
                var rb = new RadioButton
                {
                    Content = opt.name,
                    GroupName = "colorGroup",
                    IsChecked = current == opt.color,
                    Margin = new Thickness(10, 0, 0, 0),
                    Tag = opt.color
                };
                rb.Checked += (s, e) =>
                {
                    var c = (Color)((RadioButton)s).Tag;
                    setter(ColorToHex(c));
                };
                panel.Children.Add(swatch);
                panel.Children.Add(rb);
                PaneContent.Children.Add(panel);
            }
        }

        private void BuildGraphicsPane()
        {
            PaneTitle.Text = "画面设置";

            // 分辨率
            var resolutions = new[] {
                (800,600),(1024,768),(1280,720),(1600,900),(1920,1080),(2560,1440),(3840,2160)
            };
            AddLabeledCombo("分辨率", resolutions.Length, _draft.ResolutionIndex,
                i => $"{resolutions[i].Item1} × {resolutions[i].Item2}",
                i => _draft.ResolutionIndex = i);

            AddLabeledCombo("显示模式", 3, _draft.DisplayMode,
                i => new[] { "无边框全屏", "窗口化（无边框）", "窗口化" }[i],
                i => _draft.DisplayMode = i);

            AddLabeledCombo("最高帧数", 6, FpsIndexFromValue(_draft.FpsTarget),
                i => new[] { 30, 60, 120, 144, 240, 300 }[i].ToString(),
                i => _draft.FpsTarget = new[] { 30, 60, 120, 144, 240, 300 }[i]);

            AddCheckbox("抗锯齿", _draft.AntiAlias, v => _draft.AntiAlias = v);
            AddCheckbox("垂直同步", _draft.Vsync, v => _draft.Vsync = v);

            // 自定义背景
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 12, 0, 12) };
            var btn = new Button { Content = "选择背景图片", Width = 140, Height = 32 };
            var pathText = new TextBlock
            {
                Text = string.IsNullOrEmpty(_draft.CustomBackgroundPath) ? "未设置" : _draft.CustomBackgroundPath,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(12, 0, 0, 0),
                Foreground = Brushes.Gray,
                MaxWidth = 360,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            btn.Click += (s, e) =>
            {
                var ofd = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "图像|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*"
                };
                if (ofd.ShowDialog() == true)
                {
                    _draft.CustomBackgroundPath = ofd.FileName;
                    pathText.Text = ofd.FileName;
                }
            };
            sp.Children.Add(btn);
            sp.Children.Add(pathText);
            PaneContent.Children.Add(sp);

            AddCheckbox("启用自定义背景", _draft.CustomBgEnabled, v => _draft.CustomBgEnabled = v);
        }

        private int FpsIndexFromValue(int fps)
        {
            var arr = new[] { 30, 60, 120, 144, 240, 300 };
            var idx = Array.IndexOf(arr, fps);
            return idx < 0 ? 1 : idx;
        }

        private void BuildExperimentalPane()
        {
            PaneTitle.Text = "实验性功能";
            AddCheckbox("双向 IPC 通信", _draft.BidirectionalIpc, v => _draft.BidirectionalIpc = v);
            AddCheckbox("高帧数模式", _draft.HighFpsEnabled, v => _draft.HighFpsEnabled = v);
            AddCheckbox("帧数与速度平衡", _draft.SpeedBalance, v => _draft.SpeedBalance = v);
        }

        private void AddLabeledCombo(string label, int count, int current, Func<int, string> fmt, Action<int> onChanged)
        {
            var panel = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) });
            panel.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            var combo = new ComboBox { Height = 32 };
            for (int i = 0; i < count; i++) combo.Items.Add(fmt(i));
            combo.SelectedIndex = current;
            combo.SelectionChanged += (s, e) => { if (combo.SelectedIndex >= 0) onChanged(combo.SelectedIndex); };
            Grid.SetColumn(lbl, 0); Grid.SetColumn(combo, 1);
            panel.Children.Add(lbl);
            panel.Children.Add(combo);
            PaneContent.Children.Add(panel);
        }

        private void AddCheckbox(string label, bool value, Action<bool> onChanged)
        {
            var cb = new CheckBox
            {
                Content = label,
                IsChecked = value,
                Margin = new Thickness(0, 8, 0, 8)
            };
            cb.Checked += (s, e) => onChanged(true);
            cb.Unchecked += (s, e) => onChanged(false);
            PaneContent.Children.Add(cb);
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            CopyTo(_original);
            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CopyTo(AppSettings target)
        {
            target.BackgroundStyle = _draft.BackgroundStyle;
            target.HeadColorHex = _draft.HeadColorHex;
            target.BodyColorHex = _draft.BodyColorHex;
            target.AccentColorHex = _draft.AccentColorHex;
            target.ResolutionIndex = _draft.ResolutionIndex;
            target.DisplayMode = _draft.DisplayMode;
            target.FpsTarget = _draft.FpsTarget;
            target.AntiAlias = _draft.AntiAlias;
            target.Vsync = _draft.Vsync;
            target.HighFpsEnabled = _draft.HighFpsEnabled;
            target.SelectedHighFpsIndex = _draft.SelectedHighFpsIndex;
            target.BidirectionalIpc = _draft.BidirectionalIpc;
            target.SpeedBalance = _draft.SpeedBalance;
            target.CustomBgEnabled = _draft.CustomBgEnabled;
            target.CustomBackgroundPath = _draft.CustomBackgroundPath;
        }

        private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";
    }
}