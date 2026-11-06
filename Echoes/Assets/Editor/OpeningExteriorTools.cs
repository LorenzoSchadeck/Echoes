using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Cidade à noite vista pela janela do quarto 6 (Abertura1995): rua molhada, prédios do outro lado
/// com janelas acesas, silhuetas mais altas atrás, postes de sódio e camadas de neblina que escondem
/// o fundo. Só refaz o grupo Environment/Exterior: o resto da cena (editado à mão) não é tocado.
/// Escala do projeto: 4,8 unidades por metro. A rua fica em y = -18 (segundo andar).
/// </summary>
public static class OpeningExteriorTools
{
    private const string MaterialFolder = "Assets/Materials/Abertura";
    private const string TextureFolder = "Assets/Textures/Abertura";
    private const string MeshPath = "Assets/Models/Abertura/Geometria Exterior.asset";

    private const float Street = -18f;          // nível da rua
    private const float OurFacade = 82.7f;      // face externa da parede da janela
    private const float FacingRow = 128f;       // fachadas do outro lado da rua (~9,5 m)
    private const float SkylineRow = 185f;      // prédios altos ao fundo
    private const float WindowTile = 61f;       // um ladrilho de textura = 4 janelas x 4 andares

    // Brilho de cidade na neblina: sódio apagado, quase marrom
    public static readonly Color CityHaze = new Color(0.16f, 0.11f, 0.075f);

