using System.Linq;
using Echoes.UI.WarningScreen;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Aplica no jogo os textos do roteiro da demo (aba "Roteiro da demo" do documento de design):
/// boletins do rádio, falas, itens, avisos de abertura e as pistas reais do mural.
/// Pode rodar de novo: sobrescreve os textos e reaproveita o que já foi criado.
/// </summary>
public static class ScriptSetupTools
{
    private const string BoardFolder = "Assets/Data/Board";
    private const string EvidenceTable = "evidence";

    // ---------- Rádio ----------

    // Faixa 1 (Prólogo). O índice 5 é o sussurro, sem rótulo de quem fala.
    private static readonly (string key, string pt, string en)[] Track1 =
    {
        ("T1_1", "WZNY Notícias, plantão da noite. Sexta-feira, 21 de novembro. Chove forte em todo o condado, e a polícia pede que ninguém saia de casa.",
                 "WZNY News, night desk. Friday, November twenty-first. Heavy rain across the county, and police are asking everyone to stay home."),
        ("T1_2", "Um corpo foi encontrado nesta madrugada no prédio da antiga Pensão Halley, na Rua Halley, fechado desde 1996.",
                 "A body was found early this morning in the old Halley boarding house on Halley Street, closed since 1996."),
        ("T1_3", "A vítima, uma mulher ainda não identificada, estava no segundo andar. A polícia não confirma a causa da morte.",
                 "The victim, a woman not yet identified, was on the second floor. Police won't confirm the cause of death."),
        ("T1_4", "Mas fontes do departamento dizem que o local e o método lembram os crimes do Açougueiro de Harrow Creek...",
                 "But department sources say the place and the method recall the crimes of the Harrow Creek Butcher..."),
        ("T1_5", "...o assassino que fez seis vítimas entre 1994 e 1996 e que a polícia deu como morto.",
                 "...the killer who claimed six victims between 1994 and 1996, and whom police declared dead."),
        ("T1_6", "...quarto seis...", "...room six..."),
        ("T1_7", "O delegado Raymond Cole pede calma e diz que não há motivo para pânico.",
                 "Police Chief Raymond Cole is asking for calm and says there is no reason to panic."),
        ("T1_8", "Duas pessoas continuam desaparecidas desde a semana passada: um entregador de 22 anos e uma enfermeira do Hospital St. Jude.",
                 "Two people are still missing since last week: a twenty-two-year-old delivery driver and a nurse from St. Jude's Hospital."),
        ("T1_9", "A recomendação é clara: tranquem as portas, não andem sozinhos depois de escurecer e liguem para o 911 diante de qualquer movimento estranho na vizinhança.",
                 "The advice is clear: lock your doors, don't walk alone after dark, and call 911 if you see anything strange in your neighborhood."),
        ("T1_10", "WZNY. Voltamos daqui a pouco.", "WZNY. We'll be right back."),
    };
    private const int Track1Whisper = 5;

    // Faixa 2 (Ato 1). O índice 2 é o sussurro.
    private static readonly (string key, string pt, string en)[] Track2 =
    {
        ("T2_1", "WZNY, plantão da noite. A tempestade derrubou a energia em parte da zona norte.",
                 "WZNY, night desk. The storm has knocked out power in parts of the north side."),
        ("T2_2", "Com o medo, as denúncias falsas triplicaram desde ontem. Psicólogos lembram que desconfiar é natural, mas que a paranoia se espalha rápido.",
                 "Fear has tripled the number of false reports since yesterday. Psychologists remind us that suspicion is natural, but paranoia spreads fast."),
        ("T2_3", "...olhe pelo olho mágico...", "...look through the peephole..."),
        ("T2_4", "Nos anos 90, o Açougueiro nunca foi visto entrando ou saindo. Para os vizinhos, era alguém comum.",
                 "In the nineties, the Butcher was never seen coming or going. To his neighbors, he was nobody special."),
        ("T2_5", "A polícia confirma que a vítima da Pensão Halley morreu entre uma e duas da manhã.",
                 "Police confirm the Halley boarding house victim died between one and two in the morning."),
        ("T2_6", "Ainda não há suspeitos. O delegado Cole pede que quem passou pela Rua Halley ontem à noite procure a polícia.",
                 "There are still no suspects. Chief Cole asks anyone who was on Halley Street last night to come forward."),
        ("T2_7", "Não abram a porta para estranhos. Nem para rostos conhecidos.",
                 "Don't open your door to strangers. Not even to familiar faces."),
        ("T2_8", "WZNY. Fiquem em casa.", "WZNY. Stay home."),
    };
    private const int Track2Whisper = 2;

