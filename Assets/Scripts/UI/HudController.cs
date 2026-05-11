using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class HudController : MonoBehaviour
{
    [SerializeField] private Text healthText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text controlsText;
    [SerializeField] private GameObject pausePanel;

    private static readonly Color32 HudPanelTint = new Color32(6, 12, 20, 178);
    private static readonly Color32 HudTextTint = new Color32(219, 234, 226, 245);
    private static readonly Color32 HudMutedTint = new Color32(136, 156, 156, 205);
    private static readonly Color32 HudAccentTint = new Color32(97, 231, 221, 255);

    private Coroutine messageRoutine;
    private bool isBound;

    private void Awake()
    {
        if (controlsText != null)
        {
            controlsText.text = "A/D MOVE    SPACE JUMP    LMB GRAPPLE    SHIFT DASH    ESC PAUSE";
        }

        if (healthText != null)
        {
            healthText.supportRichText = true;
            healthText.text = BuildHeartDisplay(3, 3);
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        ApplyHudPreset();
    }

    private void Start()
    {
        TryBindToGameManager();
        RefreshHudState();
    }

    private void OnEnable()
    {
        TryBindToGameManager();
        RefreshHudState();
    }

    private void OnDisable()
    {
        UnbindFromGameManager();
    }

    private void Update()
    {
        if (!isBound && GameManager.Instance != null)
        {
            TryBindToGameManager();
            RefreshHudState();
        }
    }

    private void UpdateHealth(int remainingHits, int maxHits)
    {
        if (healthText != null)
        {
            healthText.text = BuildHeartDisplay(remainingHits, maxHits);
        }
    }

    private string BuildHeartDisplay(int remainingHits, int maxHits)
    {
        remainingHits = Mathf.Clamp(remainingHits, 0, maxHits);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(maxHits * 18);
        builder.Append("<color=#61E7DD>VITALS</color>  ");

        for (int index = 0; index < maxHits; index++)
        {
            bool filled = index < remainingHits;
            string color = filled ? "#FF6A78" : "#53616A";
            builder.Append($"<color={color}>{(filled ? "♥" : "♡")}</color>");

            if (index < maxHits - 1)
            {
                builder.Append(' ');
            }
        }

        return builder.ToString();
    }

    private void UpdatePauseState(bool paused)
    {
        if (pausePanel != null)
        {
            pausePanel.SetActive(paused);
        }

        if (paused)
        {
            SetStatus("Paused");
        }
        else if (statusText != null && statusText.text == "Paused")
        {
            statusText.text = string.Empty;
        }
    }

    private void ShowMessage(string message)
    {
        if (messageRoutine != null)
        {
            StopCoroutine(messageRoutine);
        }

        messageRoutine = StartCoroutine(MessageRoutine(message));
    }

    private IEnumerator MessageRoutine(string message)
    {
        SetStatus(message);
        yield return new WaitForSecondsRealtime(1.8f);

        if (statusText != null && statusText.text == message)
        {
            statusText.text = string.Empty;
        }

        messageRoutine = null;
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = string.IsNullOrEmpty(message) ? string.Empty : message.ToUpperInvariant();
        }
    }

    private void ApplyHudPreset()
    {
        StyleText(healthText, 22, FontStyle.Bold, TextAnchor.MiddleLeft, HudTextTint);
        StyleText(statusText, 21, FontStyle.Bold, TextAnchor.MiddleCenter, HudAccentTint);
        StyleText(controlsText, 13, FontStyle.Bold, TextAnchor.MiddleCenter, HudMutedTint);

        PlaceRect(healthText != null ? healthText.rectTransform : null, new Vector2(0.015f, 0.925f), new Vector2(0.31f, 0.985f), Vector2.zero, Vector2.zero);
        PlaceRect(statusText != null ? statusText.rectTransform : null, new Vector2(0.35f, 0.925f), new Vector2(0.65f, 0.985f), Vector2.zero, Vector2.zero);
        PlaceRect(controlsText != null ? controlsText.rectTransform : null, new Vector2(0.23f, 0.02f), new Vector2(0.77f, 0.075f), Vector2.zero, Vector2.zero);

        StylePanel(healthText != null ? healthText.transform.parent : null);
        StylePanel(statusText != null ? statusText.transform.parent : null);
        StylePanel(controlsText != null ? controlsText.transform.parent : null);

        if (pausePanel != null)
        {
            Image pauseImage = pausePanel.GetComponent<Image>();
            if (pauseImage != null)
                pauseImage.color = new Color32(5, 8, 13, 218);
        }
    }

    private static void StyleText(Text target, int size, FontStyle style, TextAnchor alignment, Color color)
    {
        if (target == null)
            return;

        target.supportRichText = true;
        target.fontSize = size;
        target.fontStyle = style;
        target.alignment = alignment;
        target.color = color;
        target.resizeTextForBestFit = true;
        target.resizeTextMinSize = Mathf.Max(10, size - 5);
        target.resizeTextMaxSize = size;

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color32(0, 0, 0, 180);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
    }

    private static void StylePanel(Transform panelTransform)
    {
        if (panelTransform == null)
            return;

        Image image = panelTransform.GetComponent<Image>();
        if (image != null)
            image.color = HudPanelTint;

        Outline outline = panelTransform.GetComponent<Outline>();
        if (outline == null)
            outline = panelTransform.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color32(95, 231, 218, 58);
        outline.effectDistance = new Vector2(1f, -1f);
    }

    private static void PlaceRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private void TryBindToGameManager()
    {
        if (isBound || GameManager.Instance == null)
        {
            return;
        }

        GameManager.Instance.HealthChanged += UpdateHealth;
        GameManager.Instance.PauseStateChanged += UpdatePauseState;
        GameManager.Instance.NotificationPushed += ShowMessage;
        isBound = true;
    }

    private void UnbindFromGameManager()
    {
        if (!isBound || GameManager.Instance == null)
        {
            isBound = false;
            return;
        }

        GameManager.Instance.HealthChanged -= UpdateHealth;
        GameManager.Instance.PauseStateChanged -= UpdatePauseState;
        GameManager.Instance.NotificationPushed -= ShowMessage;
        isBound = false;
    }

    private void RefreshHudState()
    {
        if (GameManager.Instance == null)
        {
            return;
        }

        UpdateHealth(GameManager.Instance.GetRemainingHits(), GameManager.Instance.MaxHits);
        UpdatePauseState(GameManager.Instance.IsPaused);
    }
}
