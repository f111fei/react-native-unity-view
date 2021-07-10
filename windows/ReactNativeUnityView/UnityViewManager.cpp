#include "pch.h"
#include "UnityViewManager.h"

#include "JSValueReader.h"
#include "JSValueXaml.h"
#include "UnityNativeModule.h"

using namespace winrt;
using namespace Microsoft::ReactNative;
using namespace Windows::Foundation;
using namespace Windows::Foundation::Collections;

using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Controls;

namespace winrt::ReactNativeUnityView::implementation {

    // IViewManager
    hstring UnityViewManager::Name() noexcept {
        return L"UnityView";
    }

    FrameworkElement UnityViewManager::CreateView() noexcept {
        auto player = UnityUtils::ensurePlayer();
        return player.GetSwapChainPanel();
    }

    // IViewManagerWithReactContext
    IReactContext UnityViewManager::ReactContext() noexcept {
        return m_reactContext;
    }

    void UnityViewManager::ReactContext(IReactContext reactContext) noexcept {
        m_reactContext = reactContext;
    }

}