    [MenuItem("Echoes/Roteiro/Abertura 1995: montar exterior (cidade)")]
    private static void BuildMenu()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/Abertura1995.unity")
        {
            Debug.LogWarning("[OpeningExteriorTools] Abra a cena Abertura1995 primeiro.");
            return;
        }
        Build(scene);
    }

    public static void Build(Scene scene)
    {
        var random = new System.Random(1995);
        GameObject environment = scene.GetRootGameObjects().First(r => r.name == "Environment");
        Transform old = environment.transform.Find("Exterior");
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var root = new GameObject("Exterior").transform;
        root.SetParent(environment.transform, false);

        ResetMeshes();
        // Fachadas sem iluminação: parede escura e janelas acesas pintadas na mesma textura (leve e
        // independente das luzes da cena); a neblina da cena ainda as engole com a distância
        Texture2D night = NightFacadeTexture(), noise = FogTexture();
        Material facade = UnlitTextured("Fachada cidade", night, new Color(1f, 1f, 1f));
        Material skyline = UnlitTextured("Silhueta cidade", night, new Color(0.55f, 0.5f, 0.5f));
        Material asphalt = Lit("Asfalto molhado", null, 0.82f, new Color(0.035f, 0.035f, 0.04f));
        Material dark = Lit("Metal telhado", null, 0.3f, new Color(0.05f, 0.05f, 0.055f));
        // Sem brilho forte: a correção de cor da cena deixa os tons quentes estourados azulados
        Material lamp = Unlit("Lampada sodio", new Color(0.42f, 0.24f, 0.08f));
        Material sky = Unlit("Ceu de cidade", CityHaze * 1.15f);
        Material fog = FogMaterial(noise);

        // ---------- Rua molhada e calçada ----------
        Quad("Rua", root, new Vector3(0f, Street, 140f), new Vector2(260f, 120f), Quaternion.Euler(90f, 0f, 0f), asphalt, Vector2.zero, 1f);

        // ---------- Fachadas do outro lado da rua (3 a 5 andares) ----------
        Transform facing = Group("Predios_Frente", root);
        float x = -110f;
        while (x < 110f)
        {
            float width = Range(random, 22f, 42f);
            float top = Street + Range(random, 46f, 82f);
            Vector2 offset = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
            GameObject front = Quad("Fachada", facing, new Vector3(x + width * 0.5f, (Street + top) * 0.5f, FacingRow), new Vector2(width, top - Street), Quaternion.identity, facade, offset, WindowTile);
            // Caixa d'água ou casa de máquinas no telhado, em silhueta
            if (random.NextDouble() < 0.55)
            {
                GameObject tank = Primitive(random.NextDouble() < 0.6 ? PrimitiveType.Cylinder : PrimitiveType.Cube, "Telhado", facing, dark);
                float size = Range(random, 4f, 7f);
                tank.transform.position = new Vector3(x + width * Range(random, 0.25f, 0.75f), top + size * 0.6f, FacingRow + 6f);
                tank.transform.localScale = new Vector3(size, size * 0.6f, size);
            }
            x += width + Range(random, 0f, 3f);
        }

        // ---------- Prédios altos ao fundo, quase só silhueta ----------
        Transform far = Group("Predios_Fundo", root);
        x = -150f;
        while (x < 150f)
        {
            float width = Range(random, 26f, 55f);
            float top = Street + Range(random, 90f, 190f);
            Vector2 offset = new Vector2((float)random.NextDouble(), (float)random.NextDouble());
            Quad("Torre", far, new Vector3(x + width * 0.5f, (Street + top) * 0.5f, SkylineRow + Range(random, -10f, 25f)), new Vector2(width, top - Street), Quaternion.identity, skyline, offset, WindowTile);
            if (random.NextDouble() < 0.35)
            {
                // Antena com luz de aviso
                GameObject mast = Primitive(PrimitiveType.Cube, "Antena", far, dark);
                mast.transform.position = new Vector3(x + width * 0.5f, top + 9f, SkylineRow);
                mast.transform.localScale = new Vector3(0.6f, 18f, 0.6f);
                GameObject beacon = Primitive(PrimitiveType.Sphere, "Luz_Aviso", far, Unlit("Luz aviso", new Color(0.4f, 0.03f, 0.02f)));
                beacon.transform.position = mast.transform.position + Vector3.up * 9.4f;
                beacon.transform.localScale = Vector3.one * 1.4f;
            }
            x += width + Range(random, -4f, 4f);
        }

        // ---------- Céu: o brilho alaranjado da cidade nas nuvens baixas ----------
        Quad("Ceu", root, new Vector3(0f, 60f, 300f), new Vector2(900f, 500f), Quaternion.identity, sky, Vector2.zero, 1f)
            .GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // ---------- Postes de sódio: dois na nossa calçada, três do outro lado ----------
        Transform lamps = Group("Postes", root);
        void Post(float px, float pz, float intensity)
        {
            Transform post = Group("Poste", lamps);
            post.position = new Vector3(px, Street, pz);
            GameObject pole = Primitive(PrimitiveType.Cylinder, "Haste", post, dark);
            pole.transform.localPosition = new Vector3(0f, 14f, 0f);
            pole.transform.localScale = new Vector3(0.7f, 14f, 0.7f);
            GameObject head = Primitive(PrimitiveType.Sphere, "Lampada", post, lamp);
            head.transform.localPosition = new Vector3(0f, 28.5f, 0f);
            head.transform.localScale = new Vector3(2.2f, 1.1f, 2.2f);
            var lightObject = new GameObject("Luz");
            lightObject.transform.SetParent(post, false);
            lightObject.transform.localPosition = new Vector3(0f, 27f, 0f);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.58f, 0.25f);
            light.intensity = intensity;
            light.range = 48f;
            light.shadows = LightShadows.None;
            light.lightmapBakeType = LightmapBakeType.Realtime;
        }
        Post(-22f, 93f, 55f);
        Post(30f, 93f, 55f);
        Post(-48f, 122f, 40f);
        Post(6f, 122f, 40f);
        Post(58f, 122f, 40f);

        // ---------- Neblina: camadas que engrossam com a distância ----------
        // Da janela ainda se vê a escada de incêndio e a rua logo abaixo; as fachadas viram janelas
        // acesas no nevoeiro, e o fundo é só silhueta e brilho
        Transform fogRoot = Group("Neblina", root);
        float[] depths = { 97f, 112f, 140f, 165f, 215f, 260f };
        float[] alphas = { 0.22f, 0.3f, 0.38f, 0.5f, 0.7f, 0.85f };
        for (int i = 0; i < depths.Length; i++)
        {
            Vector2 offset = new Vector2(i * 0.37f, i * 0.21f);
            GameObject card = Quad($"Camada_{i + 1}", fogRoot, new Vector3(0f, 40f, depths[i]), new Vector2(520f, 260f), Quaternion.identity, fog, offset, 140f);
            var block = new MaterialPropertyBlock();
            Color tint = CityHaze * 1.35f;
            tint.a = alphas[i];
            block.SetColor("_BaseColor", tint);
            var renderer = card.GetComponent<MeshRenderer>();
            renderer.SetPropertyBlock(block);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // Nada lá fora entra na luz assada: só batching (a cidade é fundo, não precisa de lightmap)
        foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        // A neblina da cena passa a ter a cor da cidade: o que fica longe some no brilho, não no preto
        SceneManager.SetActiveScene(scene);
        RenderSettings.fogColor = CityHaze;

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[OpeningExteriorTools] Exterior da abertura montado.");
    }

    // =====================================================================

    private static float Range(System.Random random, float min, float max) => min + (float)random.NextDouble() * (max - min);

    private static Transform Group(string name, Transform parent)
    {
        var group = new GameObject(name).transform;
        group.SetParent(parent, false);
        return group;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(primitive.GetComponent<Collider>());
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        return primitive;
    }

    private static void ResetMeshes()
    {
        if (File.Exists(MeshPath)) AssetDatabase.DeleteAsset(MeshPath);
    }

    // Plano virado para -Z (para o quarto), com UV em ladrilhos do tamanho dado e deslocamento próprio
    private static GameObject Quad(string name, Transform parent, Vector3 center, Vector2 size, Quaternion rotation, Material material, Vector2 offset, float tile)
    {
        var mesh = new Mesh { name = name };
        float hx = size.x * 0.5f, hy = size.y * 0.5f;
        mesh.vertices = new[] { new Vector3(-hx, -hy, 0f), new Vector3(hx, -hy, 0f), new Vector3(-hx, hy, 0f), new Vector3(hx, hy, 0f) };
        float u = size.x / tile, v = size.y / tile;
        mesh.uv = new[] { offset, offset + new Vector2(u, 0f), offset + new Vector2(0f, v), offset + new Vector2(u, v) };
        mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
        mesh.normals = Enumerable.Repeat(Vector3.back, 4).ToArray();
        mesh.tangents = Enumerable.Repeat(new Vector4(1f, 0f, 0f, 1f), 4).ToArray();
        mesh.RecalculateBounds();
        if (!File.Exists(MeshPath)) AssetDatabase.CreateAsset(mesh, MeshPath);
        else AssetDatabase.AddObjectToAsset(mesh, MeshPath);

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.SetPositionAndRotation(center, rotation);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
        return go;
    }

    // ---------- Materiais ----------

    private static Material Get(string name, string shader)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        else if (material.shader.name != shader) material.shader = Shader.Find(shader);
        return material;
    }

    private static Material Lit(string name, Texture2D baseMap, float smoothness, Color tint)
    {
        Material material = Get(name, "Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", baseMap);
        material.SetColor("_BaseColor", tint);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material Unlit(string name, Color color)
    {
        Material material = Get(name, "Universal Render Pipeline/Unlit");
        material.SetColor("_BaseColor", color);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material UnlitTextured(string name, Texture2D texture, Color tint)
    {
        Material material = Get(name, "Universal Render Pipeline/Unlit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", tint);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material FogMaterial(Texture2D noise)
    {
        Material material = Get("Neblina cidade", "Universal Render Pipeline/Particles/Unlit");
        material.SetTexture("_BaseMap", noise);
        material.SetColor("_BaseColor", CityHaze);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    // ---------- Texturas geradas ----------

    // Noite: parede quase preta com grão, caixilhos um pouco mais claros e as janelas acesas
    private static Texture2D NightFacadeTexture()
    {
        var random = new System.Random(7);
        var states = new Color[16];
        for (int i = 0; i < states.Length; i++)
        {
            double r = random.NextDouble();
            // Valores baixos de propósito: a correção de cor da cena puxa brilhos fortes para o ciano
            states[i] = r < 0.24 ? new Color(0.36f, 0.21f, 0.09f) * (0.75f + (float)random.NextDouble() * 0.35f)
                : r < 0.32 ? new Color(0.1f, 0.14f, 0.24f)
                : r < 0.38 ? new Color(0.2f, 0.16f, 0.1f)
                : new Color(0.025f, 0.027f, 0.03f);
        }
        return Generate("fachada_cidade_noite", 512, (u, v, wx, wy, cell) =>
        {
            float grime = 0.7f + 0.3f * Mathf.PerlinNoise(u * 11f, v * 11f);
            if (cell.inWindow)
            {
                Color c = states[cell.index];
                if (cell.index % 3 == 0 && cell.localV > 0.55f) c *= 0.4f; // cortina meio fechada
                return c;
            }
            if (cell.inFrame) return new Color(0.05f, 0.045f, 0.04f);
            return new Color(0.035f, 0.028f, 0.025f) * grime;
        });
    }

    private struct Cell { public bool inWindow, inFrame; public int index; public float localV; }

    private static Texture2D Generate(string name, int size, System.Func<float, float, int, int, Cell, Color> paint)
    {
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (x + 0.5f) / size, v = (y + 0.5f) / size;
            int wx = Mathf.FloorToInt(u * 4f), wy = Mathf.FloorToInt(v * 4f);
            float lu = u * 4f - wx, lv = v * 4f - wy;
            var cell = new Cell
            {
                inWindow = lu > 0.3f && lu < 0.7f && lv > 0.28f && lv < 0.78f,
                inFrame = lu > 0.27f && lu < 0.73f && lv > 0.25f && lv < 0.81f,
                index = wy * 4 + wx,
                localV = Mathf.InverseLerp(0.28f, 0.78f, lv),
            };
            pixels[y * size + x] = paint(u, v, wx, wy, cell);
        }
        return Save(name, size, size, pixels, TextureWrapMode.Repeat, false);
    }

    // Nuvens de neblina: densidade irregular para as camadas não parecerem placas
    private static Texture2D FogTexture()
    {
        const int size = 256;
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / size, v = (float)y / size;
            float n = 0f, amp = 0.5f, freq = 3f;
            for (int o = 0; o < 4; o++)
            {
                // Ruído que se repete nas bordas (ladrilho sem emenda)
                n += amp * Tileable(u, v, freq);
                freq *= 2f; amp *= 0.5f;
            }
            float alpha = Mathf.Clamp01(0.55f + (n - 0.45f) * 1.4f);
            pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
        }
        return Save("neblina_cidade", size, size, pixels, TextureWrapMode.Repeat, true);
    }

    private static float Tileable(float u, float v, float freq)
    {
        float a = Mathf.PerlinNoise(u * freq, v * freq);
        float b = Mathf.PerlinNoise((u - 1f) * freq, v * freq);
        float c = Mathf.PerlinNoise(u * freq, (v - 1f) * freq);
        float d = Mathf.PerlinNoise((u - 1f) * freq, (v - 1f) * freq);
        return Mathf.Lerp(Mathf.Lerp(a, b, u), Mathf.Lerp(c, d, u), v);
    }

    private static Texture2D Save(string name, int width, int height, Color32[] pixels, TextureWrapMode wrap, bool alpha)
    {
        string path = $"{TextureFolder}/{name}.png";
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.SetPixels32(pixels);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = wrap;
        importer.alphaIsTransparency = alpha;
        importer.maxTextureSize = width;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
}
