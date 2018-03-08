import * as React from "react";
import { requireNativeComponent, ViewProperties, findNodeHandle, NativeModules, NativeSyntheticEvent } from 'react-native';
import * as PropTypes from "prop-types";
import * as ViewPropTypes from 'react-native/Libraries/Components/View/ViewPropTypes';

const { UIManager } = NativeModules;

const UNITY_VIEW_REF = 'unityview';

export interface UnityViewMessageEventData {
    message: string;
}

export interface UnityViewProps extends ViewProperties {
    onMessage?: (event: NativeSyntheticEvent<UnityViewMessageEventData>) => void;
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

    private getViewHandle() {
        return findNodeHandle(this.refs[UNITY_VIEW_REF] as any);
    }

    private onMessage(event: NativeSyntheticEvent<UnityViewMessageEventData>) {
        if (this.props.onMessage) {
            this.props.onMessage(event);
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