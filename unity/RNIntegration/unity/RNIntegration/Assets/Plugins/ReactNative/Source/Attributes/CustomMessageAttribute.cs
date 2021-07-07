using System;

namespace ReactNative
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public sealed class CustomMessageAttribute : Attribute
    {
        public CustomMessageAttribute() { }
    }
}
