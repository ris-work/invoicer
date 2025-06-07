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
        /// WinForms only: hides native scrollbars and replaces them with custom
        /// 50px scrollbars (arrows + draggable thumb).  No-op elsewhere.
        /// </summary>
        public static Control WrapInCustomScrollbars(this Scrollable scrollable,
            bool alwaysShowH = false, bool alwaysShowV = false, int thicknessPx = 50)
        {
            Console.Error.WriteLine("▶ WrapInCustomScrollbars()");
            var inst = Platform.Instance.ToString();
            var wfId = Platform.Get(Platforms.WinForms)?.ToString();
            Console.Error.WriteLine($"   Platform={inst}, WinForms={wfId}");
            if (inst != wfId)
                return scrollable;

            // hide built-in WinForms scrollbars
            if (scrollable.ControlObject is System.Windows.Forms.ScrollableControl swc)
            {
                swc.AutoScroll = false;
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }

            // extract content
            var content = scrollable.Content;
            scrollable.Content = null;
            Console.Error.WriteLine($"   Extracted content={content}");

            // return our custom viewer
            return new CustomScrollViewer(content, alwaysShowH, alwaysShowV, thicknessPx);
        }
    }

    internal class CustomScrollViewer : Panel
    {
        readonly Panel viewport;
        readonly Control content;
        readonly Drawable vbar, hbar;
        readonly Panel corner;
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

            // clipped viewport
            viewport = new Panel
            {
                //Border = BorderType.None,
                BackgroundColor = ColorSettings.BackgroundColor,
                Content = content
            };
            if (viewport.ControlObject is System.Windows.Forms.Panel vp)
            {
                vp.AutoScroll = false;
                vp.HorizontalScroll.Visible = false;
                vp.VerticalScroll.Visible = false;
            }

            // custom bars
            vbar = new Drawable { Width = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };
            hbar = new Drawable { Height = thickness, BackgroundColor = ColorSettings.LesserBackgroundColor };

            // filler
            corner = new Panel
            {
                Width = thickness,
                Height = thickness,
                BackgroundColor = ColorSettings.BackgroundColor
            };

            // paint hooks
            vbar.Paint += Vbar_Paint;
            hbar.Paint += Hbar_Paint;

            // mouse hooks
            vbar.MouseDown += Vbar_MouseDown;
            vbar.MouseMove += Vbar_MouseMove;
            vbar.MouseUp += Vbar_MouseUp;

            hbar.MouseDown += Hbar_MouseDown;
            hbar.MouseMove += Hbar_MouseMove;
            hbar.MouseUp += Hbar_MouseUp;

            // re-paint on resize
            viewport.SizeChanged += (s, e) => { Console.Error.WriteLine("viewport.SizeChanged"); InvalidateBars(); };
            if (content != null)
                content.SizeChanged += (s, e) => { Console.Error.WriteLine("content.SizeChanged"); InvalidateBars(); };

            // layout: [viewport|vbar] / [hbar|corner]
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

            UpdateContent();
        }

        void InvalidateBars()
        {
            Console.Error.WriteLine("InvalidateBars()");
            vbar?.Invalidate();
            hbar?.Invalidate();
        }

        void UpdateContent()
        {
            Console.Error.WriteLine($"UpdateContent() scrollX={scrollX}, scrollY={scrollY}");
            if (viewport.ControlObject is System.Windows.Forms.Panel p && p.Controls.Count > 0)
                p.Controls[0].Location = new System.Drawing.Point(-scrollX, -scrollY);
        }

        //----- Vbar Paint -----
        void Vbar_Paint(object sender, PaintEventArgs e)
        {
            Console.Error.WriteLine("Vbar_Paint()");
            var g = e.Graphics;
            var r = e.ClipRectangle;
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;
            bool canScroll = contentH > viewH;

            g.FillRectangle(vbar.BackgroundColor, r);
            if (!canScroll && !alwaysShowV) return;

            // arrows
            var cx = r.Width / 2f;
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(cx,                10),
                new PointF(10,               thickness - 10),
                new PointF(r.Width - 10,     thickness - 10)
            });
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(10,               r.Height - thickness + 10),
                new PointF(r.Width - 10,     r.Height - thickness + 10),
                new PointF(cx,               r.Height - 10)
            });

            if (canScroll)
            {
                float trackY = thickness;
                float trackH = r.Height - 2 * thickness;
                float contentRange = contentH - viewH;
                float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
                float dragRange = trackH - thumbH;
                float thumbY = trackY + (scrollY / (float)contentRange) * dragRange;
                g.FillRectangle(
                    ColorSettings.ForegroundColor,
                    new RectangleF(0, thumbY, r.Width, thumbH)
                );
            }
        }

        //----- Hbar Paint -----
        void Hbar_Paint(object sender, PaintEventArgs e)
        {
            Console.Error.WriteLine("Hbar_Paint()");
            var g = e.Graphics;
            var r = e.ClipRectangle;
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;
            bool canScroll = contentW > viewW;

            g.FillRectangle(hbar.BackgroundColor, r);
            if (!canScroll && !alwaysShowH) return;

            // arrows
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(thickness - 10,    10),
                new PointF(10,               r.Height/2f),
                new PointF(thickness - 10,   r.Height - 10)
            });
            g.FillPolygon(ColorSettings.ForegroundColor, new[]
            {
                new PointF(r.Width - thickness + 10, 10),
                new PointF(r.Width - 10,              r.Height/2f),
                new PointF(r.Width - thickness + 10,  r.Height - 10)
            });

            if (canScroll)
            {
                float trackX = thickness;
                float trackW = r.Width - 2 * thickness;
                float contentRange = contentW - viewW;
                float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
                float dragRange = trackW - thumbW;
                float thumbX = trackX + (scrollX / (float)contentRange) * dragRange;
                g.FillRectangle(
                    ColorSettings.ForegroundColor,
                    new RectangleF(thumbX, 0, thumbW, r.Height)
                );
            }
        }

        //----- Vbar Mouse -----
        void Vbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Vbar_MouseDown @ {e.Location}");
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;
            var r = vbar.Bounds;
            var y = e.Location.Y;
            bool canScroll = contentH > viewH;

            // up
            if (y < thickness)
            {
                Console.Error.WriteLine(" Vbar: arrow up");
                if (canScroll)
                    scrollY = Math.Max(0, scrollY - thickness);
                UpdateContent(); vbar.Invalidate();
                return;
            }
            // down
            if (y > r.Height - thickness)
            {
                Console.Error.WriteLine(" Vbar: arrow down");
                if (canScroll)
                    scrollY = Math.Min(contentH - viewH, scrollY + thickness);
                UpdateContent(); vbar.Invalidate();
                return;
            }
            // drag start?
            if (canScroll)
            {
                float trackY = thickness;
                float trackH = r.Height - 2 * thickness;
                float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
                float contentRange = contentH - viewH;
                float dragRange = trackH - thumbH;
                float thumbY = trackY + (scrollY / (float)contentRange) * dragRange;
                var thumbRect = new RectangleF(0, thumbY, r.Width, thumbH);
                if (thumbRect.Contains(e.Location))
                {
                    Console.Error.WriteLine(" Vbar: start drag");
                    draggingV = true;
                    dragOffY = e.Location.Y - thumbY;
                    vbar.CaptureMouse();
                }
            }
        }

        void Vbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingV) return;
            Console.Error.WriteLine($"Vbar_MouseMove @ {e.Location}");
            var viewH = viewport.ClientSize.Height;
            var contentH = content?.Bounds.Height ?? 0;
            var r = vbar.Bounds;

            float trackY = thickness;
            float trackH = r.Height - 2 * thickness;
            float thumbH = Math.Max(thickness, viewH / (float)contentH * trackH);
            float dragRange = trackH - thumbH;
            if (dragRange <= 0) return;

            float delta = e.Location.Y - dragOffY - trackY;
            float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
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

        //----- Hbar Mouse -----
        void Hbar_MouseDown(object sender, MouseEventArgs e)
        {
            Console.Error.WriteLine($"Hbar_MouseDown @ {e.Location}");
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;
            var r = hbar.Bounds;
            var x = e.Location.X;
            bool canScroll = contentW > viewW;

            // left
            if (x < thickness)
            {
                Console.Error.WriteLine(" Hbar: arrow left");
                if (canScroll)
                    scrollX = Math.Max(0, scrollX - thickness);
                UpdateContent(); hbar.Invalidate();
                return;
            }
            // right
            if (x > r.Width - thickness)
            {
                Console.Error.WriteLine(" Hbar: arrow right");
                if (canScroll)
                    scrollX = Math.Min(contentW - viewW, scrollX + thickness);
                UpdateContent(); hbar.Invalidate();
                return;
            }
            // drag start?
            if (canScroll)
            {
                float trackX = thickness;
                float trackW = r.Width - 2 * thickness;
                float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
                float contentRange = contentW - viewW;
                float dragRange = trackW - thumbW;
                float thumbX = trackX + (scrollX / (float)contentRange) * dragRange;
                var thumbRect = new RectangleF(thumbX, 0, thumbW, r.Height);
                if (thumbRect.Contains(e.Location))
                {
                    Console.Error.WriteLine(" Hbar: start drag");
                    draggingH = true;
                    dragOffX = e.Location.X - thumbX;
                    hbar.CaptureMouse();
                }
            }
        }

        void Hbar_MouseMove(object sender, MouseEventArgs e)
        {
            if (!draggingH) return;
            Console.Error.WriteLine($"Hbar_MouseMove @ {e.Location}");
            var viewW = viewport.ClientSize.Width;
            var contentW = content?.Bounds.Width ?? 0;
            var r = hbar.Bounds;

            float trackX = thickness;
            float trackW = r.Width - 2 * thickness;
            float thumbW = Math.Max(thickness, viewW / (float)contentW * trackW);
            float dragRange = trackW - thumbW;
            if (dragRange <= 0) return;

            float delta = e.Location.X - dragOffX - trackX;
            float frac = Math.Max(0f, Math.Min(1f, delta / dragRange));
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
