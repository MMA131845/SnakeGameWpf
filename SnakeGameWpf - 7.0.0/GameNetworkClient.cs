using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SnakeGame
{
    public class GameNetworkClient
    {
        private TcpClient _tcp;
        private StreamReader _reader;
        private StreamWriter _writer;
        private bool _connected;

        public string PlayerId { get; private set; }
        public event Action<JObject> OnMessageReceived;

        public async Task<bool> ConnectAsync(string ip, int port, string playerName, string gameMode)
        {
            try
            {
                _tcp = new TcpClient();
                await _tcp.ConnectAsync(ip, port);
                _reader = new StreamReader(_tcp.GetStream(), Encoding.UTF8);
                _writer = new StreamWriter(_tcp.GetStream(), Encoding.UTF8) { AutoFlush = true };
                _connected = true;

                var join = new JObject { ["Type"] = "join", ["Name"] = playerName, ["Mode"] = gameMode };
                await SendJsonAsync(join);

                var line = await _reader.ReadLineAsync();
                if (line != null)
                {
                    var json = JObject.Parse(line);
                    if ((string)(json["Type"] ?? json["type"]) == "welcome")
                        PlayerId = (string)(json["PlayerId"] ?? json["playerId"]);
                }

                _ = Task.Run(ReceiveLoop);
                return true;
            }
            catch { return false; }
        }

        public Task SendInputAsync(Point dir, bool boosting) => SendJsonAsync(new JObject
        {
            ["Type"] = "input",
            ["Direction"] = new JObject { ["X"] = dir.X, ["Y"] = dir.Y },
            ["Boosting"] = boosting
        });

        public async Task SendJsonAsync(JObject json)
        {
            if (!_connected || _writer == null) return;
            try { await _writer.WriteLineAsync(json.ToString()); } catch { }
        }

        private async Task ReceiveLoop()
        {
            while (_connected)
            {
                try
                {
                    var line = await _reader.ReadLineAsync();
                    if (line == null) break;
                    OnMessageReceived?.Invoke(JObject.Parse(line));
                }
                catch { break; }
            }
            Disconnect();
        }

        public void Disconnect()
        {
            _connected = false;
            try { _tcp?.Close(); } catch { }
        }
    }
}