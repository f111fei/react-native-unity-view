using System;
using System.Runtime.CompilerServices;

namespace ReactNative
{
    public sealed class UnityRequestException : Exception
    {
        public UnityRequestException(string message,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
            : this(message, null, default(UnityMessage), memberName, sourceFilePath, sourceLineNumber)
        { }

        public UnityRequestException(string message,
                                     Exception innerException,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
            : this(message, innerException, default(UnityMessage), memberName, sourceFilePath, sourceLineNumber)
        { }

        public UnityRequestException(Exception innerException,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
            : this(null, innerException, default(UnityMessage), memberName, sourceFilePath, sourceLineNumber)
        { }

        public UnityRequestException(UnityMessage errorMessage,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
            : this(null, null, errorMessage, memberName, sourceFilePath, sourceLineNumber)
        { }

        private UnityRequestException(string message,
                                     Exception innerException,
                                     UnityMessage errorMessage,
                                     [CallerMemberName] string memberName = "",
                                     [CallerFilePath] string sourceFilePath = "",
                                     [CallerLineNumber] int sourceLineNumber = 0)
            : base(message, innerException)
        {
            this.error = errorMessage;
            this.memberName = memberName;
            this.sourceFilePath = sourceFilePath;
            this.sourceLineNumber = sourceLineNumber;
        }

        public UnityMessage error { get; }

        public string memberName { get; }

        public string sourceFilePath { get; }

        public int sourceLineNumber { get; }

        public override string ToString()
            => $"{this.error}\n\n{base.ToString()}";
    }
}
