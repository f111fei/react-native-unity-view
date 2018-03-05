//
//  RNUnityView.m
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "RCTEventDispatcher.h"
#import "RNUnityView.h"

@implementation RNUnityView

RCTEventDispatcher *_eventDispatcher;

- (id)initWithEventDispatcher:(RCTEventDispatcher *)eventDispatcher
{
    if ((self = [super initWithFrame:CGRectZero])) {
        _eventDispatcher = eventDispatcher;
//        [self addSubview:_tabController.view];
    }
    return self;
}

@end
