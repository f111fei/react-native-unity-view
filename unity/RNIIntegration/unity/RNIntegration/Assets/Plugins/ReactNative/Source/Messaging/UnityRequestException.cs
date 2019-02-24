using System;

namespace ReactNative
{
    public sealed class UnityRequestException : Exception
    {
        public UnityRequestException(string message)
            : base(message)
        { }

        public UnityRequestException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public UnityRequestException(UnityMessage errorMessage)
        {
            if (!errorMessage.IsError) throw new ArgumentException(nameof(errorMessage));

            this.ErrorMessage = errorMessage;
        }

        public UnityMessage ErrorMessage { get; }

        public override string ToString()
            => $"{this.ErrorMessage}\n\n{base.ToString()}";
    }
}
