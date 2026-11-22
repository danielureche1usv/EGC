using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public abstract class Camera
{
    public const float MinFov = 10.0f;
    public const float MaxFov = 120.0f;

    public float FieldOfView = 60.0f;

    public float Near = 0.1f;
    public float Far = 100.0f;

    public float AspectRatio { get; private set; } = 4.0f / 3.0f;

    public Matrix4 ProjectionMatrix =>
        Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(FieldOfView), AspectRatio, Near, Far);

    public float HorizontalFov
    {
        get
        {
            float halfVertical = MathHelper.DegreesToRadians(FieldOfView) / 2.0f;
            return MathHelper.RadiansToDegrees(2.0f * MathF.Atan(MathF.Tan(halfVertical) * AspectRatio));
        }
    }

    public void Resize(int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }
        AspectRatio = width / (float)height;
    }

    public void ChangeFov(float deltaDegrees)
    {
        FieldOfView = Math.Clamp(FieldOfView + deltaDegrees, MinFov, MaxFov);
    }

    public abstract string Name { get; }

    public abstract Vector3 Position { get; }

    public abstract Vector3 Front { get; }

    public abstract Matrix4 ViewMatrix { get; }

    public abstract void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt);

    public abstract void Reset();

    public abstract IEnumerable<string> Describe();
}
