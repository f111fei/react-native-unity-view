//
//  RNUnityView.h
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import <UIKit/UIKit.h>
#import <React/UIView+React.h>

#import "UnityAppController.h"
#import "UnityUtils.h"

@interface RNUnityView : UIView<UnityEventListener>

@property (nonatomic, strong) UIView* uView;

@property (nonatomic, copy) RCTDirectEventBlock onMessage;

@end
