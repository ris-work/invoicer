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
        /// On WinForms only, hides the native scrollbars of this Scrollable and
        /// wraps it in a 2×2 grid of custom bars of 'thicknessPx'. Force-show
        /// each bar with alwaysShowH / alwaysShowV.
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
                Console.Error.WriteLine("   ❌ Not WinForms. No wrap.");
                return scrollable;
            }
            Console.Error.WriteLine("   ✅ WinForms — proceeding");

            // 2) Hide the built-in WinForms scrollbars
            var swc = scrollable.ControlObject as System.Windows.Forms.ScrollableControl;
            if (swc != null)
            {
                Console.Error.WriteLine("   Hiding WinForms-native scrollbars...");
                swc.AutoScroll = false;
                swc.HorizontalScroll.Visible = false;
                swc.VerticalScroll.Visible = false;
            }
            else
                Console.Error.WriteLine("   ⚠️ ControlObject is not a WinForms ScrollableControl");

            // 3) Create our two bars (inner classes below)
            var vbar = new VScrollbar(scrollable, alwaysShowV, thicknessPx);
            var hbar = new HScrollbar(scrollable, alwaysShowH, thicknessPx);

            // 4) filler corner
            var corner = new Panel
            {
                Width = thicknessPx,
                Height = thicknessPx,
                BackgroundColor = ColorSettings.BackgroundColor
            };

            // 5) Hook events so that:
            //   • we manually offset the single child on ScrollChanged
            //   • we Invalidate() both bars on any size/layout/scroll changes
            scrollable.Scroll += (s, e) =>
            {
                Console.Error.WriteLine($"   scrollable.ScrollChanged → pos={scrollable.ScrollPosition}");
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

            // 6) Build the 2×2 TableLayout using *your* exact ScaleWidth/ScaleHeight pattern
            Console.Error.WriteLine("   Building TableLayout…");
            var table = new TableLayout
            {
                Spacing = Size.Empty,
                Rows =
                {
                    // row 0: scrollable + vbar
                    new TableRow(
                        // scrollable expands both ways
                        new TableCell(scrollable) { ScaleWidth = true }
                      , new TableCell(vbar)       {}
                    )
                    {
                        ScaleHeight = true
                    },

                    // row 1: hbar + corner
                    new TableRow(
                        new TableCell(hbar)       { ScaleWidth = true },
                        new TableCell(corner)
                    )
                    // no ScaleHeight => uses the corner & hbar's natural Height (thicknessPx)
                }
            };

            Console.Error.WriteLine("   Done—returning wrapped layout");
            return table;
        }

        #region Inner scrollbar classes

        class VScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;

            public VScrollbar(Scrollable s, bool alwaysShow, int thickness)
            {
                scrollable = s;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Width = thickness;                       // fixed width
                BackgroundColor = ColorSettings.LesserBackgroundColor;
                Paint += OnPaint;
                MouseDown += OnMouseDown;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var r = e.ClipRectangle;
                var pos = scrollable.ScrollPosition;
                var csize = scrollable.ClientSize;
                var ssize = scrollable.ScrollSize;

                Console.Error.WriteLine(
                  $"   VPaint: clip={r}, pos={pos}, client={csize}, scrollSize={ssize}, alwaysShow={alwaysShow}");

                // always fill our slot so you can see the background
                e.Graphics.FillRectangle(BackgroundColor, r);

                // if no overflow AND not forced, skip arrows
                if (ssize.Height <= csize.Height && !alwaysShow)
                    return;

                // up-arrow (top thick px)
                e.Graphics.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width/2f,   10),
                    new PointF(10,           thick - 10),
                    new PointF(r.Width - 10, thick - 10),
                });

                // down-arrow (bottom thick px)
                e.Graphics.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(10,            r.Height - thick + 10),
                    new PointF(r.Width - 10,  r.Height - thick + 10),
                    new PointF(r.Width/2f,    r.Height - 10),
                });
            }

            void OnMouseDown(object sender, MouseEventArgs e)
            {
                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;

                Console.Error.WriteLine(
                  $"   VMouseDown @ {e.Location}, pos={pos}, client={csize}, scrollSize={ssize}");

                if (ssize.Height <= csize.Height) return;

                var step = csize.Height / 2;
                var newY = pos.Y;
                var r = Bounds;

                if (e.Location.Y <= thick)
                    newY = Math.Max(0, pos.Y - step);
                else if (e.Location.Y >= r.Height - thick)
                    newY = Math.Min(ssize.Height - csize.Height, pos.Y + step);

                Console.Error.WriteLine($"     → scrolling to Y={newY}");
                scrollable.ScrollPosition = new Eto.Drawing.Point(pos.X, newY);
            }
        }

        class HScrollbar : Drawable
        {
            readonly Scrollable scrollable;
            readonly bool alwaysShow;
            readonly int thick;

            public HScrollbar(Scrollable s, bool alwaysShow, int thickness)
            {
                scrollable = s;
                this.alwaysShow = alwaysShow;
                thick = thickness;
                Height = thickness;                      // fixed height
                BackgroundColor = ColorSettings.LesserBackgroundColor;
                Paint += OnPaint;
                MouseDown += OnMouseDown;
            }

            void OnPaint(object sender, PaintEventArgs e)
            {
                var r = e.ClipRectangle;
                var pos = scrollable.ScrollPosition;
                var csize = scrollable.ClientSize;
                var ssize = scrollable.ScrollSize;

                Console.Error.WriteLine(
                  $"   HPaint: clip={r}, pos={pos}, client={csize}, scrollSize={ssize}, alwaysShow={alwaysShow}");

                e.Graphics.FillRectangle(BackgroundColor, r);

                if (ssize.Width <= csize.Width && !alwaysShow)
                    return;

                // left-arrow
                e.Graphics.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(thick - 10,   10),
                    new PointF(10,           r.Height/2f),
                    new PointF(thick - 10,   r.Height - 10),
                });

                // right-arrow
                e.Graphics.FillPolygon(ColorSettings.ForegroundColor, new[]
                {
                    new PointF(r.Width - thick + 10, 10),
                    new PointF(r.Width - 10,         r.Height/2f),
                    new PointF(r.Width - thick + 10, r.Height - 10),
                });
            }

            void OnMouseDown(object sender, MouseEventArgs e)
            {
                var pos = scrollable.ScrollPosition;
                var ssize = scrollable.ScrollSize;
                var csize = scrollable.ClientSize;

                Console.Error.WriteLine(
                  $"   HMouseDown @ {e.Location}, pos={pos}, client={csize}, scrollSize={ssize}");

                if (ssize.Width <= csize.Width) return;

                var step = csize.Width / 2;
                var newX = pos.X;
                var r = Bounds;

                if (e.Location.X <= thick)
                    newX = Math.Max(0, pos.X - step);
                else if (e.Location.X >= r.Width - thick)
                    newX = Math.Min(ssize.Width - csize.Width, pos.X + step);

                Console.Error.WriteLine($"     → scrolling to X={newX}");
                scrollable.ScrollPosition = new Eto.Drawing.Point(newX, pos.Y);
            }
        }

        #endregion
    }
}
