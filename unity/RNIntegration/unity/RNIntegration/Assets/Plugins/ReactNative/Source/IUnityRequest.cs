using System;

namespace ReactNative
{
    /// <summary>
    /// Describes unity request entity without defined response.
    /// </summary>
    public interface IUnityRequest<TType> : IUnityMessage<TType>
        where TType : Enum
    { }

    /// <summary>
    /// Describes unity request entity with defined response.
    /// </summary>
    public interface IUnityRequest<TType, TResponse> : IUnityRequest<TType>, IUnityMessage<TType>
        where TType : Enum
    { }

    /// <summary>
    /// Describes unity request entity without defined response.
    /// </summary>
    public interface IUnityReverseRequest<TType> : IUnityMessage<TType>
        where TType : Enum
    { }

    /// <summary>
    /// Describes unity request entity with defined response.
    /// </summary>
    public interface IUnityReverseRequest<TType, TResponse> : IUnityReverseRequest<TType>, IUnityMessage<TType>
        where TType : Enum
    { }
}
