#if UNITY_WSA && !UNITY_EDITOR && ENABLE_WINMD_SUPPORT
using UnityEngine;

namespace ReactNative
{
    public sealed partial class UnityMessageManager
    {
        private sealed class IL2CPPBridge : RNUnityViewBridge.IIL2CPPBridge
        {
            public void onMessage(string gameObject, string method, string message)
            {
                GameObject o = null;
                if (gameObject == UnityMessageManager.GameObjectName)
                {
                    if (method == nameof(UnityMessageManager.onMessage))
                    {
                        UnityMessageManager.instance?.onMessage(message);
                    }
                    else if (method == nameof(UnityMessageManager.onRNMessage))
                    {
                        UnityMessageManager.instance?.onRNMessage(message);
                    }
                    else
                    {
                        o = UnityMessageManager.instance?.gameObject;
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
    }
}
#endif
