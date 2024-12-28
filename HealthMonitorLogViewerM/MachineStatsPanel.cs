using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Eto.Forms;
using Microsoft.VisualBasic.Devices;

namespace HealthMonitorLogViewer
{
    internal class MachineStatsPanel : Panel
    {
        string MachineInfo = "";
        string aggDriveInfo = "";

        public MachineStatsPanel()
        {
            try
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
                aggDriveInfo = "";
                MachineInfo +=
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
                    + $"OS platform: {C.OSPlatform}";
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
                    aggDriveInfo +=
                        $"Root: {drive.RootDirectory} "
                        + $"Name: {drive.Name}\r\n"
                        + $"Free space: {drive.AvailableFreeSpace} ({(drive.AvailableFreeSpace / (1024.0 * 1024 * 1024)).ToString("F")} GiB) ({(100.0 * drive.AvailableFreeSpace / drive.TotalSize).ToString("F")}%)\r\n"
                        + $"Total size: {drive.TotalSize} ({(drive.TotalSize / (1024.0 * 1024 * 1024)).ToString("F")} GiB)\r\n"
                        + $"Format: {drive.DriveFormat} "
                        + $"Count: {count + 1}\r\n"
                        + $"Label: {drive.VolumeLabel}\r\n✨✨✨✨✨✨✨✨\r\n";
                    ;
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
            }
            catch (Exception E)
            {
                MessageBox.Show(
                    $"Exception: {E.ToString()}, {E.StackTrace}",
                    "An error occured! 🛑",
                    MessageBoxType.Error
                );
            }
            Content = new TextArea()
            {
                Text = $"Please see the message boxes. \r\n{MachineInfo}\r\n{aggDriveInfo}",
                TextColor = Eto.Drawing.Colors.White,
                Font = new Eto.Drawing.Font("Gourier", 8),
                BackgroundColor = Eto.Drawing.Colors.Black,
                Size = new Eto.Drawing.Size(1000, 600),
                ReadOnly = true,
                Border = BorderType.Line,
            };
        }
    }
}
