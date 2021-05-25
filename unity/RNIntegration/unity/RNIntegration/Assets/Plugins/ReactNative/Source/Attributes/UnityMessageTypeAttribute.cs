using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ReactNative
{
    [AttributeUsage(AttributeTargets.Enum, AllowMultiple = false)]
    public sealed class UnityMessageTypeAttribute : Attribute { }
}