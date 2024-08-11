// See https://aka.ms/new-console-template for more information
using HealthMonitor;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;
using System.Text;
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
var TM = Toml.ToModel(ConfigFile);
var TA = ((TomlArray)TM["destinations"]);
int RetentionDays = 7;
if (TM.ContainsKey("RetentionDays"))
{
    RetentionDays = ((int)((long)TM["RetentionDays"]));
}

destinations = TA.Select(x => (string)x).ToList();
//destinations = new string[] {"192.168.1.1", "8.8.8.8", "1.1.1.1"};
foreach (var item in destinations)
{
    var t = new Thread(() => { StartPing(item); });
    t.Start();
    Console.WriteLine("Ping thread started for: {0}", item);
};

(new Thread(() =>
{
    Console.WriteLine($"Retention set to: {RetentionDays} days.");
    while (true)
    {
        var list = System.Diagnostics.Process.GetProcesses();
        try
        {
            using (var ctx = new LogsContext())
            {
                var days_string = RetentionDays.ToString();
                ctx.Database.SqlQuery<int>($"DELETE FROM pings WHERE time_now < datetime('now', '-{days_string} days');").ToList();
                ctx.Database.SqlQuery<int>($"DELETE FROM process_history WHERE time_now < datetime('now', '-{days_string} days');").ToList();
                foreach (var item in list)
                {
                    try
                    {
                        int syst = 0, ut = 0, tt = 0, tc = 0; 
                        string wsmem = "0", vmuse = "0", prmemuse="0";
                        string sttt = "0";
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
                        { Console.WriteLine(ex.ToString()); }
                        ctx.ProcessHistories.Add(new ProcessHistory
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
                            TimeNow = DateTime.Now.ToString("o"),
                            VirtualMemoryUse = vmuse,
                            WorkingSet = wsmem

                        });
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
        catch (System.Exception E) {
            Console.WriteLine(E.ToString());
        }

        Thread.Sleep(5000);
    }
}
)).Start();