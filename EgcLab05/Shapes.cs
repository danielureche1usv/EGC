using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

public static class Shapes
{
    public static Mesh Grid(int halfSize, float step, Color4 color)
    {
        int linesPerDirection = 2 * halfSize + 1;
        var vertices = new float[linesPerDirection * 2 * 2 * Mesh.FloatsPerVertex];
        var indices = new uint[linesPerDirection * 2 * 2];

        float extent = halfSize * step;
        int v = 0;
        for (int k = -halfSize; k <= halfSize; k++)
        {
            float c = k * step;
            SetVertex(vertices, v++, c, 0.0f, -extent, color);
            SetVertex(vertices, v++, c, 0.0f,  extent, color);
            SetVertex(vertices, v++, -extent, 0.0f, c, color);
            SetVertex(vertices, v++,  extent, 0.0f, c, color);
        }
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = (uint)i;
        }

        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    public static Mesh Axes(float length)
    {
        var vertices = new float[6 * Mesh.FloatsPerVertex];
        SetVertex(vertices, 0, 0, 0, 0,        1, 0, 0);
        SetVertex(vertices, 1, length, 0, 0,   1, 0, 0);
        SetVertex(vertices, 2, 0, 0, 0,        0, 1, 0);
        SetVertex(vertices, 3, 0, length, 0,   0, 1, 0);
        SetVertex(vertices, 4, 0, 0, 0,        0, 0, 1);
        SetVertex(vertices, 5, 0, 0, length,   0, 0, 1);
        uint[] indices = { 0, 1, 2, 3, 4, 5 };
        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    public static Mesh Pyramid()
    {
        const float h = 0.5f;
        var vertices = new float[16 * Mesh.FloatsPerVertex];
        var indices = new uint[4 * 3 + 6];

        var apex = new Vector3(0, h, 0);
        Vector3 frontLeft = new(-h, -h, h), frontRight = new(h, -h, h);
        Vector3 backRight = new(h, -h, -h), backLeft = new(-h, -h, -h);

        AddTriangle(vertices, indices, 0, frontLeft, frontRight, apex, ColorUtil.FromHsv(0.00f, 0.8f, 1.0f));
        AddTriangle(vertices, indices, 1, frontRight, backRight, apex, ColorUtil.FromHsv(0.15f, 0.8f, 1.0f));
        AddTriangle(vertices, indices, 2, backRight, backLeft, apex,   ColorUtil.FromHsv(0.30f, 0.8f, 1.0f));
        AddTriangle(vertices, indices, 3, backLeft, frontLeft, apex,   ColorUtil.FromHsv(0.55f, 0.8f, 1.0f));

        Color4 baseColor = new(0.35f, 0.35f, 0.35f, 1.0f);
        SetVertex(vertices, 12, backLeft, baseColor);
        SetVertex(vertices, 13, backRight, baseColor);
        SetVertex(vertices, 14, frontRight, baseColor);
        SetVertex(vertices, 15, frontLeft, baseColor);
        uint[] baseIndices = { 12, 13, 14, 14, 15, 12 };
        baseIndices.CopyTo(indices, 12);

        return new Mesh(vertices, indices);
    }

    public static Mesh Cube()
    {
        const float h = 0.5f;
        var vertices = new float[24 * Mesh.FloatsPerVertex];
        var indices = new uint[36];

        AddFace(vertices, indices, 0,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Color4(0.2f, 0.4f, 1.0f, 1.0f));

        AddFace(vertices, indices, 1,
                new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
                new Color4(1.0f, 0.2f, 0.2f, 1.0f));

        AddFace(vertices, indices, 2,
                new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
                new Color4(1.0f, 1.0f, 0.2f, 1.0f));

        AddFace(vertices, indices, 3,
                new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
                new Color4(0.2f, 1.0f, 1.0f, 1.0f));

        AddFace(vertices, indices, 4,
                new Vector3(-h, h, h), new Vector3(h, h, h), new Vector3(h, h, -h), new Vector3(-h, h, -h),
                new Color4(0.2f, 1.0f, 0.2f, 1.0f));

        AddFace(vertices, indices, 5,
                new Vector3(-h, -h, -h), new Vector3(h, -h, -h), new Vector3(h, -h, h), new Vector3(-h, -h, h),
                new Color4(1.0f, 0.2f, 1.0f, 1.0f));

        return new Mesh(vertices, indices);
    }

    public static Mesh Sphere(float radius, int slices, int stacks)
    {
        int columns = slices + 1;
        int rows = stacks + 1;
        var vertices = new float[rows * columns * Mesh.FloatsPerVertex];
        var indices = new uint[stacks * slices * 6];

        for (int i = 0; i < rows; i++)
        {
            float v = (float)i / stacks;
            float latitude = -MathF.PI / 2.0f + v * MathF.PI;
            float parallelRadius = radius * MathF.Cos(latitude);
            float y = radius * MathF.Sin(latitude);

            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / slices;
                float longitude = u * 2.0f * MathF.PI;

                float x = parallelRadius * MathF.Cos(longitude);
                float z = -parallelRadius * MathF.Sin(longitude);

                SetVertex(vertices, i * columns + j, x, y, z, u, v, 0.35f);
            }
        }

        WriteGridIndices(indices, 0, 0, stacks, slices);

        return new Mesh(vertices, indices);
    }

