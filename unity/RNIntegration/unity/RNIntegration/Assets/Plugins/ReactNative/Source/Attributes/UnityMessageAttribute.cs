using System;

namespace ReactNative
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class UnityMessageAttribute : Attribute
    {
        public UnityMessageAttribute(string id)
        {
            Id = id;
        }

        public UnityMessageAttribute(string id, Enum type) : this(id)
        {
            Type = type;
        }

        public string Id { get; }

        public Enum Type { get; }
    }
}
