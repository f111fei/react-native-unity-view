using System;

namespace ReactNative
{
    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public interface IUnityMessage { }

    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public interface IUnityMessage<TType> : IUnityMessage
        where TType : Enum
    {
        TType Type();
    }
}
