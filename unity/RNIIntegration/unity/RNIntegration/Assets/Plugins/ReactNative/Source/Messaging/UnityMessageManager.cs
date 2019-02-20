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

    public delegate void UnityMessageDelegate(UnityMessageHandler handler);

    public class UnityMessageManager : MonoBehaviour
    {
        #region Constants

        public const string GameObjectName = "UnityMessageManager";
        internal const string MessagePrefix = "@UnityMessage@";

        #endregion

        #region Fields

        private static readonly object IDLock = new object();
        private static int ID = 0;
        private static int GetNextID()
        {
            lock (IDLock)
            {
                return (++ID);
            }
        }

        private static UnityMessageManager instance;
        public static UnityMessageManager Instance
        {
            get
            {
                if (instance == null)
                {
                    GameObject go = GameObject.Find(GameObjectName) ?? new GameObject(GameObjectName);
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    instance = go.AddComponent<UnityMessageManager>();
                }

                return instance;
            }
        }

        private readonly object stateLock = new object();
        private readonly Dictionary<string, Subscription[]> subscriptions = new Dictionary<string, Subscription[]>();
        private readonly Dictionary<int, UnityResponseDelegate> awaitingRequests = new Dictionary<int, UnityResponseDelegate>();

        #endregion

        static UnityMessageManager()
        {
#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
            RNUnityViewBridge.BridgeBootstrapper.SetIL2CPPBridge(new UnityMessageManager.IL2CPPBridge());
#endif
        }

        #region Events

        /// <summary>
        /// Event raised when unformtted message is received.
        /// </summary>
        public event MessageDelegate OnMessage;

        #endregion

        #region Public methods

        /// <summary>
        /// Sends unformatted message to the React Native engine.
        /// </summary>
        /// <param name="message">The unformatted message.</param>
        /// <remarks>
        /// Message is expected to be in UTF8 encoding.
        /// </remarks>
        public void Send(string message)
            => UnityMessageManager.onUnityMessage(message);

        /// <summary>
        /// Sends message with optional data and response callback.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <param name="onResponse">(optional) Callback called when target replies back.</param>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public void Send(string id, DynamicJson data = null)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                (data != null
                    ? $",\"{nameof(data)}\":{JSON.ToJSON(data)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public Task<DynamicJson> SendAsync(string id, DynamicJson data = null, CancellationToken cancellationToken = default(CancellationToken))
            => this.SendInternalAsync(id, data, cancellationToken, (response) => response.data);

        /// <summary>
        /// Sends request message with optional data.
        /// </summary>
        /// <param name="id">The message id (identifying target).</param>
        /// <param name="data">(optional) The data attached to the message.</param>
        /// <returns>Response message from the target.</returns>
        /// <remarks>
        /// Message format:
        ///  {
        ///    "id": MESSAGE_TARGET_ID, // <paramref name="id" />
        ///    "data": SERIALIZED_DATA, // <paramref name="data" />
        ///    "uuid": UNIQUE_REQUEST_IDENTIFIER // Exists only when <paramref name="onResponse" /> callback is provided
        ///  }
        ///  
        /// Message is automatically prefixed with <see cref="UnityMessageManager.MessagePrefix" /> 
        /// constant to distinguish it from unformatted messages.
        /// </remarks>
        public Task<T> SendAsync<T>(string id, DynamicJson data = null, CancellationToken cancellationToken = default(CancellationToken))
            => this.SendInternalAsync(id, data, cancellationToken, (response) => response.GetData<T>());

        /// <summary>
        /// Subscribes a new message handler to listen for a given message id.
        /// </summary>
        /// <param name="id">The message id.</param>
        /// <param name="handler">The message handler.</param>
        /// <returns>Disposable instance that unsubscribes handler when disposed.</returns>
        public IDisposable Subscribe(string id, UnityMessageDelegate handler)
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

        #endregion

        #region Private methods

        private async Task<T> SendInternalAsync<T>(string id, DynamicJson data, CancellationToken cancellationToken, Func<UnityMessage, T> responseHandler)
        {
            TaskCompletionSource<T> result = new TaskCompletionSource<T>();
            using (cancellationToken.Register(() => result.TrySetCanceled()))
            {
                int uuid = GetNextID();
                this.awaitingRequests.Add(uuid, (response) => result.TrySetResult(responseHandler(response)));

                this.SendRequest(id, uuid, data);

                return await result.Task;
            }
        }

        /// <summary>
        /// Creates and sends request message type.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="data">THe optional request data.</param>
        private void SendRequest(string id, int uuid, DynamicJson data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid},\"type\":{(int)UnityMessageType.Request}" +
                (data != null
                    ? $",\"{nameof(data)}\":{JSON.ToJSON(data)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
        }

        /// <summary>
        /// Creates and sends response message type.
        /// </summary>
        /// <param name="id">The unity message ID.</param>
        /// <param name="uuid">The unique request ID.</param>
        /// <param name="data">THe optional response data.</param>
        private void SendResponse(string id, int uuid, DynamicJson data)
        {
            string json = "{" +
                $"\"{nameof(UnityMessage.id)}\":{JSON.ToJSON(id)}" +
                $",\"{nameof(UnityMessage.uuid)}\":{uuid},\"type\":{(int)UnityMessageType.Response}" +
                (data != null
                    ? $",\"{nameof(data)}\":{JSON.ToJSON(data)}"
                    : string.Empty) +
                "}";

            UnityMessageManager.onUnityMessage(MessagePrefix + json);
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
        public void onMessage(string message)
            => this.OnMessage?.Invoke(message);

        /// <summary>
        /// Handles JSON message as <see cref="UnityMessage" /> instance (with optional callback).
        /// Message will be dispatched only to listeners of a given <see cref="UnityMessage.id" />.
        /// </summary>
        /// <param name="message">The JSON message.</param>
        public void onRNMessage(string message)
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
                if (unityMessage.type == UnityMessageType.Response)
                {
                    // handle callback message
                    UnityResponseDelegate m;
                    var uuid = unityMessage.uuid.Value;
                    if (this.awaitingRequests.TryGetValue(uuid, out m))
                    {
                        this.awaitingRequests.Remove(uuid);
                        m?.Invoke(unityMessage);
                    }
                    else
                    {
                        Debug.LogWarning("Unknown message uuid.");
                    }
                }
                else if (this.subscriptions.TryGetValue(unityMessage.id, out subscriptionList))
                {
                    var args = new UnityMessageHandlerImpl(unityMessage);

                    // handle as regular message
                    foreach (Subscription s in subscriptionList)
                    {
                        try
                        {
                            s.handler.Invoke(args);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning("Failed to handle incoming message");
                            Debug.LogError(e, this);
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("Unknown message id.");
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("Failed to parse incoming message");
                Debug.LogError(e, this);
            }
        }

        /// <summary>
        /// Send serialized message to React Native app.
        /// </summary>
        /// <param name="message">The serialized message to send.</param>
#if UNITY_IOS && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void onUnityMessage (string message);
#elif UNITY_ANDROID && !UNITY_EDITOR
        private static void onUnityMessage (string message)
        {
            using (AndroidJavaClass jc = new AndroidJavaClass("com.reactnative.unity.view.UnityUtils"))
            {
                jc.CallStatic("onUnityMessage", message);
            }
        }
#elif UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
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

        #endregion

        #region Types

        private delegate void UnityResponseDelegate(UnityMessage response);

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
                    handler?.Invoke(this);
                }
            }
        }

        private sealed class UnityMessageHandlerImpl : UnityMessageHandler
        {
            public UnityMessageHandlerImpl(UnityMessage message)
                : base(message) { }

            public override void SetResponse(DynamicJson data)
            {
                if (this.IsRequest && this.message.uuid.HasValue)
                {
                    UnityMessageManager.Instance.SendResponse(
                        this.message.id,
                        this.message.uuid.Value,
                        this.message.data);
                }
                else
                {
                    Debug.LogError("This message is not a request type.");
                }
            }
        }

#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
        private sealed class IL2CPPBridge : RNUnityViewBridge.IIL2CPPBridge
        {
            public void onMessage(string gameObject, string method, string message)
            {
                GameObject o = null;
                if (gameObject == UnityMessageManager.GameObjectName)
                {
                    if (method == nameof(UnityMessageManager.onMessage))
                    {
                        UnityMessageManager.Instance.onMessage(message);
                    }
                    else if (method == nameof(UnityMessageManager.onRNMessage))
                    {
                        UnityMessageManager.Instance.onRNMessage(message);
                    }
                    else
                    {
                        o = UnityMessageManager.Instance.gameObject;
                    }
                }
                else
                {
                    o = GameObject.Find(gameObject);
                }

                if (o != null)
                {
                    o.SendMessage(method, message, SendMessageOptions.DontRequireReceiver);
                }
            }

            public void Shutdown()
            {
                RNUnityViewBridge.BridgeBootstrapper.SetIL2CPPBridge(null);
                UnityEngine.Application.Unload();
            }
        }
#endif

        #endregion
    }
}
