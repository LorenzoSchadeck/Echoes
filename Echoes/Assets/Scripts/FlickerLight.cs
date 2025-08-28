using UnityEngine;

public class FlickerLight : MonoBehaviour
{
    public Light lightSource;
    public float minIntensity = 0.5f;
    public float maxIntensity = 1.5f;
    public float flickerSpeed = 0.1f;

    private float randomizer;

    void Start()
    {
        if (lightSource == null)
        {
            lightSource = GetComponent<Light>();
        }
    }

    void Update()
    {
        if (randomizer > flickerSpeed)
        {
            lightSource.intensity = Random.Range(minIntensity, maxIntensity);
            randomizer = 0;
        }
        randomizer += Time.deltaTime;
    }
}