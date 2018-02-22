package com.reactnative.unity.view;

import android.app.Activity;
import android.content.Intent;
import android.graphics.PixelFormat;
import android.os.Looper;
import android.view.ViewGroup;

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

    private UnityView view;

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
                if (view != null) {
                    view.getPlayer().resume();
                }
            }

            @Override
            public void onHostPause() {
                if (view != null) {
                    view.getPlayer().pause();
                }
            }

            @Override
            public void onHostDestroy() {
                if (view != null) {
                    view.getPlayer().quit();
                }
            }
        });

        context.addActivityEventListener(new ActivityEventListener() {
            @Override
            public void onActivityResult(Activity activity, int requestCode, int resultCode, Intent data) {
            }

            @Override
            public void onNewIntent(Intent intent) {
                context.getCurrentActivity().getWindow().setFormat(PixelFormat.RGBA_8888);
            }
        });
    }

    @Override
    protected UnityView createViewInstance(ThemedReactContext reactContext) {
        Activity activity = reactContext.getCurrentActivity();
        view = new UnityView(activity);
        return view;
    }
}
