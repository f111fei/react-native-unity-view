namespace ReactNative
{
    /// <summary>
    /// Possible types of the message instance.
    /// </summary>
    public enum UnityMessageType : int
    {
        /// <summary>
        /// Default.
        /// </summary>
        Undefined = 0,

        /// <summary>
        /// Message is a request and caller will await for response.
        /// </summary>
        Request = 1,

        /// <summary>
        /// Message is a response to some request sent earlier.
        /// </summary>
        Response = 2
    }
}
