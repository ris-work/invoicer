// See https://aka.ms/new-console-template for more information
using System.Security.AccessControl;
using System.Security.Principal;
using Microsoft.VisualBasic.FileIO;

Console.Title = "RV Computer Health Monitoring System installer";
System.Console.WriteLine("Minimal installer for RV Health Monitor, (Ctrl+C) to exit");
var root = Path.Combine(SpecialDirectories.ProgramFiles, "rv", "HealthMonitor");
try
{
    Directory.CreateDirectory(Path.Combine(SpecialDirectories.ProgramFiles, "rv"));
}
catch (Exception _) { }
try
{
    Directory.CreateDirectory(Path.Combine(SpecialDirectories.ProgramFiles, "rv", "HealthMonitor"));
}
catch (Exception E)
{
    System.Console.WriteLine(E.ToString());
}
var HC = new HttpClient();
List<(string, string)> FilesToGet = new()
{
    ("HealthMonitor.zip", "https://vz.al/chromebook/invnew/uv/HealthMonitor.zip"),
    ("HealthMonitorLogViewer.exe", "https://vz.al/chromebook/invnew/uv/HealthMonitorLogViewer.exe"),
    (
        "HealthMonitorLogViewerIcons.zip",
        "https://vz.al/chromebook/invnew/uv/HealthMonitorLogViewerIcons.zip"
    ),
    (
        "HealthMonitorLogViewerM.exe",
        "https://vz.al/chromebook/invnew/uv/HealthMonitorLogViewerM.exe"
    ),
    ("WinSW.exe", "https://github.com/winsw/winsw/releases/download/v3.0.0-alpha.10/WinSW-x64.exe"),
};
foreach ((string, string) FileToGet in FilesToGet)
{
    try
    {
        System.Console.WriteLine($"Get {FileToGet.Item1} from {FileToGet.Item2}...");
        var output_configinst = HC.GetStreamAsync(FileToGet.Item2).GetAwaiter().GetResult();
        var configinst_exe = File.Create(Path.Combine(root, FileToGet.Item1));
        output_configinst.CopyTo(configinst_exe);
        configinst_exe.Close();
        output_configinst.Close();
    }
    catch (Exception E)
    {
        System.Console.WriteLine($"Exception: {E.ToString()}");
    }
}
string ServiceXML = $"""
    <service>
    <id>rv-healthmonitor</id>
    <name>RV Health Monitor</name>
    <description>RV Health Monitor: Monitors CPU, RAM usage over time, along with network latency and ICMP echo request/reply success rates.</description>
    <executable>{Path.Combine(root, "HealthMonitor.exe")}</executable>
    <log mode="roll"></log>
    </service>
    """;
File.WriteAllText(Path.Combine(root, "service.xml"), ServiceXML);
string ScriptSetup = $"""
    echo Extracting...
    Expand-Archive "{Path.Combine(root, "HealthMonitor.zip")}" -DestinationPath "{Path.Combine(
        root
    )}" -Force
    Expand-Archive "{Path.Combine(
        root,
        "HealthMonitorLogViewerIcons.zip"
    )}" -DestinationPath "{Path.Combine(root)}" -Force
    Copy "{Path.Combine(root, "new.logs.sqlite3.rvhealthmonitorlogfile")}" "{Path.Combine(
        root,
        "logs.sqlite3.rvhealthmonitorlogfile"
    )}"
    $Target = "{Path.Combine(root, "HealthMonitorLogViewerM.exe")}"
    $Link = "{Path.Combine("C:\\Users\\Public\\Desktop\\Health Monitor Log Viewer.lnk")}"
    $WD = "{Path.Combine(root)}"
    $WSH = New-Object -ComObject WScript.Shell
    $Shortcut = $WSH.CreateShortcut($Link)
    $Shortcut.TargetPath = $Target
    $Shortcut.WorkingDirectory = $WD
    $Shortcut.Save()
    """;
File.WriteAllText(Path.Combine(root, "inst.ps1"), ScriptSetup);
System.Console.WriteLine("Done, starting installation script...");
System
    .Diagnostics.Process.Start(
        "powershell.exe",
        ["-ExecutionPolicy", "Bypass", "-File", $"{Path.Combine(root, "inst.ps1")}"]
    )
    .WaitForExit();
System
    .Diagnostics.Process.Start(
        Path.Combine(root, "winsw.exe"),
        ["stop", $"{Path.Combine(root, "service.xml")}"]
    )
    .WaitForExit();
System
    .Diagnostics.Process.Start(
        Path.Combine(root, "winsw.exe"),
        ["uninstall", $"{Path.Combine(root, "service.xml")}"]
    )
    .WaitForExit();
System
    .Diagnostics.Process.Start(
        Path.Combine(root, "winsw.exe"),
        ["install", $"{Path.Combine(root, "service.xml")}"]
    )
    .WaitForExit();
System
    .Diagnostics.Process.Start(
        Path.Combine(root, "winsw.exe"),
        ["start", $"{Path.Combine(root, "service.xml")}"]
    )
    .WaitForExit();

//System.Diagnostics.Process.Start(Path.Combine(root, "ui.exe"));
System.Console.WriteLine("Press any key to exit...");
System.Console.ReadKey();
System.Console.WriteLine(
    "You can use the popped up notepad window to edit the configuration (or just quit it). Please make sure it's valid TOML."
);
System
    .Diagnostics.Process.Start("notepad.exe", [$"{Path.Combine(root, "HealthMonitor.toml")}"])
    .WaitForExit();
