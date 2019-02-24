import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules, NativeSyntheticEvent } from 'react-native';
import * as PropTypes from "prop-types";
const { ViewPropTypes } = require('react-native');

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

export interface UnityRequest {
    id: string;
    type: UnityMessageType;
    data?: any;
}

let sequence = 0;
function generateUuid() {
    sequence = sequence + 1;
    return sequence;
}

interface ResponseCallback {
    id: string;
    resolve: (response: UnityMessage) => void;
    reject: (reason?: UnityMessage) => void;
}

const responseCallbackMessageMap: {
    [uuid: number]: ResponseCallback;
} = {};

const requestCallbackMessageMap: {
    [uuid: number]: UnityMessageHandlerImpl;
} = {};

const messagePrefix = '@UnityMessage@';

export interface UnityViewProps extends ViewProperties {
    /** 
     * Receive message from unity. 
     */
    onMessage?: (message: string) => void;
    onUnityMessage?: (handler: UnityMessageHandler) => void;
}

export interface UnityMessageHandler {
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
        if (!this.m_responseSent) {
            this.m_responseSent = true;
            this.sendResponse();
        }

        if (this.message.uuid) {
            delete requestCallbackMessageMap[this.message.uuid];
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
            delete requestCallbackMessageMap[key];
            if (awaitEntry && awaitEntry.cancel) {
                awaitEntry.cancel();
            }
        }

        for (var key in responseCallbackMessageMap) {
            let awaitEntry = responseCallbackMessageMap[key];
            delete responseCallbackMessageMap[key];
            if (awaitEntry && awaitEntry.reject) {
                awaitEntry.reject();
            }
        }
    }

    /**
     * Send Message to Unity.
     * @param message The message will post.
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
     * @param message The message will post.
     */

    public postMessageAsync<T>(request: UnityRequest, gameObject?: string, methodName?: string): Promise<T>;
    public postMessageAsync<T>(
        id: string,
        data: any,
        gameObject?: string,
        methodName?: string): Promise<T>;
    public postMessageAsync<T>(id: string, type: UnityMessageType | number, data: any, gameObject?: string, methodName?: string): Promise<T>;
    public postMessageAsync<T>(first: string | UnityRequest, second: any, third: any, fourth?: string, fifth?: string): Promise<T> {
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

        return new Promise<T>((resolve, reject) => {
            const uuid = generateUuid();
            responseCallbackMessageMap[uuid] = {
                id: id,
                resolve: (response: UnityMessage) => {
                    var data = response.data as T;
                    resolve(data);
                },
                reject: reject
            };

            this.postMessageInternal(gameObject, methodName, messagePrefix + JSON.stringify({
                id: id,
                type: type,
                uuid: uuid,
                data: data
            }));
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
            var json = JSON.parse(message) as UnityMessage;
            var unityMessage = new UnityMessageImpl(json);
            if (unityMessage.isRequestCompletion()) {
                if (unityMessage.isCancel()) {
                    const awaitEntry = requestCallbackMessageMap[unityMessage.uuid];
                    if (awaitEntry) {
                        awaitEntry.cancel();
                    }
                } else {
                    // handle callback message
                    const awaitEntry = responseCallbackMessageMap[unityMessage.uuid];
                    if (awaitEntry) {
                        delete responseCallbackMessageMap[unityMessage.uuid];
                        if (unityMessage.isResponse()) {
                            if (awaitEntry.resolve != null) {
                                awaitEntry.resolve(unityMessage);
                            }
                        } else if (unityMessage.isError()) {
                            if (awaitEntry.reject != null) {
                                awaitEntry.reject(unityMessage);
                            }
                        } else if (unityMessage.isCancel()) {
                            /* No-op */
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

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView', UnityView);