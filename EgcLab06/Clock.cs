namespace Egc;

public sealed class Clock
{
    public const float MaxDelta = 0.1f;

    public const float MinScale = 0.125f;
    public const float MaxScale = 4.0f;

    public float RealDeltaTime { get; private set; }

    public float DeltaTime { get; private set; }

    public double TotalTime { get; private set; }

    public float TimeScale { get; private set; } = 1.0f;

    public bool Paused { get; set; }

    public void Tick(double realSeconds)
    {
        RealDeltaTime = (float)realSeconds;

        float clamped = MathF.Min(RealDeltaTime, MaxDelta);
        DeltaTime = Paused ? 0.0f : clamped * TimeScale;

        TotalTime += DeltaTime;
    }

    public void ChangeScale(float factor)
    {
        TimeScale = Math.Clamp(TimeScale * factor, MinScale, MaxScale);
    }

    public void ResetScale() => TimeScale = 1.0f;
}
