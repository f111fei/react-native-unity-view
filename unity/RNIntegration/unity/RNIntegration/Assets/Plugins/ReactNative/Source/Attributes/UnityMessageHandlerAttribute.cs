using System;
using UnityEngine.Scripting;

namespace ReactNative
{
    [Preserve]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class UnityMessageHandlerAttribute : Attribute
    {
        public UnityMessageHandlerAttribute(string id)
        {
            Id = id;
        }

        public string Id { get; }
    }
}
