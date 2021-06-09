using System;
using System.Threading;

public class Disposable : IDisposable
{
    private Action onDispose;

    public Disposable(Action onDispose)
    {
        this.onDispose = onDispose;
    }

    public void Dispose() => Interlocked.Exchange(ref this.onDispose, null)?.Invoke();
}
