import { NativeModules } from 'react-native';
import { UnityMessage, UnityMessageImpl, UnityMessagePrefix, UnityMessageType } from "./UnityMessage";
import { UnityMessageHandler, UnityMessageHandlerImpl } from "./UnityMessageHandler";
import { Observable, Subscriber, TeardownLogic } from 'rxjs';
import { IUnityRequest } from './UnityRequest';
import UnityEventEmitter from './UnityEventEmitter';

const { UnityNativeModule } = NativeModules;

declare const __DEBUG_UNITY_VIEW__: boolean;

interface ResponseCallback {
    id: string;
    onNext: (response: UnityMessage) => void;
    onError: (reason?: UnityMessage) => void;
    onCanceled: () => void;
    onComplete: () => void;
};

const responseCallbackMessageMap: {
    [uuid: number]: ResponseCallback;
} = {};
const removeResponseCallback = function (uuid: number | string) {
    if (responseCallbackMessageMap[uuid]) {
        delete responseCallbackMessageMap[uuid];
    }
}

const requestCallbackMessageMap: {
    [uuid: number]: UnityMessageHandlerImpl;
} = {};
const removeRequestCallback = function (uuid: number | string) {
    if (requestCallbackMessageMap[uuid]) {
        delete requestCallbackMessageMap[uuid];
    }
}

export interface UnityModule {
    /**
     * Return whether is unity ready.
     */
    isReady(): Promise<boolean>;
    /**
     * Manual init the Unity. Usually Unity is auto created when the first view is added.
     */
    createUnity(): Promise<boolean>;
    /**
     * Send Message to Unity.
     * @param message The message to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    postMessage(message: string | UnityMessage, gameObject?: string, methodName?: string): void;
    /**
     * Send Message to UnityMessageManager.
     * @param request The request to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(request: IUnityRequest<TType, TData, TResponse>, gameObject?: string, methodName?: string): Observable<TResponse>;
    /**
     * Send Message to UnityMessageManager.
     * @param id The request target ID to post.
     * @param data The request data to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    /**
    * Send Message to UnityMessageManager.
    * @param id The request target ID to post.
    * @param type The custom request type to post.
    * @param data The request data to post.
    * @param gameObject (optional) The Name of GameObject. Also can be a path string.
    * @param methodName (optional) Method name in GameObject instance.
    */
    postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, type: TType, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    /**
     * Pause the unity player
     */
    pause(): void;
    /**
     * Pause the unity player
     */
    resume(): void;
    /**
     * Receive string and json message from unity.
     */
    addMessageListener(listener: (message: string | UnityMessageHandler) => void): number;
    /**
     * Only receive string message from unity.
     */
    addStringMessageListener(listener: (message: string) => void): number;
    /**
     * Only receive json message from unity.
     */
    addUnityMessageListener(listener: (handler: UnityMessageHandler) => void): number;
    /**
     * Remove message listener.
     */
    removeMessageListener(handleId: number): void;
    /**
     * Clears pending requests before shutting down the module.
     */
    clear();
}

let sequence = 0;
function generateUuid() {
    sequence = sequence + 1;
    return sequence;
}

class UnityModuleImpl implements UnityModule {
    private hid = 0;
    private stringListeners: {
        [hid: number]: (message: string) => void
    }
    private unityMessageListeners: {
        [hid: number]: (message: UnityMessageHandler) => void
    }

    public constructor() {
        this.createListeners();
    }

    private createListeners() {
        this.stringListeners = {};
        this.unityMessageListeners = {};
        UnityEventEmitter.addListener('onUnityMessage', (message) => {
            const result = this.handleMessage(message);
            if (result) {
                if (typeof result === 'string') {
                    Object.values(this.stringListeners).forEach(listener => {
                        listener(result);
                    });
                } else {
                    Object.values(this.unityMessageListeners).forEach(listener => {
                        listener(result);
                    });
                }
            }
        });
    }

    private getHandleId() {
        this.hid = this.hid + 1;
        return this.hid;
    }

    public async isReady() {
        return UnityNativeModule.isReady();
    }

