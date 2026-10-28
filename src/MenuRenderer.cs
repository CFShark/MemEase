using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace MemEase
{
    internal sealed class MemEaseRenderer : ToolStripProfessionalRenderer
    {
        public MemEaseRenderer()
            : base(new MemEaseColorTable())
        {
            RoundedEdges = false;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using (SolidBrush brush = new SolidBrush(Theme.MenuBack))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (Pen pen = new Pen(Theme.Border))
            {
                e.Graphics.DrawRectangle(
                    pen,
                    0,
                    0,
                    Math.Max(0, e.ToolStrip.Width - 1),
                    Math.Max(0, e.ToolStrip.Height - 1));
            }
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected || !e.Item.Enabled) return;
            Rectangle bounds = new Rectangle(3, 1, Math.Max(0, e.Item.Width - 6), Math.Max(0, e.Item.Height - 2));
            using (GraphicsPath path = RoundedRectangle(bounds, 5f))
            using (SolidBrush brush = new SolidBrush(Theme.Hover))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (!e.Item.Enabled)
            {
                e.TextColor = string.Equals(e.Item.Name, "header", StringComparison.Ordinal)
                    ? Theme.Text
                    : Theme.Muted;
            }
            else
            {
                e.TextColor = Theme.Text;
            }
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (Pen pen = new Pen(Theme.Separator))
            {
                e.Graphics.DrawLine(pen, 11, y, Math.Max(11, e.ToolStrip.Width - 11), y);
            }
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            Rectangle bounds = e.ImageRectangle;
            float x = bounds.Left + 4f;
            float y = bounds.Top + bounds.Height / 2f;

            using (Pen pen = new Pen(Theme.Accent, Math.Max(1.6f, e.Item.Height * 0.075f)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.DrawLines(
                    pen,
                    new PointF[]
                    {
                        new PointF(x, y),
                        new PointF(x + 3.5f, y + 3.5f),
                        new PointF(x + 9f, y - 4f)
                    });
            }
        }

        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Theme.Text;
            base.OnRenderArrow(e);
        }

        private static GraphicsPath RoundedRectangle(Rectangle bounds, float radius)
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

        private sealed class MemEaseColorTable : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground
            {
                get { return Theme.MenuBack; }
            }

            public override Color MenuBorder
            {
                get { return Theme.Border; }
            }

            public override Color MenuItemBorder
            {
                get { return Theme.Hover; }
            }

            public override Color MenuItemSelected
            {
                get { return Theme.Hover; }
            }

            public override Color ImageMarginGradientBegin
            {
                get { return Theme.MenuBack; }
            }

            public override Color ImageMarginGradientMiddle
            {
                get { return Theme.MenuBack; }
            }

            public override Color ImageMarginGradientEnd
            {
                get { return Theme.MenuBack; }
            }

            public override Color SeparatorDark
            {
                get { return Theme.Separator; }
            }

            public override Color SeparatorLight
            {
                get { return Theme.Separator; }
            }

            public override Color CheckBackground
            {
                get { return Theme.MenuBack; }
            }

            public override Color CheckSelectedBackground
            {
                get { return Theme.Hover; }
            }

            public override Color CheckPressedBackground
            {
                get { return Theme.Hover; }
            }
        }
    }
}
