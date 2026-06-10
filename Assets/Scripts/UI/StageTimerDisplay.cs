using UnityEngine;
using UnityEngine.UI;

public class StageTimerDisplay : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.28f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.35f, 0.35f, 1f);

    private Image timerFrame;

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<Text>();
        }

        EnsureTimerFrame();
    }

    private void OnEnable()
    {
        EnsureTimerFrame();
        RefreshTimerText();
    }

    private void EnsureTimerFrame()
    {
        if (timerText == null)
        {
            timerText = GetComponent<Text>();
        }

        if (timerText == null)
        {
            return;
        }

        Transform canvas = timerText.transform.parent;
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.Find("TimerFrame");
        if (existing != null)
        {
            timerFrame = existing.GetComponent<Image>();
        }

        if (timerFrame == null)
        {
            GameObject frameObject = new GameObject("TimerFrame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            frameObject.transform.SetParent(canvas, false);
            timerFrame = frameObject.GetComponent<Image>();
        }

        timerFrame.sprite = Resources.Load<Sprite>("Graphics/UI/Hud/timer_frame");
        timerFrame.color = Color.white;
        timerFrame.preserveAspect = true;
        timerFrame.raycastTarget = false;

        RectTransform frameRect = timerFrame.rectTransform;
        frameRect.anchorMin = new Vector2(1f, 1f);
        frameRect.anchorMax = new Vector2(1f, 1f);
        frameRect.pivot = new Vector2(1f, 1f);
        frameRect.anchoredPosition = new Vector2(-18f, -16f);
        frameRect.sizeDelta = new Vector2(286f, 116f);

        RectTransform textRect = timerText.rectTransform;
        textRect.anchorMin = new Vector2(1f, 1f);
        textRect.anchorMax = new Vector2(1f, 1f);
        textRect.pivot = new Vector2(1f, 1f);
        textRect.anchoredPosition = new Vector2(-39f, -33f);
        textRect.sizeDelta = new Vector2(244f, 78f);
        timerText.alignment = TextAnchor.MiddleCenter;
        timerText.fontStyle = FontStyle.Bold;
        timerText.resizeTextForBestFit = true;
        timerText.resizeTextMinSize = 24;
        timerText.resizeTextMaxSize = 38;

        timerFrame.transform.SetSiblingIndex(Mathf.Max(0, timerText.transform.GetSiblingIndex() - 1));

        Transform oldPanel = canvas.Find("HudPanel_TopRight");
        if (oldPanel != null)
        {
            Image oldPanelImage = oldPanel.GetComponent<Image>();
            if (oldPanelImage != null)
            {
                oldPanelImage.enabled = false;
            }
        }
    }

    private void Update()
    {
        RefreshTimerText();
    }

    private void RefreshTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        GameManager gameManager = GameManager.Instance;
        if (gameManager == null)
        {
            timerText.text = "--:--";
            return;
        }

        float remaining = gameManager.RemainingStageTimeSeconds;
        int minutes = Mathf.FloorToInt(remaining / 60f);
        int seconds = Mathf.CeilToInt(remaining % 60f);

        if (seconds == 60)
        {
            minutes += 1;
            seconds = 0;
        }

        timerText.text = $"{minutes:00}:{seconds:00}";

        if (remaining <= 15f)
        {
            timerText.color = dangerColor;
        }
        else if (remaining <= 45f)
        {
            timerText.color = warningColor;
        }
        else
        {
            timerText.color = normalColor;
        }
    }
}
