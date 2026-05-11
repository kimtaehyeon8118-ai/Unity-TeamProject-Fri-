using UnityEngine;

public class SpriteAccentAnimator : MonoBehaviour
{
    [SerializeField] private SpriteRenderer targetRenderer;
    [SerializeField] private bool bobEnabled = true;
    [SerializeField] private float bobAmplitude = 0.08f;
    [SerializeField] private float bobSpeed = 2.2f;
    [SerializeField] private bool pulseScaleEnabled = true;
    [SerializeField] private float pulseScaleAmplitude = 0.06f;
    [SerializeField] private float pulseSpeed = 2.8f;
    [SerializeField] private bool colorPulseEnabled = true;
    [SerializeField] private float brightnessBoost = 0.18f;
    [SerializeField] private bool rotateEnabled;
    [SerializeField] private float rotationSpeed = 18f;
    [SerializeField] private float rotationAmplitude = 3f;

    private Vector3 baseLocalPosition;
    private Vector3 baseLocalScale;
    private Quaternion baseLocalRotation;
    private Color baseColor;
    private float phaseOffset;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        CacheBaseState();
        phaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void OnEnable()
    {
        CacheBaseState();
    }

    private void Update()
    {
        float time = Time.time + phaseOffset;
        float bobWave = Mathf.Sin(time * bobSpeed);
        float pulseWave = Mathf.Sin(time * pulseSpeed);

        transform.localPosition = baseLocalPosition + (bobEnabled ? Vector3.up * bobWave * bobAmplitude : Vector3.zero);
        transform.localScale = baseLocalScale * (1f + (pulseScaleEnabled ? pulseWave * pulseScaleAmplitude : 0f));

        if (rotateEnabled)
        {
            float angle = Mathf.Sin(time * rotationSpeed * Mathf.Deg2Rad) * rotationAmplitude;
            transform.localRotation = baseLocalRotation * Quaternion.Euler(0f, 0f, angle);
        }
        else
        {
            transform.localRotation = baseLocalRotation;
        }

        if (targetRenderer != null)
        {
            if (colorPulseEnabled)
            {
                float glowT = (pulseWave + 1f) * 0.5f;
                float boost = Mathf.Lerp(0f, brightnessBoost, glowT);
                targetRenderer.color = new Color(
                    Mathf.Clamp01(baseColor.r + boost),
                    Mathf.Clamp01(baseColor.g + boost),
                    Mathf.Clamp01(baseColor.b + boost),
                    baseColor.a);
            }
            else
            {
                targetRenderer.color = baseColor;
            }
        }
    }

    public void SnapToCurrentVisual()
    {
        CacheBaseState();
    }

    private void CacheBaseState()
    {
        baseLocalPosition = transform.localPosition;
        baseLocalScale = transform.localScale;
        baseLocalRotation = transform.localRotation;

        if (targetRenderer == null)
        {
            targetRenderer = GetComponent<SpriteRenderer>();
        }

        if (targetRenderer != null)
        {
            baseColor = targetRenderer.color;
        }
    }
}
