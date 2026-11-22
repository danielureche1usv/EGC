using OpenTK.Mathematics;

namespace Egc;

public sealed class Tween
{
    public Vector3 From;
    public Vector3 To;
    public float Duration;
    public EasingFunction Ease;
    public bool PingPong;

    public float Elapsed { get; private set; }

    public Tween(Vector3 from, Vector3 to, float duration, EasingFunction ease, bool pingPong = false)
    {
        From = from;
        To = to;
        Duration = MathF.Max(duration, 0.001f);
        Ease = ease;
        PingPong = pingPong;
    }

    public bool IsFinished => !PingPong && Elapsed >= Duration;

    public float Progress
    {
        get
        {
            if (!PingPong)
            {
                return MathF.Min(Elapsed / Duration, 1.0f);
            }
            float p = Elapsed % (2.0f * Duration) / Duration;
            return p <= 1.0f ? p : 2.0f - p;
        }
    }

    public Vector3 Value => Vector3.Lerp(From, To, Ease(Progress));

    public void Update(float dt)
    {
        if (!IsFinished)
        {
            Elapsed += dt;
        }
    }

    public void Restart() => Elapsed = 0.0f;

    public void GoTo(Vector3 to, float duration)
    {
        From = Value;
        To = to;
        Duration = MathF.Max(duration, 0.001f);
        Elapsed = 0.0f;
    }
}
