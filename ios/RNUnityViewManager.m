#import "RNUnityViewManager.h"
#import "RNUnityView.h"

@implementation RNUnityViewManager

@synthesize bridge = _bridge;

RCT_EXPORT_MODULE(RNUnityView)

- (UIView *)view
{
  self.currentView = [[RNUnityView alloc] init];
  if ([UnityUtils isUnityReady]) {
    NSLog(@"already initialized");
    UIView* view = [[GetUnityFramework() appController] rootView];
    [self.currentView setUnityView: view];
    [self reattachView:view];
  } else {
    NSLog(@"about to be initialized");
    [UnityUtils createPlayer:^ (UIView* view) {
      NSLog(@"got initialized");
      [self.currentView setUnityView: view];
      [self reattachView:view];
    }];
  }
  return self.currentView;
}

- (dispatch_queue_t)methodQueue
{
  return dispatch_get_main_queue();
}

+ (BOOL)requiresMainQueueSetup
{
  return YES;
}

- (void)setBridge:(RCTBridge *)bridge {
  _bridge = bridge;
}

- (void)attachView:(UIView*) unityView {
  UIView* superView = unityView.superview;
  if (superView != nil) {
    NSLog(@"removing parent view");
    [unityView removeFromSuperview];
    [superView layoutIfNeeded];
  }
  
  NSLog(@"adding parent view");
  [self.currentView addSubview:unityView];
}

- (void)reattachView:(UIView*) unityView{
  if (!unityView){
    NSLog(@"not init unityview");
    return;
  }
  
  if (unityView.superview != self.currentView) {
    NSLog(@"changing parent unityview");
    [self attachView:unityView];
  }
}

RCT_EXPORT_METHOD(postMessage:(nonnull NSNumber *)reactTag gameObject:(NSString *)gameObject methodName:(NSString *)methodName message:(NSString *)message)
{
  UnityPostMessage(gameObject, methodName, message);
}

RCT_EXPORT_METHOD(pause:(nonnull NSNumber *)reactTag)
{
  UnityPauseCommand();
}

RCT_EXPORT_METHOD(resume:(nonnull NSNumber *)reactTag)
{
  UnityResumeCommand();
}

@end
