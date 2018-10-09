#include "RegisterMonoModules.h"
#include "RegisterFeatures.h"
#include <csignal>
#import <Foundation/Foundation.h>
#import "UnityInterface.h"
#import "UnityUtils.h"
#import <React/RCTLog.h>

// Hack to work around iOS SDK 4.3 linker problem
// we need at least one __TEXT, __const section entry in main application .o files
// to get this section emitted at right time and so avoid LC_ENCRYPTION_INFO size miscalculation
static const int constsection = 0;

bool unity_inited = false;

void UnityInitTrampoline();

extern "C" bool UnityIsInited()
{
    return unity_inited;
}

extern "C" void InitUnity()
{
    if (unity_inited) {
        return;
    }
    unity_inited = true;
    
    NSProcessInfo *processInfo = [NSProcessInfo processInfo];
    int count= (int)[processInfo.arguments count];
    char** argv = new char*[count];
    for(int i=0; i<count; i++) {
        const char* arg = [processInfo.arguments[i] UTF8String];
        argv[i] = new char[strlen(arg)];
        strcpy(argv[i], arg);
    }
    
    UnityInitTrampoline();
    UnityInitRuntime(count, argv);
    
    for(int i=0; i<count; i++){
        delete[] argv[i];
    }
    delete[] argv;
    
    RegisterMonoModules();
    NSLog(@"-> registered mono modules %p\n", &constsection);
    RegisterFeatures();
    
    // iOS terminates open sockets when an application enters background mode.
    // The next write to any of such socket causes SIGPIPE signal being raised,
    // even if the request has been done from scripting side. This disables the
    // signal and allows Mono to throw a proper C# exception.
    std::signal(SIGPIPE, SIG_IGN);
}

extern "C" void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message)
{
    UnitySendMessage([gameObject UTF8String], [methodName UTF8String], [message UTF8String]);
}

extern "C" void UnityPauseCommand()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UnityPause(1);
    });
}

extern "C" void UnityResumeCommand()
{
    dispatch_async(dispatch_get_main_queue(), ^{
        UnityPause(0);
    });
}

@implementation UnityUtils

static NSHashTable* mUnityEventListeners = [NSHashTable weakObjectsHashTable];
static onUnityMsg mUnityMsgCallback;

extern "C" void onUnityMessage(const char* message)
{
    for (id<UnityEventListener> listener in mUnityEventListeners) {
        [listener onMessage:[NSString stringWithUTF8String:message]];
    }
    
    if (mUnityMsgCallback) {
        mUnityMsgCallback([NSString stringWithUTF8String:message]);
    }
}

extern "C" void logToRN(const char* debugMessage, NSInteger logLevel)
{
    _RCTLog((RCTLogLevel)logLevel, [NSString stringWithUTF8String:debugMessage]);
}

+ (void)addUnityEventListener:(id<UnityEventListener>)listener
{
    [mUnityEventListeners addObject:listener];
}

+ (void)removeUnityEventListener:(id<UnityEventListener>)listener
{
    [mUnityEventListeners removeObject:listener];
}

+ (void)setUnityMsgCallback:(onUnityMsg)callback
{
    mUnityMsgCallback = callback;
}
@end
