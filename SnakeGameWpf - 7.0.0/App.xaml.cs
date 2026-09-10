using System.Windows;
using System.Windows.Media;

namespace SnakeGame
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 强制使用软件渲染回退时也维持清晰度
            RenderOptions.ProcessRenderMode = System.Windows.Interop.RenderMode.Default;
            // 全局文本渲染模式（ClearType 在深色背景下更清晰）
            TextOptions.TextFormattingModeProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(TextFormattingMode.Ideal));
            TextOptions.TextRenderingModeProperty.OverrideMetadata(
                typeof(Window),
                new FrameworkPropertyMetadata(TextRenderingMode.ClearType));
        }
    }
}