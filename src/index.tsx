import * as PropTypes from "prop-types";
import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules, NativeSyntheticEvent, ViewPropTypes } from 'react-native';
import { Observable, Subscriber, TeardownLogic } from 'rxjs';

const { UIManager } = NativeModules;

export interface UnityViewMessageEventData {
    message: string;
}

export enum UnityMessageType {
    Default = 0,
    Response = 1,
    Cancel = 2,
    Error = 3,
    Request = 9,
}

export interface UnityMessage {
    readonly id: string;
    readonly type: UnityMessageType;
    readonly uuid?: number;
    readonly data?: any;

    isSimple(): boolean;
    isRequest(): boolean;
    isRequestCompletion(): boolean;
    isResponse(): boolean;
    isCancel(): boolean;
    isError(): boolean;
}

export interface IUnityRequest {
    readonly id: string;
    readonly type: UnityMessageType;
    readonly data?: any;
}

export class UnityRequest implements IUnityRequest {
    private m_id: string;
    private m_type: UnityMessageType;
    private m_data: any;

    public constructor(id: string, data?: any, type: UnityMessageType = UnityMessageType.Request) {
        this.m_id = id;
        this.m_data = data;
        this.m_type = type;
    }

    public get id(): string {
        return this.m_id;
    }
    public get type(): UnityMessageType {
        return this.m_type;
    }
    public get data(): any {
        return this.m_data;
    }
}

let sequence = 0;
function generateUuid() {
    sequence = sequence + 1;
    return sequence;
}

interface ResponseCallback {
    id: string;
    onNext: (response: UnityMessage) => void;
    onError: (reason?: UnityMessage) => void;
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

const messagePrefix = '@UnityMessage@';

export interface UnityViewProps extends ViewProperties {
    /** 
     * Receive plain text message from unity. 
     */
    onMessage?: (message: string) => void;

    /** 
    * Receive JSON message or request from unity. 
    */
    onUnityMessage?: (handler: UnityMessageHandler) => void;
}

export interface UnityMessageHandler {
    readonly isCanceled: boolean;
    readonly isRequest: boolean;
    readonly message: UnityMessage;
    sendResponse(data: any): void;
    sendError(error: any): void;
}

class UnityMessageImpl implements UnityMessage {
    private m_json: UnityMessage;

    constructor(json: UnityMessage) {
        this.m_json = json;
    }

    public get id(): string {
        return this.m_json.id;
    }
    public get type(): UnityMessageType {
        return this.m_json.type;
    }
    public get uuid(): number | undefined {
        return this.m_json.uuid;
    }
    public get data(): any | undefined {
        return this.m_json.data;
    }

    public isSimple(): boolean {
        return this.uuid === undefined && this.type === UnityMessageType.Default;
    }

    public isRequest(): boolean {
        return this.uuid !== undefined && this.type >= UnityMessageType.Request;
    }

    public isRequestCompletion(): boolean {
        return this.uuid !== undefined && (this.type === UnityMessageType.Response || this.type === UnityMessageType.Cancel || this.type === UnityMessageType.Error);
    }

    public isResponse(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Response;
    }

    public isCancel(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Cancel;
    }

    public isError(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Error;
    }
}

class UnityMessageHandlerImpl implements UnityMessageHandler {
    private m_viewHandler: number;
    private m_responseSent: boolean;
    private m_isCanceled: boolean;

    constructor(viewHandler: number, message: UnityMessageImpl) {
        this.m_viewHandler = viewHandler;
        this.message = message;
    }

    public get isCanceled(): boolean {
        return this.m_isCanceled;
    }

    public get isRequest(): boolean {
        return this.message.isRequest();
    }

    public message: UnityMessage;

    public sendResponse(data?: any): void {
        if (this.isRequest) {
            this.m_responseSent = true;
            UIManager.dispatchViewManagerCommand(
                this.m_viewHandler,
                UIManager.UnityView.Commands.postMessage,
                ['UnityMessageManager', 'onRNMessage', messagePrefix + JSON.stringify({
                    id: this.message.id,
                    type: UnityMessageType.Response,
                    uuid: this.message.uuid,
                    data: data,
                })]
            );
        }

        this.dispose();
    }

