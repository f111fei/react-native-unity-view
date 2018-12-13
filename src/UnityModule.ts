import { NativeModules, DeviceEventEmitter } from 'react-native';
import MessageHandler, { UnityMessagePrefix } from "./MessageHandler";

const { UnityNativeModule } = NativeModules;

export interface UnityViewMessage {
    name: string;
    data: any;
    callBack?: (data: any) => void;
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
     * Send Message to UnityMessageManager.
     * @param message The message will post.
     */
    postMessageToUnityManager(message: string | UnityViewMessage): void;
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
     * Pause the unity player
     */
    resume(): void;
    /**
     * Receive string and json message from unity.
     */
    addMessageListener(listener: (message: string | MessageHandler) => void): number;
    /**
     * Only receive string message from unity.
     */
    addStringMessageListener(listener: (message: string) => void): number;
    /**
     * Only receive json message from unity.
     */
    addUnityMessageListener(listener: (handler: MessageHandler) => void): number;
    /**
     * Remove message listener.
     */
    removeMessageListener(handleId: number): void;
}

let sequence = 0;
function generateId() {
    sequence = sequence + 1;
    return sequence;
}

const waitCallbackMessageMap: {
    [id: number]: UnityViewMessage
} = {};

function handleMessage(message: string) {
    if (MessageHandler.isUnityMessage(message)) {
        const handler = MessageHandler.deserialize(message);
        if (handler.seq === 'end') {
            // handle callback message
            const m = waitCallbackMessageMap[handler.id];
            delete waitCallbackMessageMap[handler.id];
            if (m && m.callBack != null) {
                m.callBack(handler.data);
            }
            return;
        } else {
            return handler;
        }
    } else {
        return message;
    }
}

class UnityModuleImpl implements UnityModule {
    private hid = 0;
    private stringListeners: {
        [hid: number]: (message: string) => void
    }
    private unityMessageListeners: {
        [hid: number]: (message: MessageHandler) => void
    }

    constructor() {
        this.createListeners();
    }

    private createListeners() {
        this.stringListeners = {};
        this.unityMessageListeners = {};
        DeviceEventEmitter.addListener('onUnityMessage', message => {
            const result = handleMessage(message);
            if (result instanceof MessageHandler) {
                Object.values(this.unityMessageListeners).forEach(listener => {
                    listener(result);
                });
            }
            if (typeof result === 'string') {
                Object.values(this.stringListeners).forEach(listener => {
                    listener(result);
                });
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

    public postMessageToUnityManager(message: string | UnityViewMessage) {
        if (typeof message === 'string') {
            this.postMessage('UnityMessageManager', 'onMessage', message);
        } else {
            const id = generateId();
            if (message.callBack) {
                waitCallbackMessageMap[id] = message;
            }
            this.postMessage('UnityMessageManager', 'onRNMessage', UnityMessagePrefix + JSON.stringify({
                id: id,
                seq: message.callBack ? 'start' : '',
                name: message.name,
                data: message.data
            }));
        }
    }

    public postMessage(gameObject: string, methodName: string, message: string) {
        UnityNativeModule.postMessage(gameObject, methodName, message);
    }

    public pause() {
        UnityNativeModule.pause();
    }

    public resume() {
        UnityNativeModule.resume();
    }

    public addMessageListener(listener: (handler: string | MessageHandler) => void) {
        const id = this.getHandleId();
        this.stringListeners[id] = listener;
        this.unityMessageListeners[id] = listener;
        return id;
    }

    public addStringMessageListener(listener: (message: string) => void) {
        const id = this.getHandleId();
        this.stringListeners[id] = listener;
        return id;
    }

    public addUnityMessageListener(listener: (handler: MessageHandler) => void) {
        const id = this.getHandleId();
        this.unityMessageListeners[id] = listener;
        return id;
    }

    public removeMessageListener(handleId: number) {
        if (this.unityMessageListeners[handleId]) {
            delete this.unityMessageListeners[handleId];
        }
        if (this.stringListeners[handleId]) {
            delete this.stringListeners[handleId];
        }
    }
}

export const UnityModule: UnityModule = new UnityModuleImpl();