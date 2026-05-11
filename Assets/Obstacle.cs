using UnityEngine;

public class Obstacle : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damage = 1;
    [SerializeField] private float hitCooldown = 0.2f;

    [Header("Optional Movement")]
    [SerializeField] private float moveSpeed = 0f;
    [SerializeField] private float destroyX = -999f;
    [SerializeField] private bool moveLeft = false;

    private float lastHitTime = -999f;
    private StageClear stageClear;

    public int Damage => damage;
    public bool CanDamagePlayer => stageClear == null;

    private void Awake()
    {
        stageClear = GetComponent<StageClear>();
    }

    private void Update()
    {
        if (moveSpeed <= 0f)
        {
            return;
        }

        float direction = moveLeft ? -1f : 1f;
        transform.Translate(Vector3.right * direction * moveSpeed * Time.deltaTime, Space.World);

        if (destroyX > -998f)
        {
            bool outOfRange = moveLeft ? transform.position.x <= destroyX : transform.position.x >= destroyX;
            if (outOfRange)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryHit(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryHit(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryHit(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryHit(other.gameObject);
    }

    private void TryHit(GameObject other)
    {
        if (stageClear != null || !IsPlayer(other))
        {
            return;
        }

        if (Time.time < lastHitTime + hitCooldown)
        {
            return;
        }

        lastHitTime = Time.time;
        GameManager.Instance?.DamagePlayer(damage, transform.position);
    }

    private static bool IsPlayer(GameObject other)
    {
        return other != null
            && (other.CompareTag("Player") || other.GetComponentInParent<PlayerController>() != null);
    }
}
