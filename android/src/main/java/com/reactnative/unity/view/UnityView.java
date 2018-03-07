package com.reactnative.unity.view;

import android.app.Activity;
import android.content.Context;
import android.content.res.Configuration;
import android.graphics.PixelFormat;
import android.os.Build;
import android.view.ViewGroup;
import android.view.WindowManager;
import android.widget.FrameLayout;

import com.unity3d.player.UnityPlayer;

import static android.view.ViewGroup.LayoutParams.MATCH_PARENT;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityView extends FrameLayout {

    private static UnityPlayer unityPlayer;

    public static UnityPlayer getPlayer() {
        return unityPlayer;
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
                ViewGroup.LayoutParams layoutParams = new ViewGroup.LayoutParams(1, 1);
                activity.addContentView(unityPlayer, layoutParams);
                if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.LOLLIPOP) {
                    unityPlayer.setZ(-1f);
                }
                try {
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

    private UnityPlayer view;

    protected UnityView(Context context) {
        super(context);
        this.view = getPlayer();
    }

    @Override
    protected void onAttachedToWindow() {
        super.onAttachedToWindow();
        if (view.getParent() != null) {
            ((ViewGroup)view.getParent()).removeView(view);
        }
        addView(view, MATCH_PARENT, MATCH_PARENT);
        view.windowFocusChanged(true);
        view.requestFocus();
        view.resume();
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
        view.windowFocusChanged(false);
        // Don`t pause unity, when unity view is hide.
//        view.pause();
        removeView(view);
        super.onDetachedFromWindow();
    }
}
