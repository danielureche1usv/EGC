using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public class Game : GameWindow
{
    private readonly FpsCamera _fps = new();
    private readonly OrbitCamera _orbit = new();
    private Camera _active;

    private Shader _shader = null!;
    private readonly List<Mesh> _meshes = new();
    private readonly List<SceneObject> _scene = new();

    private bool _depthTest = true;
    private bool _faceCulling;
    private bool _wireframe;

    private bool _ignoreMouseOnce = true;

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
        _active = _fps;
        VSync = VSyncMode.On;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        ConsolePanel.Init();

        Console.WriteLine($"OpenGL  : {GL.GetString(StringName.Version)}");
        Console.WriteLine($"Renderer: {GL.GetString(StringName.Renderer)}");

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _fps.Resize(ClientSize.X, ClientSize.Y);
        _orbit.Resize(ClientSize.X, ClientSize.Y);

        _shader = Shader.FromFiles("Shaders/shader.vert", "Shaders/shader.frag");

        BuildScene();

        CursorState = CursorState.Grabbed;

        ConsolePanel.PrintHelp();
        PrintSceneInfo();
    }

    private void BuildScene()
    {
        Mesh grid = AddMesh(Shapes.Grid(10, 1.0f, new Color4(0.35f, 0.38f, 0.42f, 1.0f)));
        Mesh axes = AddMesh(Shapes.Axes(2.0f));
        Mesh pyramid = AddMesh(Shapes.Pyramid());
        Mesh cube = AddMesh(Shapes.Cube());
        Mesh sphere = AddMesh(Shapes.Sphere(1.0f, 32, 16));
        Mesh smallSphere = AddMesh(Shapes.Sphere(0.35f, 16, 8));
        Mesh cylinder = AddMesh(Shapes.Cylinder(0.4f, 1.5f, 32));
        Mesh torus = AddMesh(Shapes.Torus(0.6f, 0.2f, 32, 16));

        AddObject("podea", grid);
        AddObject("axe", axes).Transform.Position = new Vector3(0.0f, 0.01f, 0.0f);

        AddObject("piramidă", pyramid).Transform.Position = new Vector3(-3.0f, 0.5f, 0.0f);

        SceneObject bigCube = AddObject("cub", cube);
        bigCube.Transform.Position = new Vector3(0.0f, 1.2f, -2.0f);
        bigCube.Transform.Scale = new Vector3(1.2f);
        bigCube.Spin = new Vector3(20.0f, 35.0f, 0.0f);

        SceneObject globe = AddObject("sferă", sphere);
        globe.Transform.Position = new Vector3(3.0f, 1.2f, -1.0f);
        globe.Spin = new Vector3(0.0f, 30.0f, 0.0f);

        for (int k = 0; k < 6; k++)
        {
            foreach (float side in new[] { -4.0f, 4.0f })
            {
                float z = -4.0f - 3.0f * k;
                SceneObject pillar = AddObject($"stâlp {k}", cube);
                pillar.Transform.Position = new Vector3(side, 1.5f, z);
                pillar.Transform.Scale = new Vector3(0.6f, 3.0f, 0.6f);
                pillar.Tint = ColorUtil.FromHsv(k / 6.0f, 0.25f, 0.95f);

                AddObject($"bilă {k}", smallSphere).Transform.Position = new Vector3(side, 3.35f, z);
            }
        }

        SceneObject column = AddObject("cilindru", cylinder);
        column.Transform.Position = new Vector3(-1.5f, 0.75f, -6.0f);
        column.Spin = new Vector3(0.0f, 25.0f, 0.0f);

        SceneObject ring = AddObject("tor", torus);
        ring.Transform.Position = new Vector3(1.5f, 1.0f, -6.0f);
        ring.Spin = new Vector3(40.0f, 20.0f, 0.0f);
    }

    private Mesh AddMesh(Mesh mesh)
    {
        _meshes.Add(mesh);
        return mesh;
    }

    private SceneObject AddObject(string name, Mesh mesh)
    {
        var obj = new SceneObject(name, mesh);
        _scene.Add(obj);
        return obj;
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, e.Width, e.Height);
        _fps.Resize(e.Width, e.Height);
        _orbit.Resize(e.Width, e.Height);
        _titleDirty = true;
    }

    protected override void OnFocusedChanged(FocusedChangedEventArgs e)
    {
        base.OnFocusedChanged(e);
        _ignoreMouseOnce = true;
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);
        float dt = (float)args.Time;

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
            return;
        }

        HandleKeys();

        Vector2 mouseDelta = MouseState.Delta;
        bool fpsWithoutCursor = _active == _fps && CursorState != CursorState.Grabbed;
        if (_ignoreMouseOnce || fpsWithoutCursor)
        {
            mouseDelta = Vector2.Zero;
            _ignoreMouseOnce = false;
        }

        float fovBefore = _active.FieldOfView;
        _active.Update(KeyboardState, MouseState, mouseDelta, dt);
        if (_active.FieldOfView != fovBefore)
        {
            _titleDirty = true;
        }

        foreach (SceneObject obj in _scene)
        {
            obj.Update(dt);
        }

        UpdateTitle(args.Time);
    }

    private void HandleKeys()
    {
        KeyboardState kb = KeyboardState;

        if (kb.IsKeyPressed(Keys.H)) ConsolePanel.PrintHelp();
        if (kb.IsKeyPressed(Keys.I)) PrintSceneInfo();

        if (kb.IsKeyPressed(Keys.Z)) Toggle(ref _depthTest, "test de adâncime");
        if (kb.IsKeyPressed(Keys.C)) Toggle(ref _faceCulling, "eliminarea fețelor din spate");
        if (kb.IsKeyPressed(Keys.F)) Toggle(ref _wireframe, "contur (wireframe)");

        if (kb.IsKeyPressed(Keys.L))
        {
            VSync = VSync == VSyncMode.On ? VSyncMode.Off : VSyncMode.On;
            ConsolePanel.PrintMessage($"VSync (limitarea cadrelor): {(VSync == VSyncMode.On ? "pornit" : "oprit")}");
        }

        if (kb.IsKeyPressed(Keys.R))
        {
            _active.Reset();
            ConsolePanel.PrintMessage("camera a revenit în poziția inițială");
            _titleDirty = true;
        }

        if (kb.IsKeyPressed(Keys.G) && _active == _fps)
        {
            _fps.FreeFly = !_fps.FreeFly;
            ConsolePanel.PrintMessage($"mod cameră: {_fps.Name}");
            _titleDirty = true;
        }

        if (kb.IsKeyPressed(Keys.M) && _active == _fps)
        {
            CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
            _ignoreMouseOnce = true;
            ConsolePanel.PrintMessage(CursorState == CursorState.Grabbed ? "cursor capturat" : "cursor eliberat");
        }

        if (kb.IsKeyPressed(Keys.V))
        {
            SwitchCamera();
        }
    }

    private void SwitchCamera()
    {
        if (_active == _fps)
        {
            _orbit.FollowFps(_fps);
            _active = _orbit;
            CursorState = CursorState.Normal;
        }
        else
        {
            _fps.PlaceAt(_orbit.Position, _orbit.Front);
            _fps.FieldOfView = _orbit.FieldOfView;
            _active = _fps;
            CursorState = CursorState.Grabbed;
        }

        _ignoreMouseOnce = true;
        _titleDirty = true;
        ConsolePanel.PrintMessage($"cameră activă: {_active.Name}");
    }

    private void Toggle(ref bool flag, string name)
    {
        flag = !flag;
        ConsolePanel.PrintMessage($"{name}: {(flag ? "pornit" : "oprit")}");
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        _frames++;

        GL.ClearColor(0.12f, 0.14f, 0.18f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        SetCapability(EnableCap.DepthTest, _depthTest);
        SetCapability(EnableCap.CullFace, _faceCulling);
        SetWireframe(_wireframe);

        _shader.Use();
        _shader.SetMatrix4("uView", _active.ViewMatrix);
        _shader.SetMatrix4("uProjection", _active.ProjectionMatrix);

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
        foreach (Mesh mesh in _meshes)
        {
            mesh.Dispose();
        }
        _shader.Dispose();

        base.OnUnload();
    }

    private void UpdateTitle(double elapsed)
    {
        _secondTimer += elapsed;
        if (_secondTimer >= 1.0)
        {
            _lastFrames = _frames;
            _frames = 0;
            _secondTimer -= 1.0;
            _titleDirty = true;
        }

        if (_titleDirty)
        {
            Title = $"{_baseTitle} | cameră: {_active.Name} | FOV {_active.FieldOfView:F0}° | FPS: {_lastFrames}";
            _titleDirty = false;
        }
    }

    private void PrintSceneInfo()
    {
        ConsolePanel.PrintInfo(SceneInfo());
    }

    private IEnumerable<string> SceneInfo()
    {
        yield return $"Camera activă  : {_active.Name}";
        foreach (string line in _active.Describe())
        {
            yield return line;
        }
        yield return $"FOV vertical   : {_active.FieldOfView,6:F1}°   orizontal: {_active.HorizontalFov,6:F1}°";
        yield return $"Aspect         : {_active.AspectRatio,6:F3}   ({ClientSize.X} × {ClientSize.Y} pixeli)";
        yield return $"Near / far     : {_active.Near} / {_active.Far}";

        int triangles = 0;
        foreach (SceneObject obj in _scene)
        {
            triangles += obj.Mesh.TriangleCount;
        }
        yield return $"Obiecte        : {_scene.Count}   triunghiuri desenate pe cadru: {triangles}";
        yield return $"Depth test     : {OnOff(_depthTest)}   culling: {OnOff(_faceCulling)}   wireframe: {OnOff(_wireframe)}";
        yield return $"VSync          : {OnOff(VSync == VSyncMode.On)}";
        yield return $"Cadre/secundă  : {_lastFrames}";
    }

    private static string OnOff(bool value) => value ? "pornit" : "oprit";

    private static void SetCapability(EnableCap capability, bool enabled)
    {
        if (enabled) GL.Enable(capability);
        else GL.Disable(capability);
    }

    private static void SetWireframe(bool wireframe)
    {
        GL.PolygonMode(TriangleFace.FrontAndBack, wireframe ? PolygonMode.Line : PolygonMode.Fill);
    }
}
