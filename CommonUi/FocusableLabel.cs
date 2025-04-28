using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Eto.Drawing;
using Microsoft.Maui.Platform;

namespace CommonUi
{/*
    public class FocusableLabel : Drawable
    {
        // The text to display.
        public string Text { get; set; } = string.Empty;

        // Custom appearance properties.
        public Font Font { get; set; } = Fonts.Sans(12);
        public Color ForeColor { get; set; } = Colors.Black;
        public Color BackgroundColor { get; set; } = Colors.LightGray;

        // Mimic tab stop behavior (custom; further keyboard handling may be needed).
        public bool TabStop { get; set; } = true;

        // Private field to track focus state.
        private bool _hasFocus;

        public FocusableLabel()
        {
            // Here you might initialize additional properties if desired.
        }

        /// <summary>
        /// Measure the preferred size of the control using Graphics.MeasureString.
        /// </summary>
        public override Size GetPreferredSize(Size availableSize)
        {
            if (string.IsNullOrEmpty(Text))
                return Size.Empty;
            // Create a temporary 1x1 bitmap and graphics context
            using (var bmp = new Bitmap(1, 1))
            using (var g = new Graphics(bmp))
            {
                // Measure the text drawn with the current font
                var sizeF = g.MeasureString(Font, Text, new PointF(0, 0));
                return new Size((int)Math.Ceiling(sizeF.Width), (int)Math.Ceiling(sizeF.Height));
            }
        }

        /// <summary>
        /// Draw the background, text, and focus border (if focused).
        /// </summary>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            // Fill the background.
            e.Graphics.FillRectangle(BackgroundColor, new RectangleF(0, 0, (float)Width, (float)Height));

            // Draw the text at the top-left.
            e.Graphics.DrawText(Font, ForeColor, new PointF(0, 0), Text);

            // If this control has focus, draw a blue border.
            if (_hasFocus)
            {
                using (var pen = new Pen(Colors.Blue, 1))
                {
                    // (Your version may not support dash styling, so we draw a solid border.)
                    e.Graphics.DrawRectangle(pen, new RectangleF(0, 0, (float)Width - 1, (float)Height - 1));
                }
            }
        }

        /// <summary>
        /// Set focus on mouse click.
        /// </summary>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            Focus();
        }

        /// <summary>
        /// When gaining focus, update our state and repaint.
        /// </summary>
        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);
            _hasFocus = true;
            Invalidate();
        }

        /// <summary>
        /// When losing focus, update our state and repaint.
        /// </summary>
        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            _hasFocus = false;
            Invalidate();
        }
    }*/
}
