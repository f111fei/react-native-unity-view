using System;

namespace ReactNative
{
    public sealed class UnityRequestAttribute : UnityMessageAttribute
    {
        public UnityRequestAttribute(int requestID, Type responseType)
            : base(requestID)
        {
            this.ResponseType = responseType;
        }

        public UnityRequestAttribute(Enum requestType, Type responseType)
            : this(Convert.ToInt32(requestType), responseType) { }

        public Type ResponseType { get; }
    }
}
