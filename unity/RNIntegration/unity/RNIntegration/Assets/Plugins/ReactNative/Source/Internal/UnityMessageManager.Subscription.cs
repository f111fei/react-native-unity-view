using System;
using System.Threading;

namespace ReactNative
{

    public sealed partial class UnityMessageManager
    {
        private sealed class Subscription : IDisposable
        {
            public readonly string id;
            public readonly UnityMessageDelegate handler;
            public Action<Subscription> unsubscription;

            public Subscription(string id, UnityMessageDelegate handler, Action<Subscription> unsubscription)
            {
                this.id = id;
                this.handler = handler;
                this.unsubscription = unsubscription;
            }

            public void Dispose()
            {
                Interlocked.Exchange(ref this.unsubscription, null)?.Invoke(this);
            }
        }
    }
}
