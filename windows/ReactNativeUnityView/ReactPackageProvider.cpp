#include "pch.h"
#include "ReactPackageProvider.h"
#if __has_include("ReactPackageProvider.g.cpp")
#include "ReactPackageProvider.g.cpp"
#endif

#include "UnityNativeModule.h"

#include "UnityViewManager.h"

using namespace winrt::Microsoft::ReactNative;

namespace winrt::ReactNativeUnityView::implementation
{

void ReactPackageProvider::CreatePackage(IReactPackageBuilder const &packageBuilder) noexcept
{
    AddAttributedModules(packageBuilder);
    packageBuilder.AddViewManager(
        L"UnityViewManager", []() { return winrt::make<UnityViewManager>(); });
}

} // namespace winrt::ReactNativeUnityView::implementation