    public async createUnity() {
        return UnityNativeModule.createUnity();
    }

    /**
     * Send Message to Unity.
     * @param message The message to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    public postMessage(message: string | UnityMessage, gameObject?: string, methodName?: string): void {
        if (gameObject === undefined) {
            gameObject = 'UnityMessageManager';
        }

        if (typeof message === 'string') {
            if (methodName === undefined) {
                methodName = 'onMessage'
            }
            this.postMessageInternal(gameObject, methodName, message);
        } else {
            if (methodName === undefined) {
                methodName = 'onRNMessage'
            }
            this.postMessageInternal(gameObject, methodName, UnityMessagePrefix + JSON.stringify({
                id: message.id,
                data: message.data
            }));
        }
    };

    /**
     * Send Message to UnityMessageManager.
     * @param request The request to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(request: IUnityRequest<TType, TData, TResponse>, gameObject?: string, methodName?: string): Observable<TResponse>;
    /**
     * Send Message to UnityMessageManager.
     * @param id The request target ID to post.
     * @param data The request data to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    /**
    * Send Message to UnityMessageManager.
    * @param id The request target ID to post.
    * @param type The custom request type to post.
    * @param data The request data to post.
    * @param gameObject (optional) The Name of GameObject. Also can be a path string.
    * @param methodName (optional) Method name in GameObject instance.
    */
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, type: TType, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(first: string | IUnityRequest<TType, TData, TResponse>, second: any, third: any, fourth?: string, fifth?: string): Observable<TResponse> {
        var id: string;
        var type: number;
        var data: any;
        var gameObject: string;
        var methodName: string;
        if (typeof first === 'string') {
            id = first;

            if (typeof second === 'number') {
                /* postMessageAsync<TResponse>(id: string, type: UnityMessageType | number, data: any, gameObject?: string, methodName?: string) */
                type = second;
                data = third;
                gameObject = fourth;
                methodName = fifth;
            } else {
                /* postMessageAsync<TResponse>(id: string, data: any, gameObject?: string, methodName?: string) */
                type = UnityMessageType.Request;
                data = second;
                gameObject = third;
                methodName = fourth;
            }
        } else {
            /* postMessageAsync<TResponse>(request: UnityRequest, gameObject?: string, methodName?: string) */
            id = first.id;
            type = first.type;
            data = first.data;
            gameObject = second;
            methodName = third;
        }

        if (methodName === undefined) {
            methodName = 'onRNMessage'
        }
        if (gameObject === undefined) {
            gameObject = 'UnityMessageManager';
        }

        return new Observable<TResponse>((subscriber: Subscriber<TResponse>): TeardownLogic => {
            var isCompleted: boolean = false;
            const uuid = generateUuid();
            responseCallbackMessageMap[uuid] = {
                id: id,
                onNext: (response: UnityMessage) => {
                    var data = response.data as TResponse;

                    if(__DEBUG_UNITY_VIEW__) {
                        console.log(`RESPONSE ${uuid}: ${JSON.stringify(data)}`);
                    }

                    subscriber.next(data);
                },
                onError: (response: UnityMessage) => {
                    isCompleted = true; // To block cancellation request
                    subscriber.error(response); // TODO: Add well defined error format
                },
                onCanceled: () => {
                    isCompleted = true; // To block cancellation request
                    subscriber.error(); // TODO: Add well defined cancellation format
                },
                onComplete: () => {
                    isCompleted = true; // To block cancellation request
                    subscriber.complete();
                }
            };

            if (subscriber.closed) {
                removeResponseCallback(uuid);
                return;
            }

            this.postMessageInternal(gameObject, methodName, UnityMessagePrefix + JSON.stringify({
                id: id,
                type: type,
                uuid: uuid,
                data: data
            }));

            if(__DEBUG_UNITY_VIEW__) {
                console.log(`REQUEST ${uuid}: ${JSON.stringify({
                    id: id,
                    type: type,
                    data: data
                })}`);
            }

            // Return cancellation handler
            return () => {
                if (subscriber.closed && !isCompleted) {
                    removeResponseCallback(uuid);
                    // Cancel request when unsubscribed before getting a response
                    this.postMessageInternal(gameObject, methodName, UnityMessagePrefix + JSON.stringify({
                        id: id,
                        type: UnityMessageType.Cancel,
                        uuid: uuid
                    }));
                }
            };
        });
    };

