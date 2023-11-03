using Sales.Payments.WebApi.Infrastructure;
using System.Diagnostics;

namespace Sales.Payments.WebApi.Middleware
{
    public class BlockTimer : IDisposable
    {
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public BlockTimer()
        {
        }

        public BlockTimer(EventHandler<OnBlockEndEventArgs> callback)
        {
            OnBlockEnd += callback ?? throw new ArgumentNullException(nameof(callback));

            Start();
        }

        private IDisposable Start()
        {
            _stopwatch.Restart();
            return this;
        }

        void IDisposable.Dispose()
        {
            _stopwatch.Stop();
            OnBlockEnd?.Invoke(this, new OnBlockEndEventArgs(Elapsed));
        }

        public EventHandler<OnBlockEndEventArgs> OnBlockEnd { get; }
        public TimeSpan Elapsed => _stopwatch.Elapsed;

        public bool IsRunning => _stopwatch.IsRunning;
    }
}