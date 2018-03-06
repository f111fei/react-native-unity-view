//
//  RNUnityViewManager.m
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "RNUnityViewManager.h"
#import "RNUnityView.h"
#import "UnityUtils.h"

@implementation RNUnityViewManager

@synthesize bridge = _bridge;

RCT_EXPORT_MODULE(UnityView)

- (id)init {
    self = [super init];
    unity_init();
    return self;
}

- (UIView *)view
{
    return [[RNUnityView alloc] init];
}

- (dispatch_queue_t)methodQueue
{
    return dispatch_get_main_queue();
}

-(void)listenAppState
{
    for (NSString *name in @[UIApplicationDidBecomeActiveNotification,
                             UIApplicationDidEnterBackgroundNotification,
                             UIApplicationWillTerminateNotification,
                             UIApplicationWillResignActiveNotification,
                             UIApplicationWillEnterForegroundNotification]) {
        
        [[NSNotificationCenter defaultCenter] addObserver:self
                                                 selector:@selector(handleAppStateDidChange:)
                                                     name:name
                                                   object:nil];
    }
}

- (void)handleAppStateDidChange:(NSNotification *)notification
{
    UnityAppController* unityAppController = GetAppController();
    
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
    }
}

- (void)setBridge:(RCTBridge *)bridge {
    _bridge = bridge;
    UIApplication* application = [UIApplication sharedApplication];
    UnityAppController *controller = GetAppController();
    UIWindow* mainWindow = application.keyWindow;
    [controller application:application didFinishLaunchingWithOptions:bridge.launchOptions];
    [mainWindow makeKeyAndVisible];
    [self listenAppState];
}

@end
