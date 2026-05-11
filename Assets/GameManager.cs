using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private int maxHits = 3;
    [SerializeField] private float respawnDelay = 1.2f;
    [SerializeField] private Transform respawnPoint;

    [Header("Scene Flow")]
    [SerializeField] private float clearDelay = 0.75f;
    [SerializeField] private string titleSceneName = "TitleScene";
    [SerializeField] private string daySceneName = "DayScene";

    [Header("Stage Timer")]
    [SerializeField] private float stageTimeLimitSeconds = 120f;

    private PlayerController player;
    private Transform activeCheckpoint;
    private bool isRespawning;
    private bool isStageClearing;
    private bool isPaused;
    private bool timerExpired;
    private int currentHits;
    private float elapsedStageTime;

    public event Action<int, int> HealthChanged;
    public event Action<int, int> PlayerDamaged;
    public event Action<int, int> PlayerHealed;
    public event Action<bool> PauseStateChanged;
    public event Action<string> NotificationPushed;
    public event Action<Transform> CheckpointChanged;
    public event Action StageCleared;
    public event Action<string> StageFailed;

    public bool IsPaused => isPaused;
    public int CurrentHits => currentHits;
    public int MaxHits => maxHits;
    public Transform CurrentSpawnPoint => activeCheckpoint != null ? activeCheckpoint : respawnPoint;
    public float StageTimeLimitSeconds => Mathf.Max(1f, stageTimeLimitSeconds);
    public float RemainingStageTimeSeconds => Mathf.Clamp(StageTimeLimitSeconds - elapsedStageTime, 0f, StageTimeLimitSeconds);

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (SceneManager.GetActiveScene().name == "Stage01_CyberStreet" && !GameFlowState.ConsumeNightPlayRequest())
        {
            SceneManager.LoadScene(SceneFlowUtility.FindSceneIndexByName(daySceneName) >= 0
                ? SceneFlowUtility.FindSceneIndexByName(daySceneName)
                : SceneFlowUtility.ResolveGameplaySceneIndex(titleSceneName));
            Destroy(gameObject);
            return;
        }

        Instance = this;
        Time.timeScale = 1f;
    }

    private void Start()
    {
        RegisterPlayer(FindAnyObjectByType<PlayerController>());
        ResetStageState(pushNotification: true);
    }

    private void Update()
    {
        UpdateStageTimer();

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void RegisterPlayer(PlayerController targetPlayer)
    {
        player = targetPlayer;
        activeCheckpoint = respawnPoint;
        HealthChanged?.Invoke(RemainingHits(), maxHits);
    }

    public void TakeHit()
    {
        DamagePlayer(1, player != null ? (Vector2)player.transform.position : Vector2.zero);
    }

    public void DamagePlayer(int damage, Vector2 hazardPosition)
    {
        if (player == null || isRespawning || isStageClearing || isPaused || !player.CanTakeDamage)
        {
            return;
        }

        currentHits = Mathf.Clamp(currentHits + Mathf.Max(1, damage), 0, maxHits);
        player.ReceiveDamage(hazardPosition);
        HealthChanged?.Invoke(RemainingHits(), maxHits);
        PlayerDamaged?.Invoke(RemainingHits(), maxHits);

        if (currentHits >= maxHits)
        {
            StartCoroutine(RestartStageRoutine());
            return;
        }

        StartCoroutine(RespawnRoutine());
    }

    public void HealPlayer(int amount)
    {
        if (amount <= 0 || currentHits <= 0)
        {
            return;
        }

        currentHits = Mathf.Clamp(currentHits - amount, 0, maxHits);
        HealthChanged?.Invoke(RemainingHits(), maxHits);
        PlayerHealed?.Invoke(RemainingHits(), maxHits);
        PushNotification("Found usable ingredients");
    }

    public void ClearStage()
    {
        if (isRespawning || isStageClearing || timerExpired)
        {
            return;
        }

        StageCleared?.Invoke();
        StartCoroutine(ClearStageRoutine());
    }

    public void SetCheckpoint(Transform checkpoint)
    {
        if (checkpoint == null || checkpoint == activeCheckpoint)
        {
            return;
        }

        activeCheckpoint = checkpoint;
        CheckpointChanged?.Invoke(checkpoint);
        PushNotification(ResolveCheckpointMessage(checkpoint));
    }

    public int GetRemainingHits()
    {
        return RemainingHits();
    }

    public void TogglePause()
    {
        if (isStageClearing || timerExpired)
        {
            return;
        }

        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0f : 1f;
        PauseStateChanged?.Invoke(isPaused);
        PushNotification(isPaused ? "Paused" : "Resumed");
    }

    public void LoadGameplayScene()
    {
        Time.timeScale = 1f;
        int daySceneIndex = SceneFlowUtility.FindSceneIndexByName(daySceneName);
        SceneManager.LoadScene(daySceneIndex >= 0 ? daySceneIndex : SceneFlowUtility.ResolveGameplaySceneIndex(titleSceneName));
    }

    private IEnumerator RespawnRoutine()
    {
        isRespawning = true;
        PushNotification("Respawning...");

        player.SetControlEnabled(false);
        player.SetVisible(false);
        yield return new WaitForSeconds(respawnDelay);

        player.RespawnAt(CurrentSpawnPoint != null ? CurrentSpawnPoint.position : Vector3.zero);
        player.SetVisible(true);
        player.ApplyRespawnInvulnerability();
        player.SetControlEnabled(true);

        isRespawning = false;
        PushNotification("Back in action");
    }

    private IEnumerator RestartStageRoutine()
    {
        yield return RestartStageRoutine("Stage failed");
    }

    private IEnumerator RestartStageRoutine(string failureMessage)
    {
        isRespawning = true;
        timerExpired = failureMessage == "Time over";
        StageFailed?.Invoke(failureMessage);
        PushNotification(failureMessage);

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        yield return new WaitForSeconds(respawnDelay);

        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private IEnumerator ClearStageRoutine()
    {
        isStageClearing = true;
        string reward = GameProgression.GrantNightReward();
        PushNotification(string.IsNullOrEmpty(reward)
            ? "재료를 챙겼어요"
            : "새 재료 해금: " + reward);

        if (player != null)
        {
            player.SetControlEnabled(false);
        }

        yield return new WaitForSeconds(clearDelay);

        Time.timeScale = 1f;
        int daySceneIndex = SceneFlowUtility.FindSceneIndexByName(daySceneName);
        int nextSceneIndex = daySceneIndex >= 0
            ? daySceneIndex
            : SceneFlowUtility.ResolveNextSceneIndex(SceneManager.GetActiveScene().buildIndex, titleSceneName);
        SceneManager.LoadScene(nextSceneIndex);
    }

    private void ResetStageState(bool pushNotification)
    {
        currentHits = 0;
        isPaused = false;
        isRespawning = false;
        isStageClearing = false;
        timerExpired = false;
        elapsedStageTime = 0f;
        activeCheckpoint = respawnPoint;
        HealthChanged?.Invoke(RemainingHits(), maxHits);
        PauseStateChanged?.Invoke(false);

        if (pushNotification)
        {
            PushNotification("식당 밖으로 나가 밤 재료를 찾아보세요");
        }
    }

    private string ResolveCheckpointMessage(Transform checkpoint)
    {
        if (checkpoint == null)
        {
            return "체크포인트가 갱신되었습니다";
        }

        return checkpoint.name switch
        {
            "Checkpoint_Street" => "거리 구간을 통과했습니다",
            "Checkpoint_Market" => "시장 공급 구간에 진입했습니다",
            "Checkpoint_Rooftop" => "옥상 식재료 구간으로 올라갑니다",
            _ => "체크포인트가 갱신되었습니다"
        };
    }

    private int RemainingHits()
    {
        return Mathf.Clamp(maxHits - currentHits, 0, maxHits);
    }

    private void UpdateStageTimer()
    {
        if (isPaused || isRespawning || isStageClearing || timerExpired)
        {
            return;
        }

        elapsedStageTime += Time.deltaTime;

        if (elapsedStageTime >= StageTimeLimitSeconds)
        {
            elapsedStageTime = StageTimeLimitSeconds;
            timerExpired = true;
            StartCoroutine(RestartStageRoutine("Time over"));
        }
    }

    private void PushNotification(string message)
    {
        NotificationPushed?.Invoke(message);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Time.timeScale = 1f;
            Instance = null;
        }
    }
}
