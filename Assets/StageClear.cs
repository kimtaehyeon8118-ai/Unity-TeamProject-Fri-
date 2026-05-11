using UnityEngine;

public class StageClear : MonoBehaviour
{
    [SerializeField] private bool triggerOnly = true;

    private void Awake()
    {
        Collider2D goalCollider = GetComponent<Collider2D>();
        if (goalCollider != null && triggerOnly)
        {
            goalCollider.isTrigger = true;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggerOnly)
        {
            TryClear(collision.gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryClear(other.gameObject);
    }

    private void TryClear(GameObject other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        GameManager.Instance?.ClearStage();
    }
}
