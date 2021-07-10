#pragma once

#include "pch.h"

#include "winrt/Microsoft.ReactNative.h"

namespace winrt::ReactNativeUnityView::implementation {

    struct UnityViewManager : winrt::implements<
        UnityViewManager,
        winrt::Microsoft::ReactNative::IViewManager,
        winrt::Microsoft::ReactNative::IViewManagerWithReactContext> {
    public:
        UnityViewManager() = default;

        // IViewManager
        winrt::hstring Name() noexcept;

        winrt::Windows::UI::Xaml::FrameworkElement CreateView() noexcept;

        // IViewManagerWithReactContext
        winrt::Microsoft::ReactNative::IReactContext ReactContext() noexcept;

        void ReactContext(winrt::Microsoft::ReactNative::IReactContext reactContext) noexcept;

    private:
        winrt::Microsoft::ReactNative::IReactContext m_reactContext{ nullptr };
    };

}