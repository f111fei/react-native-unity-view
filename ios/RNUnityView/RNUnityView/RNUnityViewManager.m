//
//  RNUnityViewManager.m
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "RNUnityViewManager.h"
#import "RNUnityView.h"

@implementation RNUnityViewManager

@synthesize bridge = _bridge;

RCT_EXPORT_MODULE()

- (UIView *)view
{
    return [[RNUnityView alloc] initWithEventDispatcher:_bridge.eventDispatcher];
}

- (dispatch_queue_t)methodQueue
{
    return dispatch_get_main_queue();
}

@end
