using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace SnakeGame
{
    public class GameEngine
    {
        private readonly Random _rand = new Random();
        public List<NetworkPlayer> Players { get; } = new List<NetworkPlayer>();
        public List<Food> Foods { get; } = new List<Food>();
        public List<Enemy> Enemies { get; } = new List<Enemy>();

        public float WorldWidth { get; set; } = 5000f;
        public float WorldHeight { get; set; } = 4000f;
        public float Scale { get; set; } = 1f;

        public float BaseSpeed => 4.5f * Scale;
        public float SegmentRadius => 8f * Scale;

        public void Initialize(int foodCount, int enemyCount, float aiDifficulty)
        {
            GenerateFoods(foodCount);
            SpawnEnemies(enemyCount);
        }

        public void Update(float dt, Dictionary<string, (Point dir, bool boost)> inputs)
        {
            foreach (var p in Players)
            {
                if (inputs.TryGetValue(p.Id, out var i))
                {
                    p.Direction = i.dir;
                    p.Boosting = i.boost;
                }
                MoveSnake(p, dt);
            }

            UpdateEnemies(dt);
            CheckFoodCollisions();
        }

        private void MoveSnake(NetworkPlayer p, float dt)
        {
            float speed = BaseSpeed * (p.Boosting ? 2f : 1f);
            var head = p.Body[0];
            head.X += p.Direction.X * speed * dt;
            head.Y += p.Direction.Y * speed * dt;
            p.Body.Insert(0, head);
            if (p.Body.Count > 3) p.Body.RemoveAt(p.Body.Count - 1);
        }

        private void UpdateEnemies(float dt)
        {
            foreach (var e in Enemies)
            {
                if (e.Body.Count == 0) continue;
                if (Players.Count > 0)
                {
                    var t = Players.OrderBy(p => Dist(e.Body[0], p.Body[0])).First();
                    var dx = t.Body[0].X - e.Body[0].X;
                    var dy = t.Body[0].Y - e.Body[0].Y;
                    var len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 1e-6) e.Direction = new Vector(dx / len, dy / len);
                }
                float speed = BaseSpeed * 0.8f;
                var head = new Point(
                    e.Body[0].X + e.Direction.X * speed * dt,
                    e.Body[0].Y + e.Direction.Y * speed * dt);
                e.Body.Insert(0, head);
                if (e.Body.Count > 3 + e.Score) e.Body.RemoveAt(e.Body.Count - 1);
            }
        }

        private void CheckFoodCollisions()
        {
            foreach (var p in Players)
            {
                var head = p.Body[0];
                for (int i = Foods.Count - 1; i >= 0; i--)
                {
                    if (Dist(head, Foods[i].Position) < SegmentRadius + Foods[i].Radius)
                    {
                        p.Score++;
                        Foods.RemoveAt(i);
                        Foods.Add(CreateFood());
                        p.Body.Add(p.Body.Last());
                        break;
                    }
                }
            }
        }

        public Food CreateFood()
            => new Food(RandomPosition(), Colors_RandomFood(), 6f * Scale);

        private System.Windows.Media.Color Colors_RandomFood()
        {
            var palette = new[]
            {
                System.Windows.Media.Colors.Red, System.Windows.Media.Colors.Orange,
                System.Windows.Media.Colors.Yellow, System.Windows.Media.Colors.Pink,
                System.Windows.Media.Colors.Purple, System.Windows.Media.Colors.Cyan
            };
            return palette[_rand.Next(palette.Length)];
        }

        public void GenerateFoods(int count)
        {
            Foods.Clear();
            for (int i = 0; i < count; i++) Foods.Add(CreateFood());
        }

        public void SpawnEnemies(int count)
        {
            Enemies.Clear();
            for (int i = 0; i < count; i++)
            {
                var pos = RandomPosition();
                var e = new Enemy(pos, $"AI_{i}") { Score = 0 };
                Enemies.Add(e);
            }
        }

        public Point RandomPosition()
        {
            const float margin = 100f;
            return new Point(
                _rand.NextDouble() * (WorldWidth - 2 * margin) - WorldWidth / 2 + margin,
                _rand.NextDouble() * (WorldHeight - 2 * margin) - WorldHeight / 2 + margin);
        }

        public object GetState() => new
        {
            Players = Players.Select(p => new
            {
                p.Id,
                Body = p.Body.Select(b => new { b.X, b.Y }).ToArray(),
                p.Score,
                p.Alive
            }).ToArray(),
            Foods = Foods.Select(f => new { f.Position.X, f.Position.Y, f.Radius }).ToArray(),
            Enemies = Enemies.Select(e => new
            {
                Body = e.Body.Select(b => new { b.X, b.Y }).ToArray(),
                e.Name,
                e.Score
            }).ToArray()
        };

        private static double Dist(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }

    public class NetworkPlayer
    {
        public string Id { get; set; } = "";
        public List<Point> Body { get; set; } = new List<Point>();
        public Point Direction { get; set; } = new Point(1, 0);
        public bool Boosting { get; set; }
        public int Score { get; set; }
        public bool Alive { get; set; } = true;
    }
}