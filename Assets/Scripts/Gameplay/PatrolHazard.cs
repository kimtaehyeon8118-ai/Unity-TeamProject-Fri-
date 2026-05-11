using UnityEngine;

public class PatrolHazard : MonoBehaviour
{
    [SerializeField] private int contactDamage = 1;
    [SerializeField] private float patrolDistance = 2f;
    [SerializeField] private float patrolSpeed = 1.4f;
    [SerializeField] private bool startMovingRight = true;
    [SerializeField] private SpriteRenderer visual;

    private Vector3 origin;
    private bool movingRight;

    public int Damage => contactDamage;

    private void Awake()
    {
        origin = transform.position;
        movingRight = startMovingRight;

        if (visual == null)
        {
            visual = GetComponent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        float direction = movingRight ? 1f : -1f;
        transform.position += Vector3.right * direction * patrolSpeed * Time.deltaTime;

        if (movingRight && transform.position.x >= origin.x + patrolDistance)
        {
            movingRight = false;
        }
        else if (!movingRight && transform.position.x <= origin.x - patrolDistance)
        {
            movingRight = true;
        }

        if (visual != null)
        {
            visual.flipX = movingRight;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        TryDamage(collision.gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryDamage(other.gameObject);
    }

    private void TryDamage(GameObject target)
    {
        if (!IsPlayer(target))
        {
            return;
        }

        GameManager.Instance?.DamagePlayer(contactDamage, transform.position);
    }

    private static bool IsPlayer(GameObject target)
    {
        return target != null
            && (target.CompareTag("Player") || target.GetComponentInParent<PlayerController>() != null);
    }
}
