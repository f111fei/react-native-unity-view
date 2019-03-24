using System;
using System.Runtime.CompilerServices;
using System.Threading;
using UnityEngine;

namespace ReactNative
{
    public sealed partial class UnityMessageManager
    {
        private sealed class UnityMessageHandlerImpl : IUnityMessageHandler, IDisposable
        {
            private IDisposable deferral;
            private CancellationTokenSource cts;

            public UnityMessageHandlerImpl(UnityMessage message)
            {
                this.Message = message;
            }

            public UnityMessage Message { get; }

            public bool IsRequest => this.Message.IsRequest;

            public bool IsDeferred { get; private set; }

            public bool ResponseSent { get; private set; }

            public CancellationToken CancellationToken => this.CancellationTokenSource.Token;

            public CancellationTokenSource CancellationTokenSource => (this.cts ?? (this.cts = new CancellationTokenSource()));

            public IDisposable GetDeferral()
            {
                this.IsDeferred = true;
                return this.deferral ?? (this.deferral = new Disposable(() =>
                {
                    this.deferral = null;
                    this.Dispose();
                }));
            }

            public void SendResponse(object data)
            {
                if (this.IsRequest)
                {
                    this.ResponseSent = true;
                    UnityMessageManager.SendResponse(
                        this.Message.id,
                        this.Message.uuid.Value,
                        data);
                }
                else
                {
                    Debug.LogError("This message is not a request type.");
                }
            }

            public void SendCanceled()
            {
                if (this.IsRequest)
                {
                    this.ResponseSent = true;
                    UnityMessageManager.SendCanceled(
                        this.Message.id,
                        this.Message.uuid.Value);
                }
                else
                {
                    Debug.LogError("This message is not a request type.");
                }
            }

            public void SendError(UnityRequestException error)
            {
                if (this.IsRequest)
                {
                    this.ResponseSent = true;
                    UnityMessageManager.SendError(
                        this.Message.id,
                        this.Message.uuid.Value,
                        error);
                }
                else
                {
                    Debug.LogError("This message is not a request type.");
                }
            }

            public void SendError(
                Exception error,
                [CallerMemberName] string memberName = "",
                [CallerFilePath] string sourceFilePath = "",
                [CallerLineNumber] int sourceLineNumber = 0)
            {
                this.SendError(
                    new UnityRequestException(
                        error,
                        memberName,
                        sourceFilePath,
                        sourceLineNumber));
            }

            public void Dispose()
            {
                if (this.IsRequest)
                {
                    instance?.RemoveIncommingRequest(this.Message.uuid.Value);

                    if (!this.ResponseSent)
                    {
                        this.ResponseSent = true;
                        UnityMessageManager.SendResponse(
                            this.Message.id,
                            this.Message.uuid.Value,
                            null);
                    }
                }
            }

            internal void NotifyCancelled()
                => this.CancellationTokenSource.Cancel();
        }
    }
}
