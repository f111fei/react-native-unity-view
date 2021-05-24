// #define DEBUG_MESSAGING

using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
using UnityEngine.Scripting;

namespace ReactNative
{
    public delegate void MessageDelegate(string message);

    public delegate void UnityMessageDelegate(IUnityMessageHandler handler);

    [Preserve]
    public sealed partial class UnityMessageManager : MonoBehaviour
    {
        public const string GameObjectName = "UnityMessageManager";
        internal const string MessagePrefix = "@UnityMessage@";

        private static readonly object UUIDLock = new object();
        private static int UUID = 0;
        private static int GetNextUUID()
        {
            lock (UUIDLock)
            {
                return (++UUID);
            }
        }

        private static bool instanceDestroyed = false;
        private static UnityMessageManager instance;

        private readonly object stateLock = new object();
        private readonly Dictionary<string, Subscription[]> subscriptions = new Dictionary<string, Subscription[]>();
        private readonly Dictionary<int, UniTaskCompletionSource<UnityMessage>> sentRequests = new Dictionary<int, UniTaskCompletionSource<UnityMessage>>();
        private readonly Dictionary<int, UnityMessageHandlerImpl> receivedRequests = new Dictionary<int, UnityMessageHandlerImpl>();

        static UnityMessageManager()
        {
            if (instance == null && !instanceDestroyed)
            {
                GameObject go = GameObject.Find(GameObjectName) ?? new GameObject(GameObjectName);
                UnityEngine.Object.DontDestroyOnLoad(go);
                instance = go.AddComponent<UnityMessageManager>();
            }

#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            RNUnityViewBridge.BridgeBootstrapper.SetIL2CPPBridge(new UnityMessageManager.IL2CPPBridge());
#endif
        }

        /// <summary>
        /// Event raised when unformtted message is received.
        /// </summary>
        public static event MessageDelegate OnMessage;

        /// <summary>
        /// Sends unformatted message to the React Native engine.
        /// </summary>
        /// <param name="message">The unformatted message.</param>
        /// <remarks>
        /// Message is expected to be in UTF8 encoding.
        /// </remarks>
        public static void Send(string message)
            => UnityMessageManager.onUnityMessage(message);

        /// <summary>
        /// Sends message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///  }
        ///  
        /// Raw message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static void Send(string id, object data = null)
            => UnityMessageManager.SendPlainInternal(id, data);

        /// <summary>
        /// Sends message with data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="message">The message data.</param>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="message.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="message" />
        ///  }
        ///  
        /// Raw message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static void Send<TMessageType>(string id, IUnityMessage<TMessageType> message)
            where TMessageType : Enum
            => UnityMessageManager.SendPlainInternal(id, (int)(object)message.Type(), message);

