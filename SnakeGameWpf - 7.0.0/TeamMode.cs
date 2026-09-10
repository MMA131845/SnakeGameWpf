using System;
using System.Linq;
using System.Windows;

namespace SnakeGame
{
    public class TeamMode : GameModeBase
    {
        public override string Name => "team4v4";
        public override int FoodCount => 125;
        public override int EnemyCount => 7;
        public override float AiDifficulty => 0.9f;
        public override bool HasCaptureZone => true;

        public Point CaptureZone { get; set; } = new Point(0, 0);

        public override void UpdateAI()
        {
            if (Form.Snake.Count == 0) return;
            var playerHead = Form.Snake[0];

            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                Point head = enemy.Body[0];

                if (enemy.Team == "red")
                {
                    // 红队：优先攻击玩家，否则前往占领区
                    if (Dist(head, playerHead) < 400 * Form.Scale)
                        enemy.Direction = Norm(playerHead.X - head.X, playerHead.Y - head.Y);
                    else
                        enemy.Direction = Norm(CaptureZone.X - head.X, CaptureZone.Y - head.Y);
                }
                else
                {
                    // 蓝队：追击最近红队，否则前往占领区
                    var nearest = Form.Enemies
                        .Where(e => e.Team == "red" && e.Body.Count > 0)
                        .OrderBy(e => Dist(head, e.Body[0]))
                        .FirstOrDefault();

                    if (nearest != null && Dist(head, nearest.Body[0]) < 500 * Form.Scale)
                        enemy.Direction = Norm(nearest.Body[0].X - head.X,
                                               nearest.Body[0].Y - head.Y);
                    else
                        enemy.Direction = Norm(CaptureZone.X - head.X, CaptureZone.Y - head.Y);
                }
            }
        }

        private Vector Norm(double dx, double dy)
        {
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return new Vector(1, 0);
            return new Vector(dx / len, dy / len);
        }

        private double Dist(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}