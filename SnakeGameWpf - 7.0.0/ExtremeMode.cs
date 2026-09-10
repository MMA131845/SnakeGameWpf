using System;
using System.Linq;
using System.Windows;

namespace SnakeGame
{
    public class ExtremeMode : GameModeBase
    {
        public override string Name => "extreme";
        public override int FoodCount => 60;
        public override int EnemyCount => 20;
        public override float AiDifficulty => 1.5f;

        public override void UpdateAI()
        {
            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                Point head = enemy.Body[0];
                Point playerHead = Form.Snake[0];
                if (Dist(head, playerHead) < 400 * Form.Scale)
                    enemy.Direction = Norm(playerHead.X - head.X, playerHead.Y - head.Y);
                else
                {
                    var nearest = Form.Foods.OrderBy(f => Dist(head, f.Position)).FirstOrDefault();
                    enemy.Direction = nearest != null
                        ? Norm(nearest.Position.X - head.X, nearest.Position.Y - head.Y)
                        : new Vector(1, 0);
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