## What's this?
This is HealthMonitor, an open-source system health monitoring tool. It monitors (configurable) the system's network status and processes' resource usage (CPU/RAM) and stores them as an SQLite file. The tables are deliberately unindexed because this is write-heavy. For the GUI visualizer, see [Health Monitor Log Viewer](../HealthMonitorLogViewer). This tool has been very useful in detecting the causes of the network faults, including high latency spikes which led to 1000+ms latency in a LAN which should have been max. 1ms. This monitors the destination reachability by using ICMP echo request(s)/reply(-ies).

## How do I use it?
You have to first copy the `new.logs.sqlite3.rvhealthmonitorlogfile` to your desired destination and tune it accordingly in `HealthMonitor.toml`. They are SQLite3 database files with prebuilt tables and views (unindexed). Destinations are also configurable along with the console title. It should ideally be as the `Administrator` in Microsoft Windows (R) as it also tries to access some private information about the processes which are being monitored. It will still work without administrator privileges but some things might not be monitored (like CPU time).

You should probaly write a batch file or a powershell script and run it as a service using a service manager at startup using a tool like [SvcBatch](https://github.com/mturk/svcbatch).

## Why are the views necessary?
The views actually do discrete differentiation with CPU time. They use window queries for that purpose. I wrote them to reduce the round trip time, in case the DB has to be stored to or retrieved from, remotely.

# License
[OSL-v3](https://rosenlaw.com/pdf-files/OSL3.0-comparison.pdf)