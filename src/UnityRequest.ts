import { UnityMessageType } from "./UnityMessage";

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
