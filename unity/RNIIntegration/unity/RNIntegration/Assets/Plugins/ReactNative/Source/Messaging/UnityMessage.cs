using fastJSON;

namespace ReactNative
{
    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public struct UnityMessage
    {
        /// <summary>
        /// The message ID.
        /// </summary>
        /// <remarks>
        /// The message ID is used to identify target (listener) for the message.
        /// </remarks>
        public string id;

        /// <summary>
        /// Optional data of the message.
        /// </summary>
        public DynamicJson data;

        /// <summary>
        /// Unique identifier of the request-type message.
        /// </summary>
        public int? uuid;

        /// <summary>
        /// Type of the message (Undefined | Request | Response).
        /// </summary>
        public UnityMessageType type;

        /// <summary>
        /// Converts message data to a given type.
        /// </summary>
        /// <typeparam name="T">The type to use for conversion.</typeparam>
        /// <returns>The data in a given type.</returns>
        public T GetData<T>()
            => JSON.ToObject<T>(this.data);
    }
}
