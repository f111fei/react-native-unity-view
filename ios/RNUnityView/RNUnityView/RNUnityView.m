//
//  RNUnityView.m
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "RNUnityView.h"

@implementation RNUnityView

-(id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    if (self){
        self.uView = (UIView*)GetAppController().unityView;
    }
    return self;
}

-(void)layoutSubviews
{
    [super layoutSubviews];
    [self.uView removeFromSuperview];
    self.uView.frame = self.bounds;
    [self insertSubview:self.uView atIndex:0];
    [self.uView setNeedsLayout];
}

@end
