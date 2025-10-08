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
        
        [Tooltip("Duração que cada painel fica visível")]
        public float panelDuration = 3f;
        
        [Tooltip("Duração do fade in/out")]
        public float fadeDuration = 1f;
        
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
        
        private void Start()
        {
            StartCoroutine(RunWarningSequence());
        }
        
        private IEnumerator RunWarningSequence()
        {
            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(targetSceneName);
            loadOperation.allowSceneActivation = false;
            
            for (int i = 0; i < warningPanels.Count; i++)
            {
                WarningPanelData panel = warningPanels[i];
                if (panel != null)
                {
                    // Mostra spinner apenas no último painel
                    bool isLastPanel = (i == warningPanels.Count - 1);
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
            
            yield return StartCoroutine(FadePanel(1f));
            yield return new WaitForSeconds(panelDuration);
            yield return StartCoroutine(FadePanel(0f));
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