    // ---------- Falas e itens ----------

    private static readonly (string key, string pt, string en)[] SubtitleLines =
    {
        ("SUB_TRACK_1", "Quarto seis... O locutor não disse o quarto. Disse?", "Room six... The anchor never said the room. Did he?"),
        ("SUB_TRACK_2", "Entre uma e duas. E às três alguém ligou um rádio aqui.", "Between one and two. And at three, someone turned on a radio here."),
        ("SUB_PORTRAIT_3", "Rua Halley, 95. A Clara disse que essa ia pra capa. Não foi.", "Halley Street, '95. Clara said this one would make the front page. It didn't."),
        ("SUB_PORTRAIT_4", "Minha primeira capa. Março de 94. A primeira vítima.", "My first front page. March '94. The first victim."),
        ("SUB_PORTRAIT_5", "Oito anos. Eu ainda espero o telefone tocar.", "Eight years. I'm still waiting for the phone to ring."),
    };

    private static readonly (string key, string pt, string en)[] SpeakerLines =
    {
        ("S_CLARA", "CLARA:", "CLARA:"),
        ("S_DISPATCH", "CENTRAL:", "DISPATCH:"),
    };

    private static readonly (string key, string pt, string en)[] InnerVoiceLines =
    {
        ("IV_TAKE_NOTE", "Preciso anotar isso antes que eu esqueça.", "I need to write this down before I forget."),
        ("IV_WHO_THERE", "Quem está aí?", "Who's there?"),
    };

    private static readonly (string key, string pt, string en)[] ItemSubtitleLines =
    {
        ("IS_WET_COAT", "Eu não saí. Eu não saí de casa.", "I didn't go out. I didn't leave the house."),
    };

    private static readonly (string key, string pt, string en)[] MenuLines =
    {
        ("M_DEMO_END", "Echoes — Capítulo 1\n\nObrigado por jogar a demo.\nAdicione à sua lista de desejos na Steam.",
                       "Echoes — Chapter 1\n\nThank you for playing the demo.\nAdd it to your Steam wishlist."),
    };

