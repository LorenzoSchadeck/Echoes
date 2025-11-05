using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

namespace Echoes.UI.WarningScreen
{
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
        
        [Header("UI References")]
        [Tooltip("CanvasGroup do painel")]
        public CanvasGroup panelCanvasGroup;
        
        [Tooltip("Referências dos textos de título (na mesma ordem dos painéis)")]
        public List<TextMeshProUGUI> titleTextReferences = new List<TextMeshProUGUI>();
        
        [Tooltip("Referências dos textos principais (na mesma ordem dos painéis)")]
        public List<TextMeshProUGUI> mainTextReferences = new List<TextMeshProUGUI>();
        
        [Tooltip("Referência ao spinner")]
        public LoadingSpinner spinner;
        
        [Header("Skip Indicator")]
        [Tooltip("Texto que pisca quando é possível pular")]
        public TextMeshProUGUI skipIndicatorText;
        
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
        
        private bool canSkip = false;
        private bool skipRequested = false;
        private List<WarningPanelData> filteredPanels = new List<WarningPanelData>();
        private Coroutine blinkCoroutine;
        private PanelLanguage currentLanguage;
        
        private void Start()
        {
            // Detecta o idioma do sistema e armazena
            currentLanguage = GetSystemLanguage();
            
            // Configura o texto do indicador baseado no idioma
            SetupSkipIndicatorText();
            
            // Esconde o indicador de skip no início
            if (skipIndicatorText != null)
                skipIndicatorText.enabled = false;
                
            FilterPanelsByLanguage();
            StartCoroutine(RunWarningSequence());
        }
        
        /// <summary>
        /// Configura o texto do indicador de skip baseado no idioma do sistema
        /// </summary>
        private void SetupSkipIndicatorText()
        {
            if (skipIndicatorText != null)
            {
                skipIndicatorText.text = currentLanguage == PanelLanguage.Portuguese 
                    ? skipTextPortuguese 
                    : skipTextEnglish;
            }
        }
        
        /// <summary>
        /// Filtra os painéis baseado no idioma do sistema
        /// </summary>
        private void FilterPanelsByLanguage()
        {
            filteredPanels.Clear();
            
            // Usa o idioma já detectado
            PanelLanguage systemLanguage = currentLanguage;
            
            // Filtra painéis que correspondem ao idioma do sistema
            foreach (var panel in warningPanels)
            {
                if (panel != null && panel.language == systemLanguage)
                {
                    filteredPanels.Add(panel);
                }
            }
            
            // Fallback: se não encontrou painéis no idioma do sistema, usa todos
            if (filteredPanels.Count == 0)
            {
                Debug.LogWarning($"[WarningScreenManager] Nenhum painel encontrado para o idioma {systemLanguage}. Usando todos os painéis.");
                filteredPanels.AddRange(warningPanels);
            }
            else
            {
                Debug.Log($"[WarningScreenManager] {filteredPanels.Count} painéis carregados para o idioma: {systemLanguage}");
            }
        }
        
        /// <summary>
        /// Detecta o idioma do sistema operacional
        /// </summary>
        private PanelLanguage GetSystemLanguage()
        {
            SystemLanguage systemLang = Application.systemLanguage;
            
            // Detecta se é português
            if (systemLang == SystemLanguage.Portuguese)
            {
                return PanelLanguage.Portuguese;
            }
            
            // Padrão: inglês
            return PanelLanguage.English;
        }
        
        private void Update()
        {
            // Detecta qualquer clique de mouse ou tecla de espaço/enter para pular
            if (canSkip && !skipRequested)
            {
                if (Input.anyKeyDown || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
                {
                    skipRequested = true;
                }
            }
        }
        
        private IEnumerator RunWarningSequence()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            loadOperation.allowSceneActivation = false;
            
            // Usa os painéis filtrados ao invés dos painéis originais
            for (int i = 0; i < filteredPanels.Count; i++)
            {
                WarningPanelData panel = filteredPanels[i];
                if (panel != null)
                {
                    // Mostra spinner apenas no último painel
                    bool isLastPanel = (i == filteredPanels.Count - 1);
                    if (isLastPanel && spinner != null)
                        spinner.ShowSpinner();
                    
                    yield return StartCoroutine(ShowPanel(panel, i));
                }
            }
            
            while (loadOperation.progress < 0.9f)
            {
                yield return null;
            }
            
            if (spinner != null)
                spinner.HideSpinner();
                
            yield return new WaitForSeconds(1f);
            loadOperation.allowSceneActivation = true;
        }
        
