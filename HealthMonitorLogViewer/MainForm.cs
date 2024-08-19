using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;

namespace HealthMonitor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            Title = "HealthMonitor Log Analyzer & Viewer";
            MinimumSize = new Size(200, 200);
            var NetworkPingStatsButton = new Button() { Text = "🕸 Network Ping Stats (by decaminute (10 minutes)) 💾", MinimumSize = new Eto.Drawing.Size(-1, 40) };
            NetworkPingStatsButton.Click += (e, a) => { (new NetworkPingStatsForm()).Show(); };
            var NetworkPingStatsButtonHourly = new Button() { Text = "⏱ Network Ping Stats (by hour)", MinimumSize = new Eto.Drawing.Size(-1, 40) };
            NetworkPingStatsButtonHourly.Click += (e, a) => { (new NetworkPingStatsFormHourly()).Show(); };
            var ProcessStatsButton = new Button() { Text = "📃 Process Stats", MinimumSize = new Eto.Drawing.Size(-1, 40) };
            ProcessStatsButton.Click += (e, a) => { 
                MessageBox.Show("Beta version, expect crashes.", "Warning: Beta Version", MessageBoxType.Warning);
                (new ProcessStatsFormHourly("Idle")).Show();
            };
            var AdvancedProcessStatsButton = new Button() { Text = "⚙ Process Stats (Advanced)", MinimumSize = new Eto.Drawing.Size(-1, 40) };
            AdvancedProcessStatsButton.Click += (e, a) => {
                MessageBox.Show("Beta version, expect crashes.", "Warning: Beta Version", MessageBoxType.Warning);
                (new MainModuleProcessStatsFormHourly("C:\\Windows\\explorer.exe")).Show();
            };

            Content = new StackLayout
            {
                Padding = 30,
                Spacing = 10,
                Items =
                {
                    NetworkPingStatsButton,
                    NetworkPingStatsButtonHourly,
                    ProcessStatsButton,
                    AdvancedProcessStatsButton

				},
                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
                
            };

            // create a few commands that can be used for the menu and toolbar
            var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit();

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) => new AboutDialog() { 
                Copyright = "Rishikeshan Sulochana/Lavakumar", 
                ProgramName = "Health Monitor Log Viewer", 
                Website = new Uri("https://rishikeshan.com"), 
                Title = $"Health Monitor Log Viewer (Detected: [{Eto.Platform.Detect.ToString()}, {Platform}])",   
                License = "OSLv3 (no later versions), https://rosenlaw.com/OSL3.0-explained.htm",
                Developers = ["Rishikeshan S/L"]
            }.ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
					// File submenu
					new SubMenuItem { Text = "&File", Items = { clickMe } },
					// new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
					// new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
				},
                ApplicationItems =
                {
					// application (OS X) or file menu (others)
					new ButtonMenuItem { Text = "&Preferences..." },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand
            };

            // create toolbar			
            ToolBar = new ToolBar { Items = { clickMe } };
            Resizable = false;
        }
    }
}
