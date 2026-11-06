using System.Collections;
using UnityEngine;

/// <summary>
/// Lâmpada velha que falha: a luz oscila de leve e, de tempos em tempos, apaga por um instante.
/// O vidro da lâmpada acende e apaga junto (material emissivo).
/// </summary>
public class FlickeringLight : MonoBehaviour
{
    [SerializeField] private Light lightSource;
    [Tooltip("Vidro da lâmpada. Troca entre o material aceso e o apagado.")]
    [SerializeField] private Renderer bulb;
    [SerializeField] private Material bulbOn;
    [SerializeField] private Material bulbOff;

    [Header("Falhas")]
    [SerializeField] private bool startOn = true;
    [Tooltip("Oscilação contínua da intensidade (0 = estável).")]
    [SerializeField, Range(0f, 0.5f)] private float wobble = 0.08f;
    [Tooltip("Segundos entre uma falha e outra (mínimo e máximo).")]
    [SerializeField] private Vector2 failureInterval = new Vector2(2.5f, 7f);
    [Tooltip("Quantas piscadas cada falha tem (mínimo e máximo).")]
    [SerializeField] private Vector2Int blinksPerFailure = new Vector2Int(1, 4);

    private float baseIntensity;
    private bool isOn;
    private bool dark;
    private float seed;

    private void Awake()
    {
        if (lightSource == null) lightSource = GetComponentInChildren<Light>();
        baseIntensity = lightSource != null ? lightSource.intensity : 1f;
        seed = Random.value * 100f;
    }

    private void OnEnable()
    {
        SetOn(startOn);
        StartCoroutine(FailureLoop());
    }

    /// <summary>Liga ou desliga a lâmpada (as falhas só acontecem com ela ligada).</summary>
    public void SetOn(bool on)
    {
        isOn = on;
        Apply();
    }

    private void Update()
    {
        if (!isOn || dark || lightSource == null) return;
        float noise = Mathf.PerlinNoise(seed, Time.time * 6f) * 2f - 1f;
        lightSource.intensity = baseIntensity * (1f + noise * wobble);
    }

    private IEnumerator FailureLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(failureInterval.x, failureInterval.y));
            if (!isOn) continue;

            int blinks = Random.Range(blinksPerFailure.x, blinksPerFailure.y + 1);
            for (int i = 0; i < blinks; i++)
            {
                dark = true;
                Apply();
                yield return new WaitForSeconds(Random.Range(0.04f, 0.18f));
                dark = false;
                Apply();
                yield return new WaitForSeconds(Random.Range(0.05f, 0.25f));
            }
        }
    }

    private void Apply()
    {
        bool lit = isOn && !dark;
        if (lightSource != null)
        {
            lightSource.enabled = lit;
            lightSource.intensity = baseIntensity;
        }
        if (bulb != null && bulbOn != null && bulbOff != null) bulb.sharedMaterial = lit ? bulbOn : bulbOff;
    }

#if UNITY_EDITOR
    public void EditorSetup(Light source, Renderer glass, Material on, Material off, bool lightOnStart, float wobbleAmount, Vector2 interval)
    {
        lightSource = source;
        bulb = glass;
        bulbOn = on;
        bulbOff = off;
        startOn = lightOnStart;
        wobble = wobbleAmount;
        failureInterval = interval;
    }
#endif
}
