using System.Collections.Generic;
using System.IO;
using System.Linq;
using FMODUnity;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta a cena Abertura1995 (abertura da demo, Pensão Halley, 1995): corredor do segundo andar com
/// cinco portas e a escada, o quarto 6 com os modelos salvos, Clara, as pistas e o roteiro.
/// Jogador, HUD e sistemas são copiados da cena Game. Pode rodar de novo: refaz a cena inteira.
/// Escala do projeto: 4,8 unidades por metro.
/// </summary>
public static class OpeningSetupTools
{
    private const string ScenePath = "Assets/Scenes/Abertura1995.unity";
    private const string GameScenePath = "Assets/Scenes/Game.unity";
    private const string MaterialFolder = "Assets/Materials/Abertura";
    private const string TextureFolder = "Assets/Textures/Abertura";
    private const string GeometryPath = "Assets/Models/Abertura/Geometria Abertura.asset";

    // ---------- Planta (unidades do mundo) ----------
    private const float CeilingY = 13f;
    private const float WallThickness = 0.7f;
    private const float HallHalfWidth = 3.85f;     // corredor de 1,6 m
    private const float HallEnd = 62f;             // parede da porta 6
    private const float StairsEnd = -14f;          // fundo da caixa da escada
    private const float DoorWidth = 5.55f;         // mesma porta do apartamento
    private const float DoorHeight = 9.84f;
    private const float RoomMinX = -8.5f, RoomMaxX = 8.5f;
    private const float RoomMinZ = HallEnd + WallThickness, RoomMaxZ = 82f;
    private const float WindowX = 1.5f, WindowWidth = 4.3f, WindowSill = 4.1f, WindowTop = 10.8f;

    // Portas do corredor: número, lado (-1 esquerda, +1 direita) e posição ao longo do corredor
    private static readonly (int number, int side, float z)[] HallDoors =
    {
        (1, -1, 12f), (2, 1, 21f), (3, -1, 30f), (4, 1, 40f), (5, -1, 50f),
    };

    private static readonly Dictionary<string, Mesh> boxMeshes = new Dictionary<string, Mesh>();

