// File: ScrollableExtensions.cs

using System;
using CommonUi; // for ColorSettings
using Eto;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Terminal.Gui;

namespace YourApp.Extensions
{
    public static class ScrollableExtensions
    {
        /// <summary>
        /// WinForms only: hides the native scrollbars and wraps this Scrollable
        /// in a 2×2 grid with custom 50px scrollbars (arrows + proportional thumb).
        /// No-op elsewhere.
        /// </summary>
        public static Control WrapInCustomScrollbars(
            this Scrollable scrollable,
            bool alwaysShowH = false,
            bool alwaysShowV = false,
            int thicknessPx = 50
        )
        {
            Console.Error.WriteLine("▶ WrapInCustomScrollbars()");
            var inst = Platform.Instance.ToString();
            var wf = Platform.Get(Platforms.WinForms)?.ToString();
            Console.Error.WriteLine($"   Platform.Instance={inst}, WinForms={wf}");
            if (inst != wf)
                return scrollable;

            // hide only the *visible* WinForms scrollbars—leave AutoScroll = true
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
                Console.Error.WriteLine(
                    $"===========Successfully casted ${swc.GetType()} to ScrollableControl============"
                );
                swc.HideNativeScrollBars();
            }
            else
            {
                Console.Error.WriteLine(
                    $"===========Not succeddful :( casting object to ScrollableControl============"
                );
            }
            // wrap it in our decorated panel
            return new ScrollableWithCustomBars(scrollable, alwaysShowH, alwaysShowV, thicknessPx);
        }
    }

    class ScrollableWithCustomBars : Panel
    {
        readonly Scrollable scrollable;
        readonly Drawable vbar,
            hbar;
        readonly Panel corner;
        readonly int thickness;
        readonly bool alwaysShowH,
            alwaysShowV;
        int scrollV = 0;
        int scrollH = 0;

        // drag parameters (initialized in MouseDown)
        int dragViewH; // viewport height at drag-start
        int dragContentH; // content height at drag-start
        int dragMaxOffset; // dragContentH - dragViewH
        float dragTrackH; // vbar.Height - 2*thickness
        float dragThumbH; // actual thumb height
        float dragRange; // dragTrackH - dragThumbH

        bool draggingH,
            draggingV;
        float dragOffX,
            dragOffY;

        public ScrollableWithCustomBars(
            Scrollable scrollable,
            bool alwaysShowH,
            bool alwaysShowV,
            int thickness
        )
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
            vbar = new Drawable
            {
                Width = thickness,
                BackgroundColor = ColorSettings.LesserBackgroundColor,
            };
            hbar = new Drawable
            {
                Height = thickness,
                BackgroundColor = ColorSettings.LesserBackgroundColor,
            };
            corner = new Panel
            {
                Width = thickness,
                Height = thickness,
                BackgroundColor = scrollable.BackgroundColor,
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
            scrollable.Scroll += (s, e) =>
            {
                vbar.Invalidate();
                hbar.Invalidate();
            };
            scrollable.SizeChanged += (s, e) =>
            {
                vbar.Invalidate();
                hbar.Invalidate();
            };

            // layout [scrollable|vbar] / [hbar|corner]
            Content = new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow(
                        new TableCell(scrollable) { ScaleWidth = true },
                        new TableCell(vbar)
                    )
                    {
                        ScaleHeight = true,
                    },
                    new TableRow(new TableCell(hbar) { ScaleWidth = true }, new TableCell(corner)),
                },
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
            System.Console.Error.WriteLine(
                $"viewW: {viewW}, viewH: {viewH}, contentW: {contentW}, contentH: {contentH}"
            );
        }

        // ---------- Vbar Paint ----------
        // -- PAINT THE VERTICAL BAR (Drawable) --
        void Vbar_Paint(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            var r = e.ClipRectangle;

            // 1) measure
            GetViewAndContentHeights(out var viewH, out var contentH);
            Console.Error.WriteLine($"[Paint] viewH={viewH}, contentH={contentH}");

            // 2) paint background
            g.FillRectangle(vbar.BackgroundColor, r);

            // 3) draw up‐arrow at top
            var cx = r.Width / 2f;
            const float inset = 4f;
            g.FillPolygon(
                ColorSettings.ForegroundColor,
                new[]
                {
                    new PointF(cx, inset),
                    new PointF(inset, thickness - inset),
                    new PointF(r.Width - inset, thickness - inset),
                }
            );

            // 4) draw down‐arrow at bottom
            g.FillPolygon(
                ColorSettings.ForegroundColor,
                new[]
                {
                    new PointF(inset, r.Height - thickness + inset),
                    new PointF(r.Width - inset, r.Height - thickness + inset),
                    new PointF(cx, r.Height - inset),
                }
            );

            // 5) nothing more if content fits
            if (contentH <= viewH)
                return;

            // 6) compute track & thumb
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(thickness, Math.Min(trackH, rawThumbH));
            float dragRange = trackH - thumbH;
            int maxOffset = contentH - viewH;

            // 7) clamp scrollV → thumbY
            scrollV = Math.Max(0, Math.Min(maxOffset, scrollV));
            float frac = scrollV / (float)maxOffset;
            float thumbY = trackY + frac * dragRange;

            Console.Error.WriteLine(
                $"[Paint] trackH={trackH:F1}, rawThumbH={rawThumbH:F1}, thumbH={thumbH:F1}, "
                    + $"dragRange={dragRange:F1}, maxOff={maxOffset}, scrollV={scrollV}, "
                    + $"frac={frac:F3}, thumbY={thumbY:F1}"
            );

            // 8) draw thumb
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
            GetViewAndContentWidths(out int viewW, out int contentW);
            bool canScroll = contentW > viewW;
            const float inset = 4f;
            // fill track
            g.FillRectangle(hbar.BackgroundColor, r);
            if (!canScroll && !alwaysShowH)
                return;
            int c = 2;
            // draw arrows
            g.FillPolygon(
                ColorSettings.ForegroundColor,
                new[]
                {
                    new PointF(thickness - c, c),
                    new PointF(c, r.Height / 2f),
                    new PointF(thickness - c, r.Height - c),
                }
            );
            g.FillPolygon(
                ColorSettings.ForegroundColor,
                new[]
                {
                    new PointF(r.Width - thickness + c, c),
                    new PointF(r.Width - c, r.Height / 2f),
                    new PointF(r.Width - thickness + c, r.Height - c),
                }
            );

            // thumb
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float rawThumbW = viewW / (float)contentW * trackW;
            float thumbW = Math.Max(thickness, Math.Min(trackW, rawThumbW));
            float dragRange = trackW - thumbW;
            int maxOffset = contentW - viewW;
            scrollH = Math.Max(0, Math.Min(maxOffset, scrollH));
            float frac = scrollH / (float)maxOffset;
            float thumbX = trackX + frac * dragRange;

            g.FillRectangle(
                ColorSettings.ForegroundColor,
                new RectangleF(thumbX, 0, thumbW, r.Height)
            );

            Console.Error.WriteLine(
                $"Hbar_Paint viewW={viewW} contentW={contentW} trackX={trackX} trackW={trackW} "
                    + $"thumbW={thumbW} dragRange={dragRange} thumbX={thumbX}"
            );
        }

        // ---------- Vbar Mouse ----------
        void Vbar_MouseDown(object sender, MouseEventArgs e)
        {
            // 1) measure once at drag‐start
            GetViewAndContentHeights(out var viewH, out var contentH);
            var r = vbar.Bounds;
            Console.Error.WriteLine(
                $"[MouseDown] clickY={e.Location.Y}, viewH={viewH}, contentH={contentH}"
            );
            float A = thickness;
            float trackH = vbar.Size.Height - 2 * A;
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(A, Math.Min(trackH, rawThumbH));
            float dragRange = trackH - thumbH;
            int maxOffset = contentH - viewH;

            var y = e.Location.Y;
            if (contentH <= viewH)
                return;

            if (scrollable.ControlObject is System.Windows.Forms.Panel sw)
            {
                var swc = sw.Controls[0];

                // arrow up?
                if (y < thickness)
                {
                    Console.Error.WriteLine(
                        $"Vbar_MouseDown: Arrow up, y: {y}, thickness: {thickness}"
                    );
                    var nY = -Math.Max(0, Math.Abs(swc.Location.Y) - thickness);
                    swc.Location = new System.Drawing.Point(swc.Location.X, nY);
                    Console.Error.WriteLine($"Vbar_MouseDown: Arrow up, location to: {nY}");
                    float dragRangeN = trackH - thumbH;
                    int maxOffsetN = contentH - viewH;
                    float frac = -nY / (float)maxOffsetN;
                    int newScroll = (int)Math.Round(frac * maxOffset);
                    scrollV = newScroll;
                    vbar.Invalidate(true);
                    return;
                }
                // arrow down?
                if (y > r.Height - thickness)
                {
                    Console.Error.WriteLine(
                        $"Vbar_MouseDown: Arrow down, y: {y}, thickness: {thickness}"
                    );
                    var nY = -Math.Min(contentH - viewH, Math.Abs(swc.Location.Y) + thickness);
                    swc.Location = new System.Drawing.Point(swc.Location.X, nY);
                    Console.Error.WriteLine($"Vbar_MouseDown: Arrow down, location to: {nY}");

                    float dragRangeN = trackH - thumbH;
                    int maxOffsetN = contentH - viewH;

                    float pos = (A + thumbH / 2f);
                    //float frac = -(nY - A + thumbH/2) / dragRangeN;
                    float frac = -nY / (float)maxOffsetN;
                    int newScroll = (int)Math.Round(frac * maxOffsetN);
                    scrollV = newScroll;
                    Console.Error.WriteLine(
                        $"Vbar_MouseDown: Arrow down, location to: nY: {nY}, pos: {pos}, frac: {frac}, maxOffsetN: {maxOffsetN}, newScroll: {newScroll}"
                    );
                    vbar.Invalidate(true);
                    return;
                }
            }

            // 2) compute thumb geometry

            Console.Error.WriteLine(
                $"[MouseDown] trackH={trackH:F1}, rawThumbH={rawThumbH:F1}, "
                    + $"thumbH={thumbH:F1}, dragRange={dragRange:F1}, maxOff={maxOffset}"
            );

            // 3) find current thumb‐rect
            float thumbY = A + (scrollV / (float)maxOffset) * dragRange;
            var thumbRect = new RectangleF(0, thumbY, vbar.Size.Width, thumbH);
            Console.Error.WriteLine($"[MouseDown] thumbRect={thumbRect}");

            // 4) begin drag if clicked inside
            if (thumbRect.Contains(e.Location))
            {
                draggingV = true;
                // record mouse→thumb‐center offset
                dragOffY = e.Location.Y - (thumbY + thumbH / 2f);
                Console.Error.WriteLine($"[MouseDown] dragOffY={dragOffY:F1}");
                vbar.CaptureMouse();
            }
        }

        // -- DRAGGING THE VERTICAL BAR --
        void Vbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingV)
                return;

            // 1) re‐measure (in case of resize mid‐drag)
            GetViewAndContentHeights(out var viewH, out var contentH);
            if (contentH <= viewH)
                return;

            // 2) recompute thumb math
            float A = thickness;
            float trackH = vbar.Size.Height - 2 * A;
            float rawThumbH = viewH / (float)contentH * trackH;
            float thumbH = Math.Max(A, Math.Min(trackH, rawThumbH));
            float dragRange = trackH - thumbH;
            int maxOffset = contentH - viewH;
            if (dragRange <= 0 || maxOffset <= 0)
                return;

            // 3) convert mouse→normalized pos
            float centerY = e.Location.Y - dragOffY;
            float pos = centerY - (A + thumbH / 2f);
            pos = Math.Max(0f, Math.Min(dragRange, pos));
            float frac = pos / dragRange;
            int newScroll = (int)Math.Round(frac * maxOffset);

            Console.Error.WriteLine(
                $"[MouseMove] eY={e.Location.Y}, centerY={centerY:F1}, "
                    + $"pos={pos:F1}, dragRange={dragRange:F1}, frac={frac:F3}, newScroll={newScroll}"
            );

            // 4) apply & move content
            scrollV = newScroll;
            if (
                scrollable.ControlObject is System.Windows.Forms.Panel swc
                && swc.Controls.Count > 0
            )
            {
                var contentPanel = swc.Controls[0];
                contentPanel.Location = new System.Drawing.Point(contentPanel.Location.X, -scrollV);
                Console.Error.WriteLine($"[MouseMove] contentPanel.Y={contentPanel.Location.Y}");
            }

            // 5) redraw
            vbar.Invalidate();
        }

        void Vbar_MouseUp(object sender, MouseEventArgs e)
        {
            if (!draggingV)
                return;
            draggingV = false;
            vbar.ReleaseMouseCapture();
        }

        // ---------- Hbar Mouse ----------
        void Hbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Hbar_MouseDown @ {e.Location}");
            GetViewAndContentWidths(out int viewW, out int contentW);
            var r = hbar.Bounds;
            var x = e.Location.X;

            if (scrollable.ControlObject is System.Windows.Forms.Panel sw)
            {
                var swc = sw.Controls[0];
                // arrow left?
                if (x < thickness)
                {
                    var TargetX = -Math.Max(0, Math.Abs(swc.Location.X) - thickness);
                    swc.Location = new System.Drawing.Point(TargetX, swc.Location.Y);
                    Console.Error.WriteLine(
                        $"HScrollbar_MouseDown: ({swc.Location.X}, {swc.Location.Y}) => ({TargetX}, {swc.Location.Y}) Early return: Left arrow"
                    );
                    int maxOffset = contentW - viewW;
                    float frac = Math.Max(0f, Math.Min(1f, Math.Abs(TargetX) - viewW / maxOffset));
                    int newScroll = (int)Math.Round(frac * maxOffset);
                    scrollH = newScroll;
                    hbar.Invalidate();
                    return;
                }
                // arrow right?
                if (x > r.Width - thickness)
                {
                    var TargetX = -Math.Min(contentW - viewW, Math.Abs(swc.Location.X) + thickness);
                    swc.Location = new System.Drawing.Point(TargetX, swc.Location.Y);
                    Console.Error.WriteLine(
                        $"HScrollbar_MouseDown: ({swc.Location.X}, {swc.Location.Y}) => ({TargetX}, {swc.Location.Y})  Early return: Right arrow"
                    );
                    int maxOffset = contentW - viewW;
                    float frac = Math.Max(0f, Math.Min(1f, Math.Abs(TargetX) - viewW / maxOffset));
                    int newScroll = (int)Math.Round(frac * maxOffset);
                    scrollH = newScroll;
                    hbar.Invalidate(true);
                    return;
                }
            }
            if (contentW <= viewW)
            {
                Console.Error.WriteLine(
                    $"HScrollbar_MouseDown: Early return: contentW = {contentW} < viewW = {viewW}"
                );
                return;
            }

            // thumb drag start?
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float contentRange = contentW - viewW;
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;
            float thumbX = trackX + (scrollable.ScrollPosition.X / contentRange) * dragRange;
            var thumbRect = new RectangleF(thumbX, 0, thumbW, r.Height);
            var thumbRectLeftMax = new RectangleF(0, 0, r.Width - dragRange - r.Height, r.Height);
            var thumbRectRightMax = new RectangleF(dragRange + r.Height, 0, r.Width, r.Height);
            bool ThumbRightOOB = thumbX > dragRange + r.Height;
            bool ThumbLeftOOB = thumbX < 0;
            if (
                thumbRect.Contains(e.Location)
                || (ThumbRightOOB && thumbRectRightMax.Contains(e.Location))
                || (ThumbLeftOOB && thumbRectLeftMax.Contains(e.Location))
            )
            {
                draggingH = true;
                dragOffX = e.Location.X - thumbX;
                hbar.CaptureMouse();
                //Console.Error.WriteLine($"Hbar startDrag dragOffX={dragOffX} thumbX={thumbX} thumbRightOOB={ThumbRightOOB}, thumbLeftOOB={ThumbLeftOOB}");
            }
            Console.Error.WriteLine(
                $"Hbar startDrag dragOffX={dragOffX} thumbX={thumbX} thumbRightOOB={ThumbRightOOB}, thumbLeftOOB={ThumbLeftOOB}"
            );
        }

        void Hbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingH)
                return;
            var r = hbar.Bounds;
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            GetViewAndContentWidths(out int viewW, out int contentW);
            float contentRange = Math.Max(1, contentW - viewW);
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;

            float delta = e.Location.X - dragOffX - trackX;
            float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
            int newX = (int)(frac * contentRange);
            int maxOffset = contentW - viewW;
            int newScroll = (int)Math.Round(frac * maxOffset);

            scrollH = newScroll;
            if (
                scrollable.ControlObject is System.Windows.Forms.Panel swc
                && swc.Controls.Count > 0
            )
            {
                var contentPanel = swc.Controls[0];
                contentPanel.Location = new System.Drawing.Point(-scrollH, contentPanel.Location.Y);
                Console.Error.WriteLine($"[MouseMove] contentPanel.X={contentPanel.Location.X}");
            }

            //scrollable.ScrollPosition = new Point(newX, scrollable.ScrollPosition.Y);
            Console.Error.WriteLine(
                $"Hbar_MouseMove delta={delta:F1} dragRange={dragRange:F1} frac={frac:F2} newX={newX}"
            );
            hbar.Invalidate(true);
        }

        void Hbar_MouseUp(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine("Hbar_MouseUp()");
            if (draggingH)
            {
                draggingH = false;
                hbar.ReleaseMouseCapture();
            }
            hbar.Invalidate(true);
        }

        void GetViewAndContentHeights(out int viewH, out int contentH)
        {
            viewH = scrollable.ClientSize.Height;
            contentH = 0;

            if (scrollable.ControlObject is System.Windows.Forms.Panel swc)
            {
                int LY = 0;
                if (swc.Controls.Count > 0)
                {
                    LY = swc.Controls[0].Location.Y;
                }
                foreach (System.Windows.Forms.Control c in swc.Controls)
                    contentH = +Math.Max(+contentH, Math.Abs(LY) + c.Bottom);
                System.Console.Error.WriteLine(
                    $"Scroll height: LocationY:{swc.Location.Y} LocationYChild:{LY} viewH: {viewH}, contentH: {contentH}"
                );
            }
        }

        void GetViewAndContentWidths(out int viewW, out int contentW)
        {
            viewW = scrollable.ClientSize.Width;
            contentW = 0;

            if (scrollable.ControlObject is System.Windows.Forms.Panel swc)
            {
                int LX = 0;
                if (swc.Controls.Count > 0)
                {
                    LX = swc.Controls[0].Location.X;
                }
                foreach (System.Windows.Forms.Control c in swc.Controls)
                    contentW = +Math.Max(+contentW, Math.Abs(LX) + c.Right);
                System.Console.Error.WriteLine(
                    $"Scroll height: LocationY:{swc.Location.X} LocationYChild:{LX} viewW: {viewW}, contentW: {contentW}"
                );
            }
        }
    }
}
