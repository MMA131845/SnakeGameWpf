using System;
using System.IO.Pipes;
using System.Text;
using System.Threading.Tasks;

namespace SnakeGame
{
    public sealed class SimpleIpcClient : IDisposable
    {
        private NamedPipeClientStream _pipe;
        private bool _connected;

        public async Task<bool> ConnectAsync()
        {
            try
            {
                _pipe = new NamedPipeClientStream(".", "SnakeGameFPSPipe", PipeDirection.Out);
                await _pipe.ConnectAsync(2000);
                _connected = true;
                return true;
            }
            catch { return false; }
        }

        public async Task SendStatusAsync(int fps, int score, int kills, string mode)
        {
            if (!_connected || _pipe == null || !_pipe.IsConnected) return;
            byte[] data = Encoding.UTF8.GetBytes($"FPS:{fps},SCORE:{score},KILLS:{kills},MODE:{mode}\n");
            try
            {
                await _pipe.WriteAsync(data, 0, data.Length);
                await _pipe.FlushAsync();
            }
            catch { }
        }

        public void Dispose() { try { _pipe?.Dispose(); } catch { } }
    }
}