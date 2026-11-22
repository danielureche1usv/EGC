using OpenTK.Mathematics;

namespace Egc;

public sealed class SceneObject
{
    public string Name { get; }
    public Mesh? Mesh { get; }
    public Transform3D Transform { get; } = new();
    public Color4 Tint = Color4.White;
    public Vector3 Spin = Vector3.Zero;
    public bool Visible = true;

    public SceneObject? Parent { get; set; }

    public SceneObject(string name, Mesh? mesh, SceneObject? parent = null)
    {
        Name = name;
        Mesh = mesh;
        Parent = parent;
    }

    public Matrix4 WorldMatrix =>
        Parent == null ? Transform.ModelMatrix : Transform.ModelMatrix * Parent.WorldMatrix;

    public Vector3 WorldPosition => WorldMatrix.Row3.Xyz;

    public void Update(float dt)
    {
        Transform.Rotation += Spin * dt;
    }
}
