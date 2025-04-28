using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Cairo;
using Eto.Forms;

namespace HealthMonitorLogViewer
{
    internal class SinglePanelFlyout : Form
    {
        public SinglePanelFlyout(Eto.Forms.Panel Panel, string FormTitle)
        {
            Content = Panel;
            Title = FormTitle;
            BackgroundColor = Eto.Drawing.Color.FromArgb(9, 25, 18, 255);
            Resizable = false;
        }
    }
}