        private IEnumerator ShowPanel(WarningPanelData panelData, int panelIndex)
        {
            // Limpa todos os textos primeiro
            ClearAllTexts();
            
            // Atualiza texto do título se há referência correspondente
            if (panelIndex < titleTextReferences.Count && titleTextReferences[panelIndex] != null)
                titleTextReferences[panelIndex].text = panelData.title;
                
            // Atualiza texto principal se há referência correspondente
            if (panelIndex < mainTextReferences.Count && mainTextReferences[panelIndex] != null)
                mainTextReferences[panelIndex].text = panelData.text;
            
            // Fade in
            yield return StartCoroutine(FadePanel(1f));
            
            // Aguarda exibição do painel com ou sem possibilidade de skip
            float panelDisplayDuration = panelData.displayDuration > 0 ? panelData.displayDuration : defaultPanelDuration;
            yield return StartCoroutine(WaitForPanelDisplay(panelDisplayDuration, panelData.isSkippable));
            
            // Fade out
            yield return StartCoroutine(FadePanel(0f));
        }
        
        /// <summary>
        /// Aguarda a duração do painel, permitindo skip após tempo mínimo se o painel for pulável
        /// </summary>
        private IEnumerator WaitForPanelDisplay(float duration, bool allowSkip)
        {
            canSkip = false;
            skipRequested = false;
            float elapsed = 0f;
            
            // Se o painel não é pulável, apenas aguarda a duração total
            if (!allowSkip)
            {
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }
                yield break;
            }
            
            // Aguarda tempo mínimo obrigatório
            while (elapsed < minimumTimeBeforeSkip)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Habilita skip e inicia o piscar do indicador
            canSkip = true;
            StartSkipIndicatorBlink();
            
            // Aguarda até o fim da duração OU até o skip ser solicitado
            while (elapsed < duration && !skipRequested)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Desabilita skip e para o piscar do indicador
            canSkip = false;
            StopSkipIndicatorBlink();
        }
        
        /// <summary>
        /// Inicia o efeito de piscar do indicador de skip
        /// </summary>
        private void StartSkipIndicatorBlink()
        {
            if (skipIndicatorText != null)
            {
                StopSkipIndicatorBlink(); // Para qualquer blink anterior
                blinkCoroutine = StartCoroutine(BlinkIndicator());
            }
        }
        
        /// <summary>
        /// Para o efeito de piscar e esconde o indicador
        /// </summary>
        private void StopSkipIndicatorBlink()
        {
            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                blinkCoroutine = null;
            }
            
            if (skipIndicatorText != null)
            {
                skipIndicatorText.enabled = false;
                // Restaura alpha para o máximo quando desabilitar
                SetTextAlpha(maxAlpha);
            }
        }
        
        /// <summary>
        /// Coroutine que faz o texto piscar com fade suave
        /// </summary>
        private IEnumerator BlinkIndicator()
        {
            if (skipIndicatorText == null) yield break;
            
            // Garante que o texto está visível
            skipIndicatorText.enabled = true;
            
            float halfCycleDuration = fadeCycleDuration / 2f;
            
            while (true)
            {
                // Fade out (de max para min)
                float elapsed = 0f;
                while (elapsed < halfCycleDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfCycleDuration;
                    float alpha = Mathf.Lerp(maxAlpha, minAlpha, t);
                    SetTextAlpha(alpha);
                    yield return null;
                }
                SetTextAlpha(minAlpha);
                
                // Fade in (de min para max)
                elapsed = 0f;
                while (elapsed < halfCycleDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / halfCycleDuration;
                    float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
                    SetTextAlpha(alpha);
                    yield return null;
                }
                SetTextAlpha(maxAlpha);
            }
        }
        
        /// <summary>
        /// Define o alpha do texto do indicador
        /// </summary>
        private void SetTextAlpha(float alpha)
        {
            if (skipIndicatorText != null)
            {
                Color color = skipIndicatorText.color;
                color.a = alpha;
                skipIndicatorText.color = color;
            }
        }
        
        /// <summary>
        /// Limpa todos os textos das referências
        /// </summary>
        private void ClearAllTexts()
        {
            // Limpa todos os textos de título
            foreach (var titleText in titleTextReferences)
            {
                if (titleText != null)
                    titleText.text = "";
            }
            
            // Limpa todos os textos principais
            foreach (var mainText in mainTextReferences)
            {
                if (mainText != null)
                    mainText.text = "";
            }
        }
        
        private IEnumerator FadePanel(float targetAlpha)
        {
            if (panelCanvasGroup == null) yield break;
            
            float startAlpha = panelCanvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                panelCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }
            
            panelCanvasGroup.alpha = targetAlpha;
        }
        
        [ContextMenu("Show Spinner")]
        public void ShowSpinner()
        {
            if (spinner != null)
                spinner.ShowSpinner();
        }
        
        [ContextMenu("Hide Spinner")]
        public void HideSpinner()
        {
            if (spinner != null)
                spinner.HideSpinner();
        }
    }
}
