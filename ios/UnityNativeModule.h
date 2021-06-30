#import <React/RCTEventDispatcher.h>
#import <React/RCTBridgeModule.h>
#import <React/RCTEventEmitter.h>
#import <UnityFramework/UnityFramework.h>
#import "UnityUtils.h"

@interface UnityNativeModule : RCTEventEmitter <RCTBridgeModule, UnityEventListener>
@end
