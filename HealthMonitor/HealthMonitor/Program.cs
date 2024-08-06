// See https://aka.ms/new-console-template for more information
using HealthMonitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Text;
using Tomlyn;
using Tomlyn.Model;

(new Thread(() =>
{
    while (true)
    {
        var list = System.Diagnostics.Process.GetProcesses();
        try
        {
            using (var ctx = new LogsContext())
            {
                ctx.ProcessHistories.FromSql($"DELETE FROM pings WHERE time_now < datetime('now', '-7 days');").ToList();
                ctx.ProcessHistories.FromSql($"DELETE FROM process_history WHERE time_now < datetime('now', '-7 days');").ToList();
                foreach (var item in list)
                {
                    try
                    {
                        int syst = 0, ut = 0, tt = 0;
                        string sttt = "0";
                        try
                        {
                            syst = (int)item.PrivilegedProcessorTime.TotalMilliseconds;
                            ut = (int)item.UserProcessorTime.TotalMilliseconds;
                            tt = (int)item.TotalProcessorTime.TotalMilliseconds;
                            sttt = item.StartTime.ToString("o");
                        }
                        catch (Win32Exception) { }
                        catch (System.Exception ex)
                        { Console.WriteLine(ex.ToString()); }
                        ctx.ProcessHistories.Add(new ProcessHistory
                        {
                            MainWindowTitle = item.MainWindowTitle,
                            PagedMemoryUse = item.PagedMemorySize64.ToString(),
                            Pid = item.Id,
                            PrivateMemoryUse = item.PrivateMemorySize64.ToString(),
                            ProcessName = item.ProcessName,
                            Started = sttt,
                            SystemTime = syst,
                            UserTime = ut,
                            TotalTime = tt,
                            ThreadCount = item.Threads.Count,
                            TimeNow = DateTime.Now.ToString("o"),
                            VirtualMemoryUse = item.VirtualMemorySize64.ToString(),
                            WorkingSet = item.WorkingSet64.ToString()

                        });
                    }
                    catch (Win32Exception E)
                    {

                    }
                    catch (System.Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                ctx.SaveChanges();
            }
        }
        catch (System.Exception E) { }
        
        Thread.Sleep(5000);
    }
}
)).Start();

void StartPing(string dest)
{
    while (true)
    {
        try
        {
            using (var ctx = new LogsContext())
            {
                var pingSender = new System.Net.NetworkInformation.Ping();
                string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaabbbbbbbbbb";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                PingReply reply = pingSender.Send(dest, 4000, buffer);
                Console.WriteLine("{0} {1} {2}", dest, reply.RoundtripTime, reply.Buffer.SequenceEqual(buffer));
                ctx.Pings.Add(new HealthMonitor.Ping { 
                    WasItOkNotCorrupt = reply.Buffer.SequenceEqual(buffer) ? 1 : 0, 
                    DidItSucceed = reply.Status == IPStatus.Success ? 1 : 0 , 
                    Dest = dest, Latency = (int)reply.RoundtripTime, 
                    TimeNow = DateTime.UtcNow.ToString("o") 
                });
                ctx.SaveChanges();
                
            }
        }
        catch (Win32Exception E) { }
        catch (System.Exception E)
        {
            E.ToString();
        }
        Thread.Sleep(5000);
    }
}

string ConfigFile = System.IO.File.ReadAllText("HealthMonitor.toml");
List<string> destinations = new List<string>();
var TA = ((TomlArray)Toml.ToModel(ConfigFile)["destinations"]);
destinations = TA.Select(x => (string)x).ToList();
//destinations = new string[] {"192.168.1.1", "8.8.8.8", "1.1.1.1"};
foreach (var item in destinations)
{
    var t = new Thread(() => { StartPing(item); });
    t.Start();
    Console.WriteLine("Ping thread started for: {0}", item);
};