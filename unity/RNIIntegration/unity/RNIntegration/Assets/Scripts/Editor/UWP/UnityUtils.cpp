
#include "pch.h"
#include "UnityUtils.h"
#include "UnityView.xaml.h"
#include "UnityGenerated.h"

using namespace UnityBridge;
using namespace RNUnityViewBridge;

using namespace Concurrency;
using namespace Platform;
using namespace UnityPlayer;
using namespace Windows::ApplicationModel::Activation;
using namespace Windows::Foundation;
using namespace Windows::Storage;
using namespace Windows::System::Threading;
using namespace Windows::UI;
using namespace Windows::UI::Core;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Navigation;

UnityBridge::UnityPlayer^ UnityUtils::m_player = nullptr;
Windows::ApplicationModel::Activation::SplashScreen^ UnityUtils::m_splashScreen = nullptr;

bool UnityUtils::IsInitialized::get()
{
    return m_player != nullptr && m_player->IsInitialized;
}

UnityBridge::UnityPlayer^ UnityUtils::Player::get()
{
    return m_player;
}

SplashScreen^ UnityUtils::SplashScreen::get()
{
    return m_splashScreen;
}

void UnityUtils::SplashScreen::set(::SplashScreen^ value)
{
    m_splashScreen = value;
}

void UnityUtils::CreatePlayer()
{
    if (m_player == nullptr)
    {
        SetupOrientation();
        auto appCallbacks = AppCallbacks::Instance;
        if (appCallbacks == nullptr)
        {
            appCallbacks = ref new AppCallbacks();
        }

        m_player = ref new UnityPlayer(appCallbacks);
    }
}

void UnityUtils::DestroyPlayer()
{
    m_player = nullptr;
}

void UnityUtils::SetupOrientation()
{
    Unity::SetupDisplay();
}

UnityBridge::UnityPlayer::UnityPlayer(::UnityPlayer::AppCallbacks^ appCallbacks)
    : m_appCallbacks(appCallbacks)
{
    m_appCallbacks->InvokeOnAppThread(ref new ::UnityPlayer::AppCallbackItem([this]()
    {
        BridgeBootstrapper::SetDotNetBridge(this);
    }), false);

    m_view = ref new UnityView();
    m_loadedToken = m_view->Loaded += ref new Windows::UI::Xaml::RoutedEventHandler([this](auto s, auto a) {
        m_viewParent = m_view->Parent;
    });
}

UnityBridge::UnityPlayer::~UnityPlayer()
{
    Quit();
}

bool UnityBridge::UnityPlayer::IsInitialized::get()
{
    return m_appCallbacks != nullptr && m_appCallbacks->IsInitialized();
}

UnityView^ UnityBridge::UnityPlayer::View::get()
{
    return m_view;
}

void UnityBridge::UnityPlayer::Detach()
{
    auto viewParent = m_viewParent;
    m_viewParent = nullptr;

    if (viewParent)
    {
        auto panel = safe_cast<Panel^>(viewParent);
        if (panel)
        {
            unsigned int index;
            if (panel->Children->IndexOf(m_view, &index))
            {
                panel->Children->RemoveAt(index);
            }
            return;
        }

        auto control = safe_cast<UserControl^>(viewParent);
        if (control)
        {
            control->Content = nullptr;
            return;
        }

        auto border = safe_cast<Border^>(viewParent);
        if (border)
        {
            border->Child = nullptr;
            return;
        }
    }
}

void UnityBridge::UnityPlayer::Pause()
{
    m_appCallbacks->UnityPause(1);
}

void UnityBridge::UnityPlayer::Resume()
{
    m_appCallbacks->UnityPause(0);
}

void UnityBridge::UnityPlayer::Quit()
{
    Pause();

    Detach();
    m_view = nullptr;
    m_view->Loaded -= m_loadedToken;

    //UnityUtils::DestroyPlayer();

    //m_appCallbacks->InvokeOnAppThread(ref new ::UnityPlayer::AppCallbackItem([this]()
    //{
    //	//BridgeBootstrapper::SetDotNetBridge(nullptr);
    //	auto bridge = BridgeBootstrapper::GetIL2CPPBridge();
    //	if (bridge != nullptr)
    //	{
    //		bridge->Shutdown();
    //	}
    //}), true);

    //m_appCallbacks = nullptr;
}

void UnityBridge::UnityPlayer::PostMessage(Platform::String^ gameObject, Platform::String^ method, Platform::String^ message)
{
    m_appCallbacks->InvokeOnAppThread(ref new ::UnityPlayer::AppCallbackItem([gameObject, method, message]()
    {
        auto il2cpp = BridgeBootstrapper::GetIL2CPPBridge();
        if (il2cpp != nullptr)
        {
            BridgeBootstrapper::GetIL2CPPBridge()->onMessage(gameObject, method, message);
        }
    }), false);
}

void UnityBridge::UnityPlayer::onMessage(Platform::String^ message)
{
    m_appCallbacks->InvokeOnUIThread(ref new ::UnityPlayer::AppCallbackItem([this, message]()
    {
        OnUnityMessage(message);
    }), false);
}
