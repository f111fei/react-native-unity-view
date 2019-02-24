using System;

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
                lock (this)
                {
                    var handler = this.unsubscription;
                    this.unsubscription = null;

                    if (handler != null)
                    {
                        handler.Invoke(this);
                    }
                }
            }
        }
    }
}
