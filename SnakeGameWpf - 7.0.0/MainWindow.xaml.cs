using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SnakeGame
{
    public partial class MainWindow : Window
    {
        #region 常量
        public const string VERSION = "v7.0.0";

        private readonly string VERSION_FILE = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".snake_version_wpf");
        private readonly string NAME_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "player_name.txt");
        private readonly string HIGHSCORE_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "timed_scores.json");
        private readonly string ACHIEVE_FILE = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "achievements.json");

        private const double BASE_WIDTH = 1600;
        private const double BASE_HEIGHT = 900;
        private const double BASE_SPEED_BASE = 4.5;
        private const int BASE_SEGMENT_RADIUS_BASE = 8;
        private const int FOOD_RADIUS_BASE = 6;
        private const int LARGE_FOOD_RADIUS_BASE = 10;
        private const int FOOD_COUNT = 125;
        private const int FOOD_COUNT_EXTREME = 60;
        private const double BOOST_DURATION = 5.0;
        private const double BOOST_DURATION_EXTREME = 2.5;
        private const double BOOST_MULTIPLIER = 2.0;
        private const int ENEMY_COUNT = 10;
        private const int ENEMY_COUNT_EXTREME = 20;
        private const int TIMED_ENEMY_COUNT = 99;
        private const int FOOD_PER_SEGMENT = 3;
        private const int GRID_SIZE_BASE = 40;
        private const double CAPTURE_RADIUS = 150;
        private const double CAPTURE_RATE = 1.0;
        private const double SPAWN_DELAY = 3.0;
        private const double AI_DIFFICULTY_CASUAL = 0.6;
        private const double AI_DIFFICULTY_NORMAL = 0.9;
        private const double AI_DIFFICULTY_EPIC = 1.3;
        private const double AI_DIFFICULTY_EXTREME = 1.5;
        private const int MAX_ENEMIES = 50;
        #endregion

        #region 颜色
        private static readonly Color WIN11_TEXT_PRIMARY = Color.FromRgb(22, 22, 22);
        private static readonly Color WIN11_TEXT_SECONDARY = Color.FromRgb(96, 96, 96);
        private static readonly Color LIGHT_GRAY_BG = Color.FromRgb(245, 245, 245);
        private static readonly Color OVERLAY_DARK = Color.FromArgb(128, 0, 0, 0);
        private static readonly Color OVERLAY_LIGHT = Color.FromArgb(128, 128, 128, 128);
        #endregion

        #region 尺寸 / DPI
        private double _screenWidth;
        private double _screenHeight;
        private double _scale = 1.0;
        private double _dpi = 1.0;

        public double Scale => _scale;
        public double ScreenWidth => _screenWidth;
        public double ScreenHeight => _screenHeight;
        public double DeltaTime => _gameTimer?.Interval.TotalSeconds ?? 0.033;
        public DispatcherTimer GameTimer => _gameTimer;

        private double _worldWidth = 5000;
        private double _worldHeight = 4000;
        private double _worldMinX, _worldMaxX, _worldMinY, _worldMaxY;
        private double _camX, _camY;
        #endregion

        #region 游戏状态
        public List<Point> Snake { get; private set; } = new List<Point>();
        public List<Enemy> Enemies { get; private set; } = new List<Enemy>();
        public List<Food> Foods { get; private set; } = new List<Food>();
        public Vector Direction { get; set; } = new Vector(1, 0);
        public int Score { get; private set; }
        public bool GameActive { get; private set; }
        public bool GameStarted { get; set; }
        public bool Paused { get; set; }
        public bool GameOverScreen { get; set; }
        public bool Boosting { get; set; }
        public DateTime BoostStartTime { get; set; }
        public double SpeedMultiplier = 1.0;
        public string GameMode { get; set; } = "classic";
        public static bool HasQAbility = false;

        // ★ 待显示的更新日志（首次启动检测到版本变化时置 true）
        private bool _pendingChangelog = false;

        private readonly Random _rand = new Random();
        private readonly GameStats _gameStats = new GameStats();
        // ★ 教程弹出期间暂停游戏
        private bool _inTutorial = false;
        private string _playerName = "玩家";
        private readonly Dictionary<string, bool> _achievementsUnlocked = new Dictionary<string, bool>();

        private readonly List<Achievement> Achievements = new List<Achievement>
        {
            new Achievement("first_kill", "初次击杀", "第一次击杀敌人", "1"),
            new Achievement("kill_10", "十人斩", "累计击杀10个敌人", "2"),
            new Achievement("kill_50", "五十人斩", "累计击杀50个敌人", "3"),
            new Achievement("win_10", "常胜将军", "赢得10场游戏", "4"),
            new Achievement("survival_5min", "生存专家", "单局存活超过5分钟", "5"),
            new Achievement("capture_win", "占领专家", "在占领模式中获胜", "6")
        };

        private readonly List<Color> FOOD_COLORS = new List<Color>
        { Colors.Red, Colors.Orange, Colors.Yellow, Colors.Pink, Colors.Purple, Colors.Cyan, Colors.Green };

        private readonly List<Color> HeadColorOptions = new List<Color>
        { Colors.LightGreen, Colors.Yellow, Colors.Orange, Colors.Pink, Colors.Cyan, Colors.White };
        private readonly List<string> HeadColorNames = new List<string> { "浅绿", "黄", "橙", "粉", "青", "白" };

        private readonly List<Color> BodyColorOptions = new List<Color>
        { Colors.Green, Colors.DarkGreen, Colors.Blue, Colors.Purple, Colors.Brown, Colors.Gray };
        private readonly List<string> BodyColorNames = new List<string> { "绿", "深绿", "蓝", "紫", "棕", "灰" };

        private readonly List<string> BackgroundStyleOptions = new List<string> { "纯黑", "纯白", "格子", "星空" };

        private readonly List<Color> AccentColors = new List<Color>
        {
            Color.FromRgb(0, 103, 192), Color.FromRgb(232, 17, 35), Color.FromRgb(0, 138, 0),
            Color.FromRgb(255, 140, 0), Color.FromRgb(107, 105, 214), Color.FromRgb(0, 178, 210)
        };

        private readonly List<Tuple<string, List<string>>> AllChangelogs = new List<Tuple<string, List<string>>>
{
    Tuple.Create("v7.0.0", new List<string>
    {
        "【重大】从 WinForms 全面迁移到 WPF（.NET 10 SDK）",
        "【重大】所有界面统一自绘：设置 / 排行榜 / 成就 / 联机 / 更新日志全部内嵌主窗口",
        "【重大】淘汰之王模式重做：49 名 AI 同场竞技，接近地平线 6 的玩法",

        "淘汰之王：按 F 挑战 600 范围内最近的 AI，双方冲向同一目标点，先到者胜",
        "淘汰之王：击败对手可提升等级（最高 10 级），速度随等级提升",
        "淘汰之王：AI 遇玩家 25% 发起挑战，75% 躲避；玩家等级越高，AI 越倾向躲避",
        "淘汰之王：毒圈每 5 分钟收缩一次，共 3 次；圈外停留 15 秒判负",
        "淘汰之王：新增实时排行、等级颜色标签、缩圈倒计时、决斗倒计时",

        "占领模式：新增屏幕边缘黄色箭头，指向屏幕外的占领区，并显示距离",
        "占领模式：顶部显示进度条与蓝红队剩余人数",

        "设置界面重制：左侧 3 大类标签（个性化 / 画面 / 实验性），右侧内容自适应窗口",
        "设置-个性化：主题（Windows11 / 深色 / 浅色 / 纯白）、背景样式、蛇头色、蛇身色、主题色、自定义背景图",
        "设置-画面：分辨率、显示模式、最高帧数、抗锯齿、垂直同步、高帧率模式，全部实时生效",
        "设置-实验性：双向 IPC 通信、帧速平衡",

        "主题系统：切换主题会实时影响主界面 / 设置 / 排行榜等所有自绘界面的背景与卡片色",
        "自定义背景图：支持 JPG / PNG / BMP / GIF，兼容中文路径，启用/禁用状态独立保存",
        "所有内嵌面板在启用自定义图片时使用半透明卡片，图片可以透出",

        "窗口几何保存：关闭时自动记录大小、位置与最大化状态，下次启动自动恢复",
        "首次启动改为弹出名字输入界面，不再强制弹出经典模式教程",
        "版本号变更时自动弹出更新日志",

        "性能优化：Brush / Pen / Typeface 全局缓存；网格背景改为画线；实时排行每 10 帧才重排一次",
        "修复：淘汰之王决斗胜利后立即结束游戏",
        "修复：淘汰之王部分 UI 被其它 HUD 遮挡",
        "修复：缩圈倒计时与剩余人数在花哨地图背景上看不清（已加半透明黑底）",
        "修复：自定义图片无法显示（改用 FileStream + Uri 双重加载）",
        "修复：自定义背景启用状态未保存到配置文件",
        "修复：设置界面切换后自定义图片未应用",
        "修复：多处 Collection was modified 异常（遍历集合改为快照 .ToList()）",
        "修复：占领模式 AI 阵营逻辑错乱",
        "修复：模式选择选项卡尺寸过小，改为 380×150 并自动换行"
    })
};

        private Color _headColor = Colors.LightGreen;
        private Color _bodyColor = Colors.Green;
        private Color _accentColor = Color.FromRgb(0, 103, 192);
        private int _backgroundStyle = 2;

        // 队伍模式
        private readonly List<Enemy> _blueTeam = new List<Enemy>();
        private readonly List<Enemy> _redTeam = new List<Enemy>();
        private double _captureProgress = 0;
        private Point _captureZone = new Point(0, 0);
        private Color _captureZoneColor = Color.FromArgb(100, 128, 128, 128);

        private readonly List<Point> _spawnPositions = new List<Point>();
        private readonly List<double> _spawnTimers = new List<double>();

        private GameModeBase _currentMode;
        public ExtractionMode ExtractionModeInstance;
        public TimedMode TimedModeInstance;

        public AppSettings AppSettings { get; private set; }
        #endregion

        #region UI 状态
        // 所有界面统一用一个字段集合，全部内嵌主窗口
        public bool ShowModeSelect;
        public bool ShowMapSelect;
        public bool ShowLevelSelect;
        public bool ShowSettings;
        public bool ShowRankings;
        public bool ShowAchievements;
        public bool ShowOnlineLobby;
        public bool ShowChangelog;
        public bool ShowNameInput;

        public int SelectedModeIndex;
        public int SelectedMapIndex;
        public int PauseSelection;
        public int GameoverSelection;

        // 设置界面
        private int _settingsTab = 0;              // 0=个性化 1=画面 2=实验性
        private readonly string[] _settingsTabs = { "个性化", "画面", "实验性" };
        private readonly List<Tuple<Rect, Action>> _settingsHitboxes = new List<Tuple<Rect, Action>>();
        private readonly List<string> ThemeOptions = new List<string> { "Windows11", "深色", "浅色", "纯白" };

        // 更新日志滚动
        private double _changelogScroll = 0;

        // 联机：加入模式输入
        private bool _lobbyJoinMode = false;
        private string _lobbyIp = "127.0.0.1";
        private int _lobbyPort = 8888;
        private int _lobbyEditField = 0; // 0=无 1=IP 2=端口

        public Point MousePosition { get; private set; }
        public Point MousePos => MousePosition;

        private readonly HashSet<Key> _pressedKeys = new HashSet<Key>();
        public bool IsKeyPressed(Key key) => _pressedKeys.Contains(key);

        private int _hoveredMainButton = -1;
        private bool _hoverOnline, _hoverRankings, _hoverAchievements, _hoverChangelog;
        #endregion

        #region 渲染资源
        private readonly TextRenderer _text = new TextRenderer(1.0);
        public FormattedText MakeText(string text, double size, Brush brush, bool bold)
            => _text.Get(text, size, brush, bold);
        #endregion

        #region 网络 / IPC
        private readonly SimpleIpcClient _ipcClient = new SimpleIpcClient();
        private DateTime _lastFpsSend = DateTime.MinValue;
        private const double FPS_SEND_INTERVAL_MS = 200;

        private bool _onlineMode = false;
        private GameServer _server;
        private GameNetworkClient _client;
        private JObject _onlineState;
        private readonly object _onlineLock = new object();
        public bool OnlineMode => _onlineMode;
        #endregion

        private BitmapImage _customBg;
        private DispatcherTimer _gameTimer;
        // ★ 窗口几何恢复标志：OnLoaded 首次设置时不触发保存
        private bool _geometryRestored = false;

        // 排行榜缓存
        private List<Tuple<string, int>> _cachedRanking;
        private int _rankingCacheTick;

        public MainWindow()
        {
            InitializeComponent();

            AppSettings = AppSettings.Load();
            _headColor = AppSettings.HeadColor();
            _bodyColor = AppSettings.BodyColor();
            _accentColor = AppSettings.AccentColor();
            _backgroundStyle = AppSettings.BackgroundStyle;

            LoadPlayerName();
            LoadAchievements();
            CheckVersion();
            LoadCustomBackground();

            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            DpiChanged += OnDpiChanged;
            MouseMove += OnMouseMove;
            MouseDown += OnMouseDown;
            MouseUp += OnMouseUp;
            MouseWheel += OnMouseWheel;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            Closing += OnClosing;
            Closed += OnClosed;

            InputBox.KeyDown += InputBox_KeyDown;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            // 恢复上次窗口几何 → 应用显示设置
            RestoreWindowGeometry();
            ApplyDisplaySettings();
            ApplyAntiAlias();
            ApplyVsync();

            _screenWidth = ActualWidth;
            _screenHeight = ActualHeight;
            RecalculateScale();

            _gameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(1000.0 / 60) };
            _gameTimer.Tick += GameTimer_Tick;
            ApplyFrameRate();
            _gameTimer.Start();

            InitializeGame();

            // 首次启动：名字输入
            bool needNameInput = !File.Exists(NAME_FILE);
            if (needNameInput)
            {
                ShowNameInput = true;
                ActivateInputBox("", "请输入你的名字（Enter 确认）", 20);
            }
            else if (_pendingChangelog)
            {
                // 没有名字输入 → 直接弹更新日志
                ShowChangelog = true;
                _changelogScroll = 0;
                MarkVersionShown();
            }

            _ = System.Threading.Tasks.Task.Run(async () =>
            {
                await _ipcClient.ConnectAsync();
            });

            Focus();
            InvalidateVisual();
        }

        /// <summary>把当前版本号写入版本文件</summary>
        private void MarkVersionShown()
        {
            try { SecureStorage.SaveText(VERSION_FILE, VERSION); } catch { }
            _pendingChangelog = false;
        }

        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveWindowGeometry();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _gameTimer?.Stop();
            _ipcClient?.Dispose();
            _server?.Stop();
            _client?.Disconnect();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            _screenWidth = e.NewSize.Width;
            _screenHeight = e.NewSize.Height;
            RecalculateScale();
            InvalidateVisual();
        }

        private void OnDpiChanged(object sender, DpiChangedEventArgs e)
        {
            _dpi = e.NewDpi.PixelsPerDip;
            _text.Clear();
            RecalculateScale();
            InvalidateVisual();
        }

        private void RecalculateScale()
        {
            double w = _screenWidth > 0 ? _screenWidth : ActualWidth;
            double h = _screenHeight > 0 ? _screenHeight : ActualHeight;
            if (w < 1 || h < 1) return;
            _scale = Math.Max(0.1, Math.Min(w / BASE_WIDTH, h / BASE_HEIGHT));
        }

        #region 世界坐标
        public void SetWorldSizeInternal(double w, double h)
        {
            _worldWidth = w; _worldHeight = h;
            _worldMinX = -w / 2; _worldMaxX = w / 2;
            _worldMinY = -h / 2; _worldMaxY = h / 2;
        }

        public Point WorldToScreen(Point p)
            => new Point(p.X - _camX + _screenWidth / 2, p.Y - _camY + _screenHeight / 2);

        public Point ScreenToWorld(Point p)
            => new Point(p.X - _screenWidth / 2 + _camX, p.Y - _screenHeight / 2 + _camY);

        private void UpdateCamera(Point head) { _camX = head.X; _camY = head.Y; }

        public Point RandomPosition(double margin = 50)
        {
            margin *= _scale;
            double minX = _worldMinX + margin, maxX = _worldMaxX - margin;
            double minY = _worldMinY + margin, maxY = _worldMaxY - margin;
            if (maxX <= minX) maxX = minX + 1;
            if (maxY <= minY) maxY = minY + 1;
            return new Point(
                minX + _rand.NextDouble() * (maxX - minX),
                minY + _rand.NextDouble() * (maxY - minY));
        }
        #endregion

        #region 初始化 / 启动 / 结束
        public void InitializeGame()
        {
            SetWorldSizeInternal(5000, 4000);
            Snake = new List<Point> { new Point(0, 0), new Point(-16, 0), new Point(-32, 0) };
            Direction = new Vector(1, 0);
            Score = 0;
            Foods = new List<Food>();
            Enemies = new List<Enemy>();
            _blueTeam.Clear(); _redTeam.Clear();
            _captureZone = new Point(0, 0);
            _captureProgress = 0;
            _camX = _camY = 0;
            _spawnPositions.Clear(); _spawnTimers.Clear();
            GenerateFoods(GameMode == "extreme" ? FOOD_COUNT_EXTREME : FOOD_COUNT);
            GameActive = true;
            GameStarted = false;
            GameOverScreen = false;
            Paused = false;
            InvalidateVisual();
        }

        public void StartGame()
        {
            GameStarted = true;
            GameActive = true;
            Score = 0;
            _gameStats.CurrentGameKills = 0;
            _gameStats.CurrentGameDuration = 0;

            switch (GameMode)
            {
                case "classic": _currentMode = new ClassicMode(); break;
                case "timed": _currentMode = TimedModeInstance ?? new TimedMode(); break;
                case "team4v4": _currentMode = new TeamMode(); break;
                case "extreme": _currentMode = new ExtremeMode(); break;
                case "extraction": _currentMode = ExtractionModeInstance ?? new ExtractionMode(); break;
                default: _currentMode = new ClassicMode(); break;
            }

            _currentMode?.SetForm(this);
            _currentMode?.Initialize();

            if (GameMode == "timed" && TimedModeInstance != null)
                TimedModeInstance.StartGameAfterSelection();
            else
                SpawnInitialEnemies();

            // ★ 第一次进入该模式时弹教程
            ShowTutorialIfNeeded();

            SpeedMultiplier = 1.0;
            GameOverScreen = false;
            Paused = false;
            InvalidateVisual();
        }

        /// <summary>
        /// 每个模式第一次开始时弹出教程。
        /// 标记文件 tutorial_<mode>.txt 存在即视为已看过。
        /// </summary>
        private void ShowTutorialIfNeeded()
        {
            if (string.IsNullOrEmpty(GameMode)) return;

            string markerFile = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory, $"tutorial_{GameMode}.txt");

            // 已经看过 → 不弹
            if (File.Exists(markerFile)) return;

            // 弹教程（弹出期间暂停游戏逻辑）
            _inTutorial = true;
            try
            {
                var tut = new TutorialWindow(GameMode) { Owner = this };
                tut.ShowDialog();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("教程窗口异常: " + ex.Message);
            }
            finally
            {
                _inTutorial = false;
            }

            // 写入标记（加密存储）
            try { SecureStorage.SaveText(markerFile, "shown"); } catch { }
        }

        public void RestartGame() { InitializeGame(); StartGame(); }

        public void GameOver(bool won = false)
        {
            GameActive = false;
            GameOverScreen = true;
            if (GameMode == "timed" && Score > 0) SaveScore(_playerName, Score);
            if (won) _gameStats.WonGames++;
            InvalidateVisual();
        }
        #endregion

        #region 主循环
        private void GameTimer_Tick(object sender, EventArgs e)
        {
            // 名字输入 / 教程弹出时暂停游戏
            if (ShowNameInput || _inTutorial) { InvalidateVisual(); return; }

            if (_onlineMode)
            {
                if (GameStarted && GameActive && !Paused && !GameOverScreen && _client != null)
                    _ = _client.SendInputAsync(new Point(Direction.X, Direction.Y), Boosting);
                InvalidateVisual();
                return;
            }

            if (!GameStarted || Paused || GameOverScreen)
            {
                InvalidateVisual();
                return;
            }

            if (GameMode == "extraction" && _currentMode is ExtractionMode ext) ext.UpdateBeforeMove();
            if (GameMode == "timed" && _currentMode is TimedMode timed) timed.UpdateBeforeMove();

            UpdateSnake();
            if (Snake.Count > 0) UpdateCamera(Snake[0]);

            if (GameMode == "classic" || GameMode == "extreme")
                HandleEnemySpawning();

            UpdateEnemies();
            _currentMode?.UpdateAI();

            if (GameMode == "extraction" && _currentMode is ExtractionMode ext2) ext2.UpdateAfterMove();
            if (GameMode == "timed" && _currentMode is TimedMode timed2) timed2.UpdateAfterMove();
            if (GameMode == "team4v4" && _currentMode is TeamMode tm) UpdateCaptureProgress(tm);

            CheckAchievements();
            CheckFoodCollision();
            CheckEnemyCollision();
            _gameStats.CurrentGameDuration += DeltaTime;

            if ((DateTime.Now - _lastFpsSend).TotalMilliseconds >= FPS_SEND_INTERVAL_MS)
            {
                _lastFpsSend = DateTime.Now;
                int fps = (int)Math.Round(1.0 / DeltaTime);
                _ = _ipcClient.SendStatusAsync(fps, Score, _gameStats.CurrentGameKills, GameMode);
            }

            InvalidateVisual();
        }

        private void UpdateSnake()
        {
            if (GameMode == "extraction" && _currentMode is ExtractionMode ext && ext.IsPlayerSearching)
                return;

            double speed = GetCurrentSpeed();
            var head = new Point(Snake[0].X + Direction.X * speed, Snake[0].Y + Direction.Y * speed);
            double margin = GetSegmentRadius() * 2;

            if (head.X < _worldMinX + margin || head.X > _worldMaxX - margin ||
                head.Y < _worldMinY + margin || head.Y > _worldMaxY - margin)
            {
                GameOver(false); return;
            }
            Snake.Insert(0, head);
        }

        private void HandleEnemySpawning()
        {
            if (Enemies.Count < MAX_ENEMIES && _rand.NextDouble() < 0.02)
            {
                _spawnPositions.Add(RandomPosition());
                _spawnTimers.Add(0);
            }
            for (int i = _spawnTimers.Count - 1; i >= 0; i--)
            {
                _spawnTimers[i] += DeltaTime;
                if (_spawnTimers[i] >= SPAWN_DELAY)
                {
                    if (Enemies.Count < MAX_ENEMIES)
                        Enemies.Add(new Enemy(_spawnPositions[i], "AI" + _rand.Next(1000)));
                    _spawnTimers.RemoveAt(i);
                    _spawnPositions.RemoveAt(i);
                }
            }
        }

        private void UpdateEnemies()
        {
            double aiDiff = GameMode switch
            {
                "classic" => AI_DIFFICULTY_CASUAL,
                "timed" => AI_DIFFICULTY_EPIC,
                "extreme" => AI_DIFFICULTY_EXTREME,
                "team4v4" => AI_DIFFICULTY_NORMAL,
                _ => 2.5
            };

            for (int i = Enemies.Count - 1; i >= 0; i--)
            {
                var enemy = Enemies[i];
                if (enemy.Body.Count == 0) continue;

                if (_currentMode == null && Snake.Count > 0)
                {
                    var ph = Snake[0];
                    var eh = enemy.Body[0];
                    double dx = ph.X - eh.X, dy = ph.Y - eh.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 1) enemy.Direction = new Vector(dx / len, dy / len);
                }

                double speed = BASE_SPEED_BASE * _scale * aiDiff * enemy.SpeedMultiplier;
                if (enemy.InvincibleTimer > 0) enemy.InvincibleTimer -= DeltaTime;

                var newHead = new Point(
                    enemy.Body[0].X + enemy.Direction.X * speed,
                    enemy.Body[0].Y + enemy.Direction.Y * speed);
                enemy.Body.Insert(0, newHead);

                double segR = GetSegmentRadius();
                for (int j = Foods.Count - 1; j >= 0; j--)
                {
                    double dx = newHead.X - Foods[j].Position.X;
                    double dy = newHead.Y - Foods[j].Position.Y;
                    double rr = segR + Foods[j].Radius;
                    if (dx * dx + dy * dy < rr * rr)
                    {
                        enemy.Score++;
                        enemy.Body.Add(enemy.Body[enemy.Body.Count - 1]);
                        Foods.RemoveAt(j);
                        Foods.Add(CreateFood(FOOD_COLORS[_rand.Next(FOOD_COLORS.Count)]));
                        break;
                    }
                }

                int targetLen = 3 + enemy.Score;
                while (enemy.Body.Count > targetLen && enemy.Body.Count > 3)
                    enemy.Body.RemoveAt(enemy.Body.Count - 1);
            }
        }

        private void CheckFoodCollision()
        {
            if (Snake.Count == 0) return;
            var head = Snake[0];
            double segR = GetSegmentRadius();
            for (int i = Foods.Count - 1; i >= 0; i--)
            {
                double dx = head.X - Foods[i].Position.X;
                double dy = head.Y - Foods[i].Position.Y;
                double rr = segR + Foods[i].Radius;
                if (dx * dx + dy * dy < rr * rr)
                {
                    Score++;
                    Foods.RemoveAt(i);
                    Foods.Add(CreateFood(FOOD_COLORS[_rand.Next(FOOD_COLORS.Count)]));
                    return;
                }
            }
            if (Snake.Count > 1) Snake.RemoveAt(Snake.Count - 1);
        }

        private void CheckEnemyCollision()
        {
            if (GameMode == "timed") return;
            if (Snake.Count == 0) return;

            var snapshot = new List<Enemy>(Enemies);
            var playerHead = Snake[0];
            double segR = GetSegmentRadius();
            double thr = (segR * 2) * (segR * 2);
            var dead = new HashSet<Enemy>();

            foreach (var enemy in snapshot)
            {
                if (GameMode == "team4v4" && enemy.Team == "blue") continue;
                for (int j = 1; j < enemy.Body.Count; j++)
                {
                    double dx = playerHead.X - enemy.Body[j].X;
                    double dy = playerHead.Y - enemy.Body[j].Y;
                    if (dx * dx + dy * dy < thr) { GameOver(false); return; }
                }
            }

            foreach (var enemy in snapshot)
            {
                if (GameMode == "team4v4" && enemy.Team == "blue") continue;
                if (enemy.Body.Count == 0) continue;
                double dx = playerHead.X - enemy.Body[0].X;
                double dy = playerHead.Y - enemy.Body[0].Y;
                if (dx * dx + dy * dy < thr)
                {
                    if (enemy.InvincibleTimer > 0) continue;
                    enemy.Lives--;
                    enemy.InvincibleTimer = 0.5;
                    if (enemy.Lives <= 0 && !dead.Contains(enemy)) KillEnemy(enemy, dead);
                }
            }

            foreach (var enemy in snapshot)
            {
                if (GameMode == "team4v4" && enemy.Team == "blue") continue;
                if (enemy.Body.Count == 0) continue;
                var eh = enemy.Body[0];
                for (int j = 1; j < Snake.Count; j++)
                {
                    double dx = eh.X - Snake[j].X;
                    double dy = eh.Y - Snake[j].Y;
                    if (dx * dx + dy * dy < thr)
                    {
                        if (!dead.Contains(enemy)) KillEnemy(enemy, dead);
                        break;
                    }
                }
            }

            if (GameMode == "classic" || GameMode == "extreme")
            {
                var alive = snapshot.Where(x => !dead.Contains(x)).ToList();
                for (int i = 0; i < alive.Count; i++)
                {
                    var e1 = alive[i];
                    if (e1.Body.Count == 0) continue;
                    for (int k = i + 1; k < alive.Count; k++)
                    {
                        var e2 = alive[k];
                        if (e2.Body.Count == 0) continue;
                        for (int m = 0; m < e2.Body.Count; m++)
                        {
                            double dx = e1.Body[0].X - e2.Body[m].X;
                            double dy = e1.Body[0].Y - e2.Body[m].Y;
                            if (dx * dx + dy * dy < thr)
                            {
                                e2.Lives--;
                                if (e2.Lives <= 0 && !dead.Contains(e2)) KillEnemy(e2, dead);
                                break;
                            }
                        }
                        if (!dead.Contains(e2))
                        {
                            for (int m = 0; m < e1.Body.Count; m++)
                            {
                                double dx = e2.Body[0].X - e1.Body[m].X;
                                double dy = e2.Body[0].Y - e1.Body[m].Y;
                                if (dx * dx + dy * dy < thr)
                                {
                                    e1.Lives--;
                                    if (e1.Lives <= 0 && !dead.Contains(e1)) KillEnemy(e1, dead);
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            Enemies.RemoveAll(x => dead.Contains(x));
            _redTeam.RemoveAll(x => dead.Contains(x));
            _blueTeam.RemoveAll(x => dead.Contains(x));
        }

        private void KillEnemy(Enemy enemy, HashSet<Enemy> dead)
        {
            if (dead.Contains(enemy)) return;
            dead.Add(enemy);
            Score++;
            _gameStats.TotalKills++;
            _gameStats.CurrentGameKills++;
            SpawnFoodsFromEnemy(enemy.Body);
        }

        private double GetSegmentRadius() => BASE_SEGMENT_RADIUS_BASE * _scale;

        private double GetCurrentSpeed()
        {
            double baseSpeed = BASE_SPEED_BASE * _scale;
            double boostDur = GameMode == "extreme" ? BOOST_DURATION_EXTREME : BOOST_DURATION;
            if (Boosting && (DateTime.Now - BoostStartTime).TotalSeconds < boostDur)
                baseSpeed *= BOOST_MULTIPLIER;
            return baseSpeed * SpeedMultiplier;
        }
        #endregion

        #region 占领模式
        private void UpdateCaptureProgress(TeamMode mode)
        {
            if (mode == null || Snake.Count == 0) return;
            int blue = 0, red = 0;
            double r2 = CAPTURE_RADIUS * _scale * (CAPTURE_RADIUS * _scale);

            double dxp = Snake[0].X - _captureZone.X;
            double dyp = Snake[0].Y - _captureZone.Y;
            if (dxp * dxp + dyp * dyp < r2) blue++;

            foreach (var e in _blueTeam)
            {
                if (!Enemies.Contains(e) || e.Body.Count == 0) continue;
                double dx = e.Body[0].X - _captureZone.X;
                double dy = e.Body[0].Y - _captureZone.Y;
                if (dx * dx + dy * dy < r2) blue++;
            }
            foreach (var e in _redTeam)
            {
                if (!Enemies.Contains(e) || e.Body.Count == 0) continue;
                double dx = e.Body[0].X - _captureZone.X;
                double dy = e.Body[0].Y - _captureZone.Y;
                if (dx * dx + dy * dy < r2) red++;
            }

            _captureProgress += (blue - red) * CAPTURE_RATE * DeltaTime;
            _captureProgress = Math.Max(-30, Math.Min(30, _captureProgress));

            if (_captureProgress > 5) _captureZoneColor = Color.FromArgb(100, 0, 0, 255);
            else if (_captureProgress < -5) _captureZoneColor = Color.FromArgb(100, 255, 0, 0);
            else _captureZoneColor = Color.FromArgb(100, 128, 128, 128);

            if (_captureProgress >= 30) { _gameStats.CaptureWins++; GameOver(true); }
            else if (_captureProgress <= -30) GameOver(false);
        }

        public void SetCaptureProgress(double value) => _captureProgress = value;
        #endregion

        #region 食物 / 敌人生成
        private Food CreateFood(Color color, double radius = -1)
        {
            if (radius < 0) radius = FOOD_RADIUS_BASE * _scale;
            double segR = GetSegmentRadius();
            for (int i = 0; i < 50; i++)
            {
                var pos = RandomPosition();
                if (pos.X < _worldMinX + radius || pos.X > _worldMaxX - radius ||
                    pos.Y < _worldMinY + radius || pos.Y > _worldMaxY - radius) continue;
                if (Snake.Any(s => DistSq(s, pos) < Math.Pow(segR + radius, 2))) continue;
                if (Enemies.Any(e => e.Body.Any(s => DistSq(s, pos) < Math.Pow(segR + radius, 2)))) continue;
                return new Food(pos, color, radius);
            }
            return new Food(RandomPosition(), color, radius);
        }

        private void GenerateFoods(int count)
        {
            Foods.Clear();
            for (int i = 0; i < count; i++)
                Foods.Add(CreateFood(FOOD_COLORS[_rand.Next(FOOD_COLORS.Count)]));
        }

        private void SpawnFoodsFromEnemy(List<Point> body)
        {
            foreach (var seg in body)
                for (int i = 0; i < FOOD_PER_SEGMENT; i++)
                    Foods.Add(new Food(
                        new Point(seg.X + (_rand.NextDouble() - 0.5) * 60 * _scale,
                                  seg.Y + (_rand.NextDouble() - 0.5) * 60 * _scale),
                        Colors.Red, LARGE_FOOD_RADIUS_BASE * _scale));
        }

        private void SpawnInitialEnemies()
        {
            Enemies.Clear(); _blueTeam.Clear(); _redTeam.Clear();
            double minDistSq = Math.Pow(GetSegmentRadius() * 4, 2);

            int enemyCount = GameMode == "timed" ? TIMED_ENEMY_COUNT :
                             GameMode == "extreme" ? ENEMY_COUNT_EXTREME :
                             GameMode == "extraction" ? 0 : ENEMY_COUNT;

            if (GameMode == "team4v4")
            {
                _captureZone = new Point(0, 0);
                Snake.Clear();
                Snake.Add(new Point(_worldMaxX - 200 * _scale, 0));
                Snake.Add(new Point(_worldMaxX - 216 * _scale, 0));
                Snake.Add(new Point(_worldMaxX - 232 * _scale, 0));
                Direction = new Vector(-1, 0);

                for (int i = 0; i < 4; i++)
                {
                    var pos = new Point(_worldMinX + 200 * _scale, _worldMinY + 100 * _scale + i * 200 * _scale);
                    var e = new Enemy(pos, "R" + i) { Team = "red", Direction = new Vector(1, 0) };
                    Enemies.Add(e); _redTeam.Add(e);
                }
                for (int i = 0; i < 3; i++)
                {
                    var pos = new Point(_worldMaxX - 200 * _scale, _worldMinY + 150 * _scale + i * 200 * _scale);
                    var e = new Enemy(pos, "B" + i) { Team = "blue", Direction = new Vector(-1, 0) };
                    Enemies.Add(e); _blueTeam.Add(e);
                }
                return;
            }

            if (GameMode == "extraction") return;

            for (int i = 0; i < enemyCount; i++)
            {
                Point pos = RandomPosition();
                for (int j = 0; j < 50; j++)
                {
                    pos = RandomPosition();
                    if (Snake.Any(s => DistSq(s, pos) < minDistSq)) continue;
                    if (Enemies.Any(e => e.Body.Count > 0 && DistSq(e.Body[0], pos) < minDistSq)) continue;
                    break;
                }
                Enemies.Add(new Enemy(pos, "AI" + i));
            }
        }

        public void SpawnExtractionEnemies(int count, double speedMultiplier, int lives)
        {
            Enemies.Clear();
            for (int i = 0; i < count; i++)
            {
                var spawn = RandomPosition();
                Enemies.Add(new Enemy(spawn, "无敌AI")
                {
                    SpeedMultiplier = speedMultiplier,
                    Lives = lives
                });
            }
        }

        public void AddScore(int amount) => Score += amount;
        #endregion

        #region 渲染入口
        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            if (Math.Abs(_screenWidth - ActualWidth) > 0.5 || Math.Abs(_screenHeight - ActualHeight) > 0.5)
            {
                _screenWidth = ActualWidth;
                _screenHeight = ActualHeight;
                RecalculateScale();
            }

            // ★ 统一背景
            bool inGame = GameStarted && !GameOverScreen;

            // ★ 更新日志界面强制使用主题背景，不铺自定义图片
            bool useCustomBg = AppSettings != null
                            && AppSettings.CustomBgEnabled
                            && _customBg != null
                            && !inGame
                            && !ShowChangelog;

            if (useCustomBg)
            {
                dc.DrawImage(_customBg, new Rect(0, 0, _screenWidth, _screenHeight));
            }
            else
            {
                dc.DrawRectangle(
                    RenderResources.Brush(AppSettings?.ThemeBackground() ?? LIGHT_GRAY_BG),
                    null, new Rect(0, 0, _screenWidth, _screenHeight));
            }

            if (_screenWidth < 10 || _screenHeight < 10) return;

            // 优先级：名字输入 > 其他 overlay > 选择界面 > 游戏
            if (ShowNameInput) { DrawNameInput(dc); return; }
            if (ShowSettings) { DrawSettingsInline(dc); return; }
            if (ShowRankings) { DrawRankingsInline(dc); return; }
            if (ShowAchievements) { DrawAchievementsInline(dc); return; }
            if (ShowOnlineLobby) { DrawOnlineLobbyInline(dc); return; }
            if (ShowChangelog) { DrawChangelogInline(dc); return; }
            if (ShowLevelSelect) { DrawLevelSelect(dc); return; }
            if (ShowMapSelect) { DrawMapSelect(dc); return; }
            if (ShowModeSelect) { DrawModeSelect(dc); return; }

            if (!GameStarted) { DrawStartMenu(dc); return; }
            if (_onlineMode) DrawOnlineGame(dc);
            else DrawLocalGame(dc);
        }
        #endregion

        #region 通用控件绘制
        private void DrawRoundedCard(DrawingContext dc, Rect r, Brush bg, double radius = 10, Pen border = null)
        {
            dc.DrawRoundedRectangle(bg, border, r, radius, radius);
        }

        /// <summary>返回面板卡片颜色。启用自定义背景图时更透明，让图片透出来。</summary>
        private Color PanelCardColor()
        {
            if (AppSettings != null && AppSettings.CustomBgEnabled && _customBg != null)
                return Color.FromArgb(190, 255, 255, 255);
            return Color.FromArgb(220, 255, 255, 255);
        }

        private void DrawButtonText(DrawingContext dc, Rect r, string text, double size,
            Brush textBrush, bool hover, bool selected)
        {
            var bg = selected
    ? RenderResources.Brush(_accentColor)
    : (hover ? RenderResources.Brush(255, 240, 240, 240)
              : RenderResources.Brush(PanelCardColor()));
            var border = hover
                ? RenderResources.Pen(_accentColor, 1)
                : RenderResources.Pen(Color.FromRgb(200, 200, 200), 1);
            DrawRoundedCard(dc, r, bg, 10, border);
            var ft = MakeText(text, size,
                selected ? RenderResources.White : RenderResources.Win11Text, false);
            dc.DrawText(ft, new Point(r.X + (r.Width - ft.Width) / 2, r.Y + (r.Height - ft.Height) / 2));
        }

        private void DrawTextCentered(DrawingContext dc, Rect r, string text, double size, Brush brush, bool bold)
        {
            var ft = MakeText(text, size, brush, bold);
            dc.DrawText(ft, new Point(r.X + (r.Width - ft.Width) / 2, r.Y + (r.Height - ft.Height) / 2));
        }

        private void DrawPanelTitle(DrawingContext dc, string text)
        {
            var ft = MakeText(text, 32 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(ft, new Point(_screenWidth / 2 - ft.Width / 2, 24 * _scale));
        }

        private Rect GetBackButtonRect()
            => new Rect(24 * _scale, 24 * _scale, 110 * _scale, 40 * _scale);

        private void DrawBackButton(DrawingContext dc, Action onClick)
        {
            var r = GetBackButtonRect();
            DrawButtonText(dc, r, "返回", 16 * _scale, RenderResources.Win11Text, r.Contains(MousePosition), false);
        }
        #endregion

        #region 开始菜单
        private void DrawStartMenu(DrawingContext dc)
        {
            // 背景已在 OnRender 中统一绘制，这里不再重复

            double cx = _screenWidth / 2;

            // ★ 标题添加半透明卡片背景
            var title = MakeText($"自由贪吃蛇 {VERSION}", 48 * _scale, RenderResources.Win11Text, true);
            double padX = 40 * _scale, padY = 20 * _scale;
            double titleY = 70 * _scale;
            var titleBg = new Rect(cx - title.Width / 2 - padX, titleY,
                                   title.Width + padX * 2, title.Height + padY);
            dc.DrawRoundedRectangle(RenderResources.Brush(220, 255, 255, 255),
                RenderResources.Pen(Color.FromArgb(60, 0, 0, 0), 1),
                titleBg, 14, 14);
            dc.DrawText(title, new Point(cx - title.Width / 2, titleY + padY / 2));

            string[] labels = { "开始游戏 (Space)", "设置 (S)", "退出 (Q)" };
            double[] yOffsets = { -120, -30, 140 };
            double[] widths = { 380, 220, 220 };
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(cx - widths[i] * _scale / 2,
                                 _screenHeight / 2 + yOffsets[i] * _scale,
                                 widths[i] * _scale, 50 * _scale);
                DrawButtonText(dc, r, labels[i], 18 * _scale, RenderResources.Win11Text,
                    _hoveredMainButton == i, false);
            }

            // 联机按钮
            var onlineRect = new Rect(cx - 220 * _scale, _screenHeight / 2 - 180 * _scale, 440 * _scale, 45 * _scale);
            DrawButtonText(dc, onlineRect,
                _onlineMode ? "联机模式：已开启" : "局域网联机 (O键)",
                16 * _scale, RenderResources.Win11Text, _hoverOnline, _onlineMode);

            // 底部按钮
            double bottomY = _screenHeight - 140 * _scale;
            double bw = 180 * _scale, bh = 45 * _scale;
            var rankRect = new Rect(cx - bw * 1.65, bottomY, bw, bh);
            var achRect = new Rect(cx - bw * 0.55, bottomY, bw, bh);
            var logRect = new Rect(cx + bw * 0.55, bottomY, bw, bh);
            DrawButtonText(dc, rankRect, "历史排行榜 (H)", 14 * _scale, RenderResources.Win11Text, _hoverRankings, false);
            DrawButtonText(dc, achRect, "成就 (J)", 14 * _scale, RenderResources.Win11Text, _hoverAchievements, false);
            DrawButtonText(dc, logRect, "更新日志 (L)", 14 * _scale, RenderResources.Win11Text, _hoverChangelog, false);
        }
        #endregion

        #region 内嵌：名字输入
        private void DrawNameInput(DrawingContext dc)
        {
            // 顶部标题
            var title = MakeText("欢迎来到自由贪吃蛇", 40 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(title, new Point(_screenWidth / 2 - title.Width / 2, _screenHeight * 0.25));

            var hint = MakeText("请输入你的名字（最多 20 字符），按 Enter 确认",
                16 * _scale, RenderResources.Win11Text2, false);
            dc.DrawText(hint, new Point(_screenWidth / 2 - hint.Width / 2, _screenHeight * 0.25 + 60 * _scale));
        }
        #endregion

        #region 内嵌：设置
        private void DrawSettingsInline(DrawingContext dc)
        {
            DrawPanelTitle(dc, "设置");
            _settingsHitboxes.Clear();

            // Tab 栏
            double tabW = 130 * _scale, tabH = 44 * _scale, tabGap = 10 * _scale;
            double startX = 40 * _scale, startY = 90 * _scale;
            double maxX = _screenWidth - 40 * _scale;
            double x = startX, y = startY;
            for (int i = 0; i < _settingsTabs.Length; i++)
            {
                if (x + tabW > maxX) { x = startX; y += tabH + tabGap; }
                var r = new Rect(x, y, tabW, tabH);
                int idx = i;
                _settingsHitboxes.Add(Tuple.Create(r, new Action(() => _settingsTab = idx)));
                DrawButtonText(dc, r, _settingsTabs[i], 16 * _scale,
                    RenderResources.Win11Text, r.Contains(MousePosition), _settingsTab == i);
                x += tabW + tabGap;
            }
            double contentY = y + tabH + 24 * _scale;

            // 内容卡片
            var contentRect = new Rect(40 * _scale, contentY,
                                       _screenWidth - 80 * _scale,
                                       _screenHeight - contentY - 80 * _scale);
            DrawRoundedCard(dc, contentRect,
                RenderResources.Brush(PanelCardColor()), 12,
                RenderResources.Pen(AppSettings.ThemeBorder(), 1));

            switch (_settingsTab)
            {
                case 0: DrawPersonalizationPane(dc, contentRect); break;
                case 1: DrawGraphicsPane(dc, contentRect); break;
                case 2: DrawExperimentalPane(dc, contentRect); break;
            }

            DrawBackButton(dc, null);
        }

        // ---------- 个性化面板 ----------
        private void DrawPersonalizationPane(DrawingContext dc, Rect contentRect)
        {
            double px = contentRect.X + 30 * _scale;
            double py = contentRect.Y + 24 * _scale;
            double fullW = contentRect.Width - 60 * _scale;
            double rowH = 46 * _scale;
            double gap = 10 * _scale;

            // —— 主题 ——
            DrawSectionLabel(dc, "主题", px, py);
            py += 30 * _scale;
            {
                double bw = 130 * _scale, bg = 10 * _scale;
                double bx = px;
                foreach (var t in ThemeOptions)
                {
                    var r = new Rect(bx, py, bw, rowH);
                    string theme = t;
                    bool selected = AppSettings.ThemeName == theme;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        AppSettings.ThemeName = theme;
                        AppSettings.Save();
                        InvalidateVisual();
                    })));
                    DrawButtonText(dc, r, theme, 15 * _scale,
                        RenderResources.Win11Text, r.Contains(MousePosition), selected);
                    bx += bw + bg;
                }
            }
            py += rowH + gap * 2;

            // —— 自定义背景图片 ——
            DrawSectionLabel(dc, "自定义背景图片", px, py);
            py += 30 * _scale;
            {
                var r1 = new Rect(px, py, fullW - 200 * _scale, rowH);
                _settingsHitboxes.Add(Tuple.Create(r1, new Action(() =>
                {
                    var ofd = new Microsoft.Win32.OpenFileDialog
                    {
                        Filter = "图像|*.jpg;*.jpeg;*.png;*.bmp;*.gif|所有文件|*.*"
                    };
                    if (ofd.ShowDialog() == true)
                    {
                        AppSettings.CustomBackgroundPath = ofd.FileName;
                        AppSettings.CustomBgEnabled = true;
                        AppSettings.Save();
                        LoadCustomBackground();
                        InvalidateVisual();
                    }
                })));
                string bgInfo = string.IsNullOrEmpty(AppSettings.CustomBackgroundPath)
                    ? "点击选择图片…"
                    : "当前：" + Path.GetFileName(AppSettings.CustomBackgroundPath);
                DrawButtonText(dc, r1, bgInfo, 15 * _scale,
                    RenderResources.Win11Text, r1.Contains(MousePosition), false);

                var r2 = new Rect(px + fullW - 190 * _scale, py, 90 * _scale, rowH);
                _settingsHitboxes.Add(Tuple.Create(r2, new Action(() =>
                {
                    AppSettings.CustomBgEnabled = !AppSettings.CustomBgEnabled;
                    AppSettings.Save();
                    InvalidateVisual();
                })));
                DrawButtonText(dc, r2,
                    AppSettings.CustomBgEnabled ? "已启用" : "已禁用",
                    15 * _scale, RenderResources.Win11Text,
                    r2.Contains(MousePosition), AppSettings.CustomBgEnabled);

                var r3 = new Rect(px + fullW - 90 * _scale, py, 90 * _scale, rowH);
                _settingsHitboxes.Add(Tuple.Create(r3, new Action(() =>
                {
                    AppSettings.CustomBackgroundPath = "";
                    AppSettings.Save();
                    LoadCustomBackground();
                    InvalidateVisual();
                })));
                DrawButtonText(dc, r3, "清除", 15 * _scale,
                    RenderResources.Win11Text, r3.Contains(MousePosition), false);
            }
            py += rowH + gap * 2;

            // —— 背景样式 ——
            DrawSectionLabel(dc, "背景样式", px, py);
            py += 30 * _scale;
            {
                double bw = 110 * _scale, bg = 10 * _scale;
                double bx = px;
                for (int i = 0; i < BackgroundStyleOptions.Count; i++)
                {
                    var r = new Rect(bx, py, bw, rowH);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        _backgroundStyle = idx;
                        AppSettings.BackgroundStyle = idx;
                        AppSettings.Save();
                    })));
                    DrawButtonText(dc, r, BackgroundStyleOptions[i], 15 * _scale,
                        RenderResources.Win11Text, r.Contains(MousePosition),
                        _backgroundStyle == i);
                    bx += bw + bg;
                }
            }
            py += rowH + gap * 2;

            // —— 蛇头颜色 ——
            DrawSectionLabel(dc, "蛇头颜色", px, py);
            py += 30 * _scale;
            {
                double sw = 70 * _scale, sh = 36 * _scale, sg = 8 * _scale;
                double bx = px;
                for (int i = 0; i < HeadColorOptions.Count; i++)
                {
                    var r = new Rect(bx, py, sw, sh);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        _headColor = HeadColorOptions[idx];
                        AppSettings.HeadColorHex = ColorToHex(_headColor);
                        AppSettings.Save();
                    })));
                    // 画色块 + 边框
                    var brush = RenderResources.Brush(HeadColorOptions[i]);
                    dc.DrawRoundedRectangle(brush, null, r, 6, 6);
                    var border = _headColor == HeadColorOptions[i]
                        ? RenderResources.Pen(_accentColor, 3)
                        : RenderResources.Pen(Color.FromRgb(180, 180, 180), 1);
                    dc.DrawRoundedRectangle(null, border, r, 6, 6);
                    bx += sw + sg;
                }
            }
            py += rowH + gap * 2;

            // —— 蛇身颜色 ——
            DrawSectionLabel(dc, "蛇身颜色", px, py);
            py += 30 * _scale;
            {
                double sw = 70 * _scale, sh = 36 * _scale, sg = 8 * _scale;
                double bx = px;
                for (int i = 0; i < BodyColorOptions.Count; i++)
                {
                    var r = new Rect(bx, py, sw, sh);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        _bodyColor = BodyColorOptions[idx];
                        AppSettings.BodyColorHex = ColorToHex(_bodyColor);
                        AppSettings.Save();
                    })));
                    var brush = RenderResources.Brush(BodyColorOptions[i]);
                    dc.DrawRoundedRectangle(brush, null, r, 6, 6);
                    var border = _bodyColor == BodyColorOptions[i]
                        ? RenderResources.Pen(_accentColor, 3)
                        : RenderResources.Pen(Color.FromRgb(180, 180, 180), 1);
                    dc.DrawRoundedRectangle(null, border, r, 6, 6);
                    bx += sw + sg;
                }
            }
            py += rowH + gap * 2;

            // —— 主题色 ——
            DrawSectionLabel(dc, "主题色", px, py);
            py += 30 * _scale;
            {
                double sw = 70 * _scale, sh = 36 * _scale, sg = 8 * _scale;
                double bx = px;
                for (int i = 0; i < AccentColors.Count; i++)
                {
                    var r = new Rect(bx, py, sw, sh);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        _accentColor = AccentColors[idx];
                        AppSettings.AccentColorHex = ColorToHex(_accentColor);
                        AppSettings.Save();
                    })));
                    var brush = RenderResources.Brush(AccentColors[i]);
                    dc.DrawRoundedRectangle(brush, null, r, 6, 6);
                    var border = _accentColor == AccentColors[i]
                        ? RenderResources.Pen(Colors.White, 3)
                        : RenderResources.Pen(Color.FromRgb(180, 180, 180), 1);
                    dc.DrawRoundedRectangle(null, border, r, 6, 6);
                    bx += sw + sg;
                }
            }
        }

        // ---------- 画面面板 ----------
        private void DrawGraphicsPane(DrawingContext dc, Rect contentRect)
        {
            double px = contentRect.X + 30 * _scale;
            double py = contentRect.Y + 30 * _scale;
            double rowW = contentRect.Width - 60 * _scale;
            double rowH = 46 * _scale;
            double gap = 10 * _scale;

            // 分辨率
            DrawSectionLabel(dc, "分辨率", px, py);
            py += 30 * _scale;
            {
                var resolutions = new[] {
            (800,600),(1024,768),(1280,720),(1600,900),(1920,1080),(2560,1440),(3840,2160)
        };
                double bw = 130 * _scale, bg = 10 * _scale;
                double bx = px;
                for (int i = 0; i < resolutions.Length; i++)
                {
                    if (bx + bw > px + rowW) { bx = px; py += rowH + 6 * _scale; }
                    var r = new Rect(bx, py, bw, rowH);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        AppSettings.ResolutionIndex = idx;
                        AppSettings.Save();
                        _geometryRestored = false;      // ★ 用户主动改分辨率 → 用预设尺寸
                        ApplyDisplaySettings();
                        InvalidateVisual();
                    })));
                    string text = $"{resolutions[i].Item1}×{resolutions[i].Item2}";
                    DrawButtonText(dc, r, text, 13 * _scale,
                        RenderResources.Win11Text, r.Contains(MousePosition),
                        AppSettings.ResolutionIndex == i);
                    bx += bw + bg;
                }
            }
            py += rowH + gap * 2;

            // 显示模式
            DrawSectionLabel(dc, "显示模式", px, py);
            py += 30 * _scale;
            {
                string[] modes = { "无边框全屏", "窗口化(无边框)", "窗口化" };
                double bw = 150 * _scale, bg = 10 * _scale;
                double bx = px;
                for (int i = 0; i < modes.Length; i++)
                {
                    var r = new Rect(bx, py, bw, rowH);
                    int idx = i;
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        AppSettings.DisplayMode = idx;
                        AppSettings.Save();
                        _geometryRestored = false;      // ★ 切换模式 → 重新套用预设尺寸
                        ApplyDisplaySettings();
                        InvalidateVisual();
                    })));
                    DrawButtonText(dc, r, modes[i], 14 * _scale,
                        RenderResources.Win11Text, r.Contains(MousePosition),
                        AppSettings.DisplayMode == i);
                    bx += bw + bg;
                }
            }
            py += rowH + gap * 2;

            // 最高帧数
            DrawSectionLabel(dc, "最高帧数", px, py);
            py += 30 * _scale;
            {
                int[] fpsList = { 30, 60, 120, 144, 240, 300 };
                double bw = 80 * _scale, bg = 10 * _scale;
                double bx = px;
                for (int i = 0; i < fpsList.Length; i++)
                {
                    var r = new Rect(bx, py, bw, rowH);
                    int fps = fpsList[i];
                    _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
                    {
                        AppSettings.FpsTarget = fps;
                        AppSettings.Save();
                        ApplyFrameRate();           // ★ 立即生效
                        InvalidateVisual();
                    })));
                    DrawButtonText(dc, r, fps.ToString(), 15 * _scale,
                        RenderResources.Win11Text, r.Contains(MousePosition),
                        AppSettings.FpsTarget == fps);
                    bx += bw + bg;
                }
            }
            py += rowH + gap * 2;

            // 抗锯齿
            AddToggleRow(dc, px, ref py, rowW, rowH, gap, "抗锯齿",
                () => AppSettings.AntiAlias,
                v =>
                {
                    AppSettings.AntiAlias = v;
                    AppSettings.Save();
                    ApplyAntiAlias();               // ★ 立即生效
                });

            // 垂直同步
            AddToggleRow(dc, px, ref py, rowW, rowH, gap, "垂直同步",
                () => AppSettings.Vsync,
                v =>
                {
                    AppSettings.Vsync = v;
                    AppSettings.Save();
                    ApplyVsync();                   // ★ 立即生效
                });

            // 高帧率模式（仅在勾选时让帧率上限生效）
            AddToggleRow(dc, px, ref py, rowW, rowH, gap, "高帧率模式",
                () => AppSettings.HighFpsEnabled,
                v =>
                {
                    AppSettings.HighFpsEnabled = v;
                    AppSettings.Save();
                    ApplyFrameRate();               // ★ 立即生效
                });
        }

        // ---------- 实验性面板 ----------
        private void DrawExperimentalPane(DrawingContext dc, Rect contentRect)
        {
            double px = contentRect.X + 30 * _scale;
            double py = contentRect.Y + 30 * _scale;
            double rowW = contentRect.Width - 60 * _scale;
            double rowH = 46 * _scale;
            double gap = 10 * _scale;

            AddToggleRow(dc, px, ref py, rowW, rowH, gap, "双向 IPC 通信",
                () => AppSettings.BidirectionalIpc, v => { AppSettings.BidirectionalIpc = v; AppSettings.Save(); });
            AddToggleRow(dc, px, ref py, rowW, rowH, gap, "帧速平衡",
                () => AppSettings.SpeedBalance, v => { AppSettings.SpeedBalance = v; AppSettings.Save(); });
        }

        // ---------- 通用控件 ----------
        private void DrawSectionLabel(DrawingContext dc, string text, double x, double y)
        {
            var ft = MakeText(text, 16 * _scale, RenderResources.Win11Text2, true);
            dc.DrawText(ft, new Point(x, y));
        }

        private void AddToggleRow(DrawingContext dc, double px, ref double py,
            double rowW, double rowH, double gap,
            string label, Func<bool> getter, Action<bool> setter)
        {
            var r = new Rect(px, py, rowW, rowH);
            _settingsHitboxes.Add(Tuple.Create(r, new Action(() =>
            {
                setter(!getter());
                InvalidateVisual();
            })));
            bool on = getter();
            DrawButtonText(dc, r, $"{label}：{(on ? "开" : "关")}", 15 * _scale,
                RenderResources.Win11Text, r.Contains(MousePosition), on);
            py += rowH + gap;
        }
        #endregion

        #region 内嵌：排行榜
        private void DrawRankingsInline(DrawingContext dc)
        {
            DrawPanelTitle(dc, "淘汰之王历史排行榜");

            var card = new Rect(80 * _scale, 100 * _scale,
                                _screenWidth - 160 * _scale, _screenHeight - 200 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Brush(PanelCardColor()), 12,
    RenderResources.Pen(AppSettings.ThemeBorder(), 1));

            var scores = LoadScores();
            double y = card.Y + 20 * _scale;
            int shown = Math.Min(scores.Count, 15);
            var tf = MakeText("排名", 18 * _scale, RenderResources.Win11Text2, true);
            dc.DrawText(tf, new Point(card.X + 30 * _scale, y));
            tf = MakeText("玩家", 18 * _scale, RenderResources.Win11Text2, true);
            dc.DrawText(tf, new Point(card.X + 120 * _scale, y));
            tf = MakeText("分数", 18 * _scale, RenderResources.Win11Text2, true);
            dc.DrawText(tf, new Point(card.Right - 150 * _scale, y));
            y += 32 * _scale;

            for (int i = 0; i < shown; i++)
            {
                string name = scores[i][0]?.ToString() ?? "?";
                int sc = Convert.ToInt32(scores[i][1]);
                bool isMe = name == _playerName;

                var rankFt = MakeText($"{i + 1}", 16 * _scale,
                    isMe ? RenderResources.Green : RenderResources.Win11Text, isMe);
                dc.DrawText(rankFt, new Point(card.X + 30 * _scale, y));

                var nameFt = MakeText(name, 16 * _scale,
                    isMe ? RenderResources.Green : RenderResources.Win11Text, isMe);
                dc.DrawText(nameFt, new Point(card.X + 120 * _scale, y));

                var scFt = MakeText(sc.ToString(), 16 * _scale,
                    isMe ? RenderResources.Green : RenderResources.Win11Text, isMe);
                dc.DrawText(scFt, new Point(card.Right - 150 * _scale, y));

                y += 30 * _scale;
            }

            if (scores.Count == 0)
                DrawTextCentered(dc, card, "暂无记录", 20 * _scale, RenderResources.Win11Text2, false);

            DrawBackButton(dc, null);
        }
        #endregion

        #region 内嵌：成就
        private void DrawAchievementsInline(DrawingContext dc)
        {
            DrawPanelTitle(dc, "成就");

            var card = new Rect(80 * _scale, 100 * _scale,
                                _screenWidth - 160 * _scale, _screenHeight - 200 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Brush(220, 255, 255, 255), 12,
                RenderResources.Pen(Color.FromRgb(220, 220, 220), 1));

            double y = card.Y + 30 * _scale;
            foreach (var a in Achievements)
            {
                bool ok = _achievementsUnlocked.TryGetValue(a.Id, out var v) && v;
                var iconFt = MakeText(ok ? "✔" : "✘", 22 * _scale,
                    ok ? RenderResources.Green : RenderResources.Gray, false);
                dc.DrawText(iconFt, new Point(card.X + 30 * _scale, y));

                var nameFt = MakeText(a.Name, 18 * _scale,
                    ok ? RenderResources.Green : RenderResources.Win11Text, true);
                dc.DrawText(nameFt, new Point(card.X + 80 * _scale, y));

                var descFt = MakeText(a.Description, 14 * _scale,
                    RenderResources.Win11Text2, false);
                dc.DrawText(descFt, new Point(card.X + 80 * _scale, y + 26 * _scale));

                y += 60 * _scale;
            }

            DrawBackButton(dc, null);
        }
        #endregion

        #region 内嵌：更新日志
        private void DrawChangelogInline(DrawingContext dc)
        {
            DrawPanelTitle(dc, "更新日志");

            var card = new Rect(80 * _scale, 100 * _scale,
                                _screenWidth - 160 * _scale, _screenHeight - 200 * _scale);
            RenderResources.Brush(PanelCardColor());
            RenderResources.Pen(Color.FromRgb(220, 220, 220), 1);

            double y = card.Y + 30 * _scale - _changelogScroll * _scale;
            foreach (var log in AllChangelogs)
            {
                var verFt = MakeText($"版本 {log.Item1}", 22 * _scale, RenderResources.Win11Text, true);
                dc.DrawText(verFt, new Point(card.X + 30 * _scale, y));
                y += 36 * _scale;

                foreach (var item in log.Item2)
                {
                    var itemFt = MakeText("• " + item, 15 * _scale, RenderResources.Win11Text2, false);
                    dc.DrawText(itemFt, new Point(card.X + 50 * _scale, y));
                    y += 26 * _scale;
                }
                y += 20 * _scale;
            }

            DrawBackButton(dc, null);
        }
        #endregion

        #region 内嵌：联机大厅
        private void DrawOnlineLobbyInline(DrawingContext dc)
        {
            DrawPanelTitle(dc, "局域网联机");

            var card = new Rect(_screenWidth / 2 - 300 * _scale, 120 * _scale,
                                600 * _scale, 400 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Brush(PanelCardColor()), 16,
    RenderResources.Pen(AppSettings.ThemeBorder(), 1));

            double px = card.X + 40 * _scale;
            double py = card.Y + 40 * _scale;
            double rowW = card.Width - 80 * _scale;
            double rowH = 50 * _scale;

            // 主机模式
            var hostRect = new Rect(px, py, rowW, rowH);
            DrawButtonText(dc, hostRect, "创建房间（作为主机，端口 8888）", 16 * _scale,
                RenderResources.Win11Text, hostRect.Contains(MousePosition), !_lobbyJoinMode);

            // 加入模式
            var joinRect = new Rect(px, py + rowH + 12 * _scale, rowW, rowH);
            DrawButtonText(dc, joinRect, "加入房间", 16 * _scale,
                RenderResources.Win11Text, joinRect.Contains(MousePosition), _lobbyJoinMode);

            if (_lobbyJoinMode)
            {
                var ipRect = new Rect(px, py + (rowH + 12 * _scale) * 2, rowW, rowH);
                DrawRoundedCard(dc, ipRect, Brushes.White, 8,
                    RenderResources.Pen(_lobbyEditField == 1 ? _accentColor : Color.FromRgb(200, 200, 200), 2));
                DrawTextCentered(dc, ipRect, $"IP: {_lobbyIp}", 16 * _scale, RenderResources.Win11Text, false);

                var portRect = new Rect(px, py + (rowH + 12 * _scale) * 3, rowW, rowH);
                DrawRoundedCard(dc, portRect, Brushes.White, 8,
                    RenderResources.Pen(_lobbyEditField == 2 ? _accentColor : Color.FromRgb(200, 200, 200), 2));
                DrawTextCentered(dc, portRect, $"端口: {_lobbyPort}", 16 * _scale, RenderResources.Win11Text, false);
            }

            // 提示
            var hint = MakeText("点击“创建房间”或“加入房间”后会自动连接服务器", 14 * _scale,
                RenderResources.Win11Text2, false);
            dc.DrawText(hint, new Point(card.X + 40 * _scale, card.Bottom - 60 * _scale));

            DrawBackButton(dc, null);
        }
        #endregion

        #region 渲染：本地游戏
        private void DrawLocalGame(DrawingContext dc)
        {
            DrawBackground(dc);
            DrawFoods(dc);
            DrawSnake(dc);
            DrawEnemies(dc);
            DrawHUD(dc);
            _currentMode?.DrawUI(dc);

            if (GameMode == "team4v4" && _currentMode is TeamMode)
            {
                DrawCaptureUI(dc);
                DrawCaptureZoneArrow(dc);
            }
            else if (GameMode == "timed" && _currentMode is TimedMode)
            {
                DrawTimedRanking(dc);
            }

            if (Paused) DrawPauseMenu(dc);
            if (GameOverScreen) DrawGameOverScreen(dc);
        }

        private void DrawBackground(DrawingContext dc)
        {
            switch (_backgroundStyle)
            {
                case 0:
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, _screenWidth, _screenHeight));
                    break;
                case 1:
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, _screenWidth, _screenHeight));
                    break;
                case 2:
                    // ★ 性能优化：只画线，不填充矩形
                    dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, _screenWidth, _screenHeight));
                    double gridSize = GRID_SIZE_BASE * _scale;
                    double left = _camX - _screenWidth / 2;
                    double top = _camY - _screenHeight / 2;
                    double startX = Math.Floor(left / gridSize) * gridSize;
                    double startY = Math.Floor(top / gridSize) * gridSize;
                    var gridPen = RenderResources.Pen(LIGHT_GRAY_BG, 1);
                    for (double x = startX; x < _camX + _screenWidth / 2; x += gridSize)
                    {
                        var sp = WorldToScreen(new Point(x, _camY));
                        dc.DrawLine(gridPen, new Point(sp.X, 0), new Point(sp.X, _screenHeight));
                    }
                    for (double y = startY; y < _camY + _screenHeight / 2; y += gridSize)
                    {
                        var sp = WorldToScreen(new Point(_camX, y));
                        dc.DrawLine(gridPen, new Point(0, sp.Y), new Point(_screenWidth, sp.Y));
                    }
                    break;
                case 3:
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0, 0, _screenWidth, _screenHeight));
                    for (int i = 0; i < 100; i++)
                    {
                        byte r = (byte)_rand.Next(100, 255);
                        byte g = (byte)_rand.Next(100, 255);
                        byte b = (byte)_rand.Next(100, 255);
                        dc.DrawRectangle(RenderResources.Brush(255, r, g, b), null,
                            new Rect(_rand.Next(0, (int)_screenWidth), _rand.Next(0, (int)_screenHeight), 1, 1));
                    }
                    break;
            }

            if (GameMode == "team4v4")
            {
                var sp = WorldToScreen(_captureZone);
                double r = CAPTURE_RADIUS * _scale;
                dc.DrawEllipse(RenderResources.Brush(_captureZoneColor), null, sp, r, r);
                dc.DrawEllipse(null, RenderResources.Pen(Colors.Black, 1), sp, r, r);
            }

            if (GameMode == "classic" || GameMode == "extreme")
            {
                for (int i = 0; i < _spawnPositions.Count; i++)
                {
                    var sp = WorldToScreen(_spawnPositions[i]);
                    double progress = _spawnTimers[i] / SPAWN_DELAY;
                    byte alpha = (byte)(200 * (1 - progress));
                    double r = 30 * _scale * progress;
                    if (r > 0)
                        dc.DrawEllipse(RenderResources.Brush(alpha, 255, 0, 0), null, sp, r, r);
                }
            }

            var sMin = WorldToScreen(new Point(_worldMinX, _worldMinY));
            var sMax = WorldToScreen(new Point(_worldMaxX, _worldMaxY));
            dc.DrawRectangle(null, RenderResources.Pen(Colors.White, 2),
                new Rect(sMin.X, sMin.Y, sMax.X - sMin.X, sMax.Y - sMin.Y));
        }

        private void DrawFoods(DrawingContext dc)
        {
            foreach (var food in Foods)
            {
                var sp = WorldToScreen(food.Position);
                if (sp.X < -20 || sp.X > _screenWidth + 20 || sp.Y < -20 || sp.Y > _screenHeight + 20) continue;
                // ★ 用缓存画刷
                dc.DrawEllipse(RenderResources.Brush(food.Color), null, sp, food.Radius, food.Radius);
            }
        }

        private void DrawSnake(DrawingContext dc)
        {
            double segR = GetSegmentRadius();
            var headBrush = RenderResources.Brush(_headColor);
            var bodyBrush = RenderResources.Brush(_bodyColor);
            for (int i = 0; i < Snake.Count; i++)
            {
                var sp = WorldToScreen(Snake[i]);
                if (sp.X < -segR * 2 || sp.X > _screenWidth + segR * 2 ||
                    sp.Y < -segR * 2 || sp.Y > _screenHeight + segR * 2) continue;
                dc.DrawEllipse(i == 0 ? headBrush : bodyBrush, null, sp, segR, segR);
            }
        }

        private void DrawEnemies(DrawingContext dc)
        {
            double segR = GetSegmentRadius();
            var redBrush = RenderResources.Brush(Colors.Red);
            var blueBrush = RenderResources.Brush(Colors.Blue);
            foreach (var enemy in Enemies.ToList())
            {
                var brush = enemy.Team == "blue" ? blueBrush : redBrush;
                foreach (var seg in enemy.Body.ToList())
                {
                    var sp = WorldToScreen(seg);
                    if (sp.X < -segR * 2 || sp.X > _screenWidth + segR * 2 ||
                        sp.Y < -segR * 2 || sp.Y > _screenHeight + segR * 2) continue;
                    dc.DrawEllipse(brush, null, sp, segR, segR);
                }
            }
        }

        private void DrawHUD(DrawingContext dc)
        {
            // ★ 淘汰之王模式：HUD 完全由 TimedMode.DrawUI 绘制，避免与分数/击杀卡片重叠
            if (GameMode == "timed")
            {
                var fpsCardT = new Rect(_screenWidth - 120 * _scale, _screenHeight - 50 * _scale,
                                        100 * _scale, 30 * _scale);
                DrawRoundedCard(dc, fpsCardT, RenderResources.Win11Card, 6);
                int fpsT = (int)Math.Round(1.0 / DeltaTime);
                var ftT = MakeText($"FPS: {fpsT}", 14 * _scale, RenderResources.Win11Text, false);
                dc.DrawText(ftT, new Point(fpsCardT.X + 5 * _scale, fpsCardT.Y + 5 * _scale));
                return;
            }

            var scoreCard = new Rect(_screenWidth - 220 * _scale, 20 * _scale, 200 * _scale, 40 * _scale);
            DrawRoundedCard(dc, scoreCard, RenderResources.Win11Card, 8);
            var ft = MakeText($"分数: {Score}", 16 * _scale, RenderResources.Win11Text, false);
            dc.DrawText(ft, new Point(scoreCard.X + 10 * _scale, scoreCard.Y + 10 * _scale));

            var killsCard = new Rect(20 * _scale, 20 * _scale, 140 * _scale, 40 * _scale);
            DrawRoundedCard(dc, killsCard, RenderResources.Win11Card, 8);
            ft = MakeText($"击杀: {_gameStats.CurrentGameKills}", 16 * _scale, RenderResources.Win11Text, false);
            dc.DrawText(ft, new Point(killsCard.X + 10 * _scale, killsCard.Y + 10 * _scale));

            var fpsCard = new Rect(_screenWidth - 120 * _scale, _screenHeight - 50 * _scale,
                                   100 * _scale, 30 * _scale);
            DrawRoundedCard(dc, fpsCard, RenderResources.Win11Card, 6);
            int fps = (int)Math.Round(1.0 / DeltaTime);
            ft = MakeText($"FPS: {fps}", 14 * _scale, RenderResources.Win11Text, false);
            dc.DrawText(ft, new Point(fpsCard.X + 5 * _scale, fpsCard.Y + 5 * _scale));
        }

        private void DrawCaptureUI(DrawingContext dc)
        {
            var card = new Rect(_screenWidth / 2 - 250 * _scale, 20 * _scale, 500 * _scale, 60 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Win11Card, 8);

            double barW = 400 * _scale, barH = 20 * _scale;
            double barX = card.X + (card.Width - barW) / 2;
            double barY = card.Y + 10 * _scale;

            dc.DrawRectangle(RenderResources.Gray, null, new Rect(barX, barY, barW, barH));
            double blueW = (30 + _captureProgress) / 60.0 * barW;
            double redW = (30 - _captureProgress) / 60.0 * barW;
            dc.DrawRectangle(Brushes.Blue, null, new Rect(barX, barY, blueW, barH));
            dc.DrawRectangle(Brushes.Red, null, new Rect(barX + barW - redW, barY, redW, barH));
            dc.DrawRectangle(null, RenderResources.Pen(Colors.Black, 1), new Rect(barX, barY, barW, barH));

            int blueAlive = 1 + _blueTeam.Count(e => Enemies.Contains(e));
            int redAlive = _redTeam.Count(e => Enemies.Contains(e));
            var bf = MakeText($"蓝队: {blueAlive}人", 14 * _scale, Brushes.Blue, false);
            var rf = MakeText($"红队: {redAlive}人", 14 * _scale, Brushes.Red, false);
            dc.DrawText(bf, new Point(card.X + 10 * _scale, card.Y + 35 * _scale));
            dc.DrawText(rf, new Point(card.Right - 100 * _scale, card.Y + 35 * _scale));
        }

        private void DrawCaptureZoneArrow(DrawingContext dc)
        {
            if (Snake.Count == 0) return;

            var playerHead = Snake[0];
            var screenPos = WorldToScreen(_captureZone);
            double zoneRadius = CAPTURE_RADIUS * _scale;

            // 占领区已在屏幕内 → 不画箭头
            var zoneRect = new Rect(screenPos.X - zoneRadius, screenPos.Y - zoneRadius,
                                    zoneRadius * 2, zoneRadius * 2);
            var screenRect = new Rect(0, 0, _screenWidth, _screenHeight);
            if (screenRect.IntersectsWith(zoneRect)) return;

            double dx = _captureZone.X - playerHead.X;
            double dy = _captureZone.Y - playerHead.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist < 1) return;
            double dirX = dx / dist;
            double dirY = dy / dist;

            // 屏幕边缘交点
            double cx = _screenWidth / 2;
            double cy = _screenHeight / 2;
            double t = double.MaxValue;
            if (dirX > 0) t = (_screenWidth - cx) / dirX;
            else if (dirX < 0) t = -cx / dirX;
            double ty = cy + dirY * t;
            double edgeX, edgeY;
            if (ty >= 0 && ty <= _screenHeight)
            {
                edgeX = dirX > 0 ? _screenWidth : 0;
                edgeY = ty;
            }
            else
            {
                if (dirY > 0) t = (_screenHeight - cy) / dirY;
                else t = -cy / dirY;
                edgeX = cx + dirX * t;
                edgeY = dirY > 0 ? _screenHeight : 0;
                edgeX = Math.Max(0, Math.Min(_screenWidth, edgeX));
            }

            // 留 40px 边距
            double margin = 40 * _scale;
            edgeX = Math.Max(margin, Math.Min(_screenWidth - margin, edgeX));
            edgeY = Math.Max(margin, Math.Min(_screenHeight - margin, edgeY));

            // 旋转箭头
            double arrowSize = 22 * _scale;
            double angle = Math.Atan2(dirY, dirX) * 180 / Math.PI;

            dc.PushTransform(new RotateTransform(angle + 90, edgeX, edgeY));

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(new Point(edgeX, edgeY - arrowSize), true, true);
                ctx.LineTo(new Point(edgeX - arrowSize * 0.55, edgeY + arrowSize * 0.5), true, false);
                ctx.LineTo(new Point(edgeX + arrowSize * 0.55, edgeY + arrowSize * 0.5), true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(Brushes.Yellow, new Pen(Brushes.Black, 2), geo);

            // 距离文字
            var distFt = MakeText($"{dist * _scale:F0}", 14 * _scale, Brushes.Yellow, true);
            dc.DrawText(distFt, new Point(edgeX - distFt.Width / 2,
                                           edgeY + arrowSize + 4 * _scale));

            dc.Pop();
        }

        private void DrawTimedRanking(DrawingContext dc)
        {
            // ★ 每 10 帧才重新排序一次，减少 CPU 开销
            _rankingCacheTick++;
            if (_cachedRanking == null || _rankingCacheTick % 10 == 0)
            {
                var all = new List<Tuple<string, int>>();
                all.Add(Tuple.Create(_playerName, Score));
                foreach (var e in Enemies) all.Add(Tuple.Create(e.Name, e.Score));
                all.Sort((a, b) => b.Item2.CompareTo(a.Item2));
                _cachedRanking = all;
            }

            var card = new Rect(_screenWidth - 220 * _scale, 70 * _scale, 200 * _scale, 200 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Win11Card, 8);

            var titleFt = MakeText($"实时排行 (共{_cachedRanking.Count}人)", 13 * _scale,
                RenderResources.Win11Text, false);
            dc.DrawText(titleFt, new Point(card.X + 10 * _scale, card.Y + 10 * _scale));

            double y = card.Y + 40 * _scale;
            int n = Math.Min(5, _cachedRanking.Count);
            for (int i = 0; i < n; i++)
            {
                bool isMe = _cachedRanking[i].Item1 == _playerName;
                var ft = MakeText($"{i + 1}. {_cachedRanking[i].Item1}: {_cachedRanking[i].Item2}",
                    13 * _scale, isMe ? RenderResources.Green : RenderResources.Win11Text, isMe);
                dc.DrawText(ft, new Point(card.X + 10 * _scale, y));
                y += 25 * _scale;
            }
        }

        private void DrawPauseMenu(DrawingContext dc)
        {
            dc.DrawRectangle(RenderResources.Brush(OVERLAY_DARK), null, new Rect(0, 0, _screenWidth, _screenHeight));
            var card = new Rect(_screenWidth / 2 - 250 * _scale, 200 * _scale, 500 * _scale, 300 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Win11Card, 16);

            var title = MakeText("暂停", 48 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(title, new Point(card.X + (card.Width - title.Width) / 2, card.Y + 30 * _scale));

            string[] options = { "重新开始", "返回开始界面", "退出游戏" };
            double oy = card.Y + 120 * _scale;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(card.X + 50 * _scale, oy + i * 60 * _scale, 400 * _scale, 40 * _scale);
                DrawButtonText(dc, r, options[i], 20 * _scale, RenderResources.Win11Text,
                    r.Contains(MousePosition), i == PauseSelection);
            }
        }

        private void DrawGameOverScreen(DrawingContext dc)
        {
            dc.DrawRectangle(RenderResources.Brush(OVERLAY_LIGHT), null, new Rect(0, 0, _screenWidth, _screenHeight));
            var card = new Rect(_screenWidth / 2 - 300 * _scale, 200 * _scale, 600 * _scale, 350 * _scale);
            DrawRoundedCard(dc, card, RenderResources.Win11Card, 16);

            var title = MakeText("游戏结束", 48 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(title, new Point(card.X + (card.Width - title.Width) / 2, card.Y + 30 * _scale));

            var info = MakeText($"最终分数: {Score} | 击杀数: {_gameStats.CurrentGameKills}",
                22 * _scale, RenderResources.Win11Text2, false);
            dc.DrawText(info, new Point(card.X + (card.Width - info.Width) / 2, card.Y + 120 * _scale));

            if (GameMode == "team4v4")
            {
                string result = _captureProgress >= 30 ? "蓝队胜利！" : "红队胜利！";
                var c = _captureProgress >= 30 ? Color.FromRgb(0, 0, 255) : Color.FromRgb(255, 0, 0);
                var resFt = MakeText(result, 24 * _scale, RenderResources.Brush(c), true);
                dc.DrawText(resFt, new Point(card.X + (card.Width - resFt.Width) / 2, card.Y + 170 * _scale));
            }

            string[] options = { "重新开始", "返回开始界面" };
            double oy = card.Y + 230 * _scale;
            for (int i = 0; i < 2; i++)
            {
                var r = new Rect(card.X + 100 * _scale, oy + i * 60 * _scale, 400 * _scale, 40 * _scale);
                DrawButtonText(dc, r, options[i], 20 * _scale, RenderResources.Win11Text,
                    r.Contains(MousePosition), i == GameoverSelection);
            }
        }
        #endregion

        #region 渲染：联机
        private void DrawOnlineGame(DrawingContext dc)
        {
            JObject state;
            lock (_onlineLock) state = _onlineState;

            if (state == null)
            {
                var t = MakeText("正在连接服务器...", 32 * _scale, RenderResources.Win11Text, true);
                dc.DrawText(t, new Point(_screenWidth / 2 - t.Width / 2, _screenHeight / 2 - t.Height / 2));
                return;
            }

            dc.DrawRectangle(RenderResources.DarkGrayBg, null, new Rect(0, 0, _screenWidth, _screenHeight));

            var players = state["Players"] as JArray;
            var foods = state["Foods"] as JArray;
            var enemies = state["Enemies"] as JArray;

            Point myHead = default;
            if (players != null && _client != null)
            {
                var me = players.FirstOrDefault(p => (string)(p["Id"] ?? p["id"]) == _client.PlayerId);
                if (me != null)
                {
                    var body = me["Body"] as JArray;
                    if (body != null && body.Count > 0)
                    {
                        myHead = new Point((double)(body[0]["X"] ?? body[0]["x"]),
                                           (double)(body[0]["Y"] ?? body[0]["y"]));
                        UpdateCamera(myHead);
                    }
                }
            }

            if (foods != null)
            {
                foreach (var f in foods)
                {
                    double fx = (double)(f["Position"]?["X"] ?? f["X"] ?? 0);
                    double fy = (double)(f["Position"]?["Y"] ?? f["Y"] ?? 0);
                    double r = (double)(f["Radius"] ?? f["R"] ?? 6) * _scale;
                    dc.DrawEllipse(Brushes.Red, null, WorldToScreen(new Point(fx, fy)), r, r);
                }
            }

            double segR = GetSegmentRadius();
            if (enemies != null)
            {
                foreach (var e in enemies)
                {
                    var body = e["Body"] as JArray;
                    if (body == null) continue;
                    foreach (var seg in body)
                    {
                        double sx = (double)(seg["X"] ?? seg["x"]);
                        double sy = (double)(seg["Y"] ?? seg["y"]);
                        dc.DrawEllipse(Brushes.Red, null, WorldToScreen(new Point(sx, sy)), segR, segR);
                    }
                }
            }

            if (players != null)
            {
                foreach (var p in players)
                {
                    string id = (string)(p["Id"] ?? p["id"]);
                    bool alive = (bool)(p["Alive"] ?? true);
                    var body = p["Body"] as JArray;
                    if (body == null) continue;
                    bool isMe = _client != null && id == _client.PlayerId;
                    var c = !alive ? Colors.Gray : (isMe ? _headColor : Colors.Blue);
                    var brush = RenderResources.Brush(c);
                    foreach (var seg in body)
                    {
                        double sx = (double)(seg["X"] ?? seg["x"]);
                        double sy = (double)(seg["Y"] ?? seg["y"]);
                        dc.DrawEllipse(brush, null, WorldToScreen(new Point(sx, sy)), segR, segR);
                    }
                }
            }

            var connFt = MakeText("联机模式", 16 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(connFt, new Point(20 * _scale, 20 * _scale));

            if (Paused) DrawPauseMenu(dc);
            if (GameOverScreen) DrawGameOverScreen(dc);
        }
        #endregion

        #region 渲染：选择界面
        private void DrawModeSelect(DrawingContext dc)
        {
            DrawPanelTitle(dc, "选择游戏模式");

            string[] modes = { "经典模式", "淘汰之王", "占领模式 (4v4)", "极限模式", "搜打撤" };
            string[] descs = {
        "自由移动，按 Ctrl 加速，吃食物变长",
        "99 人决斗，按 F 挑战附近对手",
        "与蓝队 AI 合作占领中央区域",
        "20 个敌人，食物仅 60 个",
        "收集分数，搜索容器撤离"
    };
            Color[] accents = {
        Color.FromRgb(0, 138, 0), Color.FromRgb(255, 140, 0),
        Color.FromRgb(0, 95, 184), Color.FromRgb(232, 17, 35),
        Color.FromRgb(255, 215, 0)
    };

            double cardW = 380 * _scale, cardH = 150 * _scale, gap = 24 * _scale;
            double startX = 60 * _scale, startY = 100 * _scale;
            double maxX = _screenWidth - 60 * _scale;
            double x = startX, y = startY;

            for (int i = 0; i < modes.Length; i++)
            {
                if (x + cardW > maxX) { x = startX; y += cardH + gap; }
                var r = new Rect(x, y, cardW, cardH);
                DrawSelectCard(dc, r, modes[i], descs[i], accents[i], r.Contains(MousePosition));
                x += cardW + gap;
            }

            DrawBackButton(dc, null);
        }

        private void DrawMapSelect(DrawingContext dc)
        {
            DrawPanelTitle(dc, "选择作战地图");

            var maps = ExtractionMode.Maps;
            double cardW = 380 * _scale, cardH = 150 * _scale, gap = 24 * _scale;
            double startX = 60 * _scale, startY = 100 * _scale;
            double maxX = _screenWidth - 60 * _scale;
            double x = startX, y = startY;

            for (int i = 0; i < maps.Count; i++)
            {
                if (x + cardW > maxX) { x = startX; y += cardH + gap; }
                var r = new Rect(x, y, cardW, cardH);
                string desc = $"{maps[i].WorldSize.Width}×{maps[i].WorldSize.Height} · 容器 {maps[i].ContainerPositions.Count}";
                DrawSelectCard(dc, r, maps[i].Name, desc, Colors.SteelBlue, r.Contains(MousePosition));
                x += cardW + gap;
            }

            DrawBackButton(dc, null);
        }

        private void DrawSelectCard(DrawingContext dc, Rect r, string title, string desc,
    Color accent, bool hover)
        {
            var bg = hover ? RenderResources.Brush(255, 245, 245, 248) : Brushes.White;
            dc.DrawRoundedRectangle(bg, RenderResources.Pen(Color.FromRgb(210, 210, 210), 1), r, 14, 14);
            dc.DrawRectangle(RenderResources.Brush(accent), null, new Rect(r.X + 3, r.Y + 14, 5, r.Height - 28));

            var titleFt = MakeText(title, 22 * _scale, RenderResources.Win11Text, true);
            dc.DrawText(titleFt, new Point(r.X + 26 * _scale, r.Y + 22 * _scale));

            var descFt = MakeText(desc, 14 * _scale, RenderResources.Win11Text2, false);
            dc.DrawText(descFt, new Point(r.X + 26 * _scale, r.Y + 68 * _scale));
        }

        private void DrawLevelSelect(DrawingContext dc)
        {
            DrawPanelTitle(dc, "初始等级为 1");

            var btn = new Rect(_screenWidth / 2 - 50 * _scale, _screenHeight / 2 - 50 * _scale, 100 * _scale, 100 * _scale);
            dc.DrawRoundedRectangle(Brushes.White, RenderResources.Pen(Colors.Green, 3), btn, 20, 20);
            var num = MakeText("1", 36 * _scale, Brushes.Black, true);
            dc.DrawText(num, new Point(btn.X + (btn.Width - num.Width) / 2, btn.Y + (btn.Height - num.Height) / 2));

            var hint = MakeText("点击“1”确认，然后选择出生位置", 14 * _scale, RenderResources.Win11Text2, false);
            dc.DrawText(hint, new Point(_screenWidth / 2 - hint.Width / 2, _screenHeight - 80 * _scale));
        }
        #endregion

        #region 输入 - 鼠标
        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            MousePosition = e.GetPosition(this);

            if (!GameStarted && !ShowModeSelect && !ShowMapSelect && !ShowLevelSelect &&
                !ShowSettings && !ShowRankings && !ShowAchievements && !ShowOnlineLobby &&
                !ShowChangelog && !ShowNameInput)
            {
                UpdateStartMenuHover();
            }

            bool racing = GameMode == "timed" && (TimedModeInstance?.IsRacing ?? false);

            if (GameStarted && GameActive && !Paused && !GameOverScreen && !_onlineMode && !racing &&
                !ShowSettings && !ShowRankings && !ShowAchievements && !ShowOnlineLobby &&
                !ShowChangelog && !ShowNameInput)
            {
                if (Snake.Count > 0)
                {
                    var worldMouse = ScreenToWorld(MousePosition);
                    var head = Snake[0];
                    double dx = worldMouse.X - head.X;
                    double dy = worldMouse.Y - head.Y;
                    double len = Math.Sqrt(dx * dx + dy * dy);
                    if (len > 1) Direction = new Vector(dx / len, dy / len);
                }
            }
            else if (_onlineMode && GameStarted && GameActive && !Paused && !GameOverScreen)
            {
                var center = new Point(_screenWidth / 2, _screenHeight / 2);
                double dx = MousePosition.X - center.X;
                double dy = MousePosition.Y - center.Y;
                double len = Math.Sqrt(dx * dx + dy * dy);
                if (len > 1) Direction = new Vector(dx / len, dy / len);
            }

            InvalidateVisual();
        }

        private void UpdateStartMenuHover()
        {
            _hoveredMainButton = -1;
            double cx = _screenWidth / 2;
            double[] yOffsets = { -120, -30, 140 };
            double[] widths = { 380, 220, 220 };
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(cx - widths[i] * _scale / 2,
                                 _screenHeight / 2 + yOffsets[i] * _scale,
                                 widths[i] * _scale, 50 * _scale);
                if (r.Contains(MousePosition)) { _hoveredMainButton = i; break; }
            }

            var onlineRect = new Rect(cx - 220 * _scale, _screenHeight / 2 - 180 * _scale, 440 * _scale, 45 * _scale);
            _hoverOnline = onlineRect.Contains(MousePosition);

            double bottomY = _screenHeight - 140 * _scale;
            double bw = 180 * _scale, bh = 45 * _scale;
            _hoverRankings = new Rect(cx - bw * 1.65, bottomY, bw, bh).Contains(MousePosition);
            _hoverAchievements = new Rect(cx - bw * 0.55, bottomY, bw, bh).Contains(MousePosition);
            _hoverChangelog = new Rect(cx + bw * 0.55, bottomY, bw, bh).Contains(MousePosition);
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton != MouseButton.Left) return;
            var pos = e.GetPosition(this);

            // 名字输入时禁止其他点击
            if (ShowNameInput) return;

            // 内嵌：设置
            if (ShowSettings)
            {
                if (GetBackButtonRect().Contains(pos)) { ShowSettings = false; InvalidateVisual(); return; }
                foreach (var hb in _settingsHitboxes)
                    if (hb.Item1.Contains(pos)) { hb.Item2(); InvalidateVisual(); return; }
                return;
            }

            // 内嵌：排行榜 / 成就 / 更新日志
            if (ShowRankings || ShowAchievements || ShowChangelog)
            {
                if (GetBackButtonRect().Contains(pos))
                {
                    ShowRankings = false; ShowAchievements = false; ShowChangelog = false;
                    MarkVersionShown();   // ★ 用户看完日志 → 写入版本文件
                    InvalidateVisual();
                }
                return;
            }

            // 内嵌：联机大厅
            if (ShowOnlineLobby)
            {
                if (GetBackButtonRect().Contains(pos)) { ShowOnlineLobby = false; InvalidateVisual(); return; }

                var card = new Rect(_screenWidth / 2 - 300 * _scale, 120 * _scale, 600 * _scale, 400 * _scale);
                double px = card.X + 40 * _scale;
                double py = card.Y + 40 * _scale;
                double rowW = card.Width - 80 * _scale;
                double rowH = 50 * _scale;

                var hostRect = new Rect(px, py, rowW, rowH);
                if (hostRect.Contains(pos)) { _lobbyJoinMode = false; InvalidateVisual(); return; }

                var joinRect = new Rect(px, py + rowH + 12 * _scale, rowW, rowH);
                if (joinRect.Contains(pos)) { _lobbyJoinMode = true; InvalidateVisual(); return; }

                if (_lobbyJoinMode)
                {
                    var ipRect = new Rect(px, py + (rowH + 12 * _scale) * 2, rowW, rowH);
                    if (ipRect.Contains(pos))
                    {
                        _lobbyEditField = 1;
                        ActivateInputBox(_lobbyIp, "输入 IP 后回车", 16);
                        return;
                    }
                    var portRect = new Rect(px, py + (rowH + 12 * _scale) * 3, rowW, rowH);
                    if (portRect.Contains(pos))
                    {
                        _lobbyEditField = 2;
                        ActivateInputBox(_lobbyPort.ToString(), "输入端口后回车", 16);
                        return;
                    }
                }

                // 确定按钮：双击卡片空白区域尝试连接
                // 简化：直接启动连接
                if (_lobbyJoinMode)
                {
                    ShowOnlineLobby = false;
                    StartOnline(_lobbyIp, _lobbyPort, false);
                }
                else
                {
                    ShowOnlineLobby = false;
                    StartOnline("127.0.0.1", 8888, true);
                }
                return;
            }

            // 模式选择
            if (ShowModeSelect)
            {
                if (GetBackButtonRect().Contains(pos)) { ShowModeSelect = false; InvalidateVisual(); return; }

                double cardW = 380 * _scale, cardH = 150 * _scale, gap = 24 * _scale;
                double startX = 60 * _scale, startY = 100 * _scale;
                double maxX = _screenWidth - 60 * _scale;
                double x = startX, y = startY;
                string[] modeIds = { "classic", "timed", "team4v4", "extreme", "extraction" };

                for (int i = 0; i < modeIds.Length; i++)
                {
                    if (x + cardW > maxX) { x = startX; y += cardH + gap; }
                    var r = new Rect(x, y, cardW, cardH);
                    if (r.Contains(pos))
                    {
                        SelectedModeIndex = i;
                        GameMode = modeIds[i];
                        if (GameMode == "timed") { TimedModeInstance = new TimedMode(); ShowModeSelect = false; ShowLevelSelect = true; }
                        else if (GameMode == "extraction") { ShowModeSelect = false; ShowMapSelect = true; }
                        else { ShowModeSelect = false; InitializeGame(); StartGame(); }
                        InvalidateVisual();
                        return;
                    }
                    x += cardW + gap;
                }
                return;
            }

            // 地图选择
            if (ShowMapSelect)
            {
                if (GetBackButtonRect().Contains(pos)) { ShowMapSelect = false; ShowModeSelect = true; InvalidateVisual(); return; }

                var maps = ExtractionMode.Maps;
                double cardW = 380 * _scale, cardH = 150 * _scale, gap = 24 * _scale;
                double startX = 60 * _scale, startY = 100 * _scale;
                double maxX = _screenWidth - 60 * _scale;
                double x = startX, y = startY;

                for (int i = 0; i < maps.Count; i++)
                {
                    if (x + cardW > maxX) { x = startX; y += cardH + gap; }
                    var r = new Rect(x, y, cardW, cardH);
                    if (r.Contains(pos))
                    {
                        SelectedMapIndex = i;
                        ExtractionModeInstance = new ExtractionMode { SelectedMap = maps[i] };
                        ShowMapSelect = false;
                        InitializeGame();
                        StartGame();
                        InvalidateVisual();
                        return;
                    }
                    x += cardW + gap;
                }
                return;
            }

            // 等级选择
            if (ShowLevelSelect)
            {
                var btn = new Rect(_screenWidth / 2 - 50 * _scale, _screenHeight / 2 - 50 * _scale, 100 * _scale, 100 * _scale);
                if (btn.Contains(pos)) { ShowLevelSelect = false; InitializeGame(); StartGame(); InvalidateVisual(); }
                return;
            }

            // 主菜单
            if (!GameStarted)
            {
                double cx = _screenWidth / 2;
                double[] yOffsets = { -120, -30, 140 };
                double[] widths = { 380, 220, 220 };
                for (int i = 0; i < 3; i++)
                {
                    var r = new Rect(cx - widths[i] * _scale / 2,
                                     _screenHeight / 2 + yOffsets[i] * _scale,
                                     widths[i] * _scale, 50 * _scale);
                    if (r.Contains(pos))
                    {
                        if (i == 0) { ShowModeSelect = true; SelectedModeIndex = 0; }
                        else if (i == 1) { ShowSettings = true; }
                        else Application.Current.Shutdown();
                        InvalidateVisual();
                        return;
                    }
                }

                var onlineRect = new Rect(cx - 220 * _scale, _screenHeight / 2 - 180 * _scale, 440 * _scale, 45 * _scale);
                if (onlineRect.Contains(pos)) { ShowOnlineLobby = true; InvalidateVisual(); return; }

                double bottomY = _screenHeight - 140 * _scale;
                double bw = 180 * _scale, bh = 45 * _scale;
                if (new Rect(cx - bw * 1.65, bottomY, bw, bh).Contains(pos)) { ShowRankings = true; InvalidateVisual(); return; }
                if (new Rect(cx - bw * 0.55, bottomY, bw, bh).Contains(pos)) { ShowAchievements = true; InvalidateVisual(); return; }
                if (new Rect(cx + bw * 0.55, bottomY, bw, bh).Contains(pos)) { ShowChangelog = true; _changelogScroll = 0; InvalidateVisual(); return; }
                return;
            }

            // 暂停
            if (Paused)
            {
                var card = new Rect(_screenWidth / 2 - 250 * _scale, 200 * _scale, 500 * _scale, 300 * _scale);
                double oy = card.Y + 120 * _scale;
                for (int i = 0; i < 3; i++)
                {
                    var r = new Rect(card.X + 50 * _scale, oy + i * 60 * _scale, 400 * _scale, 40 * _scale);
                    if (r.Contains(pos))
                    {
                        if (i == 0) { Paused = false; RestartGame(); }
                        else if (i == 1) { Paused = false; GameStarted = false; InitializeGame(); }
                        else Application.Current.Shutdown();
                        InvalidateVisual();
                        return;
                    }
                }
                return;
            }

            // 游戏结束
            if (GameOverScreen)
            {
                var card = new Rect(_screenWidth / 2 - 300 * _scale, 200 * _scale, 600 * _scale, 350 * _scale);
                double oy = card.Y + 230 * _scale;
                for (int i = 0; i < 2; i++)
                {
                    var r = new Rect(card.X + 100 * _scale, oy + i * 60 * _scale, 400 * _scale, 40 * _scale);
                    if (r.Contains(pos))
                    {
                        if (i == 0) RestartGame();
                        else { GameOverScreen = false; GameStarted = false; InitializeGame(); }
                        InvalidateVisual();
                        return;
                    }
                }
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e) { }

        private void OnMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (ShowChangelog)
            {
                _changelogScroll -= e.Delta / 3.0;
                _changelogScroll = Math.Max(0, _changelogScroll);
                InvalidateVisual();
            }
        }
        #endregion

        #region 输入 - 键盘
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            _pressedKeys.Add(e.Key);

            if (ShowNameInput) return;

            if (e.Key == Key.Escape)
            {
                if (ShowSettings) { ShowSettings = false; InvalidateVisual(); return; }
                if (ShowRankings) { ShowRankings = false; InvalidateVisual(); return; }
                if (ShowAchievements) { ShowAchievements = false; InvalidateVisual(); return; }
                if (ShowOnlineLobby) { ShowOnlineLobby = false; InvalidateVisual(); return; }
                if (ShowChangelog) { ShowChangelog = false; MarkVersionShown(); InvalidateVisual(); return; }
                if (ShowLevelSelect) { ShowLevelSelect = false; ShowModeSelect = true; InvalidateVisual(); return; }
                if (ShowMapSelect) { ShowMapSelect = false; ShowModeSelect = true; InvalidateVisual(); return; }
                if (ShowModeSelect) { ShowModeSelect = false; InvalidateVisual(); return; }
                if (GameOverScreen) { RestartGame(); return; }
                if (GameStarted) { Paused = !Paused; PauseSelection = 0; InvalidateVisual(); return; }
                return;
            }

            if (e.Key == Key.Q && HasQAbility && GameActive && GameStarted)
            {
                HasQAbility = false;
                if (GameMode == "team4v4") { SetCaptureProgress(30); GameOver(true); }
                else if (GameMode == "extraction") Enemies.Clear();
                else GameOver(true);
                InvalidateVisual();
                return;
            }

            if (!GameStarted && !ShowModeSelect && !ShowMapSelect && !ShowLevelSelect &&
                !ShowSettings && !ShowRankings && !ShowAchievements && !ShowOnlineLobby && !ShowChangelog)
            {
                if (e.Key == Key.Space) { ShowModeSelect = true; SelectedModeIndex = 0; }
                else if (e.Key == Key.S) ShowSettings = true;
                else if (e.Key == Key.Q) Application.Current.Shutdown();
                else if (e.Key == Key.H) ShowRankings = true;
                else if (e.Key == Key.J) ShowAchievements = true;
                else if (e.Key == Key.L) { ShowChangelog = true; _changelogScroll = 0; }
                else if (e.Key == Key.O) ShowOnlineLobby = true;
                InvalidateVisual();
                return;
            }

            if (Paused)
            {
                if (e.Key == Key.Up) PauseSelection = (PauseSelection - 1 + 3) % 3;
                else if (e.Key == Key.Down) PauseSelection = (PauseSelection + 1) % 3;
                else if (e.Key == Key.Enter)
                {
                    if (PauseSelection == 0) { Paused = false; RestartGame(); }
                    else if (PauseSelection == 1) { Paused = false; GameStarted = false; InitializeGame(); }
                    else Application.Current.Shutdown();
                }
                InvalidateVisual();
                return;
            }

            if (GameOverScreen)
            {
                if (e.Key == Key.Up) GameoverSelection = (GameoverSelection - 1 + 2) % 2;
                else if (e.Key == Key.Down) GameoverSelection = (GameoverSelection + 1) % 2;
                else if (e.Key == Key.Enter)
                {
                    if (GameoverSelection == 0) RestartGame();
                    else { GameOverScreen = false; GameStarted = false; InitializeGame(); }
                }
                InvalidateVisual();
                return;
            }

            if (GameStarted && GameActive && (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl))
            {
                Boosting = true;
                BoostStartTime = DateTime.Now;
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) Boosting = false;
        }
        #endregion

        #region 输入框（TextBox 覆盖）

        private void ActivateInputBox(string initial, string hint, double fontSize)
        {
            InputBox.Text = initial;
            InputBox.FontSize = fontSize * _scale;
            InputBox.Width = 500 * _scale;
            InputBox.Height = 56 * _scale;
            InputBox.HorizontalAlignment = HorizontalAlignment.Center;
            InputBox.VerticalAlignment = VerticalAlignment.Center;
            InputBox.Margin = new Thickness(0, 80 * _scale, 0, 0);
            InputBox.Visibility = Visibility.Visible;
            InputBox.SelectAll();
            Dispatcher.BeginInvoke(new Action(() => InputBox.Focus()), DispatcherPriority.Input);
        }

        private void HideInputBox()
        {
            InputBox.Visibility = Visibility.Collapsed;
        }

        private void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            string text = InputBox.Text.Trim();

            if (ShowNameInput)
            {
                if (string.IsNullOrEmpty(text)) text = "玩家";
                if (text.Length > 20) text = text.Substring(0, 20);
                SecureStorage.SaveText(NAME_FILE, text);
                _playerName = text;
                ShowNameInput = false;
                HideInputBox();

                // ★ 名字输完后紧接着弹更新日志（如果检测到版本变化）
                if (_pendingChangelog)
                {
                    ShowChangelog = true;
                    _changelogScroll = 0;
                    MarkVersionShown();
                }

                InvalidateVisual();
                Focus();
                return;
            }

            if (_lobbyEditField == 1)
            {
                if (!string.IsNullOrEmpty(text)) _lobbyIp = text;
                _lobbyEditField = 0;
                HideInputBox();
                Focus();
                InvalidateVisual();
                return;
            }

            if (_lobbyEditField == 2)
            {
                if (int.TryParse(text, out var p) && p >= 1024 && p <= 65535) _lobbyPort = p;
                _lobbyEditField = 0;
                HideInputBox();
                Focus();
                InvalidateVisual();
                return;
            }
        }
        #endregion

        #region 网络
        private async void StartOnline(string ip, int port, bool asHost)
        {
            try
            {
                if (asHost)
                {
                    _server = new GameServer(port, FOOD_COUNT, ENEMY_COUNT, (float)AI_DIFFICULTY_NORMAL);
                    _server.Start();
                }

                _client = new GameNetworkClient();
                _client.OnMessageReceived += OnServerMessage;
                bool ok = await _client.ConnectAsync(ip, port, _playerName, GameMode);
                if (!ok)
                {
                    MessageBox.Show("连接服务器失败");
                    _server?.Stop(); _server = null;
                    _client = null;
                    return;
                }
                _onlineMode = true;
                GameStarted = true;
                GameActive = true;
                GameOverScreen = false;
                SetWorldSizeInternal(5000, 4000);
                InvalidateVisual();
            }
            catch (Exception ex)
            {
                MessageBox.Show("联机错误：" + ex.Message);
                _onlineMode = false;
                _server?.Stop(); _server = null;
                _client?.Disconnect(); _client = null;
            }
        }

        private void OnServerMessage(JObject msg)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                string type = (string)(msg["Type"] ?? msg["type"]);
                if (type == "welcome") { GameStarted = true; GameActive = true; GameOverScreen = false; }
                else if (type == "state")
                {
                    lock (_onlineLock) _onlineState = msg["State"] as JObject ?? msg["state"] as JObject;
                    InvalidateVisual();
                }
            }));
        }
        #endregion

        #region 辅助
        private double DistSq(Point a, Point b)
            => (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);

        private static string ColorToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

        private void LoadPlayerName()
        {
            var raw = SecureStorage.LoadText(NAME_FILE);
            if (!string.IsNullOrEmpty(raw)) _playerName = raw.Trim();
        }

        private void LoadAchievements()
        {
            foreach (var a in Achievements)
                if (!_achievementsUnlocked.ContainsKey(a.Id)) _achievementsUnlocked[a.Id] = false;

            var raw = SecureStorage.LoadText(ACHIEVE_FILE);
            if (!string.IsNullOrEmpty(raw))
            {
                try
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, bool>>(raw);
                    if (data != null)
                        foreach (var kv in data)
                            if (_achievementsUnlocked.ContainsKey(kv.Key))
                                _achievementsUnlocked[kv.Key] = kv.Value;
                }
                catch { }
            }
        }

        private void CheckAchievements()
        {
            bool modified = false;
            void Check(string id, bool cond)
            {
                if (cond && _achievementsUnlocked.TryGetValue(id, out var v) && !v)
                { _achievementsUnlocked[id] = true; modified = true; }
            }
            Check("first_kill", _gameStats.TotalKills >= 1);
            Check("kill_10", _gameStats.TotalKills >= 10);
            Check("kill_50", _gameStats.TotalKills >= 50);
            Check("win_10", _gameStats.WonGames >= 10);
            Check("survival_5min", _gameStats.CurrentGameDuration > 300);
            Check("capture_win", _gameStats.CaptureWins > 0);

            if (modified)
            {
                try { SecureStorage.SaveText(ACHIEVE_FILE, JsonConvert.SerializeObject(_achievementsUnlocked, Formatting.Indented)); }
                catch { }
            }
        }

        private void SaveScore(string name, int score)
        {
            var scores = LoadScores();
            scores.Add(new List<object> { name, score });
            scores.Sort((a, b) => Convert.ToInt32(b[1]).CompareTo(Convert.ToInt32(a[1])));
            try { SecureStorage.SaveText(HIGHSCORE_FILE, JsonConvert.SerializeObject(scores, Formatting.Indented)); }
            catch { }
        }

        public List<List<object>> LoadScores()
        {
            var raw = SecureStorage.LoadText(HIGHSCORE_FILE);
            if (string.IsNullOrEmpty(raw)) return new List<List<object>>();
            try { return JsonConvert.DeserializeObject<List<List<object>>>(raw) ?? new List<List<object>>(); }
            catch { return new List<List<object>>(); }
        }

        private void CheckVersion()
        {
            try
            {
                var current = SecureStorage.LoadText(VERSION_FILE);
                if (current == null || current.Trim() != VERSION)
                    _pendingChangelog = true;
                else
                    _pendingChangelog = false;
            }
            catch { _pendingChangelog = false; }
        }

        private void LoadCustomBackground()
        {
            _customBg = null;

            if (AppSettings == null) return;
            if (string.IsNullOrEmpty(AppSettings.CustomBackgroundPath)) return;
            if (!File.Exists(AppSettings.CustomBackgroundPath)) return;

            _customBg = TryLoadImageByFileStream(AppSettings.CustomBackgroundPath)
                     ?? TryLoadImageByUri(AppSettings.CustomBackgroundPath);

            // ★ 不再修改 CustomBgEnabled —— 尊重用户的启用/禁用选择
        }

        private BitmapImage TryLoadImageByFileStream(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    bmp.BeginInit();
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.StreamSource = fs;
                    bmp.EndInit();
                }
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("FileStream 加载失败: " + ex.Message);
                return null;
            }
        }

        private BitmapImage TryLoadImageByUri(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.UriSource = new Uri(path, UriKind.Absolute);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Uri 加载失败: " + ex.Message);
                return null;
            }
        }

        private void ApplyFrameRate()
        {
            if (_gameTimer == null) return;
            int target = AppSettings?.FpsTarget ?? 60;
            if (AppSettings != null && AppSettings.HighFpsEnabled)
            {
                var highs = new[] { 360, 500, 750, 1000 };
                int idx = Math.Min(AppSettings.SelectedHighFpsIndex, highs.Length - 1);
                target = highs[Math.Max(0, idx)];
            }
            if (target < 1) target = 60;
            _gameTimer.Interval = TimeSpan.FromMilliseconds(1000.0 / target);
        }
        /// <summary>把窗口居中到主屏幕</summary>
        private void CenterWindow()
        {
            double screenW = SystemParameters.PrimaryScreenWidth;
            double screenH = SystemParameters.PrimaryScreenHeight;

            // 不允许超过屏幕尺寸
            if (Width > screenW) Width = screenW;
            if (Height > screenH) Height = screenH;

            Left = (screenW - Width) / 2;
            Top = (screenH - Height) / 2;
        }

        /// <summary>从设置里恢复窗口几何</summary>
        private void RestoreWindowGeometry()
        {
            // 若未保存过（-1），用默认尺寸居中
            if (AppSettings.WindowLeft < 0 || AppSettings.WindowTop < 0 ||
                AppSettings.WindowWidth < 200 || AppSettings.WindowHeight < 200)
            {
                WindowStartupLocation = WindowStartupLocation.CenterScreen;
                _geometryRestored = false;
                return;
            }

            // 校验：窗口是否在屏幕上可见（防止显示器拔掉后窗口跑到屏幕外）
            double screenW = SystemParameters.VirtualScreenWidth;
            double screenH = SystemParameters.VirtualScreenHeight;
            double screenL = SystemParameters.VirtualScreenLeft;
            double screenT = SystemParameters.VirtualScreenTop;

            double w = Math.Min(AppSettings.WindowWidth, screenW);
            double h = Math.Min(AppSettings.WindowHeight, screenH);
            double l = Math.Max(screenL, Math.Min(AppSettings.WindowLeft, screenL + screenW - 100));
            double t = Math.Max(screenT, Math.Min(AppSettings.WindowTop, screenT + screenH - 100));

            WindowStartupLocation = WindowStartupLocation.Manual;
            Left = l;
            Top = t;
            Width = w;
            Height = h;

            if (AppSettings.WindowMaximized)
                WindowState = WindowState.Maximized;

            _geometryRestored = true;
        }

        /// <summary>把当前窗口几何保存到设置</summary>
        private void SaveWindowGeometry()
        {
            if (AppSettings == null) return;

            // 全屏模式下不保存尺寸（那是显示设置控制的）
            if (AppSettings.DisplayMode == 0) return;

            // 最大化时保存"还原后"的尺寸
            var bounds = WindowState == WindowState.Normal
                ? new Rect(Left, Top, Width, Height)
                : RestoreBounds;

            // RestoreBounds 在首次最大化且未还原过时可能是 Empty，跳过
            if (bounds.Width < 100 || bounds.Height < 100) return;

            AppSettings.WindowWidth = bounds.Width;
            AppSettings.WindowHeight = bounds.Height;
            AppSettings.WindowLeft = bounds.Left;
            AppSettings.WindowTop = bounds.Top;
            AppSettings.WindowMaximized = WindowState == WindowState.Maximized;

            AppSettings.Save();
        }

        /// <summary>应用显示模式与分辨率</summary>
        private void ApplyDisplaySettings()
        {
            int dispIdx = Math.Max(0, Math.Min(AppSettings.DisplayMode, 2));
            var resolutions = new[] {
        (800,600),(1024,768),(1280,720),(1600,900),(1920,1080),(2560,1440),(3840,2160)
    };
            int resIdx = Math.Max(0, Math.Min(AppSettings.ResolutionIndex, resolutions.Length - 1));

            // 先回到 Normal，再切换 Style/Resize，最后按需最大化
            if (WindowState != WindowState.Normal)
                WindowState = WindowState.Normal;

            switch (dispIdx)
            {
                case 0: // 无边框全屏
                    WindowStyle = WindowStyle.None;
                    ResizeMode = ResizeMode.NoResize;
                    Left = 0; Top = 0;
                    Width = SystemParameters.PrimaryScreenWidth;
                    Height = SystemParameters.PrimaryScreenHeight;
                    WindowState = WindowState.Maximized;
                    break;

                case 1: // 窗口化（无边框）
                    WindowStyle = WindowStyle.None;
                    ResizeMode = ResizeMode.CanResizeWithGrip;
                    if (!_geometryRestored)
                    {
                        Width = resolutions[resIdx].Item1;
                        Height = resolutions[resIdx].Item2;
                        CenterWindow();
                    }
                    break;

                default: // 窗口化
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    ResizeMode = ResizeMode.CanResize;
                    if (!_geometryRestored)
                    {
                        Width = resolutions[resIdx].Item1;
                        Height = resolutions[resIdx].Item2;
                        CenterWindow();
                    }
                    break;
            }

            // 刷新尺寸缓存
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _screenWidth = ActualWidth;
                _screenHeight = ActualHeight;
                RecalculateScale();
                InvalidateVisual();
            }), DispatcherPriority.Loaded);
        }

        /// <summary>应用抗锯齿</summary>
        private void ApplyAntiAlias()
        {
            // 打开抗锯齿：让 WPF 使用默认的边缘平滑；
            // 关闭抗锯齿：强制 Aliased 边缘，圆形会呈锯齿状
            RenderOptions.SetEdgeMode(
                this,
                (AppSettings?.AntiAlias ?? true) ? EdgeMode.Unspecified : EdgeMode.Aliased);

            InvalidateVisual();
        }

        /// <summary>应用垂直同步（WPF 层面，通过渲染模式近似实现）</summary>
        private void ApplyVsync()
        {
            try
            {
                // WPF 默认就是硬件加速 + 垂直同步（RenderMode.Default）。
                // 关闭垂直同步时切到 SoftwareOnly，可绕过 GPU 的垂直同步等待。
                // 注意：SoftwareOnly 性能下降明显，用户需自行权衡。
                RenderOptions.ProcessRenderMode = (AppSettings?.Vsync ?? true)
                    ? System.Windows.Interop.RenderMode.Default
                    : System.Windows.Interop.RenderMode.SoftwareOnly;
            }
            catch { }
        }
        #endregion
    }
}