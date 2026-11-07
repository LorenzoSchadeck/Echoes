using System.Collections;
using FMODUnity;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Abertura do estúdio (cena PreLoad), num caderno de esboços: entra do preto, o Tinker Crow chega
/// voando, pousa no fio com o acessório do jogo no bico, o nome aparece em máquina de escrever e
/// tudo volta para o preto. Qualquer tecla pula. No fim, liga o componente seguinte (o aviso).
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class StudioIntro : MonoBehaviour
{
    [Header("Mascote")]
    [SerializeField] private CrowMascot mascot;
    [Tooltip("O item do corvo neste jogo (em Echoes, a chave do quarto 5).")]
    [SerializeField] private MascotAccessory accessory;

    [Header("Texto")]
    [SerializeField] private string studioName = "TINKER CROW";
    [SerializeField] private Font font;
    [SerializeField] private Color ink = new Color(0.1f, 0.095f, 0.09f);
    [SerializeField] private Color paper = new Color(0.925f, 0.902f, 0.855f);

    [Header("Tempo")]
    [SerializeField] private float fadeIn = 0.6f;
    [SerializeField] private float flight = 1.1f;
    [SerializeField] private float letterInterval = 0.08f;
    [SerializeField] private float hold = 1.6f;
    [SerializeField] private float fadeOut = 0.9f;

    [Header("Som")]
    [Tooltip("Só a máquina de escrever, uma batida por letra.")]
    [SerializeField] private EventReference typeSound;

    [Header("Traço de esboço")]
    [Tooltip("Granulado de papel que pisca de leve (0 = desligado).")]
    [SerializeField, Range(0f, 0.3f)] private float grain = 0.08f;
    [Tooltip("Quantas vezes por segundo o granulado muda.")]
    [SerializeField] private float grainFps = 12f;

    [Header("Depois")]
    [Tooltip("Ligado quando a abertura termina (os avisos da cena PreLoad).")]
    [SerializeField] private Behaviour startAfter;

    private VisualElement root, stage, wire, crow, item, underline, grainLayer, black;
    private Label title;
    private Texture2D grainTexture;
    private Color32[] grainPixels;
    private float nextGrainAt;
    private float underlineStart = -1f;

    private enum Phase { Waiting, Flying, Landing, Idle, Done }
    private Phase phase = Phase.Waiting;
    private float phaseTime;
    private float landedAt = -1f;
    private bool skip;
    private bool finished;

    // Layout (em pixels do painel)
    private float W, H, crowSize, wireY, crowX, underlineWidth;

    private void OnEnable()
    {
        root = GetComponent<UIDocument>().rootVisualElement;
        root.Clear();
        root.style.position = Position.Absolute;
        root.style.left = 0; root.style.top = 0; root.style.right = 0; root.style.bottom = 0;
        root.pickingMode = PickingMode.Ignore;

        stage = Box(root, paper);
        stage.style.right = 0; stage.style.bottom = 0;

        // Textura de papel (fibras e manchas) e hachuras a lápis que escurecem as bordas
        Layer(stage, PaperTexture(), BackgroundSizeType.Cover);
        Layer(stage, HatchVignetteTexture(), BackgroundSizeType.Cover);

        // O fio é um traço a lápis, irregular, em vez de uma linha perfeita
        wire = Box(stage, Color.clear);
        wire.style.right = 0;
        wire.style.height = 12;
        wire.style.backgroundImage = new StyleBackground(StrokeTexture(2048, 24, 3, 7));
        wire.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);

        crow = Box(stage, Color.clear);
        item = Box(crow, Color.clear);

        title = new Label("");
        title.style.position = Position.Absolute;
        title.style.left = 0; title.style.right = 0;
        title.style.unityTextAlign = TextAnchor.UpperCenter;
        title.style.color = ink;
        if (font != null) title.style.unityFontDefinition = FontDefinition.FromFont(font);
        title.pickingMode = PickingMode.Ignore;
        stage.Add(title);

        // Sublinhado a lápis, riscado depois do nome
        underline = Box(stage, Color.clear);
        underline.style.backgroundImage = new StyleBackground(StrokeTexture(1024, 24, 2, 11));
        underline.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Cover);
        underline.style.width = 0;

        grainTexture = new Texture2D(240, 135, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        grainPixels = new Color32[grainTexture.width * grainTexture.height];
        FillGrain();
        grainLayer = Layer(root, grainTexture, BackgroundSizeType.Cover);
        grainLayer.style.opacity = grain;

        black = Box(root, Color.black);
        black.style.right = 0; black.style.bottom = 0;

        StartCoroutine(Run());
    }

    private void OnDisable()
    {
        if (grainTexture != null) Destroy(grainTexture);
    }

    private static VisualElement Layer(VisualElement parent, Texture2D texture, BackgroundSizeType size)
    {
        VisualElement layer = Box(parent, Color.clear);
        layer.style.right = 0;
        layer.style.bottom = 0;
        layer.style.backgroundImage = new StyleBackground(texture);
        layer.style.backgroundSize = new BackgroundSize(size);
        return layer;
    }

    private static VisualElement Box(VisualElement parent, Color color)
    {
        var element = new VisualElement { pickingMode = PickingMode.Ignore };
        element.style.position = Position.Absolute;
        element.style.left = 0;
        element.style.top = 0;
        element.style.backgroundColor = color;
        parent.Add(element);
        return element;
    }

    private IEnumerator Run()
    {
        // Espera o painel ter tamanho
        yield return null;
        while (root.layout.width <= 1f || float.IsNaN(root.layout.width)) yield return null;
        Layout();

        yield return FadeBlack(0f, fadeIn);

        SetPhase(Phase.Flying);
        yield return WaitOrSkip(flight);

        SetPhase(Phase.Landing);
        landedAt = Time.time;
        yield return WaitOrSkip(mascot != null ? mascot.land.Duration : 0.3f);

        SetPhase(Phase.Idle);
        yield return WaitOrSkip(0.35f);

        foreach (char letter in studioName)
        {
            if (skip) break;
            title.text += letter;
            if (!char.IsWhiteSpace(letter)) Play(typeSound);
            yield return WaitOrSkip(letterInterval);
        }
        title.text = studioName;
        underlineStart = Time.unscaledTime;
        yield return WaitOrSkip(hold);

        // Volta para o preto (também quando o jogador pula)
        skip = false;
        yield return FadeBlack(1f, fadeOut);
        yield return new WaitForSecondsRealtime(0.3f);
        Finish();
    }

    private void Finish()
    {
        if (finished) return;
        finished = true;
        SetPhase(Phase.Done);
        root.style.display = DisplayStyle.None;
        if (startAfter != null) startAfter.enabled = true;
    }

    private void Update()
    {
        if (finished) return;
        if (!skip && phase != Phase.Waiting && AnyInput()) skip = true;

        if (W <= 1f) return;
        if (Mathf.Abs(root.layout.width - W) > 1f || Mathf.Abs(root.layout.height - H) > 1f) Layout();
        phaseTime += Time.unscaledDeltaTime;
        Animate();

        // Granulado do papel, trocando poucas vezes por segundo (como película antiga)
        if (grain > 0f && Time.unscaledTime >= nextGrainAt)
        {
            nextGrainAt = Time.unscaledTime + 1f / Mathf.Max(1f, grainFps);
            FillGrain();
        }

        // O sublinhado é riscado da esquerda para a direita
        if (underlineStart >= 0f)
        {
            float k = Mathf.Clamp01((Time.unscaledTime - underlineStart) / 0.35f);
            underline.style.width = underlineWidth * (1f - (1f - k) * (1f - k));
        }
    }

    private void Layout()
    {
        W = root.layout.width;
        H = root.layout.height;
        crowSize = H * 0.62f;
        wireY = H * 0.62f;
        crowX = W * 0.5f - crowSize * 0.5f;
        crow.style.width = crowSize;
        crow.style.height = crowSize;
        wire.style.top = wireY - 6;
        title.style.top = wireY + H * 0.08f;
        title.style.fontSize = H * 0.055f;
        title.style.letterSpacing = H * 0.012f;
        underlineWidth = H * 0.42f;
        underline.style.left = W * 0.5f - underlineWidth * 0.5f;
        underline.style.top = wireY + H * 0.08f + H * 0.075f;
        underline.style.height = Mathf.Max(8f, H * 0.012f);
        if (accessory != null)
        {
            float itemSize = crowSize * accessory.size;
            item.style.width = itemSize;
            item.style.height = itemSize;
            item.style.backgroundImage = new StyleBackground(accessory.sprite);
            item.style.transformOrigin = new TransformOrigin(Length.Percent(accessory.attachPoint.x * 100f), Length.Percent(accessory.attachPoint.y * 100f));
        }
        else item.style.display = DisplayStyle.None;
    }

    private void SetPhase(Phase next)
    {
        phase = next;
        phaseTime = 0f;
    }

    private void Animate()
    {
        if (mascot == null) return;

        MascotFrame frame;
        float x = crowX, y;
        float shake = 0f;

        switch (phase)
        {
            case Phase.Waiting:
                crow.style.display = DisplayStyle.None;
                return;
            case Phase.Flying:
            {
                frame = mascot.arrive.FrameAt(phaseTime);
                float t = Mathf.Clamp01(phaseTime / Mathf.Max(0.1f, flight));
                float ease = 1f - (1f - t) * (1f - t);
                float landedTop = wireY - (frame != null ? frame.wire : 0.8f) * crowSize;
                x = Mathf.Lerp(-crowSize * 1.1f, crowX, ease);
                y = Mathf.Lerp(landedTop - H * 0.5f, landedTop, ease) + Mathf.Sin(phaseTime * 14f) * H * 0.006f * (1f - t);
                break;
            }
            case Phase.Landing:
                frame = mascot.land.FrameAt(phaseTime);
                y = wireY - (frame != null ? frame.wire : 0.8f) * crowSize;
                break;
            default:
                frame = mascot.idle.FrameAt(phaseTime);
                y = wireY - (frame != null ? frame.wire : 0.8f) * crowSize;
                break;
        }

        // O fio afunda e balança quando o corvo pousa
        if (landedAt >= 0f)
        {
            float s = Time.time - landedAt;
            shake = H * 0.006f * Mathf.Exp(-7f * s) * Mathf.Sin(s * 24f);
        }
        wire.style.top = wireY - 6 + shake;

        crow.style.display = DisplayStyle.Flex;
        crow.style.left = x;
        crow.style.top = y + shake;
        if (frame?.sprite != null) crow.style.backgroundImage = new StyleBackground(frame.sprite);

        if (accessory != null && frame != null)
        {
            float itemSize = crowSize * accessory.size;
            item.style.left = frame.beak.x * crowSize - accessory.attachPoint.x * itemSize;
            item.style.top = frame.beak.y * crowSize - accessory.attachPoint.y * itemSize;
            float angle = 0f;
            if (accessory.swings)
            {
                angle = landedAt >= 0f
                    ? 12f * Mathf.Exp(-3.5f * (Time.time - landedAt)) * Mathf.Sin((Time.time - landedAt) * 7f)
                    : Mathf.Sin(phaseTime * 14f) * 6f;
            }
            item.style.rotate = new Rotate(new Angle(angle, AngleUnit.Degree));
        }
    }

    private IEnumerator FadeBlack(float to, float duration)
    {
        float from = black.resolvedStyle.opacity;
        for (float t = 0f; t < duration && !skip; t += Time.unscaledDeltaTime)
        {
            black.style.opacity = Mathf.Lerp(from, to, t / duration);
            yield return null;
        }
        black.style.opacity = skip ? 1f : to;
    }

    private IEnumerator WaitOrSkip(float seconds)
    {
        for (float t = 0f; t < seconds && !skip; t += Time.unscaledDeltaTime) yield return null;
    }

    // ---------- Texturas de esboço (geradas, sem arquivo) ----------

    private void FillGrain()
    {
        if (grainTexture == null) return;
        for (int i = 0; i < grainPixels.Length; i++)
        {
            byte v = (byte)Random.Range(0, 70);
            grainPixels[i] = new Color32(v, (byte)(v * 0.9f), (byte)(v * 0.8f), (byte)Random.Range(0, 200));
        }
        grainTexture.SetPixels32(grainPixels);
        grainTexture.Apply(false);
    }

    // Papel: fibras finas e manchas grandes bem suaves
    private static Texture2D PaperTexture()
    {
        const int w = 512, h = 288;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        var random = new System.Random(7);
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float blotch = Mathf.PerlinNoise(x * 0.012f, y * 0.012f) * 0.6f + Mathf.PerlinNoise(x * 0.05f + 9f, y * 0.05f) * 0.4f;
            float fiber = Mathf.PerlinNoise(x * 0.9f, y * 0.06f);
            float a = Mathf.Clamp01((blotch - 0.45f) * 0.35f) + Mathf.Clamp01((fiber - 0.7f) * 0.5f) + (float)random.NextDouble() * 0.04f;
            pixels[y * w + x] = new Color32(70, 55, 40, (byte)(Mathf.Clamp01(a) * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }

    // Hachuras diagonais a lápis só nas beiradas, cruzando de leve nos cantos
    private static Texture2D HatchVignetteTexture()
    {
        const int w = 960, h = 540;
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[w * h];
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w - 0.5f, v = (y + 0.5f) / h - 0.5f;
            float d = Mathf.Sqrt(u * u * 1.1f + v * v * 2.4f);
            float edge = Smooth(0.42f, 0.78f, d);
            float corner = Smooth(0.62f, 0.9f, d);
            float wobble = Mathf.PerlinNoise(x * 0.03f, y * 0.03f) * 4f;
            // linhas suaves (1 px com borda macia) para não serrilhar ao esticar
            float lineA = 1f - Mathf.Clamp01(Mathf.Abs(Mathf.Repeat(x + y + wobble, 13f) - 6.5f) - 0.3f);
            float lineB = 1f - Mathf.Clamp01(Mathf.Abs(Mathf.Repeat(x - y + wobble, 17f) - 8.5f) - 0.3f);
            // cada traço tem começo e fim, como riscos à mão
            float breaksA = Smooth(0.38f, 0.5f, Mathf.PerlinNoise((x + y) * 0.004f + 3f, (x - y) * 0.06f));
            float breaksB = Smooth(0.42f, 0.55f, Mathf.PerlinNoise((x - y) * 0.004f + 7f, (x + y) * 0.06f));
            float strokes = lineA * breaksA * edge + lineB * breaksB * corner * 0.8f;
            float a = Mathf.Clamp01(strokes * 0.32f + edge * 0.07f);
            pixels[y * w + x] = new Color32(35, 28, 22, (byte)(a * 255f));
        }
        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }

    // smoothstep de shader (o Mathf.SmoothStep da Unity interpola entre dois valores, não é isto)
    private static float Smooth(float edge0, float edge1, float x)
    {
        float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
        return t * t * (3f - 2f * t);
    }

    // Traço a lápis: algumas passadas finas e tortas, com falhas, umas sobre as outras
    private static Texture2D StrokeTexture(int w, int h, int passes, int seed)
    {
        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var alpha = new float[w * h];
        var random = new System.Random(seed);
        for (int pass = 0; pass < passes; pass++)
        {
            float offset = (float)random.NextDouble() * 100f;
            float baseY = h * 0.5f + ((float)random.NextDouble() - 0.5f) * h * 0.25f;
            float start = (float)random.NextDouble() * w * 0.04f, end = w - (float)random.NextDouble() * w * 0.04f;
            for (int x = 0; x < w; x++)
            {
                if (x < start || x > end) continue;
                float y = baseY + (Mathf.PerlinNoise(x * 0.004f, offset) - 0.5f) * h * 0.35f;
                float thickness = 0.8f + Mathf.PerlinNoise(x * 0.02f, offset + 5f) * 1.6f;
                float pressure = Mathf.Clamp01(Mathf.PerlinNoise(x * 0.01f, offset + 11f) * 1.6f) * (pass == 0 ? 1f : 0.6f);
                for (int py = 0; py < h; py++)
                {
                    float dist = Mathf.Abs(py - y);
                    if (dist > thickness + 1f) continue;
                    float grainy = random.NextDouble() > 0.25 ? 1f : 0.4f;
                    float a = Mathf.Clamp01(thickness + 0.5f - dist) * pressure * grainy;
                    alpha[py * w + x] = Mathf.Max(alpha[py * w + x], a);
                }
            }
        }
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(26, 24, 22, (byte)(alpha[i] * 235f));
        texture.SetPixels32(pixels);
        texture.Apply(false);
        return texture;
    }

    private static void Play(EventReference sound)
    {
        if (!sound.IsNull) RuntimeManager.PlayOneShot(sound);
    }

    private static bool AnyInput()
    {
        if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) return true;
        if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame)) return true;
        if (Gamepad.current != null)
            foreach (var control in Gamepad.current.allControls)
                if (control is UnityEngine.InputSystem.Controls.ButtonControl button && button.wasPressedThisFrame) return true;
        return false;
    }

#if UNITY_EDITOR
    public void EditorSetup(CrowMascot mascotAsset, MascotAccessory accessoryAsset, Font typeFont, Behaviour after, EventReference type)
    {
        mascot = mascotAsset;
        accessory = accessoryAsset;
        font = typeFont;
        startAfter = after;
        typeSound = type;
    }
#endif
}
