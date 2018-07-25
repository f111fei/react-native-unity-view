//
//  UnityManager.m
//  RNUnityView
//
//  Created by MaTHtoE on 2018/7/24.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "UnityManager.h"
#import <React/RCTEventDispatcher.h>
#import <React/RCTUtils.h>
#import <React/RCTImageLoader.h>

@implementation UnityManager

- (instancetype)init{
    if (self = [super init]) {

    }
    
    [UnityUtils setUnityMsgCallback:^(NSString* msgStr){
        [self sendEventWithName:@"UnityEvent" body:@{@"message": msgStr}];
    }];
    
    return self;
}

- (NSArray<NSString *> *)supportedEvents
{
    return @[@"UnityEvent"];
}


// To export a module named CalendarManager
RCT_EXPORT_MODULE()

RCT_EXPORT_METHOD(postMessage:(NSString *)gameObject methodName:(NSString *)methodName message:(NSString *)message)
{
    UnityPostMessage(gameObject, methodName, message);
}

RCT_EXPORT_METHOD(pause){
    UnityPauseCommand();
}

RCT_EXPORT_METHOD(resume){
    UnityResumeCommand();
}

@end

