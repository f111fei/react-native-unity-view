import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules } from 'react-native';
import * as ViewPropTypes from 'react-native/Libraries/Components/View/ViewPropTypes';

const { UIManager } = NativeModules;

const UNITY_VIEW_REF = 'unityview';

export interface UnityViewProps extends ViewProperties {
}

export default class UnityView extends React.Component<UnityViewProps> {
    public static propTypes = {
        ...ViewPropTypes
    }

    public postMessage(gameObject: string, method: string, message: string) {
        UIManager.dispatchViewManagerCommand(
            this.getViewHandle(),
            UIManager.UnityView.Commands.postMessage,
            [String(gameObject), String(method), String(message)]
        );
    };

    private getViewHandle() {
        return findNodeHandle(this.refs[UNITY_VIEW_REF] as any);
    }

    public render() {
        const { ...props } = this.props;
        return (
            <NativeUnityView ref={UNITY_VIEW_REF} {...props}>
            </NativeUnityView>
        );
    }
}

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView', UnityView);