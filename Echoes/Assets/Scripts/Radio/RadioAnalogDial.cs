using UnityEngine;

/// <summary>Agulha do mostrador físico: a escala permanece impressa no visor.</summary>
public sealed class RadioAnalogDial : MonoBehaviour
{
    [SerializeField] private RectTransform needle;
    [SerializeField] private float left = -790f;
    [SerializeField] private float right = 790f;

    public void SetFrequency(float frequency, float minimum, float maximum)
    {
        if (needle == null) return;
        Vector2 position = needle.anchoredPosition;
        position.x = Mathf.Lerp(left, right, Mathf.InverseLerp(minimum, maximum, frequency));
        needle.anchoredPosition = position;
    }
}
