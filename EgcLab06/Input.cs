using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Egc;

public sealed class Input
{
    private readonly Dictionary<string, List<Keys>> _keys = new();
    private readonly Dictionary<string, List<MouseButton>> _buttons = new();

    private KeyboardState? _keyboard;
    private MouseState? _mouse;

    public void Update(KeyboardState keyboard, MouseState mouse)
    {
        _keyboard = keyboard;
        _mouse = mouse;
    }

    public void Bind(string action, params Keys[] keys)
    {
        if (!_keys.TryGetValue(action, out List<Keys>? list))
        {
            list = new List<Keys>();
            _keys[action] = list;
        }
        foreach (Keys key in keys)
        {
            if (!list.Contains(key)) list.Add(key);
        }
    }

    public void Bind(string action, MouseButton button)
    {
        if (!_buttons.TryGetValue(action, out List<MouseButton>? list))
        {
            list = new List<MouseButton>();
            _buttons[action] = list;
        }
        if (!list.Contains(button)) list.Add(button);
    }

    public void Unbind(string action)
    {
        _keys.Remove(action);
        _buttons.Remove(action);
    }

    public bool IsDown(string action) =>
        AnyKey(action, k => _keyboard!.IsKeyDown(k)) || AnyButton(action, b => _mouse!.IsButtonDown(b));

    public bool WasPressed(string action) =>
        AnyKey(action, k => _keyboard!.IsKeyPressed(k)) || AnyButton(action, b => _mouse!.IsButtonPressed(b));

    public bool WasReleased(string action) =>
        AnyKey(action, k => _keyboard!.IsKeyReleased(k)) || AnyButton(action, b => _mouse!.IsButtonReleased(b));

    public float Axis(string negative, string positive) =>
        (IsDown(positive) ? 1.0f : 0.0f) - (IsDown(negative) ? 1.0f : 0.0f);

    public Vector2 MousePosition => _mouse?.Position ?? Vector2.Zero;

    public Vector2 MouseDelta => _mouse?.Delta ?? Vector2.Zero;

    public float Wheel => _mouse?.ScrollDelta.Y ?? 0.0f;

    public IEnumerable<string> Describe(IEnumerable<string> actions)
    {
        foreach (string action in actions)
        {
            var names = new List<string>();
            if (_keys.TryGetValue(action, out List<Keys>? keys)) names.AddRange(keys.Select(KeyName));
            if (_buttons.TryGetValue(action, out List<MouseButton>? buttons)) names.AddRange(buttons.Select(ButtonName));
            yield return $"{string.Join(" / ", names),-26} {action}";
        }
    }

    private static string KeyName(Keys key) => key switch
    {
        Keys.LeftBracket => "[",
        Keys.RightBracket => "]",
        Keys.Minus => "-",
        Keys.Equal => "=",
        Keys.KeyPadSubtract => "Num -",
        Keys.KeyPadAdd => "Num +",
        Keys.Backspace => "Backspace",
        Keys.Escape => "Esc",
        Keys.KeyPadEnter => "Num Enter",
        _ when key >= Keys.D0 && key <= Keys.D9 => ((int)(key - Keys.D0)).ToString(),
        _ => key.ToString(),
    };

    private static string ButtonName(MouseButton button) => button switch
    {
        MouseButton.Left => "clic stânga",
        MouseButton.Right => "clic dreapta",
        MouseButton.Middle => "clic rotiță",
        _ => $"mouse {button}",
    };

    private bool AnyKey(string action, Func<Keys, bool> test) =>
        _keyboard != null && _keys.TryGetValue(action, out List<Keys>? keys) && keys.Any(test);

    private bool AnyButton(string action, Func<MouseButton, bool> test) =>
        _mouse != null && _buttons.TryGetValue(action, out List<MouseButton>? buttons) && buttons.Any(test);
}

public static class Actions
{
    public const string Quit = "ieșire";
    public const string Help = "meniul de comenzi";
    public const string Info = "parametrii scenei";
    public const string Pause = "pauză";
    public const string Slower = "timpul mai încet (× 0.5)";
    public const string Faster = "timpul mai repede (× 2)";
    public const string NormalSpeed = "timpul normal (× 1)";
    public const string Wireframe = "contur / plin";
    public const string VSync = "VSync pornit / oprit";
    public const string NextScene = "scena următoare";
    public const string Freeze = "simulează o blocare de 0,5 s";

    public const string Left = "stânga";
    public const string Right = "dreapta";
    public const string Forward = "înainte";
    public const string Back = "înapoi";
    public const string Jump = "salt (ține = mai sus)";
}
