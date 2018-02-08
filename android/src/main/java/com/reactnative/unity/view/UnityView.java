package com.reactnative.unity.view;

import android.content.Context;
import android.content.res.Configuration;
import android.view.View;

import com.unity3d.player.UnityPlayer;

/**
 * Created by xzper on 2018-02-07.
 */

public class UnityView extends UnityPlayer {

    protected UnityView(Context context) {
        super(context);
    }

    @Override
    protected void onAttachedToWindow() {
        super.onAttachedToWindow();
        this.resume();
    }

    @Override
    public void onWindowFocusChanged(boolean hasWindowFocus) {
        super.onWindowFocusChanged(hasWindowFocus);
        this.windowFocusChanged(hasWindowFocus);
    }

    @Override
    protected void onConfigurationChanged(Configuration newConfig) {
        super.onConfigurationChanged(newConfig);
        this.configurationChanged(newConfig);
    }

    @Override
    protected void onDetachedFromWindow() {
        super.onDetachedFromWindow();
    }
}
