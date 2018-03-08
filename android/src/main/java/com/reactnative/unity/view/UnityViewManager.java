package com.reactnative.unity.view;

import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.uimanager.SimpleViewManager;
import com.facebook.react.uimanager.ThemedReactContext;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewManager extends SimpleViewManager<UnityView> implements LifecycleEventListener {
    private static final String REACT_CLASS = "UnityView";

    private ReactApplicationContext context;

    UnityViewManager(ReactApplicationContext context) {
        super();
        this.context = context;
        context.addLifecycleEventListener(this);
    }

    @Override
    public String getName() {
        return REACT_CLASS;
    }

    @Override
    protected UnityView createViewInstance(ThemedReactContext reactContext) {
        UnityView view = new UnityView(reactContext, UnityUtils.getPlayer());
        return view;
    }

    @Override
    public void onHostResume() {
        if (!UnityUtils.hasUnityPlayer()) {
            UnityUtils.createPlayer(context.getCurrentActivity());
        } else {
            UnityUtils.getPlayer().resume();
        }
    }

    @Override
    public void onHostPause() {
        UnityUtils.getPlayer().pause();
    }

    @Override
    public void onHostDestroy() {
        UnityUtils.getPlayer().quit();
    }
}
