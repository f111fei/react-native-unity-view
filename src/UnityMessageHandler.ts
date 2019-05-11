import { NativeModules } from 'react-native';
import { UnityMessage, UnityMessagePrefix, UnityMessageImpl, UnityMessageType } from './UnityMessage'
const { UnityNativeModule } = NativeModules;

export interface UnityMessageHandler {
    readonly isCanceled: boolean;
    readonly isRequest: boolean;
    readonly message: UnityMessage;
    sendResponse(data: any): void;
    sendError(error: any): void;
    close(): void;
}

export class UnityMessageHandlerImpl implements UnityMessageHandler {
    private m_responseSent: boolean;
    private m_isCanceled: boolean;
    private m_onClose: (uuid: number) => void;

    constructor(message: UnityMessageImpl, onClose: (uuid: number) => void) {
        this.message = message;
        this.m_onClose = onClose;
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
            UnityNativeModule.postMessage(
                'UnityMessageManager', 'onRNMessage', UnityMessagePrefix + JSON.stringify({
                    id: this.message.id,
                    type: UnityMessageType.Response,
                    uuid: this.message.uuid,
                    data: data,
                })
            );
        }

        this.close();
    }

    public sendError(error: any): void {
        if (this.isRequest) {
            this.m_responseSent = true;
            UnityNativeModule.postMessage('UnityMessageManager', 'onRNMessage', UnityMessagePrefix + JSON.stringify({
                id: this.message.id,
                type: UnityMessageType.Error,
                uuid: this.message.uuid,
                data: error
            }));
        }

        this.close();
    }

    public close(): void {
        if (this.isRequest && this.message.uuid) {
            if (!this.m_responseSent) {
                this.m_responseSent = true;
                if (this.m_isCanceled) {
                    this.sendCanceled();
                } else {
                    this.sendResponse();
                }
            }

            this.m_onClose(this.message.uuid);
        }
    }

    public cancel(): void {
        if (this.isRequest) {
            this.m_isCanceled = true;
        }
    }

    private sendCanceled(): void {
        if (this.isRequest) {
            this.m_responseSent = true;
            UnityNativeModule.postMessage('UnityMessageManager', 'onRNMessage', UnityMessagePrefix + JSON.stringify({
                id: this.message.id,
                type: UnityMessageType.Canceled,
                uuid: this.message.uuid
            }));
        }

        this.close();
    }
}
