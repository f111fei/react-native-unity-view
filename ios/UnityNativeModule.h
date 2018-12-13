//
//  UnityNativeModule.h
//  RNUnityView
//
//  Created by xzper on 2018/12/13.
//  Copyright © 2018 xzper. All rights reserved.
//

#import <React/RCTEventDispatcher.h>
#import <React/RCTBridgeModule.h>
#import "UnityUtils.h"

@interface UnityNativeModule : NSObject <RCTBridgeModule, UnityEventListener>
@end
