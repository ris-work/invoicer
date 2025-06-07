// File: ScrollableExtensions.cs

using System;
using Eto.Forms;
using Eto.Drawing;
using CommonUi;
using Mono.Unix;    // for ColorSettings
using Eto;

namespace YourApp.Extensions
{
    public static class ScrollableExtensions
    {
        /// <summary>
        /// WinForms only: hides native scrollbars and wraps this Scrollable’s content
        /// in a clipped Panel plus custom 50px scrollbars (arrows + draggable thumb).
        /// No-op elsewhere.
        /// </summary>
        public static Control WrapInCustomScrollbars(this Scrollable scrollable,
            bool alwaysShowH = false, bool alwaysShowV = false, int thicknessPx = 50)
        {
            Console.Error.WriteLine("▶ WrapInCustomScrollbars()");
            var inst = Platform.Instance.ToString();
            var wfId = Platform.Get(Platforms.WinForms)?.ToString();
            Console.Error.WriteLine($"   Platform.Instance={inst}, WinForms={wfId}");
            if (inst != wfId)
            {
                Console.Error.WriteLine("   ❌ Not WinForms – returning original Scrollable");
                return scrollable;
            }

            // Hide the native WinForms scrollbars on the underlying control
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                Console.Error.WriteLine("   Hiding native WinForms scrollbars");
                swc.AutoScroll = false;
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }

            // Pull out the content
            var content = scrollable.Content;
            scrollable.Content = null;
            Console.Error.WriteLine($"   Extracted content: {content}");

