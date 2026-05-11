using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageFeedbackController : MonoBehaviour
{
    [SerializeField] private Image overlayImage;
    [SerializeField] private AudioSource oneShotSource;
    [SerializeField] private Color damageFlashColor = new Color(1f, 0.24f, 0.18f, 0.2f);
    [SerializeField] private Color checkpointFlashColor = new Color(0.22f, 0.86f, 1f, 0.14f);
    [SerializeField] private Color healFlashColor = new Color(0.32f, 1f, 0.58f, 0.12f);
    [SerializeField] private Color clearFlashColor = new Color(0.9f, 1f, 0.96f, 0.75f);
    [SerializeField] private float overlayFadeSpeed = 4.5f;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    private CameraFollow cameraFollow;
    private PlayerController player;
    private GameManager gameManager;
    private Color currentOverlayColor;
    private Color targetOverlayColor;
    private bool boundToGameManager;
    private bool boundToPlayer;

    private void Awake()
    {
        EnsureMoodOverlays();
        EnsureOverlay();
        EnsureAudioSource();
        BuildClips();
    }

    private void OnEnable()
    {
        TryBind();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        TryBind();

        if (overlayImage != null)
        {
            currentOverlayColor = Color.Lerp(currentOverlayColor, targetOverlayColor, Time.unscaledDeltaTime * overlayFadeSpeed);
            overlayImage.color = currentOverlayColor;
            targetOverlayColor = Color.Lerp(targetOverlayColor, Color.clear, Time.unscaledDeltaTime * overlayFadeSpeed * 0.65f);
        }
    }

    private void HandlePlayerDamaged(int remainingHits, int maxHits)
    {
        Flash(damageFlashColor);
        Play("damage");
        cameraFollow?.AddTrauma(0.35f);
        cameraFollow?.AddZoomImpulse(-0.28f);
    }

    private void HandlePlayerHealed(int remainingHits, int maxHits)
    {
        Flash(healFlashColor);
        Play("heal");
        cameraFollow?.AddZoomImpulse(-0.1f);
    }

    private void HandleCheckpointChanged(Transform checkpoint)
    {
        Flash(checkpointFlashColor);
        Play("checkpoint");
        cameraFollow?.AddZoomImpulse(-0.12f);
    }

    private void HandleStageCleared()
    {
        Flash(clearFlashColor);
        Play("clear");
        cameraFollow?.AddTrauma(0.18f);
        cameraFollow?.AddZoomImpulse(-0.45f);
    }

    private void HandleStageFailed(string reason)
    {
        Flash(damageFlashColor * 1.05f);
        Play("fail");
        cameraFollow?.AddTrauma(0.2f);
    }

    private void HandleDashed()
    {
        Play("dash");
        cameraFollow?.AddTrauma(0.12f);
        cameraFollow?.AddZoomImpulse(-0.08f);
    }

    private void HandleGrappleAttached()
    {
        Play("grapple_attach");
        cameraFollow?.AddTrauma(0.08f);
        cameraFollow?.AddZoomImpulse(-0.05f);
    }

    private void HandleGrappleReleased(bool boosted)
    {
        Play(boosted ? "grapple_release_boost" : "grapple_release");
        cameraFollow?.AddZoomImpulse(boosted ? -0.12f : -0.04f);
    }

    private void EnsureOverlay()
    {
        if (overlayImage != null)
        {
            overlayImage.raycastTarget = false;
            currentOverlayColor = overlayImage.color;
            targetOverlayColor = overlayImage.color;
            return;
        }

        Transform existing = transform.Find("FeedbackOverlay");
        if (existing != null)
        {
            overlayImage = existing.GetComponent<Image>();
        }

        if (overlayImage == null)
        {
            GameObject overlayObject = new GameObject("FeedbackOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(transform, false);
            overlayObject.transform.SetAsLastSibling();

            RectTransform rectTransform = overlayObject.GetComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            overlayImage = overlayObject.GetComponent<Image>();
        }

        overlayImage.color = Color.clear;
        overlayImage.raycastTarget = false;
        currentOverlayColor = Color.clear;
        targetOverlayColor = Color.clear;
    }

    private void EnsureMoodOverlays()
    {
        RemoveOverlayPrefix("Scanline_");
        EnsureOverlayBand("CinematicBarTop", new Vector2(0f, 0.94f), Vector2.one, new Color32(0, 0, 0, 120));
        EnsureOverlayBand("CinematicBarBottom", Vector2.zero, new Vector2(1f, 0.055f), new Color32(0, 0, 0, 120));
        EnsureOverlayBand("VignetteLeft", Vector2.zero, new Vector2(0.075f, 1f), new Color32(0, 0, 0, 78));
        EnsureOverlayBand("VignetteRight", new Vector2(0.925f, 0f), Vector2.one, new Color32(0, 0, 0, 78));
    }

    private void RemoveOverlayPrefix(string prefix)
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child != null && child.name.StartsWith(prefix))
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void EnsureOverlayBand(string objectName, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        Transform existing = transform.Find(objectName);
        GameObject bandObject;
        if (existing == null)
        {
            bandObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bandObject.transform.SetParent(transform, false);
        }
        else
        {
            bandObject = existing.gameObject;
        }

        RectTransform rectTransform = bandObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = bandObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        bandObject.transform.SetAsFirstSibling();
    }

    private void EnsureAudioSource()
    {
        if (oneShotSource == null)
        {
            oneShotSource = GetComponent<AudioSource>();
        }

        if (oneShotSource == null)
        {
            oneShotSource = gameObject.AddComponent<AudioSource>();
        }

        oneShotSource.playOnAwake = false;
        oneShotSource.loop = false;
        oneShotSource.spatialBlend = 0f;
        oneShotSource.volume = 0.26f;
        oneShotSource.ignoreListenerPause = true;
    }

    private void BuildClips()
    {
        clips["damage"] = CreateTone("damage", 180f, 0.11f, 0.24f, 0.85f);
        clips["heal"] = CreateTone("heal", 620f, 0.12f, 0.18f, 0.18f);
        clips["checkpoint"] = CreateTone("checkpoint", 740f, 0.09f, 0.18f, 0.08f);
        clips["clear"] = CreateTone("clear", 960f, 0.24f, 0.2f, 0.12f);
        clips["fail"] = CreateTone("fail", 140f, 0.18f, 0.2f, 0.8f);
        clips["dash"] = CreateTone("dash", 420f, 0.07f, 0.14f, 0.55f);
        clips["grapple_attach"] = CreateTone("grapple_attach", 860f, 0.05f, 0.16f, 0.18f);
        clips["grapple_release"] = CreateTone("grapple_release", 520f, 0.06f, 0.14f, 0.2f);
        clips["grapple_release_boost"] = CreateTone("grapple_release_boost", 660f, 0.09f, 0.17f, 0.12f);
    }

    private void TryBind()
    {
        if (cameraFollow == null && Camera.main != null)
        {
            cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        if (!boundToGameManager && GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
            gameManager.PlayerDamaged += HandlePlayerDamaged;
            gameManager.PlayerHealed += HandlePlayerHealed;
            gameManager.CheckpointChanged += HandleCheckpointChanged;
            gameManager.StageCleared += HandleStageCleared;
            gameManager.StageFailed += HandleStageFailed;
            boundToGameManager = true;
        }

        if (!boundToPlayer)
        {
            player = FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.Dashed += HandleDashed;
                player.GrappleAttached += HandleGrappleAttached;
                player.GrappleReleased += HandleGrappleReleased;
                boundToPlayer = true;
            }
        }
    }

    private void Unbind()
    {
        if (boundToGameManager && gameManager != null)
        {
            gameManager.PlayerDamaged -= HandlePlayerDamaged;
            gameManager.PlayerHealed -= HandlePlayerHealed;
            gameManager.CheckpointChanged -= HandleCheckpointChanged;
            gameManager.StageCleared -= HandleStageCleared;
            gameManager.StageFailed -= HandleStageFailed;
        }

        if (boundToPlayer && player != null)
        {
            player.Dashed -= HandleDashed;
            player.GrappleAttached -= HandleGrappleAttached;
            player.GrappleReleased -= HandleGrappleReleased;
        }

        boundToGameManager = false;
        boundToPlayer = false;
    }

    private void Flash(Color color)
    {
        if (color.a >= targetOverlayColor.a)
        {
            targetOverlayColor = color;
        }
    }

    private void Play(string key)
    {
        if (oneShotSource == null || !clips.TryGetValue(key, out AudioClip clip) || clip == null)
        {
            return;
        }

        oneShotSource.PlayOneShot(clip);
    }

    private AudioClip CreateTone(string clipName, float frequency, float duration, float volume, float detune)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(duration * sampleRate);
        float[] samples = new float[sampleCount];

        for (int sampleIndex = 0; sampleIndex < sampleCount; sampleIndex++)
        {
            float t = sampleIndex / (float)sampleRate;
            float envelope = Mathf.Clamp01(1f - t / duration);
            envelope *= envelope;
            float waveA = Mathf.Sin(t * frequency * Mathf.PI * 2f);
            float waveB = Mathf.Sin(t * frequency * (1f + detune) * Mathf.PI * 2f);
            samples[sampleIndex] = (waveA * 0.7f + waveB * 0.3f) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
