using UnityEngine;
using UnityEngine.UI;

public class StageTimerDisplay : MonoBehaviour
{
    [SerializeField] private Text timerText;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color warningColor = new Color(1f, 0.82f, 0.28f, 1f);
    [SerializeField] private Color dangerColor = new Color(1f, 0.35f, 0.35f, 1f);

    private void Awake()
    {
        if (timerText == null)
        {
            timerText = GetComponent<Text>();
        }
    }

    private void OnEnable()
    {
        RefreshTimerText();
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
