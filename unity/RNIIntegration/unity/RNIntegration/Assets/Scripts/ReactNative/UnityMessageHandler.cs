using fastJSON;
using System.Dynamic;

namespace ReactNative
{
    public abstract class UnityMessageHandler
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UnityMessageHandler" /> class.
        /// </summary>
        protected UnityMessageHandler(UnityMessage message)
        {
            this.message = message;
        }

        #endregion

        #region Fields

        /// <summary>
        /// The received message.
        /// </summary>
        public UnityMessage message { get; }

        #endregion

        #region Properties

        /// <summary>
        /// Returns true if sender of the message awaits response.
        /// </summary>
        public bool IsRequest => this.message.type == UnityMessageType.Request;

        #endregion

        #region Public methods

        /// <summary>
        /// Set response message to send back to the client.
        /// </summary>
        /// <param name="data">The response data</param>
        public abstract void SetResponse(DynamicJson data);

        #endregion
    }
}
