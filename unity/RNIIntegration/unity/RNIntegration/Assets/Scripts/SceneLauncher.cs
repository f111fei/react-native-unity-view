using ReactNative;
using System;
using UnityEngine;

public class SceneLauncher : MonoBehaviour
{
    private IDisposable subscription;
    private Color backgroundColor = Color.grey;

    void Awake()
    {
        UnityMessageManager.Instance.Send("launcher", new JsonObject { ["state"] = "awake" });
    }

    void OnEnable()
    {
        UnityMessageManager.Instance.OnMessage += this.OnReceiveMessage;
        this.subscription = UnityMessageManager.Instance.Subscribe("launcher", this.OnReceiveMessage);
        UnityMessageManager.Instance.Send("launcher", new JsonObject { ["state"] = "enable" });
    }

    void Start()
    {
        UnityMessageManager.Instance.Send("launcher", new JsonObject { ["state"] = "start" });
    }

    void OnDisable()
    {
        UnityMessageManager.Instance.Send("launcher", new JsonObject { ["state"] = "disable" });
        UnityMessageManager.Instance.OnMessage -= this.OnReceiveMessage;
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

    private void OnReceiveMessage(UnityMessageHandler handler)
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
