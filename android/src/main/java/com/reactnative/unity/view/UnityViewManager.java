package com.reactnative.unity.view;

import android.app.Activity;
import android.content.Intent;
import android.graphics.PixelFormat;

import com.facebook.react.bridge.ActivityEventListener;
import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.uimanager.SimpleViewManager;
import com.facebook.react.uimanager.ThemedReactContext;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewManager extends SimpleViewManager<UnityView> {
    private static final String REACT_CLASS = "UnityView";

    private ReactApplicationContext context;

    UnityViewManager(ReactApplicationContext context) {
        super();
        this.context = context;
        this.addEventListener();
    }

    @Override
    public String getName() {
        return REACT_CLASS;
    }

    private void addEventListener() {
        context.addLifecycleEventListener(new LifecycleEventListener() {
            @Override
            public void onHostResume() {
                if (UnityView.getPlayer() == null) {
                    UnityView.createPlayer(context.getCurrentActivity());
                } else {
                    UnityView.getPlayer().resume();
                }
            }

            @Override
            public void onHostPause() {
                UnityView.getPlayer().pause();
            }

            @Override
            public void onHostDestroy() {
                UnityView.getPlayer().quit();
            }
        });

        context.addActivityEventListener(new ActivityEventListener() {
            @Override
            public void onActivityResult(Activity activity, int requestCode, int resultCode, Intent data) {
            }

            @Override
            public void onNewIntent(Intent intent) {
            }
        });
    }

    @Override
    protected UnityView createViewInstance(ThemedReactContext reactContext) {
        UnityView view = new UnityView(reactContext);
        return view;
    }
}