    public pause() {
        UnityNativeModule.pause();
    }

    public resume() {
        UnityNativeModule.resume();
    }

    public addMessageListener(listener: (handler: string | UnityMessageHandler) => void): number {
        const id = this.getHandleId();
        this.stringListeners[id] = listener;
        this.unityMessageListeners[id] = listener;
        return id;
    }

    public addStringMessageListener(listener: (message: string) => void): number {
        const id = this.getHandleId();
        this.stringListeners[id] = listener;
        return id;
    }

    public addUnityMessageListener(listener: (handler: UnityMessageHandler) => void): number {
        const id = this.getHandleId();
        this.unityMessageListeners[id] = listener;
        return id;
    }

    public removeMessageListener(registrationToken: number) {
        if (this.unityMessageListeners[registrationToken]) {
            delete this.unityMessageListeners[registrationToken];
        }
        if (this.stringListeners[registrationToken]) {
            delete this.stringListeners[registrationToken];
        }
    }

    public clear() {
        for (var key in requestCallbackMessageMap) {
            let awaitEntry = requestCallbackMessageMap[key];
            removeRequestCallback(key);
            if (awaitEntry && awaitEntry.close) {
                awaitEntry.close();
            }
        }

        // Cancel all subscription
        for (var key in responseCallbackMessageMap) {
            let awaitEntry = responseCallbackMessageMap[key];
            removeResponseCallback(key);
            if (awaitEntry && awaitEntry.onCanceled) {
                awaitEntry.onCanceled();
            }
        }
    }

    private handleMessage(message: string): string | UnityMessageHandler | undefined {
        if (UnityMessageImpl.isUnityMessage(message)) {
            var unityMessage = new UnityMessageImpl(message);
            if (unityMessage.isRequestCompletion()) {
                // handle callback message
                const awaitEntry = responseCallbackMessageMap[unityMessage.uuid];
                if (awaitEntry) {
                    removeResponseCallback(unityMessage.uuid);
                    if (unityMessage.isResponse()) {
                        if (awaitEntry.onNext) {
                            awaitEntry.onNext(unityMessage);
                        }
                    } else if (unityMessage.isError()) {
                        if (awaitEntry.onError) {
                            awaitEntry.onError(unityMessage);
                        }
                    } else if (unityMessage.isCanceled()) {
                        if (awaitEntry.onCanceled) {
                            awaitEntry.onCanceled();
                        }
                    } else {
                        console.warn("Unknown message type: " + message)
                    }

                    if (awaitEntry.onComplete != null) {
                        awaitEntry.onComplete();
                    }
                }
            } else if (unityMessage.isCancel()) {
                if (__DEBUG_UNITY_VIEW__) {
                    console.log(`CANCELED ${unityMessage.uuid}`);
                }
                const handler = requestCallbackMessageMap[unityMessage.uuid];
                if (handler && handler.cancel) {
                    handler.cancel();
                }
            } else {
                if (__DEBUG_UNITY_VIEW__) {
                    console.log("GENERAL" + message.substr(UnityMessagePrefix.length));
                }

                if (Object.keys(this.unityMessageListeners).length > 0) {
                    const handler = new UnityMessageHandlerImpl(
                      unityMessage as UnityMessageImpl,
                      removeRequestCallback);
                    if (handler.isRequest) {
                        requestCallbackMessageMap[unityMessage.uuid] = handler;
                    }

                    return handler;
                }
            }
        } else {
            if (__DEBUG_UNITY_VIEW__) {
                console.log('Received: ' + message);
            }

            return message;
        }
    }

    private postMessageInternal(gameObject: string, methodName: string, message: string) {
        UnityNativeModule.postMessage(gameObject, methodName, message);
    };
}

export const UnityModule: UnityModule = new UnityModuleImpl();
