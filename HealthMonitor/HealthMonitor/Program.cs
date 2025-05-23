﻿// See https://aka.ms/new-console-template for more information
using System.Buffers;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
using HealthMonitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Tomlyn;
using Tomlyn.Model;

void StartPing(string dest)
{
    while (true)
    {
        try
        {
            using (var ctx = new LogsContext())
            {
                if (Config.AutoVacuum)
                {
                    try
                    {
                        ctx.Database.ExecuteSqlRaw($"PRAGMA auto_vacuum=FULL;");
                    }
                    catch (System.Exception E)
                    {
                        Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
                    }
                }
                var pingSender = new System.Net.NetworkInformation.Ping();
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaabbbbbbbbbb";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                PingReply reply = pingSender.Send(dest, 4000, buffer);
                Console.WriteLine(
                    "{0} {1} {2}",
                    dest,
                    reply.RoundtripTime,
                    reply.Buffer.SequenceEqual(buffer)
                );
                ctx.Pings.Add(
                    new HealthMonitor.Ping
                    {
                        WasItOkNotCorrupt = reply.Buffer.SequenceEqual(buffer) ? 1 : 0,
                        DidItSucceed = reply.Status == IPStatus.Success ? 1 : 0,
                        Dest = dest,
                        Latency = (int)reply.RoundtripTime,
                        TimeNow = DateTime.UtcNow.ToString("o"),
                    }
                );
                ctx.SaveChanges();
            }
        }
        catch (Win32Exception E) { }
        catch (System.Exception E)
        {
            E.ToString();
        }
        Thread.Sleep(Config.SleepTimeMsBetweenPointsPing);
    }
}

string ConfigFile = System.IO.File.ReadAllText("HealthMonitor.toml");
List<string> destinations = new List<string>();
var TM = Toml.ToModel(ConfigFile);
var TA = ((TomlArray)TM["destinations"]);
int RetentionDays = 7;
if (TM.ContainsKey("RetentionDays"))
{
    RetentionDays = ((int)((long)TM["RetentionDays"]));
}
if (TM.ContainsKey("LogFile"))
{
    Config.LogFile = (((string)TM["LogFile"]));
}
if (TM.ContainsKey("SleepTimeMsBetweenPointsPing"))
{
    Config.SleepTimeMsBetweenPointsPing = (((int)((long)TM["SleepTimeMsBetweenPointsPing"])));
}
if (TM.ContainsKey("SleepTimeMsBetweenPointsProc"))
{
    Config.SleepTimeMsBetweenPointsProc = (((int)((long)TM["SleepTimeMsBetweenPointsProc"])));
}
if (Config.SleepTimeMsBetweenPointsPing < 500)
{
    Config.SleepTimeMsBetweenPointsPing = 500;
    Console.WriteLine("Warning: Ping monitoring sleep time between points is too low, set to 500.");
}
if (Config.SleepTimeMsBetweenPointsProc < 5000)
{
    Config.SleepTimeMsBetweenPointsProc = 300000;
    Console.WriteLine(
        "Warning: Process monitoring sleep time betweeen points is too low, set to 5000. It would generate too much of data and recommended value is more than 300000ms for long runs."
    );
}
if (TM.ContainsKey("AutoVacuumOnStartup"))
{
    Config.AutoVacuumOnStartup = ((bool)(TM["AutoVacuumOnStartup"]));
    Console.WriteLine($"Vacuum on startup set to {Config.AutoVacuumOnStartup}");
}
if (TM.ContainsKey("AutoVacuum"))
{
    Config.AutoVacuum = ((bool)(TM["AutoVacuum"]));
    Console.WriteLine($"Vacuum on startup set to {Config.AutoVacuum}");
}
if (TM.ContainsKey("Title"))
{
    Config.Title = ((string)(TM["Title"]));
    Console.WriteLine($"Console.Title set to \"{Config.Title}\".");
}
Console.WriteLine(
    $"LogFile: {Config.LogFile}, RetentionDays: {RetentionDays}, \nSleepTimeMsBetweenPointsProc (time waited for next proc stats collection): {Config.SleepTimeMsBetweenPointsProc}ms, \nSleepTimeMsBetweenPointsPing (likewise for pings): {Config.SleepTimeMsBetweenPointsPing}ms."
);
FileInfo F = new FileInfo(Config.LogFile);
try
{
    Console.WriteLine($"File size (log) : {F.Length}");
}
catch (System.Exception E)
{
    Console.WriteLine($"Unable to get File Info: {E.ToString()}");
}
if (Config.AutoVacuumOnStartup)
{
    try
    {
        using (var ctx = new LogsContext())
        {
            ctx.Database.ExecuteSqlRaw($"VACUUM;");
            ctx.Database.ExecuteSqlRaw($"ANALYZE;");
        }
    }
    catch (System.Exception E)
    {
        Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
    }
}

