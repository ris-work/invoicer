// File: ScrollableExtensions.cs

using System;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;    // for ColorSettings
using Eto;

namespace YourApp.Extensions
{
    public static class ScrollableExtensions
    {
        /// <summary>
        /// WinForms only: hides the native scrollbars and wraps this Scrollable
        /// in a 2×2 grid with custom 50px scrollbars (arrows + proportional thumb).
        /// No-op elsewhere.
        /// </summary>
        public static Control WrapInCustomScrollbars(this Scrollable scrollable,
            bool alwaysShowH = false, bool alwaysShowV = false, int thicknessPx = 50)
        {
            Console.Error.WriteLine("▶ WrapInCustomScrollbars()");
            var inst = Platform.Instance.ToString();
            var wf = Platform.Get(Platforms.WinForms)?.ToString();
            Console.Error.WriteLine($"   Platform.Instance={inst}, WinForms={wf}");
            if (inst != wf) return scrollable;

            // hide only the *visible* WinForms scrollbars—leave AutoScroll = true
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
                Console.Error.WriteLine($"===========Successfully casted ${swc.GetType()} to ScrollableControl============");
                swc.HideNativeScrollBars();
            }
            else {
                Console.Error.WriteLine($"===========Not succeddful :( casting object to ScrollableControl============");
            }
            // wrap it in our decorated panel
                return new ScrollableWithCustomBars(scrollable, alwaysShowH, alwaysShowV, thicknessPx);
        }
    }

    class ScrollableWithCustomBars : Panel
    {
        readonly Scrollable scrollable;
        readonly Drawable vbar, hbar;
        readonly Panel corner;
        readonly int thickness;
        readonly bool alwaysShowH, alwaysShowV;

        bool draggingH, draggingV;
        float dragOffX, dragOffY;

        public ScrollableWithCustomBars(Scrollable scrollable,
            bool alwaysShowH, bool alwaysShowV, int thickness)
        {
            Console.Error.WriteLine("▶ ScrollableWithCustomBars()");
            this.scrollable = scrollable;
            this.thickness = thickness;
            this.alwaysShowH = alwaysShowH;
            this.alwaysShowV = alwaysShowV;

            // re-hide scrollbars in case someone reset them
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }

            // build two 50px tracks
            vbar = new Drawable { Width = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };
            hbar = new Drawable { Height = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };
            corner = new Panel
            {
                Width = thickness,
                Height = thickness,
                BackgroundColor = scrollable.BackgroundColor
            };

            // hook paint
            vbar.Paint += Vbar_Paint;
            hbar.Paint += Hbar_Paint;

            // hook mouse
            vbar.MouseDown += Vbar_MouseDown;
            vbar.MouseMove += Vbar_MouseMove;
            vbar.MouseUp += Vbar_MouseUp;

            hbar.MouseDown += Hbar_MouseDown;
            hbar.MouseMove += Hbar_MouseMove;
            hbar.MouseUp += Hbar_MouseUp;

            // repaint on any change
            scrollable.Scroll += (s, e) => { vbar.Invalidate(); hbar.Invalidate(); };
            scrollable.SizeChanged += (s, e) => { vbar.Invalidate(); hbar.Invalidate(); };

            // layout [scrollable|vbar] / [hbar|corner]
            Content = new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow(
                        new TableCell(scrollable) { ScaleWidth  = true },
                        new TableCell(vbar)
                    ) { ScaleHeight = true },

                    new TableRow(
                        new TableCell(hbar)    { ScaleWidth = true },
                        new TableCell(corner)
                    )
                }
            };
        }

        // read the true WinForms sizes
        void GetSizes(out int viewW, out int viewH, out int contentW, out int contentH)
        {
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                viewW = swc.ClientSize.Width;
                viewH = swc.ClientSize.Height;
                contentW = swc.DisplayRectangle.Width;
                contentH = swc.DisplayRectangle.Height;
            }
            else
            {
                // fallback
                viewW = scrollable.ClientSize.Width;
                viewH = scrollable.ClientSize.Height;
                contentW = scrollable.ScrollSize.Width;
                contentH = scrollable.ScrollSize.Height;
            }
        }

        // ---------- Vbar Paint ----------
        void Vbar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ClipRectangle;
            GetSizes(out _, out int viewH, out _, out int contentH);
            bool canScroll = contentH > viewH;

            // fill track
            g.FillRectangle(vbar.BackgroundColor, r);
            if (!canScroll && !alwaysShowV) return;

            // draw arrows
            var cx = r.Width / 2f;
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(cx,                10),
                new PointF(10,               thickness-10),
                new PointF(r.Width-10,       thickness-10)
            });
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(10,               r.Height-thickness+10),
                new PointF(r.Width-10,       r.Height-thickness+10),
                new PointF(cx,               r.Height-10)
            });

            // thumb
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float contentRange = Math.Max(1, contentH - viewH);
            float thumbH = canScroll
                ? Math.Max(thickness, viewH / (float)contentH * trackH)
                : trackH;
            float dragRange = trackH - thumbH;
            float thumbY = canScroll
                ? trackY + (scrollable.ScrollPosition.Y / contentRange) * dragRange
                : trackY;

            g.FillRectangle(
                ColorSettings.ForegroundColor,
                new RectangleF(0, thumbY, r.Width, thumbH)
            );

            // one-line debug
            Console.Error.WriteLine(
                $"Vbar_Paint viewH={viewH} contentH={contentH} trackY={trackY} trackH={trackH} " +
                $"thumbH={thumbH} dragRange={dragRange} thumbY={thumbY}"
            );
        }

        // ---------- Hbar Paint ----------
        void Hbar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ClipRectangle;
            GetSizes(out int viewW, out _, out int contentW, out _);
            bool canScroll = contentW > viewW;

            // fill track
            g.FillRectangle(hbar.BackgroundColor, r);
            if (!canScroll && !alwaysShowH) return;

            // draw arrows
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(thickness-10,   10),
                new PointF(10,             r.Height/2f),
                new PointF(thickness-10,   r.Height-10)
            });
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(r.Width-thickness+10, 10),
                new PointF(r.Width-10,            r.Height/2f),
                new PointF(r.Width-thickness+10, r.Height-10)
            });

            // thumb
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float contentRange = Math.Max(1, contentW - viewW);
            float thumbW = canScroll
                ? Math.Max(thickness, viewW / (float)contentW * trackW)
                : trackW;
            float dragRange = trackW - thumbW;
            float thumbX = canScroll
                ? trackX + (scrollable.ScrollPosition.X / contentRange) * dragRange
                : trackX;

            g.FillRectangle(
                ColorSettings.ForegroundColor,
                new RectangleF(thumbX, 0, thumbW, r.Height)
            );

            Console.Error.WriteLine(
                $"Hbar_Paint viewW={viewW} contentW={contentW} trackX={trackX} trackW={trackW} " +
                $"thumbW={thumbW} dragRange={dragRange} thumbX={thumbX}"
            );
        }

        // ---------- Vbar Mouse ----------
        void Vbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Vbar_MouseDown @ {e.Location}");
            GetSizes(out _, out int viewH, out _, out int contentH);
            var r = vbar.Bounds;
            var y = e.Location.Y;

            // arrow up?
            if (y < thickness)
            {
                scrollable.ScrollPosition = new Point(
                    scrollable.ScrollPosition.X,
                    Math.Max(0, scrollable.ScrollPosition.Y - thickness)
                );
                return;
            }
            // arrow down?
            if (y > r.Height - thickness)
            {
                scrollable.ScrollPosition = new Point(
                    scrollable.ScrollPosition.X,
                    Math.Min(contentH - viewH,
                             scrollable.ScrollPosition.Y + thickness)
                );
                return;
            }
            if (contentH <= viewH) return;

            // thumb drag start?
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float contentRange = contentH - viewH;
            float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
            float dragRange = trackH - thumbH;
            float thumbY = trackY + (scrollable.ScrollPosition.Y / contentRange) * dragRange;
            var thumbRect = new RectangleF(0, thumbY, r.Width, thumbH);
            if (thumbRect.Contains(e.Location))
            {
                draggingV = true;
                dragOffY = e.Location.Y - thumbY;
                vbar.CaptureMouse();
                Console.Error.WriteLine(
                    $"Vbar startDrag dragOffY={dragOffY} thumbY={thumbY}"
                );
            }
        }

        void Vbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingV) return;
            var r = vbar.Bounds;
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            GetSizes(out _, out int viewH, out _, out int contentH);
            float contentRange = Math.Max(1, contentH - viewH);
            float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
            float dragRange = trackH - thumbH;

            float delta = e.Location.Y - dragOffY - trackY;
            float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
            int newY = (int)(frac * contentRange);

            scrollable.ScrollPosition = new Point(scrollable.ScrollPosition.X, newY);
            Console.Error.WriteLine(
                $"Vbar_MouseMove delta={delta:F1} dragRange={dragRange:F1} frac={frac:F2} newY={newY}"
            );
            vbar.Invalidate();
        }

        void Vbar_MouseUp(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine("Vbar_MouseUp()");
            if (draggingV)
            {
                draggingV = false;
                vbar.ReleaseMouseCapture();
            }
            vbar.Invalidate();
        }

        // ---------- Hbar Mouse ----------
        void Hbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Hbar_MouseDown @ {e.Location}");
            GetSizes(out int viewW, out _, out int contentW, out _);
            var r = hbar.Bounds;
            var x = e.Location.X;

            // arrow left?
            if (x < thickness)
            {
                scrollable.ScrollPosition = new Point(
                    Math.Max(0, scrollable.ScrollPosition.X - thickness),
                    scrollable.ScrollPosition.Y
                );
                return;
            }
            // arrow right?
            if (x > r.Width - thickness)
            {
                scrollable.ScrollPosition = new Point(
                    Math.Min(contentW - viewW,
                             scrollable.ScrollPosition.X + thickness),
                    scrollable.ScrollPosition.Y
                );
                return;
            }
            if (contentW <= viewW) return;

            // thumb drag start?
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float contentRange = contentW - viewW;
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;
            float thumbX = trackX + (scrollable.ScrollPosition.X / contentRange) * dragRange;
            var thumbRect = new RectangleF(thumbX, 0, thumbW, r.Height);
            if (thumbRect.Contains(e.Location))
            {
                draggingH = true;
                dragOffX = e.Location.X - thumbX;
                hbar.CaptureMouse();
                Console.Error.WriteLine(
                    $"Hbar startDrag dragOffX={dragOffX} thumbX={thumbX}"
                );
            }
        }

        void Hbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingH) return;
            var r = hbar.Bounds;
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            GetSizes(out int viewW, out _, out int contentW, out _);
            float contentRange = Math.Max(1, contentW - viewW);
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;

            float delta = e.Location.X - dragOffX - trackX;
            float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
            int newX = (int)(frac * contentRange);

            scrollable.ScrollPosition = new Point(newX, scrollable.ScrollPosition.Y);
            Console.Error.WriteLine(
                $"Hbar_MouseMove delta={delta:F1} dragRange={dragRange:F1} frac={frac:F2} newX={newX}"
            );
            hbar.Invalidate();
        }

        void Hbar_MouseUp(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine("Hbar_MouseUp()");
            if (draggingH)
            {
                draggingH = false;
                hbar.ReleaseMouseCapture();
            }
            hbar.Invalidate();
        }
    }
}
