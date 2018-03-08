package com.reactnative.unity.view;

import android.app.Activity;
import android.content.Context;
import android.graphics.PixelFormat;
import android.os.Build;
import android.view.ViewGroup;
import android.view.WindowManager;

import com.unity3d.player.UnityPlayer;

import java.util.concurrent.CopyOnWriteArraySet;

import static android.view.ViewGroup.LayoutParams.MATCH_PARENT;

/**
 * Created by xzper on 2018-03-08.
 */

public class UnityUtils {
    private static UnityPlayer unityPlayer;

    private static final CopyOnWriteArraySet<UnityEventListener> mUnityEventListeners =
            new CopyOnWriteArraySet<>();

    public static UnityPlayer getPlayer() {
        return unityPlayer;
    }

    public static boolean hasUnityPlayer() {
        return unityPlayer != null;
    }

    public static void createPlayer(Context context) {
        if (unityPlayer != null) {
            return;
        }
        final Activity activity = ((Activity)context);
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                activity.getWindow().setFormat(PixelFormat.RGBA_8888);
                int flag = activity.getWindow().getAttributes().flags;
                boolean fullScreen = false;
                if((flag & WindowManager.LayoutParams.FLAG_FULLSCREEN) == WindowManager.LayoutParams.FLAG_FULLSCREEN) {
                    fullScreen = true;
                }

                unityPlayer = new UnityPlayer(activity);

                // start unity
                addUnityViewToBackground();
                try {
                    // wait a moument. fix unity cannot start when startup.
                    Thread.sleep( 100 );
                    unityPlayer.windowFocusChanged(true);
                    unityPlayer.requestFocus();
                    unityPlayer.resume();
                } catch (Exception e) {
                }

                // restore window layout
                if (!fullScreen) {
                    activity.getWindow().addFlags(WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN);
                    activity.getWindow().clearFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
                }
            }
        });
    }

    public static void postMessage(String gameObject, String methodName, String message) {
        UnityPlayer.UnitySendMessage(gameObject, methodName, message);
    }

    /**
     * Invoke by unity C#
     */
    public static void onUnityMessage(String message) {
        for (UnityEventListener listener : mUnityEventListeners) {
            try {
                listener.onMessage(message);
            } catch (Exception e) {
            }
        }
    }

    public static void addUnityEventListener(UnityEventListener listener) {
        mUnityEventListeners.add(listener);
    }

    public static void removeUnityEventListener(UnityEventListener listener) {
        mUnityEventListeners.remove(listener);
    }

    public static void addUnityViewToBackground() {
        if (unityPlayer.getParent() != null) {
            ((ViewGroup)unityPlayer.getParent()).removeView(unityPlayer);
        }
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
            unityPlayer.setZ(-1f);
        }
        final Activity activity = ((Activity)unityPlayer.getContext());
        ViewGroup.LayoutParams layoutParams = new ViewGroup.LayoutParams(1, 1);
        activity.addContentView(unityPlayer, layoutParams);
    }

    public static void addUnityViewToGroup(ViewGroup group) {
        if (unityPlayer.getParent() != null) {
            ((ViewGroup)unityPlayer.getParent()).removeView(unityPlayer);
        }
        group.addView(unityPlayer, MATCH_PARENT, MATCH_PARENT);
        unityPlayer.windowFocusChanged(true);
        unityPlayer.requestFocus();
        unityPlayer.resume();
    }
}
