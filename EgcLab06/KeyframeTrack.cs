using OpenTK.Mathematics;

namespace Egc;

public sealed class KeyframeTrack
{
    private readonly List<Vector3> _keys = new();

    public float SecondsPerKey { get; }

    public EasingFunction Ease = Easing.Linear;

    public KeyframeTrack(float secondsPerKey)
    {
        SecondsPerKey = MathF.Max(secondsPerKey, 0.001f);
    }

    public int Count => _keys.Count;
    public IReadOnlyList<Vector3> Keys => _keys;

    public float Duration => _keys.Count * SecondsPerKey;

    public KeyframeTrack Add(Vector3 value)
    {
        _keys.Add(value);
        return this;
    }

    public void Clear() => _keys.Clear();

    public Vector3 Sample(float time)
    {
        if (_keys.Count == 0) return Vector3.Zero;
        if (_keys.Count == 1) return _keys[0];

        float position = time / SecondsPerKey % _keys.Count;
        if (position < 0.0f) position += _keys.Count;

        int i = Math.Min((int)MathF.Floor(position), _keys.Count - 1);
        float u = position - i;
        int next = (i + 1) % _keys.Count;

        return Vector3.Lerp(_keys[i], _keys[next], Ease(u));
    }
}
