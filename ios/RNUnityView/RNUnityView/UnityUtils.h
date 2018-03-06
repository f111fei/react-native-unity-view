#import <Foundation/Foundation.h>

#ifndef UnityUtils_h
#define UnityUtils_h

void unity_init(void);

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message);

#endif /* UnityUtils_h */
