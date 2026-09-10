using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SnakeGame
{
    public partial class AchievementsWindow : Window
    {
        public class Row
        {
            public string Name { get; set; } = "";
            public string Description { get; set; } = "";
            public string Icon { get; set; } = "";
            public Brush IconBrush { get; set; } = Brushes.Gray;
        }

        public AchievementsWindow(List<Achievement> defs, Dictionary<string, bool> unlocked)
        {
            InitializeComponent();
            var list = new List<Row>();
            foreach (var a in defs)
            {
                bool ok = unlocked.TryGetValue(a.Id, out var v) && v;
                list.Add(new Row
                {
                    Name = a.Name,
                    Description = a.Description,
                    Icon = ok ? "✔" : "✘",
                    IconBrush = ok ? new SolidColorBrush(Color.FromRgb(0, 138, 0)) : Brushes.Gray
                });
            }
            AchList.ItemsSource = list;
        }
    }
}