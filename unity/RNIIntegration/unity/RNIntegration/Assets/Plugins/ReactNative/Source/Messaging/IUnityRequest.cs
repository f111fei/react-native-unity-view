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
    public interface IUnityRequest<T> : IUnityRequest
    { }
}
