import * as React from "react";
import { requireNativeComponent, ViewProperties, View } from 'react-native';
import * as PropTypes from "prop-types";
import { ViewPropTypes } from 'react-native';
import { UnityMessageHandler, UnityMessageHandlerImpl } from "./UnityMessageHandler";
import { UnityModule } from "./UnityModule";
import { UnityMessageType, UnityMessage } from "./UnityMessage";
import { IUnityRequest } from "./UnityRequest";
import { Observable } from "rxjs";

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

export default class UnityView extends React.Component<UnityViewProps> {
    private m_registrationToken: number;

    public static propTypes = {
        ...ViewPropTypes,
        onMessage: PropTypes.func
    }

    public constructor(props: any) {
        super(props);

        this.setNativeUnityView = this.setNativeUnityView.bind(this);

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
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(request: IUnityRequest<TType, TResponse>, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(id: string, type: TType, data: any, gameObject?: string, methodName?: string): Observable<TResponse>;
    public postMessageAsync<TResponse = any, TType extends number = UnityMessageType>(first: string | IUnityRequest<TType, TResponse>, second: any, third: any, fourth?: string, fifth?: string): Observable<TResponse> {
        return UnityModule.postMessageAsync(first as any, second, third, fourth, fifth);
    }

    public render() {
        const { onUnityMessage, onMessage, ...props } = this.props;
        return (
            <View {...props}>
                <NativeUnityView
                    ref={this.setNativeUnityView}
                    style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0 }}
                    onUnityMessage={onUnityMessage}
                    onMessage={onMessage}
                >
                </NativeUnityView>
                {this.props.children}
            </View>
        );
    }

    private setNativeUnityView(unityView: React.Component<UnityViewProps> | null) {
    }
}

const NativeUnityView = requireNativeComponent('UnityView');