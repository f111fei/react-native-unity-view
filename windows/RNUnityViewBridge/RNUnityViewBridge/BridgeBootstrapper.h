#pragma once

namespace RNUnityViewBridge
{
    public interface class IDotNetBridge
    {
    public:
        void onMessage(Platform::String^ message);
    };

    public interface class IIL2CPPBridge
    {
    public:
        void onMessage(Platform::String^ gameObject, Platform::String^ method, Platform::String^ message);
        void Shutdown();
    };

    public ref class BridgeBootstrapper sealed
    {
    public:
        static IDotNetBridge^ GetDotNetBridge()
        {
            return m_DotNetBridge;
        }

        static void SetDotNetBridge(IDotNetBridge^ dotNetBridge)
        {
            m_DotNetBridge = dotNetBridge;
        }

        static IIL2CPPBridge^ GetIL2CPPBridge()
        {
            return m_IL2CPPBridge;
        }

        static void SetIL2CPPBridge(IIL2CPPBridge^ il2cppBridge)
        {
            m_IL2CPPBridge = il2cppBridge;
        }

    private:
        static IDotNetBridge^ m_DotNetBridge;
        static IIL2CPPBridge^ m_IL2CPPBridge;

        BridgeBootstrapper();
    };

    IDotNetBridge^ BridgeBootstrapper::m_DotNetBridge;
    IIL2CPPBridge^ BridgeBootstrapper::m_IL2CPPBridge;
}