    [MenuItem("Echoes/Roteiro/Montar abertura 1995")]
    private static void Build()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.LogWarning("[OpeningSetupTools] Saia do play mode antes de montar a abertura.");
            return;
        }
        // A cena agora é editada à mão (mobília e ajustes): remontar apaga tudo isso
        if (File.Exists(ScenePath) && !EditorUtility.DisplayDialog("Remontar abertura 1995",
                "Remontar refaz a cena Abertura1995 do zero e APAGA tudo o que foi colocado ou ajustado à mão nela (mobília, posições, luzes).\n\nRemontar mesmo assim?",
                "Remontar e apagar", "Cancelar"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ScriptSetupTools.ApplyOpeningTexts();

        Scene game = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        Scene opening = File.Exists(ScenePath)
            ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive)
            : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        foreach (GameObject root in opening.GetRootGameObjects()) Object.DestroyImmediate(root);
        SceneManager.SetActiveScene(opening);

        PrepareFolders();
        ResetGeometryAsset();
        var materials = new Materials();

        var roots = new Roots();
        PlayerRig player = CopySharedObjects(game, roots);
        BuildHall(roots, materials);
        BuildRoom(roots, materials, out RoomParts room);
        ClaraPresence clara = BuildClara(roots, player);
        BuildStory(roots, player, clara, room);
        ApplyLighting(roots, materials);

        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(opening, ScenePath);
        AddToBuildSettings();
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Debug.Log("[OpeningSetupTools] Abertura 1995 montada em " + ScenePath + ".");
    }

    // =====================================================================
    // Pastas, geometria e materiais
    // =====================================================================

    private static void PrepareFolders()
    {
        foreach (string folder in new[] { MaterialFolder, TextureFolder, Path.GetDirectoryName(GeometryPath).Replace('\\', '/') })
        {
            if (AssetDatabase.IsValidFolder(folder)) continue;
            string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }

    // As caixas da cena guardam a malha num único arquivo, refeito a cada montagem
    private static void ResetGeometryAsset()
    {
        boxMeshes.Clear();
        if (File.Exists(GeometryPath)) AssetDatabase.DeleteAsset(GeometryPath);
    }

    private static Mesh BoxMesh(Vector3 size, float tile)
    {
        string key = $"{size.x:F2}_{size.y:F2}_{size.z:F2}_{tile:F1}";
        if (boxMeshes.TryGetValue(key, out Mesh cached)) return cached;

        var vertices = new List<Vector3>();
        var normals = new List<Vector3>();
        var uvs = new List<Vector2>();
        var triangles = new List<int>();
        Vector3 half = size * 0.5f;

        // Cada face com u para a direita e v para cima, vistos de fora; UV em metros de textura
        void Face(Vector3 normal, Vector3 up)
        {
            Vector3 u = Vector3.Cross(up, -normal);
            float su = Mathf.Abs(Vector3.Dot(size, u));
            float sv = Mathf.Abs(Vector3.Dot(size, up));
            Vector3 center = Vector3.Scale(normal, half);
            Vector3 du = u * (su * 0.5f), dv = up * (sv * 0.5f);
            int start = vertices.Count;
            vertices.AddRange(new[] { center - du - dv, center + du - dv, center + du + dv, center - du + dv });
            for (int i = 0; i < 4; i++) normals.Add(normal);
            float uu = su / tile, vv = sv / tile;
            uvs.AddRange(new[] { new Vector2(0f, 0f), new Vector2(uu, 0f), new Vector2(uu, vv), new Vector2(0f, vv) });
            triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
        }

        Face(Vector3.right, Vector3.up);
        Face(Vector3.left, Vector3.up);
        Face(Vector3.forward, Vector3.up);
        Face(Vector3.back, Vector3.up);
        Face(Vector3.up, Vector3.forward);
        Face(Vector3.down, Vector3.forward);

        var mesh = new Mesh { name = "Caixa " + key };
        mesh.SetVertices(vertices);
        mesh.SetNormals(normals);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangles, 0);
        mesh.RecalculateTangents();
        mesh.RecalculateBounds();
        // UV de lightmap (canal 2): as UVs normais repetem a textura em metros e se sobrepõem
        Unwrapping.GenerateSecondaryUVSet(mesh);

        if (!File.Exists(GeometryPath)) AssetDatabase.CreateAsset(mesh, GeometryPath);
        else AssetDatabase.AddObjectToAsset(mesh, GeometryPath);
        boxMeshes[key] = mesh;
        return mesh;
    }

    private sealed class Materials
    {
        public readonly Material wallpaper, carpet, floorboards, ceiling, trim, doorWood, furniture, laminate,
            brass, sheet, bulbOn, bulbOff, darkMetal, cord, rain, stainDecal, dustDecal, shadowOnly, outside;

        public Materials()
        {
            // Texturas provisórias do projeto, tingidas; trocar pelas texturas da pensão quando chegarem
            wallpaper = Lit("Papel de parede pensao", "Assets/Textures/Wall/beige_wall_002_diff_4k.jpg", new Color(0.55f, 0.57f, 0.49f), 0.15f, "Assets/Textures/Wall/beige_wall_002_nor_gl_4k.exr");
            carpet = Lit("Carpete corredor", "Assets/Textures/Carpet/dirty_carpet_diff_4k.jpg", new Color(0.33f, 0.2f, 0.18f), 0.05f, "Assets/Textures/Carpet/dirty_carpet_nor_gl_4k.exr");
            floorboards = Lit("Assoalho quarto", "Assets/Textures/Wood/dark_wood_diff_4k.jpg", new Color(0.48f, 0.42f, 0.36f), 0.35f, "Assets/Textures/Wood/dark_wood_nor_gl_4k.exr");
            ceiling = Lit("Teto pensao", "Assets/Textures/Plaster/plastered_wall_04_diff_4k.jpg", new Color(0.62f, 0.6f, 0.54f), 0.1f, null);
            trim = Lit("Rodape e batentes", "Assets/Textures/Madeira/Wood_011_Base_Color.jpg", new Color(0.26f, 0.21f, 0.17f), 0.3f, null);
            doorWood = Lit("Porta pensao", "Assets/Textures/Madeira/Wood_011_Base_Color.jpg", new Color(0.3f, 0.26f, 0.22f), 0.15f, null);
            furniture = Lit("Movel provisorio", "Assets/Textures/Madeira/Wood_011_Base_Color.jpg", new Color(0.6f, 0.48f, 0.38f), 0.3f, null);
            laminate = Lit("Bancada formica", null, new Color(0.62f, 0.58f, 0.48f), 0.45f, null);
            brass = Lit("Latao", null, new Color(0.62f, 0.48f, 0.22f), 0.7f, null);
            brass.SetFloat("_Metallic", 1f);
            darkMetal = Lit("Metal escuro", null, new Color(0.12f, 0.12f, 0.12f), 0.5f, null);
            darkMetal.SetFloat("_Metallic", 0.8f);
            cord = Lit("Fio", null, new Color(0.08f, 0.07f, 0.06f), 0.3f, null);
            sheet = Lit("Lencol", "Assets/Textures/Cotton 2/fabrics_0081_color_2k.jpg", new Color(0.86f, 0.84f, 0.8f), 0.1f, null);
            outside = Lit("Fachada externa", "Assets/Textures/Plaster/plastered_wall_04_diff_4k.jpg", new Color(0.18f, 0.17f, 0.17f), 0.6f, null);

            bulbOff = BulbMaterial("Lampada apagada", false);
            bulbOn = BulbMaterial("Lampada acesa", true);
            rain = Particles("Chuva", new Color(0.7f, 0.75f, 0.85f, 0.28f));
            stainDecal = Decal("Mancha no lencol", Texture("mancha_lencol", DrawStain, new Color32(58, 10, 8, 255), 512));
            dustDecal = Decal("Poeira com marca do chaveiro", Texture("poeira_comoda", DrawDust, new Color32(150, 144, 132, 255), 512));
            shadowOnly = Lit("Sombra da Clara", null, Color.black, 0f, null);
        }

        private static Material Get(string name, string shader)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (Shader.Find(shader) == null)
            {
                Debug.LogWarning("[OpeningSetupTools] Shader não encontrado: " + shader);
                shader = "Universal Render Pipeline/Unlit";
            }
            if (material == null)
            {
                material = new Material(Shader.Find(shader));
                AssetDatabase.CreateAsset(material, path);
            }
            else if (material.shader.name != shader)
            {
                material.shader = Shader.Find(shader);
            }
            return material;
        }

        private static Material Lit(string name, string texturePath, Color tint, float smoothness, string normalPath)
        {
            Material material = Get(name, "Universal Render Pipeline/Lit");
            material.SetTexture("_BaseMap", texturePath != null ? AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath) : null);
            material.SetColor("_BaseColor", tint);
            material.SetFloat("_Smoothness", smoothness);
            material.SetFloat("_Metallic", 0f);

            Texture2D normal = null;
            if (normalPath != null && AssetImporter.GetAtPath(normalPath) is TextureImporter importer && importer.textureType == TextureImporterType.NormalMap)
                normal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            material.SetTexture("_BumpMap", normal);
            if (normal != null) material.EnableKeyword("_NORMALMAP");
            else material.DisableKeyword("_NORMALMAP");
            EditorUtility.SetDirty(material);
            return material;
        }

        // A lâmpada do abajur que você gerou, em duas versões: apagada e acesa
        private static Material BulbMaterial(string name, bool lit)
        {
            var source = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/table lamp/lamp/lamp.mat");
            string path = $"{MaterialFolder}/{name}.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(source);
                AssetDatabase.CreateAsset(material, path);
            }
            material.CopyPropertiesFromMaterial(source);
            if (lit)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", new Color(1f, 0.72f, 0.36f) * 6f);
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", Color.black);
            }
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Particles(string name, Color color)
        {
            Material material = Get(name, "Universal Render Pipeline/Particles/Unlit");
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static Material Decal(string name, Texture2D texture)
        {
            Material material = Get(name, "Shader Graphs/Decal");
            if (material.HasProperty("Base_Map")) material.SetTexture("Base_Map", texture);
            EditorUtility.SetDirty(material);
            return material;
        }
    }

    // Textura gerada: cor fixa e transparência desenhada pela função (u, v de 0 a 1)
    private static Texture2D Texture(string name, System.Func<float, float, float> alpha, Color32 color, int size)
    {
        string path = $"{TextureFolder}/{name}.png";
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                byte a = (byte)(Mathf.Clamp01(alpha((x + 0.5f) / size, (y + 0.5f) / size)) * 255f);
                pixels[y * size + x] = new Color32(color.r, color.g, color.b, a);
            }
        }
        texture.SetPixels32(pixels);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = size;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Mancha escura que se espalhou pelo tecido: mais forte no centro, bordas irregulares
    private static float DrawStain(float u, float v)
    {
        float du = u - 0.5f, dv = v - 0.5f;
        float angle = Mathf.Atan2(dv, du);
        float edge = 0.3f + 0.07f * Mathf.Sin(angle * 3f + 1.3f) + 0.05f * Mathf.Sin(angle * 7f) + 0.04f * (Mathf.PerlinNoise(u * 6f, v * 6f) - 0.5f);
        float d = Mathf.Sqrt(du * du + dv * dv) / edge;
        float body = Mathf.Clamp01((1f - d) * 3f);
        float fibers = Mathf.PerlinNoise(u * 40f, v * 40f) * 0.25f + 0.75f;
        return body * fibers * 0.92f;
    }

    // Poeira fina sobre a cômoda com um vazio no formato do chaveiro (gota alongada e argola)
    private static float DrawDust(float u, float v)
    {
        float dust = 0.32f + 0.25f * Mathf.PerlinNoise(u * 22f, v * 22f) + 0.1f * Mathf.PerlinNoise(u * 70f, v * 70f);
        float border = Mathf.SmoothStep(0f, 1f, Mathf.Min(Mathf.Min(u, 1f - u), Mathf.Min(v, 1f - v)) * 6f);
        // Chaveiro em forma de gota, deitado na diagonal, e a argola na ponta
        float x = (u - 0.5f) * 0.94f + (v - 0.5f) * 0.34f;
        float y = -(u - 0.5f) * 0.34f + (v - 0.5f) * 0.94f;
        float width = Mathf.Lerp(0.11f, 0.04f, Mathf.InverseLerp(-0.2f, 0.18f, y));
        bool inFob = y > -0.2f && y < 0.18f && Mathf.Abs(x) < width;
        float ring = Mathf.Abs(Mathf.Sqrt(x * x + Mathf.Pow(y - 0.23f, 2f)) - 0.045f);
        bool inRing = ring < 0.012f;
        float clean = inFob || inRing ? 0.05f : 1f;
        return dust * border * clean;
    }

    // =====================================================================
    // Objetos compartilhados com a cena Game
    // =====================================================================

    private sealed class Roots
    {
        public readonly Transform systems = Root("_Systems");
        public readonly Transform player = Root("Player");
        public readonly Transform ui = Root("UI");
        public readonly Transform environment = Root("Environment");
        public readonly Transform lighting = Root("Lighting");
        public readonly Transform interactables = Root("Interactables");
        public readonly Transform story = Root("Story");

        private static Transform Root(string name) => new GameObject(name).transform;
    }

    private sealed class PlayerRig
    {
        public GameObject root;
        public Rigidbody body;
        public Transform camera;
        public CinemachinePanTilt panTilt;
        public CustomInputAxisController lookInput;
        public EventReference footstep;
    }

    private static PlayerRig CopySharedObjects(Scene game, Roots roots)
    {
        // Copiar e colar, como Ctrl+C/Ctrl+V entre cenas: o jogador da cena Game tem câmeras e
        // componentes acrescentados à instância (câmera principal, olhar, sanidade), que o
        // Player.prefab sozinho não traz. A cópia continua ligada ao prefab.
        GameObject gamePlayer = FindInScene(game, "Player");
        Scene opening = SceneManager.GetActiveScene();
        Selection.objects = new Object[] { gamePlayer };
        Unsupported.CopyGameObjectsToPasteboard();
        Selection.objects = System.Array.Empty<Object>();
        SceneManager.SetActiveScene(opening);
        Unsupported.PasteGameObjectsFromPasteboard();
        GameObject player = Selection.activeGameObject;
        if (player == null || player == gamePlayer)
        {
            Debug.LogWarning("[OpeningSetupTools] A colagem do jogador falhou; usando uma cópia desligada do prefab.");
            player = Object.Instantiate(gamePlayer);
        }
        if (player.scene != opening) SceneManager.MoveGameObjectToScene(player, opening);
        Selection.objects = System.Array.Empty<Object>();
        player.name = "Player";
        player.transform.SetParent(roots.player, false);

        // Começa no alto da escada, olhando para o corredor (mesma altura em relação ao piso do apartamento)
        float heightAboveFloor = gamePlayer.transform.position.y - 0.17f;
        player.transform.SetPositionAndRotation(new Vector3(0.8f, heightAboveFloor, 4f), Quaternion.identity);

        var rig = new PlayerRig
        {
            root = player,
            body = player.GetComponent<Rigidbody>(),
            camera = player.transform.Find("Camera/FPSCam"),
        };
        rig.panTilt = rig.camera.GetComponent<CinemachinePanTilt>();
        rig.lookInput = rig.camera.GetComponent<CustomInputAxisController>();
        rig.footstep = (EventReference)new SerializedObject(player.GetComponent<PlayerMovement>()).FindProperty("footstepEvent").boxedValue;
        rig.panTilt.PanAxis.Value = 0f;
        rig.panTilt.TiltAxis.Value = 0f;

        // HUD, pausa e sistemas: cópias das peças da cena Game, religadas ao jogador novo
        CopyObject(game, "UI/HUD", roots.ui, gamePlayer, player);
        CopyObject(game, "UI/PauseMenu", roots.ui, gamePlayer, player);
        CopyObject(game, "UI/EventSystem", roots.ui, gamePlayer, player);
        CopyObject(game, "_Systems/SubtitleManager", roots.systems, gamePlayer, player);
        CopyObject(game, "_Systems/GameSettings", roots.systems, gamePlayer, player);
        CopyObject(game, "_Systems/GameplayStarter", roots.systems, gamePlayer, player);
        CopyObject(game, "Lighting/Post-Processing Volume", roots.lighting, gamePlayer, player);
        return rig;
    }

    private static GameObject FindInScene(Scene scene, string path)
    {
        string[] parts = path.Split('/');
        GameObject root = scene.GetRootGameObjects().FirstOrDefault(r => r.name == parts[0]);
        if (root == null) return null;
        Transform current = root.transform;
        for (int i = 1; i < parts.Length && current != null; i++) current = current.Find(parts[i]);
        return current != null ? current.gameObject : null;
    }

    private static void CopyObject(Scene game, string path, Transform parent, GameObject oldPlayer, GameObject newPlayer)
    {
        GameObject source = FindInScene(game, path);
        if (source == null)
        {
            Debug.LogWarning("[OpeningSetupTools] Não achei " + path + " na cena Game.");
            return;
        }

        GameObject copy = Object.Instantiate(source, parent);
        copy.name = source.name;

        // Referências que apontavam para o jogador da cena Game passam para o jogador novo;
        // as que apontavam para outros objetos da cena Game ficam vazias
        foreach (Component component in copy.GetComponentsInChildren<Component>(true))
        {
            if (component == null || component is Transform) continue;
            var data = new SerializedObject(component);
            SerializedProperty property = data.GetIterator();
            bool changed = false;
            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference || property.objectReferenceValue == null) continue;
                Object target = property.objectReferenceValue;
                if (EditorUtility.IsPersistent(target)) continue;
                if (target is Component c && c.transform.IsChildOf(copy.transform)) continue;
                if (target is GameObject g && g.transform.IsChildOf(copy.transform)) continue;
                property.objectReferenceValue = Remap(target, oldPlayer, newPlayer);
                changed = true;
            }
            if (changed) data.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Object Remap(Object target, GameObject oldPlayer, GameObject newPlayer)
    {
        Transform oldTransform = target is Component c ? c.transform : (target as GameObject)?.transform;
        if (oldTransform == null || !oldTransform.IsChildOf(oldPlayer.transform)) return null;

        string relative = AnimationUtility.CalculateTransformPath(oldTransform, oldPlayer.transform);
        Transform newTransform = string.IsNullOrEmpty(relative) ? newPlayer.transform : newPlayer.transform.Find(relative);
        if (newTransform == null) return null;
        if (target is GameObject) return newTransform.gameObject;
        return newTransform.GetComponent(target.GetType());
    }

    // =====================================================================
    // Corredor do segundo andar
    // =====================================================================

    private static void BuildHall(Roots roots, Materials m)
    {
        Transform hall = Group("Corredor", roots.environment);
        Transform doors = Group("Portas", roots.interactables);

        // Piso de carpete, teto e a caixa da escada que desce para o escuro
        Box("Piso", hall, new Vector3(-HallHalfWidth, -0.4f, 0f), new Vector3(HallHalfWidth, 0f, HallEnd + WallThickness), m.carpet, 4.8f);
        Box("Teto", hall, new Vector3(-HallHalfWidth, CeilingY, StairsEnd), new Vector3(HallHalfWidth, CeilingY + 0.4f, HallEnd), m.ceiling, 7f);
        Transform stairs = Group("Escada", hall);
        for (int i = 0; i < 8; i++)
        {
            float top = -(i + 1) * 0.86f;
            Box($"Degrau_{i + 1}", stairs, new Vector3(-HallHalfWidth, -7.5f, -(i + 1) * 1.6f), new Vector3(HallHalfWidth, top, -i * 1.6f), m.carpet, 4.8f);
        }
        Box("Patamar", stairs, new Vector3(-HallHalfWidth, -7.5f, StairsEnd), new Vector3(HallHalfWidth, -6.9f, -12.8f), m.carpet, 4.8f);
        Box("Corrimao", stairs, new Vector3(-HallHalfWidth, 3.6f, StairsEnd + 0.5f), new Vector3(-HallHalfWidth + 0.25f, 3.85f, -0.3f), m.trim, 4.8f, collider: false)
            .transform.rotation = Quaternion.identity;

        // Barreira invisível no topo da escada: Clara está ali atrás
        var blocker = new GameObject("Barreira_Escada");
        blocker.transform.SetParent(stairs, false);
        blocker.transform.position = new Vector3(0f, 6f, -0.9f);
        blocker.AddComponent<BoxCollider>().size = new Vector3(HallHalfWidth * 2f, 14f, 0.4f);

        // Paredes laterais com as aberturas das portas 1 a 5
        var left = HallDoors.Where(d => d.side < 0).Select(d => Opening(d.z, DoorWidth, 0f, DoorHeight)).ToArray();
        var right = HallDoors.Where(d => d.side > 0).Select(d => Opening(d.z, DoorWidth, 0f, DoorHeight)).ToArray();
        WallAlongZ("Parede_Esquerda", hall, -HallHalfWidth - WallThickness, -HallHalfWidth, StairsEnd, HallEnd, -7.5f, CeilingY, left, m, innerFaceSign: 1f);
        WallAlongZ("Parede_Direita", hall, HallHalfWidth, HallHalfWidth + WallThickness, StairsEnd, HallEnd, -7.5f, CeilingY, right, m, innerFaceSign: -1f);
        Box("Parede_Fundo_Escada", hall, new Vector3(-HallHalfWidth - WallThickness, -7.5f, StairsEnd - WallThickness), new Vector3(HallHalfWidth + WallThickness, CeilingY, StairsEnd), m.wallpaper, 10f);

        // Parede do fim do corredor, com a porta 6 (o lado do quarto é a mesma parede)
        WallAlongX("Parede_Porta6", hall, HallEnd, HallEnd + WallThickness, RoomMinX - WallThickness, RoomMaxX + WallThickness, 0f, CeilingY,
            new[] { Opening(0f, DoorWidth, 0f, DoorHeight) }, m, baseboardSides: new[] { -1f, 1f });

        // Portas trancadas de 1 a 5, com número de latão e batente
        foreach (var door in HallDoors)
        {
            float faceX = door.side * HallHalfWidth;
            float wallCenterX = door.side * (HallHalfWidth + WallThickness * 0.5f);
            // A dobradiça fica do lado de quem vem da escada; a folha abre para dentro do quarto
            Vector3 hinge = new Vector3(wallCenterX, 0f, door.z - DoorWidth * 0.5f);
            GameObject leaf = DoorLeaf($"Porta_{door.number}", doors, hinge, yaw: -90f, m, door.number, numberSide: door.side);
            DoorFrame(hall, faceX, door.side, door.z, m);
            MakeLockedDoor(leaf);
        }
        DoorFrameAcross(hall, HallEnd, 0f, m);
    }

    private static (float center, float width, float bottom, float top) Opening(float center, float width, float bottom, float top) => (center, width, bottom, top);

    // Parede ao longo de Z (x fixo), com aberturas ao longo de z
    private static void WallAlongZ(string name, Transform parent, float x0, float x1, float z0, float z1, float y0, float y1,
        (float center, float width, float bottom, float top)[] openings, Materials m, float innerFaceSign)
    {
        Transform wall = Group(name, parent);
        float cursor = z0;
        int index = 0;
        foreach (var o in openings.OrderBy(o => o.center))
        {
            float a = o.center - o.width * 0.5f, b = o.center + o.width * 0.5f;
            if (a > cursor) SolidZ(wall, $"Trecho_{index++}", x0, x1, cursor, a, y0, y1, m, innerFaceSign);
            if (o.bottom > y0) Box($"Abaixo_{index}", wall, new Vector3(x0, y0, a), new Vector3(x1, o.bottom, b), m.wallpaper, 10f);
            if (o.top < y1) Box($"Acima_{index}", wall, new Vector3(x0, o.top, a), new Vector3(x1, y1, b), m.wallpaper, 10f);
            cursor = b;
        }
        if (cursor < z1) SolidZ(wall, $"Trecho_{index}", x0, x1, cursor, z1, y0, y1, m, innerFaceSign);
    }

    private static void SolidZ(Transform wall, string name, float x0, float x1, float z0, float z1, float y0, float y1, Materials m, float innerFaceSign)
    {
        Box(name, wall, new Vector3(x0, y0, z0), new Vector3(x1, y1, z1), m.wallpaper, 10f);
        // Rodapé no lado de dentro, só onde há piso (z > 0 no corredor)
        float from = Mathf.Max(z0, 0f);
        if (from >= z1) return;
        float face = innerFaceSign > 0f ? x1 : x0;
        float x2 = face + innerFaceSign * 0.12f;
        Box(name + "_Rodape", wall, new Vector3(Mathf.Min(face, x2), 0f, from), new Vector3(Mathf.Max(face, x2), 0.6f, z1), m.trim, 4.8f, collider: false);
    }

    // Parede ao longo de X (z fixo), com aberturas ao longo de x
    private static void WallAlongX(string name, Transform parent, float z0, float z1, float x0, float x1, float y0, float y1,
        (float center, float width, float bottom, float top)[] openings, Materials m, float[] baseboardSides, Material material = null)
    {
        Transform wall = Group(name, parent);
        Material surface = material != null ? material : m.wallpaper;
        float cursor = x0;
        int index = 0;
        void Solid(float a, float b)
        {
            Box($"Trecho_{index++}", wall, new Vector3(a, y0, z0), new Vector3(b, y1, z1), surface, 10f);
            foreach (float side in baseboardSides)
            {
                float face = side < 0f ? z0 : z1;
                float z2 = face + side * 0.12f;
                Box($"Rodape_{index}_{(side < 0 ? "A" : "B")}", wall, new Vector3(a, 0f, Mathf.Min(face, z2)), new Vector3(b, 0.6f, Mathf.Max(face, z2)), m.trim, 4.8f, collider: false);
            }
        }
        foreach (var o in openings.OrderBy(o => o.center))
        {
            float a = o.center - o.width * 0.5f, b = o.center + o.width * 0.5f;
            if (a > cursor) Solid(cursor, a);
            if (o.bottom > y0) Box($"Abaixo_{index}", wall, new Vector3(a, y0, z0), new Vector3(b, o.bottom, z1), surface, 10f);
            if (o.top < y1) Box($"Acima_{index}", wall, new Vector3(a, o.top, z0), new Vector3(b, y1, z1), surface, 10f);
            cursor = b;
        }
        if (cursor < x1) Solid(cursor, x1);
    }

    // Batente de madeira em volta de uma porta lateral (lado do corredor)
    private static void DoorFrame(Transform parent, float faceX, int side, float z, Materials m)
    {
        Transform frame = Group("Batente_" + z.ToString("F0"), parent);
        float x0 = faceX, x1 = faceX - side * 0.14f;
        float a = z - DoorWidth * 0.5f, b = z + DoorWidth * 0.5f;
        Box("Esquerdo", frame, new Vector3(Mathf.Min(x0, x1), 0f, a - 0.35f), new Vector3(Mathf.Max(x0, x1), DoorHeight + 0.35f, a), m.trim, 4.8f, collider: false);
        Box("Direito", frame, new Vector3(Mathf.Min(x0, x1), 0f, b), new Vector3(Mathf.Max(x0, x1), DoorHeight + 0.35f, b + 0.35f), m.trim, 4.8f, collider: false);
        Box("Topo", frame, new Vector3(Mathf.Min(x0, x1), DoorHeight, a - 0.35f), new Vector3(Mathf.Max(x0, x1), DoorHeight + 0.35f, b + 0.35f), m.trim, 4.8f, collider: false);
    }

    // Batente da porta 6, dos dois lados da parede
    private static void DoorFrameAcross(Transform parent, float wallZ, float centerX, Materials m)
    {
        Transform frame = Group("Batente_Porta6", parent);
        float a = centerX - DoorWidth * 0.5f, b = centerX + DoorWidth * 0.5f;
        foreach (float z in new[] { wallZ - 0.14f, wallZ + WallThickness })
        {
            float z1 = z + 0.14f;
            Box("Esquerdo", frame, new Vector3(a - 0.35f, 0f, z), new Vector3(a, DoorHeight + 0.35f, z1), m.trim, 4.8f, collider: false);
            Box("Direito", frame, new Vector3(b, 0f, z), new Vector3(b + 0.35f, DoorHeight + 0.35f, z1), m.trim, 4.8f, collider: false);
            Box("Topo", frame, new Vector3(a - 0.35f, DoorHeight, z), new Vector3(b + 0.35f, DoorHeight + 0.35f, z1), m.trim, 4.8f, collider: false);
        }
    }

    // Folha de porta com maçaneta de latão. O pivô fica na dobradiça; a folha vai para +x local.
    private static GameObject DoorLeaf(string name, Transform parent, Vector3 hinge, float yaw, Materials m, int number, int numberSide)
    {
        var pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.SetPositionAndRotation(hinge, Quaternion.Euler(0f, yaw, 0f));

        const float thickness = 0.35f;
        GameObject leaf = Box("Folha", pivot.transform, Vector3.zero, Vector3.zero, m.doorWood, 6f, collider: false);
        leaf.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(DoorWidth - 0.1f, DoorHeight - 0.08f, thickness), 6f);
        leaf.transform.localPosition = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f);
        leaf.transform.localRotation = Quaternion.identity;

        // Almofadas da porta (relevo simples) e maçanetas dos dois lados
        foreach (float side in new[] { -1f, 1f })
        {
            for (int i = 0; i < 2; i++)
            {
                GameObject panel = Box($"Almofada_{(side < 0 ? "A" : "B")}{i}", pivot.transform, Vector3.zero, Vector3.zero, m.doorWood, 6f, collider: false);
                panel.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(DoorWidth * 0.62f, i == 0 ? 3.6f : 3.0f, 0.06f), 6f);
                panel.transform.localPosition = new Vector3(DoorWidth * 0.5f, i == 0 ? 7f : 2.6f, side * (thickness * 0.5f + 0.03f));
            }
            GameObject knob = Primitive(PrimitiveType.Sphere, "Macaneta_" + (side < 0 ? "A" : "B"), pivot.transform, m.brass);
            knob.transform.localPosition = new Vector3(DoorWidth - 0.55f, 4.6f, side * (thickness * 0.5f + 0.22f));
            knob.transform.localScale = Vector3.one * 0.32f;
            GameObject neck = Primitive(PrimitiveType.Cylinder, "Haste_" + (side < 0 ? "A" : "B"), pivot.transform, m.brass);
            neck.transform.localPosition = new Vector3(DoorWidth - 0.55f, 4.6f, side * (thickness * 0.5f + 0.08f));
            neck.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            neck.transform.localScale = new Vector3(0.12f, 0.1f, 0.12f);
            GameObject plate = Box("Espelho_" + (side < 0 ? "A" : "B"), pivot.transform, Vector3.zero, Vector3.zero, m.brass, 1f, collider: false);
            plate.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(0.3f, 1.1f, 0.04f), 1f);
            plate.transform.localPosition = new Vector3(DoorWidth - 0.55f, 4.4f, side * (thickness * 0.5f + 0.02f));
        }

        // Número de latão do lado do corredor
        var label = new GameObject("Numero");
        label.transform.SetParent(pivot.transform, false);
        label.transform.localPosition = new Vector3(DoorWidth * 0.5f, 7.9f, numberSide * (thickness * 0.5f + 0.08f));
        label.transform.localRotation = Quaternion.Euler(0f, numberSide > 0 ? 180f : 0f, 0f);
        var text = label.AddComponent<TextMeshPro>();
        text.text = number.ToString();
        text.fontSize = 6f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.42f, 0.31f, 0.14f); // latão escurecido: o texto não recebe luz
        text.rectTransform.sizeDelta = new Vector2(1.5f, 1.5f);

        var collider = pivot.AddComponent<BoxCollider>();
        collider.center = new Vector3(DoorWidth * 0.5f, DoorHeight * 0.5f, 0f);
        collider.size = new Vector3(DoorWidth, DoorHeight, thickness + 0.3f);
        return pivot;
    }

    private static void MakeLockedDoor(GameObject pivot)
    {
        var door = pivot.AddComponent<DoorController>();
        var data = new SerializedObject(door);
        data.FindProperty("currentState").enumValueIndex = (int)DoorController.DoorState.Locked;
        data.FindProperty("interactionDistance").floatValue = 6f;
        data.FindProperty("pivot").objectReferenceValue = pivot.transform;
        data.FindProperty("openPrompt").boxedValue = new LocalizedString(InteractionText.Table, "PROMPT_OPEN_DOOR");
        data.FindProperty("closePrompt").boxedValue = new LocalizedString(InteractionText.Table, "PROMPT_CLOSE_DOOR");
        data.FindProperty("lockedPrompt").boxedValue = new LocalizedString(InteractionText.Table, "PROMPT_LOCKED");
        data.FindProperty("movingPrompt").boxedValue = new LocalizedString(InteractionText.Table, "PROMPT_MOVING");
        data.FindProperty("lockedEvent").boxedValue = Sound("event:/Door/Door_Locked");
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    // =====================================================================
    // Quarto 6
    // =====================================================================

    private sealed class RoomParts
    {
        public GameObject door6;
        public GameObject switchOff, switchOn;
        public StoryInteractable lightSwitchUse;
        public FlickeringLight ceilingLight;
        public StoryInteractable drawerClue, clockClue, lockClue, dustClue, window;
        public Transform windowSpot, door5Key;
        public PoliceLights police;
        public FlickeringLight hallBulb, hallBulbFar;
    }

    private static void BuildRoom(Roots roots, Materials m, out RoomParts parts)
    {
        parts = new RoomParts();
        Transform room = Group("Quarto6", roots.environment);
        Transform props = Group("Quarto6", roots.interactables);

        Box("Piso", room, new Vector3(RoomMinX, -0.4f, RoomMinZ), new Vector3(RoomMaxX, 0f, RoomMaxZ), m.floorboards, 6f);
        Box("Teto", room, new Vector3(RoomMinX, CeilingY, RoomMinZ), new Vector3(RoomMaxX, CeilingY + 0.4f, RoomMaxZ), m.ceiling, 7f);
        WallAlongZ("Parede_Esquerda", room, RoomMinX - WallThickness, RoomMinX, HallEnd, RoomMaxZ + WallThickness, 0f, CeilingY,
            System.Array.Empty<(float, float, float, float)>(), m, innerFaceSign: 1f);
        WallAlongZ("Parede_Direita", room, RoomMaxX, RoomMaxX + WallThickness, HallEnd, RoomMaxZ + WallThickness, 0f, CeilingY,
            System.Array.Empty<(float, float, float, float)>(), m, innerFaceSign: -1f);
        WallAlongX("Parede_Janela", room, RoomMaxZ, RoomMaxZ + WallThickness, RoomMinX - WallThickness, RoomMaxX + WallThickness, 0f, CeilingY,
            new[] { Opening(WindowX, WindowWidth, WindowSill, WindowTop) }, m, baseboardSides: new[] { -1f });

        // Porta 6, encostada: a folha abre para dentro do quarto
        Transform doors = roots.interactables.Find("Portas");
        Vector3 hinge = new Vector3(-DoorWidth * 0.5f, 0f, HallEnd + WallThickness * 0.5f);
        parts.door6 = DoorLeaf("Porta_6", doors, hinge, yaw: 0f, m, 6, numberSide: -1);
        parts.door6.transform.rotation = Quaternion.Euler(0f, -8f, 0f);
        var doorUse = parts.door6.AddComponent<StoryInteractable>();
        doorUse.EditorSetup("PROMPT_OPEN_DOOR", 7f, true);

        // Trinco passado, saindo da borda da porta (fica à vista com a porta aberta), sem chave
        GameObject bolt = Box("Trinco", parts.door6.transform, Vector3.zero, Vector3.zero, m.brass, 1f, collider: false);
        bolt.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(0.5f, 0.22f, 0.16f), 1f);
        bolt.transform.localPosition = new Vector3(DoorWidth + 0.2f, 5.5f, 0f);
        GameObject latch = Box("Trava_Trinco", parts.door6.transform, Vector3.zero, Vector3.zero, m.brass, 1f, collider: false);
        latch.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(0.6f, 0.45f, 0.12f), 1f);
        latch.transform.localPosition = new Vector3(DoorWidth - 0.6f, 5.5f, 0.24f);
        // Só além da borda da folha: com a porta fechada fica dentro do batente e não cobre a maçaneta
        parts.lockClue = ClueVolume("Pista_Trinco", parts.door6.transform, new Vector3(DoorWidth + 0.35f, 5.4f, 0f), new Vector3(0.9f, 1.8f, 0.6f), local: true);

        // Interruptor à direita de quem entra: Ethan acende sem procurar
        GameObject lightSwitch = PlaceModel("Models/light switch/light switch.prefab", "Interruptor", props, new Vector3(DoorWidth * 0.5f + 1.1f, 5.4f, RoomMinZ + 0.1f), 180f, 0.55f, ModelSize.Longest, castShadows: false);
        parts.switchOff = lightSwitch.transform.Find("desligado")?.gameObject;
        parts.switchOn = lightSwitch.transform.Find("ligado")?.gameObject;
        if (parts.switchOn != null) parts.switchOn.SetActive(false);
        parts.lightSwitchUse = AddSwitchInteraction(lightSwitch);

        // Cama de ferro com a cabeceira na parede esquerda; o corpo sob o lençol
        GameObject bed = PlaceModel("Models/motel bed/motel bed.prefab", "Cama", props, new Vector3(RoomMinX + 4.75f, 0f, 75.5f), 90f, 9.4f, ModelSize.Longest);
        Bounds bedBounds = BoundsOf(bed);
        float mattressTop = MattressHeight(bed, bedBounds);
        GameObject body = PlaceModel("Prefabs/Cadaver.prefab", "Corpo_Sob_Lencol", props, new Vector3(bedBounds.center.x + 0.3f, mattressTop, bedBounds.center.z), 0f, 7.8f, ModelSize.Longest);
        foreach (Renderer renderer in body.GetComponentsInChildren<Renderer>()) renderer.sharedMaterial = m.sheet;
        Bounds bodyBounds = BoundsOf(body);
        Decal("Mancha", props, new Vector3(bodyBounds.center.x - bodyBounds.size.x * 0.12f, bodyBounds.max.y + 0.6f, bodyBounds.center.z + 0.3f), new Vector3(3.2f, 2.6f, 2.5f), m.stainDecal);

        // Mesa de cabeceira (provisória) com o rádio-relógio parado em 1h47 e o abajur apagado
        Vector3 nightstand = new Vector3(RoomMinX + 1.2f, 0f, 80.2f);
        FurnitureNightstand(props, nightstand, m);
        float nightstandTop = 2.7f;
        GameObject clock = PlaceModel("Models/radio clock/radio clock.prefab", "Radio_Relogio", props, new Vector3(nightstand.x + 0.15f, nightstandTop, nightstand.z - 0.35f), 90f, 1.25f, ModelSize.Longest);
        ClockFace(clock, "1:47");
        parts.clockClue = ClueVolume("Pista_Relogio", props, BoundsOf(clock).center, BoundsOf(clock).size + new Vector3(0.6f, 0.6f, 0.6f), local: false);
        PlaceModel("Models/table lamp/table lamp.prefab", "Abajur", props, new Vector3(nightstand.x - 0.2f, nightstandTop, nightstand.z + 0.75f), 0f, 2.4f, ModelSize.Height);
        UnpluggedCord(props, new Vector3(nightstand.x + 1.15f, nightstandTop, nightstand.z - 0.6f), m);

        // Cômoda (provisória) na parede direita, com a carteira aberta e a marca sem poeira
        Vector3 dresser = new Vector3(RoomMaxX - 1.05f, 0f, 72f);
        float dresserTop = FurnitureDresser(props, dresser, m);
        GameObject wallet = PlaceModel("Models/wallet/wallet.prefab", "Carteira", props, new Vector3(dresser.x - 0.1f, dresserTop, dresser.z - 1.1f), -70f, 0.9f, ModelSize.Longest, castShadows: false);
        MakeWalletInspectable(wallet);
        Vector3 dustSpot = new Vector3(dresser.x - 0.1f, dresserTop, dresser.z + 1.0f);
        Decal("Poeira", props, dustSpot + Vector3.up * 0.4f, new Vector3(2.2f, 1.7f, 0.8f), m.dustDecal);
        parts.dustClue = ClueVolume("Pista_Marca_Poeira", props, dustSpot + Vector3.up * 0.15f, new Vector3(1.6f, 0.3f, 1.4f), local: false);

        // Guarda-roupa (provisório) no canto
        FurnitureWardrobe(props, new Vector3(RoomMaxX - 1.35f, 0f, 79.2f), m);

        // Bancada da pia (provisória) com a gaveta de talheres aberta, o fogareiro e a pia de parede
        Vector3 counter = new Vector3(RoomMinX + 1.45f, 0f, 65.6f);
        float counterTop = FurnitureCounter(props, counter, m, out Vector3 drawerCenter);
        parts.drawerClue = ClueVolume("Pista_Gaveta", props, drawerCenter, new Vector3(2.4f, 1.2f, 3.2f), local: false);
        PlaceModel("Models/eletric stove/eletric stove.prefab", "Fogareiro", props, new Vector3(counter.x - 0.2f, counterTop, counter.z + 0.6f), 90f, 1.5f, ModelSize.Longest);
        PlaceModel("Models/motel sink/motel sink.prefab", "Pia", props, new Vector3(RoomMinX + 1.25f, 2.3f, 69.6f), 90f, 2.6f, ModelSize.Width);

        // Luminária de teto do quarto, apagada até Ethan entrar
        parts.ceilingLight = HangingBulb("Luminaria_Teto", roots.lighting, new Vector3(0f, CeilingY, 72.5f), 0.6f, m, intensity: 32f, range: 40f, startOn: false, wobble: 0.02f, interval: new Vector2(10f, 25f));

        // Janela de guilhotina e a escada de incêndio lá fora
        GameObject window = PlaceModel("Models/window/window.prefab", "Janela", props, new Vector3(WindowX, WindowSill - 0.1f, RoomMaxZ + WallThickness * 0.5f), 0f, WindowTop - WindowSill + 0.2f, ModelSize.Height);
        foreach (Renderer renderer in window.GetComponentsInChildren<Renderer>()) renderer.shadowCastingMode = ShadowCastingMode.Off;
        parts.window = ClueVolume("Saida_Janela", props, new Vector3(WindowX, (WindowSill + WindowTop) * 0.5f, RoomMaxZ - 0.2f), new Vector3(WindowWidth, WindowTop - WindowSill, 0.6f), local: false);
        parts.window.EditorSetup("PROMPT_FIRE_ESCAPE", 7f, false);
        var spot = new GameObject("Ponto_Parapeito");
        spot.transform.SetParent(props, false);
        spot.transform.SetPositionAndRotation(new Vector3(WindowX, 0f, RoomMaxZ - 1.6f), Quaternion.Euler(0f, 180f, 0f));
        parts.windowSpot = spot.transform;
        // O corrimão do patamar fica um pouco acima do parapeito; o resto da escada desce para a rua
        GameObject fireEscape = PlaceModel("Models/fire escape/fire escape.prefab", "Escada_Incendio", props, new Vector3(WindowX + 1f, 0f, RoomMaxZ + WallThickness + 3.6f), 0f, 14f, ModelSize.Width);
        fireEscape.transform.position += Vector3.up * (WindowSill + 3.4f - BoundsOf(fireEscape).max.y);
        Rain(roots.environment, m);

        // Giroflex da viatura, lá embaixo na rua
        parts.police = PoliceCar(roots.lighting);

        // Chave do quarto 5 na fechadura (lado do corredor): a câmera se demora nela
        var door5 = HallDoors.First(d => d.number == 5);
        // A folha fica recuada na parede: a chave sai da fechadura, abaixo da maçaneta
        GameObject key = PlaceModel("Models/red key/red key.prefab", "Chave_Quarto5", props, new Vector3(-HallHalfWidth - 0.05f, 3.55f, door5.z + DoorWidth * 0.5f - 0.55f), 0f, 0.6f, ModelSize.Height, castShadows: false);
        key.transform.SetParent(roots.interactables.Find("Portas/Porta_5"), true);
        parts.door5Key = key.transform;

        // Lâmpadas do corredor: a que falha, sobre a escada (atrás de Ethan e Clara), e uma fraca perto do fim
        parts.hallBulb = HangingBulb("Lampada_Escada", roots.lighting, new Vector3(0f, CeilingY, -2f), 1.5f, m, intensity: 110f, range: 60f, startOn: true, wobble: 0.1f, interval: new Vector2(2.5f, 6f));
        parts.hallBulbFar = HangingBulb("Lampada_Corredor", roots.lighting, new Vector3(0f, CeilingY, 45f), 1.5f, m, intensity: 30f, range: 36f, startOn: true, wobble: 0.04f, interval: new Vector2(9f, 20f));
        HangingBulb("Lampada_Queimada", roots.lighting, new Vector3(0f, CeilingY, 22f), 1.5f, m, intensity: 0f, range: 1f, startOn: false, wobble: 0f, interval: new Vector2(999f, 999f));
    }

    private enum ModelSize { Longest, Height, Width }

    // Modelo salvo (prefab) apoiado pela base no ponto, girado e escalado para o tamanho real
    private static GameObject PlaceModel(string prefabPath, string name, Transform parent, Vector3 basePoint, float yaw, float size, ModelSize measure, bool castShadows = true)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[OpeningSetupTools] Prefab não encontrado: " + prefabPath);
            return new GameObject(name + " (faltando)");
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.gameObject.scene);
        instance.name = name;
        instance.transform.SetParent(parent, false);
        instance.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0f, yaw, 0f) * prefab.transform.rotation);
        instance.transform.localScale = prefab.transform.localScale;

        Bounds bounds = BoundsOf(instance);
        float current = measure switch
        {
            ModelSize.Height => bounds.size.y,
            ModelSize.Width => Mathf.Max(bounds.size.x, bounds.size.z),
            _ => Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z),
        };
        if (current > 0.0001f) instance.transform.localScale = prefab.transform.localScale * (size / current);

        bounds = BoundsOf(instance);
        instance.transform.position += basePoint - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            renderer.shadowCastingMode = castShadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        return instance;
    }

    private static Bounds BoundsOf(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>().Where(r => r is MeshRenderer || r is SkinnedMeshRenderer).ToArray();
        if (renderers.Length == 0) return new Bounds(target.transform.position, Vector3.zero);
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    // Altura do colchão: raio de cima para baixo no meio da cama, contra a malha dela
    private static float MattressHeight(GameObject bed, Bounds bounds)
    {
        var temp = new List<MeshCollider>();
        foreach (MeshFilter filter in bed.GetComponentsInChildren<MeshFilter>())
        {
            var collider = filter.gameObject.AddComponent<MeshCollider>();
            collider.sharedMesh = filter.sharedMesh;
            temp.Add(collider);
        }
        Physics.SyncTransforms();
        float height = bounds.min.y + bounds.size.y * 0.45f;
        if (Physics.Raycast(new Vector3(bounds.center.x, bounds.max.y + 2f, bounds.center.z), Vector3.down, out RaycastHit hit, bounds.size.y + 4f))
            height = hit.point.y;
        foreach (MeshCollider collider in temp) Object.DestroyImmediate(collider);

        // Colisão simples para a cama inteira (o jogador não atravessa)
        var box = bed.AddComponent<BoxCollider>();
        box.center = bed.transform.InverseTransformPoint(bounds.center);
        Vector3 size = bed.transform.InverseTransformVector(new Vector3(bounds.size.x, height - bounds.min.y, bounds.size.z));
        box.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));
        box.center = bed.transform.InverseTransformPoint(new Vector3(bounds.center.x, (bounds.min.y + height) * 0.5f, bounds.center.z));
        return height;
    }

    private static void ClockFace(GameObject clock, string time)
    {
        Bounds bounds = BoundsOf(clock);
        // A frente do rádio-relógio olha para o centro do quarto (+x)
        var label = new GameObject("Mostrador_1h47");
        label.transform.SetParent(clock.transform, true);
        label.transform.SetPositionAndRotation(new Vector3(bounds.max.x + 0.02f, bounds.center.y, bounds.center.z + bounds.size.z * 0.18f), Quaternion.Euler(0f, -90f, 0f));
        label.transform.localScale = Vector3.one;
        label.transform.localScale = new Vector3(1f / clock.transform.lossyScale.x, 1f / clock.transform.lossyScale.y, 1f / clock.transform.lossyScale.z);
        var text = label.AddComponent<TextMeshPro>();
        text.text = time;
        text.fontSize = 1.6f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = new Color(0.32f, 0.3f, 0.27f); // dígitos parados, sem luz: o rádio está desligado da tomada
        text.rectTransform.sizeDelta = new Vector2(1.2f, 0.4f);
    }

    // O fio desce da mesa, corre pelo chão até perto da tomada e termina no plugue solto
    private static void UnpluggedCord(Transform parent, Vector3 from, Materials m)
    {
        Transform cord = Group("Fio_Desligado", parent);
        const float outletZ = 81.65f;
        Box("Descida", cord, new Vector3(from.x - 0.03f, 0.05f, from.z - 0.03f), new Vector3(from.x + 0.03f, from.y, from.z + 0.03f), m.cord, 1f, collider: false);
        Box("No_Chao_A", cord, new Vector3(from.x - 0.03f, 0f, from.z), new Vector3(from.x + 0.03f, 0.06f, outletZ), m.cord, 1f, collider: false);
        Box("No_Chao_B", cord, new Vector3(RoomMinX + 0.9f, 0f, outletZ - 0.03f), new Vector3(from.x + 0.03f, 0.06f, outletZ + 0.03f), m.cord, 1f, collider: false);
        Box("Plugue", cord, new Vector3(RoomMinX + 0.6f, 0f, outletZ - 0.12f), new Vector3(RoomMinX + 0.9f, 0.16f, outletZ + 0.12f), m.cord, 1f, collider: false);
        // Tomada vazia na parede, logo acima
        Box("Tomada", cord, new Vector3(RoomMinX, 1.1f, outletZ - 0.2f), new Vector3(RoomMinX + 0.05f, 1.7f, outletZ + 0.2f), m.laminate, 1f, collider: false);
    }

    private static void MakeWalletInspectable(GameObject wallet)
    {
        Bounds bounds = BoundsOf(wallet);
        var collider = wallet.AddComponent<BoxCollider>();
        collider.center = wallet.transform.InverseTransformPoint(bounds.center);
        Vector3 size = wallet.transform.InverseTransformVector(bounds.size + new Vector3(0.2f, 0.3f, 0.2f));
        collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

        var item = wallet.AddComponent<ItemInteract>();
        var data = new SerializedObject(item);
        data.FindProperty("promptString").boxedValue = new LocalizedString(InteractionText.Table, "PROMPT_INSPECTION");
        data.FindProperty("itemNameString").boxedValue = new LocalizedString("items", "ITEM_NAME_WALLET");
        data.FindProperty("itemDescriptionString").boxedValue = new LocalizedString("items", "ITEM_DESC_WALLET");
        data.FindProperty("interactionDistance").floatValue = 6f;
        data.FindProperty("inspectionDistance").floatValue = 1.5f;
        data.FindProperty("itemPickupSoundEvent").boxedValue = Sound("event:/Grab/Grab_Drop");
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Móveis provisórios (até os modelos chegarem) ----------

    private static void FurnitureNightstand(Transform parent, Vector3 basePoint, Materials m)
    {
        Transform piece = Group("Mesa_Cabeceira_Provisoria", parent);
        Vector3 size = new Vector3(2.2f, 2.7f, 2.1f);
        Box("Corpo", piece, basePoint + new Vector3(-size.x / 2, 0.25f, -size.z / 2), basePoint + new Vector3(size.x / 2, size.y, size.z / 2), m.furniture, 2f);
        Box("Gaveta", piece, basePoint + new Vector3(size.x / 2, 1.8f, -0.9f), basePoint + new Vector3(size.x / 2 + 0.06f, 2.45f, 0.9f), m.furniture, 2f, collider: false);
        Knob(piece, basePoint + new Vector3(size.x / 2 + 0.12f, 2.12f, 0f), m);
        foreach (var leg in new[] { new Vector2(-1, -1), new Vector2(1, -1), new Vector2(-1, 1), new Vector2(1, 1) })
            Box("Pe", piece, basePoint + new Vector3(leg.x * 0.95f - 0.08f, 0f, leg.y * 0.9f - 0.08f), basePoint + new Vector3(leg.x * 0.95f + 0.08f, 0.25f, leg.y * 0.9f + 0.08f), m.furniture, 2f, collider: false);
    }

    private static float FurnitureDresser(Transform parent, Vector3 basePoint, Materials m)
    {
        Transform piece = Group("Comoda_Provisoria", parent);
        Vector3 size = new Vector3(2.1f, 3.9f, 5f);
        Box("Corpo", piece, basePoint + new Vector3(-size.x / 2, 0.2f, -size.z / 2), basePoint + new Vector3(size.x / 2, size.y, size.z / 2), m.furniture, 2f);
        for (int i = 0; i < 3; i++)
        {
            float y0 = 0.45f + i * 1.12f;
            Box($"Gaveta_{i + 1}", piece, basePoint + new Vector3(-size.x / 2 - 0.06f, y0, -2.3f), basePoint + new Vector3(-size.x / 2, y0 + 0.98f, 2.3f), m.furniture, 2f, collider: false);
            Knob(piece, basePoint + new Vector3(-size.x / 2 - 0.12f, y0 + 0.5f, -1.2f), m);
            Knob(piece, basePoint + new Vector3(-size.x / 2 - 0.12f, y0 + 0.5f, 1.2f), m);
        }
        return basePoint.y + size.y;
    }

    private static void FurnitureWardrobe(Transform parent, Vector3 basePoint, Materials m)
    {
        Transform piece = Group("Guarda_Roupa_Provisorio", parent);
        Vector3 size = new Vector3(2.6f, 9.2f, 4.4f);
        Box("Corpo", piece, basePoint + new Vector3(-size.x / 2, 0.2f, -size.z / 2), basePoint + new Vector3(size.x / 2, size.y, size.z / 2), m.furniture, 2.5f);
        Box("Porta", piece, basePoint + new Vector3(-size.x / 2 - 0.06f, 0.5f, -2f), basePoint + new Vector3(-size.x / 2, size.y - 0.4f, 2f), m.furniture, 2.5f, collider: false);
        Box("Fechadura", piece, basePoint + new Vector3(-size.x / 2 - 0.1f, 4.6f, 1.55f), basePoint + new Vector3(-size.x / 2 - 0.06f, 4.95f, 1.75f), m.brass, 1f, collider: false);
    }

    private static float FurnitureCounter(Transform parent, Vector3 basePoint, Materials m, out Vector3 drawerCenter)
    {
        Transform piece = Group("Bancada_Pia_Provisoria", parent);
        Vector3 size = new Vector3(2.9f, 4.2f, 3.8f);
        Box("Corpo", piece, basePoint + new Vector3(-size.x / 2, 0.2f, -size.z / 2), basePoint + new Vector3(size.x / 2, size.y, size.z / 2), m.furniture, 2f);
        Box("Tampo", piece, basePoint + new Vector3(-size.x / 2 - 0.05f, size.y, -size.z / 2 - 0.05f), basePoint + new Vector3(size.x / 2 + 0.12f, size.y + 0.18f, size.z / 2 + 0.05f), m.laminate, 2f);
        Box("Porta_Baixa", piece, basePoint + new Vector3(size.x / 2, 0.45f, -1.7f), basePoint + new Vector3(size.x / 2 + 0.06f, 2.9f, 1.7f), m.furniture, 2f, collider: false);
        Knob(piece, basePoint + new Vector3(size.x / 2 + 0.12f, 2.5f, 1.3f), m);

        // Gaveta de talheres puxada para fora, com o lugar da faca maior vazio
        float y0 = 3.1f, y1 = 3.95f, out0 = size.x / 2 - 0.4f, out1 = size.x / 2 + 1.5f;
        Transform drawer = Group("Gaveta_Talheres_Aberta", piece);
        Box("Fundo", drawer, basePoint + new Vector3(out0, y0, -1.6f), basePoint + new Vector3(out1, y0 + 0.08f, 1.6f), m.furniture, 2f, collider: false);
        Box("Frente", drawer, basePoint + new Vector3(out1, y0, -1.75f), basePoint + new Vector3(out1 + 0.08f, y1, 1.75f), m.furniture, 2f, collider: false);
        Box("Lado_A", drawer, basePoint + new Vector3(out0, y0, -1.6f), basePoint + new Vector3(out1, y1 - 0.15f, -1.52f), m.furniture, 2f, collider: false);
        Box("Lado_B", drawer, basePoint + new Vector3(out0, y0, 1.52f), basePoint + new Vector3(out1, y1 - 0.15f, 1.6f), m.furniture, 2f, collider: false);
        Knob(drawer, basePoint + new Vector3(out1 + 0.14f, (y0 + y1) * 0.5f, 0f), m);
        // Talheres desencontrados, deitados ao longo da gaveta; o espaço da faca de cozinha ficou vazio
        float[] slots = { -1.25f, -0.85f, -0.45f, 0.75f, 1.15f };
        for (int i = 0; i < slots.Length; i++)
        {
            float length = i % 2 == 0 ? 1.0f : 0.85f;
            Box($"Talher_{i + 1}", drawer, basePoint + new Vector3(out0 + 0.3f, y0 + 0.08f, slots[i] - 0.06f), basePoint + new Vector3(out0 + 0.3f + length, y0 + 0.13f, slots[i] + 0.06f), m.darkMetal, 1f, collider: false);
        }
        Box("Divisoria", drawer, basePoint + new Vector3(out0 + 0.1f, y0 + 0.08f, 0.1f), basePoint + new Vector3(out1 - 0.1f, y0 + 0.4f, 0.16f), m.furniture, 2f, collider: false);
        drawerCenter = basePoint + new Vector3((out0 + out1) * 0.5f, (y0 + y1) * 0.5f, 0f);
        return basePoint.y + size.y + 0.18f;
    }

    private static void Knob(Transform parent, Vector3 position, Materials m)
    {
        GameObject knob = Primitive(PrimitiveType.Sphere, "Puxador", parent, m.brass);
        knob.transform.position = position;
        knob.transform.localScale = Vector3.one * 0.2f;
    }

    // Lâmpada pendurada por um fio, com a lâmpada que você gerou (malha "lampada" do abajur)
    private static FlickeringLight HangingBulb(string name, Transform parent, Vector3 ceilingPoint, float drop, Materials m, float intensity, float range, bool startOn, float wobble, Vector2 interval)
    {
        Transform group = Group(name, parent);
        group.position = ceilingPoint;
        GameObject rose = Primitive(PrimitiveType.Cylinder, "Canopla", group, m.ceiling);
        rose.transform.localPosition = new Vector3(0f, -0.06f, 0f);
        rose.transform.localScale = new Vector3(0.7f, 0.06f, 0.7f);
        GameObject wire = Box("Fio", group, Vector3.zero, Vector3.zero, m.cord, 1f, collider: false);
        wire.GetComponent<MeshFilter>().sharedMesh = BoxMesh(new Vector3(0.05f, drop, 0.05f), 1f);
        wire.transform.localPosition = new Vector3(0f, -drop * 0.5f, 0f);
        GameObject socket = Primitive(PrimitiveType.Cylinder, "Soquete", group, m.darkMetal);
        socket.transform.localPosition = new Vector3(0f, -drop - 0.15f, 0f);
        socket.transform.localScale = new Vector3(0.22f, 0.15f, 0.22f);

        // A malha "lampada" do abajur, de cabeça para baixo, com 9 cm de altura
        var bulbObject = new GameObject("Lampada");
        bulbObject.transform.SetParent(group, false);
        var lampPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Models/table lamp/table lamp.prefab");
        Transform lampBulb = lampPrefab != null ? lampPrefab.transform.Find("lampada") : null;
        var bulbFilter = bulbObject.AddComponent<MeshFilter>();
        var bulbRenderer = bulbObject.AddComponent<MeshRenderer>();
        bulbRenderer.shadowCastingMode = ShadowCastingMode.Off;
        if (lampBulb != null)
        {
            bulbFilter.sharedMesh = lampBulb.GetComponent<MeshFilter>().sharedMesh;
            bulbObject.transform.localRotation = Quaternion.Euler(180f, 0f, 0f) * lampBulb.localRotation;
            bulbObject.transform.localScale = Vector3.one;
            float height = Mathf.Max(bulbRenderer.bounds.size.y, 0.0001f);
            bulbObject.transform.localScale = Vector3.one * (0.42f / height);
            Bounds bulbBounds = bulbRenderer.bounds;
            float socketBottom = ceilingPoint.y - drop - 0.3f;
            bulbObject.transform.position += new Vector3(ceilingPoint.x - bulbBounds.center.x, socketBottom - bulbBounds.max.y, ceilingPoint.z - bulbBounds.center.z);
        }

        var lightObject = new GameObject("Luz");
        lightObject.transform.SetParent(group, false);
        lightObject.transform.localPosition = new Vector3(0f, -drop - 0.55f, 0f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.82f, 0.55f);
        light.intensity = intensity;
        light.range = range;
        light.shadows = intensity > 0f ? LightShadows.Soft : LightShadows.None;
        light.shadowBias = 0.02f;
        light.shadowNormalBias = 0.3f;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        light.enabled = startOn && intensity > 0f;

        var flicker = group.gameObject.AddComponent<FlickeringLight>();
        flicker.EditorSetup(light, bulbRenderer, m.bulbOn, m.bulbOff, startOn && intensity > 0f, wobble, interval);
        bulbRenderer.sharedMaterial = startOn && intensity > 0f ? m.bulbOn : m.bulbOff;
        return flicker;
    }

    private static void Rain(Transform parent, Materials m)
    {
        var rain = new GameObject("Chuva_Fina");
        rain.transform.SetParent(parent, false);
        rain.transform.position = new Vector3(WindowX, CeilingY + 12f, RoomMaxZ + 9f);
        var system = rain.AddComponent<ParticleSystem>();
        var main = system.main;
        main.startLifetime = 1.4f;
        main.startSpeed = 28f;
        main.startSize3D = true;
        main.startSizeX = 0.025f;
        main.startSizeY = 0.9f;
        main.startSizeZ = 0.025f;
        main.maxParticles = 1500;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = system.emission;
        emission.rateOverTime = 700f;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30f, 14f, 0.5f);
        shape.rotation = new Vector3(90f, 0f, 0f);
        var renderer = rain.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.04f;
        renderer.lengthScale = 2f;
        renderer.sharedMaterial = m.rain;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
    }

    private static PoliceLights PoliceCar(Transform parent)
    {
        var car = new GameObject("Viatura_Giroflex");
        car.transform.SetParent(parent, false);
        car.transform.position = new Vector3(WindowX + 4f, -18f, RoomMaxZ + 26f);
        Light Spot(string name, Color color, float offset)
        {
            var lightObject = new GameObject(name);
            lightObject.transform.SetParent(car.transform, false);
            lightObject.transform.localPosition = new Vector3(offset, 0f, 0f);
            lightObject.transform.LookAt(new Vector3(WindowX, CeilingY, RoomMaxZ - 8f));
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Spot;
            light.color = color;
            light.range = 90f;
            light.spotAngle = 40f;
            light.innerSpotAngle = 12f;
            light.shadows = LightShadows.Soft;
            light.shadowBias = 0.02f;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            light.enabled = false;
            return light;
        }
        var lights = car.AddComponent<PoliceLights>();
        lights.EditorSetup(Spot("Vermelha", new Color(1f, 0.08f, 0.05f), -0.6f), Spot("Azul", new Color(0.1f, 0.25f, 1f), 0.6f), 1500f);
        AddPoliceGlow(lights);
        return lights;
    }

    // Clarão do giroflex lá fora, visto pela janela: dois planos aditivos atrás da escada de
    // incêndio (ela fica em silhueta contra a luz), acesos junto com a luz vermelha e a azul.
    // Usado também pelo cenário do menu. Pode rodar de novo: troca o clarão antigo.
    internal static void AddPoliceGlow(PoliceLights lights)
    {
        Transform old = lights.transform.Find("Clarao_Janela");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        PrepareFolders();
        Texture2D texture = Texture("clarao_giroflex", DrawGlow, new Color32(255, 255, 255, 255), 256);
        string materialPath = $"{MaterialFolder}/Clarao giroflex.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, materialPath);
        }
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f); // aditivo
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)RenderQueue.Transparent;
        EditorUtility.SetDirty(material);

        var group = new GameObject("Clarao_Janela").transform;
        group.SetParent(lights.transform, false);
        group.position = new Vector3(WindowX, 0f, RoomMaxZ + 22f);
        Renderer Plane(string name, float offsetX)
        {
            GameObject quad = Primitive(PrimitiveType.Quad, name, group, material);
            quad.transform.localPosition = new Vector3(offsetX, 7f, 0f);
            quad.transform.localScale = new Vector3(26f, 24f, 1f);
            var renderer = quad.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.enabled = false;
            return renderer;
        }
        lights.EditorSetupGlow(Plane("Vermelho", -3f), Plane("Azul", 3.5f));
    }

    // Clarão que sobe da rua: forte embaixo, no centro, e some nas bordas
    private static float DrawGlow(float u, float v)
    {
        float du = (u - 0.5f) / 0.5f, dv = (v - 0.25f) / 0.75f;
        float d = Mathf.Sqrt(du * du + dv * dv);
        float falloff = Mathf.Clamp01(1f - d);
        return falloff * falloff * (3f - 2f * falloff);
    }

    // =====================================================================
    // Clara e o roteiro
    // =====================================================================

    private static ClaraPresence BuildClara(Roots roots, PlayerRig player)
    {
        var clara = new GameObject("Clara");
        clara.transform.SetParent(roots.story, false);
        clara.transform.position = new Vector3(0f, 0f, 0.8f);

        // Silhueta que só projeta sombra: o jogador nunca a vê, só a sombra na parede
        Transform shadow = Group("Sombra", clara.transform);
        GameObject torso = Primitive(PrimitiveType.Capsule, "Corpo", shadow, null);
        torso.transform.localPosition = new Vector3(0f, 3.5f, 0f);
        torso.transform.localScale = new Vector3(2f, 3.2f, 1.2f);
        GameObject head = Primitive(PrimitiveType.Sphere, "Cabeca", shadow, null);
        head.transform.localPosition = new Vector3(0f, 7.25f, 0f);
        head.transform.localScale = new Vector3(1f, 1.15f, 1.05f);
        foreach (MeshRenderer renderer in shadow.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialFolder}/Sombra da Clara.mat");
        }

        // Flash um pouco à frente dela (fora da silhueta, para a sombra dela não bloquear a luz)
        var flashObject = new GameObject("Flash");
        flashObject.transform.SetParent(clara.transform, false);
        flashObject.transform.localPosition = new Vector3(0.3f, 6.9f, 1.8f);
        var flash = flashObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = new Color(0.92f, 0.95f, 1f);
        flash.range = 50f;
        flash.intensity = 160f;
        flash.shadows = LightShadows.Soft;
        flash.lightmapBakeType = LightmapBakeType.Realtime;
        flash.enabled = false;

        var presence = clara.AddComponent<ClaraPresence>();
        presence.EditorSetup(player.root.transform, player.camera, flash, player.footstep, Sound(FirstSound("event:/Opening/Camera_Flash", "event:/Opening/camera flash", "event:/Effects/camera flash")));
        return presence;
    }

    private static void BuildStory(Roots roots, PlayerRig player, ClaraPresence clara, RoomParts room)
    {
        PlayerZone Zone(string name, Vector3 min, Vector3 max)
        {
            var zone = new GameObject(name);
            zone.transform.SetParent(roots.story, false);
            zone.transform.position = (min + max) * 0.5f;
            zone.layer = 2; // Ignore Raycast: não atrapalha a mira das interações
            var box = zone.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = max - min;
            return zone.AddComponent<PlayerZone>();
        }

        PlayerZone stairs = Zone("Zona_Escada", new Vector3(-HallHalfWidth, 0f, -0.7f), new Vector3(HallHalfWidth, 10f, 1.4f));
        PlayerZone end = Zone("Zona_Fim_Corredor", new Vector3(-HallHalfWidth, 0f, 52f), new Vector3(HallHalfWidth, 10f, HallEnd));
        var door5 = HallDoors.First(d => d.number == 5);
        PlayerZone door5Zone = Zone("Zona_Porta5", new Vector3(-HallHalfWidth, 0f, door5.z - 5f), new Vector3(HallHalfWidth, 10f, door5.z - 2.5f));
        PlayerZone entry = Zone("Zona_Entrada_Quarto", new Vector3(-DoorWidth * 0.5f, 0f, RoomMinZ + 0.6f), new Vector3(DoorWidth * 0.5f, 10f, RoomMinZ + 3.2f));

        var directorObject = new GameObject("Roteiro_Abertura");
        directorObject.transform.SetParent(roots.story, false);
        var director = directorObject.AddComponent<OpeningDirector>();
        director.EditorSetupPlayer(clara, player.body, player.camera, player.panTilt, player.lookInput);

        var data = new SerializedObject(director);
        void Set(string field, Object value) => data.FindProperty(field).objectReferenceValue = value;
        void SetSound(string field, string path) => data.FindProperty(field).boxedValue = Sound(path);
        SetSound("scannerStatic", FirstSound("event:/Opening/Scanner", "event:/Effects/police scanner", "event:/Events/static_burst"));
        Set("stairs", stairs);
        Set("corridorEnd", end);
        Set("door5Zone", door5Zone);
        Set("door5Key", room.door5Key);
        Set("door6", room.door6.GetComponent<StoryInteractable>());
        Set("door6Pivot", room.door6.transform);
        data.FindProperty("door6OpenAngle").floatValue = -80f;
        SetSound("doorOpenSound", "event:/Door/Door_Open");
        Set("roomEntry", entry);
        Set("lightSwitch", room.lightSwitchUse);
        Set("switchOff", room.switchOff);
        Set("switchOn", room.switchOn);
        SetSound("switchSound", FirstSound("event:/Opening/Light_Switch", "event:/Effects/light switch", "event:/Effects/Key"));
        Set("roomLight", room.ceilingLight);
        Set("drawerClue", room.drawerClue);
        Set("clockClue", room.clockClue);
        Set("lockClue", room.lockClue);
        Set("dustMarkClue", room.dustClue);
        Set("police", room.police);
        SetSound("sirenSound", FirstSound("event:/Opening/Siren", "event:/Ambience/police siren"));
        Set("window", room.window);
        Set("windowSpot", room.windowSpot);
        SetSound("thunderSound", "event:/Ambience/Thunder");
        SetSound("rainAmbience", FirstSound("event:/Opening/Rain", "event:/Opening/horror rain", "event:/Effects/horror rain"));
        data.ApplyModifiedPropertiesWithoutUndo();

        AssignVoices(director);
    }

    // Vozes gravadas: quando existir no FMOD um evento event:/Voices/<chave da fala>, ele entra na fala
    private static void AssignVoices(OpeningDirector director)
    {
        var data = new SerializedObject(director);
        var found = new List<(string path, EventReference voice)>();
        SerializedProperty property = data.GetIterator();
        while (property.Next(true))
        {
            if (property.type != nameof(DialogueLine)) continue;
            SerializedProperty key = property.FindPropertyRelative("text.m_TableEntryReference.m_Key");
            if (key == null || string.IsNullOrEmpty(key.stringValue)) continue;
            EventReference voice = Sound("event:/Voices/" + key.stringValue);
            if (!voice.IsNull) found.Add((property.propertyPath + ".voice", voice));
        }
        foreach (var (path, voice) in found) data.FindProperty(path).boxedValue = voice;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    [MenuItem("Echoes/Roteiro/Abertura 1995: atualizar sons e vozes")]
    private static void RefreshSounds()
    {
        var director = Object.FindAnyObjectByType<OpeningDirector>();
        if (director == null)
        {
            Debug.LogWarning("[OpeningSetupTools] Abra a cena Abertura1995 primeiro.");
            return;
        }
        var data = new SerializedObject(director);
        data.FindProperty("scannerStatic").boxedValue = Sound(FirstSound("event:/Opening/Scanner", "event:/Effects/police scanner", "event:/Events/static_burst"));
        data.FindProperty("switchSound").boxedValue = Sound(FirstSound("event:/Opening/Light_Switch", "event:/Effects/light switch", "event:/Effects/Key"));
        data.FindProperty("sirenSound").boxedValue = Sound(FirstSound("event:/Opening/Siren", "event:/Ambience/police siren"));
        data.FindProperty("rainAmbience").boxedValue = Sound(FirstSound("event:/Opening/Rain", "event:/Opening/horror rain", "event:/Effects/horror rain"));
        data.ApplyModifiedPropertiesWithoutUndo();
        AssignVoices(director);

        var clara = Object.FindAnyObjectByType<ClaraPresence>();
        if (clara != null)
        {
            var claraData = new SerializedObject(clara);
            claraData.FindProperty("flashSound").boxedValue = Sound(FirstSound("event:/Opening/Camera_Flash", "event:/Opening/camera flash", "event:/Effects/camera flash"));
            claraData.ApplyModifiedPropertiesWithoutUndo();
        }
        EditorSceneManager.MarkSceneDirty(director.gameObject.scene);
        Debug.Log("[OpeningSetupTools] Sons e vozes da abertura atualizados. Salve a cena.");
    }

    // =====================================================================
    // Luz da cena e ordem de build
    // =====================================================================

    private static void ApplyLighting(Roots roots, Materials m)
    {
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.1f, 0.105f, 0.13f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.015f, 0.017f, 0.022f);
        RenderSettings.fogDensity = 0.012f;
        RenderSettings.reflectionIntensity = 0.25f;
        RenderSettings.sun = null;

        // Luz fria da rua entrando pela janela, para o quarto não ficar preto antes da luz acender
        var street = new GameObject("Luz_Rua_Janela");
        street.transform.SetParent(roots.lighting, false);
        street.transform.position = new Vector3(WindowX + 3f, CeilingY + 6f, RoomMaxZ + 14f);
        street.transform.LookAt(new Vector3(WindowX - 2f, 0f, RoomMaxZ - 10f));
        var light = street.AddComponent<Light>();
        light.type = LightType.Spot;
        light.color = new Color(0.5f, 0.58f, 0.78f);
        light.intensity = 30f;
        light.range = 60f;
        light.spotAngle = 55f;
        light.shadows = LightShadows.Soft;
        light.lightmapBakeType = LightmapBakeType.Realtime;
    }

    private static void AddToBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(s => s.path == ScenePath);
        int gameIndex = scenes.FindIndex(s => s.path == GameScenePath);
        var entry = new EditorBuildSettingsScene(ScenePath, true);
        if (gameIndex >= 0) scenes.Insert(gameIndex, entry);
        else scenes.Add(entry);
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // =====================================================================
    // Apoio
    // =====================================================================

    private static Transform Group(string name, Transform parent)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        return group.transform;
    }

    private static GameObject Box(string name, Transform parent, Vector3 min, Vector3 max, Material material, float tile, bool collider = true)
    {
        var box = new GameObject(name);
        box.transform.SetParent(parent, false);
        Vector3 size = max - min;
        box.transform.position = (min + max) * 0.5f;
        if (size.sqrMagnitude > 0f) box.AddComponent<MeshFilter>().sharedMesh = BoxMesh(size, tile);
        else box.AddComponent<MeshFilter>();
        var renderer = box.AddComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        if (collider && size.sqrMagnitude > 0f) box.AddComponent<BoxCollider>().size = size;
        return box;
    }

    private static GameObject Primitive(PrimitiveType type, string name, Transform parent, Material material)
    {
        GameObject primitive = GameObject.CreatePrimitive(type);
        Object.DestroyImmediate(primitive.GetComponent<Collider>());
        primitive.name = name;
        primitive.transform.SetParent(parent, false);
        if (material != null) primitive.GetComponent<MeshRenderer>().sharedMaterial = material;
        return primitive;
    }

    /// <summary>
    /// Torna o interruptor uma ação do jogador: caixa de colisão um pouco maior que o modelo (fácil de
    /// mirar no escuro) e o StoryInteractable que o roteiro libera ao entrar no quarto.
    /// </summary>
    public static StoryInteractable AddSwitchInteraction(GameObject lightSwitch)
    {
        // O collider vem primeiro: o StoryInteractable exige um e o AddComponent falha sem ele.
        // (Sem "??": o operador ignora o "nulo" especial dos objetos da Unity.)
        var box = lightSwitch.GetComponent<BoxCollider>();
        if (box == null) box = lightSwitch.AddComponent<BoxCollider>();
        box.isTrigger = false;
        Bounds bounds = BoundsOf(lightSwitch);
        Vector3 size = lightSwitch.transform.InverseTransformVector(bounds.size);
        box.center = lightSwitch.transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(Mathf.Max(Mathf.Abs(size.x), 1.1f), Mathf.Max(Mathf.Abs(size.y), 1.6f), Mathf.Max(Mathf.Abs(size.z), 0.5f));

        var interactable = lightSwitch.GetComponent<StoryInteractable>();
        if (interactable == null) interactable = lightSwitch.AddComponent<StoryInteractable>();
        interactable.EditorSetup("PROMPT_LIGHT_SWITCH", 7f, false);
        return interactable;
    }

    private static StoryInteractable ClueVolume(string name, Transform parent, Vector3 center, Vector3 size, bool local)
    {
        var clue = new GameObject(name);
        clue.transform.SetParent(parent, false);
        if (local) clue.transform.localPosition = center;
        else clue.transform.position = center;
        clue.transform.rotation = parent.rotation;
        var box = clue.AddComponent<BoxCollider>();
        box.isTrigger = false;
        box.size = size;
        var interactable = clue.AddComponent<StoryInteractable>();
        interactable.EditorSetup("PROMPT_EXAMINE", 7f, false);
        return interactable;
    }

    private static void Decal(string name, Transform parent, Vector3 position, Vector3 size, Material material)
    {
        var decal = new GameObject(name);
        decal.transform.SetParent(parent, false);
        decal.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, 0f, 0f));
        var projector = decal.AddComponent<DecalProjector>();
        projector.material = material;
        projector.size = size;
        projector.pivot = new Vector3(0f, 0f, size.z * 0.5f);
    }

    private static string FirstSound(params string[] paths) => paths.FirstOrDefault(p => FindEvent(p) != null) ?? paths[0];

    private static EventReference Sound(string path)
    {
        EditorEventRef found = FindEvent(path);
        return found == null ? default : new EventReference { Path = found.Path, Guid = found.Guid };
    }

    // EventFromPath às vezes não acha eventos novos (com espaço no nome) logo depois de atualizar os
    // bancos; a lista de eventos do cache sempre tem
    private static EditorEventRef FindEvent(string path) =>
        EventManager.EventFromPath(path)
        ?? EventManager.Events.FirstOrDefault(e => string.Equals(e.Path, path, System.StringComparison.OrdinalIgnoreCase));
}
