#import <React/RCTViewManager.h>
#import <UnityFramework/UnityFramework.h>
#import "UnityUtils.h"
#import "RNUnityView.h"

@interface RNUnityViewManager : RCTViewManager

@property (nonatomic, strong) RNUnityView *currentView;

@end