    private static readonly (string key, string pt, string en)[] ItemLines =
    {
        ("ITEM_DESC_TAPES",
            "Você, na janela da Pensão Halley, com a luz de uma viatura atrás. A luz deixa seu rosto meio no escuro. Dá para jurar que você está sorrindo.",
            "You, at the window of the Halley boarding house, a patrol car's lights behind you. The glare leaves half your face in shadow. You could swear you're smiling."),
        ("ITEM_DESC_SMILE",
            "Você e Clara Reyes na redação do Ledger, em 1994. Ela ri de alguma coisa que você disse. Você não lembra o quê.",
            "You and Clara Reyes in the Ledger newsroom, 1994. She's laughing at something you said. You can't remember what."),
        ("ITEM_DESC_KNIFE",
            "Uma faca de chef, pesada e bem afiada. Você não lembra de ter afiado.",
            "A heavy chef's knife, freshly sharpened. You don't remember sharpening it."),
        ("ITEM_NAME_HELLO", "Bilhete de boas-vindas", "Welcome note"),
        ("ITEM_DESC_HELLO",
            "Vizinho,\n\nDesculpe o barulho da mudança. Cheguei há pouco no 3B, do outro lado do corredor, e ainda estou arrumando as coisas. Se precisar de algo, é só bater.\n\n— W. Kessler",
            "Neighbor,\n\nSorry about the noise from the move. I just got into 3B, across the hall, and I'm still unpacking. If you need anything, just knock.\n\n— W. Kessler"),
        ("ITEM_DESC_MEDICAL_REPORT",
            "CLÍNICA NEW LEAF DE SAÚDE MENTAL\nDra. Helen Moore — Registro NY 0412337\n\nLAUDO PSIQUIÁTRICO\nPaciente: Ethan Myers\nData: 18/09/2003\n\nDiagnóstico: Transtorno de Estresse Pós-Traumático (TEPT) com sintomas dissociativos graves.\nSintomas: crises agudas com visão em túnel e alucinações, seguidas de perda de memória (\"apagões\").\n\nORIENTAÇÕES:\nAo primeiro sinal de crise, tomar uma dose de Serenil imediatamente. O timer no quarto serve de lembrete.\n\nOBSERVAÇÃO:\nO paciente mostra um padrão de hipervigilância: observa os vizinhos pelo olho mágico de forma compulsiva. Esse hábito agrava a paranoia e pode desencadear um episódio dissociativo. O paciente deve resistir a esse impulso.\n\nRISCO: deixar de tomar a medicação durante uma crise pode levar a choque sistêmico e parada cardíaca.\n\n__________________________\nDra. Helen Moore, psiquiatra",
            "NEW LEAF MENTAL HEALTH CLINIC\nDr. Helen Moore — NY License 0412337\n\nPSYCHIATRIC REPORT\nPatient: Ethan Myers\nDate: 09/18/2003\n\nDiagnosis: Post-Traumatic Stress Disorder (PTSD) with severe dissociative symptoms.\nSymptoms: acute episodes of tunnel vision and hallucinations, followed by memory loss (\"blackouts\").\n\nINSTRUCTIONS:\nAt the first sign of an episode, take one dose of Serenil immediately. The timer in the bedroom serves as a reminder.\n\nNOTE:\nThe patient shows a pattern of hypervigilance: he compulsively watches his neighbors through the peephole. This habit worsens his paranoia and may trigger a dissociative episode. The patient must resist this impulse.\n\nRISK: failing to take the medication during an episode may lead to systemic shock and cardiac arrest.\n\n__________________________\nDr. Helen Moore, psychiatrist"),
        ("ITEM_NAME_CLIPPING", "Recorte de jornal", "Newspaper clipping"),
        ("ITEM_DESC_CLIPPING",
            "Harrow Creek Ledger, 10 de fevereiro de 1995.\n\n\"GARÇONETE É A TERCEIRA VÍTIMA DO AÇOUGUEIRO\"\n\nRuth Pierce, 34 anos, foi encontrada morta no quarto 6 da Pensão Halley, na Rua Halley, 41. A polícia não tem suspeitos.\n\nTexto: Ethan Myers. Fotos: Clara Reyes.",
            "Harrow Creek Ledger, February 10, 1995.\n\n\"WAITRESS IS THE BUTCHER'S THIRD VICTIM\"\n\nRuth Pierce, 34, was found dead in room 6 of the Halley boarding house, 41 Halley Street. Police have no suspects.\n\nStory: Ethan Myers. Photos: Clara Reyes."),
    };

    // ---------- Mural ----------

