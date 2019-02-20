//
// UnityView.xaml.h
// Declaration of the UnityView class
//

#pragma once

namespace UnityBridge
{
    public ref class UnityView sealed : Windows::UI::Xaml::Controls::UserControl
    {
    public:
        UnityView();

    private:
        Windows::UI::Xaml::Controls::SwapChainPanel^ m_DXSwapChainPanel;
        Windows::UI::Xaml::Controls::Grid^ m_ExtendedSplashGrid;
        Windows::UI::Xaml::Controls::Image^ m_ExtendedSplashImage;
        Windows::Foundation::EventRegistrationToken m_SplashScreenRemovalEventToken;

        ~UnityView();

        void RemoveSplashScreen();
    };
}
