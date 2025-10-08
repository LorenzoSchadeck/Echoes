using UnityEngine;
using System.Collections;

namespace Echoes.UI.WarningScreen
{
    public class LoadingSpinner : MonoBehaviour
    {
        [Header("Spinner Settings")]
        [Tooltip("Velocidade de rotação em graus por segundo")]
        public float rotationSpeed = 180f;
        
        [Tooltip("Duração do fade in/out")]
        public float fadeDuration = 0.5f;
        
        [Header("References")]
        [Tooltip("CanvasGroup para controle de fade")]
        public CanvasGroup canvasGroup;
        
        private bool isSpinning = false;
        private Coroutine spinCoroutine;
        private Coroutine fadeCoroutine;
        
        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
                
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
        
        public void ShowSpinner()
        {
            if (isSpinning) return;
            
            isSpinning = true;
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
                
            spinCoroutine = StartCoroutine(SpinCoroutine());
            fadeCoroutine = StartCoroutine(FadeSpinner(1f));
        }
        
        public void HideSpinner()
        {
            if (!isSpinning) return;
            
            isSpinning = false;
            
            if (spinCoroutine != null)
            {
                StopCoroutine(spinCoroutine);
                spinCoroutine = null;
            }
            
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
                
            fadeCoroutine = StartCoroutine(FadeSpinner(0f));
        }
        
        private IEnumerator SpinCoroutine()
        {
            while (isSpinning)
            {
                transform.Rotate(0f, 0f, -rotationSpeed * Time.deltaTime);
                yield return null;
            }
        }
        
        private IEnumerator FadeSpinner(float targetAlpha)
        {
            if (canvasGroup == null) yield break;
            
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
                yield return null;
            }
            
            canvasGroup.alpha = targetAlpha;
        }
        
        private void OnDestroy()
        {
            if (spinCoroutine != null)
                StopCoroutine(spinCoroutine);
            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
        }
    }
}
