using Newtonsoft.Json.Linq;
using ReactNative.UIManager.Events;

namespace RNUnityView
{
    public sealed class UnityMessageEvent : Event
    {
        public const string EVENT_NAME = "unityMessage";

        private readonly string data;

        public UnityMessageEvent(int viewId, string data)
            : base(viewId)
        {
            this.data = data;
        }

        public override string EventName => EVENT_NAME;

        public override void Dispatch(RCTEventEmitter eventEmitter)
        {
            var eventData = new JObject
            {
                { "message", data },
                { "target", ViewTag },
            };

            eventEmitter.receiveEvent(ViewTag, EventName, eventData);
        }
    }
}
