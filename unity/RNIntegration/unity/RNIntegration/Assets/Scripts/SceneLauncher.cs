using Newtonsoft.Json.Linq;
using ReactNative;
using System;
using UnityEngine;

[UnityMessageHandler(MessageID)]
public class SceneLauncher : MonoBehaviour
{
    public const string MessageID = "launcher";

    private IDisposable subscription;
    private Color backgroundColor = Color.grey;

    void Awake()
    {
        UnityMessageManager.Send(MessageID, new JObject { { "state", "awake" } });
    }

    void OnEnable()
    {
        UnityMessageManager.OnMessage += this.OnReceiveMessage;
        this.subscription = UnityMessageManager.Subscribe("launcher", this.OnReceiveMessage);
        UnityMessageManager.Send(MessageID, new JObject { { "state", "enable" } });
    }

    void Start()
    {
        UnityMessageManager.Send(MessageID, new JObject { { "state", "start" } });
    }

    void OnDisable()
    {
        UnityMessageManager.Send(MessageID, new JObject { { "state", "disable" } });
        UnityMessageManager.OnMessage -= this.OnReceiveMessage;
        this.subscription?.Dispose();
        this.subscription = null;
    }

    private void Update()
    {
        Camera.main.backgroundColor = this.backgroundColor;
    }

    private void OnReceiveMessage(string message)
    {
        if (this.backgroundColor != Color.green)
        {
            this.backgroundColor = Color.green;
        }
        else
        {
            this.backgroundColor = Color.red;
        }
    }

    private void OnReceiveMessage(IUnityMessageHandler handler)
    {
        if (this.backgroundColor != Color.grey)
        {
            this.backgroundColor = Color.grey;
        }
        else
        {
            this.backgroundColor = Color.white;
        }
    }
}
