/// <reference types="react" />
import * as React from "react";
import { ViewProperties } from 'react-native';
export interface UnityViewMessageEventData {
    message: string;
}
export interface UnityViewMessage {
    name: string;
    data: any;
    callBack?: (data: any) => void;
}
export interface UnityViewProps extends ViewProperties {
    /**
     * Receive message from unity.
     */
    onMessage?: (message: string) => void;
    onUnityMessage?: (handler: MessageHandler) => void;
}
export declare class MessageHandler {
    static deserialize(viewHandler: number, message: string): MessageHandler;
    id: number;
    seq: 'start' | 'end' | '';
    name: string;
    data: any;
    private viewHandler;
    constructor(viewHandler: number);
    send(data: any): void;
}
export default class UnityView extends React.Component<UnityViewProps> {
    static propTypes: any;
    /**
     * Send Message to Unity.
     * @param gameObject The Name of GameObject. Also can be a path string.
     * @param methodName Method name in GameObject instance.
     * @param message The message will post.
     */
    postMessage(gameObject: string, methodName: string, message: string): void;
    /**
     * Pause the unity player
     */
    pause(): void;
    /**
     * Resume the unity player
     */
    resume(): void;
    /**
     * Send Message to UnityMessageManager.
     * @param message The message will post.
     */
    postMessageToUnityManager(message: string | UnityViewMessage): void;
    private getViewHandle();
    private onMessage(event);
    render(): JSX.Element;
}
