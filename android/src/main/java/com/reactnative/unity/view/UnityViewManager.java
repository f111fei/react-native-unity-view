package com.reactnative.unity.view;

import android.os.Handler;
import android.view.View;

import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReadableArray;
import com.facebook.react.common.MapBuilder;
import com.facebook.react.uimanager.ViewGroupManager;
import com.facebook.react.uimanager.ThemedReactContext;
import com.unity3d.player.UnityPlayer;
import com.google.ar.core.ArCoreApk;
import java.util.Map;

import javax.annotation.Nullable;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewManager extends ViewGroupManager<UnityView> implements LifecycleEventListener, View.OnAttachStateChangeListener {
    private static final String REACT_CLASS = "UnityView";

    public static final int COMMAND_POST_MESSAGE = 1;
    public static final int COMMAND_PAUSE = 2;
    public static final int COMMAND_RESUME = 3;

    private static boolean DONOT_RESUME = false;
    boolean isARCoreSupported;

    private ReactApplicationContext context;

    UnityViewManager(ReactApplicationContext context) {
        super();
        this.context = context;
        this.isARCoreSupported = ArCoreApk.getInstance().checkAvailability(context).isSupported();
        context.addLifecycleEventListener(this);
    }

    @Override
    public String getName() {
        return REACT_CLASS;
    }

    @Override
    public @Nullable Map<String, Integer> getCommandsMap() {
        return MapBuilder.of(
                "postMessage", COMMAND_POST_MESSAGE,
                "pause", COMMAND_PAUSE,
                "resume", COMMAND_RESUME
        );
    }

    @Override
    public void receiveCommand(UnityView root, int commandId, @Nullable ReadableArray args) {
        switch (commandId) {
            case COMMAND_POST_MESSAGE:
                String gameObject = args.getString(0);
                String methodName = args.getString(1);
                String message = args.getString(2);
                UnityUtils.postMessage(gameObject, methodName, message);
                break;
            case COMMAND_PAUSE:
                UnityUtils.getPlayer().pause();
                DONOT_RESUME = true;
                break;
            case COMMAND_RESUME:
                UnityUtils.getPlayer().resume();
                DONOT_RESUME = false;
                break;
        }
    }

    @Override
    protected UnityView createViewInstance(ThemedReactContext reactContext) {
        UnityView view = new UnityView(reactContext, UnityUtils.getPlayer());
        UnityUtils.addUnityEventListener(view);
        view.addOnAttachStateChangeListener(this);
        return view;
    }

    @Override
    public void onDropViewInstance(UnityView view) {
        UnityUtils.removeUnityEventListener(view);
        view.removeOnAttachStateChangeListener(this);
        super.onDropViewInstance(view);
    }

    @Override
    public @Nullable Map getExportedCustomDirectEventTypeConstants() {
        return MapBuilder.of(
                UnityMessageEvent.EVENT_NAME, MapBuilder.of("registrationName", "onMessage")
        );
    }

    @Override
    public void onHostResume() {
        if (this.isARCoreSupported) {
            if (!UnityUtils.hasUnityPlayer()) {
                UnityUtils.createPlayer(context.getCurrentActivity());
            } else {
                if (!DONOT_RESUME) {
                    UnityUtils.getPlayer().resume();
                }
            }
        }
    }

    @Override
    public void onHostPause() {
        if (this.isARCoreSupported) {
            UnityUtils.getPlayer().pause();
        }
    }

    @Override
    public void onHostDestroy() {
        if (this.isARCoreSupported) {
            UnityUtils.getPlayer().quit();
        }
    }

    @Override
    public void onViewAttachedToWindow(View v) {
        // restore the unity player state
        if (DONOT_RESUME) {
            Handler handler = new Handler();
            handler.postDelayed(new Runnable() {
                @Override
                public void run() {
                    UnityUtils.getPlayer().pause();
                }
            }, 300); //TODO: 300 is the right one?
        }
    }

    @Override
    public void onViewDetachedFromWindow(View v) {

    }
}
