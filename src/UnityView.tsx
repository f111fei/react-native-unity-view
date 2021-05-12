import * as React from "react";
import { NativeModules, requireNativeComponent, ViewProps, View } from 'react-native';
import * as PropTypes from "prop-types";
const { ViewPropTypes } = require('react-native');
import { UnityMessageHandler, UnityMessageHandlerImpl } from "./UnityMessageHandler";
import { UnityModule } from "./UnityModule";
import { UnityMessageType, UnityMessage } from "./UnityMessage";
import { IUnityRequest } from "./UnityRequest";
import { Observable } from "rxjs";

const { UIManager } = NativeModules;

export interface UnityViewProps extends ViewProps {
    /** 
     * Receive plain text message from unity. 
     */
    onMessage?: (message: string) => void;

    /** 
    * Receive JSON message or request from unity. 
    */
    onUnityMessage?: (handler: UnityMessageHandler) => void;
}

export default class UnityView extends React.Component<UnityViewProps> {
    private m_registrationToken: number;

    public static propTypes = {
        ...ViewPropTypes,
        onMessage: PropTypes.func
    }

    public constructor(props: any) {
        super(props);

        this.m_registrationToken = UnityModule.addMessageListener(message => {
            if (this.props.onUnityMessage && message instanceof UnityMessageHandlerImpl) {
                this.props.onUnityMessage(message);
            }
            if (this.props.onMessage && typeof message === 'string') {
                this.props.onMessage(message);
            }
        });
    }

    public componentWillUnmount() {
        UnityModule.removeMessageListener(this.m_registrationToken);
        UnityModule.clear();
    }

    /**
     * [Deprecated] Use `UnityModule.pause` instead.
     */
    public pause() {
        UnityModule.pause();
    };

    /**
     * [Deprecated] Use `UnityModule.resume` instead.
     */
    public resume() {
        UnityModule.resume();
    };

    /**
     * [Deprecated] Use `UnityModule.postMessage` instead.
     */
    public postMessage(message: string | UnityMessage, gameObject?: string, methodName?: string): void {
        UnityModule.postMessage(message, gameObject, methodName);
    };

    /**
     * [Deprecated] Use `UnityModule.postMessageAsync` instead.
     */
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(request: IUnityRequest<TType, TData, TResponse>, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(id: string, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(id: string, type: TType, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType, TData = any>(first: string | IUnityRequest<TType, TData, TResponse>, second: any, third: any, fourth?: string, fifth?: string): Observable<TResponse> {
        return UnityModule.postMessageAsync(first as any, second, third, fourth, fifth);
    }

    public render() {
        const { onUnityMessage, onMessage, ...props } = this.props;
        return (
            <View {...props}>
                <NativeUnityView
                    style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0 }}
                    onUnityMessage={onUnityMessage}
                    onMessage={onMessage}
                >
                </NativeUnityView>
                {this.props.children}
            </View>
        );
    }
}

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView');
