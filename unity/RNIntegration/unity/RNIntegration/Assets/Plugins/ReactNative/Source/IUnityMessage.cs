using System;

namespace ReactNative
{
    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public interface IUnityMessage
    {
        int Type();
    }

    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public interface IUnityMessage<T> where T : Enum
    {
        T Type();
    }
}
