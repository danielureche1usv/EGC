using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class OrbitCamera : Camera
{
    public const float MaxElevation = 89.0f;
    public const float MinDistance = 1.5f;
    public const float MaxDistance = 30.0f;

    public float MouseSensitivity = 0.3f;
    public float KeyRotationSpeed = 90.0f;
    public float ZoomFactor = 0.9f;
    public float TargetSpeed = 3.0f;
    public float FollowDistance = 5.0f;

    private static readonly Vector3 WorldUp = Vector3.UnitY;
    private static readonly Vector3 StartTarget = new(0.0f, 1.2f, -2.0f);
    private const float StartDistance = 8.0f;
    private const float StartAzimuth = 90.0f;
    private const float StartElevation = 20.0f;

    public Vector3 Target { get; private set; }
    public float Distance { get; private set; }
    public float Azimuth { get; private set; }
    public float Elevation { get; private set; }

    public OrbitCamera()
    {
        Reset();
    }

    public override string Name => "Orbită";

    public override Vector3 Position
    {
        get
        {
            float az = MathHelper.DegreesToRadians(Azimuth);
            float el = MathHelper.DegreesToRadians(Elevation);
            var offset = new Vector3(
                MathF.Cos(el) * MathF.Cos(az),
                MathF.Sin(el),
                MathF.Cos(el) * MathF.Sin(az));
            return Target + Distance * offset;
        }
    }

    public override Vector3 Front => Vector3.Normalize(Target - Position);

    public override Matrix4 ViewMatrix => Matrix4.LookAt(Position, Target, WorldUp);

    public override void Update(KeyboardState keyboard, MouseState mouse, Vector2 mouseDelta, float dt)
    {
        if (mouse.IsButtonDown(MouseButton.Left))
        {
            Rotate(mouseDelta.X * MouseSensitivity, mouseDelta.Y * MouseSensitivity);
        }

        float step = KeyRotationSpeed * dt;
        if (keyboard.IsKeyDown(Keys.Left))  Rotate(step, 0.0f);
        if (keyboard.IsKeyDown(Keys.Right)) Rotate(-step, 0.0f);
        if (keyboard.IsKeyDown(Keys.Up))    Rotate(0.0f, step);
        if (keyboard.IsKeyDown(Keys.Down))  Rotate(0.0f, -step);

        if (mouse.ScrollDelta.Y != 0.0f)
        {
            Distance = Math.Clamp(Distance * MathF.Pow(ZoomFactor, mouse.ScrollDelta.Y), MinDistance, MaxDistance);
        }

        Vector3 front = Front;
        var forward = new Vector3(front.X, 0.0f, front.Z);
        if (forward.LengthSquared > 0.0f)
        {
            forward = Vector3.Normalize(forward);
            Vector3 right = Vector3.Normalize(Vector3.Cross(forward, WorldUp));

            Vector3 move = Vector3.Zero;
            if (keyboard.IsKeyDown(Keys.W)) move += forward;
            if (keyboard.IsKeyDown(Keys.S)) move -= forward;
            if (keyboard.IsKeyDown(Keys.D)) move += right;
            if (keyboard.IsKeyDown(Keys.A)) move -= right;

            if (move.LengthSquared > 0.0f)
            {
                Target += Vector3.Normalize(move) * TargetSpeed * dt;
            }
        }
    }

    public void FollowFps(FpsCamera fps)
    {
        Vector3 front = Vector3.Normalize(fps.Front);
        Vector3 toCamera = -front;

        Target = fps.Position + front * FollowDistance;
        Distance = FollowDistance;
        Elevation = Math.Clamp(MathHelper.RadiansToDegrees(MathF.Asin(toCamera.Y)), -MaxElevation, MaxElevation);
        Azimuth = MathHelper.RadiansToDegrees(MathF.Atan2(toCamera.Z, toCamera.X));
        FieldOfView = fps.FieldOfView;
    }

    private void Rotate(float deltaAzimuth, float deltaElevation)
    {
        Azimuth += deltaAzimuth;
        Elevation = Math.Clamp(Elevation + deltaElevation, -MaxElevation, MaxElevation);
    }

    public override void Reset()
    {
        Target = StartTarget;
        Distance = StartDistance;
        Azimuth = StartAzimuth;
        Elevation = StartElevation;
        FieldOfView = 60.0f;
    }

    public override IEnumerable<string> Describe()
    {
        Vector3 p = Position;
        yield return $"Țintă          : ({Target.X,6:F2}, {Target.Y,6:F2}, {Target.Z,6:F2})";
        yield return $"Distanță       : {Distance,6:F2}";
        yield return $"Azimut / elev. : {NormalizeAngle(Azimuth),7:F1}° / {Elevation,6:F1}°";
        yield return $"Poziție        : ({p.X,6:F2}, {p.Y,6:F2}, {p.Z,6:F2})";
    }

    private static float NormalizeAngle(float degrees)
    {
        degrees %= 360.0f;
        if (degrees > 180.0f) degrees -= 360.0f;
        if (degrees <= -180.0f) degrees += 360.0f;
        return degrees;
    }
}
