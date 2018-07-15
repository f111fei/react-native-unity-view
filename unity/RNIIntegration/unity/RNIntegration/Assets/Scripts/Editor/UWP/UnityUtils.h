#pragma once

namespace UnityBridge
{
    ref class UnityPlayer;
    ref class UnityView;

    public ref class UnityUtils sealed
    {
        static UnityBridge::UnityPlayer^ m_player;
        static Windows::ApplicationModel::Activation::SplashScreen^ m_splashScreen;

    private:
        UnityUtils() {};

    public:
        static property bool IsInitialized
        {
            bool get();
        }

        static property UnityBridge::UnityPlayer^ Player
        {
            UnityBridge::UnityPlayer^ get();
        }

        static property Windows::ApplicationModel::Activation::SplashScreen^ SplashScreen
        {
            Windows::ApplicationModel::Activation::SplashScreen^ get();
            void set(Windows::ApplicationModel::Activation::SplashScreen^ value);
        }

        static void CreatePlayer();
        static void SetupOrientation();

    internal:
        static void DestroyPlayer();
    };

    public delegate void OnUnityMessageDelegate(Platform::String^ message);

    public ref class UnityPlayer sealed : RNUnityViewBridge::IDotNetBridge
    {
        ::UnityPlayer::AppCallbacks^ m_appCallbacks;
        UnityView^ m_view;

        Windows::Foundation::EventRegistrationToken m_loadedToken;
        Windows::UI::Xaml::DependencyObject^ m_viewParent;

    public:
        UnityPlayer(::UnityPlayer::AppCallbacks^ appCallbacks);
        virtual ~UnityPlayer();

        event OnUnityMessageDelegate^ OnUnityMessage;

        property bool IsInitialized
        {
            bool get();
        }

        property UnityView^ View
        {
            UnityView^ get();
        }

        void Detach();

        void Pause();
        void Resume();
        void Quit();

        void PostMessage(Platform::String^ gameObject, Platform::String^ method, Platform::String^ message);

        virtual void onMessage(Platform::String^ message);
    };
}
