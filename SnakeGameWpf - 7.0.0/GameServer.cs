using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeGame
{
    public class GameServer : IDisposable
    {
        private readonly TcpListener _listener;
        private readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private readonly GameEngine _engine;
        private readonly object _sync = new object();
        private bool _running;
        private Timer _updateTimer;
        private const float UpdateInterval = 33f;

        public GameServer(int port, int foodCount, int enemyCount, float aiDifficulty)
        {
            _engine = new GameEngine();
            _engine.Initialize(foodCount, enemyCount, aiDifficulty);
            _listener = new TcpListener(IPAddress.Any, port);
        }

        public void Start()
        {
            _listener.Start();
            _running = true;
            _ = Task.Run(AcceptClients);
            _updateTimer = new Timer(UpdateCallback, null, 0, (int)UpdateInterval);
        }

        public void Stop()
        {
            _running = false;
            _updateTimer?.Dispose();
            try { _listener?.Stop(); } catch { }
            lock (_sync)
            {
                foreach (var c in _clients) c.Close();
                _clients.Clear();
            }
        }

        private async Task AcceptClients()
        {
            while (_running)
            {
                try
                {
                    var tcp = await _listener.AcceptTcpClientAsync();
                    var client = new ClientConnection(tcp, this);
                    lock (_sync) _clients.Add(client);
                    _ = client.HandleAsync();
                }
                catch { break; }
            }
        }

        private void UpdateCallback(object state)
        {
            if (!_running) return;

            var inputs = new Dictionary<string, (Point dir, bool boost)>();
            lock (_sync)
            {
                foreach (var c in _clients)
                    if (c.PlayerId != null && c.Ready)
                        inputs[c.PlayerId] = (c.Direction, c.Boosting);
            }

            _engine.Update(UpdateInterval / 1000f, inputs);
            var gameState = _engine.GetState();
            var json = JObject.FromObject(new { Type = "state", State = gameState });
            Broadcast(json.ToString());
        }

        private void Broadcast(string message)
        {
            lock (_sync)
                foreach (var c in _clients) c.SendAsync(message);
        }

        // ✔ 改为 private —— ClientConnection 是 private 嵌套类
        private void OnClientMessage(ClientConnection client, string message)
        {
            try
            {
                var json = JObject.Parse(message);
                string type = (json["Type"] ?? json["type"])?.ToString();
                if (type == "input")
                {
                    var dir = json["Direction"];
                    if (dir != null)
                    {
                        double dx = (double)(dir["X"] ?? 0);
                        double dy = (double)(dir["Y"] ?? 0);
                        client.Direction = new Point(dx, dy);
                    }
                    client.Boosting = (bool)(json["Boosting"] ?? false);
                }
                else if (type == "join")
                {
                    client.PlayerId = Guid.NewGuid().ToString();
                    var player = new NetworkPlayer { Id = client.PlayerId };
                    var spawn = _engine.RandomPosition();
                    player.Body = new List<Point>
                    {
                        spawn,
                        new Point(spawn.X - 16, spawn.Y),
                        new Point(spawn.X - 32, spawn.Y)
                    };
                    _engine.Players.Add(player);
                    client.Ready = true;

                    var welcome = JObject.FromObject(new
                    {
                        Type = "welcome",
                        PlayerId = client.PlayerId
                    });
                    client.SendAsync(welcome.ToString());
                }
            }
            catch { }
        }

        public void Dispose() => Stop();

        private class ClientConnection
        {
            private readonly TcpClient _tcp;
            private readonly GameServer _server;
            private readonly StreamReader _reader;
            private readonly StreamWriter _writer;
            public string PlayerId { get; set; }
            public Point Direction { get; set; } = new Point(1, 0);
            public bool Boosting { get; set; }
            public bool Ready { get; set; }

            public ClientConnection(TcpClient tcp, GameServer server)
            {
                _tcp = tcp;
                _server = server;
                _reader = new StreamReader(tcp.GetStream(), Encoding.UTF8);
                _writer = new StreamWriter(tcp.GetStream(), Encoding.UTF8) { AutoFlush = true };
            }

            public async Task HandleAsync()
            {
                try
                {
                    while (_tcp.Connected)
                    {
                        var line = await _reader.ReadLineAsync();
                        if (line == null) break;
                        _server.OnClientMessage(this, line);
                    }
                }
                catch { }
                finally { Close(); }
            }

            public async void SendAsync(string message)
            {
                try { await _writer.WriteLineAsync(message); } catch { }
            }

            public void Close()
            {
                try { _tcp?.Close(); } catch { }
                lock (_server._sync) _server._clients.Remove(this);
            }
        }
    }
}