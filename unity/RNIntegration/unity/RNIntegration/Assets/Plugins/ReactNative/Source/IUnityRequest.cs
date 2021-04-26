using System;

namespace ReactNative
{
    /// <summary>
    /// Describes unity request entity.
    /// </summary>
    public interface IUnityRequest
    {
        int Type();
    }

    /// <summary>
    /// Describes unity request entity.
    /// </summary>
    public interface IUnityRequest<out T> : IUnityRequest
    { }

    /// <summary>
    /// Describes unity request entity.
    /// </summary>
    public interface IUnityRequest<out TType, out TResponse> : IUnityRequest<TType>
        where TType : Enum
    {
        new TType Type();
    }
}
