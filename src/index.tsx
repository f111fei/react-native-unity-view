import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules, NativeSyntheticEvent } from 'react-native';
import * as PropTypes from "prop-types";
import * as ViewPropTypes from 'react-native/Libraries/Components/View/ViewPropTypes';

const { UIManager } = NativeModules;

export interface UnityViewMessageEventData {
    message: string;
}

export enum UnityMessageType {
    Unknown = 0,
    Request = 1,
    Response = 2
}

export interface UnityMessage {
    id: string;
    data: any;
    uuid?: number;
    type?: UnityMessageType;
}

let sequence = 0;
function generateUuid() {
    sequence = sequence + 1;
    return sequence;
}

interface ResponseCallback {
    id: string;
    resolve: (response: UnityMessage) => void;
    reject: () => void;
}

const responseCallbackMessageMap: {
    [uuid: number]: ResponseCallback;
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
    readonly message: UnityMessage;
    setResponse(data: any): void;
}

class UnityMessageHandlerImpl implements UnityMessageHandler {
    private m_viewHandler: number;

    constructor(viewHandler: number, message: UnityMessage) {
        this.m_viewHandler = viewHandler;
        this.message = message;
    }

    public message: UnityMessage;

    public setResponse(data: any): void {
        UIManager.dispatchViewManagerCommand(
            this.m_viewHandler,
            UIManager.UnityView.Commands.postMessage,
            ['UnityMessageManager', 'onRNMessage', messagePrefix + JSON.stringify({
                id: this.message.id,
                data: data,
                uuid: this.message.uuid,
                type: UnityMessageType.Response
            })]
        );
    }
}

export default class UnityView extends React.Component<UnityViewProps> {
    private m_unityView: React.ComponentClass<UnityViewProps> | null = null;

    public constructor(props: any) {
        super(props);

        this.setNativeUnityView = this.setNativeUnityView.bind(this);
    }

    public static propTypes = {
        ...ViewPropTypes,
        onMessage: PropTypes.func
    }

    public componentWillUnmount() {
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
    public postMessageAsync<T>(message: UnityMessage, gameObject?: string, methodName?: string): Promise<T> {
        if (methodName === undefined) {
            methodName = 'onRNMessage'
        }
        if (gameObject === undefined) {
            gameObject = 'UnityMessageManager';
        }

        return new Promise<T>((resolve, reject) => {
            const uuid = generateUuid();
            responseCallbackMessageMap[uuid] = {
                id: message.id,
                resolve: (response: UnityMessage) => {
                    var data = response.data as T;
                    resolve(data);
                },
                reject: reject
            };

            this.postMessageInternal(gameObject, methodName, messagePrefix + JSON.stringify({
                id: message.id,
                data: message.data,
                uuid: uuid,
                type: UnityMessageType.Request
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

            const unityMessage = JSON.parse(message) as UnityMessage;
            if (unityMessage.type === UnityMessageType.Response && unityMessage.uuid !== undefined) {
                // handle callback message
                const awaitEntry = responseCallbackMessageMap[unityMessage.uuid];
                if (awaitEntry) {
                    delete responseCallbackMessageMap[unityMessage.uuid];
                    if (awaitEntry && awaitEntry.resolve != null) {
                        awaitEntry.resolve(unityMessage);
                    }
                }
            } else if (this.props.onUnityMessage) {
                let handler = new UnityMessageHandlerImpl(this.getViewHandle(), unityMessage);
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

    private setNativeUnityView(unityView: React.ComponentClass<UnityViewProps> | null) {
        this.m_unityView = unityView;
    }
}

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView', UnityView);