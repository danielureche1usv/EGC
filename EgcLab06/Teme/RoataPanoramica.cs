// Varianta C: roata panoramică
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class RoataPanoramica : Scene
{
    private const string SpeedUp = "roata: viteza țintă +10°/s";
    private const string SpeedDown = "roata: viteza țintă -10°/s";
    private const string StopResume = "roata: oprește / pornește lin";
    private static readonly string[] CabinActions =
        Enumerable.Range(1, 8).Select(n => $"roata: cabina {n} la peron").ToArray();

    private const int CabinCount = 8;
    private const int RimSegments = 32;
    private const float Radius = 4.0f;
    private const float HubHeight = 6.0f;
    private const float SpeedStep = 10.0f;
    private const float MaxSpeed = 60.0f;
    private const float DefaultSpeed = 20.0f;
    private const float Response = 1.5f;
    private const float Gravity = 9.81f;
    private const float SwingStiffness = 25.0f;
    private const float SwingDamping = 3.0f;

    private SceneObject _hub = null!;
    private readonly SceneObject[] _mounts = new SceneObject[CabinCount];
    private readonly float[] _swing = new float[CabinCount];
    private readonly float[] _swingSpeed = new float[CabinCount];

    private float _angle;
    private float _speed;
    private float _targetSpeed = DefaultSpeed;
    private float _resumeSpeed = DefaultSpeed;
    private float _angularVelocity;
    private float _angularAcceleration;

    private Tween? _dock;
    private int _dockCabin;

    public override string Name => "tema – roata panoramică";

    public override IEnumerable<string> ActionNames =>
        new[] { SpeedUp, SpeedDown, StopResume }.Concat(CabinActions);

    public override string Status => _dock != null
        ? $"cabina {_dockCabin} vine la peron"
        : $"v = {_speed:F1}°/s (țintă {_targetSpeed:F0}°/s)";

    public override void Load(Input input)
    {
        CameraPosition = new Vector3(0.0f, 5.0f, 16.0f);
        CameraTarget = new Vector3(0.0f, 5.0f, 0.0f);

        input.Bind(SpeedUp, Keys.Up);
        input.Bind(SpeedDown, Keys.Down);
        input.Bind(StopResume, Keys.Space);
        for (int k = 0; k < CabinCount; k++)
        {
            input.Bind(CabinActions[k], Keys.D1 + k, Keys.KeyPad1 + k);
        }

        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.30f, 0.33f, 0.38f, 1.0f)));
        Mesh cube = AddMesh(Shapes.ShadedCube());
        Mesh cylinder = AddMesh(Shapes.ShadedCylinder(0.5f, 1.0f, 24));

        AddObject("podea", grid);
        BuildStand(cube);
        BuildWheel(cube, cylinder);
        UpdateHierarchy();
    }

    private void BuildStand(Mesh cube)
    {
        var steel = new Color4(0.55f, 0.58f, 0.65f, 1.0f);
        float footX = 3.0f;
        float length = MathF.Sqrt(footX * footX + HubHeight * HubHeight);
        float tilt = MathHelper.RadiansToDegrees(MathF.Asin(footX / length));

        foreach (float z in new[] { -0.9f, 0.9f })
        {
            foreach (float side in new[] { -1.0f, 1.0f })
            {
                SceneObject leg = AddObject("picior", cube);
                leg.Transform.Position = new Vector3(side * footX / 2.0f, HubHeight / 2.0f, z);
                leg.Transform.Rotation = new Vector3(0.0f, 0.0f, side * tilt);
                leg.Transform.Scale = new Vector3(0.2f, length, 0.2f);
                leg.Tint = steel;
            }
        }

        SceneObject platform = AddObject("peron", cube);
        platform.Transform.Position = new Vector3(0.0f, 0.3f, 0.0f);
        platform.Transform.Scale = new Vector3(2.4f, 0.6f, 2.2f);
        platform.Tint = new Color4(0.75f, 0.5f, 0.3f, 1.0f);
    }

    private void BuildWheel(Mesh cube, Mesh cylinder)
    {
        var steel = new Color4(0.85f, 0.85f, 0.9f, 1.0f);

        _hub = AddObject("butuc", null);
        _hub.Transform.Position = new Vector3(0.0f, HubHeight, 0.0f);

        SceneObject axle = AddObject("ax", cylinder, _hub);
        axle.Transform.Rotation = new Vector3(90.0f, 0.0f, 0.0f);
        axle.Transform.Scale = new Vector3(0.6f, 2.0f, 0.6f);
        axle.Tint = new Color4(0.9f, 0.3f, 0.3f, 1.0f);

        for (int k = 0; k < CabinCount; k++)
        {
            SceneObject spokePivot = AddObject($"spiță {k + 1}", null, _hub);
            spokePivot.Transform.Rotation = new Vector3(0.0f, 0.0f, CabinAngle(k));

            SceneObject spoke = AddObject($"spiță {k + 1} (formă)", cube, spokePivot);
            spoke.Transform.Position = new Vector3(Radius / 2.0f, 0.0f, 0.0f);
            spoke.Transform.Scale = new Vector3(Radius, 0.08f, 0.08f);
            spoke.Tint = steel;
        }

        float halfStep = MathF.PI / RimSegments;
        float chord = 2.0f * Radius * MathF.Sin(halfStep);
        float rimRadius = Radius * MathF.Cos(halfStep);
        for (int i = 0; i < RimSegments; i++)
        {
            SceneObject rimPivot = AddObject("jantă", null, _hub);
            rimPivot.Transform.Rotation = new Vector3(0.0f, 0.0f, i * 360.0f / RimSegments + 180.0f / RimSegments);

            SceneObject segment = AddObject("jantă (segment)", cube, rimPivot);
            segment.Transform.Position = new Vector3(rimRadius, 0.0f, 0.0f);
            segment.Transform.Scale = new Vector3(0.12f, chord, 0.12f);
            segment.Tint = steel;
        }

        for (int k = 0; k < CabinCount; k++)
        {
            float a = MathHelper.DegreesToRadians(CabinAngle(k));

            SceneObject mount = AddObject($"prindere cabina {k + 1}", null, _hub);
            mount.Transform.Position = new Vector3(Radius * MathF.Cos(a), Radius * MathF.Sin(a), 0.0f);
            _mounts[k] = mount;

            SceneObject hanger = AddObject($"tijă cabina {k + 1}", cube, mount);
            hanger.Transform.Position = new Vector3(0.0f, -0.25f, 0.0f);
            hanger.Transform.Scale = new Vector3(0.05f, 0.5f, 0.05f);
            hanger.Tint = steel;

            SceneObject cabin = AddObject($"cabina {k + 1}", cube, mount);
            cabin.Transform.Position = new Vector3(0.0f, -0.85f, 0.0f);
            cabin.Transform.Scale = new Vector3(0.75f, 0.7f, 0.65f);
            cabin.Tint = ColorUtil.FromHsv(k / (float)CabinCount, 0.6f, 1.0f);
        }
    }

    private static float CabinAngle(int k) => k * 360.0f / CabinCount;

    public override void Update(float dt, Input input)
    {
        base.Update(dt, input);
        HandleInput(input);

        float previousAngle = _angle;
        float previousVelocity = _angularVelocity;

        if (_dock != null)
        {
            _dock.Update(dt);
            _angle = _dock.Value.X;
            if (_dock.IsFinished)
            {
                _dock = null;
                _speed = 0.0f;
                ConsolePanel.PrintMessage($"cabina {_dockCabin} a ajuns la peron");
            }
        }
        else
        {
            _speed += (_targetSpeed - _speed) * (1.0f - MathF.Exp(-Response * dt));
            _angle += _speed * dt;
        }

        if (dt > 0.0f)
        {
            _angularVelocity = (_angle - previousAngle) / dt;
            _angularAcceleration = (_angularVelocity - previousVelocity) / dt;
            UpdateSwing(dt);
        }

        if (_dock == null)
        {
            _angle = Wrap360(_angle);
        }

        UpdateHierarchy();
    }

    private void HandleInput(Input input)
    {
        if (input.WasPressed(SpeedUp)) SetTarget(_targetSpeed + SpeedStep);
        if (input.WasPressed(SpeedDown)) SetTarget(_targetSpeed - SpeedStep);

        if (input.WasPressed(StopResume) && _dock == null)
        {
            if (MathF.Abs(_targetSpeed) > 0.01f)
            {
                _resumeSpeed = _targetSpeed;
                _targetSpeed = 0.0f;
                ConsolePanel.PrintMessage("roata se oprește");
            }
            else
            {
                _targetSpeed = MathF.Abs(_resumeSpeed) > 0.01f ? _resumeSpeed : DefaultSpeed;
                ConsolePanel.PrintMessage("roata pornește");
            }
        }

        for (int k = 0; k < CabinCount; k++)
        {
            if (input.WasPressed(CabinActions[k]))
            {
                StartDocking(k);
            }
        }
    }

    private void SetTarget(float value)
    {
        _targetSpeed = Math.Clamp(value, -MaxSpeed, MaxSpeed);
    }

    private void StartDocking(int cabin)
    {
        float desired = -90.0f - CabinAngle(cabin);
        float delta = Wrap180(desired - _angle);

        float duration;
        EasingFunction ease;
        if (MathF.Abs(_speed) > 1.0f && MathF.Sign(_speed) == MathF.Sign(delta) && MathF.Abs(delta) > 1.0f)
        {
            duration = Math.Clamp(2.0f * MathF.Abs(delta) / MathF.Abs(_speed), 0.6f, 8.0f);
            ease = Easing.EaseOut;
        }
        else
        {
            duration = 1.0f + MathF.Abs(delta) / 90.0f;
            ease = Easing.EaseInOut;
        }

        _dock = new Tween(new Vector3(_angle, 0.0f, 0.0f), new Vector3(_angle + delta, 0.0f, 0.0f), duration, ease);
        _dockCabin = cabin + 1;
        _resumeSpeed = MathF.Abs(_targetSpeed) > 0.01f ? _targetSpeed : _resumeSpeed;
        _targetSpeed = 0.0f;
        ConsolePanel.PrintMessage($"cabina {_dockCabin} vine la peron ({delta:F0}°)");
    }

    private void UpdateSwing(float dt)
    {
        float alpha = MathHelper.DegreesToRadians(_angularAcceleration);
        for (int k = 0; k < CabinCount; k++)
        {
            float worldAngle = MathHelper.DegreesToRadians(CabinAngle(k) + _angle);
            float horizontalAcceleration = -MathF.Sin(worldAngle) * alpha * Radius;
            float rest = -MathHelper.RadiansToDegrees(MathF.Atan(horizontalAcceleration / Gravity));

            _swingSpeed[k] += (-SwingStiffness * (_swing[k] - rest) - SwingDamping * _swingSpeed[k]) * dt;
            _swing[k] = Math.Clamp(_swing[k] + _swingSpeed[k] * dt, -35.0f, 35.0f);
        }
    }

    private void UpdateHierarchy()
    {
        _hub.Transform.Rotation = new Vector3(0.0f, 0.0f, _angle);
        for (int k = 0; k < CabinCount; k++)
        {
            _mounts[k].Transform.Rotation = new Vector3(0.0f, 0.0f, -_angle + _swing[k]);
        }
    }

    private static float Wrap360(float degrees)
    {
        degrees %= 360.0f;
        return degrees < 0.0f ? degrees + 360.0f : degrees;
    }

    private static float Wrap180(float degrees)
    {
        float wrapped = Wrap360(degrees + 180.0f);
        return wrapped - 180.0f;
    }

    public override IEnumerable<string> Describe()
    {
        foreach (string line in base.Describe()) yield return line;
        yield return $"Unghiul roții  : {Wrap360(_angle),6:F1}°";
        yield return $"Viteză / țintă : {_speed,6:F1}°/s / {_targetSpeed,6:F1}°/s   accelerație: {_angularAcceleration,7:F1}°/s²";
        yield return $"Andocare       : {(_dock != null ? $"cabina {_dockCabin}, {_dock.Progress:P0}" : "nu")}";
        yield return $"Legănare max.  : {_swing.Select(MathF.Abs).Max(),5:F1}°";
    }
}
