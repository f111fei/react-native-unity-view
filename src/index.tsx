import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules, NativeSyntheticEvent } from 'react-native';
import * as PropTypes from "prop-types";
import * as ViewPropTypes from 'react-native/Libraries/Components/View/ViewPropTypes';

const { UIManager } = NativeModules;

const UNITY_VIEW_REF = 'unityview';

export interface UnityViewMessageEventData {
    message: string;
}

export interface UnityViewMessage {
    name: string;
    data: any;
    callBack?: (data: any) => void;
}

let sequence = 0;
function generateId() {
    sequence = sequence + 1;
    return sequence;
}

const waitCallbackMessageMap: {
    [id: number]: UnityViewMessage
} = {};

const messagePrefix = '@UnityMessage@';

export interface UnityViewProps extends ViewProperties {
    /** 
     * Receive message from unity. 
     */
    onMessage?: (message: string) => void;
    onUnityMessage?: (handler: MessageHandler) => void;
}

export class MessageHandler {
    public static deserialize(viewHandler: number, message: string) {
        const m = JSON.parse(message);
        const handler = new MessageHandler(viewHandler);
        handler.id = m.id;
        handler.seq = m.seq;
        handler.name = m.name;
        handler.data = m.data;
        return handler;
    }

    public id: number;
    public seq: 'start' | 'end' | '';
    public name: string;
    public data: any;

    private viewHandler: number;

    constructor(viewHandler: number) {
        this.viewHandler = viewHandler;
    }

    public send(data: any) {
        UIManager.dispatchViewManagerCommand(
            this.viewHandler,
            UIManager.UnityView.Commands.postMessage,
            ['UnityMessageManager', 'onUnityMessage', messagePrefix + JSON.stringify({
                id: this.id,
                seq: 'end',
                name: this.name,
                data: data
            })]
        );
    }
}

export default class UnityView extends React.Component<UnityViewProps> {
    public static propTypes = {
        ...ViewPropTypes,
        onMessage: PropTypes.func
    }

    /**
     * Send Message to Unity.
     * @param gameObject The Name of GameObject. Also can be a path string.
     * @param methodName Method name in GameObject instance.
     * @param message The message will post.
     */
    public postMessage(gameObject: string, methodName: string, message: string) {
        UIManager.dispatchViewManagerCommand(
            this.getViewHandle(),
            UIManager.UnityView.Commands.postMessage,
            [String(gameObject), String(methodName), String(message)]
        );
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

    /**
     * Send Message to UnityMessageManager.
     * @param message The message will post.
     */
    public postMessageToUnityManager(message: string | UnityViewMessage) {
        if (typeof message === 'string') {
            this.postMessage('UnityMessageManager', 'onMessage', message);
        } else {
            const id = generateId();
            if (message.callBack) {
                waitCallbackMessageMap[id] = message;
            }
            this.postMessage('UnityMessageManager', 'onUnityMessage', messagePrefix + JSON.stringify({
                id: id,
                seq: message.callBack ? 'start' : '',
                name: message.name,
                data: message.data
            }));
        }
    };

    private getViewHandle() {
        return findNodeHandle(this.refs[UNITY_VIEW_REF] as any);
    }

    private onMessage(event: NativeSyntheticEvent<UnityViewMessageEventData>) {
        let message = event.nativeEvent.message
        if (message.startsWith(messagePrefix)) {
            message = message.replace(messagePrefix, '');

            const handler = MessageHandler.deserialize(this.getViewHandle(), message);

            if (handler.seq === 'end') {
                // handle callback message
                const m = waitCallbackMessageMap[handler.id];
                delete waitCallbackMessageMap[handler.id];
                if (m && m.callBack != null) {
                    m.callBack(handler.data);
                }
                return;
            }

            if (this.props.onUnityMessage) {
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
                ref={UNITY_VIEW_REF}
                {...props}
                onMessage={this.onMessage.bind(this)}
            >
            </NativeUnityView>
        );
    }
}

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView', UnityView);