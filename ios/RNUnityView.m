//
//  RNUnityView.m
//  RNUnityView
//
//  Created by xzper on 2018/2/23.
//  Copyright © 2018年 xzper. All rights reserved.
//

#import "RNUnityView.h"

@implementation RNUnityView

- (id)initWithFrame:(CGRect)frame
{
    self = [super initWithFrame:frame];
    return self;
}

- (void)dealloc
{
}

- (void)setUnityView:(UnityView *)view
{
    self.uView = view;
    [self setNeedsLayout];
}

- (void)layoutSubviews
{
    [super layoutSubviews];
    [(UIView *)self.uView removeFromSuperview];
    [self insertSubview:(UIView *)self.uView atIndex:0];
    ((UIView *)self.uView).frame = self.bounds;
    [(UIView *)self.uView setNeedsLayout];
}

@end
