using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [SerializeField] private int healAmount = 1;
    [SerializeField] private bool destroyOnPickup = true;
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobSpeed = 2.4f;

    private Vector3 startPosition;

    private void Awake()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        transform.position = startPosition + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        GameManager.Instance?.HealPlayer(healAmount);

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
    }
}
