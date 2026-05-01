using UnityEngine;

public class Cooldown
{
    private readonly float _duration;
    private float _lastReset;

    public Cooldown(float duration)
    {
        _duration = duration;
        _lastReset = -duration;
    }

    public void Reset()
    {
        _lastReset = Time.time;
    }

    public bool Over()
    {
        return Time.time - _lastReset >= _duration;
    }

    public float Progress()
    {
        return (Time.time - _lastReset) / _duration;
    }
}