destinations = TA.Select(x => (string)x).ToList();

//destinations = new string[] {"192.168.1.1", "8.8.8.8", "1.1.1.1"};
foreach (var item in destinations)
{
    var t = new Thread(() =>
    {
        StartPing(item);
    });
    t.Start();
    Console.WriteLine("Ping thread started for: {0}", item);
}
;

(
    new Thread(() =>
    {
        Console.WriteLine($"Retention set to: {RetentionDays} days.");
        while (true)
        {
            var list = System.Diagnostics.Process.GetProcesses();
            try
            {
                using (var ctx = new LogsContext())
                {
                    if (Config.AutoVacuum)
                    {
                        try
                        {
                            ctx.Database.ExecuteSqlRaw($"PRAGMA auto_vacuum=FULL;");
                        }
                        catch (System.Exception E)
                        {
                            Console.WriteLine($"Error while VACUUM/ANALYZE: {E.ToString()}");
                        }
                    }
                    var days_string = RetentionDays.ToString();
                    var PingCleaner = ctx.Database.ExecuteSql(
                        $"DELETE FROM pings WHERE time_now < {DateTime.Now.Subtract(TimeSpan.FromDays(RetentionDays))};"
                    );
                    var ProcessHistoryCleaner = ctx.Database.ExecuteSql(
                        $"DELETE FROM main.process_history WHERE time_now < {DateTime.Now.Subtract(TimeSpan.FromDays(RetentionDays))};"
                    );
                    Console.WriteLine(
                        $"{PingCleaner.ToString()}, {ProcessHistoryCleaner.ToString()}"
                    );
                    //PingCleaner.ToList();
                    //ProcessHistoryCleaner.ToList();
                    string time_now = DateTime.Now.ToString("o");
                    foreach (var item in list)
                    {
                        try
                        {
                            int syst = 0,
                                ut = 0,
                                tt = 0,
                                tc = 0;
                            string wsmem = "0",
                                vmuse = "0",
                                prmemuse = "0";
                            string sttt = "0";
                            string mmpath = "",
                                mmver = "";
                            try
                            {
                                mmpath = item.MainModule.FileName;
                                mmver = item.MainModule.FileVersionInfo.ToString();
                            }
                            catch (System.Exception E) { }
                            try
                            {
                                vmuse = item.VirtualMemorySize64.ToString();
                                syst = (int)item.PrivilegedProcessorTime.TotalMilliseconds;
                                ut = (int)item.UserProcessorTime.TotalMilliseconds;
                                tt = (int)item.TotalProcessorTime.TotalMilliseconds;
                                sttt = item.StartTime.ToString("o");
                                wsmem = item.WorkingSet64.ToString();
                                prmemuse = item.PrivateMemorySize64.ToString();
                                tc = item.Threads.Count;
                            }
                            catch (Win32Exception) { }
                            catch (System.Exception ex)
                            {
                                Console.WriteLine(ex.ToString());
                            }
                            ctx.ProcessHistories.Add(
                                new ProcessHistory
                                {
                                    MainWindowTitle = item.MainWindowTitle,
                                    PagedMemoryUse = item.PagedMemorySize64.ToString(),
                                    Pid = item.Id,
                                    PrivateMemoryUse = prmemuse,
                                    ProcessName = item.ProcessName,
                                    Started = sttt,
                                    SystemTime = syst,
                                    UserTime = ut,
                                    TotalTime = tt,
                                    ThreadCount = tc,
                                    TimeNow = time_now,
                                    VirtualMemoryUse = vmuse,
                                    WorkingSet = wsmem,
                                    MainModulePath = mmpath,
                                    MainModuleVersion = mmver,
                                }
                            );
                        }
                        catch (Win32Exception E)
                        {
                            //Console.WriteLine(E.ToString());
                        }
                        catch (System.Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                    }
                    ctx.SaveChanges();
                }
            }
            catch (System.Exception E)
            {
                Console.WriteLine(E.ToString());
            }

            Thread.Sleep(Config.SleepTimeMsBetweenPointsProc);
        }
    })
).Start();
try
{
    Console.Title = Config.Title;
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.BackgroundColor = ConsoleColor.DarkBlue;
    Console.CursorVisible = true;
}
catch (System.Exception E)
{
    Console.WriteLine($"Unable to set title: {E.ToString()}, {E.StackTrace}");
}

public static class Config
{
    public static string LogFile = "logs.sqlite3.rvhealthmonitorlogfile";
    public static int SleepTimeMsBetweenPointsPing = 30000;
    public static int SleepTimeMsBetweenPointsProc = 300000;
    public static bool AutoVacuumOnStartup = true;
    public static bool AutoVacuum = true;
    public static string Title =
        "Health Monitor (logging service), © Rishikeshan S/L, License: Open Software License, V3 (no later).";
}