    private static readonly (string key, string pt, string en)[] EvidenceLines =
    {
        ("EV_HALLEY_CLIPPING", "Recorte do Ledger", "Ledger clipping"),
        ("EV_HALLEY_CLIPPING_TEXT", "\"Garçonete é a terceira vítima do Açougueiro.\" Texto de Ethan Myers, fotos de Clara Reyes.", "\"Waitress is the Butcher's third victim.\" Story by Ethan Myers, photos by Clara Reyes."),
        ("EV_HALLEY_CLIPPING_PLACE", "Pensão Halley, Rua Halley, 41", "Halley boarding house, 41 Halley Street"),
        ("EV_BULLETIN_NOTE", "Anotação do boletim", "Notes from the bulletin"),
        ("EV_BULLETIN_NOTE_TEXT", "WZNY, 22h50: corpo de mulher na antiga Pensão Halley, segundo andar. A polícia não confirma a causa.", "WZNY, 10:50 p.m.: a woman's body at the old Halley boarding house, second floor. Police won't confirm the cause."),
        ("EV_BULLETIN_NOTE_PLACE", "Rua Halley, 41", "41 Halley Street"),
        ("EV_NEIGHBOR_NOTE", "Bilhete do vizinho", "Neighbor's note"),
        ("EV_NEIGHBOR_NOTE_TEXT", "Passado por baixo da porta. Reclama do meu rádio às três da manhã. No verso, uma lista de compras e um endereço.", "Slipped under the door. A complaint about my radio at three in the morning. On the back, a shopping list and an address."),
        ("EV_NEIGHBOR_NOTE_PLACE", "Rua Halley, 41", "41 Halley Street"),
        ("EV_CLINIC_REPORT", "Laudo da New Leaf", "New Leaf report"),
        ("EV_CLINIC_REPORT_TEXT", "TEPT com sintomas dissociativos. Serenil ao primeiro sinal de crise. Evitar o olho mágico.", "PTSD with dissociative symptoms. Serenil at the first sign of an episode. Stay away from the peephole."),
        ("EV_CLINIC_REPORT_PLACE", "Clínica New Leaf", "New Leaf Clinic"),
        ("DED_HALLEY_RETURN", "Oito anos depois, ele voltou para a Pensão Halley.", "Eight years later, he went back to the Halley boarding house."),
        ("DED_HALLEY_RETURN_THOUGHT", "O mesmo lugar. Ele quer que alguém lembre.", "The same place. He wants someone to remember."),
        ("DED_NEIGHBOR_ADDRESS", "O vizinho tem o endereço da pensão.", "The neighbor has the boarding house's address."),
        ("DED_NEIGHBOR_ADDRESS_THOUGHT", "Eu sabia. Eu sabia que era ele.", "I knew it. I knew it was him."),
    };

    private static readonly string[] OldEvidenceKeys =
    {
        "EV_TEST_CLIPPING", "EV_TEST_CLIPPING_TEXT", "EV_TEST_CLIPPING_PLACE",
        "EV_TEST_NOTE", "EV_TEST_NOTE_TEXT", "EV_TEST_NOTE_PLACE",
        "EV_TEST_PRESCRIPTION", "EV_TEST_PRESCRIPTION_TEXT", "EV_TEST_PRESCRIPTION_PLACE",
        "DED_TEST_HALLEY", "DED_TEST_HALLEY_THOUGHT",
    };

    // ---------- Avisos de abertura (cena PreLoad) ----------

    private const string Lore1Pt =
        "Entre 1994 e 1996, Harrow Creek viveu com medo. O \"Açougueiro de Harrow Creek\" fez seis vítimas e deixou a mesma assinatura em todas: uma faca de cozinha da própria casa.\n\n" +
        "Ethan Myers, repórter policial do Harrow Creek Ledger, cobriu o caso do primeiro corpo ao último, ao lado da fotógrafa Clara Reyes.\n\n" +
        "Em 1996, as mortes pararam. Clara desapareceu na mesma semana. A polícia arquivou o caso e deu o principal suspeito como morto. Ninguém foi preso.";
    private const string Lore1En =
        "Between 1994 and 1996, Harrow Creek lived in fear. The \"Harrow Creek Butcher\" claimed six victims and left the same signature on every one: a kitchen knife from the victim's own home.\n\n" +
        "Ethan Myers, a crime reporter for the Harrow Creek Ledger, covered the case from the first body to the last, alongside photographer Clara Reyes.\n\n" +
        "In 1996, the killings stopped. Clara vanished that same week. Police closed the case and declared their main suspect dead. No one was ever arrested.";
    private const string Lore2Pt =
        "Hoje, em 2003, Ethan não escreve mais. O que ele viu naqueles anos ficou com ele: um Transtorno de Estresse Pós-Traumático que transforma qualquer barulho em ameaça e apaga horas inteiras da sua memória.\n\n" +
        "Nas crises, a visão se fecha, o ar pesa e 1996 volta a acontecer.\n\n" +
        "O remédio acalma as crises. Mas também deixa as lembranças longe demais.";
    private const string Lore2En =
        "Today, in 2003, Ethan no longer writes. What he saw in those years stayed with him: post-traumatic stress disorder that turns every sound into a threat and erases whole hours from his memory.\n\n" +
        "During an episode, his vision narrows, the air grows heavy, and 1996 happens all over again.\n\n" +
        "The medication calms the episodes. It also keeps the memories just out of reach.";

