using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private Color inactiveColor = new Color(0.4f, 0.8f, 1f, 0.55f);
    [SerializeField] private Color activeColor = new Color(0.2f, 1f, 0.45f, 1f);

    private bool activated;

    private void Awake()
    {
        SetVisual(false);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        activated = true;
        SetVisual(true);
        GameManager.Instance?.SetCheckpoint(transform);
    }

    private void SetVisual(bool active)
    {
        if (indicator == null)
        {
            indicator = GetComponent<SpriteRenderer>();
        }

        if (indicator != null)
        {
            indicator.color = active ? activeColor : inactiveColor;
        }

        SpriteAccentAnimator accentAnimator = GetComponent<SpriteAccentAnimator>();
        if (accentAnimator != null)
        {
            accentAnimator.SnapToCurrentVisual();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            SetVisual(activated);
        }
    }
}
