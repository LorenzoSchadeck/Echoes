using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;

namespace Echoes.UI.WarningScreen
{
    /// <summary>
    /// Tela de avisos e contexto da cena PreLoad, em UI Toolkit (Warning.uxml). Mostra os painéis
    /// do idioma escolhido enquanto carrega a próxima cena, com spinner no último painel.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class WarningScreenManager : MonoBehaviour
    {
        [Header("Panel Settings")]
        [Tooltip("Lista de painéis para exibir")]
        public List<WarningPanelData> warningPanels = new List<WarningPanelData>();

        [Tooltip("Duração padrão se o painel não tiver duração definida")]
        public float defaultPanelDuration = 3f;

        [Tooltip("Duração do fade in/out")]
        public float fadeDuration = 1f;

        [Header("Input Settings")]
        [Tooltip("Tempo mínimo antes de permitir clique para pular")]
        [Min(0f)]
        public float minimumTimeBeforeSkip = 5f;

        [Header("Scene Loading")]
        [Tooltip("Nome da cena para carregar")]
        public string targetSceneName = "Game";

        [Header("Spinner")]
        [Tooltip("Imagem do spinner de carregamento")]
        public Sprite spinnerSprite;
        [Tooltip("Velocidade de rotação em graus por segundo")]
        public float spinnerRotationSpeed = 180f;
        [Tooltip("Duração do fade in/out do spinner")]
        public float spinnerFadeDuration = 0.5f;

        [Header("Skip Indicator")]
        [Tooltip("Duração do ciclo completo de fade (in + out)")]
        [Range(0.5f, 3f)]
        public float fadeCycleDuration = 1.5f;

        [Tooltip("Alpha mínimo do fade")]
        [Range(0f, 0.8f)]
        public float minAlpha = 0.2f;

        [Tooltip("Alpha máximo do fade")]
        [Range(0.2f, 1f)]
        public float maxAlpha = 1f;

        [Header("Localized Texts")]
        [Tooltip("Texto do indicador em inglês")]
        public string skipTextEnglish = "Press any key to continue";

        [Tooltip("Texto do indicador em português")]
        public string skipTextPortuguese = "Pressione qualquer tecla para continuar";

        private VisualElement panel;
        private VisualElement spinner;
        private Label titleLabel;
        private Label bodyLabel;
        private Label skipLabel;

        private bool canSkip;
        private bool skipRequested;
        private bool spinning;
        private float spinnerAngle;
        private readonly List<WarningPanelData> filteredPanels = new List<WarningPanelData>();
        private Coroutine blinkCoroutine;
        private Coroutine spinnerFade;
        private PanelLanguage currentLanguage;

        private void Start()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
            panel = root.Q("panel");
            spinner = root.Q("spinner");
            titleLabel = root.Q<Label>("title");
            bodyLabel = root.Q<Label>("body");
            skipLabel = root.Q<Label>("skip");

            if (spinnerSprite != null)
                spinner.style.backgroundImage = new StyleBackground(spinnerSprite);

            currentLanguage = GetLanguage();
            skipLabel.text = currentLanguage == PanelLanguage.Portuguese ? skipTextPortuguese : skipTextEnglish;

            FilterPanelsByLanguage();
            StartCoroutine(RunWarningSequence());
        }

        private void Update()
        {
            if (canSkip && !skipRequested && AnySkipInputPressed())
                skipRequested = true;

            if (spinning)
            {
                spinnerAngle -= spinnerRotationSpeed * Time.deltaTime;
                spinner.style.rotate = new Rotate(new Angle(spinnerAngle, AngleUnit.Degree));
            }
        }

        /// <summary>
        /// Idioma escolhido pelo jogador nas opções; sem escolha, o do sistema operacional
        /// </summary>
        private static PanelLanguage GetLanguage()
        {
            string chosen = GameOptions.Current.language;
            if (!string.IsNullOrEmpty(chosen))
                return chosen.StartsWith("pt") ? PanelLanguage.Portuguese : PanelLanguage.English;

            return Application.systemLanguage == SystemLanguage.Portuguese ? PanelLanguage.Portuguese : PanelLanguage.English;
        }

