using UnityEngine;
using UnityEngine.UIElements;

/// <summary>Textura discreta e margem gasta do caderno; desenhada apenas quando o painel repinta.</summary>
public class BoardTrayBackdrop : VisualElement
{
    public BoardTrayBackdrop()
    {
        pickingMode = PickingMode.Ignore;
        AddToClassList("archive-backdrop");
        generateVisualContent += Draw;
    }

    private void Draw(MeshGenerationContext context)
    {
        float width = contentRect.width, height = contentRect.height;
        if (width < 1 || height < 1) return;
        var p = context.painter2D;
        uint seed = 4619;
        p.strokeColor = new Color(.82f, .78f, .66f, .035f);
        p.lineWidth = .5f;
        p.BeginPath();
        for (int i = 0; i < 1000; i++)
        {
            float x = Next(ref seed) * width, y = Next(ref seed) * height;
            p.MoveTo(new Vector2(x,y));
            p.LineTo(new Vector2(x + .35f + Next(ref seed) * 1.4f,y));
        }
        p.Stroke();
        p.strokeColor = new Color(.63f,.34f,.23f,.18f);
        p.lineWidth = 1;
        p.BeginPath();
        for(float y = 0; y < height; y += 43)
        {
            float x = 18 + Next(ref seed) * .8f;
            p.MoveTo(new Vector2(x,y));
            p.LineTo(new Vector2(x,y+40));
        }
        p.Stroke();
        p.fillColor = new Color(.025f,.02f,.018f,.35f);
        p.BeginPath();p.MoveTo(Vector2.zero);
        for(float y=0;y<height;y+=13)p.LineTo(new Vector2(3+Next(ref seed)*3,y));
        p.LineTo(new Vector2(0,height));p.ClosePath();p.Fill();
    }
    private static float Next(ref uint seed)
    {
        seed = unchecked(seed * 1664525 + 1013904223);
        return (seed & 65535) / 65535f;
    }
}
