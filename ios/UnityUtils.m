#import <UIKit/UIKit.h>
#import "UnityUtils.h"


// Hack to work around iOS SDK 4.3 linker problem
// we need at least one __TEXT, __const section entry in main application .o files
// to get this section emitted at right time and so avoid LC_ENCRYPTION_INFO size miscalculation
static const int constsection = 0;

int g_argc;
char** g_argv;

UnityFramework* UnityFrameworkLoad() {
  NSString* bundlePath = nil;
  bundlePath = [[NSBundle mainBundle] bundlePath];
  bundlePath = [bundlePath stringByAppendingString: @"/Frameworks/UnityFramework.framework"];
  
  NSBundle* bundle = [NSBundle bundleWithPath: bundlePath];
  if ([bundle isLoaded] == false) [bundle load];
  
  UnityFramework* ufw = [bundle.principalClass getInstance];
  if (![ufw appController]){
    [ufw setExecuteHeader: &_mh_execute_header];
    [ufw setDataBundleId: "com.unity3d.framework"];
  }
  return ufw;
}

UnityFramework* ufw;
UnityFramework* GetUnityFramework(){
  if (ufw == nil){
    ufw = UnityFrameworkLoad();
  }
  return ufw;
}


void InitArgs(int argc, char* argv[])
{
  g_argc = argc;
  g_argv = argv;
}

bool UnityIsInited()
{
  return (ufw != nil);
}


void InitUnity()
{
  if (UnityIsInited()) {
    [ufw showUnityWindow];
    return;
  }
  
  UnityFramework* ufw = GetUnityFramework();
  [ufw setDataBundleId: "com.unity3d.framework"];
  [ufw runEmbeddedWithArgc:g_argc argv:g_argv appLaunchOpts:nil];
  
  UnityAppController* appController = [ufw appController] ;
  if (appController!= nil) {
    [appController window].windowLevel= UIWindowLevelNormal - 1;
  }
}

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message)
{
  NSLog(@"Sending %@ %@ %@", gameObject, methodName, message);
  dispatch_async(dispatch_get_main_queue(), ^{
    [ufw sendMessageToGOWithName:[gameObject UTF8String] functionName:[methodName UTF8String] message:[message UTF8String]];
  });
}

void UnityPauseCommand()
{
  dispatch_async(dispatch_get_main_queue(), ^{
    [ufw pause:true];
  });
}

void UnityResumeCommand()
{
  dispatch_async(dispatch_get_main_queue(), ^{
    [ufw pause:false];
  });
}

@implementation UnityUtils

static NSHashTable* mUnityEventListeners = nil;
static BOOL _isUnityReady = NO;

+(void)initialize{
  if (self == UnityUtils.class){
    mUnityEventListeners = [NSHashTable weakObjectsHashTable];
  }
}

+ (BOOL)isUnityReady
{
  return _isUnityReady;
}

+ (void)handleAppStateDidChange:(NSNotification *)notification
{
  if (!_isUnityReady) {
    return;
  }
  UnityAppController* unityAppController = [GetUnityFramework() appController];
  
  UIApplication* application = [UIApplication sharedApplication];
  
  if ([notification.name isEqualToString:UIApplicationWillResignActiveNotification]) {
    [unityAppController applicationWillResignActive:application];
  } else if ([notification.name isEqualToString:UIApplicationDidEnterBackgroundNotification]) {
    [unityAppController applicationDidEnterBackground:application];
  } else if ([notification.name isEqualToString:UIApplicationWillEnterForegroundNotification]) {
    [unityAppController applicationWillEnterForeground:application];
  } else if ([notification.name isEqualToString:UIApplicationDidBecomeActiveNotification]) {
    [unityAppController applicationDidBecomeActive:application];
  } else if ([notification.name isEqualToString:UIApplicationWillTerminateNotification]) {
    [unityAppController applicationWillTerminate:application];
  } else if ([notification.name isEqualToString:UIApplicationDidReceiveMemoryWarningNotification]) {
    [unityAppController applicationDidReceiveMemoryWarning:application];
  }
}

+ (void)listenAppState
{
  for (NSString *name in @[UIApplicationDidBecomeActiveNotification,
                           UIApplicationDidEnterBackgroundNotification,
                           UIApplicationWillTerminateNotification,
                           UIApplicationWillResignActiveNotification,
                           UIApplicationWillEnterForegroundNotification,
                           UIApplicationDidReceiveMemoryWarningNotification]) {
    
    [[NSNotificationCenter defaultCenter] addObserver:self
                                             selector:@selector(handleAppStateDidChange:)
                                                 name:name
                                               object:nil];
  }
}

+ (void)createPlayer:(void (^)(UIView*))completed
{
  if (_isUnityReady) {
    completed([ufw appController].rootView);
    return;
  }
  
  [[NSNotificationCenter defaultCenter] addObserverForName:@"UnityReady" object:nil queue:[NSOperationQueue mainQueue]  usingBlock:^(NSNotification * _Nonnull note) {
    _isUnityReady = YES;
    completed([ufw appController].rootView);
  }];
  
  if (UnityIsInited()) {
    return;
  }
  
  dispatch_async(dispatch_get_main_queue(), ^{
    
    InitUnity();
    
    // Makes RN window key window to handle events
    UIApplication* application = [UIApplication sharedApplication];
    [application.windows[1] makeKeyWindow];
    
    [UnityUtils listenAppState];
    
    completed([ufw appController].rootView);
  });
}

extern void onUnityMessage(const char* message)
{
  NSLog(@"Receiving: %s", message);
  for (id<UnityEventListener> listener in mUnityEventListeners) {
    [listener onMessage:[NSString stringWithUTF8String:message]];
  }
}

+ (void)addUnityEventListener:(id<UnityEventListener>)listener
{
  [mUnityEventListeners addObject:listener];
}

+ (void)removeUnityEventListener:(id<UnityEventListener>)listener
{
  [mUnityEventListeners removeObject:listener];
}

@end
