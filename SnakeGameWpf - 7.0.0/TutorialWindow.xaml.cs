using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace SnakeGame
{
    public partial class TutorialWindow : Window
    {
        private static readonly Dictionary<string, (string title, string desc)> ModeData = new()
        {
            ["classic"] = ("经典模式",
        "• 鼠标控制蛇头方向\n" +
        "• 吃食物变长，躲避敌人与自己的身体\n" +
        "• 按住 Ctrl 加速\n" +
        "• 撞敌人身体或世界边缘即失败\n" +
        "• 击杀敌人获得额外分数与食物"),

            ["timed"] = ("淘汰之王",
        "• 49 名 AI 同场竞技，实时排行显示在右侧\n" +
        "• 靠进 AI 后按 F 发起决斗，双方冲向目标点\n" +
        "• 玩家先到 → 淘汰对手，等级 +1，速度提升\n" +
        "• 对手先到 → 游戏结束\n" +
        "• AI 等级越高越倾向躲避你的挑战\n" +
        "• 毒圈每 5 分钟收缩一次，圈外停留 15 秒判负"),

            ["team4v4"] = ("占领模式 (4v4)",
        "• 与蓝队 AI 合作，占领地图中央区域\n" +
        "• 站在圈内推进进度条，先到 ±30 分的一方获胜\n" +
        "• 蓝队是队友，红队是敌人\n" +
        "• 占领区不在屏幕内时，屏幕边缘会显示黄色箭头"),

            ["extreme"] = ("极限模式",
        "• 20 个敌人，食物仅 60 个\n" +
        "• 加速时间减半，AI 更强\n" +
        "• 考验反应速度与极限操作"),

            ["extraction"] = ("搜打撤",
        "• 靠近容器按 F 搜索\n" +
        "• 搜索完成后按 F 拾取分数\n" +
        "• 收集足够分数后前往绿色撤离点\n" +
        "• 在撤离点停留 9 秒即撤离成功\n" +
        "• 撤离成功解锁全局 Q 技能（下次游戏可用）\n" +
        "• 小心红色燃烧弹范围")
        };

        public TutorialWindow(string mode)
        {
            InitializeComponent();
            if (!ModeData.TryGetValue(mode, out var data))
                data = ModeData["classic"];

            TitleText.Text = $"🐍 自由贪吃蛇 · {data.title}";
            DescText.Text = data.desc;

            AddKeyTip("鼠标", "控制方向", Color.FromRgb(0, 103, 192));
            AddKeyTip("Ctrl", "加速", Color.FromRgb(120, 120, 120));
            AddKeyTip("F", "搜索 / 决斗", Color.FromRgb(255, 140, 0));
            AddKeyTip("Q", "释放技能", Color.FromRgb(255, 200, 10));
            AddKeyTip("Esc", "暂停 / 返回", Color.FromRgb(120, 120, 120));
            AddKeyTip("O", "联机", Color.FromRgb(0, 138, 0));

            var anim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(280))
            { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } };
            BeginAnimation(OpacityProperty, anim);
        }

        private void AddKeyTip(string key, string desc, Color keyColor)
        {
            var border = new Border
            {
                Background = new SolidColorBrush(keyColor),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(4, 4, 4, 4)
            };
            border.Child = new TextBlock
            {
                Text = key,
                Foreground = Brushes.White,
                FontWeight = FontWeights.Bold,
                FontSize = 12
            };
            KeyPanel.Children.Add(border);

            var descTxt = new TextBlock
            {
                Text = desc,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2, 4, 14, 4),
                Foreground = new SolidColorBrush(Color.FromRgb(80, 80, 80)),
                FontSize = 12
            };
            KeyPanel.Children.Add(descTxt);
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}