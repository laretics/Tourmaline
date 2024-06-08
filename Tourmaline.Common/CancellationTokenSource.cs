using System;

namespace TOURMALINE.Common
{
    public class CancellationTokenSource
    {
        public CancellationToken Token { get; private set; }

        readonly Action Ping;
        bool Cancelled;

        public CancellationTokenSource(Action ping)
        {
            Ping = ping;
            Token = new CancellationToken(this);
        }

        public bool IsCancellationRequested
        {
            get
            {
                DoPing();
                return Cancelled;
            }
        }

        public void Cancel()
        {
            Cancelled = true;
        }

        internal void DoPing()
        {
            var ping = Ping;
            if (ping != null)
                ping();
        }
    }
}
