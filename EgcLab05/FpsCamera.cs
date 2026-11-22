using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class FpsCamera : Camera
{
    private Vector3 _position;
    public override Vector3 Position => _position;

    public float Yaw { get; private set; }

    public float Pitch { get; private set; }

    public bool FreeFly { get; set; }

    private Vector3 _front = -Vector3.UnitZ;
    private Vector3 _right = Vector3.UnitX;
    private Vector3 _up = Vector3.UnitY;

    public override Vector3 Front => _front;

    public const float MaxPitch = 89.0f;
    public float MoveSpeed = 3.0f;
    public float SprintFactor = 2.5f;
    public float MouseSensitivity = 0.1f;
    public float FovStep = 2.0f;

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartPosition = new(0.0f, 1.7f, 6.0f);
    private const float StartYaw = -90.0f;
    private const float StartPitch = 0.0f;

    public FpsCamera()
    {
        Reset();
    }

    public override string Name => FreeFly ? "FPS – zbor liber" : "FPS – mers";

    public override Matrix4 ViewMatrix =>
        Matrix4.LookAt(_position, _position + _front, _up);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        Yaw += mouseDelta.X * MouseSensitivity;
        Pitch = Math.Clamp(Pitch - mouseDelta.Y * MouseSensitivity, -MaxPitch, MaxPitch);

        UpdateVectors();

        if (mouse.ScrollDelta.Y != 0.0f)
        {
            ChangeFov(-mouse.ScrollDelta.Y * FovStep);
        }

        Vector3 forward = FreeFly ? _front : Horizontal(_front);

        Vector3 move = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W)) move += forward;
        if (keyboard.IsKeyDown(Keys.S)) move -= forward;
        if (keyboard.IsKeyDown(Keys.D)) move += _right;
        if (keyboard.IsKeyDown(Keys.A)) move -= _right;

        if (FreeFly)
        {
            if (keyboard.IsKeyDown(Keys.Space)) move += WorldUp;
            if (keyboard.IsKeyDown(Keys.LeftControl)) move -= WorldUp;
        }

        if (move.LengthSquared > 0.0f)
        {
            float speed = keyboard.IsKeyDown(Keys.LeftShift) ? MoveSpeed * SprintFactor : MoveSpeed;
            _position += Vector3.Normalize(move) * speed * dt;
        }
    }

    public void PlaceAt(Vector3 position, Vector3 front)
    {
        Vector3 direction = Vector3.Normalize(front);
        _position = position;
        Pitch = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(direction.Y)), -MaxPitch, MaxPitch);
        Yaw = MathHelper.RadiansToDegrees(MathF.Atan2(direction.Z, direction.X));
        UpdateVectors();
    }

    private static Vector3 Horizontal(Vector3 v)
    {
        var flat = new Vector3(v.X, 0.0f, v.Z);
        return flat.LengthSquared > 0.0f ? Vector3.Normalize(flat) : Vector3.Zero;
    }

    private void UpdateVectors()
    {
        float yaw = MathHelper.DegreesToRadians(Yaw);
        float pitch = MathHelper.DegreesToRadians(Pitch);

        _front = Vector3.Normalize(new Vector3(
            MathF.Cos(yaw) * MathF.Cos(pitch),
            MathF.Sin(pitch),
            MathF.Sin(yaw) * MathF.Cos(pitch)));
        _right = Vector3.Normalize(Vector3.Cross(_front, WorldUp));
        _up = Vector3.Normalize(Vector3.Cross(_right, _front));
    }

    public override void Reset()
    {
        _position = StartPosition;
        Yaw = StartYaw;
        Pitch = StartPitch;
        FieldOfView = 60.0f;
        UpdateVectors();
    }

    public override IEnumerable<string> Describe()
    {
        yield return $"Poziție        : ({_position.X,6:F2}, {_position.Y,6:F2}, {_position.Z,6:F2})";
        yield return $"Yaw / pitch    : {NormalizeAngle(Yaw),7:F1}° / {Pitch,6:F1}°";
        yield return $"Front          : ({_front.X,6:F2}, {_front.Y,6:F2}, {_front.Z,6:F2})";
        yield return $"Right          : ({_right.X,6:F2}, {_right.Y,6:F2}, {_right.Z,6:F2})";
        yield return $"Up             : ({_up.X,6:F2}, {_up.Y,6:F2}, {_up.Z,6:F2})";
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