        private void FilterPanelsByLanguage()
        {
            filteredPanels.Clear();
            foreach (var data in warningPanels)
            {
                if (data != null && data.language == currentLanguage)
                    filteredPanels.Add(data);
            }

            // Sem painéis no idioma: mostra todos
            if (filteredPanels.Count == 0)
            {
                Debug.LogWarning($"[WarningScreenManager] Nenhum painel encontrado para o idioma {currentLanguage}. Usando todos os painéis.");
                filteredPanels.AddRange(warningPanels);
            }
        }

        private static bool AnySkipInputPressed()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;
            var gamepad = Gamepad.current;

            return (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
                || (mouse != null && (mouse.leftButton.wasPressedThisFrame || mouse.rightButton.wasPressedThisFrame || mouse.middleButton.wasPressedThisFrame))
                || (gamepad != null && (gamepad.buttonSouth.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame));
        }

        private IEnumerator RunWarningSequence()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            loadOperation.allowSceneActivation = false;

            for (int i = 0; i < filteredPanels.Count; i++)
            {
                // Spinner só no último painel
                if (i == filteredPanels.Count - 1)
                    SetSpinner(true);

                yield return StartCoroutine(ShowPanel(filteredPanels[i]));
            }

            while (loadOperation.progress < 0.9f)
                yield return null;

            SetSpinner(false);

            yield return new WaitForSeconds(1f);
            loadOperation.allowSceneActivation = true;
        }

        private IEnumerator ShowPanel(WarningPanelData data)
        {
            bool hasTitle = !string.IsNullOrEmpty(data.title);
            titleLabel.text = data.title;
            titleLabel.style.display = hasTitle ? DisplayStyle.Flex : DisplayStyle.None;
            bodyLabel.text = data.text;
            bodyLabel.EnableInClassList("warning-body--no-title", !hasTitle);

            yield return Fade.Alpha(panel, 1f, fadeDuration);

            float duration = data.displayDuration > 0 ? data.displayDuration : defaultPanelDuration;
            yield return StartCoroutine(WaitForPanelDisplay(duration, data.isSkippable));

            yield return Fade.Alpha(panel, 0f, fadeDuration);
        }

        /// <summary>
        /// Aguarda a duração do painel, permitindo pular depois do tempo mínimo se o painel for pulável
        /// </summary>
        private IEnumerator WaitForPanelDisplay(float duration, bool allowSkip)
        {
            canSkip = false;
            skipRequested = false;
            float elapsed = 0f;

            if (!allowSkip)
            {
                yield return new WaitForSeconds(duration);
                yield break;
            }

            while (elapsed < minimumTimeBeforeSkip)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            canSkip = true;
            blinkCoroutine = StartCoroutine(BlinkIndicator());

            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            canSkip = false;
            if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
            blinkCoroutine = null;
            skipLabel.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Faz o indicador de pular piscar com fade suave
        /// </summary>
        private IEnumerator BlinkIndicator()
        {
            skipLabel.style.display = DisplayStyle.Flex;
            skipLabel.style.opacity = maxAlpha;
            float half = fadeCycleDuration / 2f;

            while (true)
            {
                yield return Fade.Alpha(skipLabel, minAlpha, half);
                yield return Fade.Alpha(skipLabel, maxAlpha, half);
            }
        }

        private void SetSpinner(bool show)
        {
            if (show) spinning = true;
            if (spinnerFade != null) StopCoroutine(spinnerFade);
            spinnerFade = StartCoroutine(FadeSpinner(show));
        }

        private IEnumerator FadeSpinner(bool show)
        {
            yield return Fade.Alpha(spinner, show ? 1f : 0f, spinnerFadeDuration);
            if (!show) spinning = false;
        }
    }
}
