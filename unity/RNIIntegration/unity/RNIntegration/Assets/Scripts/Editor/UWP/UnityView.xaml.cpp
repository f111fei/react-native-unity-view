//
// UnityView.xaml.cpp
// Implementation of the UnityView class
//

#include "pch.h"
#include "UnityView.xaml.h"
#include "UnityUtils.h"

using namespace UnityBridge;

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
using namespace Windows::UI::Xaml::Media::Imaging;
using namespace Windows::UI::Xaml::Navigation;

UnityView::UnityView()
{
    m_SplashScreenRemovalEventToken.Value = 0;

    //<SwapChainPanel x:Name="m_DXSwapChainPanel">
    //    <Grid x:Name="m_ExtendedSplashGrid">
    //        <Image x:Name="m_ExtendedSplashImage" Source="Assets/UnitySplashScreen.png" VerticalAlignment="Center" HorizontalAlignment="Center"/>
    //    </Grid>
    //</SwapChainPanel>
    m_DXSwapChainPanel = ref new SwapChainPanel();
    m_ExtendedSplashGrid = ref new Grid();
    m_ExtendedSplashImage = ref new Image();
    m_ExtendedSplashImage->Source = ref new BitmapImage(ref new Uri("ms-appx:///Assets/UnitySplashScreen.png"));
    m_ExtendedSplashImage->VerticalAlignment = ::VerticalAlignment::Center;
    m_ExtendedSplashImage->HorizontalAlignment = ::HorizontalAlignment::Center;
    m_ExtendedSplashGrid->Children->Append(m_ExtendedSplashImage);
    m_DXSwapChainPanel->Children->Append(m_ExtendedSplashGrid);
    Content = m_DXSwapChainPanel;

    auto appCallbacks = AppCallbacks::Instance;

    bool isWindowsHolographic = false;

#if UNITY_HOLOGRAPHIC
    // If application was exported as Holographic check if the device actually supports it,
    // otherwise we treat this as a normal XAML application
    isWindowsHolographic = AppCallbacks::IsMixedRealitySupported();
#endif

    if (isWindowsHolographic)
    {
        if (!appCallbacks->IsInitialized())
        {
            appCallbacks->InitializeViewManager(Window::Current->CoreWindow);
        }
    }
    else
    {
        m_SplashScreenRemovalEventToken = appCallbacks->RenderingStarted += ref new RenderingStartedHandler(this, &UnityView::RemoveSplashScreen);

        if (!appCallbacks->IsInitialized())
        {
            appCallbacks->SetSwapChainPanel(m_DXSwapChainPanel);
            appCallbacks->SetCoreWindowEvents(Window::Current->CoreWindow);
            appCallbacks->InitializeD3DXAML();
        }
    }
}

UnityView::~UnityView()
{
    if (m_SplashScreenRemovalEventToken.Value != 0)
    {
        AppCallbacks::Instance->RenderingStarted -= m_SplashScreenRemovalEventToken;
        m_SplashScreenRemovalEventToken.Value = 0;
    }
}

void UnityView::RemoveSplashScreen()
{
    uint32_t index;

    if (m_DXSwapChainPanel->Children->IndexOf(m_ExtendedSplashGrid, &index))
    {
        m_DXSwapChainPanel->Children->RemoveAt(index);
    }
}
