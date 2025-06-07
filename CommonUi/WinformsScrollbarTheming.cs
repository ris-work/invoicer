// File: ScrollableExtensions.cs

using System;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;   // for ColorSettings
using Eto;

namespace YourApp.Extensions
{
    public static class ScrollableExtensions
    {
        /// <summary>
        /// On WinForms only, hides the native scrollbars of this Scrollable and wraps it
        /// in a 2×2 grid of custom scrollbars of 'thicknessPx' size.  You can force-show
        /// each bar with alwaysShowH / alwaysShowV.
        /// </summary>
        public static Control WrapInCustomScrollbars(this Scrollable scrollable,
            bool alwaysShowH = false, bool alwaysShowV = false, int thicknessPx = 50)
        {
            // platform-check
            var wfId = Platform.Get(Platforms.WinForms)?.ToString();
            if (Platform.Instance.ToString() != wfId)
            {
                Console.Error.WriteLine("[DBG] Not WinForms – skipping custom scrollbars");
                return scrollable;
            }

            // grab underlying WinForms panel and disable its native scrollbars
            var swc = scrollable.ControlObject as System.Windows.Forms.ScrollableControl;
            if (swc != null)
            {
                swc.AutoScroll = false;
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }

            // when ScrollPosition changes, manually offset the content
            scrollable.Scroll += (s, e) =>
            {
                if (swc != null && swc.Controls.Count > 0)
                {
                    var p = scrollable.ScrollPosition;
                    swc.Controls[0].Location = new System.Drawing.Point(-p.X, -p.Y);
                }
            };

            // re‐draw our bars on size/content changes
            scrollable.SizeChanged += (s, e) => scrollable.Invalidate();
            if (scrollable.Content != null)
                scrollable.Content.SizeChanged += (s, e) => scrollable.Invalidate();

            // our custom bars
            var vbar = new VScrollbar(scrollable, alwaysShowV, thicknessPx);
            var hbar = new HScrollbar(scrollable, alwaysShowH, thicknessPx);

            // bottom‐right filler
            var corner = new Panel
            {
                Width = thicknessPx,
                Height = thicknessPx,
                BackgroundColor = ColorSettings.BackgroundColor
            };

            // assemble a 2×2 table:
            // [ scrollable | vbar   ]
            // [ hbar       | corner ]
            return new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow(
                        new TableCell(scrollable) { ScaleWidth = true,   },
                        new TableCell(vbar)       {  }
                    ){ ScaleHeight = true},
                    new TableRow(
                        new TableCell(hbar)       { ScaleWidth = true },
                        new TableCell(corner)
                    )
                }
            };
        }

        #region Inner scrollbar classes

        class VScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;

            public VScrollbar(Scrollable scrollable, bool alwaysShow, int thickness)
            {
                this.scrollable = scrollable;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Width = thickness;
                BackgroundColor = ColorSettings.LesserBackgroundColor;
                Paint += OnPaint;
                MouseDown += OnMouseDown;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                var r = e.ClipRectangle;
                g.FillRectangle(BackgroundColor, r);

                var contentH = scrollable.ScrollSize.Height;
                var viewH = scrollable.ClientSize.Height;
                if (contentH <= viewH && !alwaysShow) return;

                // up‐arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width/2f,   10),
                    new PointF(10,           thick-10),
                    new PointF(r.Width-10,   thick-10),
                });

                // down‐arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(10,            r.Height-thick+10),
                    new PointF(r.Width-10,    r.Height-thick+10),
                    new PointF(r.Width/2f,    r.Height-10),
                });
            }

            void OnMouseDown(object sender, MouseEventArgs e)
            {
                var contentH = scrollable.ScrollSize.Height;
                var viewH = scrollable.ClientSize.Height;
                if (contentH <= viewH) return;

                var pos = scrollable.ScrollPosition;
                var step = viewH / 2;
                var newY = pos.Y;
                var r = Bounds;

                if (e.Location.Y <= thick)
                    newY = Math.Max(0, pos.Y - step);
                else if (e.Location.Y >= r.Height - thick)
                    newY = Math.Min(contentH - viewH, pos.Y + step);

                scrollable.ScrollPosition = new Eto.Drawing.Point(pos.X, newY);
            }
        }

        class HScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;

            public HScrollbar(Scrollable scrollable, bool alwaysShow, int thickness)
            {
                this.scrollable = scrollable;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Height = thickness;
                BackgroundColor = ColorSettings.LesserBackgroundColor;
                Paint += OnPaint;
                MouseDown += OnMouseDown;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var g = e.Graphics;
                var r = e.ClipRectangle;
                g.FillRectangle(BackgroundColor, r);

                var contentW = scrollable.ScrollSize.Width;
                var viewW = scrollable.ClientSize.Width;
                if (contentW <= viewW && !alwaysShow) return;

                // left‐arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(thick-10,      10),
                    new PointF(10,            r.Height/2f),
                    new PointF(thick-10,      r.Height-10),
                });

                // right‐arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width-thick+10, 10),
                    new PointF(r.Width-10,         r.Height/2f),
                    new PointF(r.Width-thick+10,   r.Height-10),
                });
            }

            void OnMouseDown(object sender, MouseEventArgs e)
            {
                var contentW = scrollable.ScrollSize.Width;
                var viewW = scrollable.ClientSize.Width;
                if (contentW <= viewW) return;

                var pos = scrollable.ScrollPosition;
                var step = viewW / 2;
                var newX = pos.X;
                var r = Bounds;

                if (e.Location.X <= thick)
                    newX = Math.Max(0, pos.X - step);
                else if (e.Location.X >= r.Width - thick)
                    newX = Math.Min(contentW - viewW, pos.X + step);

                scrollable.ScrollPosition = new Eto.Drawing.Point(newX, pos.Y);
            }
        }

        #endregion
    }
}
