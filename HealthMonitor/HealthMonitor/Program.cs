// See https://aka.ms/new-console-template for more information
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
            Console.WriteLine("{0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}", item.ProcessName, item.Threads.Count, item.Id, item.VirtualMemorySize64, item.PagedMemorySize64, item.PrivateMemorySize64, item.WorkingSet64, item.MainWindowTitle);
        }
        Thread.Sleep(1000);
    }
}
)).Start();

void StartPing(string dest)
{
    while (true)
    {
        var pingSender = new Ping();
        string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
        byte[] buffer = Encoding.ASCII.GetBytes(data);
        PingReply reply = pingSender.Send(dest, 120, buffer);
        Console.WriteLine("{0} {1} {2}", dest, reply.RoundtripTime, reply.Buffer.SequenceEqual(buffer));
        Thread.Sleep(1000);
    }
}

foreach (var item in new string[] {
    "192.168.1.1",
    "8.8.8.8",
    "1.1.1.1"
}
)
{
    var t = new Thread(() => { StartPing(item); });
    t.Start();
};