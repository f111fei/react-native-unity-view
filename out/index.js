"use strict";
var __extends = (this && this.__extends) || (function () {
    var extendStatics = Object.setPrototypeOf ||
        ({ __proto__: [] } instanceof Array && function (d, b) { d.__proto__ = b; }) ||
        function (d, b) { for (var p in b) if (b.hasOwnProperty(p)) d[p] = b[p]; };
    return function (d, b) {
        extendStatics(d, b);
        function __() { this.constructor = d; }
        d.prototype = b === null ? Object.create(b) : (__.prototype = b.prototype, new __());
    };
})();
var __assign = (this && this.__assign) || Object.assign || function(t) {
    for (var s, i = 1, n = arguments.length; i < n; i++) {
        s = arguments[i];
        for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p))
            t[p] = s[p];
    }
    return t;
};
var __rest = (this && this.__rest) || function (s, e) {
    var t = {};
    for (var p in s) if (Object.prototype.hasOwnProperty.call(s, p) && e.indexOf(p) < 0)
        t[p] = s[p];
    if (s != null && typeof Object.getOwnPropertySymbols === "function")
        for (var i = 0, p = Object.getOwnPropertySymbols(s); i < p.length; i++) if (e.indexOf(p[i]) < 0)
            t[p[i]] = s[p[i]];
    return t;
};
exports.__esModule = true;
var React = require("react");
var react_native_1 = require("react-native");
var PropTypes = require("prop-types");
// import { ViewPropTypes } from "@types/react-native";
var UIManager = react_native_1.NativeModules.UIManager;
var UNITY_VIEW_REF = "unityview";
var sequence = 0;
function generateId() {
    sequence = sequence + 1;
    return sequence;
}
var waitCallbackMessageMap = {};
var messagePrefix = "@UnityMessage@";
var MessageHandler = /** @class */ (function () {
    function MessageHandler(viewHandler) {
        this.viewHandler = viewHandler;
    }
    MessageHandler.deserialize = function (viewHandler, message) {
        var m = JSON.parse(message);
        var handler = new MessageHandler(viewHandler);
        handler.id = m.id;
        handler.seq = m.seq;
        handler.name = m.name;
        handler.data = m.data;
        return handler;
    };
    MessageHandler.prototype.send = function (data) {
        UIManager.dispatchViewManagerCommand(this.viewHandler, UIManager.UnityView.Commands.postMessage, [
            "UnityMessageManager",
            "onUnityMessage",
            messagePrefix +
                JSON.stringify({
                    id: this.id,
                    seq: "end",
                    name: this.name,
                    data: data
                })
        ]);
    };
    return MessageHandler;
}());
exports.MessageHandler = MessageHandler;
var UnityView = /** @class */ (function (_super) {
    __extends(UnityView, _super);
    function UnityView() {
        return _super !== null && _super.apply(this, arguments) || this;
    }
    /**
     * Send Message to Unity.
     * @param gameObject The Name of GameObject. Also can be a path string.
     * @param methodName Method name in GameObject instance.
     * @param message The message will post.
     */
    UnityView.prototype.postMessage = function (gameObject, methodName, message) {
        UIManager.dispatchViewManagerCommand(this.getViewHandle(), UIManager.UnityView.Commands.postMessage, [String(gameObject), String(methodName), String(message)]);
    };
    /**
     * Pause the unity player
     */
    UnityView.prototype.pause = function () {
        UIManager.dispatchViewManagerCommand(this.getViewHandle(), UIManager.UnityView.Commands.pause, []);
    };
    /**
     * Resume the unity player
     */
    UnityView.prototype.resume = function () {
        UIManager.dispatchViewManagerCommand(this.getViewHandle(), UIManager.UnityView.Commands.resume, []);
    };
    /**
     * Send Message to UnityMessageManager.
     * @param message The message will post.
     */
    UnityView.prototype.postMessageToUnityManager = function (message) {
        if (typeof message === "string") {
            this.postMessage("UnityMessageManager", "onMessage", message);
        }
        else {
            var id = generateId();
            if (message.callBack) {
                waitCallbackMessageMap[id] = message;
            }
            this.postMessage("UnityMessageManager", "onRNMessage", messagePrefix +
                JSON.stringify({
                    id: id,
                    seq: message.callBack ? "start" : "",
                    name: message.name,
                    data: message.data
                }));
        }
    };
    UnityView.prototype.getViewHandle = function () {
        return react_native_1.findNodeHandle(this.refs[UNITY_VIEW_REF]);
    };
    UnityView.prototype.onMessage = function (event) {
        var message = event.nativeEvent.message;
        if (message.startsWith(messagePrefix)) {
            message = message.replace(messagePrefix, "");
            var handler = MessageHandler.deserialize(this.getViewHandle(), message);
            if (handler.seq === "end") {
                // handle callback message
                var m = waitCallbackMessageMap[handler.id];
                delete waitCallbackMessageMap[handler.id];
                if (m && m.callBack != null) {
                    m.callBack(handler.data);
                }
                return;
            }
            if (this.props.onUnityMessage) {
                this.props.onUnityMessage(handler);
            }
        }
        else {
            if (this.props.onMessage) {
                this.props.onMessage(message);
            }
        }
    };
    UnityView.prototype.render = function () {
        var props = __rest(this.props, []);
        return (React.createElement(NativeUnityView, __assign({ ref: UNITY_VIEW_REF }, props, { onMessage: this.onMessage.bind(this) })));
    };
    UnityView.propTypes = __assign({}, react_native_1.ViewPropTypes, { onMessage: PropTypes.func });
    return UnityView;
}(React.Component));
exports["default"] = UnityView;
var NativeUnityView = react_native_1.requireNativeComponent("UnityView", UnityView);
//# sourceMappingURL=index.js.map