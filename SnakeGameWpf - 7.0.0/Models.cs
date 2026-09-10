using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SnakeGame
{
    public class Enemy
    {
        public List<Point> Body { get; set; }
        public Vector Direction { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public string Team { get; set; } = "red";
        public int Lives { get; set; } = 1;
        public double SpeedMultiplier { get; set; } = 1.0;
        public double InvincibleTimer { get; set; } = 0;

        public Enemy(Point startPos, string name)
        {
            Body = new List<Point>
            {
                startPos,
                new Point(startPos.X - 16, startPos.Y),
                new Point(startPos.X - 32, startPos.Y)
            };
            Direction = new Vector(1, 0);
            Name = name;
            Score = 0;
            Lives = 1;
            SpeedMultiplier = 1.0;
        }
    }

    public class Food
    {
        public Point Position { get; set; }
        public Color Color { get; set; }
        public double Radius { get; set; }
        public Food(Point pos, Color color, double radius)
        { Position = pos; Color = color; Radius = radius; }
    }

    public class Achievement
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public Achievement(string id, string name, string desc, string icon)
        { Id = id; Name = name; Description = desc; Icon = icon; }
    }

    public class GameStats
    {
        public int TotalKills { get; set; }
        public int TotalGames { get; set; }
        public int CurrentGameKills { get; set; }
        public double CurrentGameDuration { get; set; }
        public int WonGames { get; set; }
        public int CaptureWins { get; set; }
    }

    public class Container
    {
        public Point Position;
        public bool IsSearched;
    }

    public class MapData
    {
        public string Name;
        public Size WorldSize;
        public Point ExtractionPoint;
        public List<Point> ContainerPositions;
        public int EnemyCount;
        public double EnemySpeedMultiplier = 1;
        public int EnemyLives = 1;
        public bool HasFirebombs;
    }

    public class Firebomb
    {
        public Point Position;
        public double Radius;
        public double Duration;
        public double Elapsed;
    }
}