        /// <summary>
        /// Sends message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="type">The message type.</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///  }
        ///  
        /// Raw message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static void Send(string id, int type, object data = null)
            => UnityMessageManager.SendPlainInternal(id, type, data);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="type">The request type (to identify response).</param>
        /// <param name="request">The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="request.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="request" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static async UniTask<UnityMessage> SendAsync<TMessageType>(string id, IUnityRequest<TMessageType> request, CancellationToken cancellationToken = default(CancellationToken))
            where TMessageType : Enum
            => await UnityMessageManager.instance.SendRequestAsync<UnityMessage>(id, (int)(object)request.Type(), request, cancellationToken);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="type">The request type (to identify response).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static async UniTask<UnityMessage> SendAsync(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => await UnityMessageManager.instance.SendRequestAsync<UnityMessage>(id, type, data, cancellationToken);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="request">The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="request.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="request" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static async UniTask<TResponse> SendAsync<TMessageType, TResponse>(string id, IUnityRequest<TMessageType> request, CancellationToken cancellationToken = default(CancellationToken))
            where TMessageType : Enum
            => await UnityMessageManager.instance.SendRequestAsync<TResponse>(id, (int)(object)request.Type(), request, cancellationToken);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="type">The request type (to identify response).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static async UniTask<T> SendAsync<T>(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => await UnityMessageManager.instance.SendRequestAsync<T>(id, type, data, cancellationToken);

        /// <summary>
        /// Subscribes a new message handler to listen for a given message id.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="handler">The message handler.</param>
        /// <returns>Disposable instance that unsubscribes handler when disposed.</returns>
        public static IDisposable Subscribe(string id, UnityMessageDelegate handler)
            => UnityMessageManager.instance?.SubscribeInternal(id, handler);

        private void OnDestroy()
        {
            UnityMessageManager.instanceDestroyed = true;
            UnityMessageManager.instance = null;
        }

        private async UniTask<T> SendRequestAsync<T>(string id, int type, object data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int uuid = GetNextUUID();
            var awaiter = new UniTaskCompletionSource<UnityMessage>();

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                this.AddOutboundRequest(uuid, awaiter);
                SendRequestInternal(id, uuid, type, data);

                UnityMessage unityMessage;
                using (cancellationToken.Register(() => SendCancel(id, uuid)))
                {
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
                this.RemoveOutboundRequest(uuid);
            }
        }

        private IDisposable SubscribeInternal(string id, UnityMessageDelegate handler)
        {
            lock (this.stateLock)
            {
                Subscription subscriptionInfo = null;

                Subscription[] list;
                if (this.subscriptions.TryGetValue(id, out list))
                {
                    subscriptionInfo = list.FirstOrDefault(m => m.handler == handler);
                    if (subscriptionInfo == null)
                    {
                        Array.Resize(ref list, list.Length + 1);
                        this.subscriptions[id] = list;
                    }
                }
                else
                {
                    list = new Subscription[1];
                    this.subscriptions.Add(id, list);
                }

                if (subscriptionInfo == null)
                {
                    subscriptionInfo = new Subscription(
                        id,
                        handler,
                        (self) => this.Unsubscribe(self));
                    list[list.Length - 1] = subscriptionInfo;
                }

                return subscriptionInfo;
            }
        }

        /// <summary>
        /// Unsubscribes listener from a given message id.
        /// </summary>
        /// <param name="subscription"></param>
        private void Unsubscribe(Subscription subscription)
        {
            lock (this.stateLock)
            {
                var id = subscription.id;
                var handler = subscription.handler;

                Subscription[] list;
                if (this.subscriptions.TryGetValue(id, out list))
                {
                    list = list.Where(m => m.handler != handler).ToArray();

                    if (list.Length > 0)
                    {
                        this.subscriptions[id] = list;
                    }
                    else
                    {
                        this.subscriptions.Remove(id);
                    }
                }
            }
        }

        /// <summary>
        /// Handle for receiving incomming plain-text message.
        /// </summary>
        /// <param name="message">The plain-text message.</param>
        [Preserve]
        private void onMessage(string message)
            => UnityMessageManager.OnMessage?.Invoke(message);

        /// <summary>
        /// Handle for receiving incomming <see cref="UnityMessage" /> message.
        /// Message will be dispatched only to listeners of a given <see cref="UnityMessage.id" />.
        /// </summary>
        /// <param name="message">The JSON message.</param>
        [Preserve]
        private void onRNMessage(string message)
        {
            try
            {
                if (!message.StartsWith(MessagePrefix))
                {
                    Debug.unityLogger.LogWarning("messaging", $"Invalid message format.");
                    return;
                }

                message = message.Substring(MessagePrefix.Length);

                Subscription[] subscriptionList;
                UnityMessage unityMessage = JsonConvert.DeserializeObject<UnityMessage>(message);
                if (unityMessage.IsRequestCompletion)
                {
                    // Handle as request response/error/cancellation
                    this.TryResolveOutboundRequest(unityMessage);
                }
                else if (unityMessage.IsCancel)
                {
                    // Handle as request cancellation
                    this.TryCancelRequest(unityMessage);
                }
                else
                {
                    lock (this.stateLock)
                    {
                        var args = new UnityMessageHandlerImpl(unityMessage);

                        try
                        {
                            // Handle as incomming message or request
                            if (this.subscriptions.TryGetValue(unityMessage.id, out subscriptionList) && subscriptionList.Length > 0)
                            {
                                if (unityMessage.IsRequest)
                                {
                                    // Remember request for incomming cancelation handling
                                    this.AddIncommingRequest(unityMessage.uuid.Value, args);
                                }

                                try
                                {
                                    foreach (Subscription s in subscriptionList)
                                    {
                                        s.handler.Invoke(args);
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.unityLogger.LogError("messaging", $"Failed to handle incoming message:\n{e}", this);

                                    if (args.IsRequest && !args.IsDeferred && !args.ResponseSent)
                                    {
                                        args.SendError(e);
                                    }
                                }
                            }
                            else
                            {
                                if (args.IsRequest)
                                {
                                    args.SendError(new ArgumentException("Invalid message ID.", nameof(UnityMessage.id)));
                                }

                                Debug.unityLogger.LogError("messaging", $"Unknown message id: {unityMessage.id}.", this);
                            }
                        }
                        finally
                        {
                            if (!args.IsDeferred)
                            {
                                args.Dispose();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogError("messaging", $"Failed to parse incoming message:\n{e}", this);
            }
        }

        private void AddIncommingRequest(int uuid, UnityMessageHandlerImpl handler)
        {
            lock (this.stateLock)
            {
                this.receivedRequests[uuid] = handler;
            }
        }

        private void RemoveIncommingRequest(int uuid)
        {
            lock (this.stateLock)
            {
                this.receivedRequests.Remove(uuid);
            }
        }

        private bool RemoveIncommingRequest(int uuid, out UnityMessageHandlerImpl handler)
        {
            lock (this.stateLock)
            {
                if (this.receivedRequests.TryGetValue(uuid, out handler))
                {
                    return this.receivedRequests.Remove(uuid);
                }

                return false;
            }
        }

        private void AddOutboundRequest(int uuid, UniTaskCompletionSource<UnityMessage> awaiter)
        {
            lock (this.stateLock)
            {
                this.sentRequests.Add(uuid, awaiter);
            }
        }

        private void RemoveOutboundRequest(int uuid)
        {
            lock (this.stateLock)
            {
                this.sentRequests.Remove(uuid);
            }
        }

        private bool RemoveOutboundRequest(int uuid, out UniTaskCompletionSource<UnityMessage> awaiter)
        {
            lock (this.stateLock)
            {
                if (this.sentRequests.TryGetValue(uuid, out awaiter))
                {
                    return this.sentRequests.Remove(uuid);
                }

                return false;
            }
        }

        /// <summary>
        /// Tries to find and resolve awaiting outbound request handler.
        /// </summary>
        /// <param name="id">The unity response message.</param>
        private void TryResolveOutboundRequest(UnityMessage unityMessage)
        {
            var uuid = unityMessage.uuid.Value;

            lock (this.stateLock)
            {
                // Response (success or failure) to sent request
                if (this.RemoveOutboundRequest(uuid, out UniTaskCompletionSource<UnityMessage> awaiter))
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
                    Debug.unityLogger.LogError("messaging", $"Unknown outbound request to resolve [uuid: {unityMessage.uuid}]", instance);
                }
            }
        }

        private void TryCancelRequest(UnityMessage unityMessage)
        {
            var uuid = unityMessage.uuid.Value;

            lock (this.stateLock)
            {
                // Cancellation of received request
                if (this.RemoveIncommingRequest(uuid, out UnityMessageHandlerImpl handler))
                {
                    handler.NotifyCancelled();
                }
                else
                if (this.RemoveOutboundRequest(uuid, out UniTaskCompletionSource<UnityMessage> awaiter))
                {
                    awaiter.TrySetCanceled();
                }
                else
                {
                    Debug.unityLogger.LogError("messaging", $"Unknown request to cancel. [uuid: {uuid}]", this);
                }
            }
        }

        /// <summary>
        /// Send serialized message to React Native app.
        /// </summary>
        /// <param name="message">The serialized message to send.</param>
#if UNITY_EXPORT && !UNITY_EDITOR
#if UNITY_IOS
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void onUnityMessage (string message);
#elif UNITY_ANDROID
        private static void onUnityMessage(string message)
        {
            try
            {
                using (AndroidJavaClass jc = new AndroidJavaClass("com.reactnative.unity.view.UnityUtils"))
                {
                    jc.CallStatic("onUnityMessage", message);
                }
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogException(e);
            }
        }
#elif UNITY_WSA && ENABLE_WINMD_SUPPORT
        private static void onUnityMessage(string message)
        {
            RNUnityViewBridge.BridgeBootstrapper.GetDotNetBridge()?.onMessage(message);
        }
#else
        private static void onUnityMessage(string message)
        {
            Debug.unityLogger.Log("messaging", $"Plain: {message}");
        }
#endif
#endif

        /// <summary>
        /// Creates simple message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="data">The optional data.</param>
        private static string SerializeMessage(string id, object data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JsonConvert.SerializeObject(data)}"
                    : string.Empty) +
                "}";

            return json;
        }

        /// <summary>
        /// Creates simple message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="type">The message type ID.</param>
        /// <param name="data">The optional data.</param>
        private static string SerializeMessage(string id, int type, object data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{type}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JsonConvert.SerializeObject(data)}"
                    : string.Empty) +
                "}";

            return json;
        }

        /// <summary>
        /// Creates request message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="type">The request type ID.</param>
        /// <param name="data">The optional request data.</param>
        private static string SerializeRequest(string id, int uuid, int type, object data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{type}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JsonConvert.SerializeObject(data)}"
                    : string.Empty) +
                "}";

            return json;
        }

        /// <summary>
        /// Creates and sends plain message type.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="data">The optional request data.</param>
        private static void SendPlainInternal(string id, object data)
        {
            string json = SerializeMessage(id, data);

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates and sends plain message type.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="type">The type of the request.</param>
        /// <param name="data">The optional request data.</param>
        private static void SendPlainInternal(string id, int type, object data)
        {
            string json = SerializeMessage(id, type, data);

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates and sends request message type.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="type">The type of the request.</param>
        /// <param name="data">The optional request data.</param>
        private static void SendRequestInternal(string id, int uuid, int type, object data)
        {
            string json = SerializeRequest(id, uuid, type, data);

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates response message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="data">THe optional response data.</param>
        private static void SendResponse(string id, int uuid, object data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Response}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JsonConvert.SerializeObject(data)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates cancellation request message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        private static void SendCancel(string id, int uuid)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Cancel}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates cancellation notification message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        private static void SendCanceled(string id, int uuid)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Canceled}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates error message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="error">The optional response data.</param>
        private static void SendError(string id, int uuid, Exception error)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JsonConvert.SerializeObject(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Error}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (error != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JsonConvert.SerializeObject(error)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }
    }
}
