package com.reactnative.unity.view;

import android.app.Activity;
import android.content.Context;
import android.content.res.Configuration;
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

    private static UnityPlayer getPlayer(Context context) {
        if (unityPlayer == null) {
            final Activity activity = ((Activity)context);
            int flag = activity.getWindow().getAttributes().flags;
            boolean fullScreen = false;
            if((flag & WindowManager.LayoutParams.FLAG_FULLSCREEN) == WindowManager.LayoutParams.FLAG_FULLSCREEN) {
                fullScreen = true;
            }
            unityPlayer = new UnityPlayer(context);
            // restore window layout
            if (!fullScreen) {
                activity.getWindow().addFlags(WindowManager.LayoutParams.FLAG_FORCE_NOT_FULLSCREEN);
                activity.getWindow().clearFlags(WindowManager.LayoutParams.FLAG_FULLSCREEN);
            }
        }
        return unityPlayer;
    }

    private UnityPlayer view;

    protected UnityView(Context context) {
        super(context);
        this.view = getPlayer(context);
    }

    public UnityPlayer getPlayer() {
        return view;
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
        view.pause();
        removeView(view);
        super.onDetachedFromWindow();
    }
}
