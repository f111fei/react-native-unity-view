package com.reactnative.unity.view;

import android.content.Context;
import android.content.res.Configuration;
import android.widget.FrameLayout;

import com.facebook.react.bridge.Arguments;
import com.facebook.react.bridge.ReactContext;
import com.facebook.react.bridge.WritableMap;
import com.facebook.react.uimanager.events.RCTEventEmitter;
import com.unity3d.player.UnityPlayer;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityView extends FrameLayout implements UnityEventListener {

    private UnityPlayer view;

    protected UnityView(Context context, UnityPlayer view) {
        super(context);
        this.view = view;
    }

    @Override
    protected void onAttachedToWindow() {
        super.onAttachedToWindow();
        UnityUtils.addUnityViewToGroup(this);
    }

    @Override
    public void onWindowFocusChanged(boolean hasWindowFocus) {
        super.onWindowFocusChanged(hasWindowFocus);
        view.windowFocusChanged(hasWindowFocus);
    }

    @Override
    protected void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        view.configurationChanged(newConfig);
    }

    @Override
    protected void onDetachedFromWindow() {
        UnityUtils.addUnityViewToBackground();
        super.onDetachedFromWindow();
    }

    @Override
    public void onMessage(String message) {
        ReactContext reactContext = (ReactContext) getContext();
        WritableMap data = Arguments.createMap();
        data.putString("message", message);
        reactContext.getJSModule(RCTEventEmitter.class).receiveEvent(
                getId(),
                "unityMessage",
                data
        );
    }
}
