using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace MemEase
{
    internal static class IconFactory
    {
        private static readonly int[] TraySizes = new int[] { 16, 20, 24, 32, 40, 48 };
        private static readonly int[] AppSizes = new int[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 };

        public static byte[] CreateTrayIcon(int loadPercent)
        {
            return BuildIcon(TraySizes, delegate(int size)
            {
                return DrawTray(size, loadPercent);
            });
        }

        public static byte[] CreateAppIcon()
        {
            return BuildIcon(AppSizes, delegate(int size)
            {
                return DrawApp(size);
            });
        }

        public static Color LoadColor(int loadPercent)
        {
            if (loadPercent >= 85) return Color.FromArgb(220, 38, 38);
            if (loadPercent >= 70) return Color.FromArgb(217, 119, 6);
            return Color.FromArgb(22, 163, 74);
        }

        private static Bitmap DrawTray(int size, int loadPercent)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.Clear(Color.Transparent);

                float scale = size;
                float chipWidth = scale * 0.62f;
                float chipHeight = scale * 0.40f;
                float left = (scale - chipWidth) / 2f;
                float top = (scale - chipHeight) / 2f;
                float pinWidth = Math.Max(1f, scale * 0.10f);
                float pinHeight = Math.Max(1f, scale * 0.075f);
                float pinGap = chipHeight / 4f;
                Color color = LoadColor(loadPercent);

                using (SolidBrush brush = new SolidBrush(color))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        float pinTop = top + pinGap * i - pinHeight / 2f;
                        graphics.FillRectangle(brush, left - pinWidth, pinTop, pinWidth + 1f, pinHeight);
                        graphics.FillRectangle(brush, left + chipWidth - 1f, pinTop, pinWidth + 1f, pinHeight);
                    }

                    using (GraphicsPath chip = RoundedRectangle(
                        left,
                        top,
                        chipWidth,
                        chipHeight,
                        Math.Max(1f, scale * 0.10f)))
                    {
                        graphics.FillPath(brush, chip);
                        using (Pen outline = new Pen(Darken(color, 0.72f), Math.Max(1f, scale * 0.035f)))
                        {
                            graphics.DrawPath(outline, chip);
                        }
                    }
                }

                using (SolidBrush contact = new SolidBrush(Color.FromArgb(238, 255, 255, 255)))
                {
                    float barWidth = Math.Max(1f, scale * 0.055f);
                    float barHeight = chipHeight * 0.44f;
                    float gap = chipWidth * 0.12f;
                    float totalWidth = barWidth * 3f + gap * 2f;
                    float startX = left + (chipWidth - totalWidth) / 2f;
                    float barTop = top + (chipHeight - barHeight) / 2f;
                    for (int i = 0; i < 3; i++)
                    {
                        graphics.FillRectangle(
                            contact,
                            startX + i * (barWidth + gap),
                            barTop,
                            barWidth,
                            barHeight);
                    }
                }
            }
            return bitmap;
        }

        private static Bitmap DrawApp(int size)
        {
            Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.SmoothingMode = SmoothingMode.AntiAlias;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.Clear(Color.Transparent);

                float scale = size;
                RectangleF background = new RectangleF(
                    scale * 0.025f,
                    scale * 0.025f,
                    scale * 0.95f,
                    scale * 0.95f);

                using (GraphicsPath path = RoundedRectangleF(background, Math.Max(2f, scale * 0.22f)))
                using (LinearGradientBrush brush = new LinearGradientBrush(
                    background,
                    Color.FromArgb(15, 118, 110),
                    Color.FromArgb(6, 78, 59),
                    LinearGradientMode.ForwardDiagonal))
                {
                    graphics.FillPath(brush, path);
                    using (Pen highlight = new Pen(Color.FromArgb(42, 255, 255, 255), Math.Max(1f, scale * 0.012f)))
                    {
                        graphics.DrawPath(highlight, path);
                    }
                }

                float chipWidth = scale * 0.54f;
                float chipHeight = scale * 0.34f;
                float left = (scale - chipWidth) / 2f;
                float top = (scale - chipHeight) / 2f;
                float pinWidth = Math.Max(1f, scale * 0.085f);
                float pinHeight = Math.Max(1f, scale * 0.062f);
                float pinGap = chipHeight / 4f;

                using (SolidBrush white = new SolidBrush(Color.White))
                {
                    for (int i = 1; i <= 3; i++)
                    {
                        float pinTop = top + pinGap * i - pinHeight / 2f;
                        graphics.FillRectangle(white, left - pinWidth, pinTop, pinWidth + 1f, pinHeight);
                        graphics.FillRectangle(white, left + chipWidth - 1f, pinTop, pinWidth + 1f, pinHeight);
                    }

                    using (GraphicsPath chip = RoundedRectangle(
                        left,
                        top,
                        chipWidth,
                        chipHeight,
                        Math.Max(1f, scale * 0.085f)))
                    {
                        graphics.FillPath(white, chip);
                    }
                }

                using (SolidBrush contact = new SolidBrush(Color.FromArgb(11, 103, 92)))
                {
                    float barWidth = Math.Max(1f, scale * 0.045f);
                    float barHeight = chipHeight * 0.44f;
                    float gap = chipWidth * 0.12f;
                    float totalWidth = barWidth * 3f + gap * 2f;
                    float startX = left + (chipWidth - totalWidth) / 2f;
                    float barTop = top + (chipHeight - barHeight) / 2f;
                    for (int i = 0; i < 3; i++)
                    {
                        graphics.FillRectangle(
                            contact,
                            startX + i * (barWidth + gap),
                            barTop,
                            barWidth,
                            barHeight);
                    }
                }
            }
            return bitmap;
        }

        private static byte[] BuildIcon(int[] sizes, Func<int, Bitmap> draw)
        {
            List<byte[]> frames = new List<byte[]>();
            for (int i = 0; i < sizes.Length; i++)
            {
                using (Bitmap bitmap = draw(sizes[i]))
                using (MemoryStream stream = new MemoryStream())
                {
                    bitmap.Save(stream, ImageFormat.Png);
                    frames.Add(stream.ToArray());
                }
            }

            using (MemoryStream output = new MemoryStream())
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)frames.Count);

                int offset = 6 + 16 * frames.Count;
                for (int i = 0; i < sizes.Length; i++)
                {
                    int size = sizes[i];
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write((uint)frames[i].Length);
                    writer.Write((uint)offset);
                    offset += frames[i].Length;
                }

                for (int i = 0; i < frames.Count; i++)
                {
                    writer.Write(frames[i]);
                }
                writer.Flush();
                return output.ToArray();
            }
        }

        private static GraphicsPath RoundedRectangle(float x, float y, float width, float height, float radius)
        {
            return RoundedRectangleF(new RectangleF(x, y, width, height), radius);
        }

        private static GraphicsPath RoundedRectangleF(RectangleF bounds, float radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            if (diameter > bounds.Width) diameter = bounds.Width;
            if (diameter > bounds.Height) diameter = bounds.Height;
            if (diameter <= 1f)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Darken(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                (int)Math.Max(0, Math.Min(255, color.R * factor)),
                (int)Math.Max(0, Math.Min(255, color.G * factor)),
                (int)Math.Max(0, Math.Min(255, color.B * factor)));
        }
    }
}
