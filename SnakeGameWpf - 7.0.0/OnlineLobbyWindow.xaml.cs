using System.Windows;

namespace SnakeGame
{
    public partial class OnlineLobbyWindow : Window
    {
        public bool IsHost { get; private set; }
        public string ServerIP { get; private set; } = "127.0.0.1";
        public int Port { get; private set; } = 8888;

        public OnlineLobbyWindow() { InitializeComponent(); }

        private void RbJoin_Checked(object sender, RoutedEventArgs e)
        {
            if (JoinPanel != null)
                JoinPanel.Visibility = RbJoin.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void OkBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(PortBox.Text, out var p) || p < 1024 || p > 65535)
            {
                MessageBox.Show("端口必须在 1024-65535 之间");
                return;
            }
            IsHost = RbHost.IsChecked == true;
            Port = p;
            if (!IsHost)
            {
                var ip = IpBox.Text.Trim();
                if (string.IsNullOrEmpty(ip)) { MessageBox.Show("请输入服务器 IP"); return; }
                ServerIP = ip;
            }
            else ServerIP = "127.0.0.1";

            DialogResult = true;
            Close();
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}