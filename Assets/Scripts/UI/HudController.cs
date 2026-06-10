using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private Coroutine tutorialRoutine;
    private bool isBound;
    private Image healthImage;
    private Sprite[] healthSprites;
    private CanvasGroup tutorialGroup;
    private Text tutorialText;
    private PlayerController tutorialPlayer;
    private Transform firstDamageObject;
    private Transform clearGoal;
    private bool damageTutorialShown;
    private bool goalTutorialShown;
    private bool tutorialsEnabled;

    private void Awake()
    {
        tutorialsEnabled = SceneManager.GetActiveScene().name == "Stage01_CyberStreet";
        EnsureHealthImage();

        if (controlsText != null)
        {
            controlsText.text = "방향키 이동    SPACE 점프    Z 와이어    SHIFT 대쉬    ESC 일시정지";
            controlsText.gameObject.SetActive(tutorialsEnabled);
        }

        if (healthText != null)
        {
            healthText.supportRichText = true;
            healthText.text = string.Empty;
            healthText.enabled = false;
        }

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        ApplyHudPreset();
        if (tutorialsEnabled)
        {
            EnsureTutorialPanel();
        }
    }

    private void Start()
    {
        TryBindToGameManager();
        RefreshHudState();
        if (tutorialsEnabled)
        {
            CacheStageTutorialTargets();
            ShowStartTutorial();
        }
    }

    private void OnEnable()
    {
        TryBindToGameManager();
        RefreshHudState();
    }

    private void OnDisable()
    {
        UnbindFromGameManager();

        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
            tutorialRoutine = null;
        }
    }

    private void Update()
    {
        if (!isBound && GameManager.Instance != null)
        {
            TryBindToGameManager();
            RefreshHudState();
        }

        if (tutorialsEnabled)
        {
            UpdateContextTutorials();
        }
    }

    private void UpdateHealth(int remainingHits, int maxHits)
    {
        EnsureHealthImage();
        remainingHits = Mathf.Clamp(remainingHits, 0, 3);
        if (healthImage != null && healthSprites != null && remainingHits < healthSprites.Length)
        {
            healthImage.sprite = healthSprites[remainingHits];
            healthImage.enabled = healthImage.sprite != null;
        }

        if (healthText != null)
        {
            healthText.text = string.Empty;
            healthText.enabled = false;
        }
    }

    private void EnsureHealthImage()
    {
        if (healthSprites == null || healthSprites.Length != 4)
        {
            healthSprites = new[]
            {
                Resources.Load<Sprite>("Graphics/UI/Hud/health_0"),
                Resources.Load<Sprite>("Graphics/UI/Hud/health_1"),
                Resources.Load<Sprite>("Graphics/UI/Hud/health_2"),
                Resources.Load<Sprite>("Graphics/UI/Hud/health_3")
            };
        }

        Transform existing = transform.Find("HealthImage");
        if (existing != null)
        {
            healthImage = existing.GetComponent<Image>();
        }

        if (healthImage == null)
        {
            GameObject imageObject = new GameObject("HealthImage", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            imageObject.transform.SetParent(transform, false);
            healthImage = imageObject.GetComponent<Image>();
        }

        RectTransform rect = healthImage.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(20f, -18f);
        rect.sizeDelta = new Vector2(270f, 96f);
        healthImage.preserveAspect = true;
        healthImage.raycastTarget = false;
        healthImage.color = Color.white;
        if (healthImage.sprite == null && healthSprites[3] != null)
        {
            healthImage.sprite = healthSprites[3];
        }
        healthImage.transform.SetAsLastSibling();

        HidePanel("HudPanel_TopLeft");
    }

    private void EnsureTutorialPanel()
    {
        Transform existing = transform.Find("StartTutorial");
        GameObject panel;
        if (existing != null)
        {
            panel = existing.gameObject;
        }
        else
        {
            panel = new GameObject(
                "StartTutorial",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(CanvasGroup));
            panel.transform.SetParent(transform, false);
        }

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -128f);
        panelRect.sizeDelta = new Vector2(570f, 176f);

        Image panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color32(6, 12, 20, 218);
        panelImage.raycastTarget = false;

        Outline outline = panel.GetComponent<Outline>();
        if (outline == null)
            outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color32(97, 231, 221, 80);
        outline.effectDistance = new Vector2(1f, -1f);

        tutorialGroup = panel.GetComponent<CanvasGroup>();
        tutorialGroup.interactable = false;
        tutorialGroup.blocksRaycasts = false;

        Transform textTransform = panel.transform.Find("Text");
        if (textTransform == null)
        {
            GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            textTransform = textObject.transform;
        }

        tutorialText = textTransform.GetComponent<Text>();
        tutorialText.font = controlsText != null ? controlsText.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        tutorialText.fontSize = 20;
        tutorialText.fontStyle = FontStyle.Bold;
        tutorialText.alignment = TextAnchor.MiddleLeft;
        tutorialText.color = HudTextTint;
        tutorialText.supportRichText = true;
        tutorialText.resizeTextForBestFit = true;
        tutorialText.resizeTextMinSize = 15;
        tutorialText.resizeTextMaxSize = 20;
        tutorialText.raycastTarget = false;
        tutorialText.text =
            "<color=#61E7DD>방향키</color>를 눌러 캐릭터를 조작할 수 있습니다.\n" +
            "<color=#61E7DD>Space 키</color>를 눌러 점프를 사용할 수 있습니다.\n" +
            "<color=#61E7DD>Z 키</color>를 눌러 와이어를 연결할 수 있습니다.\n" +
            "<color=#61E7DD>Shift 키</color>를 눌러 대쉬를 사용할 수 있습니다.";

        RectTransform textRect = tutorialText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(22f, 14f);
        textRect.offsetMax = new Vector2(-22f, -14f);

        Shadow shadow = tutorialText.GetComponent<Shadow>();
        if (shadow == null)
            shadow = tutorialText.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color32(0, 0, 0, 190);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);

        panel.transform.SetAsLastSibling();
        panel.SetActive(false);
    }

    private void ShowStartTutorial()
    {
        if (tutorialGroup == null)
        {
            return;
        }

        ShowTutorial(
            "<color=#61E7DD>방향키</color>를 눌러 캐릭터를 조작할 수 있습니다.\n" +
            "<color=#61E7DD>Space 키</color>를 눌러 점프를 사용할 수 있습니다.\n" +
            "<color=#61E7DD>Z 키</color>를 눌러 와이어를 연결할 수 있습니다.\n" +
            "<color=#61E7DD>Shift 키</color>를 눌러 대쉬를 사용할 수 있습니다.",
            8f);
    }

    private void CacheStageTutorialTargets()
    {
        tutorialPlayer = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (tutorialPlayer == null)
        {
            return;
        }

        float playerX = tutorialPlayer.transform.position.x;
        float nearestX = float.PositiveInfinity;
        Obstacle[] obstacles = FindObjectsByType<Obstacle>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (Obstacle obstacle in obstacles)
        {
            if (obstacle == null || !obstacle.gameObject.activeInHierarchy || !obstacle.CanDamagePlayer)
            {
                continue;
            }

            float obstacleX = obstacle.transform.position.x;
            if (obstacleX > playerX + 1f && obstacleX < nearestX)
            {
                nearestX = obstacleX;
                firstDamageObject = obstacle.transform;
            }
        }

        StageClear stageClear = FindFirstObjectByType<StageClear>(FindObjectsInactive.Include);
        clearGoal = stageClear != null ? stageClear.transform : null;
    }

    private void UpdateContextTutorials()
    {
        if (tutorialPlayer == null)
        {
            CacheStageTutorialTargets();
        }

        if (tutorialPlayer == null)
        {
            return;
        }

        float playerX = tutorialPlayer.transform.position.x;
        if (!damageTutorialShown
            && firstDamageObject != null
            && playerX >= firstDamageObject.position.x - 10f)
        {
            damageTutorialShown = true;
            ShowTutorial(
                "<color=#FF6A78>위험 오브젝트</color>에 닿으면 체력이 줄어듭니다.",
                4.5f);
        }

        if (!goalTutorialShown
            && clearGoal != null
            && playerX >= clearGoal.position.x - 14f)
        {
            goalTutorialShown = true;
            ShowTutorial(
                "<color=#61E7DD>목표 지점</color>에 도착하면 스테이지가 클리어됩니다.",
                4.5f);
        }
    }

    private void ShowTutorial(string message, float displayDuration)
    {
        if (tutorialGroup == null || tutorialText == null)
        {
            return;
        }

        tutorialText.text = message;
        tutorialGroup.gameObject.SetActive(true);
        tutorialGroup.alpha = 1f;

        if (tutorialRoutine != null)
        {
            StopCoroutine(tutorialRoutine);
        }

        tutorialRoutine = StartCoroutine(HideTutorialRoutine(displayDuration));
    }

    private IEnumerator HideTutorialRoutine(float displayDuration)
    {
        yield return new WaitForSecondsRealtime(displayDuration);

        const float fadeDuration = 0.8f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            tutorialGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }

        tutorialGroup.gameObject.SetActive(false);
        tutorialRoutine = null;
    }

    private void HidePanel(string objectName)
    {
        Transform panel = transform.Find(objectName);
        if (panel == null)
        {
            return;
        }

        Image panelImage = panel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.enabled = false;
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
