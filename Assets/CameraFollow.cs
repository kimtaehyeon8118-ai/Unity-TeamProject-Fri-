using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.2f, -10f);

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float lookAheadDistance = 1.8f;
    [SerializeField] private float verticalLookAhead = 0.75f;

    [Header("Dead Zone")]
    [SerializeField] private Vector2 deadZone = new Vector2(1.2f, 0.8f);

    [Header("Bounds")]
    [SerializeField] private bool useBounds;
    [SerializeField] private Vector2 minBounds;
    [SerializeField] private Vector2 maxBounds;

    [Header("Game Feel")]
    [SerializeField] private float shakeDecay = 2.8f;
    [SerializeField] private float maxShakeOffset = 0.34f;
    [SerializeField] private float maxShakeRotation = 1.7f;
    [SerializeField] private float noiseFrequency = 22f;
    [SerializeField] private float zoomReturnSpeed = 7.5f;

    private Vector3 currentVelocity;
    private Vector3 focusPoint;
    private Camera cachedCamera;
    private float baseOrthoSize;
    private float trauma;
    private float zoomOffset;
    private float zoomVelocity;

    private void Start()
    {
        cachedCamera = GetComponent<Camera>();
        if (cachedCamera != null)
        {
            baseOrthoSize = cachedCamera.orthographicSize;
        }

        if (offset.z == 0f)
        {
            offset.z = -10f;
        }

        ResolveTarget();

        if (target != null)
        {
            focusPoint = target.position;
            transform.position = focusPoint + offset;
        }
    }

    private void LateUpdate()
    {
        ResolveTarget();

        if (target == null)
        {
            return;
        }

        Vector3 targetPosition = target.position;
        float horizontalDelta = targetPosition.x - focusPoint.x;
        float verticalDelta = targetPosition.y - focusPoint.y;

        if (Mathf.Abs(horizontalDelta) > deadZone.x)
        {
            focusPoint.x = targetPosition.x - Mathf.Sign(horizontalDelta) * deadZone.x;
        }

        if (Mathf.Abs(verticalDelta) > deadZone.y)
        {
            focusPoint.y = targetPosition.y - Mathf.Sign(verticalDelta) * deadZone.y;
        }

        Vector3 desiredPosition = focusPoint + offset;
        desiredPosition.x += Mathf.Sign(horizontalDelta) * lookAheadDistance;

        if (verticalDelta < -0.2f)
        {
            desiredPosition.y -= verticalLookAhead;
        }

        desiredPosition.z = offset.z;

        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minBounds.x, maxBounds.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minBounds.y, maxBounds.y);
        }

        Vector3 finalPosition = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, smoothTime);

        if (trauma > 0f)
        {
            float shakeAmount = trauma * trauma;
            float time = Time.unscaledTime * noiseFrequency;
            float xNoise = Mathf.PerlinNoise(time, 0.11f) * 2f - 1f;
            float yNoise = Mathf.PerlinNoise(0.17f, time) * 2f - 1f;
            float rotationNoise = Mathf.PerlinNoise(time, 0.37f) * 2f - 1f;

            finalPosition.x += xNoise * maxShakeOffset * shakeAmount;
            finalPosition.y += yNoise * maxShakeOffset * shakeAmount;
            transform.rotation = Quaternion.Euler(0f, 0f, rotationNoise * maxShakeRotation * shakeAmount);
            trauma = Mathf.MoveTowards(trauma, 0f, Time.unscaledDeltaTime * shakeDecay);
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }

        transform.position = finalPosition;

        if (cachedCamera != null)
        {
            baseOrthoSize = Mathf.Max(baseOrthoSize, 0.1f);
            zoomOffset = Mathf.SmoothDamp(zoomOffset, 0f, ref zoomVelocity, 1f / zoomReturnSpeed);
            cachedCamera.orthographicSize = Mathf.Max(0.1f, baseOrthoSize + zoomOffset);
        }
    }

    private void ResolveTarget()
    {
        if (target != null)
        {
            return;
        }

        PlayerController player = FindAnyObjectByType<PlayerController>();
        if (player != null)
        {
            target = player.transform;
        }
    }

    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void AddZoomImpulse(float delta)
    {
        zoomOffset += delta;
    }
}
