#import <Foundation/Foundation.h>

#ifndef UnityUtils_h
#define UnityUtils_h

#ifdef __cplusplus
extern "C" {
#endif
    
bool UnityIsInited(void);

void InitUnity(void);

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message);

void UnityPauseCommand();

void UnityResumeCommand();

#ifdef __cplusplus
} // extern "C"
#endif

@protocol UnityEventListener <NSObject>
- (void)onMessage:(NSString *)message;
@end

typedef void(^onUnityMsg)(NSString * msg);

@interface UnityUtils : NSObject

+ (void)addUnityEventListener:(id<UnityEventListener>)listener;
+ (void)removeUnityEventListener:(id<UnityEventListener>)listener;
+ (void)setUnityMsgCallback:(onUnityMsg)callback;
@end

#endif /* UnityUtils_h */
