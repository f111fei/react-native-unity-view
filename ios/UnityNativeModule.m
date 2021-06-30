#import "UnityNativeModule.h"

@implementation UnityNativeModule
{
  bool hasListeners;
}

@synthesize bridge = _bridge;

RCT_EXPORT_MODULE(UnityNativeModule);

- (id)init
{
  self = [super init];
  if (self) {
    [UnityUtils addUnityEventListener:self];
  }
  return self;
}

- (NSArray<NSString *> *)supportedEvents
{
  return @[@"onUnityMessage"];
}

+ (BOOL)requiresMainQueueSetup
{
  return YES;
}

-(void)startObserving
{
  hasListeners = YES;
}

-(void)stopObserving
{
  hasListeners=NO;
}

RCT_EXPORT_METHOD(isReady:(RCTPromiseResolveBlock)resolve rejecter:(RCTPromiseRejectBlock)reject)
{
  resolve(@([UnityUtils isUnityReady]));
}
//
//RCT_EXPORT_METHOD(createUnity:(RCTPromiseResolveBlock)resolve rejecter:(RCTPromiseRejectBlock)reject)
//{
//  [UnityUtils createPlayer:^{
//    resolve(@(YES));
//  }];
//}

RCT_EXPORT_METHOD(postMessage:(NSString *)gameObject methodName:(NSString *)methodName message:(NSString *)message)
{
  UnityPostMessage(gameObject, methodName, message);
}

RCT_EXPORT_METHOD(pause)
{
  UnityPauseCommand();
}

RCT_EXPORT_METHOD(resume)
{
  UnityResumeCommand();
}

- (void)onMessage:(NSString *)message {
  //if (hasListeners){
    [_bridge.eventDispatcher sendEvent:<#(id<RCTEvent>)#> sendEvent:<#(id<RCTEvent>)#>
    [self sendEventWithName:@"onUnityMessage"
                       body:[NSString stringWithUTF8String:[message UTF8String]]];
  //}
}

@end
