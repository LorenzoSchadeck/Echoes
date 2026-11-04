using TMPro;
using UnityEngine;

/// <summary>
/// Mostra a hora do <see cref="GameClock"/> num texto (TextMeshPro 3D ou de UI), como "23:40".
/// Coloque num texto sobre o despertador ou qualquer relógio da cena.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
public class ClockDisplay : MonoBehaviour
{
    [Tooltip("Pisca os dois pontos a cada segundo, como um despertador digital.")]
    [SerializeField] private bool blinkColon = true;

    private TMP_Text text;
    private int shownMinute = -1;
    private bool colonVisible = true;

    private void Awake() => text = GetComponent<TMP_Text>();

    private void Update()
    {
        bool colon = !blinkColon || Mathf.FloorToInt(Time.unscaledTime) % 2 == 0;
        int minute = GameClock.Hour * 60 + GameClock.Minute;
        if (minute == shownMinute && colon == colonVisible) return;

        shownMinute = minute;
        colonVisible = colon;
        text.text = $"{GameClock.Hour:00}{(colon ? ":" : " ")}{GameClock.Minute:00}";
    }
}
