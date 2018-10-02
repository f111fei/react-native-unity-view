package com.reactnative.unity.view;

import com.facebook.react.ReactPackage;
import com.facebook.react.bridge.NativeModule;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.uimanager.ViewManager;

import java.util.ArrayList;
import java.util.Collections;
import java.util.List;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewPackage implements ReactPackage {

    private final boolean startUnity;

    public UnityViewPackage() {
        this(true);
    }

    public UnityViewPackage(boolean startUnity) {
        super();
        this.startUnity = startUnity;
    }

    @Override
    public List<ViewManager> createViewManagers(ReactApplicationContext reactContext) {
        List<ViewManager> viewManagers = new ArrayList<>();
        if (startUnity) {
            viewManagers.add(new UnityViewManager(reactContext));
        }
        return viewManagers;
    }

    @Override
    public List<NativeModule> createNativeModules(ReactApplicationContext reactContext) {
        return Collections.emptyList();
    }
}
