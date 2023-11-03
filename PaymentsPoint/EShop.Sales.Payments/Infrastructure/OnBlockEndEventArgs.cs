namespace Sales.Payments.WebApi.Infrastructure
{
    public sealed class OnBlockEndEventArgs : EventArgs
    {
        public OnBlockEndEventArgs(TimeSpan elapsed) 
        {
            Elapsed = elapsed;
        }

        public TimeSpan Elapsed { get; }
    }
}
