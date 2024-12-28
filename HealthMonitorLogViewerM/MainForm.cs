using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using Eto.Drawing;
using Eto.Forms;
using Microsoft.VisualBasic.Devices;

namespace HealthMonitor
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            Title = $"HealthMonitor Log Analyzer & Viewer [{Config.LogFile}]";
            MovableByWindowBackground = true;
            MinimumSize = new Size(200, 200);
            var NetworkPingStatsButton = new Button()
            {
                Text = "🕸 Network Ping Stats (by decaminute (10 minutes)) 💾",
                MinimumSize = new Eto.Drawing.Size(-1, 40),
            };
            NetworkPingStatsButton.Click += (e, a) =>
            {
                (new NetworkPingStatsForm()).Show();
            };
            var NetworkPingStatsButtonHourly = new Button()
            {
                Text = "⏱ Network Ping Stats (by hour)",
                MinimumSize = new Eto.Drawing.Size(-1, 40),
            };
            //NetworkPingStatsButtonHourly.Click += (e, a) => { (new NetworkPingStatsFormHourly()).Show(); };
            var ProcessStatsButton = new Button()
            {
                Text = "📃 Process Stats",
                MinimumSize = new Eto.Drawing.Size(-1, 40),
            };
            ProcessStatsButton.Click += (e, a) =>
            {
                MessageBox.Show(
                    "Beta version, expect crashes.",
                    "Warning: Beta Version",
                    MessageBoxType.Warning
                );
                (new ProcessStatsFormHourly("Idle")).Show();
            };
            var AdvancedProcessStatsButton = new Button()
            {
                Text = "⚙ Process Stats (Advanced)",
                MinimumSize = new Eto.Drawing.Size(-1, 40),
            };
            AdvancedProcessStatsButton.Click += (e, a) =>
            {
                MessageBox.Show(
                    "Beta version, expect crashes.",
                    "Warning: Beta Version",
                    MessageBoxType.Warning
                );
                (new MainModuleProcessStatsFormHourly("C:\\Windows\\explorer.exe")).Show();
            };
            var FileAndSystemInformationButton = new Button()
            {
                Text = "ℹ File information",
                MinimumSize = new Eto.Drawing.Size(-1, 40),
            };
            FileAndSystemInformationButton.Click += (e, a) =>
            {
                FileInfo F = new FileInfo(Config.LogFile);
                ComputerInfo C = new ComputerInfo();
                MessageBox.Show(
                    $"Filename: {Config.LogFile}\r\n"
                        + $"File size: {F.Length}\r\n"
                        + $"Full path: {F.FullName}\r\n"
                        + $"Last Written: {F.LastWriteTime} ({(DateTime.UtcNow - F.LastWriteTimeUtc).TotalHours.ToString("F")} hours ago)\r\n"
                        + $"↔↔↔↔SYSTEM INFO BELOW↔↔↔↔\r\n"
                        + $"RAM: {C.TotalPhysicalMemory} ({(C.TotalPhysicalMemory / 1024.0 / 1024 / 1024).ToString("F")} GiB)\r\n"
                        + $"OS name: {C.OSFullName}\r\n"
                        + $"Culture: {C.InstalledUICulture}\r\n"
                        + $"OS full name: {C.OSFullName}\r\n"
                        + $"OS version: {C.OSVersion}\r\n"
                        + $"OS platform: {C.OSPlatform}",
                    "File and System Info",
                    MessageBoxType.Information
                );
                var drives = DriveInfo.GetDrives();
                string driveInfo = "";
                int count = 0;
                foreach (var drive in drives)
                {
                    driveInfo +=
                        $"Root: {drive.RootDirectory} "
                        + $"Name: {drive.Name}\r\n"
                        + $"Free space: {drive.AvailableFreeSpace} ({(drive.AvailableFreeSpace / (1024.0 * 1024 * 1024)).ToString("F")} GiB) ({(100.0 * drive.AvailableFreeSpace / drive.TotalSize).ToString("F")}%)\r\n"
                        + $"Total size: {drive.TotalSize} ({(drive.TotalSize / (1024.0 * 1024 * 1024)).ToString("F")} GiB)\r\n"
                        + $"Format: {drive.DriveFormat} "
                        + $"Count: {count + 1}\r\n"
                        + $"Label: {drive.VolumeLabel}\r\n✨✨✨✨✨✨✨✨\r\n";
                    count++;
                    if (count > 0 && count % 4 == 0)
                    {
                        MessageBox.Show(driveInfo, "Drive information", MessageBoxType.Information);
                        driveInfo = "";
                    }
                }
                if (driveInfo != "")
                {
                    MessageBox.Show(driveInfo, "Drive information", MessageBoxType.Information);
                }
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
                    AdvancedProcessStatsButton,
                    FileAndSystemInformationButton,
                },
                Orientation = Orientation.Vertical,
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
            };

            // create a few commands that can be used for the menu and toolbar
            //var clickMe = new Command { MenuText = "Click Me!", ToolBarText = "Click Me!" };
            //clickMe.Executed += (sender, e) => MessageBox.Show(this, "I was clicked!");

            var quitCommand = new Command
            {
                MenuText = "Quit",
                Shortcut = Application.Instance.CommonModifier | Keys.Q,
            };
            var infoCommand = new Command
            {
                MenuText = "Info",
                Shortcut = Application.Instance.CommonModifier | Keys.I,
            };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit();
            infoCommand.Executed += (sender, e) =>
                MessageBox.Show(
                    $"File: {Config.LogFile}",
                    "Log File Information",
                    MessageBoxType.Information
                );

            var aboutCommand = new Command { MenuText = "About..." };
            aboutCommand.Executed += (sender, e) =>
                new AboutDialog()
                {
                    Copyright = "Rishikeshan Sulochana/Lavakumar",
                    ProgramName = "Health Monitor Log Viewer",
                    Website = new Uri("https://rishikeshan.com"),
                    Title =
                        $"Health Monitor Log Viewer (Detected: [{Eto.Platform.Detect.ToString()}, {Platform}])",
                    License =
                        "OSLv3 (no later versions), https://rosenlaw.com/OSL3.0-explained.htm",
                    Developers = ["Rishikeshan S/L"],
                }.ShowDialog(this);

            // create menu
            Menu = new MenuBar
            {
                Items =
                {
                    // File submenu
                    new SubMenuItem { Text = "&File", Items = { infoCommand } },
                    // new SubMenuItem { Text = "&Edit", Items = { /* commands/items */ } },
                    // new SubMenuItem { Text = "&View", Items = { /* commands/items */ } },
                },
                ApplicationItems =
                {
                    // application (OS X) or file menu (others)
                    //new ButtonMenuItem { Text = "&Preferences..." },
                },
                QuitItem = quitCommand,
                AboutItem = aboutCommand,
            };

            // create toolbar
            //ToolBar = new ToolBar { Items = { clickMe } };
            Resizable = false;
        }
    }
}
