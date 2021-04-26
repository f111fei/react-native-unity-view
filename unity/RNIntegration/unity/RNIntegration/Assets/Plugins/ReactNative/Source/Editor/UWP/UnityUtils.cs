#if NETFX_CORE
using ReactNative;
using UnityPlayer;
using Windows.ApplicationModel.Activation;

namespace RNUnityViewBridge
{
    public static class UnityUtils
    {
        private static AppCallbacks appCallbacks;
        public static SplashScreen splashScreen;

        public static bool IsInitialized
            => appCallbacks?.IsInitialized() ?? false;

        public static UnityPlayer Player { get; private set; }

        public static void CreatePlayer()
        {
            if (appCallbacks == null)
            {
                SetupOrientation();
                appCallbacks = new AppCallbacks();
                Player = new UnityPlayer(appCallbacks);
            }
        }

        private static void SetupOrientation()
        {
            Unity.UnityGenerated.SetupDisplay();
        }
    }

    public class UnityPlayer : IDotNetBridge
    {
        private AppCallbacks appCallbacks;

        public UnityPlayer(AppCallbacks appCallbacks)
        {
            this.appCallbacks = appCallbacks;
            this.appCallbacks.InvokeOnAppThread(() =>
            {
                BridgeBootstrapper.SetDotNetBridge(this);
            }, true);
        }

        public delegate void OnUnityMessageDelegate(string message);
        public event OnUnityMessageDelegate OnUnityMessage;

        public void Pause()
        {
            this.appCallbacks.UnityPause(1);
        }

        public void Resume()
        {
            this.appCallbacks.UnityPause(0);
        }

        public void Quit()
        {
            var tmp = this.appCallbacks;
            this.appCallbacks = null;

            if (tmp != null)
            {
                tmp.InvokeOnAppThread(() =>
                {
                    BridgeBootstrapper.SetDotNetBridge(null);
                }, true);

                tmp.Dispose();
            }
        }

        public void PostMessage(string gameObject, string methodName, string message)
        {
            this.appCallbacks.InvokeOnAppThread(() =>
            {
                BridgeBootstrapper.GetIL2CPPBridge()?.onMessage(gameObject, methodName, message);
            }, false);
        }

        public void onMessage(string message)
        {
            this.appCallbacks.InvokeOnUIThread(() =>
            {
                this.OnUnityMessage?.Invoke(message);
            }, false);
        }
    }
}
#endif
