using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class Day2StageBuilder : MonoBehaviour
{
    private const float StageStartX = 0.8f;
    private const float Day2StageEndX = 136f;
    private const float Day3StageEndX = 138f;

    private struct PlatformSpec
    {
        public Vector2 Position;
        public Vector2 Size;
        public int ArtIndex;
        public float Rotation;

        public PlatformSpec(float x, float y, float width, int artIndex, float rotation = 0f)
        {
            Position = new Vector2(x, y);
            Size = new Vector2(width, 0.45f);
            ArtIndex = artIndex;
            Rotation = rotation;
        }
    }

    private static readonly PlatformSpec[] Stage2Platforms =
    {
        // Act 1: dash gap, then a grapple-only climb.
        new PlatformSpec(3.5f, -3.2f, 4.0f, 0),
        new PlatformSpec(13.2f, -3.0f, 3.4f, 1),
        new PlatformSpec(18.2f, -1.5f, 3.6f, 2),
        new PlatformSpec(25.2f, 2.1f, 4.0f, 3),
        new PlatformSpec(31.2f, 0.5f, 3.8f, 4),
        new PlatformSpec(36.5f, -1.0f, 3.6f, 5),
        new PlatformSpec(45.5f, -0.9f, 3.6f, 0),

        // Act 2: grapple ascent followed by a committed air dash.
        new PlatformSpec(50.5f, -2.5f, 4.0f, 1),
        new PlatformSpec(57.5f, 1.4f, 4.0f, 2),
        new PlatformSpec(63.5f, -0.1f, 3.8f, 3),
        new PlatformSpec(72.8f, 0.0f, 3.6f, 4),
        new PlatformSpec(78f, -1.5f, 4.0f, 5),
        new PlatformSpec(85f, 2.2f, 4.0f, 0),
        new PlatformSpec(91f, 0.6f, 3.8f, 1),

        // Act 3: dash, grapple, then a final dash to the goal.
        new PlatformSpec(96f, -2.4f, 4.2f, 2),
        new PlatformSpec(105.5f, -2.2f, 3.6f, 3),
        new PlatformSpec(113f, 1.8f, 4.0f, 4),
        new PlatformSpec(119f, 0.3f, 3.8f, 5),
        new PlatformSpec(128.5f, 0.4f, 3.6f, 0),
        new PlatformSpec(133f, -2.7f, 4.8f, 1),
        new PlatformSpec(114f, 4.6f, 48f, 1)
    };

    private static readonly Vector2[] WirePoints =
    {
        new Vector2(22.2f, 3.2f),
        new Vector2(54.3f, 2.9f),
        new Vector2(81.8f, 3.5f),
        new Vector2(109.2f, 3.1f)
    };

    private static readonly Vector2[] GroundHazards =
    {
        new Vector2(9f, 9.5f),
        new Vector2(21.5f, 13f),
        new Vector2(36f, 12f),
        new Vector2(52f, 14f),
        new Vector2(69f, 16f),
        new Vector2(86f, 15f),
        new Vector2(103f, 17f),
        new Vector2(121f, 17f),
        new Vector2(132f, 5f)
    };

    private static readonly Vector2[] Checkpoints =
    {
        new Vector2(45.5f, 0.1f),
        new Vector2(91f, 1.6f)
    };

    private static readonly PlatformSpec[] Stage3Platforms =
    {
        // Reference 1: split upper and lower routes joined by steep ramps.
        new PlatformSpec(3f, -3.2f, 5.2f, 0),
        new PlatformSpec(8.2f, -2.35f, 5.2f, 1, 25f),
        new PlatformSpec(15f, -1.75f, 8.2f, 2),
        new PlatformSpec(21.3f, -3.05f, 3.4f, 3, -66f),
        new PlatformSpec(27f, -3.65f, 7.2f, 4),
        new PlatformSpec(34f, -2.55f, 7.6f, 5, 29f),
        new PlatformSpec(41f, -1.45f, 7.0f, 0),
        new PlatformSpec(9.5f, 2.55f, 5.6f, 1),
        new PlatformSpec(19f, 2.0f, 5.2f, 2),
        new PlatformSpec(24f, 0.65f, 5.2f, 3, 36f),
        new PlatformSpec(35f, 0.15f, 3.2f, 4),
        new PlatformSpec(39.5f, 1.55f, 4.6f, 5),

        // Reference 2: alternating ramps with deliberate dash gaps.
        new PlatformSpec(47f, -3.45f, 5.2f, 0),
        new PlatformSpec(53f, -2.55f, 5.4f, 1, 29f),
        new PlatformSpec(60.5f, -1.75f, 7.4f, 2),
        new PlatformSpec(66.5f, -0.05f, 3.4f, 3, 58f),
        new PlatformSpec(72f, -0.25f, 4.8f, 4),
        new PlatformSpec(80f, 1.4f, 7.0f, 5),
        new PlatformSpec(88f, -0.35f, 4.2f, 0),
        new PlatformSpec(92f, -2.65f, 3.2f, 1),

        // Reference 3: long descending entry, central climb, terminal finish.
        new PlatformSpec(96f, -3.25f, 5.4f, 2),
        new PlatformSpec(102f, -2.45f, 5.4f, 3, 24f),
        new PlatformSpec(109f, -1.65f, 7.0f, 4),
        new PlatformSpec(115f, -0.55f, 5.2f, 5, 24f),
        new PlatformSpec(120.5f, -0.2f, 3.2f, 0),
        new PlatformSpec(126f, 1.15f, 5.0f, 1, 34f),
        new PlatformSpec(133.5f, 1.85f, 7.0f, 2)
    };

    private static readonly Vector2[] Day3WirePoints =
    {
        new Vector2(11f, 0.8f),
        new Vector2(22.5f, 3.1f),
        new Vector2(37f, 2.6f),
        new Vector2(55f, 1.0f),
        new Vector2(68f, 3.0f),
        new Vector2(82f, 3.6f),
        new Vector2(106f, 1.3f),
        new Vector2(123f, 3.0f)
    };

    private static readonly Vector2[] Day3GroundHazards =
    {
        new Vector2(8f, 12f),
        new Vector2(23f, 16f),
        new Vector2(40f, 14f),
        new Vector2(56f, 16f),
        new Vector2(73f, 16f),
        new Vector2(90f, 15f),
        new Vector2(106f, 16f),
        new Vector2(123f, 17f),
        new Vector2(135f, 5f)
    };

    private static readonly Vector2[] Day3Checkpoints =
    {
        new Vector2(41f, -0.4f),
        new Vector2(88f, 0.65f)
    };

    private readonly List<Sprite> platformSprites = new List<Sprite>();
    private static readonly float[] PlatformSurfaceOffsets =
    {
        -0.30f,
        -0.25f,
        -0.07f,
        -0.07f,
        -0.34f,
        0.05f
    };

    private Sprite solidSprite;
    private bool stageBuilt;
    private bool isDayThree;

    private float StageEndX => isDayThree ? Day3StageEndX : Day2StageEndX;

    private void Awake()
    {
        EnsureStageBuilt();
    }

    private void Start()
    {
        EnsureStageBuilt();
    }

    private void EnsureStageBuilt()
    {
        if (stageBuilt || GameObject.Find("NightStageGround") != null)
        {
            stageBuilt = true;
            return;
        }

        string sceneName = gameObject.scene.name;
        if (sceneName != "Stage02_1" && sceneName != "Stage03_1")
        {
            return;
        }

        isDayThree = sceneName == "Stage03_1";
        stageBuilt = true;
        StageArtApplier artApplier = GetComponent<StageArtApplier>();
        if (artApplier != null)
        {
            artApplier.enabled = false;
        }

        RemoveLegacyStage();
        LoadSprites();
        BuildStage();
        StartCoroutine(SnapPlayerToStartAfterCleanup());
    }

    private void RemoveLegacyStage()
    {
        string[] rootNames =
        {
            "StageGeometry",
            "BackgroundArt",
            "Hazards",
            "GameplayMarkers",
            "GrappleAnchors"
        };

        foreach (string rootName in rootNames)
        {
            GameObject root = GameObject.Find(rootName);
            if (root != null)
            {
                root.SetActive(false);
                Destroy(root);
            }
        }
    }

    private void LoadSprites()
    {
        string[] paths =
        {
            "Graphics/FloatingPlatforms/floating_platform_cracked_a",
            "Graphics/FloatingPlatforms/floating_platform_cracked_b",
            "Graphics/FloatingPlatforms/floating_platform_cracked_c",
            "Graphics/FloatingPlatforms/floating_platform_debris",
            "Graphics/FloatingPlatforms/floating_platform_stone",
            "Graphics/FloatingPlatforms/floating_platform_barrel"
        };

        platformSprites.Clear();
        foreach (string path in paths)
        {
            platformSprites.Add(Resources.Load<Sprite>(path));
        }
    }

    private void BuildStage()
    {
        ConfigurePlayerAndCamera();
        CreateBackground();

        Transform stageRoot = new GameObject("Day2StageGeometry").transform;
        CreateGround(stageRoot);

        PlatformSpec[] platforms = isDayThree ? Stage3Platforms : Stage2Platforms;
        for (int index = 0; index < platforms.Length; index++)
        {
            CreatePlatform(stageRoot, platforms[index], index);
        }

        Transform hazardRoot = new GameObject("Day2Hazards").transform;
        Vector2[] hazards = isDayThree ? Day3GroundHazards : GroundHazards;
        for (int index = 0; index < hazards.Length; index++)
        {
            CreateBarbedWire(hazardRoot, hazards[index], index);
        }

        Transform markerRoot = new GameObject("Day2GameplayMarkers").transform;
        Vector2[] wirePoints = isDayThree ? Day3WirePoints : WirePoints;
        for (int index = 0; index < wirePoints.Length; index++)
        {
            CreateInvisibleWirePoint(markerRoot, wirePoints[index], index);
        }

        Vector2[] checkpoints = isDayThree ? Day3Checkpoints : Checkpoints;
        for (int index = 0; index < checkpoints.Length; index++)
        {
            CreateCheckpoint(markerRoot, checkpoints[index], index);
        }

        CreateGoal(
            markerRoot,
            isDayThree ? new Vector2(133.5f, 2.85f) : new Vector2(133f, -1.78f));
        CreateBoundary(stageRoot, new Vector2(-1.5f, 0f));
        CreateBoundary(stageRoot, new Vector2(StageEndX + 1.5f, 0f));
        CreateStageLabel();
    }

    private void ConfigurePlayerAndCamera()
    {
        PlayerController player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = new Vector2(StageStartX, -4.72f);
            }

            player.transform.position = new Vector3(StageStartX, -4.72f, 0f);
            player.transform.localScale = Vector3.one;

            SpriteRenderer playerRenderer = player.GetComponentInChildren<SpriteRenderer>(true);
            if (playerRenderer != null)
            {
                playerRenderer.enabled = true;
                playerRenderer.color = Color.white;
                playerRenderer.sortingOrder = 30;
            }

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.enabled = true;
                animator.speed = 1f;
            }
        }

        GameObject respawn = GameObject.Find("RespawnPoint");
        if (respawn != null)
        {
            respawn.transform.position = new Vector3(StageStartX, -4.72f, 0f);
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        CameraFollow follow = mainCamera.GetComponent<CameraFollow>();
        if (follow != null)
        {
            follow.enabled = true;
            follow.ConfigureBounds(
                new Vector2(9.2f, -0.55f),
                new Vector2(StageEndX - 9.2f, -0.55f));
        }

        mainCamera.orthographic = true;
        mainCamera.orthographicSize = 5.4f;
        mainCamera.transform.position = new Vector3(9.2f, -0.55f, -10f);
        mainCamera.backgroundColor = new Color32(5, 12, 23, 255);
    }

    private IEnumerator SnapPlayerToStartAfterCleanup()
    {
        yield return null;
        yield return new WaitForFixedUpdate();

        PlayerController player = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (player != null)
        {
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
                body.position = new Vector2(StageStartX, -4.72f);
            }

            player.transform.position = new Vector3(StageStartX, -4.72f, 0f);
        }

        Physics2D.SyncTransforms();
        if (player != null)
        {
            player.SendMessage("UpdateGroundedState", SendMessageOptions.DontRequireReceiver);

            Animator animator = player.GetComponentInChildren<Animator>(true);
            if (animator != null)
            {
                animator.Rebind();
                animator.SetBool("isRun", false);
                animator.SetBool("isJump", false);
                animator.Update(0f);
            }
        }
    }

    private void CreateBackground()
    {
        Sprite background = Resources.Load<Sprite>("Graphics/Day2/day2_city_background");
        GameObject root = new GameObject("Day2Background");
        if (background != null)
        {
            const float backgroundHeight = 11.8f;
            float segmentWidth = backgroundHeight
                * background.bounds.size.x
                / Mathf.Max(background.bounds.size.y, 0.001f);
            int segmentCount = Mathf.CeilToInt((StageEndX + 18f) / segmentWidth);

            for (int index = 0; index < segmentCount; index++)
            {
                GameObject segment = new GameObject($"Background_{index + 1}", typeof(SpriteRenderer));
                segment.transform.SetParent(root.transform, false);
                segment.transform.position = new Vector3(
                    (segmentWidth * 0.5f) + (segmentWidth * index) - 8f,
                    0.15f,
                    5f);

                SpriteRenderer renderer = segment.GetComponent<SpriteRenderer>();
                renderer.sprite = background;
                renderer.color = new Color32(205, 215, 225, 255);
                renderer.sortingOrder = -100;
                ScaleSprite(segment.transform, background, new Vector2(segmentWidth, backgroundHeight));
            }
        }
        else
        {
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = GetSolidSprite();
            renderer.color = new Color32(9, 22, 39, 255);
            root.transform.localScale = new Vector3(StageEndX + 20f, 13.6f, 1f);
            renderer.sortingOrder = -100;
            root.transform.position = new Vector3(StageEndX * 0.5f, 0.2f, 5f);
        }
    }

    private void CreateGround(Transform parent)
    {
        GameObject ground = new GameObject("NightStageGround", typeof(BoxCollider2D));
        ground.transform.SetParent(parent, false);
        ground.layer = ResolveGroundLayer();
        ground.transform.position = new Vector3(StageEndX * 0.5f, -5.35f, 0f);

        BoxCollider2D collider = ground.GetComponent<BoxCollider2D>();
        collider.size = new Vector2(StageEndX + 4f, 1.25f);

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(ground.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        Sprite groundSprite = Resources.Load<Sprite>("Graphics/Ground/night_road_ground");
        if (groundSprite != null)
        {
            Destroy(visual);
            const float segmentHeight = 2.4f;
            const float surfaceRatio = 0.13f;
            float segmentWidth = segmentHeight
                * groundSprite.bounds.size.x
                / Mathf.Max(groundSprite.bounds.size.y, 0.001f);
            int segmentCount = Mathf.CeilToInt((StageEndX + 4f) / segmentWidth);
            float coveredWidth = segmentCount * segmentWidth;
            float startX = -(coveredWidth * 0.5f) + (segmentWidth * 0.5f);
            float colliderTop = collider.size.y * 0.5f;
            float visualCenterY = colliderTop + ((surfaceRatio - 0.5f) * segmentHeight);

            for (int index = 0; index < segmentCount; index++)
            {
                GameObject segment = new GameObject($"Road_{index + 1}", typeof(SpriteRenderer));
                segment.transform.SetParent(ground.transform, false);
                segment.transform.localPosition = new Vector3(
                    startX + (segmentWidth * index),
                    visualCenterY,
                    0f);

                SpriteRenderer segmentRenderer = segment.GetComponent<SpriteRenderer>();
                segmentRenderer.sprite = groundSprite;
                segmentRenderer.sortingOrder = 4;
                ScaleSprite(segment.transform, groundSprite, new Vector2(segmentWidth, segmentHeight));
            }
        }
        else
        {
            renderer.sprite = GetSolidSprite();
            renderer.color = new Color32(66, 75, 82, 255);
            renderer.sortingOrder = 4;
            visual.transform.localScale = new Vector3(StageEndX + 4f, 1.25f, 1f);
        }
    }

    private void CreatePlatform(Transform parent, PlatformSpec spec, int index)
    {
        GameObject platform = new GameObject($"Day2Platform_{index + 1}", typeof(BoxCollider2D));
        platform.transform.SetParent(parent, false);
        platform.layer = ResolveGroundLayer();
        platform.transform.position = spec.Position;
        platform.transform.rotation = Quaternion.Euler(0f, 0f, spec.Rotation);

        BoxCollider2D collider = platform.GetComponent<BoxCollider2D>();
        Vector2 expandedSize = new Vector2(spec.Size.x * 1.15f, spec.Size.y);
        collider.size = expandedSize;
        collider.offset = new Vector2(0f, -expandedSize.y * 0.5f);

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(platform.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        Sprite sprite = platformSprites.Count > 0
            ? platformSprites[Mathf.Abs(spec.ArtIndex) % platformSprites.Count]
            : null;

        if (sprite != null)
        {
            renderer.sprite = sprite;
            ScaleSprite(visual.transform, sprite, new Vector2(expandedSize.x, 0.85f));
            int spriteIndex = Mathf.Abs(spec.ArtIndex) % platformSprites.Count;
            visual.transform.localPosition = new Vector3(
                0f,
                PlatformSurfaceOffsets[spriteIndex],
                0f);
        }
        else
        {
            renderer.sprite = GetSolidSprite();
            renderer.color = new Color32(75, 82, 89, 255);
            visual.transform.localScale = new Vector3(expandedSize.x, expandedSize.y, 1f);
        }

        renderer.sortingOrder = 5;
    }

    private void CreateGoal(Transform parent, Vector2 position)
    {
        GameObject goal = new GameObject(
            isDayThree ? "Day3ClearTerminal" : "Day2FoodCrate",
            typeof(BoxCollider2D),
            typeof(StageClear));
        goal.transform.SetParent(parent, false);
        goal.transform.position = position;

        GameObject visual = new GameObject("Visual", typeof(SpriteRenderer));
        visual.transform.SetParent(goal.transform, false);
        SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
        Sprite sprite = Resources.Load<Sprite>(
            isDayThree
                ? "Graphics/Goal/clear_terminal_gate"
                : "Graphics/Goal/day2_food_crate");
        renderer.sprite = sprite != null ? sprite : GetSolidSprite();
        renderer.color = sprite != null ? Color.white : new Color32(158, 133, 76, 255);
        renderer.sortingOrder = 10;

        if (sprite != null)
        {
            ScaleSprite(
                visual.transform,
                sprite,
                isDayThree ? new Vector2(3.1f, 2.0f) : new Vector2(2.8f, 2.8f));
        }
        else
        {
            visual.transform.localScale = new Vector3(1.2f, 1.2f, 1f);
        }

        BoxCollider2D collider = goal.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = isDayThree
            ? new Vector2(1.15f, 1.55f)
            : new Vector2(1.15f, 0.95f);
        collider.offset = isDayThree
            ? new Vector2(-0.7f, 0f)
            : new Vector2(0f, -0.05f);
    }

    private void CreateBarbedWire(Transform parent, Vector2 spec, int index)
    {
        float width = spec.y;
        GameObject hazard = new GameObject(
            $"Day2BarbedWire_{index + 1}",
            typeof(BoxCollider2D),
            typeof(Obstacle));
        hazard.transform.SetParent(parent, false);
        hazard.transform.position = new Vector3(spec.x, -4.18f, 0f);

        BoxCollider2D collider = hazard.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(width, 0.72f);
        collider.offset = new Vector2(0f, -0.04f);

        Sprite sprite = Resources.Load<Sprite>("Graphics/Obstacles/barbed_wire");
        int segmentCount = Mathf.Max(1, Mathf.CeilToInt(width / 1.35f));
        float segmentWidth = width / segmentCount;
        float startX = -(width * 0.5f) + (segmentWidth * 0.5f);

        for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
        {
            GameObject visual = new GameObject($"Wire_{segmentIndex + 1}", typeof(SpriteRenderer));
            visual.transform.SetParent(hazard.transform, false);
            visual.transform.localPosition = new Vector3(startX + (segmentWidth * segmentIndex), 0f, 0f);

            SpriteRenderer renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite != null ? sprite : GetSolidSprite();
            renderer.color = sprite != null ? Color.white : new Color32(190, 170, 70, 255);
            renderer.sortingOrder = 8;
            ScaleSprite(visual.transform, renderer.sprite, new Vector2(segmentWidth * 1.08f, 1.05f));
        }
    }

    private static void CreateInvisibleWirePoint(Transform parent, Vector2 position, int index)
    {
        GameObject wirePoint = new GameObject($"Day2WirePoint_{index + 1}", typeof(GrappleAnchor));
        wirePoint.transform.SetParent(parent, false);
        wirePoint.transform.position = position;
    }

    private static void CreateCheckpoint(Transform parent, Vector2 position, int index)
    {
        GameObject checkpoint = new GameObject(
            $"Day2Checkpoint_{index + 1}",
            typeof(BoxCollider2D),
            typeof(Checkpoint));
        checkpoint.transform.SetParent(parent, false);
        checkpoint.transform.position = position;

        BoxCollider2D collider = checkpoint.GetComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(2.4f, 2.2f);
    }

    private void CreateBoundary(Transform parent, Vector2 position)
    {
        GameObject boundary = new GameObject("StageBoundary", typeof(BoxCollider2D));
        boundary.transform.SetParent(parent, false);
        boundary.transform.position = position;
        boundary.GetComponent<BoxCollider2D>().size = new Vector2(0.5f, 14f);
    }

    private void CreateStageLabel()
    {
        HudController hud = FindFirstObjectByType<HudController>(FindObjectsInactive.Include);
        if (hud == null)
        {
            return;
        }

        GameObject label = new GameObject("Day2StageLabel", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Text));
        label.transform.SetParent(hud.transform, false);

        UnityEngine.UI.Text text = label.GetComponent<UnityEngine.UI.Text>();
        text.text = isDayThree ? "DAY 3  /  STAGE 3" : "DAY 2  /  STAGE 2";
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 18;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color32(164, 224, 218, 230);
        text.raycastTarget = false;

        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.42f, 0.865f);
        rect.anchorMax = new Vector2(0.58f, 0.91f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private Sprite GetSolidSprite()
    {
        if (solidSprite != null)
        {
            return solidSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        texture.name = "Day2SolidTexture";
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        solidSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        solidSprite.name = "Day2SolidSprite";
        return solidSprite;
    }

    private static void ScaleSprite(Transform target, Sprite sprite, Vector2 worldSize)
    {
        Vector2 spriteSize = sprite.bounds.size;
        target.localScale = new Vector3(
            worldSize.x / Mathf.Max(spriteSize.x, 0.001f),
            worldSize.y / Mathf.Max(spriteSize.y, 0.001f),
            1f);
    }

    private static int ResolveGroundLayer()
    {
        int layer = LayerMask.NameToLayer("Ground");
        return layer >= 0 ? layer : 3;
    }
}
