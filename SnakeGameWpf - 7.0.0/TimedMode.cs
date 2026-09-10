using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace SnakeGame
{
    public class TimedMode : GameModeBase
    {
        public override string Name => "timed";
        public override int FoodCount => 0;
        // ★ AI 数量：49（加上玩家 = 50 人局）
        public override int EnemyCount => 49;
        public override float AiDifficulty => 1.0f;
        public override bool HasTimedRanking => true;

        public int PlayerLevel = 1;
        public const int MaxLevel = 10;

        private readonly Random _rand = new Random();
        private readonly List<int> _enemyLevels = new List<int>();
        private double _aiTime = 0;

        public double PoisonRadius = 5000;
        public Point PoisonCenter = new Point(0, 0);
        public double PoisonShrinkTimer = 300;
        public int ShrinkCount = 0;
        public double PoisonTimeout = 15;
        private readonly Dictionary<int, double> _outsideTimers = new Dictionary<int, double>();

        public bool IsRacing = false;
        public Enemy RaceOpponent = null;
        public Point RaceDestination = new Point(0, 0);
        public double RaceTimer = 0;
        public const double RaceTimeout = 25.0;
        public const double RaceArriveRadius = 80.0;
        public string RaceResultMessage = "";
        public double RaceResultTimer = 0;

        public bool RaceDisabled = false;
        public double DisableTimer = 180;

        private Rect _roadHorizontal, _roadVertical;
        private const double RoadWidth = 100;
        private double _worldWidth = 5000, _worldHeight = 4000;

        public static readonly Size WorldSize = new Size(5000, 4000);
        public static readonly double WorldWidth = 5000;
        public static readonly double WorldHeight = 4000;

        public bool LevelSelected = false;
        public bool PositionSelected = false;
        public Point SelectedSpawn = default;

        public override void Initialize()
        {
            if (Form == null) return;
            Form.SetWorldSizeInternal(WorldWidth, WorldHeight);
            PlayerLevel = 1;
            PoisonRadius = 5000;
            PoisonCenter = new Point(0, 0);
            ShrinkCount = 0;
            PoisonShrinkTimer = 300;
            RaceDisabled = false;
            DisableTimer = 180;
            IsRacing = false;
            RaceOpponent = null;
            RaceTimer = 0;
            RaceResultMessage = "";
            RaceResultTimer = 0;
            _enemyLevels.Clear();
            _outsideTimers.Clear();
            _aiTime = 0;
            _worldWidth = WorldWidth;
            _worldHeight = WorldHeight;
            _roadHorizontal = new Rect(-_worldWidth / 2, -RoadWidth / 2, _worldWidth, RoadWidth);
            _roadVertical = new Rect(-RoadWidth / 2, -_worldHeight / 2, RoadWidth, _worldHeight);
        }

        public void StartGameAfterSelection()
        {
            Form.SetWorldSizeInternal(WorldWidth, WorldHeight);
            Form.Enemies.Clear();
            _enemyLevels.Clear();
            for (int i = 0; i < EnemyCount; i++)
            {
                Point spawn = Form.RandomPosition(100);
                var enemy = new Enemy(spawn, GenerateRandomName());
                int level = _rand.Next(1, MaxLevel + 1);
                _enemyLevels.Add(level);
                enemy.Score = level;
                enemy.SpeedMultiplier = 0.85 + level * 0.03;
                enemy.Body.Clear();
                enemy.Body.Add(spawn);
                enemy.Body.Add(new Point(spawn.X - 16 * Form.Scale, spawn.Y));
                enemy.Body.Add(new Point(spawn.X - 32 * Form.Scale, spawn.Y));
                Form.Enemies.Add(enemy);
            }
            PlayerLevel = 1;
        }

        private string GenerateRandomName()
        {
            string[] names = { "MMA", "ACE", "VIP", "GOD", "FOX", "WOLF", "BEAR", "TIGER", "LION", "EAGLE" };
            return names[_rand.Next(names.Length)];
        }

        public override void UpdateBeforeMove()
        {
            if (Form == null || !Form.GameActive || Form.Paused) return;

            // 等级影响玩家速度
            Form.SpeedMultiplier = 1.0 + (PlayerLevel - 1) * 0.03;

            if (IsRacing)
            {
                UpdateRace();
                return;
            }

            UpdatePoison();
            CheckPlayerRaceRequest();
        }

        // 玩家按 F 挑战附近 AI
        private void CheckPlayerRaceRequest()
        {
            if (RaceDisabled) return;
            if (Form.Snake.Count == 0) return;
            if (!Form.IsKeyPressed(Key.F)) return;

            var head = Form.Snake[0];
            Enemy nearest = null;
            double nearestDist = 600 * Form.Scale;
            foreach (var e in Form.Enemies.ToList())
            {
                if (e.Body.Count == 0) continue;
                double d = Distance(head, e.Body[0]);
                if (d < nearestDist) { nearestDist = d; nearest = e; }
            }
            if (nearest != null) StartRace(nearest);
        }

        private void StartRace(Enemy opponent)
        {
            IsRacing = true;
            RaceOpponent = opponent;
            RaceTimer = 0;

            // ★ 目的地限制在毒圈内 + 世界边界内，避免撞墙
            double angle = _rand.NextDouble() * Math.PI * 2;
            double maxByPoison = PoisonRadius * 0.6;
            double maxByWorld = Math.Min(_worldWidth, _worldHeight) * 0.35;
            double maxRadius = Math.Min(maxByPoison, maxByWorld);
            if (maxRadius < 300) maxRadius = 300;
            double radius = maxRadius * (0.5 + _rand.NextDouble() * 0.5);

            RaceDestination = new Point(
                PoisonCenter.X + Math.Cos(angle) * radius,
                PoisonCenter.Y + Math.Sin(angle) * radius);
            RaceResultMessage = "";
        }

        private void UpdateRace()
        {
            if (Form.Snake.Count == 0 || RaceOpponent == null || RaceOpponent.Body.Count == 0)
            {
                EndRace(0);
                return;
            }
            double dt = Form.DeltaTime;
            RaceTimer += dt;

            var head = Form.Snake[0];
            double dxp = RaceDestination.X - head.X;
            double dyp = RaceDestination.Y - head.Y;
            double lenP = Math.Sqrt(dxp * dxp + dyp * dyp);
            if (lenP > 1) Form.Direction = new Vector(dxp / lenP, dyp / lenP);

            var oh = RaceOpponent.Body[0];
            double dxo = RaceDestination.X - oh.X;
            double dyo = RaceDestination.Y - oh.Y;
            double lenO = Math.Sqrt(dxo * dxo + dyo * dyo);
            if (lenO > 1) RaceOpponent.Direction = new Vector(dxo / lenO, dyo / lenO);

            if (lenP < RaceArriveRadius * Form.Scale) { EndRace(1); return; }
            if (lenO < RaceArriveRadius * Form.Scale) { EndRace(-1); return; }
            if (RaceTimer >= RaceTimeout) EndRace(0);
        }

        private void EndRace(int result)
        {
            IsRacing = false;
            if (result == 1)
            {
                if (RaceOpponent != null)
                {
                    int idx = Form.Enemies.IndexOf(RaceOpponent);
                    if (idx >= 0)
                    {
                        Form.Enemies.RemoveAt(idx);
                        if (idx < _enemyLevels.Count) _enemyLevels.RemoveAt(idx);
                    }
                }
                if (PlayerLevel < MaxLevel) PlayerLevel++;
                RaceResultMessage = $"决斗胜利！等级 {PlayerLevel}";
                RaceResultTimer = 3.0;
            }
            else if (result == -1)
            {
                RaceResultMessage = "决斗失败！";
                RaceResultTimer = 3.0;
                Form.GameOver(false);
            }
            else
            {
                RaceResultMessage = "决斗超时，平局";
                RaceResultTimer = 3.0;
            }
            RaceOpponent = null;
        }

        private void UpdatePoison()
        {
            double dt = Form.DeltaTime;
            if (ShrinkCount < 3)
            {
                PoisonShrinkTimer -= dt;
                if (PoisonShrinkTimer <= 0)
                {
                    PoisonRadius -= 1000;
                    ShrinkCount++;
                    PoisonShrinkTimer = 300;
                    if (ShrinkCount == 3) DisableTimer = 180;
                }
            }
            if (DisableTimer > 0 && ShrinkCount == 3)
            {
                DisableTimer -= dt;
                if (DisableTimer <= 0) RaceDisabled = true;
            }

            if (Form.Snake.Count == 0) return;
            double playerDist = Distance(Form.Snake[0], PoisonCenter);
            if (playerDist > PoisonRadius)
            {
                if (!_outsideTimers.ContainsKey(-1)) _outsideTimers[-1] = 0;
                _outsideTimers[-1] += dt;
                if (_outsideTimers[-1] >= PoisonTimeout) Form.GameOver(false);
            }
            else _outsideTimers[-1] = 0;
        }

        // ★ AI 逻辑：
        //   ① 毒圈边缘 → 向中心撤
        //   ② 玩家在 400px 内 → 25% 追击 / 75% 躲避（玩家等级越高越倾向躲避）
        //   ③ 否则 → 巡逻
        public override void UpdateAI()
        {
            if (IsRacing) return;
            if (Form.Snake.Count == 0) return;

            _aiTime += Form.DeltaTime;
            var playerHead = Form.Snake[0];
            double triggerDist = 400 * Form.Scale;

            // 躲避概率：等级 1 → 75%，等级 10 → 93%，封顶 95%
            double dodgeChance = 0.75 + (PlayerLevel - 1) * 0.02;
            if (dodgeChance > 0.95) dodgeChance = 0.95;

            var enemies = Form.Enemies;
            for (int i = 0, n = enemies.Count; i < n; i++)
            {
                var enemy = enemies[i];
                if (enemy.Body.Count == 0) continue;
                Point head = enemy.Body[0];
                double dxc = head.X - PoisonCenter.X;
                double dyc = head.Y - PoisonCenter.Y;
                double distToCenterSq = dxc * dxc + dyc * dyc;
                double poisonThreshold = PoisonRadius * 0.9;
                if (distToCenterSq > poisonThreshold * poisonThreshold)
                {
                    enemy.Direction = Norm(PoisonCenter.X - head.X, PoisonCenter.Y - head.Y);
                    continue;
                }

                double dxp = head.X - playerHead.X;
                double dyp = head.Y - playerHead.Y;
                double distToPlayerSq = dxp * dxp + dyp * dyp;

                int id = enemy.GetHashCode();

                if (distToPlayerSq < triggerDist * triggerDist)
                {
                    double seed = id * 0.6180339887 + Math.Floor(_aiTime * 2.0);
                    double roll = Math.Abs(seed * 9301 + 49297) % 233280 / 233280.0;

                    if (roll < dodgeChance)
                    {
                        double angle = Math.Atan2(dyp, dxp) + Math.Sin(_aiTime + id) * 0.4;
                        enemy.Direction = new Vector(Math.Cos(angle), Math.Sin(angle));
                    }
                    else
                    {
                        enemy.Direction = Norm(-dxp, -dyp);
                    }
                }
                else
                {
                    double angle = _aiTime * 0.5 + id * 0.7;
                    double radius = 1200 + Math.Sin(_aiTime * 0.3 + id) * 600;
                    double tx = PoisonCenter.X + Math.Cos(angle) * radius;
                    double ty = PoisonCenter.Y + Math.Sin(angle) * radius;
                    enemy.Direction = Norm(tx - head.X, ty - head.Y);
                }
            }
        }

        public override void UpdateAfterMove()
        {
            if (RaceResultTimer > 0)
            {
                RaceResultTimer -= Form.DeltaTime;
                if (RaceResultTimer < 0) RaceResultTimer = 0;
            }
        }

        public override void DrawUI(DrawingContext dc)
        {
            if (Form == null) return;
            double scale = Form.Scale;

            // 毒圈
            var screenCenter = Form.WorldToScreen(PoisonCenter);
            double radius = PoisonRadius * scale;
            if (PoisonRadius > 0)
                dc.DrawEllipse(null, new Pen(Brushes.Red, 3), screenCenter, radius, radius);

            // 道路
            var rs = Form.WorldToScreen(new Point(-_worldWidth / 2, -RoadWidth / 2));
            var re = Form.WorldToScreen(new Point(_worldWidth / 2, RoadWidth / 2));
            dc.DrawRectangle(RenderResources.Brush(255, 211, 211, 211), null, new Rect(rs, re));
            rs = Form.WorldToScreen(new Point(-RoadWidth / 2, -_worldHeight / 2));
            re = Form.WorldToScreen(new Point(RoadWidth / 2, _worldHeight / 2));
            dc.DrawRectangle(RenderResources.Brush(255, 211, 211, 211), null, new Rect(rs, re));

            // ★ 顶部信息卡：半透明黑底 → 任何地图背景下都清晰可见
            var infoCard = new Rect(20 * scale, 20 * scale, 360 * scale, 36 * scale);
            dc.DrawRoundedRectangle(RenderResources.Brush(200, 0, 0, 0), null, infoCard, 8, 8);

            var levelFt = Form.MakeText($"等级 {PlayerLevel}", 14 * scale,
                GetLevelBrush(PlayerLevel), true);
            dc.DrawText(levelFt, new Point(infoCard.X + 12 * scale, infoCard.Y + 9 * scale));

            var infoFt = Form.MakeText(
                $"剩余 {Form.Enemies.Count + 1}   缩圈 {Math.Max(0, (int)PoisonShrinkTimer)}s   按F决斗",
                13 * scale, Brushes.White, false);
            dc.DrawText(infoFt, new Point(infoCard.X + 90 * scale, infoCard.Y + 9 * scale));

            // 敌人标签（仅显示屏幕内的，最多 30 个）
            int shown = 0;
            for (int i = 0; i < Form.Enemies.Count && i < _enemyLevels.Count && shown < 30; i++)
            {
                var enemy = Form.Enemies[i];
                if (enemy.Body.Count == 0) continue;
                var hs = Form.WorldToScreen(enemy.Body[0]);
                if (hs.X < -100 || hs.X > Form.ScreenWidth + 100 ||
                    hs.Y < -50 || hs.Y > Form.ScreenHeight + 50) continue;

                // 标签底色
                var t = Form.MakeText($"{_enemyLevels[i]} {enemy.Name}", 9 * scale,
                    GetLevelBrush(_enemyLevels[i]), true);
                dc.DrawRectangle(RenderResources.Brush(180, 0, 0, 0), null,
                    new Rect(hs.X - 22 * scale, hs.Y - 26 * scale,
                             t.Width + 8 * scale, t.Height + 4 * scale));
                dc.DrawText(t, new Point(hs.X - 20 * scale, hs.Y - 25 * scale));
                shown++;
            }

            if (IsRacing) DrawRaceUI(dc);

            if (RaceResultTimer > 0 && !string.IsNullOrEmpty(RaceResultMessage))
            {
                var msgFt = Form.MakeText(RaceResultMessage, 28 * scale,
                    RaceResultMessage.Contains("胜") ? Brushes.Gold : Brushes.White, true);

                var bg = new Rect(Form.ScreenWidth / 2 - msgFt.Width / 2 - 20 * scale,
                                  Form.ScreenHeight * 0.2 - 10 * scale,
                                  msgFt.Width + 40 * scale, msgFt.Height + 20 * scale);
                dc.DrawRoundedRectangle(RenderResources.Brush(180, 0, 0, 0), null, bg, 10, 10);
                dc.DrawText(msgFt, new Point(Form.ScreenWidth / 2 - msgFt.Width / 2,
                                              Form.ScreenHeight * 0.2));
            }

            DrawMiniMap(dc);
        }

        private void DrawRaceUI(DrawingContext dc)
        {
            double scale = Form.Scale;

            // 目的地
            var destScreen = Form.WorldToScreen(RaceDestination);
            dc.DrawEllipse(Brushes.Gold, new Pen(Brushes.DarkOrange, 3),
                destScreen, 30 * scale, 30 * scale);

            if (Form.Snake.Count > 0)
            {
                var ph = Form.WorldToScreen(Form.Snake[0]);
                var pen = new Pen(Brushes.Gold, 2) { DashStyle = DashStyles.Dash };
                dc.DrawLine(pen, ph, destScreen);
            }
            if (RaceOpponent != null && RaceOpponent.Body.Count > 0)
            {
                var oh = Form.WorldToScreen(RaceOpponent.Body[0]);
                var pen = new Pen(Brushes.Red, 2) { DashStyle = DashStyles.Dash };
                dc.DrawLine(pen, oh, destScreen);
            }

            // 决斗状态卡（顶部中间，不与 MainWindow 的分数卡片重叠）
            var card = new Rect(Form.ScreenWidth / 2 - 250 * scale, 70 * scale,
                                500 * scale, 60 * scale);
            dc.DrawRoundedRectangle(RenderResources.Win11Card, null, card, 8, 8);

            var titleFt = Form.MakeText($"决斗中！{Math.Max(0, RaceTimeout - RaceTimer):F1}s",
                16 * scale, Brushes.DarkRed, true);
            dc.DrawText(titleFt, new Point(card.X + 20 * scale, card.Y + 8 * scale));

            if (Form.Snake.Count > 0 && RaceOpponent != null && RaceOpponent.Body.Count > 0)
            {
                double pd = Distance(Form.Snake[0], RaceDestination);
                double od = Distance(RaceOpponent.Body[0], RaceDestination);
                var distFt = Form.MakeText($"你: {pd:F0}    对手: {od:F0}",
                    14 * scale, RenderResources.Win11Text, false);
                dc.DrawText(distFt, new Point(card.X + 20 * scale, card.Y + 32 * scale));
            }
        }

        private void DrawMiniMap(DrawingContext dc)
        {
            double size = 150 * Form.Scale;
            double x = 20, y = Form.ScreenHeight - size - 20;
            dc.DrawRectangle(Brushes.Black, null, new Rect(x, y, size, size));

            double scaleFactor = size / _worldWidth;
            var miniCenter = new Point(x + size / 2, y + size / 2);
            double pRadius = PoisonRadius * scaleFactor;
            dc.DrawEllipse(null, new Pen(Brushes.Red, 1), miniCenter, pRadius, pRadius);

            if (Form.Snake.Count > 0)
            {
                var playerPos = Form.Snake[0];
                double px = x + (playerPos.X - (-_worldWidth / 2)) / _worldWidth * size;
                double py = y + (playerPos.Y - (-_worldHeight / 2)) / _worldHeight * size;
                dc.DrawEllipse(Brushes.Blue, null, new Point(px, py), 2, 2);
            }

            foreach (var enemy in Form.Enemies.ToList())
            {
                if (enemy.Body.Count == 0) continue;
                var ep = enemy.Body[0];
                double ex = x + (ep.X - (-_worldWidth / 2)) / _worldWidth * size;
                double ey = y + (ep.Y - (-_worldHeight / 2)) / _worldHeight * size;
                dc.DrawEllipse(Brushes.Red, null, new Point(ex, ey), 1, 1);
            }

            if (IsRacing)
            {
                double dx = x + (RaceDestination.X - (-_worldWidth / 2)) / _worldWidth * size;
                double dy = y + (RaceDestination.Y - (-_worldHeight / 2)) / _worldHeight * size;
                dc.DrawEllipse(Brushes.Gold, null, new Point(dx, dy), 3, 3);
            }
        }

        private double Distance(Point a, Point b)
            => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

        private static Vector Norm(double dx, double dy)
        {
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-6) return new Vector(1, 0);
            return new Vector(dx / len, dy / len);
        }

        public static Color GetLevelColor(int level)
        {
            if (level >= 10) return Colors.Gold;
            if (level >= 7) return Colors.Purple;
            if (level >= 4) return Colors.Blue;
            return Colors.Green;
        }
        private static readonly Brush _lvlGreen = new SolidColorBrush(Colors.Green);
        private static readonly Brush _lvlBlue = new SolidColorBrush(Colors.Blue);
        private static readonly Brush _lvlPurple = new SolidColorBrush(Colors.Purple);
        private static readonly Brush _lvlGold = new SolidColorBrush(Colors.Gold);

        static TimedMode()
        {
            _lvlGreen.Freeze();
            _lvlBlue.Freeze();
            _lvlPurple.Freeze();
            _lvlGold.Freeze();
        }

        private static Brush GetLevelBrush(int level)
        {
            if (level >= 10) return _lvlGold;
            if (level >= 7) return _lvlPurple;
            if (level >= 4) return _lvlBlue;
            return _lvlGreen;
        }
    }
}