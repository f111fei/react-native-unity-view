namespace ReactNative
{
    /// <summary>
    /// Possible types of the message instance.
    /// </summary>
    public enum UnityMessageType : int
    {
        /// <summary>
        /// Default message type that does not expect any response.
        /// </summary>
        Default = 0,

        /// <summary>
        /// Message is a response to some request that was sent earlier.
        /// </summary>
        Response = 1,

        /// <summary>
        /// Message describes an error that occurred on the other end.
        /// </summary>
        Error = 2,

        /// <summary>
        /// Message is a request for cancellation.
        /// </summary>
        Cancel = 3,

        /// <summary>
        /// Message is a notification of a request cancellation.
        /// </summary>
        Canceled = 4,

        /// <summary>
        /// The ID of the first request message type free to use.
        /// Values greather than this one can be used for implementing custom requests and message types.
        /// </summary>
        Request = 9
    }
}
