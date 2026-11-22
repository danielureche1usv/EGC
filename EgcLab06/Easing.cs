namespace Egc;

public delegate float EasingFunction(float t);

public static class Easing
{
    public static float Lerp(float a, float b, float t) => a + (b - a) * t;

    public static float Linear(float t) => t;

    public static float EaseIn(float t) => t * t;

    public static float EaseOut(float t)
    {
        float inverse = 1.0f - t;
        return 1.0f - inverse * inverse;
    }

    public static float EaseInOut(float t) => t * t * (3.0f - 2.0f * t);

    public static readonly (string Name, EasingFunction Function)[] All =
    {
        ("Linear", Linear),
        ("EaseIn", EaseIn),
        ("EaseOut", EaseOut),
        ("EaseInOut", EaseInOut),
    };
}
