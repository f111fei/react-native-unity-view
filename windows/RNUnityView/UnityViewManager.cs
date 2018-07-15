using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json.Linq;
using ReactNative.Bridge;
using ReactNative.UIManager;
using ReactNative.UIManager.Events;
using UnityBridge;

namespace RNUnityView
{
    public class UnityViewManager : SimpleViewManager<UnityView>, ILifecycleEventListener
    {
        private const string REACT_CLASS = "UnityView";

        public const int COMMAND_POST_MESSAGE = 1;
        public const int COMMAND_PAUSE = 2;
        public const int COMMAND_RESUME = 3;

        private static bool DONOT_RESUME = false;

        private ReactContext reactContext;

        private static UnityViewManager instance;
        public static UnityViewManager GetInstance(ReactContext reactContext)
        {
            if (instance == null)
            {
                instance = new UnityViewManager();
            }

            instance.SetReactContext(reactContext);

            return instance;
        }

        private UnityViewManager() { }

        public override string Name => REACT_CLASS;

        public override JObject ViewCommandsMap =>
            new JObject
            {
                { "postMessage", COMMAND_POST_MESSAGE },
                { "pause", COMMAND_PAUSE },
                { "resume", COMMAND_RESUME }
            };

        public override void ReceiveCommand(UnityView view, int commandId, JArray args)
        {
            if (UnityUtils.IsInitialized)
            {
                switch (commandId)
                {
                    case COMMAND_POST_MESSAGE:
                        String gameObject = args[0].ToString();
                        String methodName = args[1].ToString();
                        String message = args[2].ToString();
                        UnityUtils.Player.PostMessage(gameObject, methodName, message);
                        break;
                    case COMMAND_PAUSE:
                        UnityUtils.Player.Pause();
                        DONOT_RESUME = true;
                        break;
                    case COMMAND_RESUME:
                        UnityUtils.Player.Resume();
                        DONOT_RESUME = false;
                        break;
                }
            }
        }

        protected override UnityView CreateViewInstance(ThemedReactContext reactContext)
        {
            if (!UnityUtils.IsInitialized)
            {
                UnityUtils.CreatePlayer();
                UnityUtils.Player.OnUnityMessage -= Player_OnUnityMessage;
                UnityUtils.Player.OnUnityMessage += Player_OnUnityMessage;
            }

            UnityUtils.Player.Detach();

            return UnityUtils.Player.View;
        }

        public override void OnDropViewInstance(ThemedReactContext reactContext, UnityView view)
        {
            if (UnityUtils.IsInitialized)
            {
                UnityUtils.Player.OnUnityMessage -= Player_OnUnityMessage;
                UnityUtils.Player.Quit();
            }

            base.OnDropViewInstance(reactContext, view);
        }

        public override JObject CustomDirectEventTypeConstants =>
            new JObject
            {
                {
                    UnityMessageEvent.EVENT_NAME,
                    new JObject
                    {
                        { "registrationName", "onMessage" }
                    }
                }
            };

        public void OnSuspend()
        {
            UnityUtils.Player.Pause();
        }

        public void OnResume()
        {
            if (!UnityUtils.IsInitialized)
            {
                UnityUtils.CreatePlayer();
                UnityUtils.Player.OnUnityMessage -= Player_OnUnityMessage;
                UnityUtils.Player.OnUnityMessage += Player_OnUnityMessage;
            }
            else
            {
                if (!DONOT_RESUME)
                {
                    UnityUtils.Player.Resume();
                }
            }
        }

        public void OnDestroy()
        {
            if (UnityUtils.IsInitialized)
            {
                UnityUtils.Player.Quit();
            }
        }

        private void Player_OnUnityMessage(string message)
        {
            if (UnityUtils.IsInitialized)
            {
                this.DispatchEvent(
                    new UnityMessageEvent(
                        UnityUtils.Player.View.GetTag(),
                        message));
            }
        }

        private void DispatchEvent(Event @event)
        {
            EventDispatcher eventDispatcher = this.reactContext.GetNativeModule<UIManagerModule>().EventDispatcher;
            eventDispatcher.DispatchEvent(@event);
        }

        private void SetReactContext(ReactContext reactContext)
        {
            this.reactContext = reactContext;
            this.reactContext.AddLifecycleEventListener(this);
        }
    }
}
