package com.reactnative.unity.view;

import android.os.Handler;
import android.view.View;

import com.facebook.react.bridge.LifecycleEventListener;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.bridge.ReadableArray;
import com.facebook.react.common.MapBuilder;
import com.facebook.react.uimanager.SimpleViewManager;
import com.facebook.react.uimanager.ThemedReactContext;
import com.unity3d.player.UnityPlayer;

import java.util.Map;

import javax.annotation.Nullable;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityViewManager extends SimpleViewManager<UnityView> implements LifecycleEventListener, View.OnAttachStateChangeListener {
    private static final String REACT_CLASS = "UnityView";

    public static final int COMMAND_POST_MESSAGE = 1;
    public static final int COMMAND_PAUSE = 2;
    public static final int COMMAND_RESUME = 3;

    private static boolean DONOT_RESUME = false;

    private ReactApplicationContext context;
    private UnityPlayer player;

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
    public @Nullable Map<String, Integer> getCommandsMap() {
        return MapBuilder.of(
                "postMessage", COMMAND_POST_MESSAGE,
                "pause", COMMAND_PAUSE,
                "resume", COMMAND_RESUME
        );
    }

    @Override
    public void receiveCommand(UnityView root, int commandId, @Nullable ReadableArray args) {
        if (player == null) {
            return;
        }
        switch (commandId) {
            case COMMAND_POST_MESSAGE:
                String gameObject = args.getString(0);
                String methodName = args.getString(1);
                String message = args.getString(2);
                UnityUtils.postMessage(gameObject, methodName, message);
                break;
            case COMMAND_PAUSE:
                player.pause();
                DONOT_RESUME = true;
                break;
            case COMMAND_RESUME:
                player.resume();
                DONOT_RESUME = false;
                break;                
        }
    }

    @Override
    protected UnityView createViewInstance(ThemedReactContext reactContext) {
        final UnityView view = new UnityView(reactContext);
        UnityUtils.addUnityEventListener(view);
        view.addOnAttachStateChangeListener(this);

        if (player != null) {
            view.setUnityPlayer(player);
        } else {
            UnityUtils.createPlayer(context.getCurrentActivity(), new UnityUtils.CreateCallback() {
                @Override
                public void onReady() {
                    player = UnityUtils.getPlayer();
                    view.setUnityPlayer(player);
                }
            });
        }
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
                "unityMessage", MapBuilder.of("registrationName", "onMessage")
        );
    }

    @Override
    public void onHostResume() {
        if (!DONOT_RESUME && player != null) {
            player.resume();
        }
    }

    @Override
    public void onHostPause() {
        if (player != null) {
            player.pause();
        }
    }

    @Override
    public void onHostDestroy() {
        if (player != null) {
            player.quit();
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
                if (player != null) {
                    player.pause();
                }
                }
            }, 300); //TODO: 300 is the right one?
        }
    }

    @Override
    public void onViewDetachedFromWindow(View v) {

    }
}