    [MenuItem("Echoes/Roteiro/Aplicar textos da demo")]
    private static void Apply()
    {
        foreach (string table in new[] { "items", "speaker", "subtitles", "track_1", "track_2" })
            LocalizationSetup.TrimKeys(table);

        LocalizationSetup.SetStrings("track_1", Track1);
        LocalizationSetup.SetStrings("track_2", Track2);
        LocalizationSetup.SetStrings("subtitles", SubtitleLines);
        LocalizationSetup.SetStrings("speaker", SpeakerLines);
        LocalizationSetup.SetStrings(InnerVoice.Table, InnerVoiceLines);
        LocalizationSetup.SetStrings("items", ItemLines);
        LocalizationSetup.SetStrings(EvidenceTable, EvidenceLines);
        LocalizationSetup.SetStrings("ItemsSubtitles", ItemSubtitleLines);
        LocalizationSetup.SetStrings("menu", MenuLines);

        ApplyTrack("Assets/Resources/Track/Track 1.asset", Track1, Track1Whisper);
        ApplyTrack("Assets/Resources/Track/Track 2.asset", Track2, Track2Whisper);
        ApplyWarningPanels();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path == "Assets/Scenes/Game.unity")
        {
            ApplyBoard();
            EditorSceneManager.MarkSceneDirty(scene);
        }
        else
        {
            Debug.LogWarning("[ScriptSetupTools] Abra a cena Game para aplicar o mural e o recorte; os textos já foram gravados.");
        }

