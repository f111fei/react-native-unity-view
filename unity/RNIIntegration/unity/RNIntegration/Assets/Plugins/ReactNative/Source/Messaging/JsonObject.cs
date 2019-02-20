using fastJSON;
using System.Collections.Generic;

namespace ReactNative
{
    public class JsonObject : Dictionary<string, object>
    {
        public static implicit operator DynamicJson(JsonObject self)
            => new DynamicJson(self);
    }
}
