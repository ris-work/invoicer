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
        /// WinForms only: hides native scrollbars, wraps this Scrollable in a 2×2 grid
        /// of custom scrollbars of 'thicknessPx'. Force-show each bar with alwaysShowH/V.
        /// </summary>
        public static Control WrapInCustomScrollbars(this Scrollable scrollable,
            bool alwaysShowH = false, bool alwaysShowV = false, int thicknessPx = 50)
        {
            Console.Error.WriteLine("▶ WrapInCustomScrollbars()");

            // 1) Platform guard
            var inst = Platform.Instance.ToString();
            var wfId = Platform.Get(Platforms.WinForms)?.ToString();
            Console.Error.WriteLine($"   Platform.Instance = {inst}");
            Console.Error.WriteLine($"   WinForms ID       = {wfId}");
            if (inst != wfId)
            {
                Console.Error.WriteLine("   ❌ Not WinForms. Aborting wrap.");
                return scrollable;
            }
            Console.Error.WriteLine("   ✅ Running on WinForms");

            // 2) Hide the built-in WinForms scrollbars
            var swc = scrollable.ControlObject as System.Windows.Forms.ScrollableControl;
            if (swc != null)
            {
                Console.Error.WriteLine("   Hiding native scrollbars...");
                swc.AutoScroll = false;
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }
            else
                Console.Error.WriteLine("   ⚠️   ControlObject is not WinForms ScrollableControl");

            // 3) Create our custom bars
            var vbar = new VScrollbar(scrollable, alwaysShowV, thicknessPx);
            var hbar = new HScrollbar(scrollable, alwaysShowH, thicknessPx);

            // 4) Corner filler
            var corner = new Panel
            {
                Width = thicknessPx,
                Height = thicknessPx,
                BackgroundColor = ColorSettings.BackgroundColor
            };

            // 5) Hook events to repaint bars & manually offset content
            scrollable.Scroll += (s, e) =>
            {
                Console.Error.WriteLine($"   scrollable.ScrollChanged → {scrollable.ScrollPosition}");
                if (swc != null && swc.Controls.Count > 0)
                {
                    var p = scrollable.ScrollPosition;
                    swc.Controls[0].Location = new System.Drawing.Point(-p.X, -p.Y);
                }
                vbar.Invalidate();
                hbar.Invalidate();
            };
            scrollable.SizeChanged += (s, e) => { Console.Error.WriteLine("   scrollable.SizeChanged"); vbar.Invalidate(); hbar.Invalidate(); };
            //scrollable.LayoutChanged += (s, e) => { Console.Error.WriteLine("   scrollable.LayoutChanged"); vbar.Invalidate(); hbar.Invalidate(); };
            if (scrollable.Content != null)
                scrollable.Content.SizeChanged += (s, e) =>
                {
                    Console.Error.WriteLine("   content.SizeChanged");
                    vbar.Invalidate();
                    hbar.Invalidate();
                };

            // 6) Build the 2×2 TableLayout exactly as you specified
            Console.Error.WriteLine("   Building TableLayout…");
            var table = new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow(
                        new TableCell(scrollable) { ScaleWidth = true },
                        new TableCell(vbar)
                    ) { ScaleHeight = true },

                    new TableRow(
                        new TableCell(hbar) { ScaleWidth = true },
                        new TableCell(corner)
                    )
                }
            };

            Console.Error.WriteLine("   Done – returning wrapped layout");
            return table;
        }

        #region Inner scrollbar classes

        class VScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;
            bool dragging;
            float dragOffset;

            public VScrollbar(Scrollable s, bool alwaysShow, int thickness)
            {
                scrollable = s;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Width = thickness;
                BackgroundColor = ColorSettings.LesserBackgroundColor;

                Paint += OnPaint;
                MouseDown += OnMouseDown;
                MouseMove += OnMouseMove;
                MouseUp += OnMouseUp;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var r = e.ClipRectangle;
                var pos = scrollable.ScrollPosition;
                var csize = scrollable.ClientSize;
                var ssize = scrollable.ScrollSize;
                Console.Error.WriteLine(
                  $"   VPaint: clip={r}, pos={pos}, client={csize}, scrollSize={ssize}, alwaysShow={alwaysShow}");

                var g = e.Graphics;
                g.FillRectangle(BackgroundColor, r);

                if (ssize.Height <= csize.Height && !alwaysShow)
                    return;

                // compute track
                var trackY = thick;
                var trackH = r.Height - 2 * thick;
                if (trackH <= 0) return;

                // draw up/down arrows
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width/2f,          10),
                    new PointF(10,                  thick - 10),
                    new PointF(r.Width - 10,        thick - 10),
                });
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(10,            r.Height - thick + 10),
                    new PointF(r.Width - 10,  r.Height - thick + 10),
                    new PointF(r.Width/2f,    r.Height - 10),
                });

                // draw thumb
                float frac = (ssize.Height == csize.Height) ? 0f : pos.Y / (float)(ssize.Height - csize.Height);
                float thumbH = Math.Max(thick, frac * trackH);
                float thumbY = trackY + (trackH - thumbH) * frac;
                var thumbRect = new RectangleF(0, thumbY, r.Width, thumbH);
                Console.Error.WriteLine($"     → VThumb @ {thumbRect}");
                g.FillRectangle(ColorSettings.ForegroundColor, thumbRect);
            }

            // VScrollbar.OnMouseDown
            void OnMouseDown(object sender, Eto.Forms.MouseEventArgs e)
            {
                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;
                var r = Bounds;

                Console.Error.WriteLine(
                  $"VMouseDown @ {e.Location}, pos={pos}, client={csize}, scrollSize={ssize}");

                // 1) Compute thumb area
                var trackY = thick;
                var trackH = r.Height - 2 * thick;
                if (trackH <= 0) return;

                float frac = (ssize.Height <= csize.Height)
                                 ? 0f
                                 : pos.Y / (float)(ssize.Height - csize.Height);
                float thumbH = Math.Max(thick, frac * trackH);
                float thumbY = trackY + (trackH - thumbH) * frac;
                var thumbRect = new Eto.Drawing.RectangleF(0, thumbY, r.Width, thumbH);

                // 1a) Start thumb‐drag?
                if (thumbRect.Contains(e.Location))
                {
                    dragging = true;
                    dragOffset = e.Location.Y - thumbY;
                    CaptureMouse();   // begin capturing all mouse moves
                    Console.Error.WriteLine($" → start V‐drag, offset={dragOffset}");
                    return;
                }

                // 2) Arrow‐click: page up / page down
                var step = csize.Height / 2;
                int newY = pos.Y;
                if (e.Location.Y < thick)
                    newY = Math.Max(0, pos.Y - step);
                else if (e.Location.Y > r.Height - thick)
                    newY = Math.Min(ssize.Height - csize.Height, pos.Y + step);

                Console.Error.WriteLine($" → V‐arrow click, scrolling to Y={newY}");
                scrollable.ScrollPosition = new Eto.Drawing.Point(pos.X, newY+100);
            }


            // VScrollbar.OnMouseMove
            void OnMouseMove(object sender, MouseEventArgs e)
            {
                if (!dragging) return;

                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;
                var r = Bounds;
                var scrollRange = ssize.Height - csize.Height;
                if (scrollRange <= 0) return;     // nothing to scroll

                // 1) Compute track & thumb sizes
                var trackY = thick;
                var trackH = r.Height - 2 * thick;
                if (trackH <= 0) return;

                // thumb height is proportional to viewport/content ratio
                float thumbH = Math.Max(
                    thick,
                    (csize.Height / (float)ssize.Height) * trackH
                );
                // how far the thumb can move
                float dragRange = trackH - thumbH;
                if (dragRange <= 0) return;

                // 2) Compute fraction within that range
                float delta = e.Location.Y - dragOffset - trackY;
                float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));

                // 3) Map to scroll position
                int newY = (int)(frac * scrollRange);

                Console.Error.WriteLine(
                  $"   VMouseMove → delta={delta:F1}, dragRange={dragRange:F1}, frac={frac:F2}, newY={newY}"
                );

                scrollable.ScrollPosition = new Eto.Drawing.Point(pos.X, newY);
            }


            void OnMouseUp(object sender, MouseEventArgs e)
            {
                if (!dragging) return;
                dragging = false;
                //Capt
                //CaptureMouse = false;
                
                Console.Error.WriteLine("   VMouseUp → stop dragging");
            }
        }

        class HScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;
            bool dragging;
            float dragOffset;

            public HScrollbar(Scrollable s, bool alwaysShow, int thickness)
            {
                scrollable = s;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Height = thickness;
                BackgroundColor = ColorSettings.LesserBackgroundColor;

                Paint += OnPaint;
                MouseDown += OnMouseDown;
                MouseMove += OnMouseMove;
                MouseUp += OnMouseUp;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var r = e.ClipRectangle;
                var pos = scrollable.ScrollPosition;
                var csize = scrollable.ClientSize;
                var ssize = scrollable.ScrollSize;
                Console.Error.WriteLine(
                  $"   HPaint: clip={r}, pos={pos}, client={csize}, scrollSize={ssize}, alwaysShow={alwaysShow}");

                var g = e.Graphics;
                g.FillRectangle(BackgroundColor, r);

                if (ssize.Width <= csize.Width && !alwaysShow)
                    return;

                // track region
                var trackX = thick;
                var trackW = r.Width - 2 * thick;
                if (trackW <= 0) return;

                // arrows
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(thick - 10,     10),
                    new PointF(10,             r.Height/2f),
                    new PointF(thick - 10,     r.Height - 10),
                });
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width - thick + 10, 10),
                    new PointF(r.Width - 10,         r.Height/2f),
                    new PointF(r.Width - thick + 10, r.Height - 10),
                });

                // thumb
                float frac = (ssize.Width == csize.Width) ? 0f : pos.X / (float)(ssize.Width - csize.Width);
                float thumbW = Math.Max(thick, frac * trackW);
                float thumbX = trackX + (trackW - thumbW) * frac;
                var thumbRect = new RectangleF(thumbX, 0, thumbW, r.Height);
                Console.Error.WriteLine($"     → HThumb @ {thumbRect}");
                g.FillRectangle(ColorSettings.ForegroundColor, thumbRect);
            }

            // HScrollbar.OnMouseDown
            void OnMouseDown(object sender, Eto.Forms.MouseEventArgs e)
            {
                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;
                var r = Bounds;

                Console.Error.WriteLine(
                  $"HMouseDown @ {e.Location}, pos={pos}, client={csize}, scrollSize={ssize}");

                // 1) Compute thumb area
                var trackX = thick;
                var trackW = r.Width - 2 * thick;
                if (trackW <= 0) return;

                float frac = (ssize.Width <= csize.Width)
                                 ? 0f
                                 : pos.X / (float)(ssize.Width - csize.Width);
                float thumbW = Math.Max(thick, frac * trackW);
                float thumbX = trackX + (trackW - thumbW) * frac;
                var thumbRect = new Eto.Drawing.RectangleF(thumbX, 0, thumbW, r.Height);

                // 1a) Start thumb‐drag?
                if (thumbRect.Contains(e.Location))
                {
                    dragging = true;
                    dragOffset = e.Location.X - thumbX;
                    CaptureMouse();   // begin capturing all mouse moves
                    Console.Error.WriteLine($" → start H‐drag, offset={dragOffset}");
                    return;
                }

                // 2) Arrow‐click: page left / page right
                var step = csize.Width / 2;
                int newX = pos.X;
                if (e.Location.X < thick)
                    newX = Math.Max(0, pos.X - step);
                else if (e.Location.X > r.Width - thick)
                    newX = Math.Min(ssize.Width - csize.Width, pos.X + step);

                Console.Error.WriteLine($" → H‐arrow click, scrolling to X={newX}");
                
                scrollable.ScrollPosition = new Eto.Drawing.Point(newX+100, pos.Y);
            }


            // HScrollbar.OnMouseMove
            void OnMouseMove(object sender, Eto.Forms.MouseEventArgs e)
            {
                if (!dragging) return;

                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;
                var r = Bounds;
                var scrollRange = ssize.Width - csize.Width;
                if (scrollRange <= 0) return;     // nothing to scroll

                // 1) Compute track & thumb sizes
                var trackX = thick;
                var trackW = r.Width - 2 * thick;
                if (trackW <= 0) return;

                // thumb width is proportional to viewport/content ratio
                float thumbW = Math.Max(
                    thick,
                    (csize.Width / (float)ssize.Width) * trackW
                );
                // how far the thumb can move
                float dragRange = trackW - thumbW;
                if (dragRange <= 0) return;

                // 2) Compute fraction within that range
                float delta = e.Location.X - dragOffset - trackX;
                float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));

                // 3) Map to scroll position
                int newX = (int)(frac * scrollRange);

                Console.Error.WriteLine(
                  $"   HMouseMove → delta={delta:F1}, dragRange={dragRange:F1}, frac={frac:F2}, newX={newX}"
                );

                scrollable.ScrollPosition = new Eto.Drawing.Point(newX, pos.Y);
            }


            void OnMouseUp(object sender, MouseEventArgs e)
            {
                if (!dragging) return;
                dragging = false;
                //CaptureMouse = false;
                Console.Error.WriteLine("   HMouseUp → stop dragging");
            }
        }

        #endregion
    }
}
