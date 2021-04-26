using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace ReactNative
{
    public interface IUnityMessageHandler
    {
        #region Properties

        /// <summary>
        /// The received message.
        /// </summary>
        UnityMessage Message { get; }

        /// <summary>
        /// Returns true if sender of the message awaits response.
        /// </summary>
        bool IsRequest { get; }

        /// <summary>
        /// Returns true if processing of the message (and thus disposing of this handler) has been deferred.
        /// </summary>
        bool IsDeferred { get; }

        /// <summary>
        /// Gets a token to check for request cancellation.
        /// </summary>
        CancellationToken CancellationToken { get; }

        #endregion

        #region Public methods

        /// <summary>
        /// Get deferral for processing the incomming message on another thread.
        /// </summary>
        IDisposable GetDeferral();

        /// <summary>
        /// Send response message to send back to the client.
        /// </summary>
        /// <param name="data">The response data</param>
        void SendResponse(object data = default);

        /// <summary>
        /// Send cancellation to send back to the client.
        /// </summary>
        /// <param name="data">The response data</param>
        void SendCanceled();

        /// <summary>
        /// Send error message to send back to the client.
        /// </summary>
        /// <param name="error">The error info</param>
        void SendError(UnityRequestException error);

        /// <summary>
        /// Send error message to send back to the client.
        /// </summary>
        /// <param name="error">The error info</param>
        /// <param name="memberName">The method where <see cref="SendError(Exception, string, string, int)"/> was called.</param>
        /// <param name="sourceFilePath">The source file where <see cref="SendError(Exception, string, string, int)"/> was called.</param>
        /// <param name="sourceLineNumber">The line number where <see cref="SendError(Exception, string, string, int)"/> was called.</param>
        void SendError(
            Exception error = null,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0);

        #endregion
    }

    public interface IUnityMessageHandler<in TResponse> : IUnityMessageHandler
    {
    }
}
