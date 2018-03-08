#import <Foundation/Foundation.h>

#ifndef UnityUtils_h
#define UnityUtils_h

bool unity_inited;

void unity_init(void);

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message);

#endif /* UnityUtils_h */
