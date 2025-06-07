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
        int scrollV = 0;

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
            Size GetContentSize(System.Windows.Forms.Control container)
            {
                var w = 0;
                var h = 0;
                foreach (System.Windows.Forms.Control c in container.Controls)
                {
                    // Right/bottom edge of each child
                    w = Math.Max(w, c.Right);
                    h = Math.Max(h, c.Bottom);
                }
                return new Size(w, h);
            }

            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                var S = GetContentSize(swc);
                viewW = swc.ClientSize.Width;
                viewH = swc.ClientSize.Height;
                /*contentW = swc.DisplayRectangle.Width;
                contentH = swc.DisplayRectangle.Height;*/
                contentW = S.Width;
                contentH = S.Height;
            }
            else
            {
                // fallback
                viewW = scrollable.ClientSize.Width;
                viewH = scrollable.ClientSize.Height;
                contentW = scrollable.ScrollSize.Width;
                contentH = scrollable.ScrollSize.Height;
            }
            System.Console.Error.WriteLine($"viewW: {viewW}, viewH: {viewH}, contentW: {contentW}, contentH: {contentH}");
        }

        // ---------- Vbar Paint ----------
        // -- PAINT THE VERTICAL BAR (Drawable) --
        void Vbar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ClipRectangle;            // the full Drawable area
            GetSizes(out _, out int viewH, out _, out int contentH);

            // background
            g.FillRectangle(vbar.BackgroundColor, r);

            // draw up/down arrows
            var cx = r.Width / 2f;
            const int inset = 4;
            // — up arrow (pointing up) at Y=[0…thickness]
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
        new PointF(cx,        inset),                 // tip
        new PointF(inset,     thickness-inset),       // base-left
        new PointF(r.Width-inset, thickness-inset),   // base-right
    });
            // — down arrow (pointing down) at Y=[r.Height-thickness…r.Height]
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
        new PointF(inset,     r.Height-thickness+inset), // base-left
        new PointF(r.Width-inset, r.Height-thickness+inset), // base-right
        new PointF(cx,        r.Height-inset),           // tip
    });

            // if no need to scroll, we’re done
            if (contentH <= viewH)
                return;

            // compute track inside the arrows
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            if (trackH <= 0) return;

            // how many pixels the content can scroll
            int maxOffset = contentH - viewH;
            float contentRange = maxOffset;

            // thumb height ∝ view/content, clamped to [thickness…trackH]
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(thickness, Math.Min(trackH, rawThumbH));

            // distance the thumb may travel
            float dragRange = trackH - thumbH;
            if (dragRange <= 0) return;

            // clamp scrollV into its valid range [0…maxOffset]
            scrollV = Math.Max(0, Math.Min(maxOffset, scrollV));

            // compute the fraction [0…1] of where the thumb should sit
            float frac = scrollV / (float)contentRange;
            float thumbY = trackY + frac * dragRange;

            // draw the thumb
            g.FillRectangle(
                ColorSettings.ForegroundColor,
                new RectangleF(0, thumbY, r.Width, thumbH)
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
            GetSizes(out _, out int viewH, out _, out int contentH);
            if (contentH <= viewH) return;  // nothing to scroll

            // recompute exactly what the thumb’s rect is (same as in Paint)
            int barH = vbar.Size.Height;
            float trackY = thickness;
            float trackH = barH - 2 * thickness;
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(thickness, Math.Min(trackH, rawThumbH));
            float dragRange = trackH - thumbH;
            if (dragRange <= 0) return;

            // find the current thumb Y
            int maxOffset = contentH - viewH;
            float frac = scrollV / (float)maxOffset;
            float thumbY = trackY + frac * dragRange;

            var thumbRect = new RectangleF(0, thumbY, vbar.Size.Width, thumbH);
            if (thumbRect.Contains(e.Location.X, e.Location.Y))
            {
                draggingV = true;
                dragOffY = e.Location.Y - thumbY;
                vbar.CaptureMouse();
            }
        }

        // -- DRAGGING THE VERTICAL BAR --
        void Vbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingV) return;

            GetSizes(out _, out int viewH, out _, out int contentH);
            if (contentH <= viewH) return;

            // recompute track & thumb sizes
            int barH = vbar.Size.Height;
            float trackY = thickness;
            float trackH = barH - 2 * thickness;
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(thickness, Math.Min(trackH, rawThumbH));
            float dragRange = trackH - thumbH;
            if (dragRange <= 0) return;

            // how far the user moved the mouse within the track
            float rawPos = e.Location.Y - dragOffY - trackY;
            float clamped = Math.Max(0f, Math.Min(dragRange, rawPos));
            float frac = clamped / dragRange;

            // map [0…1] → [0…maxOffset]
            int maxOffset = contentH - viewH;
            scrollV = (int)Math.Round(frac * maxOffset);

            // slide your content panel *only* by –scrollV
            if (scrollable.ControlObject is System.Windows.Forms.Panel swc
                && swc.Controls.Count > 0)
            {
                var contentPanel = swc.Controls[0];
                contentPanel.Location = new System.Drawing.Point(
                    contentPanel.Location.X,
                    -scrollV
                );
                swc.Invalidate(); // repaint content if needed
            }

            vbar.Invalidate(); // redraw the thumb
        }


        void Vbar_MouseUp(object sender, MouseEventArgs e)
        {
            if (!draggingV) return;
            draggingV = false;
            vbar.ReleaseMouseCapture();
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
