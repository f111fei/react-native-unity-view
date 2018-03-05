//
//  RNUnityView.h
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import <UIKit/UIKit.h>

@class RCTEventDispatcher;

@interface RNUnityView : UIView

- (instancetype)initWithEventDispatcher:(RCTEventDispatcher *)eventDispatcher NS_DESIGNATED_INITIALIZER;

@end