    public static Mesh Cylinder(float radius, float height, int segments)
    {
        int columns = segments + 1;
        int sideVertices = 2 * columns;
        int capVertices = 1 + columns;
        var vertices = new float[(sideVertices + 2 * capVertices) * Mesh.FloatsPerVertex];
        var indices = new uint[segments * 12];

        float half = height / 2.0f;

        for (int i = 0; i < 2; i++)
        {
            float y = i == 0 ? -half : half;
            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / segments;
                float angle = u * 2.0f * MathF.PI;
                SetVertex(vertices, i * columns + j,
                          radius * MathF.Cos(angle), y, -radius * MathF.Sin(angle), u, i, 0.35f);
            }
        }

        int k = WriteGridIndices(indices, 0, 0, 1, segments);

        int topCenter = sideVertices;
        int bottomCenter = sideVertices + capVertices;
        var topColor = new Color4(0.9f, 0.9f, 0.3f, 1.0f);
        var bottomColor = new Color4(0.4f, 0.4f, 0.45f, 1.0f);

        SetVertex(vertices, topCenter, 0.0f, half, 0.0f, topColor);
        SetVertex(vertices, bottomCenter, 0.0f, -half, 0.0f, bottomColor);

        for (int j = 0; j < columns; j++)
        {
            float angle = (float)j / segments * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);
            SetVertex(vertices, topCenter + 1 + j, x, half, z, topColor);
            SetVertex(vertices, bottomCenter + 1 + j, x, -half, z, bottomColor);
        }

        for (int j = 0; j < segments; j++)
        {
            indices[k++] = (uint)topCenter;
            indices[k++] = (uint)(topCenter + 1 + j);
            indices[k++] = (uint)(topCenter + 2 + j);

            indices[k++] = (uint)bottomCenter;
            indices[k++] = (uint)(bottomCenter + 2 + j);
            indices[k++] = (uint)(bottomCenter + 1 + j);
        }

        return new Mesh(vertices, indices);
    }

    public static Mesh Torus(float majorRadius, float minorRadius, int slices, int stacks)
    {
        int columns = slices + 1;
        int rows = stacks + 1;
        var vertices = new float[rows * columns * Mesh.FloatsPerVertex];
        var indices = new uint[stacks * slices * 6];

        for (int i = 0; i < rows; i++)
        {
            float v = (float)i / stacks;
            float tubeAngle = v * 2.0f * MathF.PI;
            float ringRadius = majorRadius + minorRadius * MathF.Cos(tubeAngle);
            float y = minorRadius * MathF.Sin(tubeAngle);

            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / slices;
                float angle = u * 2.0f * MathF.PI;

                float x = ringRadius * MathF.Cos(angle);
                float z = -ringRadius * MathF.Sin(angle);

                SetVertex(vertices, i * columns + j, x, y, z, u, v, 0.35f);
            }
        }

        WriteGridIndices(indices, 0, 0, stacks, slices);

        return new Mesh(vertices, indices);
    }

    private static int WriteGridIndices(uint[] indices, int k, int firstVertex, int stacks, int slices)
    {
        int columns = slices + 1;
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                uint bottomLeft = (uint)(firstVertex + i * columns + j);
                uint bottomRight = bottomLeft + 1;
                uint topLeft = bottomLeft + (uint)columns;
                uint topRight = topLeft + 1;

                indices[k++] = bottomLeft;
                indices[k++] = bottomRight;
                indices[k++] = topRight;

                indices[k++] = topRight;
                indices[k++] = topLeft;
                indices[k++] = bottomLeft;
            }
        }
        return k;
    }

    private static void AddFace(float[] vertices, uint[] indices, int face,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color4 color)
    {
        int first = 4 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        SetVertex(vertices, first + 3, d, color);

        uint f = (uint)first;
        int k = 6 * face;
        indices[k] = f;         indices[k + 1] = f + 1; indices[k + 2] = f + 2;
        indices[k + 3] = f + 2; indices[k + 4] = f + 3; indices[k + 5] = f;
    }

    private static void AddTriangle(float[] vertices, uint[] indices, int face,
                                    Vector3 a, Vector3 b, Vector3 c, Color4 color)
    {
        int first = 3 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        indices[first] = (uint)first;
        indices[first + 1] = (uint)first + 1;
        indices[first + 2] = (uint)first + 2;
    }

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, float r, float g, float b)
    {
        int i = index * Mesh.FloatsPerVertex;
        vertices[i] = x;
        vertices[i + 1] = y;
        vertices[i + 2] = z;
        vertices[i + 3] = r;
        vertices[i + 4] = g;
        vertices[i + 5] = b;
    }

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color) =>
        SetVertex(vertices, index, x, y, z, color.R, color.G, color.B);

    private static void SetVertex(float[] vertices, int index, Vector3 p, Color4 color) =>
        SetVertex(vertices, index, p.X, p.Y, p.Z, color.R, color.G, color.B);
}
