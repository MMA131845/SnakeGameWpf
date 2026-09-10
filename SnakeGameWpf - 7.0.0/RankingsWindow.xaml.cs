using System.Collections.Generic;
using System.Windows;

namespace SnakeGame
{
    public partial class RankingsWindow : Window
    {
        public class Row
        {
            public int Rank { get; set; }
            public string Name { get; set; } = "";
            public int Score { get; set; }
        }

        public RankingsWindow(List<List<object>> scores)
        {
            InitializeComponent();
            var list = new List<Row>();
            for (int i = 0; i < scores.Count; i++)
            {
                list.Add(new Row
                {
                    Rank = i + 1,
                    Name = scores[i][0]?.ToString() ?? "?",
                    Score = System.Convert.ToInt32(scores[i][1])
                });
            }
            RankList.ItemsSource = list;
        }
    }
}