        LocalizationSetup.RemoveKeys(EvidenceTable, OldEvidenceKeys);
        AssetDatabase.SaveAssets();
        Debug.Log("[ScriptSetupTools] Textos do roteiro aplicados. Salve a cena.");
    }

    // Tempos estimados pela fala em inglês (cerca de 15 caracteres por segundo). Quando as
    // gravações novas entrarem no FMOD, os tempos são refeitos a partir do áudio.
    private static void ApplyTrack(string path, (string key, string pt, string en)[] lines, int whisperIndex)
    {
        var data = AssetDatabase.LoadAssetAtPath<RadioSubtitleData>(path);
        if (data == null || data.subtitles.Length != lines.Length)
        {
            Debug.LogError($"[ScriptSetupTools] {path}: esperava {lines.Length} legendas.");
            return;
        }

        float time = 0f;
        for (int i = 0; i < lines.Length; i++)
        {
            float duration = Mathf.Max(1.6f, lines[i].en.Length / 15f);
            RadioSubtitle subtitle = data.subtitles[i];
            subtitle.subtitleText = new LocalizedString(path.Contains("Track 1") ? "track_1" : "track_2", lines[i].key);
            subtitle.speakerName = i == whisperIndex ? new LocalizedString() : new LocalizedString("speaker", "S_ANCHOR");
            subtitle.useItalic = i == whisperIndex;
            subtitle.startTime = time;
            subtitle.duration = duration;
            data.subtitles[i] = subtitle;
            time += duration + 0.4f;
        }

        data.trackDuration = Mathf.Ceil(time + data.startDelay);
        data.maxDuration = data.trackDuration;
        EditorUtility.SetDirty(data);
    }

    private static void ApplyWarningPanels()
    {
        SetPanel("Assets/Resources/WarningPanels/ptbr/Lore-pt-br.asset", Lore1Pt);
        SetPanel("Assets/Resources/WarningPanels/en/Lore-en.asset", Lore1En);
        SetPanel("Assets/Resources/WarningPanels/ptbr/Lore-pt-br-2.asset", Lore2Pt);
        SetPanel("Assets/Resources/WarningPanels/en/Lore-en-2.asset", Lore2En);
    }

    private static void SetPanel(string path, string text)
    {
        var panel = AssetDatabase.LoadAssetAtPath<WarningPanelData>(path);
        if (panel == null) return;
        panel.text = text;
        EditorUtility.SetDirty(panel);
    }

    private static void ApplyBoard()
    {
        Evidence clipping = GetEvidence("EV_TEST_CLIPPING", "EV_HALLEY_CLIPPING", "10.02.1995", new Vector2(0.26f, 0.3f));
        Evidence bulletin = GetEvidence(null, "EV_BULLETIN_NOTE", "21.11.2003", new Vector2(0.5f, 0.46f));
        Evidence neighbor = GetEvidence("EV_TEST_NOTE", "EV_NEIGHBOR_NOTE", "21.11.2003", new Vector2(0.74f, 0.3f));
        Evidence clinic = GetEvidence("EV_TEST_PRESCRIPTION", "EV_CLINIC_REPORT", "18.09.2003", new Vector2(0.5f, 0.76f));

        Deduction halleyReturn = GetDeduction(null, "DED_HALLEY_RETURN", clipping, bulletin);
        Deduction neighborAddress = GetDeduction("DED_TEST_HALLEY", "DED_NEIGHBOR_ADDRESS", neighbor, bulletin);

        EvidenceBoardUI board = Object.FindAnyObjectByType<EvidenceBoardUI>(FindObjectsInactive.Include);
        if (board != null)
        {
            var data = new SerializedObject(board);
            SetArray(data.FindProperty("evidenceCatalog"), clipping, bulletin, neighbor, clinic);
            SetArray(data.FindProperty("deductionCatalog"), halleyReturn, neighborAddress);
            data.ApplyModifiedProperties();
        }

        CreateClipping(clipping);
        CreateBulletinNote(bulletin);
    }

    private static Evidence GetEvidence(string oldName, string newName, string date, Vector2 position)
    {
        string path = $"{BoardFolder}/{newName}.asset";
        var evidence = AssetDatabase.LoadAssetAtPath<Evidence>(path);
        if (evidence == null && oldName != null && AssetDatabase.LoadAssetAtPath<Evidence>($"{BoardFolder}/{oldName}.asset") != null)
        {
            AssetDatabase.RenameAsset($"{BoardFolder}/{oldName}.asset", newName);
            evidence = AssetDatabase.LoadAssetAtPath<Evidence>(path);
        }
        if (evidence == null)
        {
            evidence = ScriptableObject.CreateInstance<Evidence>();
            AssetDatabase.CreateAsset(evidence, path);
        }

        evidence.title = new LocalizedString(EvidenceTable, newName);
        evidence.details = new LocalizedString(EvidenceTable, newName + "_TEXT");
        evidence.place = new LocalizedString(EvidenceTable, newName + "_PLACE");
        evidence.date = date;
        evidence.boardPosition = position;
        EditorUtility.SetDirty(evidence);
        return evidence;
    }

    private static Deduction GetDeduction(string oldName, string newName, Evidence first, Evidence second)
    {
        string path = $"{BoardFolder}/{newName}.asset";
        var deduction = AssetDatabase.LoadAssetAtPath<Deduction>(path);
        if (deduction == null && oldName != null && AssetDatabase.LoadAssetAtPath<Deduction>($"{BoardFolder}/{oldName}.asset") != null)
        {
            AssetDatabase.RenameAsset($"{BoardFolder}/{oldName}.asset", newName);
            deduction = AssetDatabase.LoadAssetAtPath<Deduction>(path);
        }
        if (deduction == null)
        {
            deduction = ScriptableObject.CreateInstance<Deduction>();
            AssetDatabase.CreateAsset(deduction, path);
        }

        deduction.first = first;
        deduction.second = second;
        deduction.conclusion = new LocalizedString(EvidenceTable, newName);
        deduction.reaction = new InnerVoiceLine { text = new LocalizedString(EvidenceTable, newName + "_THOUGHT") };
        EditorUtility.SetDirty(deduction);
        return deduction;
    }

    private static void SetArray(SerializedProperty array, params Object[] values)
    {
        array.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    // O recorte de 1995 fica em cima da caixa de arquivos do escritório (cópia de um papel que já existe)
    private static void CreateClipping(Evidence clipping)
    {
        ItemInteract[] items = Object.FindObjectsByType<ItemInteract>(FindObjectsInactive.Include);

        // A pista do recorte ficava provisoriamente no diário
        foreach (EvidenceSource oldSource in Object.FindObjectsByType<EvidenceSource>(FindObjectsInactive.Include))
        {
            if (oldSource.name == "DIARY" && new SerializedObject(oldSource).FindProperty("evidence").objectReferenceValue == clipping)
                Undo.DestroyObjectImmediate(oldSource);
        }

        if (items.Any(i => i.name == "Recorte_1995")) return;

        ItemInteract template = items.FirstOrDefault(i => i.name == "Carta_Amigo");
        GameObject box = GameObject.Find("Environment/R_REAL/Caixa de arquivos/arquivos");
        if (template == null || box == null)
        {
            Debug.LogError("[ScriptSetupTools] Carta_Amigo ou a caixa de arquivos não encontradas.");
            return;
        }

        Transform parent = items.First(i => i.name == "Carta_OLA").transform.parent;
        GameObject clippingObject = Object.Instantiate(template.gameObject, parent);
        clippingObject.name = "Recorte_1995";
        Undo.RegisterCreatedObjectUndo(clippingObject, "Criar recorte de 1995");

        Bounds top = box.GetComponent<Renderer>().bounds;
        clippingObject.transform.position = new Vector3(top.center.x, top.max.y + 0.01f, top.center.z);
        clippingObject.transform.rotation = template.transform.rotation * Quaternion.Euler(0f, 0f, -12f);

        var data = new SerializedObject(clippingObject.GetComponent<ItemInteract>());
        data.FindProperty("itemNameString").boxedValue = new LocalizedString("items", "ITEM_NAME_CLIPPING");
        data.FindProperty("itemDescriptionString").boxedValue = new LocalizedString("items", "ITEM_DESC_CLIPPING");
        data.FindProperty("triggerRadioEvents").boolValue = false;
        data.FindProperty("radioTriggerType").enumValueIndex = (int)ItemInteract.RadioTriggerType.None;
        data.ApplyModifiedPropertiesWithoutUndo();

        var source = Undo.AddComponent<EvidenceSource>(clippingObject);
        source.EditorSetup(clipping, new InnerVoiceLine { text = new LocalizedString(EvidenceTable, "EV_FOUND_THOUGHT") });
        EditorUtility.SetDirty(source);
    }

    private static void CreateBulletinNote(Evidence bulletin)
    {
        BulletinEvidence existing = Object.FindAnyObjectByType<BulletinEvidence>(FindObjectsInactive.Include);
        if (existing == null)
        {
            var go = new GameObject("Boletim");
            Undo.RegisterCreatedObjectUndo(go, "Criar anotação do boletim");
            SceneManager.MoveGameObjectToScene(go, SceneManager.GetActiveScene());
            GameObject systems = SceneManager.GetActiveScene().GetRootGameObjects().FirstOrDefault(r => r.name == "_Systems");
            if (systems != null) go.transform.SetParent(systems.transform, false);
            existing = Undo.AddComponent<BulletinEvidence>(go);
        }

        existing.EditorSetup(bulletin, new InnerVoiceLine { text = new LocalizedString(InnerVoice.Table, "IV_TAKE_NOTE") });
        EditorUtility.SetDirty(existing);
    }
}
