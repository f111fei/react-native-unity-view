import { UnityMessageType } from "./UnityMessage";

export interface IUnityRequest<TType extends number = UnityMessageType, TResponse = any, TData = any> {
    readonly id: string;
    readonly type: TType;
    readonly data?: TData;
}

export class UnityRequest<TType extends number = UnityMessageType, TResponse = any, TData = any> implements IUnityRequest<TType, TResponse, TData> {
    private m_id: string;
    private m_type: TType;
    private m_data?: TData;

    public constructor(id: string);
    public constructor(id: string, data: TData);
    public constructor(id: string, type: TType, data: TData)
    constructor(id: string, second?: any, third?: any) {
        this.m_id = id;
        if (typeof second === 'number') {
            this.m_type = <TType>second;
            this.m_data = third;
        } else {
            this.m_data = second;
        }

        if (typeof second === 'number') {
            this.m_type = third;
        } else {
            this.m_type = <TType>UnityMessageType.Request;
        }
    }

    public get id(): string {
        return this.m_id;
    }
    public get type(): TType {
        return this.m_type;
    }
    public get data(): TData {
        return this.m_data;
    }
}
