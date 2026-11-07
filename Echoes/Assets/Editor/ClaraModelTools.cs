using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Coloca o modelo animado da Clara na abertura de 1995 (menu Echoes > Roteiro). Cria o Animator
/// Controller (parada/andando e a foto), troca a silhueta de sombra pelo modelo, prende a câmera na mão
/// direita na pose do disparo e marca o ponto da porta do quarto 6, onde ela para. Pode rodar de novo.
/// </summary>
public static class ClaraModelTools
{
    private const string ScenePath = "Assets/Scenes/Abertura1995.unity";
    private const string ControllerPath = "Assets/Anim/Clara.controller";
    private const string ClaraPrefabPath = "Assets/Models/Clara/Clara_rigged.prefab";
    private const string ClaraModelPath = "Assets/Models/Clara/Clara_rigged.fbx";
    private const string CameraPrefabPath = "Assets/Models/camera/camera.prefab";
    private const string IdlePath = "Assets/Anim/Idle.fbx";
    private const string WalkPath = "Assets/Anim/Walking.fbx";
    private const string PhotoPath = "Assets/Models/Clara/clara anim.fbx";

    private const string ArmMaskPath = "Assets/Anim/Clara_BracoDireito.mask";
    private const string CarryClipPath = "Assets/Anim/Clara_CarregarCamera.anim";
    // Ajustes da pose de carregar, em músculos do Humanoid (-1 a 1)
    private const float CarryArmForward = 0.12f;
    private const float CarryArmOut = 0.05f;

    // Altura da Clara na cena (a silhueta antiga tinha ~7,5 unidades; o modelo tem 1,9 m)
    private const float ModelScale = 4.0f;

    // Pose da câmera no quadro do disparo, no espaço do modelo (medida no Blender)
    private static readonly Vector3 CameraAtShutter = new Vector3(0.044f, 1.705f, 0.207f);
    private const float CameraScale = 0.208f;
    private const float ShutterTime = 1.2f;

    // No corredor, logo antes da soleira da porta 6 (que começa em z ≈ 62,1)
    private static readonly Vector3 DoorSpotPosition = new Vector3(0.9f, 0f, 60.4f);

