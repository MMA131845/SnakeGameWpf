using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SnakeGame
{
    public static class RenderResources
    {
        private static readonly Dictionary<uint, SolidColorBrush> _brushCache = new();
        private static readonly Dictionary<ulong, Pen> _penCache = new();
        private static readonly Dictionary<(string font, bool bold), Typeface> _tfCache = new();

        // ---------- Brush ----------
        public static SolidColorBrush Brush(byte a, byte r, byte g, byte b)
        {
            uint key = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            if (_brushCache.TryGetValue(key, out var cached)) return cached;
            var br = new SolidColorBrush(Color.FromArgb(a, r, g, b));
            br.Freeze();
            _brushCache[key] = br;
            return br;
        }

        public static SolidColorBrush Brush(Color c) => Brush(c.A, c.R, c.G, c.B);

        // ---------- Pen ----------
        // ★ 用 ulong 作为 key，避免 uint 左移溢出导致不同颜色/厚度命中同一缓存
        public static Pen Pen(Color c, double thickness)
        {
            uint ck = ((uint)c.A << 24) | ((uint)c.R << 16) | ((uint)c.G << 8) | c.B;
            uint tk = (uint)Math.Round(thickness * 100);   // 保留两位小数
            ulong key = ((ulong)ck << 32) | tk;

            if (_penCache.TryGetValue(key, out var p)) return p;
            p = new Pen(Brush(c), thickness);
            p.Freeze();
            _penCache[key] = p;
            return p;
        }

        // ---------- Typeface ----------
        public static Typeface Typeface(string family = "Microsoft YaHei", bool bold = false)
        {
            var k = (family, bold);
            if (_tfCache.TryGetValue(k, out var tf)) return tf;
            tf = new Typeface(new FontFamily(family),
                FontStyles.Normal,
                bold ? FontWeights.Bold : FontWeights.Normal,
                FontStretches.Normal);
            _tfCache[k] = tf;
            return tf;
        }

        // ---------- 预定义常用资源 ----------
        public static readonly Brush White = Brush(255, 255, 255, 255);
        public static readonly Brush Black = Brush(255, 0, 0, 0);
        public static readonly Brush Gray = Brush(255, 128, 128, 128);
        public static readonly Brush Red = Brush(255, 232, 17, 35);
        public static readonly Brush Blue = Brush(255, 0, 95, 184);
        public static readonly Brush Green = Brush(255, 0, 138, 0);
        public static readonly Brush Yellow = Brush(255, 255, 200, 10);
        public static readonly Brush Orange = Brush(255, 255, 140, 0);
        public static readonly Brush Gold = Brush(255, 255, 215, 0);
        public static readonly Brush Lime = Brush(255, 0, 255, 0);
        public static readonly Brush DarkGrayBg = Brush(255, 245, 245, 247);
        public static readonly Brush Win11Card = Brush(220, 255, 255, 255);
        public static readonly Brush Win11Text = Brush(255, 22, 22, 22);
        public static readonly Brush Win11Text2 = Brush(255, 96, 96, 96);

        // ★ 常用 Pen 预定义，避免每次查找
        public static readonly Pen PenBlack1 = Pen(Colors.Black, 1);
        public static readonly Pen PenWhite1 = Pen(Colors.White, 1);
        public static readonly Pen PenWhite2 = Pen(Colors.White, 2);
        public static readonly Pen PenGray1 = Pen(Colors.Gray, 1);
        public static readonly Pen PenRed2 = Pen(Colors.Red, 2);
        public static readonly Pen PenRed3 = Pen(Colors.Red, 3);
        public static readonly Pen PenGreen2 = Pen(Colors.Green, 2);
        public static readonly Pen PenYellow2 = Pen(Colors.Yellow, 2);
    }

    /// <summary>FormattedText 轻量池</summary>
    public sealed class TextRenderer
    {
        private readonly Dictionary<(string, double, bool, uint), FormattedText> _pool = new();
        private double _dpi;

        public TextRenderer(double dpi) { _dpi = dpi; }
        public void SetDpi(double dpi) { _dpi = dpi; _pool.Clear(); }

        public FormattedText Get(string text, double size, Brush brush, bool bold)
        {
            uint bkey = brush is SolidColorBrush scb
                ? ((uint)scb.Color.A << 24) | ((uint)scb.Color.R << 16) | ((uint)scb.Color.G << 8) | scb.Color.B
                : 0xFFFFFFFF;
            var key = (text, size, bold, bkey);
            if (_pool.TryGetValue(key, out var ft) && ft.Width > 0) return ft;

            var tf = RenderResources.Typeface(bold: bold);
            ft = new FormattedText(text, CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                tf, size, brush, _dpi);

            if (_pool.Count > 512) _pool.Clear();
            _pool[key] = ft;
            return ft;
        }

        public void Clear() => _pool.Clear();
    }
}