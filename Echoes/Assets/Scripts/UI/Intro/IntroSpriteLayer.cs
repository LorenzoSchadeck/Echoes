using UnityEngine;
using UnityEngine.UIElements;

internal sealed class IntroSpriteLayer : VisualElement
{
    private Sprite source;
    private Vector2[] outline;
    private readonly Vector2[] rectangle = new Vector2[4];

    public IntroSpriteLayer()
    {
        pickingMode = PickingMode.Ignore;
        style.position = Position.Absolute;
        style.left = 0;
        style.top = 0;
        generateVisualContent += Draw;
    }

    public void SetRectangle(Sprite sprite, Rect region)
    {
        if (source == sprite && outline == rectangle && rectangle[0] == region.min && rectangle[2] == region.max) return;
        rectangle[0] = region.min;
        rectangle[1] = new Vector2(region.xMax, region.yMin);
        rectangle[2] = region.max;
        rectangle[3] = new Vector2(region.xMin, region.yMax);
        SetPolygon(sprite, rectangle);
        MarkDirtyRepaint();
    }

    public void SetPolygon(Sprite sprite, Vector2[] points)
    {
        if (source == sprite && outline == points) return;
        source = sprite;
        outline = points;
        MarkDirtyRepaint();
    }

    private void Draw(MeshGenerationContext context)
    {
        if (source == null || outline == null || outline.Length < 3 || contentRect.width <= 0 || contentRect.height <= 0) return;
        var texture = source.texture;
        Rect spriteRect = source.packed ? source.textureRect : source.rect;
        var mesh = context.Allocate(outline.Length + 1, outline.Length * 3, texture);
        Vector2 center = Vector2.zero;
        foreach (Vector2 point in outline) center += point;
        center /= outline.Length;

        void AddVertex(Vector2 point)
        {
            float u = (spriteRect.x + point.x * spriteRect.width) / texture.width;
            float v = (spriteRect.y + (1f - point.y) * spriteRect.height) / texture.height;
            mesh.SetNextVertex(new Vertex
            {
                position = new Vector3(point.x * contentRect.width, point.y * contentRect.height, Vertex.nearZ),
                tint = Color.white,
                uv = new Vector2(u, v)
            });
        }

        AddVertex(center);
        foreach (Vector2 point in outline) AddVertex(point);
        for (int i = 0; i < outline.Length; i++)
        {
            mesh.SetNextIndex(0);
            mesh.SetNextIndex((ushort)(i + 1));
            mesh.SetNextIndex((ushort)((i + 1) % outline.Length + 1));
        }
    }
}
