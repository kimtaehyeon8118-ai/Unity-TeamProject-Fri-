using System;
using UnityEngine;
using UnityEngine.UI;

public sealed class ButtonMashingGauge : MonoBehaviour
{
    public enum GaugeResult
    {
        Low,
        Good,
        Overheat
    }

    [Header("UI")]
    [SerializeField] private RectTransform gaugeBarArea;
    [SerializeField] private RectTransform pointer;
    [SerializeField] private Image gaugeFrame;
    [SerializeField] private Image gaugeBar;
    [SerializeField] private Image guideTextImage;

    [Header("Gauge")]
    [SerializeField, Range(0f, 1f)] private float gaugeValue;
    [SerializeField, Range(0.01f, 1f)] private float inputIncrease = 0.125f;
    [SerializeField, Min(0f)] private float decreaseSpeed = 0.18f;
    [SerializeField, Min(0.1f)] private float timeLimit = 4f;
    [SerializeField, Range(0f, 1f)] private float goodMin = 0.38f;
    [SerializeField, Range(0f, 1f)] private float goodMax = 0.82f;

    public event Action<GaugeResult> OnGaugeFinished;

    private bool isRunning;
    private float elapsedTime;

    public RectTransform GaugeBarArea => gaugeBarArea;
    public RectTransform Pointer => pointer;
    public Image GaugeFrame => gaugeFrame;
    public Image GaugeBar => gaugeBar;
    public Image GuideTextImage => guideTextImage;
    public float CurrentValue => gaugeValue;
    public bool IsRunning => isRunning;

    private void Awake()
    {
        SetVisible(false);
        UpdatePointer();
    }

    private void Update()
    {
        if (!isRunning)
            return;

        elapsedTime += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
            gaugeValue += inputIncrease;
        else
            gaugeValue -= decreaseSpeed * Time.deltaTime;

        gaugeValue = Mathf.Clamp01(gaugeValue);
        UpdatePointer();

        if (elapsedTime >= timeLimit)
            FinishGauge();
    }

    public void Configure(RectTransform barArea, RectTransform pointerRect, Image frameImage, Image barImage, Image guideImage)
    {
        gaugeBarArea = barArea;
        pointer = pointerRect;
        gaugeFrame = frameImage;
        gaugeBar = barImage;
        guideTextImage = guideImage;
        UpdatePointer();
    }

    public void StartGauge()
    {
        elapsedTime = 0f;
        gaugeValue = 0f;
        isRunning = true;
        SetVisible(true);
        UpdatePointer();
    }

    public void StopGauge()
    {
        isRunning = false;
        SetVisible(false);
    }

    public GaugeResult GetCurrentResult()
    {
        if (gaugeValue < goodMin)
            return GaugeResult.Low;

        return gaugeValue <= goodMax ? GaugeResult.Good : GaugeResult.Overheat;
    }

    private void FinishGauge()
    {
        if (!isRunning)
            return;

        isRunning = false;
        GaugeResult result = GetCurrentResult();
        SetVisible(false);
        OnGaugeFinished?.Invoke(result);
    }

    private void UpdatePointer()
    {
        if (gaugeBarArea == null || pointer == null)
            return;

        Rect rect = gaugeBarArea.rect;
        float x = Mathf.Lerp(rect.xMin, rect.xMax, gaugeValue);
        Vector2 anchoredPosition = pointer.anchoredPosition;
        anchoredPosition.x = x;
        pointer.anchoredPosition = anchoredPosition;
    }

    private void SetVisible(bool isVisible)
    {
        gameObject.SetActive(isVisible);
    }
}
