using System;
using System.Threading;
using System.Threading.Tasks;

static class LazyRefresherHelpers
{
    // Minimal refresher: single-attempt fetch per cycle, background refresh, logs to Console
    private sealed class Refresher<T> where T : class
    {
        readonly Func<T> _fetchOnce;
        readonly TimeSpan _interval;
        readonly CancellationTokenSource _cts = new CancellationTokenSource();
        readonly Lazy<T> _lazy;
        T _current;

        public Refresher(Func<T> fetchOnce, TimeSpan interval)
        {
            _fetchOnce = fetchOnce ?? throw new ArgumentNullException(nameof(fetchOnce));
            _interval = interval;
            _lazy = new Lazy<T>(InitAndStart);
        }

        public T Value => _lazy.Value;

        public void RefreshNow() => TryFetchAndSwap();

        public void Stop() => _cts.Cancel();

        T InitAndStart()
        {
            Console.WriteLine($"[{DateTime.UtcNow:O}] Initializing {typeof(T).Name}");
            TryFetchAndSwap(); // one attempt on first read

            Task.Run(async () =>
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] Background refresher started for {typeof(T).Name}");
                try
                {
                    while (!_cts.Token.IsCancellationRequested)
                    {
                        try { await Task.Delay(_interval, _cts.Token).ConfigureAwait(false); }
                        catch (OperationCanceledException) { break; }

                        TryFetchAndSwap();
                    }
                }
                finally
                {
                    Console.WriteLine($"[{DateTime.UtcNow:O}] Background refresher stopped for {typeof(T).Name}");
                }
            }, _cts.Token);

            return _current;
        }

        void TryFetchAndSwap()
        {
            try
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] Attempt fetch for {typeof(T).Name}");
                var newVal = _fetchOnce();
                if (newVal != null)
                {
                    Interlocked.Exchange(ref _current, newVal);
                    Console.WriteLine($"[{DateTime.UtcNow:O}] Fetch succeeded and swapped {typeof(T).Name}");
                }
                else
                {
                    Console.WriteLine($"[{DateTime.UtcNow:O}] Fetch returned null for {typeof(T).Name}; keeping previous value");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.UtcNow:O}] Fetch exception for {typeof(T).Name}: {ex}");
            }
        }
    }

    // Helper that returns a pair (getter, controller) so you can keep field and have lazy behavior with one registration line.
    public static (Func<T> Get, Action RefreshNow, Action Stop) Create<T>(Func<T> fetchOnce, TimeSpan interval) where T : class
    {
        var r = new Refresher<T>(fetchOnce, interval);
        return (
            Get: () => r.Value,
            RefreshNow: () => r.RefreshNow(),
            Stop: () => r.Stop()
        );
    }
}