    public sendError(error: any): void {
        if (this.isRequest) {
            this.m_responseSent = true;
            UIManager.dispatchViewManagerCommand(
                this.m_viewHandler,
                UIManager.UnityView.Commands.postMessage,
                ['UnityMessageManager', 'onRNMessage', messagePrefix + JSON.stringify({
                    id: this.message.id,
                    type: UnityMessageType.Error,
                    uuid: this.message.uuid,
                    data: error
                })]
            );
        }

        this.dispose();
    }

    public cancel(): void {
        if (this.isRequest) {
            this.m_isCanceled = true;
            this.dispose();
        }
    }

    private dispose(): void {
        if (this.isRequest && this.message.uuid) {
            if (!this.m_responseSent) {
                this.m_responseSent = true;
                this.sendResponse();
            }

            removeRequestCallback(this.message.uuid);
        }
    }
}

export default class UnityView extends React.Component<UnityViewProps> {
    private m_unityView: React.Component<UnityViewProps> | null = null;

    public constructor(props: any) {
        super(props);

        this.setNativeUnityView = this.setNativeUnityView.bind(this);
    }

    public static propTypes = {
        ...ViewPropTypes,
        onMessage: PropTypes.func
    }

    public componentWillUnmount() {
        for (var key in requestCallbackMessageMap) {
            let awaitEntry = requestCallbackMessageMap[key];
            removeRequestCallback(key);
            if (awaitEntry && awaitEntry.cancel) {
                awaitEntry.cancel();
            }
        }

        // Complete all subscription
        for (var key in responseCallbackMessageMap) {
            let awaitEntry = responseCallbackMessageMap[key];
            removeResponseCallback(key);
            if (awaitEntry && awaitEntry.onComplete) {
                awaitEntry.onComplete();
            }
        }
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
            this.postMessageInternal(gameObject, methodName, messagePrefix + JSON.stringify({
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
    public postMessageAsync<T>(request: IUnityRequest, gameObject?: string, methodName?: string): Observable<T>;
    /**
     * Send Message to UnityMessageManager.
     * @param id The request target ID to post.
     * @param data The request data to post.
     * @param gameObject (optional) The Name of GameObject. Also can be a path string.
     * @param methodName (optional) Method name in GameObject instance.
     */
    public postMessageAsync<T>(id: string, data: any, gameObject?: string, methodName?: string): Observable<T>;
    /**
    * Send Message to UnityMessageManager.
    * @param id The request target ID to post.
    * @param type The custom request type to post.
    * @param data The request data to post.
    * @param gameObject (optional) The Name of GameObject. Also can be a path string.
    * @param methodName (optional) Method name in GameObject instance.
    */
    public postMessageAsync<T>(id: string, type: UnityMessageType | number, data: any, gameObject?: string, methodName?: string): Observable<T>;
    public postMessageAsync<T>(first: string | IUnityRequest, second: any, third: any, fourth?: string, fifth?: string): Observable<T> {
        var id: string;
        var type: number;
        var data: any;
        var gameObject: string;
        var methodName: string;
        if (typeof first === 'string') {
            id = first;

            if (typeof second === 'number') {
                /* postMessageAsync<T>(id: string, type: UnityMessageType | number, data: any, gameObject?: string, methodName?: string) */
                type = second;
                data = third;
                gameObject = fourth;
                methodName = fifth;
            } else {
                /* postMessageAsync<T>(id: string, data: any, gameObject?: string, methodName?: string) */
                type = UnityMessageType.Request;
                data = second;
                gameObject = third;
                methodName = fourth;
            }
        } else {
            /* postMessageAsync<T>(request: UnityRequest, gameObject?: string, methodName?: string) */
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

        return new Observable<T>((subscriber: Subscriber<T>): TeardownLogic => {
            var isCompleted: boolean = false;
            const uuid = generateUuid();
            responseCallbackMessageMap[uuid] = {
                id: id,
                onNext: (response: UnityMessage) => {
                    var data = response.data as T;
                    subscriber.next(data);
                },
                onError: (response: UnityMessage) => {
                    // TODO: Add well defined error format
                    subscriber.error(response);
                },
                onComplete: () => {
                    isCompleted = true; // To block cancellation
                    subscriber.complete();
                }
            };

            if (subscriber.closed) {
                removeResponseCallback(uuid);
                return;
            }

            this.postMessageInternal(gameObject, methodName, messagePrefix + JSON.stringify({
                id: id,
                type: type,
                uuid: uuid,
                data: data
            }));

            // Return cancellation handler
            return () => {
                if (subscriber.closed && !isCompleted) {
                    removeResponseCallback(uuid);
                    // Cancel request when unsubscribed before getting a response
                    this.postMessageInternal(gameObject, methodName, messagePrefix + JSON.stringify({
                        id: id,
                        type: UnityMessageType.Cancel,
                        uuid: uuid
                    }));
                }
            };
        });
    };

    /**
     * Pause the unity player
     */
    public pause() {
        UIManager.dispatchViewManagerCommand(
            this.getViewHandle(),
            UIManager.UnityView.Commands.pause,
            []
        );
    };

    /**
     * Resume the unity player
     */
    public resume() {
        UIManager.dispatchViewManagerCommand(
            this.getViewHandle(),
            UIManager.UnityView.Commands.resume,
            []
        );
    };

    private postMessageInternal(gameObject: string, methodName: string, message: string) {
        if (__DEV__) {
            if (message.startsWith(messagePrefix)) {
                console.log('Sending: ' + message.substr(messagePrefix.length));
            } else {
                console.log('Sending: ' + message);
            }
        }

        UIManager.dispatchViewManagerCommand(
            this.getViewHandle(),
            UIManager.UnityView.Commands.postMessage,
            [String(gameObject), String(methodName), String(message)]
        );
    };

    private getViewHandle() {
        return findNodeHandle(this.m_unityView as any);
    }

    private onMessage(event: NativeSyntheticEvent<UnityViewMessageEventData>) {
        let message = event.nativeEvent.message
        if (message.startsWith(messagePrefix)) {
            message = message.replace(messagePrefix, '');

            if (__DEV__) {
                console.log('Received: ' + message);
            }

            var json = JSON.parse(message) as UnityMessage;
            var unityMessage = new UnityMessageImpl(json);
            if (unityMessage.isRequestCompletion()) {
                if (unityMessage.isCancel()) {
                    const awaitEntry = requestCallbackMessageMap[unityMessage.uuid];
                    if (awaitEntry && awaitEntry.cancel) {
                        awaitEntry.cancel();
                    }
                } else {
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
                        } else {
                            console.warn("Unknown message type: " + message)
                        }

                        if (awaitEntry.onComplete != null) {
                            awaitEntry.onComplete();
                        }
                    }
                }
            } else if (this.props.onUnityMessage) {
                let handler = new UnityMessageHandlerImpl(this.getViewHandle(), unityMessage as UnityMessageImpl);

                if (handler.isRequest) {
                    requestCallbackMessageMap[unityMessage.uuid] = handler;
                }

                this.props.onUnityMessage(handler);
            }
        } else {
            if (__DEV__) {
                console.log('Received: ' + message);
            }

            if (this.props.onMessage) {
                this.props.onMessage(message);
            }
        }
    }

    public render() {
        const { ...props } = this.props;
        return (
            <NativeUnityView
                ref={this.setNativeUnityView}
                {...props}
                onMessage={this.onMessage.bind(this)}
            >
            </NativeUnityView>
        );
    }

    private setNativeUnityView(unityView: React.Component<UnityViewProps> | null) {
        this.m_unityView = unityView;
    }
}

const NativeUnityView = requireNativeComponent('UnityView');
