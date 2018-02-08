import * as React from "react";
import { requireNativeComponent, ViewProperties } from 'react-native';
import * as ViewPropTypes from 'react-native/Libraries/Components/View/ViewPropTypes';

export interface UnityViewProps extends ViewProperties {
}

export default class UnityView extends React.Component<UnityViewProps> {
    public static propTypes = {
        ...ViewPropTypes
    }

    public render() {
        const { ...props } = this.props;
        return (
            <NativeUnityView {...props}>
            </NativeUnityView>
        );
    }
}

const NativeUnityView = requireNativeComponent<UnityViewProps>('UnityView', UnityView);