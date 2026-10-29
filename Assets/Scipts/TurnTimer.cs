using System;
using UnityEngine;

public class TurnTimer : MonoBehaviour
{
    [Min(1f)]
    public float allowedTime = 15f;
    public float RemainingTime { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action<float> TimeChanged;
    public event Action TimedOut;

    public void StartTimer(float duration = -1f)
    {
        RemainingTime = duration > 0f ? duration : allowedTime;
        IsRunning = true;
        TimeChanged?.Invoke(RemainingTime);
    }

    public void StopTimer()
    {
        IsRunning = false;
    }

    private void Update()
    {
        if (!IsRunning) { return; }

        RemainingTime = Mathf.Max(0f, RemainingTime - Time.deltaTime);
        TimeChanged?.Invoke(RemainingTime);
        if (RemainingTime <= 0f)
        {
            IsRunning = false;
            TimedOut?.Invoke();
        }
    }
}
