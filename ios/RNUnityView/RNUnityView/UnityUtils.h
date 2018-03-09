#import <Foundation/Foundation.h>

#ifndef UnityUtils_h
#define UnityUtils_h

#ifdef __cplusplus
extern "C" {
#endif
    
bool UnityIsInited(void);

void InitUnity(void);

void UnityPostMessage(NSString* gameObject, NSString* methodName, NSString* message);

#ifdef __cplusplus
} // extern "C"
#endif

@protocol UnityEventListener <NSObject>
- (void)onMessage:(NSString *)message;
@end

@interface UnityUtils : NSObject

+ (void)addUnityEventListener:(id<UnityEventListener>)listener;
+ (void)removeUnityEventListener:(id<UnityEventListener>)listener;

@end

#endif /* UnityUtils_h */
