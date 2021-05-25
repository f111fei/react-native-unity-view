using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace ReactNative
{
#if !UNITY_EXPORT || UNITY_EDITOR
    public sealed partial class UnityMessageManager : MonoBehaviour
    {
        private readonly Dictionary<int, UniTaskCompletionSource<UnityMessage>> injectedRequests = new Dictionary<int, UniTaskCompletionSource<UnityMessage>>();

        public static event EventHandler<RoutedEventArgs<UnityMessage>> OnMessageSent;

        public static void Inject(string data)
            => UnityMessageManager.instance?.InjectInternal(data);
        private void InjectInternal(string data)
            => this.onMessage(data);

        public static void Inject(string id, object data = null)
            => UnityMessageManager.instance?.InjectInternal(id, data);
        private void InjectInternal(string id, object data)
        {
            string json = UnityMessageManager.SerializeMessage(id, data);
            this.onRNMessage(MessagePrefix + json);
        }

        public static UniTask InjectAsync<TMessageType>(string id, IUnityRequest<TMessageType> data, CancellationToken cancellationToken = default(CancellationToken))
            where TMessageType : Enum
            => UnityMessageManager.instance.InjectInternalAsync<object>(id, GetNextUUID(), (int)(object)data.Type(), data, cancellationToken);
        public static UniTask<TResponse> InjectAsync<TRequestType, TResponse>(string id, IUnityRequest<TRequestType, TResponse> data, CancellationToken cancellationToken = default(CancellationToken))
            where TRequestType : Enum
            => UnityMessageManager.instance.InjectInternalAsync<TResponse>(id, GetNextUUID(), (int)(object)data.Type(), data, cancellationToken);

        public static UniTask<T> InjectAsync<T>(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance.InjectInternalAsync<T>(id, GetNextUUID(), type, data, cancellationToken);
        public static UniTask<T> InjectAsync<T>(string id, int uuid, int type, object data, CancellationToken cancellationToken)
            => UnityMessageManager.instance.InjectInternalAsync<T>(id, uuid, type, data, cancellationToken);

        private async UniTask<T> InjectInternalAsync<T>(string id, int uuid, int type, object data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var awaiter = new UniTaskCompletionSource<UnityMessage>();
            string json = UnityMessageManager.SerializeRequest(id, uuid, type, data);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                if (type >= (int)UnityMessageType.Request)
                {
                    this.MOCK_AddInjectedRequest(uuid, awaiter);
                }

                UnityMessage unityMessage;
                using (cancellationToken.Register(() => SendCancel(id, uuid)))
                {
                    this.onRNMessage(MessagePrefix + json);
                    unityMessage = await awaiter.Task;
                }

                if (typeof(T) == typeof(UnityMessage))
                {
                    return (T)Convert.ChangeType(unityMessage, typeof(UnityMessage));
                }
                else
                {
                    return unityMessage.GetData<T>();
                }
            }
            finally
            {
                this.MOCK_RemoveInjectedRequest(uuid);
            }
        }

        private void MOCK_AddInjectedRequest(int uuid, UniTaskCompletionSource<UnityMessage> awaiter)
        {
            lock (this.stateLock)
            {
                this.injectedRequests.Add(uuid, awaiter);
            }
        }

        private void MOCK_RemoveInjectedRequest(int uuid)
        {
            lock (this.stateLock)
            {
                this.injectedRequests.Remove(uuid);
            }
        }

        private bool MOCK_RemoveInjectedRequest(int uuid, out UniTaskCompletionSource<UnityMessage> awaiter)
        {
            lock (this.stateLock)
            {
                if (this.injectedRequests.TryGetValue(uuid, out awaiter))
                {
                    return this.injectedRequests.Remove(uuid);
                }

                return false;
            }
        }

        private void MOCK_TryResolveInjectedRequest(UnityMessage unityMessage)
        {
            var uuid = unityMessage.uuid.Value;

            lock (this.stateLock)
            {
                // Response (success or failure) to sent request
                if (this.MOCK_RemoveInjectedRequest(uuid, out UniTaskCompletionSource<UnityMessage> awaiter))
                {
                    if (unityMessage.IsResponse)
                    {
                        awaiter.TrySetResult(unityMessage);
                    }
                    else if (unityMessage.IsCanceled)
                    {
                        awaiter.TrySetCanceled();
                    }
                    else if (unityMessage.IsError)
                    {
                        awaiter.TrySetException(new UnityRequestException(unityMessage));
                    }
                    else
                    {
                        Debug.unityLogger.LogError("messaging", $"Unknown response message type: {unityMessage.type}", instance);
                    }
                }
                else
                {
                    Debug.unityLogger.LogError("messaging", $"Unknown injected request to resolve [uuid: {unityMessage.uuid}]", instance);
                }
            }
        }

        /// <summary>
        /// Send serialized message to React Native app (MOCK)
        /// </summary>
        /// <param name="message">The serialized message to send.</param>
        private static void onUnityMessage(string message)
        {
            if (!message.StartsWith(MessagePrefix))
            {
                Debug.unityLogger.Log("messaging", $"Unknown type: {message}");
                return;
            }

            message = message.Substring(MessagePrefix.Length);

            UnityMessage unityMessage = JsonConvert.DeserializeObject<UnityMessage>(message);
            if (unityMessage.IsRequestCompletion)
            {
                Debug.unityLogger.Log("messaging", $"response[{unityMessage.uuid}] {message}");
                if (instance.sentRequests.ContainsKey(unityMessage.uuid.Value))
                {
                    instance.TryResolveOutboundRequest(unityMessage);
                }
                else
                {
                    instance.MOCK_TryResolveInjectedRequest(unityMessage);
                }
            }
            else if (unityMessage.IsCancel)
            {
                Debug.unityLogger.Log("messaging", $"cancel[{unityMessage.uuid}] {message}");
                instance.TryCancelRequest(unityMessage);
            }
            else
            {
                Debug.unityLogger.Log("messaging", $"request[{unityMessage.uuid}] {message}");
                UnityMessageManager.OnMessageSent?.Invoke(instance, unityMessage);
            }
        }
    }
#endif
}
