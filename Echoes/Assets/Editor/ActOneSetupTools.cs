using System.IO;
using System.Linq;
using FMODUnity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Monta o fim do Ato 1 da demo na cena ApartamentoDemo (roteiro, Ato 1, seções 8 e 9): casaco, corrente da
/// porta e cepo de facas; o clímax no corredor; a hora perdida com o que muda no apartamento; e o
/// cartão final. Rodar de novo apaga o grupo "Ato1_Demo" e monta tudo outra vez.
/// </summary>
public static class ActOneSetupTools
{
    private const string GroupName = "Ato1_Demo";
    private const string TextureFolder = "Assets/Textures/Demo";
    private const string MaterialFolder = "Assets/Materials/Demo";

    // Medidas da cena (cerca de 4,8 unidades por metro)
    private const float FloorY = 0.17f;
    private const float EntranceWallX = 21.32f;      // face interna da parede da porta de entrada
    private const float EntranceDoorInnerX = 21.23f; // face interna da porta
    private const float DoorCenterZ = -33.61f;
    private static readonly Vector3 HallFrom = new Vector3(38.5f, 0.6f, -33.6f); // porta do 3B
    private static readonly Vector3 HallTo = new Vector3(22.6f, 0.6f, -33.6f);   // porta de Ethan

    [MenuItem("Echoes/Roteiro/Montar Ato 1 da demo")]
    private static void Setup()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != "Assets/Scenes/ApartamentoDemo.unity")
        {
            Debug.LogError("[ActOneSetupTools] Abra a cena ApartamentoDemo.");
            return;
        }

        EnsureDemoSettings();
        Directory.CreateDirectory(TextureFolder);
        Directory.CreateDirectory(MaterialFolder);

        GameObject story = scene.GetRootGameObjects().First(r => r.name == "Story");
        Transform old = story.transform.Find(GroupName);
        if (old != null) Object.DestroyImmediate(old.gameObject);
        var group = new GameObject(GroupName);
        group.transform.SetParent(story.transform, false);

        Material wetPrint = WetMaterial("Pegada molhada", Texture("pegada", DrawFootprint));
        Material puddle = WetMaterial("Poca", Texture("poca", DrawPuddle));
        Material gapLight = GlowMaterial("Fresta luz", Texture("fresta_luz", (u, v) => DrawGap(u, v, false)));
        Material gapShadow = GlowMaterial("Fresta sombra", Texture("fresta_sombra", (u, v) => DrawGap(u, v, true)));

        // As instâncias que já estavam na cena (com a escala antiga) dão lugar às montadas aqui
        RemoveLooseInstances(group.transform);

        // Casaco seco (antes) e encharcado (depois da hora perdida), no canto ao lado da porta,
        // no lugar e na orientação em que ele já estava na cena
        Vector3 coatPosition = new Vector3(16.29f, 3.29f, -40.49f);
        GameObject dryCoat = Place("Models/leather coat/leather coat.prefab", "Casaco", group.transform, coatPosition, Quaternion.identity);
        // O prefab do casaco tem escala 508 na raiz: poça e gotas ficam ao lado dele, num objeto
        // de escala 1, e não como filhas do modelo
        var wetCoat = new GameObject("Casaco_Molhado");
        wetCoat.transform.SetParent(group.transform, false);
        wetCoat.transform.position = coatPosition;
        GameObject wetModel = Place("Models/leather coat/leather coat.prefab", "Casaco", wetCoat.transform, coatPosition, Quaternion.identity);
        MakeWet(wetModel);
        AddDrips(wetCoat);
        Flat("Poca_Casaco", wetCoat.transform, new Vector3(coatPosition.x, FloorY + 0.012f, coatPosition.z + 0.6f), 0f, new Vector2(3.2f, 2.4f), puddle);
        SimpleItemDisplay coatDisplay = MakeInspectable(wetModel, "IS_WET_COAT");
        wetCoat.SetActive(false);

        // Corrente da porta: presa antes, solta depois (na porta, acompanha quando ela abre)
        Transform door = Object.FindObjectsByType<DoorController>(FindObjectsInactive.Include)
            .Where(d => d.name == "Pivot_Entrada" && d.transform.position.x < 100f).First().transform.Find("Porta2");
        // As correntes ficam na porta, fora do grupo: limpa as de uma montagem anterior
        foreach (Transform child in door.Cast<Transform>().Where(c => c.name.StartsWith("Corrente_")).ToArray())
            Object.DestroyImmediate(child.gameObject);
        Vector3 chainPosition = new Vector3(EntranceDoorInnerX - 0.03f, 6.84f, -31.45f);
        GameObject chainLatched = Place("Models/brass door chain/tranca fechada.prefab", "Corrente_Presa", door, chainPosition, Quaternion.Euler(0f, 90f, 0f), mirrorX: true);
        GameObject chainLoose = Place("Models/brass door chain/tranca aberta.prefab", "Corrente_Solta", door, chainPosition, Quaternion.Euler(0f, 90f, 0f), mirrorX: true);
        chainLoose.SetActive(false);

        // Cepo de facas na bancada, onde ele já estava na cena, apoiado no tampo
        GameObject block = Place("Models/wooden kitchen knife/cepo de facas.prefab", "Cepo_Facas", group.transform, new Vector3(19.18f, 5.25f, -20.31f), Quaternion.identity);
        RestOnSurface(block);
        GameObject knife = Object.FindObjectsByType<ItemInteract>(FindObjectsInactive.Include)
            .First(i => i.name == "FACA" && i.transform.parent.name == "R_INTERATIVOS").gameObject;

        // Pegadas molhadas: no corredor (vistas pelo olho mágico) e da porta até a poltrona
        GameObject hallPrints = Footprints("Pegadas_Corredor", group.transform, HallFrom.x - 1f, HallTo.x + 0.5f, -33.6f, wetPrint, needsFloorHit: false);
        GameObject homePrints = Footprints("Pegadas_Apartamento", group.transform, 20.2f, -16.5f, -33.2f, wetPrint, needsFloorHit: true);
        hallPrints.SetActive(false);
        homePrints.SetActive(false);

        // Fresta de luz sob a porta de entrada, com e sem a sombra dos pés
        GameObject gapOn = Flat("Fresta_Luz", group.transform, new Vector3(EntranceDoorInnerX - 0.25f, FloorY + 0.01f, DoorCenterZ), -90f, new Vector2(5.4f, 0.5f), gapLight);
        GameObject gapFeet = Flat("Fresta_Sombra", group.transform, gapOn.transform.position, -90f, new Vector2(5.4f, 0.5f), gapShadow);
        gapFeet.SetActive(false);

        // Luz acesa dentro do 3B
        var lightObject = new GameObject("Luz_3B");
        lightObject.transform.SetParent(group.transform, false);
        lightObject.transform.position = new Vector3(41.5f, 5f, -33.6f);
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(1f, 0.78f, 0.5f);
        light.intensity = 3f;
        light.range = 14f;
        light.shadows = LightShadows.None;
        light.lightmapBakeType = LightmapBakeType.Realtime;
        lightObject.SetActive(false);

        Transform stepsFrom = Marker("Passos_De", group.transform, HallFrom);
        Transform stepsTo = Marker("Passos_Ate", group.transform, HallTo);

        // A hora perdida: Ethan acorda na poltrona à 1h13 e o apartamento mudou
        var lostObject = new GameObject("TempoPerdido_Ato1");
        lostObject.transform.SetParent(group.transform, false);
        var lostTime = lostObject.AddComponent<LostTimeEvent>();
        lostObject.AddComponent<SaveableId>();
        var lostData = new SerializedObject(lostTime);
        lostData.FindProperty("triggerOnAct").boolValue = false;
        lostData.FindProperty("triggerOnPlayerEnter").boolValue = false;
        lostData.FindProperty("wakeAtFixedTime").boolValue = true;
        lostData.FindProperty("wakeHour").intValue = 1;
        lostData.FindProperty("wakeMinute").intValue = 13;
        lostData.FindProperty("blackoutSeconds").floatValue = 3f;
        lostData.FindProperty("blackoutSound").boxedValue = EventReference.Find("event:/Ambience/Thunder");
        lostData.FindProperty("wakeUpPoint").objectReferenceValue = GameObject.Find("Story/Checkpoint_Poltrona").transform;
        SetChanges(lostData.FindProperty("changes"),
            (dryCoat, false), (wetCoat, true), (chainLatched, false), (chainLoose, true), (homePrints, true), (knife, false));
        lostData.ApplyModifiedPropertiesWithoutUndo();

        // O clímax no corredor
        var climaxObject = new GameObject("Climax_Corredor");
        climaxObject.transform.SetParent(group.transform, false);
        var climax = climaxObject.AddComponent<CorridorClimax>();
        climaxObject.AddComponent<SaveableId>();
        climax.EditorSetup(
            GameObject.Find("Story/Eventos/Vizinhos/pivot").transform,
            new[] { lightObject, hallPrints },
            EventReference.Find("event:/Footsteps/Footsteps"), stepsFrom, stepsTo,
            gapOn, gapFeet,
            new InnerVoiceLine { text = new LocalizedString(InnerVoice.Table, "IV_WHO_THERE") },
            Object.FindObjectsByType<Peephole>(FindObjectsInactive.Include).First(),
            EventReference.Find("event:/Effects/broken_light"),
            EventReference.Find("event:/Ambience/Thunder"),
            lostTime);

        // Fim da demo e o que fica de fora dela
        var demoObject = new GameObject("Demo");
        demoObject.transform.SetParent(group.transform, false);
        demoObject.AddComponent<DemoEnding>().EditorSetup(coatDisplay);
        GameObject killerLetter = Object.FindObjectsByType<ItemInteract>(FindObjectsInactive.Include).First(i => i.name == "Carta_Amigo").gameObject;
        demoObject.AddComponent<DemoContent>().EditorSetup(new[] { killerLetter });

        RemoveJumpScare();
        var delivery = new SerializedObject(Object.FindAnyObjectByType<NeighborNoteDelivery>(FindObjectsInactive.Include));
        delivery.FindProperty("delaySeconds").floatValue = 150f; // depois do bilhete de boas-vindas, não junto
        delivery.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[ActOneSetupTools] Ato 1 da demo montado. Salve a cena.");
    }

    // ---------- Objetos ----------

    // Usa os prefabs salvos em Assets/Models (com materiais e escala já ajustados), mantendo a
    // rotação e a escala deles; "rotation" é um giro extra no mundo
    private static GameObject Place(string modelPath, string name, Transform parent, Vector3 position, Quaternion rotation, bool mirrorX = false)
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/" + modelPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.name = name;
        instance.transform.SetPositionAndRotation(position, rotation * instance.transform.rotation);
        instance.transform.SetParent(parent, worldPositionStays: true);
        if (mirrorX)
        {
            Vector3 scale = instance.transform.localScale;
            instance.transform.localScale = new Vector3(-scale.x, scale.y, scale.z);
        }
        foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return instance;
    }

    private static void RemoveLooseInstances(Transform group)
    {
        string[] models = { "leather coat", "brass door chain", "wooden kitchen knife" };
        foreach (GameObject root in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if (root == null || !root.scene.IsValid() || !PrefabUtility.IsAnyPrefabInstanceRoot(root) || root.transform.IsChildOf(group)) continue;
            string path = AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(root));
            bool onDoor = root.transform.parent != null && root.transform.parent.name == "Porta2";
            if (models.Any(m => path.Contains(m)) && !onDoor) Object.DestroyImmediate(root);
        }
    }

    private static void RestOnSurface(GameObject target)
    {
        Bounds bounds = target.GetComponentsInChildren<Renderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        if (Physics.Raycast(new Vector3(bounds.center.x, bounds.max.y + 3f, bounds.center.z), Vector3.down, out RaycastHit hit, 10f))
            target.transform.position += Vector3.up * (hit.point.y - bounds.min.y);
    }

    private static void MakeWet(GameObject coat)
    {
        foreach (Renderer renderer in coat.GetComponentsInChildren<Renderer>())
        {
            Material source = renderer.sharedMaterial;
            string path = $"{MaterialFolder}/{source.name} molhado.mat";
            var wet = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (wet == null)
            {
                wet = new Material(source);
                AssetDatabase.CreateAsset(wet, path);
            }
            wet.CopyPropertiesFromMaterial(source);
            if (wet.HasProperty("_BaseColor")) wet.SetColor("_BaseColor", source.GetColor("_BaseColor") * 0.35f);
            if (wet.HasProperty("_Smoothness")) wet.SetFloat("_Smoothness", 0.85f);
            EditorUtility.SetDirty(wet);
            renderer.sharedMaterial = wet;
        }
    }

    private static void AddDrips(GameObject coat)
    {
        var drips = new GameObject("Gotas");
        drips.transform.SetParent(coat.transform, false);
        drips.transform.position = coat.transform.position + Vector3.up * 0.3f;
        var system = drips.AddComponent<ParticleSystem>();
        var main = system.main;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;
        main.startSize = 0.05f;
        main.gravityModifier = 2f;
        main.startColor = new Color(0.7f, 0.75f, 0.8f, 0.7f);
        main.maxParticles = 30;
        var emission = system.emission;
        emission.rateOverTime = 4f;
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(2.4f, 0.05f, 0.6f);
        var renderer = drips.GetComponent<ParticleSystemRenderer>();
        string path = $"{MaterialFolder}/Gota.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            AssetDatabase.CreateAsset(material, path);
        }
        renderer.sharedMaterial = material;
    }

    private static SimpleItemDisplay MakeInspectable(GameObject target, string textKey)
    {
        // Só as malhas do próprio objeto: partículas e quads ao redor não entram no colisor
        Bounds bounds = target.GetComponentsInChildren<MeshRenderer>().Select(r => r.bounds).Aggregate((a, b) => { a.Encapsulate(b); return a; });
        var collider = target.AddComponent<BoxCollider>();
        collider.center = target.transform.InverseTransformPoint(bounds.center);
        Vector3 size = target.transform.InverseTransformVector(bounds.size);
        collider.size = new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z));

        var display = target.AddComponent<SimpleItemDisplay>();
        var data = new SerializedObject(display);
        data.FindProperty("promptString").boxedValue = new LocalizedString("ItemsSubtitles", "IS_INSPECT");
        data.FindProperty("displayTextString").boxedValue = new LocalizedString("ItemsSubtitles", textKey);
        data.ApplyModifiedPropertiesWithoutUndo();
        return display;
    }

    private static GameObject Footprints(string name, Transform parent, float fromX, float toX, float z, Material material, bool needsFloorHit)
    {
        var group = new GameObject(name);
        group.transform.SetParent(parent, false);
        float stride = 3.4f;
        int count = Mathf.FloorToInt(Mathf.Abs(toX - fromX) / stride);
        float direction = Mathf.Sign(toX - fromX);
        for (int i = 0; i <= count; i++)
        {
            float x = fromX + direction * stride * i;
            float side = i % 2 == 0 ? 0.32f : -0.32f;
            var position = new Vector3(x, FloorY + 0.01f, z + side + Random.Range(-0.08f, 0.08f));

            if (needsFloorHit)
            {
                // Pula onde houver móvel por cima do piso
                if (!Physics.Raycast(new Vector3(position.x, 9f, position.z), Vector3.down, out RaycastHit hit, 12f) || hit.point.y > FloorY + 0.3f) continue;
            }

            float yaw = direction < 0 ? -90f : 90f;
            GameObject print = Flat($"Pegada_{i:00}", group.transform, position, yaw + Random.Range(-6f, 6f), new Vector2(0.55f, 1.4f), material);
            if (i % 2 == 1) print.transform.localScale = new Vector3(-0.55f, 1.4f, 1f); // pé esquerdo
        }
        return group;
    }

    private static GameObject Flat(string name, Transform parent, Vector3 position, float yaw, Vector2 size, Material material)
    {
        GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Object.DestroyImmediate(quad.GetComponent<Collider>());
        quad.name = name;
        quad.transform.SetParent(parent, false);
        quad.transform.SetPositionAndRotation(position, Quaternion.Euler(90f, yaw, 0f));
        quad.transform.localScale = new Vector3(size.x, size.y, 1f);
        var renderer = quad.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        return quad;
    }

    private static Transform Marker(string name, Transform parent, Vector3 position)
    {
        var marker = new GameObject(name);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        return marker.transform;
    }

    private static void SetChanges(SerializedProperty changes, params (GameObject target, bool active)[] items)
    {
        changes.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
        {
            SerializedProperty change = changes.GetArrayElementAtIndex(i);
            change.FindPropertyRelative("target").objectReferenceValue = items[i].target;
            change.FindPropertyRelative("moveTo").objectReferenceValue = null;
            change.FindPropertyRelative("changeActive").boolValue = true;
            change.FindPropertyRelative("active").boolValue = items[i].active;
            change.FindPropertyRelative("open").boolValue = false;
        }
    }

    // O documento de design tira o jumpscare do vizinho: o medo vem da dúvida
    private static void RemoveJumpScare()
    {
        var manager = Object.FindAnyObjectByType<NeighborEventManager>(FindObjectsInactive.Include);
        var data = new SerializedObject(manager);
        SerializedProperty library = data.FindProperty("eventLibrary");
        for (int i = library.arraySize - 1; i >= 0; i--)
        {
            SerializedProperty type = library.GetArrayElementAtIndex(i).FindPropertyRelative("type");
            if (type.enumNames[type.enumValueIndex] == "JumpScare") library.DeleteArrayElementAtIndex(i);
        }
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void EnsureDemoSettings()
    {
        const string path = "Assets/Resources/DemoSettings.asset";
        if (AssetDatabase.LoadAssetAtPath<DemoSettings>(path) != null) return;
        AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<DemoSettings>(), path);
    }

    // ---------- Materiais e texturas geradas ----------

    private static Material WetMaterial(string name, Texture2D texture)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Lit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(0.26f, 0.22f, 0.17f, 0.8f)); // lama molhada, visível no piso escuro
        material.SetFloat("_Smoothness", 0.92f);
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GlowMaterial(string name, Texture2D texture)
    {
        Material material = GetMaterial(name, "Universal Render Pipeline/Unlit");
        material.SetTexture("_BaseMap", texture);
        material.SetColor("_BaseColor", new Color(1f, 0.82f, 0.55f, 0.55f));
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 2f); // aditivo
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Material GetMaterial(string name, string shader)
    {
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find(shader));
            AssetDatabase.CreateAsset(material, path);
        }
        return material;
    }

    private static Texture2D Texture(string name, System.Func<float, float, float> alpha)
    {
        string path = $"{TextureFolder}/{name}.png";
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                pixels[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha((x + 0.5f) / size, (y + 0.5f) / size)) * 255f));
        texture.SetPixels32(pixels);
        File.WriteAllBytes(path, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 256;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    // Sola de sapato: antepé e calcanhar, com borda irregular de água
    private static float DrawFootprint(float u, float v)
    {
        float front = Ellipse(u, v, 0.5f, 0.66f, 0.3f, 0.3f);
        float heel = Ellipse(u, v, 0.5f, 0.2f, 0.24f, 0.16f);
        float noise = Mathf.PerlinNoise(u * 18f, v * 18f) * 0.5f + 0.5f;
        return Mathf.Max(front, heel) * noise;
    }

    private static float DrawPuddle(float u, float v)
    {
        float blob = Mathf.Max(Ellipse(u, v, 0.5f, 0.5f, 0.38f, 0.3f), Ellipse(u, v, 0.35f, 0.62f, 0.2f, 0.18f), Ellipse(u, v, 0.66f, 0.4f, 0.2f, 0.2f));
        return blob * (Mathf.PerlinNoise(u * 9f, v * 9f) * 0.4f + 0.6f);
    }

    // Faixa de luz que entra pela fresta: forte junto à porta (v = 0), some para dentro
    private static float DrawGap(float u, float v, bool feet)
    {
        float strip = Mathf.Pow(1f - v, 3f) * Mathf.SmoothStep(0f, 1f, Mathf.Min(u, 1f - u) * 8f);
        if (feet)
        {
            float shadow = Mathf.Max(Ellipse(u, v, 0.42f, 0.1f, 0.07f, 0.5f), Ellipse(u, v, 0.58f, 0.1f, 0.07f, 0.5f));
            strip *= 1f - shadow;
        }
        return strip;
    }

    private static float Ellipse(float u, float v, float cu, float cv, float ru, float rv)
    {
        float d = Mathf.Sqrt(Mathf.Pow((u - cu) / ru, 2f) + Mathf.Pow((v - cv) / rv, 2f));
        return Mathf.Clamp01((1f - d) * 4f);
    }
}
