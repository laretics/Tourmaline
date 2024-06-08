namespace TOURMALINE.Common
{
    public struct CancellationToken
    {
        readonly CancellationTokenSource Source;

        public CancellationToken(CancellationTokenSource source)
        {
            Source = source;
        }

        public bool IsCancellationRequested
        {
            get
            {
                Source.DoPing();
                return Source.IsCancellationRequested;
            }
        }
    }
}
