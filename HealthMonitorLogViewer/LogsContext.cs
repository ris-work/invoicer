using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace HealthMonitor;

public partial class LogsContext : DbContext
{
    public LogsContext()
    {
    }

    public LogsContext(DbContextOptions<LogsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AvgWorkingSet> AvgWorkingSets { get; set; }

    public virtual DbSet<DistinctProcess> DistinctProcesses { get; set; }

    public virtual DbSet<Exception> Exceptions { get; set; }

    public virtual DbSet<MaxWorkingSet> MaxWorkingSets { get; set; }

    public virtual DbSet<Ping> Pings { get; set; }

    public virtual DbSet<ProcessHistory> ProcessHistories { get; set; }

    public virtual DbSet<TimesCollectedByDecaminute> TimesCollectedByDecaminutes { get; set; }

    public virtual DbSet<TimesCollectedByHour> TimesCollectedByHours { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseSqlite("Data Source=logs.sqlite3");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AvgWorkingSet>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("avg_working_set");

            entity.Property(e => e.AvgWorkingSetValue).HasColumnName("avg_working_set_value");
            entity.Property(e => e.ProcessName).HasColumnName("process_name");
        });

        modelBuilder.Entity<DistinctProcess>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("distinct_processes");

            entity.Property(e => e.ProcessName).HasColumnName("process_name");
        });

        modelBuilder.Entity<Exception>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("exceptions");

            entity.Property(e => e.Exception1).HasColumnName("exception");
            entity.Property(e => e.TimeNow).HasColumnName("time_now");
        });

        modelBuilder.Entity<MaxWorkingSet>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("max_working_set");

            entity.Property(e => e.MaxWorkingSetValue).HasColumnName("max_working_set_value");
            entity.Property(e => e.ProcessName).HasColumnName("process_name");
        });

        modelBuilder.Entity<Ping>(entity =>
        {
            entity.HasKey(e => new { e.Dest, e.TimeNow });

            entity.ToTable("pings");

            entity.Property(e => e.Dest).HasColumnName("dest");
            entity.Property(e => e.TimeNow).HasColumnName("time_now");
            entity.Property(e => e.DidItSucceed)
                .HasColumnType("INT")
                .HasColumnName("did_it_succeed");
            entity.Property(e => e.Latency)
                .HasColumnType("INT")
                .HasColumnName("latency");
            entity.Property(e => e.WasItOkNotCorrupt)
                .HasColumnType("INT")
                .HasColumnName("was_it_ok_not_corrupt");
        });

        modelBuilder.Entity<ProcessHistory>(entity =>
        {
            entity.HasKey(e => new { e.TimeNow, e.Pid });

            entity.ToTable("process_history");

            entity.Property(e => e.TimeNow).HasColumnName("time_now");
            entity.Property(e => e.Pid)
                .HasColumnType("INT")
                .HasColumnName("pid");
            entity.Property(e => e.MainWindowTitle).HasColumnName("main_window_title");
            entity.Property(e => e.PagedMemoryUse).HasColumnName("paged_memory_use");
            entity.Property(e => e.PrivateMemoryUse).HasColumnName("private_memory_use");
            entity.Property(e => e.ProcessName).HasColumnName("process_name");
            entity.Property(e => e.Started).HasColumnName("started");
            entity.Property(e => e.SystemTime)
                .HasColumnType("INT")
                .HasColumnName("system_time");
            entity.Property(e => e.ThreadCount)
                .HasColumnType("INT")
                .HasColumnName("thread_count");
            entity.Property(e => e.TotalTime)
                .HasColumnType("INT")
                .HasColumnName("total_time");
            entity.Property(e => e.UserTime)
                .HasColumnType("INT")
                .HasColumnName("user_time");
            entity.Property(e => e.VirtualMemoryUse).HasColumnName("virtual_memory_use");
            entity.Property(e => e.WorkingSet).HasColumnName("working_set");
        });

        modelBuilder.Entity<TimesCollectedByDecaminute>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("times_collected_by_decaminute");

            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.TimeDecaminute).HasColumnName("time_decaminute");
        });

        modelBuilder.Entity<TimesCollectedByHour>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("times_collected_by_hour");

            entity.Property(e => e.Count).HasColumnName("count");
            entity.Property(e => e.TimeHour).HasColumnName("time_hour");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
