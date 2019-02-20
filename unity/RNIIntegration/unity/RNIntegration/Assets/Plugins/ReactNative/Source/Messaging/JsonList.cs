using fastJSON;
using System.Collections.Generic;

namespace ReactNative
{
    public class JsonList : List<object>
    {
        public static implicit operator DynamicJson(JsonList self)
            => new DynamicJson(self);
    }
}
