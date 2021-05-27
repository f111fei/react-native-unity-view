export enum UnityMessageType {
    Default = 0,
    Response = 1,
    Error = 2,
    Cancel = 3,
    Canceled = 4,
    Request = 9,
}

export const UnityMessagePrefix = '@UnityMessage@';

export interface IUnityMessage<TType extends number = UnityMessageType, TData = any> {
    readonly id: string;
    readonly type: TType;
    readonly data?: TData;
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

export class UnityMessageImpl implements UnityMessage {
    private m_json: UnityMessage;

    constructor(message: string) {
        message = message.replace(UnityMessagePrefix, '');
        this.m_json = JSON.parse(message) as UnityMessage;;
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
        return this.uuid !== undefined && (this.type === UnityMessageType.Response || this.type === UnityMessageType.Canceled || this.type === UnityMessageType.Error);
    }

    public isResponse(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Response;
    }

    public isCancel(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Cancel;
    }

    public isCanceled(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Canceled;
    }

    public isError(): boolean {
        return this.uuid !== undefined && this.type === UnityMessageType.Error;
    }

    public static isUnityMessage(message: string) {
        if (message.startsWith(UnityMessagePrefix)) {
            return true;
        } else {
            return false;
        }
    }
}
