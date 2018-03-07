# react-native-unity-view

Integrate unity3d within a React Native app. Add a react native component to show unity. Works on both iOS and Android.

## Example

See [react-native-unity-demo](https://github.com/f111fei/react-native-unity-demo)

## Preview

![gif](https://user-images.githubusercontent.com/7069719/35962773-9623cf56-0ced-11e8-94aa-b93a35a39800.gif)

## How to use

### Install

    npm install react-native-unity-view --save
    react-native link react-native-unity-view

### Add Unity Project

1. Create an unity project, Example: 'Cube'.
2. Create a folder named `unity` in react native project folder.
2. Move unity project folder to `unity` folder.

Now your project files should look like this.

    .
    ├── android
    ├── ios
    ├── unity
    │   └── <Your Unity Project>    // Example: Cube
    ├── node_modules
    ├── package.json
    ├── README.md

### Configure Player Settings

1. First Open Unity Project.

2. Click Menu: File => Build Settings => Player Settings

3. Change `Product Name` to Name of the Xcode project, You can find it follow `ios/${XcodeProjectName}.xcodeproj`.

**IOS Platform**:

Other Settings find the Rendering part, uncheck the `Auto Graphics API` and select only `OpenGLES2`.

### Add Unity Build Scripts and Export

Copy [`Build.cs`](https://github.com/f111fei/react-native-unity-demo/blob/master/unity/Cube/Assets/Scripts/Editor/Build.cs) and [`XCodePostBuild.cs`](https://github.com/f111fei/react-native-unity-demo/blob/master/unity/Cube/Assets/Scripts/Editor/XCodePostBuild.cs) to `unity/<Your Unity Project>/Assets/Scripts/Editor/`

Open your unity project in Unity Editor. Now you can export unity project with `Build/Export Android` or `Build/Export IOS` menu.

![image](https://user-images.githubusercontent.com/7069719/37091489-5417a66c-2243-11e8-8946-4d9e1ac652e8.png)

Android will export unity project to `android/UnityExport`.

IOS will export unity project to `ios/UnityExport`.

### Configure Native Build

#### Android Build

Make alterations to the following files:

- `android/settings.gradle`

```
...
include ":UnityExport"
project(":UnityExport").projectDir = file("./UnityExport")
```

- `android/build.gradle`

```
allprojects {
    repositories {
        ...
        flatDir {
            dirs project(':UnityExport').file('libs')
        }
    }
}
```

- `android/app/build.gradle`

```
dependencies {
    ...
    compile project(':UnityExport')
}
```

#### IOS Build

1. Open your react native project in XCode.

1. Copy File [`UnityConfig.xcconfig`](https://github.com/f111fei/react-native-unity-demo/blob/master/ios/rnunitydemo/UnityConfig.xcconfig) to `ios/${XcodeProjectName}/`.

2. Drag `UnityConfig.xcconfig` to XCode. Choose `Create folder references`.

3. Setting `.xcconfig` to project.

![image](https://user-images.githubusercontent.com/7069719/37093471-638b7810-224a-11e8-8263-b9882f707c15.png)

### Use In React Native

```
import React from 'react';
import { StyleSheet, Image, View, Dimensions } from 'react-native';
import UnityView from 'react-native-unity-view';

export default class App extends React.Component<Props, State> {
    render() {
    return (
      <View style={styles.container}>
        <UnityView style={{ position: 'absolute', left: 0, right: 0, top: 0, bottom: 0, }} /> : null}
        <Text style={styles.welcome}>
          Welcome to React Native!
        </Text>
      </View>
    );
  }
}
```

Enjoy!!!

