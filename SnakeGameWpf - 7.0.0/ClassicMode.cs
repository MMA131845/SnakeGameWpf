using System;
using System.Linq;
using System.Windows;

namespace SnakeGame
{
    public class ClassicMode : GameModeBase
    {
        public override string Name => "classic";
        public override int FoodCount => 125;
        public override int EnemyCount => 10;
        public override float AiDifficulty => 0.6f;

        private readonly Random _rand = new Random();

        public override void UpdateAI()
        {
            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                Point head = enemy.Body[0];
                Point playerHead = Form.Snake[0];
                double dist = Distance(head, playerHead);
                if (dist < 300 * Form.Scale)
                    enemy.Direction = Normalize(playerHead.X - head.X, playerHead.Y - head.Y);
                else
                {
                    var nearestFood = Form.Foods
                        .OrderBy(f => Distance(head, f.Position))
                        .FirstOrDefault();
                    if (nearestFood != null)
                        enemy.Direction = Normalize(
                            nearestFood.Position.X - head.X,
                            nearestFood.Position.Y - head.Y);
                    else
                        enemy.Direction = RandomDirection();
                }
            }
        }

        // ✔ 返回类型改为 Vector
        private Vector RandomDirection()
        {
            double angle = _rand.NextDouble() * Math.PI * 2;
            return new Vector(Math.Cos(angle), Math.Sin(angle));
        }

        private Vector Normalize(double dx, double dy)
        {
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return new Vector(1, 0);
            return new Vector(dx / len, dy / len);
        }

        private double Distance(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}