            // Create our custom viewer
            var viewer = new CustomScrollViewer(content, alwaysShowH, alwaysShowV, thicknessPx);
            Console.Error.WriteLine("   Returning CustomScrollViewer");
            return viewer;
        }
    }

    /// <summary>
    /// A Panel that hosts your content in a clipped 'viewport' and draws two
    /// fixed-thickness scrollbars (arrows + draggable thumb) on WinForms.
    /// </summary>
    internal class CustomScrollViewer : Panel
    {
        readonly Panel viewport;
        readonly Drawable vbar, hbar;
        readonly Panel corner;
        readonly Control content;
        readonly int thickness;
        readonly bool alwaysShowH, alwaysShowV;

        int scrollX, scrollY;
        bool draggingH, draggingV;
        float dragOffX, dragOffY;

        public CustomScrollViewer(Control content, bool alwaysShowH, bool alwaysShowV, int thickness)
        {
            Console.Error.WriteLine("▶ CustomScrollViewer.ctor()");
            this.content = content;
            this.alwaysShowH = alwaysShowH;
            this.alwaysShowV = alwaysShowV;
            this.thickness = thickness;

            // 1) Create the clipped viewport panel
            viewport = new Panel
            {
                //Border = BorderType.None,
                BackgroundColor = ColorSettings.BackgroundColor,
                Content = content
            };
            if (viewport.ControlObject is System.Windows.Forms.Panel vp)
            {
                Console.Error.WriteLine("   Hiding viewport's native scrollbars");
                vp.AutoScroll = false;
                vp.HorizontalScroll.Visible = false;
                vp.VerticalScroll.Visible = false;
            }

            // 2) Create the two Drawable scrollbars
            vbar = new Drawable { Width = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };
            hbar = new Drawable { Height = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };

            // 3) Create the bottom-right filler
            corner = new Panel
            {
                Width = thickness,
                Height = thickness,
                BackgroundColor = ColorSettings.BackgroundColor
            };

            // 4) Hook painting events
            vbar.Paint += Vbar_Paint;
            hbar.Paint += Hbar_Paint;

            // 5) Hook mouse events
            vbar.MouseDown += Vbar_MouseDown;
            vbar.MouseMove += Vbar_MouseMove;
            vbar.MouseUp += Vbar_MouseUp;

            hbar.MouseDown += Hbar_MouseDown;
            hbar.MouseMove += Hbar_MouseMove;
            hbar.MouseUp += Hbar_MouseUp;

            // 6) Repaint bars on resize/size-changed
            viewport.SizeChanged += (s, e) =>
            {
                Console.Error.WriteLine("viewport.SizeChanged");
                InvalidateBars();
            };
            if (content != null)
                content.SizeChanged += (s, e) =>
                {
                    Console.Error.WriteLine("content.SizeChanged");
                    InvalidateBars();
                };

            // 7) Build 2×2 grid: [viewport|vbar] / [hbar|corner]
            Content = new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    new TableRow(
                        new TableCell(viewport) { ScaleWidth  = true },
                        new TableCell(vbar)
                    ) { ScaleHeight = true },

                    new TableRow(
                        new TableCell(hbar)    { ScaleWidth = true },
                        new TableCell(corner)
                    )
                }
            };

            // 8) Initial layout
            UpdateContent();
        }

        void InvalidateBars()
        {
            vbar?.Invalidate();
            hbar?.Invalidate();
        }

        void UpdateContent()
        {
            Console.Error.WriteLine($"UpdateContent: scrollX={scrollX}, scrollY={scrollY}");
            if (viewport.ControlObject is System.Windows.Forms.Panel p
                && p.Controls.Count > 0)
            {
                p.Controls[0].Location = new System.Drawing.Point(-scrollX, -scrollY);
            }
        }

        // ---- VBAR Paint ----
        void Vbar_Paint(object sender, PaintEventArgs e)
        {
            Console.Error.WriteLine("Vbar_Paint()");
            var g = e.Graphics;
            var r = e.ClipRectangle;
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;

            // fill background
            g.FillRectangle(vbar.BackgroundColor, r);

            // arrows/thumb only if overflow or forced
            if (contentH > viewH || alwaysShowV)
            {
                // up arrow
                var cx = r.Width / 2f;
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(cx,                10),
                    new PointF(10,               thickness - 10),
                    new PointF(r.Width - 10,     thickness - 10),
                });
                // down arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(10,               r.Height - thickness + 10),
                    new PointF(r.Width - 10,     r.Height - thickness + 10),
                    new PointF(cx,               r.Height - 10),
                });

                // draw thumb (full height if no overflow)
                float trackY = thickness;
                float trackH = r.Height - 2 * thickness;
                float contentRange = Math.Max(1, contentH - viewH);
                float thumbH = (contentH <= viewH)
                    ? trackH
                    : Math.Max(thickness, viewH / (float)contentH * trackH);
                float dragRange = trackH - thumbH;
                float thumbY = (contentH <= viewH)
                    ? trackY
                    : trackY + (scrollY / (float)contentRange) * dragRange;

                g.FillRectangle(
                    ColorSettings.ForegroundColor,
                    new RectangleF(0, thumbY, r.Width, thumbH)
                );
            }
        }

        // ---- HBAR Paint ----
        void Hbar_Paint(object sender, PaintEventArgs e)
        {
            Console.Error.WriteLine("Hbar_Paint()");
            var g = e.Graphics;
            var r = e.ClipRectangle;
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;

            // fill background
            g.FillRectangle(hbar.BackgroundColor, r);

            if (contentW > viewW || alwaysShowH)
            {
                // left arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(thickness - 10,    10),
                    new PointF(10,                r.Height/2f),
                    new PointF(thickness - 10,    r.Height - 10),
                });
                // right arrow
                g.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width - thickness + 10, 10),
                    new PointF(r.Width - 10,              r.Height/2f),
                    new PointF(r.Width - thickness + 10,  r.Height - 10),
                });

                // draw thumb
                float trackX = thickness;
                float trackW = r.Width - 2 * thickness;
                float contentRange = Math.Max(1, contentW - viewW);
                float thumbW = (contentW <= viewW)
                    ? trackW
                    : Math.Max(thickness, viewW / (float)contentW * trackW);
                float dragRange = trackW - thumbW;
                float thumbX = (contentW <= viewW)
                    ? trackX
                    : trackX + (scrollX / (float)contentRange) * dragRange;

                g.FillRectangle(
                    ColorSettings.ForegroundColor,
                    new RectangleF(thumbX, 0, thumbW, r.Height)
                );
            }
        }

        // ---- VBAR Mouse ----
        void Vbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Vbar_MouseDown @ {e.Location}");
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;
            if (contentH <= viewH) return;

            var r = vbar.Bounds;
            var y = e.Location.Y;

            // up arrow
            if (y < thickness)
            {
                scrollY = Math.Max(0, scrollY - thickness);
                UpdateContent(); vbar.Invalidate();
                return;
            }
            // down arrow
            if (y > r.Height - thickness)
            {
                scrollY = Math.Min(contentH - viewH, scrollY + thickness);
                UpdateContent(); vbar.Invalidate();
                return;
            }

            // start thumb drag?
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
            float contentRange = contentH - viewH;
            float dragRange = trackH - thumbH;
            float frac = scrollY / (float)contentRange;
            float thumbY = trackY + frac * dragRange;
            var thumbRect = new RectangleF(0, thumbY, r.Width, thumbH);

            if (thumbRect.Contains(e.Location))
            {
                draggingV = true;
                dragOffY = e.Location.Y - thumbY;
                vbar.CaptureMouse();
            }
        }

        void Vbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingV) return;
            Console.Error.WriteLine($"Vbar_MouseMove @ {e.Location}");
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;
            if (contentH <= viewH) return;

            var r = vbar.Bounds;
            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
            float dragRange = trackH - thumbH;
            if (dragRange <= 0) return;

            var delta = e.Location.Y - dragOffY - trackY;
            var frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
            scrollY = (int)(frac * (contentH - viewH));

            UpdateContent(); vbar.Invalidate();
        }

        void Vbar_MouseUp(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine("Vbar_MouseUp()");
            if (draggingV)
            {
                draggingV = false;
                vbar.ReleaseMouseCapture();
            }
        }

        // ---- HBAR Mouse ----
        void Hbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Hbar_MouseDown @ {e.Location}");
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;
            if (contentW <= viewW) return;

            var r = hbar.Bounds;
            var x = e.Location.X;

            // left arrow
            if (x < thickness)
            {
                scrollX = Math.Max(0, scrollX - thickness);
                UpdateContent(); hbar.Invalidate();
                return;
            }
            // right arrow
            if (x > r.Width - thickness)
            {
                scrollX = Math.Min(contentW - viewW, scrollX + thickness);
                UpdateContent(); hbar.Invalidate();
                return;
            }

            // start thumb drag?
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float contentRange = contentW - viewW;
            float dragRange = trackW - thumbW;
            float frac = scrollX / (float)contentRange;
            float thumbX = trackX + frac * dragRange;
            var thumbRect = new RectangleF(thumbX, 0, thumbW, r.Height);

            if (thumbRect.Contains(e.Location))
            {
                draggingH = true;
                dragOffX = e.Location.X - thumbX;
                hbar.CaptureMouse();
            }
        }

        void Hbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingH) return;
            Console.Error.WriteLine($"Hbar_MouseMove @ {e.Location}");
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;
            if (contentW <= viewW) return;

            var r = hbar.Bounds;
            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;
            if (dragRange <= 0) return;

            var delta = e.Location.X - dragOffX - trackX;
            var frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
            scrollX = (int)(frac * (contentW - viewW));

            UpdateContent(); hbar.Invalidate();
        }

        void Hbar_MouseUp(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine("Hbar_MouseUp()");
            if (draggingH)
            {
                draggingH = false;
                hbar.ReleaseMouseCapture();
            }
        }
    }
}
