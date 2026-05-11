using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class StageArtApplier : MonoBehaviour
{
    [SerializeField] private bool applyInEditMode = true;

    private readonly Dictionary<string, Sprite> runtimeSprites = new Dictionary<string, Sprite>();
    private static readonly Color32 SkyTopTint = new Color32(7, 12, 23, 255);
    private static readonly Color32 SkyMidTint = new Color32(13, 25, 43, 255);
    private static readonly Color32 FogTint = new Color32(73, 115, 130, 32);
    private static readonly Color32 DistantCityTint = new Color32(24, 45, 67, 120);
    private static readonly Color32 PlatformTint = new Color32(57, 69, 79, 255);
    private static readonly Color32 HazardTint = new Color32(198, 168, 91, 255);
    private static readonly Color32 NeonCyanTint = new Color32(70, 224, 230, 255);
    private static readonly Color32 NeonCoralTint = new Color32(255, 91, 97, 255);
    private static readonly Color32 NeonVioletTint = new Color32(150, 96, 210, 255);
#if UNITY_EDITOR
    private bool editorApplyQueued;
#endif

    private void Awake()
    {
        TryApply();
    }

    private void OnEnable()
    {
        TryApply();
    }

    private void OnValidate()
    {
        TryApply();
    }

    private void TryApply()
    {
        if (!Application.isPlaying && !applyInEditMode)
        {
            return;
        }

        if (!Application.isPlaying)
        {
#if UNITY_EDITOR
            QueueEditorApply();
#endif
            return;
        }

        ApplyStageArt();
    }

#if UNITY_EDITOR
    private void QueueEditorApply()
    {
        if (editorApplyQueued)
        {
            return;
        }

        editorApplyQueued = true;
        EditorApplication.delayCall += ApplyStageArtAfterValidation;
    }

    private void ApplyStageArtAfterValidation()
    {
        editorApplyQueued = false;
        if (this == null || !applyInEditMode)
        {
            return;
        }

        ApplyStageArt();
    }
#endif

    private void ApplyStageArt()
    {
        SpriteRenderer[] renderers = FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        Bounds stageBounds = new Bounds(Vector3.zero, new Vector3(24f, 12f, 0f));
        bool hasBounds = false;

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
            {
                continue;
            }

            string objectName = renderer.gameObject.name;
            if (objectName == "_ArtVisual")
            {
                continue;
            }

            if (renderer.sprite != null && IsStageVisual(objectName))
            {
                if (!hasBounds)
                {
                    stageBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    stageBounds.Encapsulate(renderer.bounds);
                }
            }

            if (objectName.StartsWith("Vending_"))
            {
                ApplyGroundObstacle(renderer);
            }
            else if (objectName.StartsWith("DebrisHazard_"))
            {
                ApplyGroundHazard(renderer, UseWireHazard(objectName));
            }
            else
            {
                RestoreBaseVisual(renderer);
            }

            EnsureContactDamageComponent(renderer);
            SyncGameplayColliderSafety(renderer);
            ApplyRendererMood(renderer);
        }

        EnsureStageAtmosphere(stageBounds);
        ApplyCameraMood();
    }

    private static bool UseMetalBarrier(string objectName)
    {
        return objectName.EndsWith("_B")
            || objectName.EndsWith("_D")
            || objectName.EndsWith("_F");
    }

    private static bool UseWireHazard(string objectName)
    {
        return objectName.EndsWith("_3")
            || objectName.EndsWith("_5")
            || objectName.EndsWith("_7");
    }

    private static bool IsStageVisual(string objectName)
    {
        return objectName.StartsWith("Ground")
            || objectName.StartsWith("SkyDeck")
            || objectName.StartsWith("DashDeck")
            || objectName.StartsWith("GoalDeck")
            || objectName.StartsWith("FinalLift")
            || objectName.StartsWith("Vending")
            || objectName.StartsWith("Debris")
            || objectName.StartsWith("CeilingShard")
            || objectName.StartsWith("Neon")
            || objectName.StartsWith("Checkpoint")
            || objectName == "Scaffold_Start"
            || objectName == "BossPerch";
    }

    private void ApplyRendererMood(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        string objectName = renderer.gameObject.name;
        if (objectName.StartsWith("NeonStrip") || objectName.StartsWith("NeonSign"))
        {
            renderer.color = GetNeonTint(objectName);
            EnsureGlowChild(renderer, renderer.color, 1.8f, 0.24f);
            return;
        }

        if (objectName == "BackgroundArt" || objectName == "SkyBackdrop")
        {
            renderer.color = new Color32(12, 20, 34, 255);
            renderer.sortingOrder = Mathf.Min(renderer.sortingOrder, -900);
            return;
        }

        if (objectName == "CityGlow")
        {
            renderer.color = DistantCityTint;
            renderer.sortingOrder = Mathf.Min(renderer.sortingOrder, -820);
            return;
        }

        if (objectName.StartsWith("Ground")
            || objectName.StartsWith("SkyDeck")
            || objectName.StartsWith("DashDeck")
            || objectName.StartsWith("GoalDeck")
            || objectName.StartsWith("FinalLift")
            || objectName == "Scaffold_Start"
            || objectName == "BossPerch")
        {
            renderer.color = PlatformTint;
            return;
        }

        if (objectName.StartsWith("DebrisPile"))
        {
            renderer.color = new Color32(70, 77, 80, 255);
        }
    }

    private static Color32 GetNeonTint(string objectName)
    {
        int hash = Mathf.Abs(objectName.GetHashCode());
        switch (hash % 3)
        {
            case 0:
                return NeonCyanTint;

            case 1:
                return NeonCoralTint;

            default:
                return NeonVioletTint;
        }
    }

    private void ApplyGroundObstacle(SpriteRenderer baseRenderer)
    {
        bool useMetalBarrier = UseMetalBarrier(baseRenderer.gameObject.name);
        string resourcePath = useMetalBarrier
            ? "Graphics/Obstacles/metal_platform"
            : "Graphics/Obstacles/caution_block";
        Sprite sprite = LoadSprite(resourcePath);
        if (sprite == null)
        {
            return;
        }

        Vector2 targetSize = GetTargetSize(baseRenderer);
        Transform root = PrepareVisualRoot(baseRenderer);
        float worldHeight = useMetalBarrier ? targetSize.y * 0.95f : targetSize.y * 0.92f;
        float yOffset = useMetalBarrier ? 0.08f : 0f;
        CreateSpriteChild(
            root,
            "Main",
            sprite,
            baseRenderer,
            new Vector3(0f, yOffset, 0f),
            new Vector2(targetSize.x, worldHeight),
            1);
        baseRenderer.enabled = false;
    }

    private void ApplyGroundHazard(SpriteRenderer baseRenderer, bool withWire)
    {
        Vector2 targetSize = GetTargetSize(baseRenderer);
        Transform root = PrepareVisualRoot(baseRenderer);
        if (withWire)
        {
            CreateWireHazardCluster(root, baseRenderer, targetSize);
        }
        else
        {
            CreateSingleMarker(root, baseRenderer, targetSize * new Vector2(0.4f, 1f));
        }

        baseRenderer.enabled = false;
    }

    private void CreateWireHazardCluster(Transform root, SpriteRenderer baseRenderer, Vector2 targetSize)
    {
        Sprite rubbleSprite = LoadSprite("Graphics/Obstacles/rubble_hazard");
        float clusterWidth = Mathf.Clamp(targetSize.x * 0.32f, 4f, 12f);

        if (rubbleSprite != null)
        {
        CreateSpriteChild(
            root,
            "RubbleBase",
            rubbleSprite,
            baseRenderer,
            new Vector3(0f, -0.07f, 0f),
            new Vector2(clusterWidth, targetSize.y * 0.42f),
            1,
            true);
        }

        CreateTiledWireStrip(root, baseRenderer, new Vector2(clusterWidth, targetSize.y));
        CreateHazardEndCaps(root, baseRenderer, new Vector2(clusterWidth, targetSize.y));
        CreateNeonHazardUnderline(root, baseRenderer, new Vector2(clusterWidth, targetSize.y));
    }

    private void RestoreBaseVisual(SpriteRenderer baseRenderer)
    {
        if (baseRenderer == null)
        {
            return;
        }

        baseRenderer.enabled = true;

        Transform artTransform = baseRenderer.transform.Find("_ArtVisual");
        if (artTransform == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(artTransform.gameObject);
        }
        else
        {
            DestroyImmediate(artTransform.gameObject);
        }
    }

    private void SyncGameplayColliderSafety(SpriteRenderer baseRenderer)
    {
        if (!NeedsVisibilityBoundCollider(baseRenderer.gameObject.name))
        {
            return;
        }

        BoxCollider2D boxCollider = baseRenderer.GetComponent<BoxCollider2D>();
        if (boxCollider == null)
        {
            return;
        }

        bool hasVisibleBase = baseRenderer.enabled && baseRenderer.color.a > 0.05f;
        bool hasVisibleArt = false;

        Transform artTransform = baseRenderer.transform.Find("_ArtVisual");
        if (artTransform != null)
        {
            SpriteRenderer[] artRenderers = artTransform.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (SpriteRenderer artRenderer in artRenderers)
            {
                if (artRenderer != null && artRenderer.enabled && artRenderer.color.a > 0.05f && artRenderer.sprite != null)
                {
                    hasVisibleArt = true;
                    break;
                }
            }
        }

        boxCollider.enabled = hasVisibleBase || hasVisibleArt;
    }

    private void EnsureContactDamageComponent(SpriteRenderer baseRenderer)
    {
        if (!NeedsContactDamage(baseRenderer.gameObject.name))
        {
            return;
        }

        if (baseRenderer.GetComponent<Obstacle>() == null)
        {
            baseRenderer.gameObject.AddComponent<Obstacle>();
        }
    }

    private static bool NeedsVisibilityBoundCollider(string objectName)
    {
        return objectName.StartsWith("Vending_")
            || objectName.StartsWith("VendingTop")
            || objectName.StartsWith("DebrisHazard_")
            || objectName.StartsWith("CeilingShard_")
            || objectName.StartsWith("GapStep_")
            || objectName.StartsWith("SkyDeck_")
            || objectName.StartsWith("GoalDeck")
            || objectName.StartsWith("FinalLift")
            || objectName == "BossPerch"
            || objectName == "Scaffold_Start";
    }

    private static bool NeedsContactDamage(string objectName)
    {
        return objectName.StartsWith("Vending_")
            || objectName.StartsWith("DebrisHazard_")
            || objectName.StartsWith("CeilingShard_");
    }

    private Vector2 GetTargetSize(SpriteRenderer baseRenderer)
    {
        BoxCollider2D boxCollider = baseRenderer.GetComponent<BoxCollider2D>();
        Vector2 targetSize = boxCollider != null ? boxCollider.size : baseRenderer.size;
        return targetSize.x > 0f && targetSize.y > 0f ? targetSize : Vector2.one;
    }

    private Transform PrepareVisualRoot(SpriteRenderer baseRenderer)
    {
        Transform root = baseRenderer.transform.Find("_ArtVisual");
        if (root == null)
        {
            root = new GameObject("_ArtVisual").transform;
            root.SetParent(baseRenderer.transform, false);
        }

        SpriteRenderer legacyRenderer = root.GetComponent<SpriteRenderer>();
        if (legacyRenderer != null)
        {
            if (Application.isPlaying)
            {
                Destroy(legacyRenderer);
            }
            else
            {
                DestroyImmediate(legacyRenderer);
            }
        }

        for (int i = root.childCount - 1; i >= 0; i--)
        {
            if (Application.isPlaying)
            {
                Destroy(root.GetChild(i).gameObject);
            }
            else
            {
                DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        root.localPosition = Vector3.zero;
        root.localRotation = Quaternion.identity;
        root.localScale = Vector3.one;
        return root;
    }

    private void CreateTiledWireStrip(Transform root, SpriteRenderer baseRenderer, Vector2 targetSize)
    {
        Sprite wireSprite = LoadSprite("Graphics/Obstacles/barbed_wire");
        if (wireSprite == null)
        {
            return;
        }

        float desiredHeight = targetSize.y * 0.92f;
        float aspect = wireSprite.bounds.size.x / Mathf.Max(0.001f, wireSprite.bounds.size.y);
        float segmentWidth = desiredHeight * aspect;
        float spanWidth = Mathf.Max(targetSize.x - 0.8f, 1f);
        int count = Mathf.Max(2, Mathf.CeilToInt(spanWidth / Mathf.Max(segmentWidth * 0.78f, 0.5f)));
        float step = count > 1 ? spanWidth / (count - 1) : 0f;
        float startX = -spanWidth * 0.5f;

        for (int i = 0; i < count; i++)
        {
            float x = startX + step * i;
            float y = 0.06f + ((i % 2 == 0) ? 0.02f : -0.01f);
            CreateSpriteChild(
                root,
                $"Wire_{i}",
                wireSprite,
                baseRenderer,
                new Vector3(x, y, 0f),
                new Vector2(segmentWidth, desiredHeight),
                3,
                true);
        }
    }

    private void CreateHazardEndCaps(Transform root, SpriteRenderer baseRenderer, Vector2 targetSize)
    {
        Sprite blockSprite = LoadSprite("Graphics/Obstacles/caution_block");
        if (blockSprite == null)
        {
            return;
        }

        float capHeight = targetSize.y * 0.78f;
        float aspect = blockSprite.bounds.size.x / Mathf.Max(0.001f, blockSprite.bounds.size.y);
        float capWidth = capHeight * aspect;
        float xOffset = Mathf.Max((targetSize.x * 0.5f) - (capWidth * 0.38f), 0.4f);

        CreateSpriteChild(
            root,
            "CapLeft",
            blockSprite,
            baseRenderer,
            new Vector3(-xOffset, 0.02f, 0f),
            new Vector2(capWidth, capHeight),
            4,
            true);

        CreateSpriteChild(
            root,
            "CapRight",
            blockSprite,
            baseRenderer,
            new Vector3(xOffset, 0.02f, 0f),
            new Vector2(capWidth, capHeight),
            4,
            true);
    }

    private void CreateSingleMarker(Transform root, SpriteRenderer baseRenderer, Vector2 targetSize)
    {
        Sprite blockSprite = LoadSprite("Graphics/Obstacles/caution_block");
        if (blockSprite == null)
        {
            return;
        }

        float markerHeight = targetSize.y * 0.72f;
        float aspect = blockSprite.bounds.size.x / Mathf.Max(0.001f, blockSprite.bounds.size.y);
        float markerWidth = markerHeight * aspect;
        CreateSpriteChild(
            root,
            "Marker",
            blockSprite,
            baseRenderer,
            new Vector3(0f, 0.04f, 0f),
            new Vector2(markerWidth, markerHeight),
            3,
            true);
        CreateNeonHazardUnderline(root, baseRenderer, new Vector2(markerWidth * 1.25f, markerHeight));
    }

    private void CreateNeonHazardUnderline(Transform root, SpriteRenderer baseRenderer, Vector2 targetSize)
    {
        Sprite sprite = GetSolidSprite();
        if (sprite == null)
        {
            return;
        }

        GameObject child = new GameObject("HazardGlow", typeof(SpriteRenderer));
        child.transform.SetParent(root, false);
        child.transform.localPosition = new Vector3(0f, -targetSize.y * 0.28f, 0f);
        child.transform.localScale = new Vector3(targetSize.x, Mathf.Max(targetSize.y * 0.12f, 0.05f), 1f);

        SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
        childRenderer.sprite = sprite;
        childRenderer.color = new Color32(255, 88, 75, 92);
        childRenderer.sortingLayerID = baseRenderer.sortingLayerID;
        childRenderer.sortingOrder = baseRenderer.sortingOrder + 2;
    }

    private void CreateSpriteChild(
        Transform root,
        string childName,
        Sprite sprite,
        SpriteRenderer baseRenderer,
        Vector3 localPosition,
        Vector2 worldSize,
        int sortingOffset,
        bool addTriggerCollider = false)
    {
        if (sprite == null)
        {
            return;
        }

        GameObject child = new GameObject(childName, typeof(SpriteRenderer));
        child.transform.SetParent(root, false);
        child.transform.localPosition = localPosition;
        child.transform.localRotation = Quaternion.identity;

        SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();
        childRenderer.sprite = sprite;
        childRenderer.color = GetChildTint(childName, baseRenderer.gameObject.name);
        childRenderer.sortingLayerID = baseRenderer.sortingLayerID;
        childRenderer.sortingOrder = baseRenderer.sortingOrder + sortingOffset;
        childRenderer.maskInteraction = baseRenderer.maskInteraction;
        childRenderer.sharedMaterial = baseRenderer.sharedMaterial;
        childRenderer.drawMode = SpriteDrawMode.Simple;

        if (addTriggerCollider)
        {
            BoxCollider2D childCollider = child.AddComponent<BoxCollider2D>();
            childCollider.isTrigger = true;
            childCollider.size = worldSize;
        }

        Vector2 spriteSize = new Vector2(sprite.bounds.size.x, sprite.bounds.size.y);
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            child.transform.localScale = Vector3.one;
            return;
        }

        child.transform.localScale = new Vector3(
            worldSize.x / spriteSize.x,
            worldSize.y / spriteSize.y,
            1f);
    }

    private static Color GetChildTint(string childName, string parentName)
    {
        if (childName.StartsWith("Wire"))
        {
            return new Color32(196, 209, 198, 255);
        }

        if (childName.StartsWith("Cap") || childName == "Marker")
        {
            return HazardTint;
        }

        if (childName == "RubbleBase")
        {
            return new Color32(79, 77, 72, 255);
        }

        if (parentName.StartsWith("Vending_"))
        {
            return new Color32(104, 126, 132, 255);
        }

        return Color.white;
    }

    private void EnsureGlowChild(SpriteRenderer source, Color glowColor, float scaleMultiplier, float alpha)
    {
        if (source == null || source.sprite == null)
        {
            return;
        }

        Transform glow = source.transform.Find("_NeonGlow");
        if (glow == null)
        {
            glow = new GameObject("_NeonGlow", typeof(SpriteRenderer)).transform;
            glow.SetParent(source.transform, false);
        }

        glow.localPosition = Vector3.zero;
        glow.localRotation = Quaternion.identity;
        glow.localScale = Vector3.one * scaleMultiplier;

        SpriteRenderer glowRenderer = glow.GetComponent<SpriteRenderer>();
        glowRenderer.sprite = source.sprite;
        glowRenderer.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
        glowRenderer.sortingLayerID = source.sortingLayerID;
        glowRenderer.sortingOrder = source.sortingOrder - 1;
        glowRenderer.maskInteraction = source.maskInteraction;
        glowRenderer.sharedMaterial = source.sharedMaterial;
    }

    private void EnsureStageAtmosphere(Bounds stageBounds)
    {
        Transform root = transform.Find("_NightAtmosphere");
        if (root == null)
        {
            root = new GameObject("_NightAtmosphere").transform;
            root.SetParent(transform, false);
        }

        float width = Mathf.Max(stageBounds.size.x + 24f, 48f);
        float centerX = stageBounds.center.x;
        float bottomY = stageBounds.min.y - 1f;
        float topY = stageBounds.max.y + 8f;

        CreateAtmosphereQuad(root, "SkyTop", new Vector3(centerX, topY - 2f, 8f), new Vector2(width, 12f), SkyTopTint, -1200);
        CreateAtmosphereQuad(root, "SkyMid", new Vector3(centerX, stageBounds.center.y + 3f, 8f), new Vector2(width, 10f), SkyMidTint, -1190);
        CreateAtmosphereQuad(root, "DistantCity", new Vector3(centerX, bottomY + 5.2f, 8f), new Vector2(width * 0.92f, 3.6f), DistantCityTint, -850);
        CreateAtmosphereQuad(root, "LowFog_A", new Vector3(centerX - width * 0.08f, bottomY + 1.4f, 8f), new Vector2(width * 0.72f, 1.2f), FogTint, 80);
        CreateAtmosphereQuad(root, "LowFog_B", new Vector3(centerX + width * 0.14f, bottomY + 2.4f, 8f), new Vector2(width * 0.58f, 1.0f), new Color32(92, 74, 112, 28), 82);
    }

    private void CreateAtmosphereQuad(Transform root, string objectName, Vector3 position, Vector2 size, Color color, int sortingOrder)
    {
        Sprite sprite = GetSolidSprite();
        if (sprite == null || root == null)
        {
            return;
        }

        Transform existing = root.Find(objectName);
        GameObject quadObject;
        if (existing == null)
        {
            quadObject = new GameObject(objectName, typeof(SpriteRenderer));
            quadObject.transform.SetParent(root, false);
        }
        else
        {
            quadObject = existing.gameObject;
        }

        quadObject.transform.position = position;
        quadObject.transform.localRotation = Quaternion.identity;
        quadObject.transform.localScale = new Vector3(size.x, size.y, 1f);

        SpriteRenderer renderer = quadObject.GetComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
    }

    private void ApplyCameraMood()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        mainCamera.backgroundColor = new Color32(6, 10, 19, 255);
    }

    private Sprite GetSolidSprite()
    {
        const string key = "__solid_pixel";
        if (runtimeSprites.TryGetValue(key, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.hideFlags = HideFlags.HideAndDontSave;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        sprite.hideFlags = HideFlags.HideAndDontSave;
        runtimeSprites[key] = sprite;
        return sprite;
    }

    private Sprite LoadSprite(string resourcePath)
    {
        if (runtimeSprites.TryGetValue(resourcePath, out Sprite cachedSprite) && cachedSprite != null)
        {
            return cachedSprite;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            Sprite[] sprites = Resources.LoadAll<Sprite>(resourcePath);
            float bestArea = 0f;

            foreach (Sprite candidate in sprites)
            {
                if (candidate == null)
                {
                    continue;
                }

                float candidateArea = candidate.rect.width * candidate.rect.height;
                if (candidateArea > bestArea)
                {
                    bestArea = candidateArea;
                    sprite = candidate;
                }
            }
        }

        if (sprite == null)
        {
            Texture2D texture = Resources.Load<Texture2D>(resourcePath);
            if (texture != null)
            {
                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }
        }

        if (sprite != null)
        {
            runtimeSprites[resourcePath] = sprite;
        }

        return sprite;
    }
}
