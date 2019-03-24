using fastJSON;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ReactNative
{
    public delegate void MessageDelegate(string message);

    public delegate void UnityMessageDelegate(IUnityMessageHandler handler);

    public sealed partial class UnityMessageManager : MonoBehaviour
    {
        #region Constants

        public const string GameObjectName = "UnityMessageManager";
        internal const string MessagePrefix = "@UnityMessage@";

        #endregion

        #region Fields

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
        private readonly Dictionary<int, TaskCompletionSource<UnityMessage>> sentRequests = new Dictionary<int, TaskCompletionSource<UnityMessage>>();
        private readonly Dictionary<int, UnityMessageHandlerImpl> receivedRequests = new Dictionary<int, UnityMessageHandlerImpl>();

        #endregion

        #region Constructors

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

        #endregion

        #region Events

        /// <summary>
        /// Event raised when unformtted message is received.
        /// </summary>
        public static event MessageDelegate OnMessage;

        #endregion

        #region Public methods

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
        /// <param name="data">The message data.</param>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="data.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///  }
        ///  
        /// Raw message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static void Send(string id, IUnityResponse data)
            => UnityMessageManager.SendPlainInternal(id, data.Type(), data);

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
        /// <param name="data">The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="data.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static Task<UnityMessage> SendAsync(string id, IUnityRequest data, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.SendRequestAsync<UnityMessage>(id, data.Type(), data, cancellationToken);

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
        public static Task<UnityMessage> SendAsync(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.SendRequestAsync<UnityMessage>(id, type, data, cancellationToken);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="data">The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "type": SERIALIZED_TYPE, // <paramref name="data.Type" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public static Task<T> SendAsync<T>(string id, IUnityRequest data, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.SendRequestAsync<T>(id, data.Type(), data, cancellationToken);

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
        public static Task<T> SendAsync<T>(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.SendRequestAsync<T>(id, type, data, cancellationToken);

        /// <summary>
        /// Subscribes a new message handler to listen for a given message id.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="handler">The message handler.</param>
        /// <returns>Disposable instance that unsubscribes handler when disposed.</returns>
        public static IDisposable Subscribe(string id, UnityMessageDelegate handler)
            => UnityMessageManager.instance?.SubscribeInternal(id, handler);

#if !UNITY_EXPORT
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

        public static Task<T> InjectAsync<T>(string id, IUnityRequest data, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.InjectInternalAsync<T>(id, GetNextUUID(), data.Type(), data, cancellationToken);
        public static Task<T> InjectAsync<T>(string id, IUnityRequest<T> data, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.InjectInternalAsync<T>(id, GetNextUUID(), data.Type(), data, cancellationToken);
        public static Task<T> InjectAsync<T>(string id, int type, object data = null, CancellationToken cancellationToken = default(CancellationToken))
            => UnityMessageManager.instance?.InjectInternalAsync<T>(id, GetNextUUID(), type, data, cancellationToken);
        private async Task<T> InjectInternalAsync<T>(string id, int uuid, int type, object data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var awaiter = new TaskCompletionSource<UnityMessage>();
            string json = UnityMessageManager.SerializeRequest(id, uuid, type, data);

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                this.AddOutboundRequest(uuid, awaiter);
                SendRequestInternal(id, uuid, type, data); // Note: This will only print message to Unity Console

                UnityMessage unityMessage;
                using (cancellationToken.Register(() => SendCancel(id, uuid)))
                {
                    this.onRNMessage(MessagePrefix + json);
                    unityMessage = await awaiter.Task.ConfigureAwait(false);
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
#endif
        #endregion

        #region Unity lifecycle

        private void OnDestroy()
        {
            UnityMessageManager.instanceDestroyed = true;
            UnityMessageManager.instance = null;
        }

        #endregion

        #region Private methods

        private async Task<T> SendRequestAsync<T>(string id, int type, object data, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            int uuid = GetNextUUID();
            var awaiter = new TaskCompletionSource<UnityMessage>();

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                this.AddOutboundRequest(uuid, awaiter);
                SendRequestInternal(id, uuid, type, data);

                UnityMessage unityMessage;
                using (cancellationToken.Register(() =>
                                                  {
                                                      if (awaiter.TrySetCanceled())
                                                      {
                                                          SendCancel(id, uuid);
                                                      }
                                                  }))
                {
                    unityMessage = await awaiter.Task.ConfigureAwait(false);
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
                        this.name,
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
        /// Handles message forwarding it to all 
        /// </summary>
        /// <param name="message"></param>
        private void onMessage(string message)
            => UnityMessageManager.OnMessage?.Invoke(message);

        /// <summary>
        /// Handles JSON message as <see cref="UnityMessage" /> instance (with optional callback).
        /// Message will be dispatched only to listeners of a given <see cref="UnityMessage.id" />.
        /// </summary>
        /// <param name="message">The JSON message.</param>
        private void onRNMessage(string message)
        {
            try
            {
                if (!message.StartsWith(MessagePrefix))
                {
                    Debug.LogWarning("Invalid message format.");
                    return;
                }

                message = message.Substring(MessagePrefix.Length);

                Subscription[] subscriptionList;
                UnityMessage unityMessage = JSON.ToObject<UnityMessage>(message);
                if (unityMessage.IsRequestCompletion)
                {
                    // Handle as request response/error/cancellation
                    this.TryResolveRequest(unityMessage);
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
                                    Debug.LogError($"Failed to handle incoming message:\n{e}", this);

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

                                Debug.LogError($"Unknown message id: {unityMessage.id}.", this);
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
                Debug.LogError($"Failed to parse incoming message:\n{e}", this);
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

        private void AddOutboundRequest(int uuid, TaskCompletionSource<UnityMessage> awaiter)
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

        private bool RemoveOutboundRequest(int uuid, out TaskCompletionSource<UnityMessage> awaiter)
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
        /// Tries to find and resolve awaiting request handler.
        /// </summary>
        /// <param name="id">The unity response message.</param>
        private void TryResolveRequest(UnityMessage unityMessage)
        {
            var uuid = unityMessage.uuid.Value;

            lock (this.stateLock)
            {
                // Response (success or failure) to sent request
                if (this.RemoveOutboundRequest(uuid, out TaskCompletionSource<UnityMessage> awaiter))
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
                        Debug.LogError($"Unknown response message type: {unityMessage.type}", instance);
                    }
                }
                else
                {
                    Debug.LogError($"Unknown outbound request uuid: {unityMessage.uuid}", instance);
                }
            }
        }

        /// <summary>
        /// Tries to find and resolve awaiting request handler.
        /// </summary>
        /// <param name="id">The unity response message.</param>
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
                {
                    Debug.LogError($"Unknown incomming request uuid: {uuid}", this);
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
                Debug.LogException(e);
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
            Debug.Log("onUnityMessage: " + message);
        }
#endif
#else
        private static void onUnityMessage(string message)
        {
            if (!message.StartsWith(MessagePrefix))
            {
                Debug.Log("Unformatted: " + message);
                return;
            }

            message = message.Substring(MessagePrefix.Length);

            UnityMessage unityMessage = JSON.ToObject<UnityMessage>(message);
            if (unityMessage.IsRequestCompletion)
            {
                Debug.Log($"onResponse[{unityMessage.uuid}]: {message}");
                instance?.TryResolveRequest(unityMessage);
            }
            else if (unityMessage.IsCancel)
            {
                Debug.Log($"onCancel[{unityMessage.uuid}]: {message}");
                instance?.TryCancelRequest(unityMessage);
            }
            else
            {
                Debug.Log($"onUnityMessage[{unityMessage.uuid}]: {message}");
            }
        }
#endif

        /// <summary>
        /// Creates simple message in JSON format.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="data">The optional data.</param>
        private static string SerializeMessage(string id, object data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JSON.ToJSON(data)}"
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{type}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JSON.ToJSON(data)}"
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{type}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JSON.ToJSON(data)}"
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Response}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (data != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JSON.ToJSON(data)}"
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
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
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.type)}\":{(int)UnityMessageType.Error}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid}" +
                (error != null
                    ? $",\"{nameof(UnityMessage.data)}\":{JSON.ToJSON(error)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        #endregion
    }
}
