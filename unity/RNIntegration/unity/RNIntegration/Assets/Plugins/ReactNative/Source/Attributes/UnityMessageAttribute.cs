using System;
using UnityEngine.Scripting;

namespace ReactNative
{
    public class UnityMessageAttribute : PreserveAttribute
    {
        public UnityMessageAttribute(int messageID)
        {
            this.ID = messageID;
        }

        public UnityMessageAttribute(Enum messageID)
            : this(Convert.ToInt32(messageID)) { }

        public int ID { get; }
    }
}
