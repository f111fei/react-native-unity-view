using fastJSON;
using System;

namespace ReactNative
{
    /// <summary>
    /// Describes unity message entity.
    /// </summary>
    public struct UnityMessage
    {
        /// <summary>
        /// The message channel ID.
        /// </summary>
        /// <remarks>
        /// The message channel ID can be used to identify target (listener) for the message.
        /// </remarks>
        public string id;

        /// <summary>
        /// Type of the message (Undefined | Request | Response).
        /// </summary>
        public int type;

        /// <summary>
        /// Optional unique identifier of the message.
        /// </summary>
        /// <remarks>
        /// This is used to route response message to the source request message.
        /// When message does not expect any response this message shall be empty.
        /// </remarks>
        public int? uuid;

        /// <summary>
        /// Optional data of the message.
        /// </summary>
        public DynamicJson data;

        /// <summary>
        /// Gets a boolean flag indicating whether this is a simple message (no response expected).
        /// </summary>
        public bool IsSimple => !this.uuid.HasValue || this.type == (int)UnityMessageType.Default;

        /// <summary>
        /// Gets a boolean flag indicating whether this is a request message.
        /// </summary>
        public bool IsRequest => this.uuid.HasValue && this.type >= (int)UnityMessageType.Request;

        /// <summary>
        /// Gets a boolean flag indicating whether this is a response, cancel or error message.
        /// </summary>
        public bool IsRequestCompletion => this.uuid.HasValue && (this.type == (int)UnityMessageType.Response || this.type == (int)UnityMessageType.Error || this.type == (int)UnityMessageType.Cancel);

        /// <summary>
        /// Gets a boolean flag indicating whether this is a response message.
        /// </summary>
        public bool IsResponse => this.uuid.HasValue && this.type == (int)UnityMessageType.Response;

        /// <summary>
        /// Gets a boolean flag indicating whether this is a cancellation request.
        /// </summary>
        public bool IsCancel => this.uuid.HasValue && this.type == (int)UnityMessageType.Cancel;

        /// <summary>
        /// Gets a boolean flag indicating whether this is an error message.
        /// </summary>
        public bool IsError => this.uuid.HasValue && this.type == (int)UnityMessageType.Error;


        /// <summary>
        /// Converts message data to a given type.
        /// </summary>
        /// <typeparam name="T">The type to use for conversion.</typeparam>
        /// <returns>The data in a given type.</returns>
        public T GetData<T>()
            => JSON.ToObject<T>(this.data);

        /// <summary>
        /// Converts message data to minimized JSON.
        /// </summary>
        /// <returns>The message in minimized JSON.</returns>
        public override string ToString()
            => this.ToString(false);

        /// <summary>
        /// Converts message data to formatted JSON.
        /// </summary>
        /// <returns>The message in formatted JSON.</returns>
        public string ToString(bool formatted)
            => ToStringInternal(
                formatted ? JSON.ToNiceJSON : (Func<object, string>)JSON.ToJSON,
                new
                {
                    this.id,
                    this.type,
                    this.uuid,
                    this.data
                });

        private static string ToStringInternal(Func<object, string> serializer, object data)
            => serializer(data);
    }
}
