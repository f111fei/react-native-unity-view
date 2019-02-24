using System;
using System.Collections;
using System.Threading;

namespace ReactNative
{
    public sealed class Disposable : IDisposable
    {
        private Action disposeAction;
        private IDisposable[] disposables;

        public Disposable(Action action)
        {
            this.disposeAction = action;
        }

        public Disposable(params IDisposable[] disposables)
        {
            this.disposables = disposables;
        }

        public void Dispose()
        {
            var disposables = Interlocked.Exchange(ref this.disposables, null);
            if (disposables != null)
            {
                foreach (var d in disposables)
                {
                    d.Dispose();
                }
            }

            Interlocked.Exchange(ref this.disposeAction, null)?.Invoke();
        }

        public static IEnumerator ReleaseCoroutine(IDisposable selectionManagerLock)
        {
            yield return null;
            selectionManagerLock?.Dispose();
        }
    }
}
