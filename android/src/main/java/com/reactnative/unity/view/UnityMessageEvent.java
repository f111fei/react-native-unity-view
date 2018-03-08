package com.reactnative.unity.view;

import com.facebook.react.bridge.Arguments;
import com.facebook.react.bridge.WritableMap;
import com.facebook.react.uimanager.events.Event;
import com.facebook.react.uimanager.events.RCTEventEmitter;

/**
 * Created by xzper on 2018-03-08.
 */

public class UnityMessageEvent extends Event<UnityMessageEvent> {

    public static final String EVENT_NAME = "unityMessage";
    private final String mData;

    public UnityMessageEvent(int viewId, String data) {
        super(viewId);
        mData = data;
    }

    @Override
    public String getEventName() {
        return EVENT_NAME;
    }

    @Override
    public void dispatch(RCTEventEmitter rctEventEmitter) {
        WritableMap data = Arguments.createMap();
        data.putString("message", mData);
        rctEventEmitter.receiveEvent(getViewTag(), EVENT_NAME, data);
    }
}
