using ReactNative.Bridge;
using ReactNative.Modules.Core;
using ReactNative.UIManager;
using System.Collections.Generic;

namespace RNUnityView
{
    public class UnityViewPackage : IReactPackage
    {
        public IReadOnlyList<IViewManager> CreateViewManagers(ReactContext reactContext)
        {
            List<IViewManager> viewManagers = new List<IViewManager>
            {
                new UnityViewManager(reactContext)
            };
            return viewManagers;
        }

        public IReadOnlyList<INativeModule> CreateNativeModules(ReactContext reactContext)
        {
            return new List<INativeModule>();
        }
    }
}
