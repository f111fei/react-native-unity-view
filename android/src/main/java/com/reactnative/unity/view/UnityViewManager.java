package com.reactnative.unity.view;

import android.os.Handler;
import android.view.View;

import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.common.MapBuilder;
import com.facebook.react.uimanager.SimpleViewManager;
import com.facebook.react.uimanager.ThemedReactContext;

import java.util.Map;

import javax.annotation.Nullable;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewManager extends SimpleViewManager<UnityView> implements LifecycleEventListener, View.OnAttachStateChangeListener {
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
        final UnityView view = new UnityView(reactContext);
        view.addOnAttachStateChangeListener(this);

        if (UnityUtils.getPlayer() != null) {
            view.setUnityPlayer(UnityUtils.getPlayer());
        } else {
            UnityUtils.createPlayer(context.getCurrentActivity(), new UnityUtils.CreateCallback() {
                @Override
                public void onReady() {
                    view.setUnityPlayer(UnityUtils.getPlayer());
                }
            });
        }
        return view;
    }

    @Override
    public void onDropViewInstance(UnityView view) {
        view.removeOnAttachStateChangeListener(this);
        super.onDropViewInstance(view);
    }

    @Override
    public void onHostResume() {
        if (UnityUtils.isUnityReady()) {
            UnityUtils.getPlayer().resume();
            restoreUnityUserState();
        }
    }

    @Override
    public void onHostPause() {
        if (UnityUtils.isUnityReady()) {
            // Don't use UnityUtils.pause()
            UnityUtils.getPlayer().pause();
        }
    }

    @Override
    public void onHostDestroy() {
        if (UnityUtils.isUnityReady()) {
            UnityUtils.getPlayer().quit();
        }
    }

    private void restoreUnityUserState() {
        // restore the unity player state
        if (UnityUtils.isUnityPaused()) {
            Handler handler = new Handler();
            handler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    if (UnityUtils.getPlayer() != null) {
                        UnityUtils.getPlayer().pause();
                    }
                }
            }, 300); //TODO: 300 is the right one?
        }
    }

    @Override
    public void onViewAttachedToWindow(View v) {
        restoreUnityUserState();
    }

    @Override
    public void onViewDetachedFromWindow(View v) {

    }
}
