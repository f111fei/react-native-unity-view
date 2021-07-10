#pragma once
#include <functional>

using namespace winrt;
using namespace Windows::UI::Xaml;

using namespace UnityPlayer;

namespace UnityUtils
{
	void createPlayer();
	AppCallbacks ensurePlayer();
	AppCallbacks getPlayer();
	bool isUnityReady();
	void pause();
	void resume();
	void postMessage(std::string gameObject, std::string methodName, std::string message);
	void addUnityMessageCallBack(std::function<void(wchar_t*)> callback);
};

