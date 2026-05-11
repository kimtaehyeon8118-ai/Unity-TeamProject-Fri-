using System.Collections.Generic;
using UnityEngine;

public class GrappleAnchor : MonoBehaviour
{
    private static readonly List<GrappleAnchor> ActiveAnchors = new List<GrappleAnchor>();

    [SerializeField] private Transform attachPoint;
    [SerializeField] private SpriteRenderer indicator;
    [SerializeField] private float scoreBonus;
    [SerializeField] private Color idleColor = new Color(0.25f, 0.92f, 1f, 0.82f);
    [SerializeField] private Color previewColor = new Color(1f, 0.98f, 0.72f, 0.98f);
    [SerializeField] private Color attachedColor = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Vector3 previewScale = new Vector3(0.34f, 0.34f, 1f);
    [SerializeField] private Vector3 attachedScale = new Vector3(0.42f, 0.42f, 1f);

    private bool isPreviewed;
    private bool isAttached;
    private Vector3 baseScale;

    public Vector2 WorldPosition => attachPoint != null ? attachPoint.position : transform.position;

    private void Awake()
    {
        CacheReferences();
        ApplyVisualState();
    }

    private void OnEnable()
    {
        if (!ActiveAnchors.Contains(this))
        {
            ActiveAnchors.Add(this);
        }

        CacheReferences();
        ApplyVisualState();
    }

    private void OnDisable()
    {
        ActiveAnchors.Remove(this);
    }

    public static GrappleAnchor FindBestAnchor(Vector2 origin, Vector2 facing, float maxDistance, float minVerticalOffset, float forwardBias)
    {
        GrappleAnchor bestAnchor = null;
        float bestScore = float.NegativeInfinity;
        Vector2 normalizedFacing = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector2.right;

        for (int index = 0; index < ActiveAnchors.Count; index++)
        {
            GrappleAnchor anchor = ActiveAnchors[index];
            if (anchor == null || !anchor.isActiveAndEnabled)
            {
                continue;
            }

            Vector2 delta = anchor.WorldPosition - origin;
            float distance = delta.magnitude;
            if (distance > maxDistance || delta.y < minVerticalOffset)
            {
                continue;
            }

            float facingDot = Vector2.Dot(delta.normalized, normalizedFacing);
            float verticalScore = delta.y * 1.35f;
            float forwardScore = Mathf.Max(0f, facingDot) * forwardBias;
            float centeredBonus = Mathf.Clamp01(1f - Mathf.Abs(delta.x) / maxDistance) * 0.9f;
            float score = verticalScore + forwardScore + centeredBonus - distance * 0.38f + anchor.scoreBonus;

            if (score > bestScore)
            {
                bestScore = score;
                bestAnchor = anchor;
            }
        }

        return bestAnchor;
    }

    public void SetPreview(bool active)
    {
        if (isAttached)
        {
            return;
        }

        isPreviewed = active;
        ApplyVisualState();
    }

    public void SetHighlighted(bool active)
    {
        isAttached = active;

        if (active)
        {
            isPreviewed = false;
        }

        ApplyVisualState();
    }

    private void CacheReferences()
    {
        if (indicator == null)
        {
            indicator = GetComponent<SpriteRenderer>();
        }

        if (indicator != null)
        {
            baseScale = previewScale;
        }
    }

    private void ApplyVisualState()
    {
        if (indicator == null)
        {
            return;
        }

        if (isAttached)
        {
            indicator.color = attachedColor;
            indicator.transform.localScale = attachedScale;
        }
        else if (isPreviewed)
        {
            indicator.color = previewColor;
            indicator.transform.localScale = attachedScale;
        }
        else
        {
            indicator.color = idleColor;
            indicator.transform.localScale = previewScale;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.25f, 0.92f, 1f, 0.6f);
        Gizmos.DrawWireSphere(WorldPosition, 0.18f);
    }
}
