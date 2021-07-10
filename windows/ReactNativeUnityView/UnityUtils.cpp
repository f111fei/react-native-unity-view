#include "pch.h"
#include "UnityUtils.h"
#include <codecvt>

#include <UnityMessageBridge.h>

using namespace winrt::Windows::UI::Xaml::Controls;

namespace UnityUtils
{
	static AppCallbacks appCallbacks = {nullptr};

	AppCallbacks getPlayer()
	{
		return appCallbacks;
	}

	void createPlayer()
	{
		if (appCallbacks == nullptr)
		{
			auto panel = SwapChainPanel();
			appCallbacks = AppCallbacks();
			appCallbacks.SetCoreWindowEvents(Window::Current().CoreWindow());
			appCallbacks.InitializeD3DXAML();
			appCallbacks.SetSwapChainPanel(panel);
		}
	}

	AppCallbacks ensurePlayer()
	{
		if (appCallbacks == nullptr)
		{
			createPlayer();
		}
		return appCallbacks;
	}

	bool isUnityReady()
	{
		return appCallbacks == nullptr;
	}

	void pause()
	{
		if (appCallbacks != nullptr)
		{
			appCallbacks.UnityPause(1);
		}
	}

	void resume()
	{
		if (appCallbacks != nullptr)
		{
			appCallbacks.UnityPause(0);
		}
	}

	std::wstring string2wstring(std::string str)
	{
		std::wstring result;
		int len = MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.size(), NULL, 0);
		TCHAR* buffer = new TCHAR[len + 1];
		MultiByteToWideChar(CP_ACP, 0, str.c_str(), (int)str.size(), buffer, len);
		buffer[len] = '\0';
		result.append(buffer);
		delete[] buffer;
		return result;
	}

	void postMessage(std::string gameObject, std::string methodName, std::string message)
	{
		//TODO: default send to UnityManager.
		auto wmesaage = string2wstring(message);
		SendMessageToUnity(wmesaage.c_str());
	}

	void addUnityMessageCallBack(std::function<void(wchar_t*)> callback)
	{
		AddUnityMessageCallBack(callback);
	}
}