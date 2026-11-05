using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Localization;
using UnityEngine.SceneManagement;

/// <summary>
/// Fim da demo: depois da primeira hora perdida, quando Ethan examina o casaco molhado (ou depois
/// de um tempo), a tela escurece, entra o cartão final e o jogo volta ao menu.
/// Só age com o modo demo ligado (DemoSettings).
/// </summary>
public class DemoEnding : MonoBehaviour
{
    [Tooltip("Objeto que, examinado depois da hora perdida, encerra a demo (o casaco molhado).")]
    [SerializeField] private SimpleItemDisplay endTrigger;
    [Tooltip("Segundos depois de examinar o objeto, para a fala terminar.")]
    [SerializeField] private float delayAfterTrigger = 4f;
    [Tooltip("Se o jogador não examinar o objeto, a demo termina depois destes segundos.")]
    [SerializeField] private float fallbackSeconds = 75f;
    [SerializeField] private LocalizedString endText = new LocalizedString("menu", "M_DEMO_END");
    [SerializeField] private float cardSeconds = 10f;
    [SerializeField] private string menuScene = "Menu";

    private bool waitingForEnd;
    private bool ending;
    private float lostTimeAt;

    private void OnEnable()
    {
        GameEvents.OnLostTime += HandleLostTime;
        SimpleItemDisplay.OnDisplayed += HandleDisplayed;
    }

    private void OnDisable()
    {
        GameEvents.OnLostTime -= HandleLostTime;
        SimpleItemDisplay.OnDisplayed -= HandleDisplayed;
    }

    private void HandleLostTime(float minutes)
    {
        if (!DemoSettings.IsDemo) return;
        waitingForEnd = true;
        lostTimeAt = Time.time;
    }

    private void HandleDisplayed(SimpleItemDisplay item)
    {
        if (waitingForEnd && item == endTrigger) StartCoroutine(EndAfter(delayAfterTrigger));
    }

    private void Update()
    {
        if (waitingForEnd && !ending && Time.time - lostTimeAt >= fallbackSeconds) StartCoroutine(EndAfter(0f));
    }

    private IEnumerator EndAfter(float delay)
    {
        if (ending) yield break;
        ending = true;
        waitingForEnd = false;

        yield return new WaitForSeconds(delay);
        PlayerControlLock.Acquire(this);
        GameHud.Prompt.enabled = false;
        GameHud.Crosshair.enabled = false;
        yield return Fade.Alpha(GameHud.FadeScreen, 1f, 2.5f, smooth: true);
        GameHud.Subtitle.enabled = false;
        GameHud.Speaker.enabled = false;

        var text = endText.GetLocalizedStringAsync();
        yield return text;
        GameHud.EndText.text = text.Result;
        yield return Fade.Alpha(GameHud.EndText, 1f, 2f);

        // Fica na tela pelo tempo do cartão; depois de 3 s, qualquer tecla volta ao menu
        for (float t = 0f; t < cardSeconds; t += Time.deltaTime)
        {
            if (t > 3f && AnyKeyPressed()) break;
            yield return null;
        }

        yield return Fade.Alpha(GameHud.EndText, 0f, 1f);
        PlayerControlLock.Release(this);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneManager.LoadScene(menuScene);
    }

    private static bool AnyKeyPressed()
    {
        return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
    }

#if UNITY_EDITOR
    public void EditorSetup(SimpleItemDisplay trigger) => endTrigger = trigger;
#endif
}
