#pragma once

#include "JSValue.h"
#include "NativeModules.h"
#include "UnityUtils.h"

using namespace winrt::Microsoft::ReactNative;

namespace winrt::ReactNativeUnityView
{

REACT_MODULE(UnityNativeModule, L"UnityNativeModule")
struct UnityNativeModule
{
    // See https://microsoft.github.io/react-native-windows/docs/native-modules for details on writing native modules

    void onUnityMessage(wchar_t* message)
    {
        m_reactContext.EmitJSEvent(L"RCTDeviceEventEmitter", L"onUnityMessage", message);
    }

    REACT_INIT(Initialize)
    void Initialize(ReactContext const &reactContext) noexcept
    {
        m_reactContext = reactContext;
        UnityUtils::addUnityMessageCallBack(std::bind(&UnityNativeModule::onUnityMessage, this, std::placeholders::_1));
    }

    REACT_METHOD(isReady)
    void isReady(React::ReactPromise<bool>&& result) noexcept
    {
        result.Resolve(UnityUtils::isUnityReady());
    }

    REACT_METHOD(createUnity)
    void createUnity(React::ReactPromise<bool>&& result) noexcept
    {
        UnityUtils::createPlayer();
        result.Resolve(true);
    }

    REACT_METHOD(postMessage)
    void postMessage(std::string gameObject, std::string methodName, std::string message) noexcept
    {
        UnityUtils::postMessage(gameObject, methodName, message);
    }

    REACT_METHOD(pause)
    void pause() noexcept
    {
        UnityUtils::pause();
    }

    REACT_METHOD(resume)
    void resume() noexcept
    {
        UnityUtils::resume();
    }

    private:
        ReactContext m_reactContext{nullptr};
};

} // namespace winrt::ReactNativeUnityView
