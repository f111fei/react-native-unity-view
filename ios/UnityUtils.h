#import <Foundation/Foundation.h>
#import <UnityFramework/UnityFramework.h>

#ifndef UnityUtils_h
#define UnityUtils_h

#ifdef __cplusplus
extern "C" {
#endif

UnityFramework* GetUnityFramework(void);

void InitArgs(int argc, char* argv[]);

bool UnityIsInited(void);

void InitUnity(void);

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message);

void UnityPauseCommand(void);

void UnityResumeCommand(void);

#ifdef __cplusplus
} // extern "C"
#endif

@protocol UnityEventListener <NSObject>
- (void)onMessage:(NSString *)message;
@end

@interface UnityUtils : NSObject

+ (BOOL)isUnityReady;
+ (void)createPlayer:(void (^)(UIView*))completed;
+ (void)onUnityMessage:(const char*)message;
+ (void)addUnityEventListener:(id<UnityEventListener>)listener;
+ (void)removeUnityEventListener:(id<UnityEventListener>)listener;

@end

#endif /* UnityUtils_h */
