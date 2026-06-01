using System.Collections;
using Mirror;
using Mirror.Examples.Common;
using TMPro;
using UnityEngine;

public class PingUI : MonoBehaviour
{
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private TMP_Text fpsText;

    private int _lastFrameCount;
    private float _lastTime;
    private int _fps;

    private void FixedUpdate()
    {
        float elapsed = Time.unscaledTime - _lastTime;
        if (elapsed >= 1f)
        {
            _fps = Mathf.RoundToInt((Time.frameCount - _lastFrameCount) / elapsed);
            _lastFrameCount = Time.frameCount;
            _lastTime = Time.unscaledTime;
        }

        if (NetworkClient.isConnected)
        {
            int ms = (int)(NetworkTime.rtt * 1000);
            pingText.text = $"ping: {ms} ms";
            fpsText.text = $"fps: {_fps}";
        }
        else
        {
            pingText.text = $"";
            fpsText.text = $"fps: {_fps}";
        }
    }
}