    [MenuItem("Echoes/Roteiro/Clara: modelo, câmera e animações")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[ClaraModelTools] Saia do play mode."); return; }

        AnimatorController controller = BuildController();
        if (controller == null) return;

        Scene scene = SceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        ClaraPresence clara = scene.GetRootGameObjects().Select(r => r.GetComponentInChildren<ClaraPresence>(true)).FirstOrDefault(c => c != null);
        if (clara == null) { Debug.LogError("[ClaraModelTools] ClaraPresence não encontrado em " + ScenePath); return; }

        // A silhueta de sombra sai; o modelo projeta a própria sombra
        Transform shadow = clara.transform.Find("Sombra");
        if (shadow != null) shadow.gameObject.SetActive(false);

        Transform old = clara.transform.Find("Modelo");
        if (old != null) Object.DestroyImmediate(old.gameObject);

        var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ClaraPrefabPath), scene);
        model.name = "Modelo";
        model.transform.SetParent(clara.transform, false);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;
        model.transform.localScale = Vector3.one * ModelScale;

        Animator animator = model.GetComponent<Animator>();
        if (animator == null) animator = model.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(ClaraModelPath).OfType<Avatar>().First();
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        if (model.GetComponent<ClaraAnimationEvents>() == null) model.AddComponent<ClaraAnimationEvents>();
        foreach (Renderer r in model.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        AttachCamera(model, animator);

        Transform door = clara.transform.parent != null ? clara.transform.parent.Find("Ponto_Porta_Clara") : null;
        if (door == null)
        {
            door = new GameObject("Ponto_Porta_Clara").transform;
            door.SetParent(clara.transform.parent, false);
        }
        door.position = DoorSpotPosition;
        door.rotation = Quaternion.LookRotation(Vector3.forward);

        Undo.RecordObject(clara, "Clara modelo");
        clara.EditorSetupModel(animator, door);
        EditorUtility.SetDirty(clara);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (openedHere) EditorSceneManager.CloseScene(scene, true);
        Debug.Log("[ClaraModelTools] Clara com modelo, câmera na mão e ponto da porta.");
    }

    // Parada/andando por Speed (0 a 1) e a foto pelo gatilho Photo
    private static AnimatorController BuildController()
    {
        AnimationClip Clip(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c => !c.name.StartsWith("__preview"));
        AnimationClip idle = Clip(IdlePath), walk = Clip(WalkPath), photo = Clip(PhotoPath);
        if (idle == null || walk == null || photo == null) { Debug.LogError("[ClaraModelTools] Faltam clipes (Idle, Walking ou Clara_Photo)."); return null; }

        // Reaproveita o controller (mantém as referências da cena) e refaz os estados
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath)
            ?? AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        foreach (AnimatorControllerParameter parameter in controller.parameters) controller.RemoveParameter(parameter);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Photo", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState child in machine.states) machine.RemoveState(child.state);
        foreach (Object sub in AssetDatabase.LoadAllAssetsAtPath(ControllerPath))
            if (sub is BlendTree) AssetDatabase.RemoveObjectFromAsset(sub);
        AnimatorState locomotion = controller.CreateBlendTreeInController("Locomocao", out BlendTree tree, 0);
        tree.blendType = BlendTreeType.Simple1D;
        tree.blendParameter = "Speed";
        tree.useAutomaticThresholds = false;
        tree.AddChild(idle, 0f);
        tree.AddChild(walk, 1f);
        machine.defaultState = locomotion;

        AnimatorState photoState = machine.AddState("Foto");
        photoState.motion = photo;

        AnimatorStateTransition toPhoto = locomotion.AddTransition(photoState);
        toPhoto.AddCondition(AnimatorConditionMode.If, 0f, "Photo");
        toPhoto.hasExitTime = false;
        toPhoto.duration = 0.2f;

        AnimatorStateTransition back = photoState.AddTransition(locomotion);
        back.hasExitTime = true;
        back.exitTime = 0.92f;
        back.duration = 0.25f;

        // Braço direito: carrega a câmera na frente da barriga enquanto anda ou espera (o balanço do
        // Walking jogava o braço para trás e a câmera entrava no corpo). Na foto, segue o clipe da foto.
        while (controller.layers.Length > 1) controller.RemoveLayer(controller.layers.Length - 1);
        controller.AddLayer(new AnimatorControllerLayer
        {
            name = "Braco direito (camera)",
            defaultWeight = 1f,
            avatarMask = BuildRightArmMask(),
            syncedLayerIndex = 0,
            stateMachine = new AnimatorStateMachine(),
        });
        AnimatorControllerLayer[] layers = controller.layers;
        layers[1].SetOverrideMotion(locomotion, BuildCarryClip(photo));
        layers[1].SetOverrideMotion(photoState, photo);
        controller.layers = layers;

        AssetDatabase.SaveAssets();
        return controller;
    }

    // Só o braço e os dedos da mão direita
    private static AvatarMask BuildRightArmMask()
    {
        var mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(ArmMaskPath);
        if (mask == null)
        {
            mask = new AvatarMask();
            AssetDatabase.CreateAsset(mask, ArmMaskPath);
        }
        for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
        {
            var part = (AvatarMaskBodyPart)i;
            mask.SetHumanoidBodyPartActive(part, part == AvatarMaskBodyPart.RightArm || part == AvatarMaskBodyPart.RightFingers);
        }
        mask.transformCount = 0;
        EditorUtility.SetDirty(mask);
        return mask;
    }

    // Pose parada do início da foto (câmera na frente da barriga), com o braço um pouco mais à
    // frente e afastado do corpo, para a câmera não encostar na jaqueta quando o tronco balança
    private static AnimationClip BuildCarryClip(AnimationClip photo)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(CarryClipPath);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, CarryClipPath);
        }
        clip.ClearCurves();
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(photo))
        {
            AnimationCurve source = AnimationUtility.GetEditorCurve(photo, binding);
            if (source == null || source.length == 0) continue;
            float value = source.Evaluate(0f);
            if (binding.propertyName == "Right Arm Front-Back") value += CarryArmForward;
            if (binding.propertyName == "Right Arm Down-Up") value += CarryArmOut;
            AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, 1f, value));
        }
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    // Põe a Clara na pose do disparo, coloca a câmera diante do olho dela e prende na mão direita
    private static void AttachCamera(GameObject model, Animator animator)
    {
        Transform hand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        AnimationClip photo = AssetDatabase.LoadAllAssetsAtPath(PhotoPath).OfType<AnimationClip>().First(c => !c.name.StartsWith("__preview"));

        AnimationMode.StartAnimationMode();
        try
        {
            AnimationMode.BeginSampling();
            AnimationMode.SampleAnimationClip(model, photo, ShutterTime);
            AnimationMode.EndSampling();

            var cam = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(CameraPrefabPath), model.scene);
            cam.name = "Camera_Fotografica";
            cam.transform.SetPositionAndRotation(model.transform.TransformPoint(CameraAtShutter), model.transform.rotation);
            cam.transform.localScale = Vector3.one * CameraScale * ModelScale;
            Vector3 position = cam.transform.position;
            Quaternion rotation = cam.transform.rotation;
            Vector3 scale = cam.transform.lossyScale;

            // Guarda a pose relativa à mão nesta pose e reaplica depois que a pose de repouso voltar
            Matrix4x4 handAtShutter = hand.localToWorldMatrix;
            Matrix4x4 relative = handAtShutter.inverse * Matrix4x4.TRS(position, rotation, Vector3.one);
            AnimationMode.StopAnimationMode();

            cam.transform.SetParent(hand, false);
            cam.transform.localPosition = relative.GetColumn(3);
            cam.transform.localRotation = relative.rotation;
            Vector3 handScale = hand.lossyScale;
            cam.transform.localScale = new Vector3(scale.x / handScale.x, scale.y / handScale.y, scale.z / handScale.z);
            foreach (Collider c in cam.GetComponentsInChildren<Collider>()) Object.DestroyImmediate(c);
        }
        finally
        {
            if (AnimationMode.InAnimationMode()) AnimationMode.StopAnimationMode();
        }
    }
}
