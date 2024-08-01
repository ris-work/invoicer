// See https://aka.ms/new-console-template for more information
using HealthMonitor;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Text;

Console.WriteLine("Hello, World!");

(new Thread(() =>
{
    while (true)
    {
        var list = System.Diagnostics.Process.GetProcesses();
            foreach (var item in list)
            {
            try
            {
                //Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", item.ProcessName, item.Threads.Count, item.Id, item.VirtualMemorySize64, item.PagedMemorySize64, item.PrivateMemorySize64, item.WorkingSet64, item.MainWindowTitle);
                using (var ctx = new LogsContext())
                {
                    ctx.ProcessHistories.Add(new ProcessHistory
                    {
                        MainWindowTitle = item.MainWindowTitle,
                        PagedMemoryUse = item.PagedMemorySize64.ToString(),
                        Pid = item.Id,
                        PrivateMemoryUse = item.PrivateMemorySize64.ToString(),
                        ProcessName = item.ProcessName,
                        Started = item.StartTime.ToString("o"),
                        SystemTime = (int)item.PrivilegedProcessorTime.TotalMilliseconds,
                        UserTime = (int)item.UserProcessorTime.TotalMilliseconds,
                        TotalTime = (int)item.TotalProcessorTime.TotalMilliseconds,
                        ThreadCount = item.Threads.Count,
                        TimeNow = DateTime.Now.ToString("o"),
                        VirtualMemoryUse = item.VirtualMemorySize64.ToString(),
                        WorkingSet = item.WorkingSet64.ToString()

                    });
                    ctx.SaveChanges();
                }
            }
            catch(System.Exception ex) {
                Console.WriteLine(ex.ToString());
            }
        }
        Thread.Sleep(1000);
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
                PingReply reply = pingSender.Send(dest, 120, buffer);
                Console.WriteLine("{0} {1} {2}", dest, reply.RoundtripTime, reply.Buffer.SequenceEqual(buffer));
                ctx.Pings.Add(new HealthMonitor.Ping { Corrupt = reply.Buffer.SequenceEqual(buffer) ? 0 : 1, Dest = dest, Latency = (int)reply.RoundtripTime, TimeNow=DateTime.UtcNow.ToString("o") });
                ctx.SaveChanges();
                Thread.Sleep(1000);
            }
        }
        catch (System.Exception E)
        {
            E.ToString();
        }
    }
}

foreach (var item in new string[] {
    "192.168.1.1",
    "8.8.8.8",
    "1.1.1.1"
})
{
    var t = new Thread(() => { StartPing(item); });
    t.Start();
};