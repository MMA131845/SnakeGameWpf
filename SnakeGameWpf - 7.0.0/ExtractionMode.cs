using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SnakeGame
{
    public class ExtractionMode : GameModeBase
    {
        public override string Name => "extraction";
        public override int FoodCount => 40;
        public override int EnemyCount => SelectedMap?.EnemyCount ?? 0;
        public override float AiDifficulty => 2.5f;

        public Point ExtractionPoint { get; set; }
        public double ExtractionRadius { get; set; } = 120;
        public double ExtractionTimer { get; set; } = 0;
        public const double RequiredExtractionTime = 9.0;
        public bool ExtractionComplete { get; set; } = false;

        public List<Container> Containers { get; set; } = new List<Container>();
        public Container CurrentNearContainer { get; set; } = null;
        public bool IsPlayerSearching { get; set; } = false;
        public double SearchProgress { get; set; } = 0;
        public int SearchScore { get; set; } = 0;
        public bool SearchComplete { get; set; } = false;
        public Point SearchCirclePos { get; set; }

        public List<Firebomb> Firebombs { get; set; } = new List<Firebomb>();
        public double FireExposureTimer { get; set; } = 0;
        private Random _rand = new Random();

        public static readonly List<MapData> Maps = new List<MapData>
        {
            new MapData
            {
                Name = "废弃工厂",
                WorldSize = new Size(4000, 3500),
                ExtractionPoint = new Point(800, 600),
                ContainerPositions = GenerateRandomPositions(30, new Size(4000, 3500)),
                EnemyCount = 5,
                EnemySpeedMultiplier = 1.0,
                EnemyLives = 1,
                HasFirebombs = false
            },
            new MapData
            {
                Name = "边境森林",
                WorldSize = new Size(5000, 4000),
                ExtractionPoint = new Point(1500, -1000),
                ContainerPositions = GenerateRandomPositions(99, new Size(5000, 4000)),
                EnemyCount = 10,
                EnemySpeedMultiplier = 1.5,
                EnemyLives = 1,
                HasFirebombs = false
            },
            new MapData
            {
                Name = "航天基地",
                WorldSize = new Size(8000, 6000),
                ExtractionPoint = new Point(3000, -2000),
                ContainerPositions = GenerateRandomPositions(999, new Size(8000, 6000)),
                EnemyCount = 20,
                EnemySpeedMultiplier = 5.0,
                EnemyLives = 3,
                HasFirebombs = true
            }
        };

        public MapData SelectedMap { get; set; }
        public ExtractionMode() { SelectedMap = Maps[0]; }

        private static List<Point> GenerateRandomPositions(int count, Size world)
        {
            var list = new List<Point>();
            var rng = new Random();
            for (int i = 0; i < count; i++)
            {
                list.Add(new Point(
                    rng.NextDouble() * world.Width - world.Width / 2,
                    rng.NextDouble() * world.Height - world.Height / 2));
            }
            return list;
        }

        public override void Initialize()
        {
            if (Form == null) return;
            Form.SetWorldSizeInternal(SelectedMap.WorldSize.Width, SelectedMap.WorldSize.Height);
            ExtractionPoint = SelectedMap.ExtractionPoint;
            ExtractionTimer = 0;
            ExtractionComplete = false;
            Firebombs.Clear();
            FireExposureTimer = 0;
            IsPlayerSearching = false;
            SearchProgress = 0;
            SearchComplete = false;

            Containers.Clear();
            foreach (var pos in SelectedMap.ContainerPositions)
                Containers.Add(new Container { Position = pos, IsSearched = false });

            Form.SpawnExtractionEnemies(SelectedMap.EnemyCount,
                SelectedMap.EnemySpeedMultiplier, SelectedMap.EnemyLives);
        }

        public override void UpdateBeforeMove()
        {
            if (Form == null) return;
            double dt = Form.DeltaTime;
            var playerHead = Form.Snake[0];
            double distToExtract = Distance(playerHead, ExtractionPoint);

            if (distToExtract <= ExtractionRadius * Form.Scale)
            {
                ExtractionTimer += dt;
                if (ExtractionTimer >= RequiredExtractionTime)
                {
                    ExtractionComplete = true;
                    MainWindow.HasQAbility = true;
                    Form.GameOver(true);
                }
            }
            else ExtractionTimer = 0;

            CurrentNearContainer = null;
            foreach (var c in Containers.Where(c => !c.IsSearched).ToList())
            {
                if (Distance(playerHead, c.Position) < 100 * Form.Scale)
                { CurrentNearContainer = c; break; }
            }

            bool fPressed = Form.IsKeyPressed(Key.F);
            if (CurrentNearContainer != null && !IsPlayerSearching && fPressed)
            {
                IsPlayerSearching = true;
                SearchProgress = 0;
                SearchComplete = false;
            }
            else if (IsPlayerSearching)
            {
                double searchTime = 0.02 * (Form.Score + 1);
                SearchProgress += dt;
                if (SearchProgress >= searchTime && !SearchComplete)
                {
                    SearchComplete = true;
                    SearchScore = GenerateSearchScore();
                    SearchCirclePos = new Point(
                        CurrentNearContainer.Position.X,
                        CurrentNearContainer.Position.Y - 40 * Form.Scale);
                }
                if (SearchComplete && fPressed)
                {
                    Form.AddScore(SearchScore);
                    CurrentNearContainer.IsSearched = true;
                    IsPlayerSearching = false;
                    SearchComplete = false;
                }
            }

            if (SelectedMap.HasFirebombs)
            {
                foreach (var enemy in Form.Enemies.ToList())
                {
                    if (_rand.NextDouble() < 0.02)
                    {
                        var target = new Point(
                            playerHead.X + (_rand.NextDouble() - 0.5) * 300,
                            playerHead.Y + (_rand.NextDouble() - 0.5) * 300);
                        Firebombs.Add(new Firebomb
                        {
                            Position = target,
                            Radius = 100,
                            Duration = 10,
                            Elapsed = 0
                        });
                    }
                }
                for (int i = Firebombs.Count - 1; i >= 0; i--)
                {
                    Firebombs[i].Elapsed += dt;
                    if (Firebombs[i].Elapsed >= Firebombs[i].Duration) Firebombs.RemoveAt(i);
                }

                bool inFire = Firebombs.ToList().Any(b =>
                    Distance(playerHead, b.Position) <= b.Radius * Form.Scale);
                if (inFire)
                {
                    FireExposureTimer += dt;
                    if (FireExposureTimer >= 5) Form.GameOver(false);
                }
                else FireExposureTimer = 0;
            }
        }

        private int GenerateSearchScore()
        {
            double baseChance = 0.5;
            if (SelectedMap.Name == "边境森林") baseChance = 0.7;
            else if (SelectedMap.Name == "航天基地") baseChance = 0.9;
            return _rand.NextDouble() < baseChance ? _rand.Next(80, 100) : _rand.Next(20, 60);
        }

        public override void UpdateAfterMove()
        {
            if (Form == null) return;
            var playerHead = Form.Snake[0];
            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                double dx = playerHead.X - enemy.Body[0].X;
                double dy = playerHead.Y - enemy.Body[0].Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 0) enemy.Direction = new Vector(dx / len, dy / len);
            }
        }

        public override void DrawUI(DrawingContext dc)
        {
            if (Form == null) return;
            double scale = Form.Scale;

            // 撤离点
            var exScreen = Form.WorldToScreen(ExtractionPoint);
            double exRadius = ExtractionRadius * scale;
            dc.DrawEllipse(
                new SolidColorBrush(Color.FromArgb(80, 0, 255, 0)), null,
                exScreen, exRadius, exRadius);
            dc.DrawEllipse(null, new Pen(Brushes.Green, 2), exScreen, exRadius, exRadius);

            if (ExtractionTimer > 0 && ExtractionTimer < RequiredExtractionTime)
            {
                double progress = ExtractionTimer / RequiredExtractionTime;
                double barW = 200 * scale, barH = 20 * scale;
                var barRect = new Rect(exScreen.X - barW / 2, exScreen.Y - exRadius - 30 * scale, barW, barH);
                dc.DrawRectangle(Brushes.Gray, null, barRect);
                dc.DrawRectangle(Brushes.Lime, null, new Rect(barRect.X, barRect.Y, barW * progress, barH));
                dc.DrawRectangle(null, new Pen(Brushes.White, 1), barRect);
            }

            // 容器
            foreach (var container in Containers.ToList())
            {
                var cs = Form.WorldToScreen(container.Position);
                double size = 20 * scale;
                if (container.IsSearched)
                {
                    dc.DrawRectangle(Brushes.Gray, null,
                        new Rect(cs.X - size / 2, cs.Y - size / 2, size, size));
                }
                else
                {
                    dc.DrawRectangle(Brushes.Yellow, null,
                        new Rect(cs.X - size / 2, cs.Y - size / 2, size, size));
                    if (container == CurrentNearContainer && !IsPlayerSearching)
                    {
                        var ft = Form.MakeText("按 F 搜索", 12 * scale, Brushes.White, false);
                        var bg = new Rect(
                            cs.X - ft.Width / 2 - 6,
                            cs.Y - size - 26 * scale,
                            ft.Width + 12, ft.Height + 6);
                        dc.DrawRectangle(Brushes.Black, null, bg);
                        dc.DrawText(ft, new Point(cs.X - ft.Width / 2, cs.Y - size - 22 * scale));
                    }
                }
            }

            if (IsPlayerSearching) DrawSearchOverlay(dc);

            // 燃烧弹
            foreach (var bomb in Firebombs.ToList())
            {
                var bs = Form.WorldToScreen(bomb.Position);
                double r = bomb.Radius * scale;
                dc.DrawEllipse(new SolidColorBrush(Color.FromArgb(60, 255, 0, 0)),
                    new Pen(Brushes.Red, 3), bs, r, r);
            }

            DrawMiniMap(dc);
        }

        private void DrawSearchOverlay(DrawingContext dc)
        {
            double scale = Form.Scale;
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)), null,
                new Rect(0, 0, Form.ScreenWidth, Form.ScreenHeight));

            double centerX = Form.ScreenWidth / 2;
            double centerY = Form.ScreenHeight / 2;
            double barW = 300 * scale, barH = 30 * scale;
            var barRect = new Rect(centerX - barW / 2, centerY + 80 * scale, barW, barH);

            dc.DrawRectangle(Brushes.Gray, null, barRect);
            double progress = Math.Min(1, SearchProgress / (0.02 * (Form.Score + 1)));
            dc.DrawRectangle(Brushes.Lime, null, new Rect(barRect.X, barRect.Y, barW * progress, barH));
            dc.DrawRectangle(null, new Pen(Brushes.White, 1), barRect);

            string text = SearchComplete ? "搜索完成！点击获取" : $"搜索中... {(int)(progress * 100)}%";
            var ft = Form.MakeText(text, 20 * scale, Brushes.White, false);
            dc.DrawText(ft, new Point(centerX - ft.Width / 2, centerY + 30 * scale));

            if (SearchComplete)
            {
                var cs = Form.WorldToScreen(SearchCirclePos);
                double radius = 50 * scale;
                var brush = new SolidColorBrush(GetScoreColor(SearchScore));
                dc.DrawEllipse(brush, new Pen(Brushes.White, 2), cs, radius, radius);
                var scoreFt = Form.MakeText($"+{SearchScore}", 24 * scale, Brushes.White, true);
                dc.DrawText(scoreFt, new Point(cs.X - scoreFt.Width / 2, cs.Y - scoreFt.Height / 2));
            }
        }

        private static Color GetScoreColor(int score)
        {
            if (score >= 90) return Colors.Gold;
            if (score >= 70) return Colors.Orange;
            if (score >= 50) return Colors.Purple;
            return Colors.CornflowerBlue;
        }

        private void DrawMiniMap(DrawingContext dc)
        {
            double size = 150 * Form.Scale;
            double x = 20, y = Form.ScreenHeight - size - 20;
            dc.DrawRectangle(Brushes.Black, null, new Rect(x, y, size, size));

            double worldW = SelectedMap.WorldSize.Width;
            double worldH = SelectedMap.WorldSize.Height;

            var player = Form.Snake[0];
            double px = x + (player.X - (-worldW / 2)) / worldW * size;
            double py = y + (player.Y - (-worldH / 2)) / worldH * size;
            dc.DrawEllipse(Brushes.Blue, null, new Point(px, py), 2, 2);

            // 撤离点
            double ex = x + (ExtractionPoint.X - (-worldW / 2)) / worldW * size;
            double ey = y + (ExtractionPoint.Y - (-worldH / 2)) / worldH * size;
            dc.DrawEllipse(Brushes.Green, null, new Point(ex, ey), 3, 3);

            // 容器
            foreach (var c in Containers.ToList())
            {
                if (c.IsSearched) continue;
                double cx = x + (c.Position.X - (-worldW / 2)) / worldW * size;
                double cy = y + (c.Position.Y - (-worldH / 2)) / worldH * size;
                dc.DrawRectangle(Brushes.Yellow, null, new Rect(cx - 1, cy - 1, 2, 2));
            }

            // 敌人
            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                var ep = enemy.Body[0];
                double epx = x + (ep.X - (-worldW / 2)) / worldW * size;
                double epy = y + (ep.Y - (-worldH / 2)) / worldH * size;
                dc.DrawEllipse(Brushes.Red, null, new Point(epx, epy), 1, 1);
            }
        }

        private double Distance(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
    }
}