using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;

namespace HealthMonitorLogViewer
{
    internal class AboutPanel: Panel
    {
        public AboutPanel() {
            new AboutDialog()
            {
                Copyright = "Rishikeshan Sulochana/Lavakumar",
                ProgramName = "Health Monitor Log Viewer",
                Website = new Uri("https://rishikeshan.com"),
                Title = $"Health Monitor Log Viewer (Detected: [{Eto.Platform.Detect.ToString()}, {Platform}])",
                License = "OSLv3 (no later versions), https://rosenlaw.com/OSL3.0-explained.htm",
                Developers = ["Rishikeshan S/L"]
            }.ShowDialog(this);
        }
    }
}
