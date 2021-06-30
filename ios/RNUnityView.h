#import <UIKit/UIKit.h>
#import <React/UIView+React.h>
#import "UnityUtils.h"


@interface RNUnityView : UIView

@property (nonatomic, strong) UnityView* uView;

- (void)setUnityView:(UIView *)view;

@end
