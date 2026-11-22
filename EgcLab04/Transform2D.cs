using OpenTK.Mathematics;

namespace Egc;

public class Transform2D
{
    public Vector2 Position = Vector2.Zero;
    public float Rotation = 0.0f;
    public Vector2 Scale = Vector2.One;
    public float Depth = 0.0f;

    // T1: daca translatia ar fi aplicata inaintea rotatiei, obiectul nu s-ar mai roti pe loc, ci ar descrie un arc de cerc in jurul originii lumii, iar centrul lui nu ar mai ramane in Position.
    public Matrix4 ModelMatrix =>
        Matrix4.CreateScale(Scale.X, Scale.Y, 1.0f)
      * Matrix4.CreateRotationZ(MathHelper.DegreesToRadians(Rotation))
      * Matrix4.CreateTranslation(Position.X, Position.Y, Depth);

    public Transform2D Clone() => (Transform2D)MemberwiseClone();
}
