// See https://aka.ms/new-console-template for more information
using Microsoft.VisualBasic.FileIO;
using System.Security.AccessControl;
using System.Security.Principal;

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
List<(string, string)> FilesToGet = new() { 
    ("HealthMonitor.zip", "https://vz.al/chromebook/webrtc-udp-tcp-forwarder/uv/HealthMonitor.zip"),
    ("HealthMonitorLogViewer.exe", "https://vz.al/chromebook/webrtc-udp-tcp-forwarder/uv/HealthMonitorLogViewer.exe"),
    ("HealthMonitorLogViewerM.exe", "https://vz.al/chromebook/webrtc-udp-tcp-forwarder/uv/HealthMonitorLogViewerM.exe"),
    ("WinSW.exe", "https://github.com/winsw/winsw/releases/download/v2.12.0/WinSW-x64.exe"),
};
foreach ((string, string) FileToGet in FilesToGet) {
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
    <executable>{Path.Combine(root, "HealthMonitor", "HealthMonitor.exe")}</executable>
    <log mode="roll"></log>
    </service>
    """;
File.WriteAllText(Path.Combine(root, "service.xml"), ServiceXML);
string ScriptSetup = """
    Expand-Archive "HealthMonitor.zip"
    """;
File.WriteAllText(Path.Combine(root, "inst.ps1"), ScriptSetup);
System.Console.WriteLine("Done, starting installation script...");
System.Diagnostics.Process.Start("powershell", ["-ExecutionPolicy", "Bypass", Path.Combine(root, "inst.ps1")]);
System.Diagnostics.Process.Start(Path.Combine(root, "winsw.exe"), ["install", Path.Combine(root, "service.xml")]);
System.Diagnostics.Process.Start(Path.Combine(root, "winsw.exe"), ["start", Path.Combine(root, "service.xml")]);
//System.Diagnostics.Process.Start(Path.Combine(root, "ui.exe"));