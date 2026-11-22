using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class Game : GameWindow
{
    private const float MoveSpeed = 2.0f;
    private const float RotationSpeed = 90.0f;
    private const float ScaleFactor = 1.5f;
    private const float MinScale = 0.1f;
    private const float MaxScale = 10.0f;
    private const float ShiftMultiplier = 3.0f;

    private readonly Camera _camera = new();

    private Shader _shader = null!;
    private readonly List<SceneObject> _scene = new();
    private readonly List<Transform2D> _initialTransforms = new();

    private int _selected;
    private bool _wireframe;

    private readonly string _baseTitle;
    private double _secondTimer;
    private int _frames, _lastFrames;
    private bool _titleDirty = true;

    public Game(int width, int height, string title)
        : base(GameWindowSettings.Default,
               new NativeWindowSettings
               {
                   ClientSize = new Vector2i(width, height),
                   Title = title,
                   APIVersion = new Version(3, 3),
                   Profile = ContextProfile.Core,
                   Flags = ContextFlags.ForwardCompatible
               })
    {
        _baseTitle = title;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _camera.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        AddObject("pătrat", Shapes.Quad(),
                  new Transform2D { Position = new Vector2(-1.8f, -1.0f), Depth = -3.0f });
        AddObject("triunghi", Shapes.Triangle(),
                  new Transform2D { Position = new Vector2(-1.2f, 1.0f) });
        AddObject("hexagon", Shapes.RegularPolygon(6, 0.5f),
                  new Transform2D { Position = new Vector2(2.0f, 1.0f) });
        AddObject("cerc", Shapes.RegularPolygon(64, 0.5f),
                  new Transform2D { Position = new Vector2(0.8f, -0.6f), Depth = 2.0f });
    }

    private void AddObject(string name, Mesh mesh, Transform2D transform)
    {
        _scene.Add(new SceneObject(name, mesh, transform));
        _initialTransforms.Add(transform.Clone());
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        _camera.Resize(e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }

        float dt = (float)args.Time;
        Transform2D t = _scene[_selected].Transform;

        float boost = KeyboardState.IsKeyDown(Keys.LeftShift) || KeyboardState.IsKeyDown(Keys.RightShift)
            ? ShiftMultiplier
            : 1.0f;

        float move = MoveSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.Left))  t.Position.X -= move;
        if (KeyboardState.IsKeyDown(Keys.Right)) t.Position.X += move;
        if (KeyboardState.IsKeyDown(Keys.Down))  t.Position.Y -= move;
        if (KeyboardState.IsKeyDown(Keys.Up))    t.Position.Y += move;

        float rotate = RotationSpeed * boost * dt;
        if (KeyboardState.IsKeyDown(Keys.Q)) t.Rotation += rotate;
        if (KeyboardState.IsKeyDown(Keys.E)) t.Rotation -= rotate;

        // T2: scalarea se face prin inmultire pentru ca o scadere constanta ar duce scara la 0 si apoi la valori negative (forma ar disparea si ar reaparea oglindita), pe cand inmultirea doar se apropie de 0 fara sa-l atinga, iar marirea si micsorarea sunt simetrice.
        float scaleStep = MathF.Pow(ScaleFactor, boost * dt);
        if (KeyboardState.IsKeyDown(Keys.X)) t.Scale *= scaleStep;
        if (KeyboardState.IsKeyDown(Keys.Z)) t.Scale /= scaleStep;
        t.Scale = new Vector2(
            Math.Clamp(t.Scale.X, MinScale, MaxScale),
            Math.Clamp(t.Scale.Y, MinScale, MaxScale));

        if (KeyboardState.IsKeyPressed(Keys.Tab)) _selected = (_selected + 1) % _scene.Count;
        if (KeyboardState.IsKeyPressed(Keys.O))   _camera.IsPerspective = !_camera.IsPerspective;
        if (KeyboardState.IsKeyPressed(Keys.F))   _wireframe = !_wireframe;
        if (KeyboardState.IsKeyPressed(Keys.R))   ResetScene();

        if (KeyboardState.IsKeyPressed(Keys.Tab) || KeyboardState.IsKeyPressed(Keys.O) ||
            KeyboardState.IsKeyPressed(Keys.R))
        {
            _titleDirty = true;
        }

        if (MouseState.IsButtonPressed(MouseButton.Left))
        {
            t.Position = ScreenToWorld(MouseState.Position);
        }

        float camSpeed = 3.0f / _camera.Zoom;
        if (KeyboardState.IsKeyDown(Keys.A)) _camera.Position.X -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.D)) _camera.Position.X += camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.S)) _camera.Position.Y -= camSpeed * dt;
        if (KeyboardState.IsKeyDown(Keys.W)) _camera.Position.Y += camSpeed * dt;

        _camera.Zoom *= MathF.Pow(1.1f, MouseState.ScrollDelta.Y);
        _camera.Zoom = Math.Clamp(_camera.Zoom, 0.1f, 10.0f);

        _secondTimer += args.Time;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            string projection = _camera.IsPerspective ? "perspectivă" : "ortografică";
            Title = $"{_baseTitle} | selectat: {_scene[_selected].Name} | proiecție {projection} | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        SetWireframe(_wireframe);
        _shader.Use();

        _shader.SetMatrix4("uView", _camera.ViewMatrix);
        _shader.SetMatrix4("uProjection", _camera.ProjectionMatrix);

        foreach (SceneObject obj in _scene)
        {
            _shader.SetMatrix4("uModel", obj.Transform.ModelMatrix);
            _shader.SetColor("uTint", obj.Tint);
            obj.Mesh.Draw();
        }

        SwapBuffers();
    }

    protected override void OnUnload()
    {
        foreach (SceneObject obj in _scene)
        {
            obj.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    private Vector2 ScreenToWorld(Vector2 mousePixels)
    {
        float xNdc = 2.0f * mousePixels.X / ClientSize.X - 1.0f;
        // T3: mouse-ul are originea in coltul stanga-sus si y creste in jos, iar in NDC y creste in sus; de aceea axa y trebuie intoarsa (1 - ...), altfel clicul ar ajunge in punctul simetric fata de axa orizontala.
        float yNdc = 1.0f - 2.0f * mousePixels.Y / ClientSize.Y;

        Matrix4 inverse = (_camera.ViewMatrix * _camera.ProjectionMatrix).Inverted();
        Vector4 world = new Vector4(xNdc, yNdc, 0.0f, 1.0f) * inverse;

        return world.Xy / world.W;
    }

    private void ResetScene()
    {
        for (int i = 0; i < _scene.Count; i++)
        {
            _scene[i].Transform = _initialTransforms[i].Clone();
        }
        _camera.Reset();
    }

    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
