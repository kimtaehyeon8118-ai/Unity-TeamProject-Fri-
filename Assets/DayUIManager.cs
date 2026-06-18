using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

public class DayUIManager : MonoBehaviour
{
    // Designer layout mode keeps KitchenPanel and ResultPanel object placement editable in the scene.
    private enum MenuId
    {
        None,
        DoenjangJjigae,
        KimchiJjigae,
        SoondubuJjigae
    }

    private enum CustomerPreference
    {
        Unknown,
        MildSoup,
        SpicySoup
    }

    private enum EvaluationGrade
    {
        Poor,
        Okay,
        Good,
        Perfect
    }

    private enum CookingGaugeResult
    {
        Low,
        Good,
        Overheated
    }

    private enum CookedStewId
    {
        None,
        KimchiJjigae,
        DoenjangJjigae,
        SoondubuJjigae
    }

    [Serializable]
    public class DialogueLine
    {
        public bool isCustomer;
        public bool isNarration;

        [TextArea]
        public string text;
    }

    private sealed class RecipeDefinition
    {
        public RecipeDefinition(
            MenuId id,
            string displayName,
            string description,
            string[] ingredientOptions,
            string[] requiredIngredients,
            string[] preferredTags,
            string[] riskyTags)
        {
            Id = id;
            DisplayName = displayName;
            Description = description;
            IngredientOptions = ingredientOptions;
            RequiredIngredients = requiredIngredients;
            PreferredTags = preferredTags;
            RiskyTags = riskyTags;
        }

        public MenuId Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        public string[] IngredientOptions { get; }
        public string[] RequiredIngredients { get; }
        public string[] PreferredTags { get; }
        public string[] RiskyTags { get; }
    }

    private sealed class EvaluationResult
    {
        public EvaluationResult(EvaluationGrade grade, int score, string reaction, string clue)
        {
            Grade = grade;
            Score = score;
            Reaction = reaction;
            Clue = clue;
        }

        public EvaluationGrade Grade { get; }
        public int Score { get; }
        public string Reaction { get; }
        public string Clue { get; }
    }

    private sealed class CookingResultData
    {
        public CookedStewId StewId;
        public string StewName;
        public bool HasOptionalIngredient;
        public EvaluationGrade Grade;
        public Sprite ResultSprite;
        public string Comment;
    }

    private sealed class DraggableIngredientUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private DayUIManager owner;
        private RectTransform rectTransform;
        private CanvasGroup canvasGroup;
        private Canvas rootCanvas;
        private Transform originalParent;
        private Vector2 originalAnchoredPosition;
        private int ingredientIndex;

        public void Initialize(DayUIManager manager, int index, Canvas canvas)
        {
            owner = manager;
            ingredientIndex = index;
            rootCanvas = canvas;
            rectTransform = GetComponent<RectTransform>();
            canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (owner == null || !owner.CanStartIngredientDrag(ingredientIndex))
                return;

            originalParent = transform.parent;
            originalAnchoredPosition = rectTransform.anchoredPosition;

            if (rootCanvas != null)
                transform.SetParent(rootCanvas.transform, false);

            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.75f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (owner == null || !owner.CanStartIngredientDrag(ingredientIndex))
                return;

            rectTransform.position = eventData.position;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (owner == null)
                return;

            bool dropped = owner.TryDropIngredientIntoPot(ingredientIndex, eventData);

            if (originalParent != null)
                transform.SetParent(originalParent, false);

            rectTransform.anchoredPosition = originalAnchoredPosition;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;

            if (!dropped)
                owner.ShowPotHint("재료를 뚝배기 안에 넣어주세요.");
        }
    }

    [Header("Panels")]
    public GameObject customerPanel;
    public GameObject kitchenPanel;
    public GameObject resultPanel;
    private GameObject resultDimOverlay;

    [Header("Customer UI")]
    public Image portraitImage;
    public TMP_Text nameText;
    public TMP_Text dialogueText;
    public TMP_Text customerSpeechText;
    public TMP_Text customerInfoText;

    public GameObject choiceGroup;
    public Button choiceButtonA;
    public Button choiceButtonB;
    public TMP_Text choiceButtonAText;
    public TMP_Text choiceButtonBText;
    public Button nextButton;
    public Button goKitchenButton;

    [Header("Menu Board UI")]
    public TMP_Text menuListText;
    public GameObject menuBoardPanel;
    public Button menuOpenButton;
    public Button closeButton;

    public Button menuButtonBibimbap;
    public Button menuButtonKimchiJjigae;
    public Button menuButtonJeyuk;

    public TMP_Text recipeTitleText;
    public TMP_Text recipeDetailText;

    [Header("Kitchen UI")]
    public TMP_Text selectedRecipeText;

    public Button recipeButton1;
    public Button recipeButton2;
    public Button recipeButton3;
    public TMP_Text recipeButton1Text;
    public TMP_Text recipeButton2Text;
    public TMP_Text recipeButton3Text;

    public Image selectedMenuImage;
    public TMP_Text ingredientGuideText;

    public Button ingredientButton1;
    public Button ingredientButton2;
    public Button ingredientButton3;
    public Button ingredientButton4;

    public TMP_Text ingredientButton1Text;
    public TMP_Text ingredientButton2Text;
    public TMP_Text ingredientButton3Text;
    public TMP_Text ingredientButton4Text;

    public TMP_Text slot1Text;
    public TMP_Text slot2Text;
    public TMP_Text slot3Text;

    public Button cookButton;
    public Button backButton;

    [Header("Drag Cooking UI")]
    public RectTransform cookingPotDropZone;
    public Image cookingPotImage;
    public TMP_Text cookingPotHintText;
    [SerializeField] private ButtonMashingGauge buttonMashingGauge;

    [Header("Result UI")]
    public Image foodImage;
    public TMP_Text resultText;
    public TMP_Text reactionText;
    public TMP_Text clueText;
    public Button nextDayButton;

    public TMP_Text unlockTitleText;
    [Header("Scene Flow")]
    [SerializeField] private string nightSceneName = "Stage01_CyberStreet";
    [SerializeField] private string dayTwoNightSceneName = "Stage02_1";
    [SerializeField] private string dayThreeNightSceneName = "Stage03_1";

    [Header("Editor Preview")]
    [SerializeField] private bool startFromPreviewDayInEditor = false;
    [SerializeField, Range(1, 3)] private int editorPreviewDayNumber = 1;

    [Header("Day Art Layout")]
    [SerializeField] private bool useDayArtLayout = true;
    [SerializeField] private DayResponseArtView dayResponseArtView;

    [Header("Designer Static Layout")]
    [SerializeField] private bool useStaticDesignerLayout = true;
    [SerializeField] private bool applyStaticNpcPlacement = true;
    [SerializeField] private Vector2 staticNpcAnchorMin = new Vector2(0.270f, 0.145f);
    [SerializeField] private Vector2 staticNpcAnchorMax = new Vector2(0.600f, 0.835f);
    [SerializeField, Range(-0.2f, 0.2f)] private float staticNpcCenterOffsetX = 0.060f;

    [Header("Kitchen Text Readability")]
    [SerializeField] private bool enhanceKitchenWorldTextReadability = true;
    [SerializeField] private Color kitchenWorldTextColor = new Color32(248, 232, 199, 255);
    [SerializeField] private Color kitchenWorldTextOutlineColor = new Color32(49, 30, 19, 255);
    [SerializeField, Range(0f, 0.5f)] private float kitchenWorldTextOutlineWidth = 0.16f;

    [Header("Kitchen Side Button Labels")]
    [SerializeField] private Color kitchenSideButtonLabelColor = new Color32(246, 236, 218, 255);
    [SerializeField] private Color kitchenSideButtonLabelOutlineColor = new Color32(34, 27, 31, 255);
    [SerializeField, Range(0f, 0.5f)] private float kitchenSideButtonLabelOutlineWidth = 0.18f;
    [SerializeField] private Color kitchenCameraBackgroundColor = new Color32(24, 21, 24, 255);

    [Header("Test Data")]
    public Sprite customerPortrait;
    public Sprite dayOneCustomerPortrait;
    public Sprite dayTwoCustomerPortrait;
    public Sprite dayThreeCustomerPortrait;
    public Sprite kitchenBackgroundSprite;
    public Sprite emptyCookingPotSprite;
    public Sprite cookButtonSprite;
    public Sprite ingredientItemSprite;
    public Sprite dayOptionButtonSprite;
    public Sprite dayMenuButtonSprite;
    public Sprite dayNoteButtonSprite;
    public Sprite resultUiSprite;
    public Sprite resultOkUiSprite;
    public Sprite kitchenIngredientPanelSprite;
    public Sprite kitchenSlotPanelSprite;
    public Sprite[] ingredientSprites;
    public Sprite bibimbapSprite;
    public Sprite kimchiJjigaeSprite;
    public Sprite jeyukSprite;

    [Header("Cooked Stew Result Sprites")]
    public Sprite kimchiStewSprite;
    public Sprite tofuKimchiStewSprite;
    public Sprite doenjangStewSprite;
    public Sprite pumpkinDoenjangStewSprite;
    public Sprite soondubuStewSprite;
    public Sprite shellSoondubuStewSprite;

    [Header("Designer Sprite Overrides")]
    public Sprite customerPanelSpriteOverride;
    public Sprite kitchenPanelSpriteOverride;
    public Sprite resultPanelSpriteOverride;
    public Sprite menuBoardPanelSpriteOverride;
    public Sprite portraitPanelSpriteOverride;
    public Sprite customerSpeechPanelSpriteOverride;
    public Sprite dialogueBoxSpriteOverride;
    public Sprite bottomPanelSpriteOverride;
    public Sprite cookingPotSpriteOverride;
    public Sprite cookButtonSpriteOverride;
    public Sprite kitchenSideButtonSpriteOverride;
    public Sprite ingredientButtonSpriteOverride;
    public Sprite lockedIngredientButtonSpriteOverride;
    public Sprite selectedIngredientButtonSpriteOverride;
    public Sprite selectedSlotPanelSpriteOverride;
    public Sprite resultFoodPanelSpriteOverride;
    public Sprite resultNextButtonSpriteOverride;
    public DaySceneSpriteOverrideSlot[] extraSpriteOverrides;

    private const int ChoiceDialogueIndex = 2;
    private const int MinIngredientSlots = 3;
    private const int MaxIngredientSlots = 4;
    private const float SelectedSlotIconSize = 124f;
    private static readonly Vector2 IngredientScrollAnchorMin = new Vector2(0.030f, 0.100f);
    private static readonly Vector2 IngredientScrollAnchorMax = new Vector2(0.185f, 0.825f);
    private const string MildChoiceText = "담백한 국물";
    private const string SpicyChoiceText = "얼큰한 국물";

    private static readonly Color32 PanelCustomerTint = new Color32(248, 239, 218, 248);
    private static readonly Color32 PanelKitchenTint = new Color32(246, 236, 215, 248);
    private static readonly Color32 PanelResultTint = new Color32(250, 243, 226, 250);
    private static readonly Color32 PanelMenuTint = new Color32(250, 240, 214, 248);

    private static readonly Color32 PrimaryTextTint = new Color32(32, 22, 14, 255);
    private static readonly Color32 SecondaryTextTint = new Color32(72, 49, 31, 255);
    private static readonly Color32 MutedTextTint = new Color32(118, 86, 59, 255);
    private static readonly Color32 WarningTextTint = new Color32(156, 43, 31, 255);
    private static readonly Color32 AccentTextTint = new Color32(42, 89, 60, 255);
    private static readonly Color32 ButtonLabelTint = new Color32(30, 19, 11, 255);

    private static readonly Color32 ButtonNormalTint = new Color32(255, 247, 227, 255);
    private static readonly Color32 ButtonHighlightTint = new Color32(246, 225, 186, 255);
    private static readonly Color32 ButtonPressedTint = new Color32(224, 178, 119, 255);
    private static readonly Color32 ButtonSelectedTint = new Color32(215, 231, 190, 255);
    private static readonly Color32 ButtonDisabledTint = new Color32(206, 190, 166, 180);
    private static readonly Color32 IngredientLockedTint = new Color32(169, 151, 123, 255);
    private static readonly Color32 IngredientLockedHighlightTint = new Color32(185, 167, 139, 255);
    private static readonly Color32 IngredientLockedPressedTint = new Color32(145, 126, 100, 255);
    private static readonly Color32 PanelShadowTint = new Color32(70, 37, 20, 70);
    private static readonly Color32 ButtonShadowTint = new Color32(73, 39, 22, 54);
    private static readonly Color32 TextShadowTint = new Color32(255, 246, 224, 80);
    private static readonly Vector2 PanelShadowOffset = new Vector2(4f, -4f);
    private static readonly Vector2 ButtonShadowOffset = new Vector2(1.25f, -1.25f);
    private static readonly Vector2 TextShadowOffset = new Vector2(1f, -1f);

    private readonly DialogueLine[] dayOneDialogueLines =
    {
        new DialogueLine
        {
            isCustomer = false,
            text = "어서 오세요. 괜찮으세요? 얼굴이 많이 지쳐 보이세요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "...불 냄새만 맡으면 아직도 그날이 떠올라."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "천천히 말씀하셔도 괜찮아요. 여기서는 잠깐 쉬어가셔도 됩니다."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "사람들이 비명을 지르며 타는 그 냄새... 아직도 코끝에서 안 떠나."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그날의 냄새가 계속 따라오고 있는 거군요. 지금은 다른 냄새로 숨을 돌려보죠."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "사람을 살리던 손이... 결국 아무도 못 살렸어."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그 손으로 지금도 버티고 계시잖아요. 오늘은 손님을 위한 따뜻한 한 끼부터 만들게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "내 딸도... 아내도... 난 왜 살아있지..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "살아남은 이유를 오늘 다 설명하지 않아도 괜찮아요. 그래도 지금은 드셔야 해요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "매콤한 냄새가 그리워... 쉬는 날마다 집에서 나던 그 냄새가."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그 기억에 가까운 음식이라면, 김치찌개가 좋겠네요. 매콤하고 뜨거운 국물로요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "어이 주인장. 날 위해서 매콤한 음식 만들어줄 수 있나?"
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "알겠습니다. 그날의 냄새가 아니라 집의 냄새가 떠오르도록 끓여볼게요."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "혹시 김치찌개에서 어떤 식재료를 좋아하세요?"
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "두부가 좋더군. 매운 국물 사이에서 부드럽게 풀리는 게... 그 사람도 꼭 넣었어."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "좋아요. 김치와 돼지고기에 두부를 넉넉히 넣어서, 그 기억에 가까운 맛으로 끓여볼게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "그래.. 맛있는 한 끼 부탁하마.."
        }
    };

    private readonly DialogueLine[] dayTwoDialogueLines =
    {
        new DialogueLine
        {
            isCustomer = false,
            text = "어서 오세요. 많이 지쳐 보이세요. 잠시 앉아서 숨부터 고르셔도 괜찮아요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "괜찮다고... 환자들과 그 가족들에게 거짓말 하는 것도... 이제 너무 지쳤어요.."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "계속 버티느라 마음이 많이 닳으셨겠어요. 여기서는 괜찮은 척하지 않으셔도 됩니다."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "살려달라는 말을 너무 많이 들었어요..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그 말들이 아직도 마음에 남아 있는 거군요. 오늘은 자극적이지 않고 속을 감싸주는 음식이 좋겠어요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "혹시 따뜻하고 부드러운 음식... 만들어 주실 수 있으신가요?"
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그럼 순두부찌개로 따뜻하게 끓여볼게요. 순두부찌개에서 특히 좋아하는 재료가 있으세요?"
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "순두부요. 부드럽게 넘어가는 게 좋고, 고춧가루가 적당히 들어갔으면 좋겠어요. 참고로 전 매운 걸 잘 못 먹어요. 버섯 향이 있으면 마음이 조금 가라앉고, 조개가 들어가면 더 좋을 것 같아요."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "좋아요. 순두부와 적당한 고춧가루, 버섯을 넣고 조개로 시원하게 끓여볼게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "부탁드릴게요. 오늘은 조용히 속을 데우고 싶어요."
        }
    };

    private readonly DialogueLine[] dayThreeDialogueLines =
    {
        new DialogueLine
        {
            isCustomer = false,
            text = "어서 와요. 여기엔 안전하게 쉬어갈 수 있어요. 천천히 앉아도 괜찮아요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "저기... 여기엔 좀비들 없죠?..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "없어요. 지금은 문도 닫혀 있고, 제가 곁에 있어요. 배부터 조금 채워볼까요?"
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "솔직히 배고픈 건 참을 수 있는데,... 혼자 사는 건... 무서워요..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "혼자 버틴 시간이 너무 길었겠네요. 여기서는 밝은 척하지 않아도 괜찮아요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "혹시... 된장찌개... 가능할까요?..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "가능해요. 그런데 민준이가 떠올리는 집밥의 안전한 맛을 조금 더 따라가볼게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "엄마가 시험날에 먹고 가라고 하셨는데 안 먹은 게 후회가..."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "그 기억이라면 따뜻한 된장찌개가 잘 맞을 것 같아요. 엄마가 차려준 밥상처럼 끓여볼게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "엄마의 손 맛을 느끼고 싶어요. 애호박도 좋아해요."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "좋아요. 된장과 두부, 버섯을 넣어서 집밥처럼 따뜻하게 끓여볼게요."
        }
    };

    private readonly Dictionary<MenuId, RecipeDefinition> recipes = new Dictionary<MenuId, RecipeDefinition>();
    private readonly Dictionary<string, string[]> ingredientTags = new Dictionary<string, string[]>();
    private readonly List<string> selectedIngredients = new List<string>(MaxIngredientSlots);
    private readonly string[] currentIngredientOptions = new string[9];
    private readonly List<Button> ingredientListButtons = new List<Button>();
    private readonly List<TMP_Text> ingredientListButtonTexts = new List<TMP_Text>();
    private RectTransform ingredientScrollView;
    private RectTransform ingredientScrollContent;
    private ScrollRect ingredientScrollRect;
    private bool runtimeIngredientButtonsBuilt;
    private RectTransform selectedSlotInteractionLayer;
    private readonly Button[] selectedSlotButtons = new Button[MaxIngredientSlots];
    private readonly Image[] selectedSlotIconImages = new Image[MaxIngredientSlots];
    private Button dayArtNoteButton;
    private Button dayArtOptionButton;
    private HashSet<string> unlockedIngredients = new HashSet<string>();
    private bool cookingGaugeActive;
    private bool lastCookingGaugeSuccess = true;
    private CookingGaugeResult lastCookingGaugeResult = CookingGaugeResult.Good;
    private float cookingGaugeValue;
    private static readonly string[] KitchenIngredientList =
    {
        "고춧가루",
        "김치",
        "버섯",
        "돼지고기",
        "조개",
        "순두부",
        "된장",
        "애호박",
        "두부"
    };
    private Canvas rootCanvas;
    private bool layoutApplied;
    private bool typographyApplied;
    private string lastCustomerSpeech = string.Empty;
    private bool colorApplied;
    private bool polishApplied;

    private int dialogueIndex;
    private bool choiceAnswered;
    private int currentDayNumber = 1;
    private DialogueLine[] currentDialogueLines;
    private MenuId selectedRecipeId = MenuId.None;
    private CustomerPreference selectedPreference = CustomerPreference.Unknown;
    private bool showingPostResultDialogue;
    private bool lastCookingSucceeded;
#if UNITY_EDITOR
    private bool staticHierarchyBuildQueued;
#endif

    private void Awake()
    {
        ResolveEditorSpriteFallbacks();
        BuildIngredientTags();
        BuildRecipes();
    }

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        BindDayResponseArtView();
        ResolveKitchenSideButtonReferences();
        RemoveDeletedResultUiObjects();
        InitUI();
        if (!useStaticDesignerLayout)
        {
            ApplyLayoutPreset();
            ApplyColorPreset();
            ApplyTypographyPreset();
            ApplyViewportFitPreset();
            ApplyCustomerOrderLayout();
            ApplyKitchenPrepLayout();
            ApplyResultLayout();
            ApplyMenuBoardLayout();
            ApplyIndieUiPolish();
            ApplyTextPlacementPolish();
            ApplyKitchenArtLayout();
            ApplyDayArtSceneLayout();
        }
        EnsureIngredientListButtons();
        BindButtons();
        EnsureCookingPotDropZone();
        ConfigureIngredientDragSources();
        LoadCustomerScene();
        ApplyDesignerSpriteOverrides();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyDesignerSpriteOverrides();
        QueueEnsureDesignerStaticHierarchy();
    }

    [ContextMenu("Rebuild Designer Static Kitchen And Result")]
    private void RebuildDesignerStaticKitchenAndResult()
    {
        if (!useStaticDesignerLayout || Application.isPlaying)
            return;

        ResolveEditorSpriteFallbacks();
        bool changed = EnsureDesignerStaticHierarchy();
        ApplyDesignerPreviewKitchenAndResultState();

        EditorUtility.SetDirty(this);
        if (gameObject.scene.IsValid())
            EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }

    [MenuItem("Tools/Day UI/Rebuild Designer Static Kitchen And Result")]
    private static void RebuildDesignerStaticKitchenAndResultInDayScene()
    {
        const string dayScenePath = "Assets/Scenes/DayScene.unity";
        Scene targetScene = default;
        bool alreadyLoaded = false;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path != dayScenePath)
                continue;

            targetScene = scene;
            alreadyLoaded = true;
            break;
        }

        if (!alreadyLoaded)
            targetScene = EditorSceneManager.OpenScene(dayScenePath, OpenSceneMode.Additive);

        DayUIManager[] managers = FindObjectsByType<DayUIManager>(FindObjectsInactive.Include);
        int rebuiltCount = 0;
        for (int i = 0; i < managers.Length; i++)
        {
            DayUIManager manager = managers[i];
            if (manager == null || manager.gameObject.scene != targetScene)
                continue;

            manager.RebuildDesignerStaticKitchenAndResult();
            manager.ApplyDesignerPreviewKitchenAndResultState();
            EditorUtility.SetDirty(manager);
            rebuiltCount++;
        }

        EditorSceneManager.MarkSceneDirty(targetScene);
        EditorSceneManager.SaveScene(targetScene);
        AssetDatabase.SaveAssets();

        if (!alreadyLoaded)
            EditorSceneManager.CloseScene(targetScene, true);

        Debug.Log("Designer static Kitchen/Result hierarchy rebuilt: " + rebuiltCount);
    }

    private void ApplyDesignerPreviewKitchenAndResultState()
    {
        if (!useStaticDesignerLayout || Application.isPlaying)
            return;

        ResolveEditorSpriteFallbacks();
        PopulateKitchenIngredientOptions();
        selectedIngredients.Clear();
        selectedIngredients.Add("김치");
        selectedIngredients.Add("돼지고기");
        selectedIngredients.Add("버섯");
        selectedIngredients.Add("두부");
        selectedRecipeId = MenuId.KimchiJjigae;
        StopCookingGaugeUi();
        cookingGaugeActive = false;
        lastCookingGaugeSuccess = true;
        cookingGaugeValue = 0f;

        UpdateIngredientButtonTexts();
        ApplyStaticKitchenArtState();
        UpdateIngredientSlots();
        UpdateCookButtonState();

        CookingResultData previewResult = new CookingResultData
        {
            StewId = CookedStewId.KimchiJjigae,
            StewName = "김치찌개",
            Grade = EvaluationGrade.Good,
            ResultSprite = kimchiStewSprite != null ? kimchiStewSprite : emptyCookingPotSprite,
            Comment = "그래... 이 정도면 몸을 데울 수 있겠군."
        };

        SetText(resultText, GetGradeLabel(previewResult.Grade));
        SetText(reactionText, previewResult.Comment);
        SetText(clueText, previewResult.StewName);
        SetText(unlockTitleText, "해금 기록");
        SetButtonLabel(nextDayButton, "다음으로");
        ApplyStaticResultArtState(previewResult);
    }

    private void QueueEnsureDesignerStaticHierarchy()
    {
        if (!useStaticDesignerLayout || Application.isPlaying || staticHierarchyBuildQueued)
            return;

        staticHierarchyBuildQueued = true;
        EditorApplication.delayCall += () =>
        {
            staticHierarchyBuildQueued = false;
            if (this == null || Application.isPlaying || !useStaticDesignerLayout)
                return;

            bool changed = EnsureDesignerStaticHierarchy();
            if (!changed)
                return;

            EditorUtility.SetDirty(this);
            if (gameObject.scene.IsValid())
                EditorSceneManager.MarkSceneDirty(gameObject.scene);
        };
    }

    private bool EnsureDesignerStaticHierarchy()
    {
        bool changed = false;
        changed |= EnsureDesignerKitchenChrome();
        changed |= EnsureDesignerIngredientList();
        changed |= EnsureDesignerSelectedSlots();
        changed |= EnsureDesignerResultChrome();
        return changed;
    }

    private bool EnsureDesignerKitchenChrome()
    {
        if (kitchenPanel == null)
            return false;

        bool changed = false;
        Image background = kitchenPanel.GetComponent<Image>();
        if (background != null && background.sprite == null && kitchenBackgroundSprite != null)
        {
            background.sprite = kitchenBackgroundSprite;
            background.color = Color.white;
            background.preserveAspect = false;
            changed = true;
        }

        changed |= EnsureDesignerKitchenGraphic("IngredientPanelGraphic", kitchenIngredientPanelSprite, new Vector2(0.010f, 0.015f), new Vector2(0.225f, 0.985f), 1);
        changed |= EnsureDesignerKitchenGraphic("SelectedSlotPanelGraphic", kitchenSlotPanelSprite, new Vector2(0.240f, 0.005f), new Vector2(0.760f, 0.250f), 2);

        if (cookingPotDropZone == null)
        {
            Transform existingPot = kitchenPanel.transform.Find("CookingPotDropZone");
            if (existingPot != null)
            {
                cookingPotDropZone = existingPot.GetComponent<RectTransform>();
                cookingPotImage = existingPot.GetComponent<Image>();
                cookingPotHintText = existingPot.GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (cookingPotDropZone == null)
        {
            RectTransform pot = CreateDesignerUiObject("CookingPotDropZone", kitchenPanel.transform, typeof(Image)).GetComponent<RectTransform>();
            SetRelativeRect(pot, new Vector2(0.390f, 0.430f), new Vector2(0.610f, 0.660f), Vector2.zero, Vector2.zero);
            cookingPotDropZone = pot;
            cookingPotImage = pot.GetComponent<Image>();
            changed = true;
        }

        if (cookingPotImage != null && cookingPotImage.sprite == null && emptyCookingPotSprite != null)
        {
            cookingPotImage.sprite = emptyCookingPotSprite;
            cookingPotImage.color = Color.white;
            cookingPotImage.preserveAspect = true;
            changed = true;
        }

        if (cookingPotHintText == null && cookingPotDropZone != null)
        {
            RectTransform hint = CreateDesignerUiObject("PotHintText", cookingPotDropZone, typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
            SetRelativeRect(hint, new Vector2(0.18f, 0.18f), new Vector2(0.82f, 0.82f), Vector2.zero, Vector2.zero);
            cookingPotHintText = hint.GetComponent<TMP_Text>();
            cookingPotHintText.alignment = TextAlignmentOptions.Center;
            cookingPotHintText.fontSize = 18f;
            cookingPotHintText.color = kitchenWorldTextColor;
            changed = true;
        }

        changed |= EnsureDesignerButtonLabel(recipeButton2, "KitchenRecipeButtonLabel", "레시피");
        changed |= EnsureDesignerButtonLabel(recipeButton3, "KitchenMemoButtonLabel", "메모장");
        return changed;
    }

    private bool EnsureDesignerKitchenGraphic(string objectName, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, int siblingIndex)
    {
        if (kitchenPanel == null)
            return false;

        bool changed = false;
        Transform existing = kitchenPanel.transform.Find(objectName);
        RectTransform rect = existing != null
            ? existing.GetComponent<RectTransform>()
            : CreateDesignerUiObject(objectName, kitchenPanel.transform, typeof(Image)).GetComponent<RectTransform>();

        if (existing == null)
        {
            SetRelativeRect(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
            rect.SetSiblingIndex(Mathf.Min(siblingIndex, kitchenPanel.transform.childCount - 1));
            changed = true;
        }

        Image image = rect.GetComponent<Image>();
        if (image != null && image.sprite == null && sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.preserveAspect = true;
            image.raycastTarget = false;
            changed = true;
        }

        return changed;
    }

    private bool EnsureDesignerButtonLabel(Button button, string objectName, string labelText)
    {
        if (button == null)
            return false;

        Transform existing = button.transform.Find(objectName);
        if (existing != null)
            return false;

        RectTransform labelRect = CreateDesignerUiObject(objectName, button.transform, typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        SetRelativeRect(labelRect, new Vector2(0.04f, 0.05f), new Vector2(0.96f, 0.35f), Vector2.zero, Vector2.zero);
        TMP_Text label = labelRect.GetComponent<TMP_Text>();
        label.text = labelText;
        label.fontSize = 17f;
        label.alignment = TextAlignmentOptions.Center;
        label.color = kitchenSideButtonLabelColor;
        label.raycastTarget = false;
        return true;
    }

    private void RemoveDeletedResultUiObjects()
    {
        Transform root = resultPanel != null ? resultPanel.transform : null;
        if (root == null)
            return;

        RemoveDeletedResultUiObject(root, "UnlockMenuText");
        RemoveDeletedResultUiObject(root, "ResultUnlockPanel");
        RemoveDeletedResultUiObject(root, "ResultPlayerPanel");
        RemoveDeletedResultUiObject(root, "ResultReactionPanel");
    }

    private static void RemoveDeletedResultUiObject(Transform root, string objectName)
    {
        Transform target = FindChildRecursive(root, objectName);
        if (target == null)
            return;

        if (Application.isPlaying)
            Destroy(target.gameObject);
        else
            DestroyImmediate(target.gameObject);
    }

    private bool EnsureDesignerResultChrome()
    {
        if (resultPanel == null)
            return false;

        RemoveDeletedResultUiObjects();
        bool changed = false;
        Image background = resultPanel.GetComponent<Image>();
        if (background != null)
            background.raycastTarget = false;

        changed |= EnsureDesignerResultText(ref resultText, "ResultText", new Vector2(0.355f, 0.620f), new Vector2(0.645f, 0.735f), 34f, TextAlignmentOptions.Center);
        changed |= EnsureDesignerResultText(ref reactionText, "ReactionText", new Vector2(0.140f, 0.270f), new Vector2(0.485f, 0.505f), 18f, TextAlignmentOptions.TopLeft);
        changed |= EnsureDesignerResultText(ref clueText, "ClueText", new Vector2(0.545f, 0.395f), new Vector2(0.850f, 0.500f), 18f, TextAlignmentOptions.TopLeft);
        changed |= EnsureDesignerResultText(ref unlockTitleText, "UnlockTitleText", new Vector2(0.545f, 0.245f), new Vector2(0.850f, 0.300f), 20f, TextAlignmentOptions.TopLeft);
        changed |= EnsureDesignerResultFoodImage();
        return changed;
    }

    private bool EnsureDesignerResultPanel(string objectName, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (resultPanel == null)
            return false;

        Transform existing = resultPanel.transform.Find(objectName);
        if (existing != null)
            return false;

        RectTransform rect = CreateDesignerUiObject(objectName, resultPanel.transform, typeof(Image)).GetComponent<RectTransform>();
        SetRelativeRect(rect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Image image = rect.GetComponent<Image>();
        image.color = new Color32(31, 42, 67, 145);
        image.raycastTarget = false;
        return true;
    }

    private bool EnsureDesignerResultFoodImage()
    {
        if (resultPanel == null)
            return false;

        if (foodImage == null)
        {
            Transform existing = resultPanel.transform.Find("FoodImage");
            if (existing != null)
                foodImage = existing.GetComponent<Image>();
        }

        if (foodImage != null)
            return false;

        RectTransform foodRect = CreateDesignerUiObject("FoodImage", resultPanel.transform, typeof(Image)).GetComponent<RectTransform>();
        SetRelativeRect(foodRect, new Vector2(0.205f, 0.590f), new Vector2(0.365f, 0.760f), Vector2.zero, Vector2.zero);
        foodImage = foodRect.GetComponent<Image>();
        foodImage.preserveAspect = true;
        foodImage.raycastTarget = false;
        foodImage.color = Color.white;
        return true;
    }

    private bool EnsureDesignerResultText(ref TMP_Text field, string objectName, Vector2 anchorMin, Vector2 anchorMax, float fontSize, TextAlignmentOptions alignment)
    {
        if (resultPanel == null)
            return false;

        if (field == null)
        {
            Transform existing = resultPanel.transform.Find(objectName);
            if (existing != null)
                field = existing.GetComponent<TMP_Text>();
        }

        if (field != null)
            return false;

        RectTransform textRect = CreateDesignerUiObject(objectName, resultPanel.transform, typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
        SetRelativeRect(textRect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        field = textRect.GetComponent<TMP_Text>();
        field.fontSize = fontSize;
        field.alignment = alignment;
        field.color = new Color32(246, 236, 218, 255);
        field.raycastTarget = false;
        return true;
    }

    private bool EnsureDesignerIngredientList()
    {
        if (kitchenPanel == null)
            return false;

        bool changed = false;
        RectTransform scrollView = FindDirectRect(kitchenPanel.transform, "IngredientListScrollView");
        if (scrollView == null)
        {
            GameObject scrollObject = CreateDesignerUiObject("IngredientListScrollView", kitchenPanel.transform, typeof(Image), typeof(ScrollRect));
            scrollView = scrollObject.GetComponent<RectTransform>();
            SetRelativeRect(scrollView, IngredientScrollAnchorMin, IngredientScrollAnchorMax, Vector2.zero, Vector2.zero);
            changed = true;
        }

        Image scrollImage = scrollView.GetComponent<Image>();
        if (scrollImage != null && changed)
            scrollImage.color = new Color32(245, 232, 199, 70);

        ScrollRect scrollRect = scrollView.GetComponent<ScrollRect>();
        RectTransform viewport = FindDirectRect(scrollView, "Viewport");
        if (viewport == null)
        {
            GameObject viewportObject = CreateDesignerUiObject("Viewport", scrollView, typeof(Image), typeof(RectMask2D));
            viewport = viewportObject.GetComponent<RectTransform>();
            SetRelativeRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            changed = true;
        }

        Image viewportImage = viewport.GetComponent<Image>();
        if (viewportImage != null && changed)
            viewportImage.color = new Color32(255, 255, 255, 1);

        RectTransform content = FindDirectRect(viewport, "Content");
        if (content == null)
        {
            GameObject contentObject = CreateDesignerUiObject("Content", viewport);
            content = contentObject.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            changed = true;
        }

        if (scrollRect != null)
        {
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 32f;
        }

        const float buttonHeight = 92f;
        const float gap = 12f;
        const float topPadding = 12f;
        content.sizeDelta = new Vector2(0f, topPadding + KitchenIngredientList.Length * buttonHeight + (KitchenIngredientList.Length - 1) * gap + 12f);

        for (int i = 0; i < KitchenIngredientList.Length; i++)
        {
            string buttonName = "RuntimeIngredientSlot_" + i;
            RectTransform buttonRect = FindDirectRect(content, buttonName);
            if (buttonRect != null)
                continue;

            GameObject buttonObject = CreateDesignerUiObject(buttonName, content, typeof(Image), typeof(Button));
            buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.sizeDelta = new Vector2(0f, buttonHeight);
            buttonRect.anchoredPosition = new Vector2(0f, -(topPadding + i * (buttonHeight + gap)));

            Image buttonImage = buttonObject.GetComponent<Image>();
            if (buttonImage != null)
                buttonImage.sprite = ingredientItemSprite;

            RectTransform iconRect = CreateDesignerUiObject("IngredientIcon", buttonRect, typeof(Image)).GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.06f, 0.18f);
            iconRect.anchorMax = new Vector2(0.27f, 0.82f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            RectTransform textRect = CreateDesignerUiObject("Name_Text", buttonRect, typeof(TextMeshProUGUI)).GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.31f, 0f);
            textRect.anchorMax = new Vector2(0.96f, 1f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            TMP_Text text = textRect.GetComponent<TMP_Text>();
            if (text != null)
            {
                text.text = KitchenIngredientList[i];
                text.alignment = TextAlignmentOptions.MidlineLeft;
                text.fontSize = 15f;
                text.fontStyle = FontStyles.Bold;
            }

            changed = true;
        }

        return changed;
    }

    private bool EnsureDesignerSelectedSlots()
    {
        if (kitchenPanel == null)
            return false;

        bool changed = false;
        RectTransform panel = FindDirectRect(kitchenPanel.transform, "SelectedIngredientSlotPanel");
        if (panel == null)
        {
            panel = CreateDesignerUiObject("SelectedIngredientSlotPanel", kitchenPanel.transform, typeof(Image)).GetComponent<RectTransform>();
            SetRelativeRect(panel, new Vector2(0.240f, 0.005f), new Vector2(0.760f, 0.250f), Vector2.zero, Vector2.zero);
            changed = true;
        }

        RectTransform layer = FindDirectRect(panel, "SelectedSlotInteractionLayer");
        if (layer == null)
        {
            layer = CreateDesignerUiObject("SelectedSlotInteractionLayer", panel).GetComponent<RectTransform>();
            SetRelativeRect(layer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            changed = true;
        }

        Vector2[] mins =
        {
            new Vector2(0.040f, 0.120f),
            new Vector2(0.280f, 0.120f),
            new Vector2(0.525f, 0.120f),
            new Vector2(0.765f, 0.120f)
        };
        Vector2[] maxs =
        {
            new Vector2(0.235f, 0.870f),
            new Vector2(0.475f, 0.870f),
            new Vector2(0.720f, 0.870f),
            new Vector2(0.960f, 0.870f)
        };

        for (int i = 0; i < MaxIngredientSlots; i++)
        {
            RectTransform slot = FindDirectRect(layer, "SelectedSlot_" + i);
            if (slot != null)
                continue;

            slot = CreateDesignerUiObject("SelectedSlot_" + i, layer).GetComponent<RectTransform>();
            SetRelativeRect(slot, mins[i], maxs[i], Vector2.zero, Vector2.zero);

            RectTransform clickArea = CreateDesignerUiObject("ClickArea", slot, typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            SetRelativeRect(clickArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            Image clickImage = clickArea.GetComponent<Image>();
            if (clickImage != null)
                clickImage.color = new Color(1f, 1f, 1f, 0f);

            RectTransform icon = CreateDesignerUiObject("IngredientIcon", slot, typeof(Image)).GetComponent<RectTransform>();
            icon.anchorMin = new Vector2(0.5f, 0.5f);
            icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = Vector2.zero;
            icon.sizeDelta = new Vector2(SelectedSlotIconSize, SelectedSlotIconSize);
            changed = true;
        }

        return changed;
    }

    private static RectTransform FindDirectRect(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        Transform child = parent.Find(childName);
        return child != null ? child.GetComponent<RectTransform>() : null;
    }

    private static GameObject CreateDesignerUiObject(string objectName, Transform parent, params Type[] extraComponents)
    {
        List<Type> components = new List<Type> { typeof(RectTransform), typeof(CanvasRenderer) };
        if (extraComponents != null)
            components.AddRange(extraComponents);

        GameObject gameObject = new GameObject(objectName, components.ToArray());
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }
#endif

    private void ResolveEditorSpriteFallbacks()
    {
#if UNITY_EDITOR
        dayOneCustomerPortrait = dayOneCustomerPortrait != null ? dayOneCustomerPortrait : LoadEditorSprite("Assets/UI/NPC1_teasu.png");
        dayTwoCustomerPortrait = dayTwoCustomerPortrait != null ? dayTwoCustomerPortrait : LoadEditorSprite("Assets/UI/NPC2_seoa.png");
        dayThreeCustomerPortrait = dayThreeCustomerPortrait != null ? dayThreeCustomerPortrait : LoadEditorSprite("Assets/UI/NPC3minjun.png");
        customerPortrait = customerPortrait != null ? customerPortrait : dayOneCustomerPortrait;
        kitchenBackgroundSprite = kitchenBackgroundSprite != null ? kitchenBackgroundSprite : LoadEditorSprite("Assets/UI/day_cookbackground.png");
        emptyCookingPotSprite = emptyCookingPotSprite != null ? emptyCookingPotSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_empty.png");
        cookButtonSprite = cookButtonSprite != null ? cookButtonSprite : LoadEditorSprite("Assets/UI/day_CookButtun.png");
        ingredientItemSprite = ingredientItemSprite != null ? ingredientItemSprite : LoadEditorSprite("Assets/UI/day_Ingredient.png");
        dayOptionButtonSprite = dayOptionButtonSprite != null ? dayOptionButtonSprite : LoadEditorSprite("Assets/UI/day_option.png");
        dayMenuButtonSprite = dayMenuButtonSprite != null ? dayMenuButtonSprite : LoadEditorSprite("Assets/UI/day_menu.png");
        dayNoteButtonSprite = dayNoteButtonSprite != null ? dayNoteButtonSprite : LoadEditorSprite("Assets/UI/day_note.png");
        resultUiSprite = resultUiSprite != null ? resultUiSprite : LoadEditorSprite("Assets/UI/resultui.png");
        resultOkUiSprite = resultOkUiSprite != null ? resultOkUiSprite : LoadEditorSprite("Assets/UI/resultokui.png");
        kitchenIngredientPanelSprite = kitchenIngredientPanelSprite != null ? kitchenIngredientPanelSprite : LoadEditorSprite("Assets/UI/daykitchen_main1.png");
        kitchenSlotPanelSprite = kitchenSlotPanelSprite != null ? kitchenSlotPanelSprite : LoadEditorSprite("Assets/UI/daykitchen_main2.png");

        if (ingredientSprites == null || ingredientSprites.Length < KitchenIngredientList.Length || ingredientSprites.Any(sprite => sprite == null))
        {
            ingredientSprites = new[]
            {
                LoadEditorSprite("Assets/UI/재료/ChiliPowder.png"),
                LoadEditorSprite("Assets/UI/재료/Kimchi.png"),
                LoadEditorSprite("Assets/UI/재료/Mushroom.png"),
                LoadEditorSprite("Assets/UI/재료/Pork.png"),
                LoadEditorSprite("Assets/UI/재료/Seasheell.png"),
                LoadEditorSprite("Assets/UI/재료/SoftTofu.png"),
                LoadEditorSprite("Assets/UI/재료/SoybeenPaste.png"),
                LoadEditorSprite("Assets/UI/재료/Squash.png"),
                LoadEditorSprite("Assets/UI/재료/Tofu.png")
            };
        }

        kimchiStewSprite = kimchiStewSprite != null ? kimchiStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_kimchi.png");
        tofuKimchiStewSprite = tofuKimchiStewSprite != null ? tofuKimchiStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_tofukimchi.png");
        doenjangStewSprite = doenjangStewSprite != null ? doenjangStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_soybeen.png");
        pumpkinDoenjangStewSprite = pumpkinDoenjangStewSprite != null ? pumpkinDoenjangStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_hobaksoybeen.png");
        soondubuStewSprite = soondubuStewSprite != null ? soondubuStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_tufu.png");
        shellSoondubuStewSprite = shellSoondubuStewSprite != null ? shellSoondubuStewSprite : LoadEditorSprite("Assets/UI/찌개/Enrqorl_bajiraktufu.png");
#endif
    }

#if UNITY_EDITOR
    private static Sprite LoadEditorSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }
#endif

    private void Update()
    {
    }

    private void InitUI()
    {
        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(choiceGroup, false);
        SetActive(menuBoardPanel, false);
        SetActive(goKitchenButton, false);
        SetInteractable(cookButton, false);
    }

    private void ResolveKitchenSideButtonReferences()
    {
        if (recipeButton1 == null)
            recipeButton1 = FindNamedButton("ChoiceButtonA");

        if (recipeButton2 == null)
            recipeButton2 = FindNamedButton("ChoiceButtonB");

        if (recipeButton3 == null)
            recipeButton3 = FindNamedButton("ChoiceButtonC");
    }

    private void BindButtons()
    {
        Bind(nextButton, OnClickNextDialogue);
        Bind(goKitchenButton, OpenKitchen);
        Bind(choiceButtonA, () => OnChoiceSelected(CustomerPreference.MildSoup));
        Bind(choiceButtonB, () => OnChoiceSelected(CustomerPreference.SpicySoup));

        Bind(recipeButton1, () => Debug.Log("설정 버튼 클릭"));
        Bind(recipeButton2, OpenMenuBoard);
        Bind(recipeButton3, OpenMemoPopup);
        BindIngredientListButtons();

        Bind(cookButton, CookSelectedRecipe);
        Bind(backButton, BackToCustomer);
        Bind(nextDayButton, StartNightFlow);

        Bind(menuOpenButton, OpenMenuBoard);
        Bind(dayArtNoteButton, OpenMemoPopup);
        Bind(dayArtOptionButton, () => Debug.Log("설정 버튼 클릭"));
        Bind(closeButton, CloseMenuBoard);

        Bind(menuButtonBibimbap, () => ShowRecipeDetail(MenuId.KimchiJjigae));
        Bind(menuButtonKimchiJjigae, () => ShowRecipeDetail(MenuId.DoenjangJjigae));
        Bind(menuButtonJeyuk, () => ShowRecipeDetail(MenuId.SoondubuJjigae));
    }

    private void EnsureCookingPotDropZone()
    {
        if (kitchenPanel == null)
            return;

        if (cookingPotDropZone == null)
        {
            Transform existingPot = kitchenPanel.transform.Find("CookingPotDropZone");

            if (existingPot != null)
            {
                cookingPotDropZone = existingPot.GetComponent<RectTransform>();
                cookingPotImage = existingPot.GetComponent<Image>();
                cookingPotHintText = existingPot.GetComponentInChildren<TMP_Text>();
            }
        }

        if (cookingPotDropZone == null)
            CreateRuntimeCookingPot();

        if (cookingPotImage != null)
        {
            cookingPotImage.raycastTarget = true;
            if (emptyCookingPotSprite != null)
            {
                cookingPotImage.sprite = emptyCookingPotSprite;
                cookingPotImage.preserveAspect = true;
                cookingPotImage.color = Color.white;
            }
        }

        ShowPotHint("재료를 뚝배기에\n넣어주세요");
    }

    private void CreateRuntimeCookingPot()
    {
        GameObject potObject = new GameObject("CookingPotDropZone", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        potObject.transform.SetParent(kitchenPanel.transform, false);

        cookingPotDropZone = potObject.GetComponent<RectTransform>();
        cookingPotDropZone.anchorMin = new Vector2(0.5f, 0.5f);
        cookingPotDropZone.anchorMax = new Vector2(0.5f, 0.5f);
        cookingPotDropZone.pivot = new Vector2(0.5f, 0.5f);
        cookingPotDropZone.anchoredPosition = new Vector2(0f, -45f);
        cookingPotDropZone.sizeDelta = new Vector2(190f, 140f);

        cookingPotImage = potObject.GetComponent<Image>();
        cookingPotImage.color = new Color(0.38f, 0.22f, 0.12f, 0.9f);

        GameObject labelObject = new GameObject("PotHintText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(potObject.transform, false);

        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 8f);
        labelRect.offsetMax = new Vector2(-8f, -8f);

        cookingPotHintText = labelObject.GetComponent<TMP_Text>();
        cookingPotHintText.alignment = TextAlignmentOptions.Center;
        cookingPotHintText.fontSize = 20f;
        cookingPotHintText.color = Color.white;
        cookingPotHintText.raycastTarget = false;
    }

    private void ConfigureIngredientDragSources()
    {
        EnsureIngredientListButtons();

        for (int i = 0; i < ingredientListButtons.Count; i++)
            ConfigureIngredientDragSource(ingredientListButtons[i], i);
    }

    private void ConfigureIngredientDragSource(Button button, int index)
    {
        if (button == null)
            return;

        DraggableIngredientUI dragSource = button.GetComponent<DraggableIngredientUI>();
        if (dragSource == null)
            dragSource = button.gameObject.AddComponent<DraggableIngredientUI>();

        dragSource.Initialize(this, index, rootCanvas);
        dragSource.enabled = false;
    }

    private void EnsureIngredientListButtons()
    {
        if (kitchenPanel == null)
            return;

        EnsureIngredientScrollView();
        HideLegacyIngredientButtons();

        if (ingredientScrollContent == null)
            return;

        RegisterStaticIngredientListButtons();

        if (!runtimeIngredientButtonsBuilt || ingredientListButtons.Count < KitchenIngredientList.Length)
        {
            if (ingredientListButtons.Count == 0)
            {
                ingredientListButtons.Clear();
                ingredientListButtonTexts.Clear();
            }

            for (int i = 0; i < KitchenIngredientList.Length; i++)
            {
                if (i < ingredientListButtons.Count && ingredientListButtons[i] != null)
                    continue;

                CreateRuntimeIngredientButton(i);
            }

            runtimeIngredientButtonsBuilt = true;
        }

        if (!useStaticDesignerLayout || !Application.isPlaying)
            ApplyIngredientListButtonLayout();
    }

    private void RegisterStaticIngredientListButtons()
    {
        if (runtimeIngredientButtonsBuilt && ingredientListButtons.Count >= KitchenIngredientList.Length)
            return;

        ingredientListButtons.Clear();
        ingredientListButtonTexts.Clear();

        Button[] buttons = ingredientScrollContent != null
            ? ingredientScrollContent.GetComponentsInChildren<Button>(true)
            : Array.Empty<Button>();

        for (int i = 0; i < buttons.Length && i < KitchenIngredientList.Length; i++)
            RegisterIngredientButton(buttons[i], buttons[i].GetComponentInChildren<TMP_Text>(true));

        runtimeIngredientButtonsBuilt = ingredientListButtons.Count > 0;
    }

    private void HideLegacyIngredientButtons()
    {
        SetActive(ingredientButton1, false);
        SetActive(ingredientButton2, false);
        SetActive(ingredientButton3, false);
        SetActive(ingredientButton4, false);

        HideLegacyIngredientButtonByName("IngredientButton1");
        HideLegacyIngredientButtonByName("IngredientButton2");
        HideLegacyIngredientButtonByName("IngredientButton3");
        HideLegacyIngredientButtonByName("IngredientButton4");
    }

    private void HideLegacyIngredientButtonByName(string objectName)
    {
        Transform legacy = FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, objectName);
        if (legacy != null)
            legacy.gameObject.SetActive(false);
    }

    private void CreateRuntimeIngredientButton(int index)
    {
        string buttonName = "RuntimeIngredientSlot_" + index;
        Transform existing = ingredientScrollContent != null ? ingredientScrollContent.Find(buttonName) : null;
        GameObject buttonObject = existing != null
            ? existing.gameObject
            : new GameObject(buttonName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.name = buttonName;
        buttonObject.transform.SetParent(ingredientScrollContent, false);
        DisableLayoutDrivenResize(buttonObject);

        Image buttonImage = buttonObject.GetComponent<Image>();
        if (buttonImage == null)
            buttonImage = buttonObject.AddComponent<Image>();
        buttonImage.sprite = ingredientItemSprite;
        buttonImage.type = Image.Type.Simple;
        buttonImage.color = Color.white;
        buttonImage.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        if (button == null)
            button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;

        Transform iconTransform = buttonObject.transform.Find("IngredientIcon");
        GameObject iconObject = iconTransform != null
            ? iconTransform.gameObject
            : new GameObject("IngredientIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(buttonObject.transform, false);
        DisableLayoutDrivenResize(iconObject);

        Image iconImage = iconObject.GetComponent<Image>();
        if (iconImage == null)
            iconImage = iconObject.AddComponent<Image>();
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;

        Transform textTransform = buttonObject.transform.Find("Name_Text");
        GameObject textObject = textTransform != null
            ? textTransform.gameObject
            : new GameObject("Name_Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);
        DisableLayoutDrivenResize(textObject);

        TMP_Text label = textObject.GetComponent<TMP_Text>();
        if (label == null)
            label = textObject.AddComponent<TextMeshProUGUI>();
        ApplyTextStyle(label, ResolveSceneFont(), 15f, FontStyles.Bold, Color.white);
        label.enableAutoSizing = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        SetTextAlignment(label, TextAlignmentOptions.MidlineLeft);

        RegisterIngredientButton(button, label);
    }

    private void EnsureIngredientScrollView()
    {
        if (kitchenPanel == null)
            return;

        bool created = false;
        if (ingredientScrollView == null)
        {
            Transform existing = kitchenPanel.transform.Find("IngredientListScrollView");
            if (existing != null)
            {
                ingredientScrollView = existing.GetComponent<RectTransform>();
                ingredientScrollRect = existing.GetComponent<ScrollRect>();

                Transform content = FindChildRecursive(existing, "Content");
                ingredientScrollContent = content != null ? content.GetComponent<RectTransform>() : null;
            }
        }

        if (ingredientScrollView == null)
        {
            GameObject scrollObject = new GameObject("IngredientListScrollView", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
            scrollObject.transform.SetParent(kitchenPanel.transform, false);
            ingredientScrollView = scrollObject.GetComponent<RectTransform>();

            Image scrollImage = scrollObject.GetComponent<Image>();
            scrollImage.color = new Color32(245, 232, 199, 70);

            GameObject viewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
            viewportObject.transform.SetParent(scrollObject.transform, false);
            RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;

            Image viewportImage = viewportObject.GetComponent<Image>();
            viewportImage.color = new Color32(255, 255, 255, 1);

            GameObject contentObject = new GameObject("Content", typeof(RectTransform));
            contentObject.transform.SetParent(viewportObject.transform, false);
            ingredientScrollContent = contentObject.GetComponent<RectTransform>();
            ingredientScrollContent.anchorMin = new Vector2(0f, 1f);
            ingredientScrollContent.anchorMax = new Vector2(1f, 1f);
            ingredientScrollContent.pivot = new Vector2(0.5f, 1f);
            ingredientScrollContent.anchoredPosition = Vector2.zero;

            ingredientScrollRect = scrollObject.GetComponent<ScrollRect>();
            ingredientScrollRect.viewport = viewportRect;
            ingredientScrollRect.content = ingredientScrollContent;
            ingredientScrollRect.horizontal = false;
            ingredientScrollRect.vertical = true;
            ingredientScrollRect.movementType = ScrollRect.MovementType.Clamped;
            ingredientScrollRect.inertia = true;
            ingredientScrollRect.scrollSensitivity = 32f;
            created = true;
        }

        if (!useStaticDesignerLayout || !Application.isPlaying || created)
            ApplyIngredientScrollViewRect();
    }

    private void ApplyIngredientScrollViewRect()
    {
        if (ingredientScrollView == null)
            return;

        SetRelativeRect(ingredientScrollView, IngredientScrollAnchorMin, IngredientScrollAnchorMax, Vector2.zero, Vector2.zero);
        ingredientScrollView.localScale = Vector3.one;

        if (ingredientScrollRect != null && ingredientScrollRect.viewport != null)
        {
            RectTransform viewport = ingredientScrollRect.viewport;
            SetRelativeRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            viewport.localScale = Vector3.one;
        }

        if (ingredientScrollContent != null)
        {
            ingredientScrollContent.anchorMin = new Vector2(0f, 1f);
            ingredientScrollContent.anchorMax = new Vector2(1f, 1f);
            ingredientScrollContent.pivot = new Vector2(0.5f, 1f);
            ingredientScrollContent.anchoredPosition = Vector2.zero;
            ingredientScrollContent.localScale = Vector3.one;
        }
    }

    private void RegisterIngredientButton(Button button, TMP_Text label)
    {
        if (button == null || ingredientListButtons.Contains(button))
            return;

        ingredientListButtons.Add(button);
        ingredientListButtonTexts.Add(label != null ? label : button.GetComponentInChildren<TMP_Text>(true));
    }

    private void BindIngredientListButtons()
    {
        EnsureIngredientListButtons();

        for (int i = 0; i < ingredientListButtons.Count; i++)
        {
            int index = i;
            Bind(ingredientListButtons[i], () => AddIngredientFromList(index));
        }
    }

    private void ApplyIngredientListButtonLayout()
    {
        EnsureIngredientScrollView();

        if (ingredientScrollContent == null)
            return;

        if (useStaticDesignerLayout && Application.isPlaying)
        {
            for (int i = 0; i < ingredientListButtons.Count; i++)
                ApplyIngredientItemStateOnly(ingredientListButtons[i], i);

            return;
        }

        const float buttonHeight = 92f;
        const float gap = 12f;
        const float topPadding = 12f;
        const float bottomPadding = 12f;
        float contentHeight = topPadding + ingredientListButtons.Count * buttonHeight + Mathf.Max(0, ingredientListButtons.Count - 1) * gap + bottomPadding;

        ingredientScrollContent.sizeDelta = new Vector2(0f, contentHeight);

        for (int i = 0; i < ingredientListButtons.Count; i++)
        {
            Button button = ingredientListButtons[i];
            if (button == null)
                continue;

            DisableLayoutDrivenResize(button.gameObject);
            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.SetParent(ingredientScrollContent, false);
            buttonRect.anchorMin = new Vector2(0f, 1f);
            buttonRect.anchorMax = new Vector2(1f, 1f);
            buttonRect.pivot = new Vector2(0.5f, 1f);
            buttonRect.offsetMin = new Vector2(0f, 0f);
            buttonRect.offsetMax = new Vector2(0f, 0f);
            buttonRect.sizeDelta = new Vector2(0f, buttonHeight);
            buttonRect.anchoredPosition = new Vector2(0f, -(topPadding + i * (buttonHeight + gap)));
            ApplyIngredientItemArt(button, i);

            TMP_Text label = i < ingredientListButtonTexts.Count ? ingredientListButtonTexts[i] : null;
            SetTextAlignment(label, TextAlignmentOptions.MidlineLeft);
        }
    }

    private void ApplyIngredientItemStateOnly(Button button, int index)
    {
        if (button == null)
            return;

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && ingredientItemSprite != null && buttonImage.sprite == null)
        {
            buttonImage.sprite = ingredientItemSprite;
            buttonImage.type = Image.Type.Simple;
        }

        Transform iconTransform = button.transform.Find("IngredientIcon");
        if (iconTransform == null)
            iconTransform = button.transform.Find("Icon_Image");

        Image iconImage = iconTransform != null ? iconTransform.GetComponent<Image>() : null;
        if (iconImage != null && ingredientSprites != null && index >= 0 && index < ingredientSprites.Length && iconImage.sprite == null)
        {
            iconImage.sprite = ingredientSprites[index];
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        TMP_Text label = index < ingredientListButtonTexts.Count ? ingredientListButtonTexts[index] : button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            TMP_FontAsset font = ResolveSceneFont();
            if (font != null && label.font == null)
                label.font = font;
            label.enableAutoSizing = false;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.alignment = TextAlignmentOptions.MidlineLeft;
        }
    }

    private void BuildIngredientTags()
    {
        ingredientTags.Clear();
        ingredientTags.Add("된장", new[] { "구수함", "깊은맛", "따뜻함", "국물" });
        ingredientTags.Add("두부", new[] { "부드러움", "따뜻함", "담백함" });
        ingredientTags.Add("버섯", new[] { "감칠맛", "깊은맛", "따뜻함" });
        ingredientTags.Add("애호박", new[] { "가벼움", "채소", "담백함" });
        ingredientTags.Add("김치", new[] { "매움", "발효", "해장", "국물" });
        ingredientTags.Add("돼지고기", new[] { "든든함", "기름짐", "고기" });
        ingredientTags.Add("대파", new[] { "향", "시원함" });
        ingredientTags.Add("순두부", new[] { "부드러움", "따뜻함", "담백함", "국물" });
        ingredientTags.Add("고춧가루", new[] { "매움", "칼칼함", "자극적" });
        ingredientTags.Add("계란", new[] { "부드러움", "고소함" });
        ingredientTags.Add("조개", new[] { "시원함", "바다향", "감칠맛" });
    }

    private void BuildRecipes()
    {
        recipes.Clear();
        recipes.Add(
            MenuId.DoenjangJjigae,
            new RecipeDefinition(
                MenuId.DoenjangJjigae,
                "된장찌개",
                "구수한 된장 국물에 두부와 버섯을 넣어 속이 편한 한 끼를 만든다.",
                new[] { "된장", "두부", "버섯", "애호박" },
                new[] { "된장", "두부", "버섯" },
                new[] { "구수함", "깊은맛", "따뜻함", "담백함" },
                new[] { "자극적", "기름짐" }));

        recipes.Add(
            MenuId.KimchiJjigae,
            new RecipeDefinition(
                MenuId.KimchiJjigae,
                "김치찌개",
                "김치와 돼지고기, 버섯을 넣어 얼큰하고 든든한 국물 요리를 만든다.",
                new[] { "김치", "돼지고기", "버섯", "두부" },
                new[] { "김치", "돼지고기", "버섯" },
                new[] { "매움", "해장", "든든함" },
                new[] { "기름짐" }));

        recipes.Add(
            MenuId.SoondubuJjigae,
            new RecipeDefinition(
                MenuId.SoondubuJjigae,
                "순두부찌개",
                "순두부와 적당한 고춧가루, 버섯을 넣어 따뜻하고 부드러운 국물 요리를 만든다.",
                new[] { "순두부", "고춧가루", "버섯", "조개" },
                new[] { "순두부", "고춧가루", "버섯" },
                new[] { "부드러움", "따뜻함", "담백함", "국물", "칼칼함" },
                new[] { "매우매움", "기름짐" }));
    }

    private void LoadCustomerScene()
    {
        bool returnedFromNight = GameFlowState.ConsumeReturnedFromNight();

        RefreshUnlockedIngredients();
        currentDayNumber = GameProgression.GetCurrentDayNumber();
#if UNITY_EDITOR
        if (startFromPreviewDayInEditor && !returnedFromNight)
        {
            currentDayNumber = Mathf.Clamp(editorPreviewDayNumber, 1, 3);
            ApplyEditorPreviewUnlocks();
        }
#endif
        currentDialogueLines = GetDialogueLinesForCurrentDay();
        dialogueIndex = 0;
        choiceAnswered = true;
        showingPostResultDialogue = false;
        lastCookingSucceeded = false;
        lastCustomerSpeech = string.Empty;
        selectedRecipeId = MenuId.None;
        selectedPreference = GetPreferenceForCurrentDay();
        selectedIngredients.Clear();
        ClearIngredientOptions();

        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(menuBoardPanel, false);
        SetActive(choiceGroup, false);
        SetActive(nextButton, true);
        SetActive(goKitchenButton, false);
        SetInteractable(nextButton, true);
        SetInteractable(cookButton, false);

        SetText(nameText, GetCustomerNameForCurrentDay());
        SetText(customerSpeechText, string.Empty);
        SetText(dialogueText, string.Empty);
        SetActive(customerSpeechText, false);
        SetText(customerInfoText, GetCustomerInfoForCurrentDay());
        UpdateDayArtHudText();
        SetPanelHeaderTitle(customerPanel, currentDayNumber + "일차 오늘의 한식");
        HideKitchenTemporaryHeader();
        SetPanelHeaderTitle(resultPanel, "요리결과");
        SetText(choiceButtonAText, MildChoiceText);
        SetText(choiceButtonBText, SpicyChoiceText);
        SetText(selectedRecipeText, "선택한 메뉴\n없음");
        SetText(recipeTitleText, "손님 단서 노트");
        SetText(recipeDetailText, BuildCustomerClueGuide());
        UpdateUnlockSummary();
        SetButtonLabel(nextDayButton, "밤 파트 시작");

        Sprite portrait = GetCustomerPortraitForCurrentDay();
        if (portraitImage != null && portrait != null)
            portraitImage.sprite = portrait;

        UpdateMenuButtons();
        UpdateMenuBoard();
        ResetKitchenIngredientUI();
        ShowCurrentDialogue();
        ApplyStaticCustomerNpcPlacement();
    }

    private void UpdateDayArtHudText()
    {
        if (dayResponseArtView == null)
            return;

        SetText(dayResponseArtView.dayText, currentDayNumber + "일차");
    }

    private void ShowCurrentDialogue()
    {
        if (currentDialogueLines == null)
            currentDialogueLines = GetDialogueLinesForCurrentDay();

        if (dialogueIndex < 0 || dialogueIndex >= currentDialogueLines.Length)
            return;

        DialogueLine line = currentDialogueLines[dialogueIndex];

        if (line.isNarration)
        {
            SetText(nameText, string.Empty);
            SetText(dialogueText, line.text);
        }
        else if (line.isCustomer)
        {
            lastCustomerSpeech = line.text;
            SetText(nameText, GetCustomerNameForCurrentDay());
            SetText(dialogueText, FormatCustomerDialogue(line.text));
        }
        else
        {
            SetText(nameText, "플레이어");
            SetText(dialogueText, FormatPlayerDialogue(line.text));
        }
    }

    private void OnClickNextDialogue()
    {
        dialogueIndex++;

        if (dialogueIndex < currentDialogueLines.Length)
        {
            ShowCurrentDialogue();
            return;
        }

        if (showingPostResultDialogue)
        {
            SetActive(nextButton, false);
            SetActive(goKitchenButton, true);
            SetButtonLabel(goKitchenButton, "밤 파트 시작");
            Bind(goKitchenButton, StartNightFlow);
            SetText(nameText, "플레이어");
            SetText(dialogueText, "손님의 대화가 끝났습니다. 밤 파트로 이동할 수 있습니다.");
            return;
        }

        SetActive(nextButton, false);
        SetActive(goKitchenButton, true);
        SetButtonLabel(goKitchenButton, "주방으로 이동");
        Bind(goKitchenButton, OpenKitchen);
        SetText(nameText, "플레이어");
        SetText(dialogueText, FormatPlayerDialogue(GetKitchenMoveTextForCurrentDay()));
    }

    private void ShowChoice()
    {
        SetActive(choiceGroup, true);
        SetInteractable(nextButton, false);
        SetText(dialogueText, FormatPlayerDialogue("강태수씨가 붙잡고 있는 음식의 기억을 짚어보세요."));
    }

    private void EnsureCustomerSpeechVisible()
    {
        if (currentDialogueLines == null)
            currentDialogueLines = GetDialogueLinesForCurrentDay();

        if (string.IsNullOrWhiteSpace(lastCustomerSpeech))
        {
            DialogueLine customerLine = currentDialogueLines.FirstOrDefault(line => line.isCustomer);
            if (customerLine != null)
                lastCustomerSpeech = customerLine.text;
        }

        SetText(dialogueText, FormatCustomerDialogue(lastCustomerSpeech));
    }

    private void OnChoiceSelected(CustomerPreference preference)
    {
        choiceAnswered = true;
        selectedPreference = preference;

        SetActive(choiceGroup, false);
        SetInteractable(nextButton, true);
        UpdateMenuBoard();

        dialogueIndex++;
        ShowCurrentDialogue();
        lastCustomerSpeech = GetCustomerReplyForChoice(preference);
        SetText(dialogueText, FormatCustomerDialogue(lastCustomerSpeech));
    }

    private string FormatCustomerDialogue(string text)
    {
        return text;
    }

    private string FormatPlayerDialogue(string text)
    {
        return text;
    }

    private string GetCustomerReplyForChoice(CustomerPreference preference)
    {
        if (currentDayNumber >= 3)
        {
            switch (preference)
            {
                case CustomerPreference.MildSoup:
                    return "네... 엄마가 끓여주던 따뜻한 된장 냄새가 생각나요.";

                case CustomerPreference.SpicySoup:
                    return "매운 것보단... 집에서 먹던 따뜻한 국물 냄새가 더 그리운 것 같아요.";

                default:
                    return "네... 엄마가 끓여주던 따뜻한 된장 냄새가 생각나요.";
            }
        }

        if (currentDayNumber >= 2)
        {
            switch (preference)
            {
                case CustomerPreference.MildSoup:
                    return "맞아요. 오늘은 따뜻하고 부드러운 순두부 국물이 좋겠어요.";

                case CustomerPreference.SpicySoup:
                    return "오늘은 매운 냄새보다 속이 편한 국물이 더 필요해요.";

                default:
                    return "맞아요. 오늘은 따뜻하고 부드러운 순두부 국물이 좋겠어요.";
            }
        }

        switch (preference)
        {
            case CustomerPreference.MildSoup:
                return "오늘은 담백한 음식으론 안 될 것 같아. 매콤한 김치 냄새가 필요해.";

            case CustomerPreference.SpicySoup:
                return "...그래. 김치찌개 냄새면 그날이 아니라, 집에서 먹던 저녁이 떠오를 것 같아.";

            default:
                return "...그래. 김치찌개 냄새면 그날이 아니라, 집에서 먹던 저녁이 떠오를 것 같아.";
        }
    }

    private DialogueLine[] GetDialogueLinesForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return dayThreeDialogueLines;

        return currentDayNumber >= 2 ? dayTwoDialogueLines : dayOneDialogueLines;
    }

    private string GetCustomerNameForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return "민준";

        return currentDayNumber >= 2 ? "윤서아" : "강태수";
    }

    private string GetCustomerInfoForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return "이름: 민준\n나이: 17세\n직업: 고등학생\n특징: 학교 피난 중 홀로 고립됨";

        return currentDayNumber >= 2
            ? "이름: 윤서아\n나이: 29세\n직업: 간호사\n특징: 백신 부족으로 동료들을 잃음"
            : "이름: 강태수\n나이: 42세\n직업: 소방관\n특징: 구조 활동 중 아내와 딸을 잃음";
    }

    private Sprite GetCustomerPortraitForCurrentDay()
    {
        if (currentDayNumber >= 3 && dayThreeCustomerPortrait != null)
            return dayThreeCustomerPortrait;

        if (currentDayNumber >= 2 && dayTwoCustomerPortrait != null)
            return dayTwoCustomerPortrait;

        if (dayOneCustomerPortrait != null)
            return dayOneCustomerPortrait;

        return customerPortrait;
    }

    private string GetKitchenMoveTextForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return "민준이가 떠올리는 엄마의 집밥 기억에 맞춰 주방으로 이동해 조리해요.";

        return currentDayNumber >= 2
            ? "2일차 손님의 단서에 맞춰 주방으로 이동해 조리해요."
            : "강태수씨의 기억에 맞춰 주방으로 이동해 조리해요.";
    }

    private CustomerPreference GetPreferenceForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return CustomerPreference.MildSoup;

        return currentDayNumber >= 2 ? CustomerPreference.MildSoup : CustomerPreference.SpicySoup;
    }

    private MenuId GetTargetRecipeForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return MenuId.DoenjangJjigae;

        return currentDayNumber >= 2 ? MenuId.SoondubuJjigae : MenuId.KimchiJjigae;
    }

    private string GetWrongRecipeReactionForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return "손님 반응: 맛있지만... 엄마가 차려주던 그 안전한 냄새와는 조금 달라요.";

        return currentDayNumber >= 2
            ? "손님 반응: 맛은 있지만 오늘 제 속에는 조금 강하게 느껴져요."
            : "손님 반응: 하... 역시. 내가 떠올리고 싶던 냄새는 이게 아니었어.";
    }

    private string GetWrongRecipeClueForCurrentDay()
    {
        if (currentDayNumber >= 3)
            return "다음 단서: 민준이는 매운 기억보다 엄마가 차려주던 집밥 같은 된장찌개를 찾고 있습니다.";

        return currentDayNumber >= 2
            ? "다음 단서: 2일차 손님은 따뜻하고 부드러운 순두부찌개를 원했습니다."
            : "다음 단서: 강태수씨는 담백한 국물이 아니라 가족과 먹던 매콤한 김치찌개의 냄새를 찾고 있었습니다.";
    }

    public void OpenMenuBoard()
    {
        PopupRootController popupRoot = FindAnyObjectByType<PopupRootController>();
        if (popupRoot != null)
        {
            SetActive(popupRoot, true);
            popupRoot.transform.SetAsLastSibling();
            popupRoot.ShowRecipe();
            return;
        }

        if (dayResponseArtView != null && dayResponseArtView.recipePopup != null)
        {
            SetActive(dayResponseArtView.recipePopup, true);
            dayResponseArtView.recipePopup.transform.SetAsLastSibling();
            return;
        }

        SetActive(menuBoardPanel, true);
        ApplyMenuBoardLayout();
        if (menuBoardPanel != null)
            menuBoardPanel.transform.SetAsLastSibling();
    }

    public void OpenMemoPopup()
    {
        PopupRootController popupRoot = FindAnyObjectByType<PopupRootController>();
        if (popupRoot != null)
        {
            popupRoot.transform.SetAsLastSibling();
            popupRoot.ShowMemo();
            return;
        }

        if (dayResponseArtView != null && dayResponseArtView.memoPopup != null)
        {
            SetActive(dayResponseArtView.memoPopup, true);
            dayResponseArtView.memoPopup.transform.SetAsLastSibling();
            return;
        }

        ShowPotHint("메모장을 찾을 수 없습니다.");
    }

    public void CloseMenuBoard()
    {
        SetActive(menuBoardPanel, false);
    }

    private void ShowRecipeDetail(MenuId menuId)
    {
        if (!CanUseRecipe(menuId))
        {
            ShowPotHint("이 메뉴는 아직 밤 파트에서 재료를 더 해금해야 열립니다.");
            return;
        }

        RecipeDefinition recipe = recipes[menuId];
        SetText(recipeTitleText, recipe.DisplayName);
        SetText(recipeDetailText, BuildRecipeDetail(recipe));
    }

    private void SelectRecipe(MenuId menuId)
    {
        if (!CanUseRecipe(menuId))
        {
            PopulateKitchenIngredientOptions();
            UpdateIngredientButtonTexts();
            ShowPotHint("이 메뉴는 아직 잠겨 있어요. 밤 파트를 먼저 진행해보세요.");
            return;
        }

        selectedRecipeId = menuId;
        RecipeDefinition recipe = recipes[menuId];

        selectedIngredients.Clear();
        SetText(selectedRecipeText, "선택한 메뉴\n" + recipe.DisplayName);
        UpdateSelectedMenuImage(menuId);
        SetupIngredientsForRecipe(recipe);
        SetText(recipeTitleText, recipe.DisplayName);
        SetText(recipeDetailText, BuildRecipeDetail(recipe));
        UpdateCookButtonState();
    }

    private void UpdateSelectedMenuImage(MenuId menuId)
    {
        if (selectedMenuImage == null)
            return;

        Sprite sprite = GetMenuSprite(menuId);
        if (sprite != null)
            selectedMenuImage.sprite = sprite;
    }

    private void SetupIngredientsForRecipe(RecipeDefinition recipe)
    {
        PopulateKitchenIngredientOptions();

        StopCookingGaugeUi();
        cookingGaugeActive = false;
        lastCookingGaugeSuccess = true;
        cookingGaugeValue = 0f;
        SetButtonLabel(cookButton, "조리하기");
        SetText(ingredientGuideText, "재료 목록");
        UpdateIngredientButtonTexts();
        UpdateIngredientSlots();
        ShowPotHint("왼쪽 재료 목록에서\n재료를 선택하세요.");
        UpdateCookButtonState();
    }

    private void SetupIngredientsForCurrentCustomer()
    {
        PopulateKitchenIngredientOptions();

        MenuId defaultRecipe = GetDefaultRecipeForCurrentClue();
        if (!CanUseRecipe(defaultRecipe))
            defaultRecipe = GetFirstUnlockedRecipe();

        SelectRecipe(defaultRecipe);
    }

    private MenuId GetDefaultRecipeForCurrentClue()
    {
        MenuId targetRecipe = GetTargetRecipeForCurrentDay();
        if (CanUseRecipe(targetRecipe))
            return targetRecipe;

        if (CanUseRecipe(MenuId.DoenjangJjigae))
            return MenuId.DoenjangJjigae;

        if (CanUseRecipe(MenuId.KimchiJjigae))
            return MenuId.KimchiJjigae;

        if (CanUseRecipe(MenuId.SoondubuJjigae))
            return MenuId.SoondubuJjigae;

        return MenuId.DoenjangJjigae;
    }

    private MenuId GetFirstUnlockedRecipe()
    {
        if (CanUseRecipe(MenuId.KimchiJjigae))
            return MenuId.KimchiJjigae;

        if (CanUseRecipe(MenuId.DoenjangJjigae))
            return MenuId.DoenjangJjigae;

        if (CanUseRecipe(MenuId.SoondubuJjigae))
            return MenuId.SoondubuJjigae;

        return MenuId.None;
    }

    private void UpdateIngredientButtonTexts()
    {
        EnsureIngredientListButtons();
        PopulateKitchenIngredientOptionsIfEmpty();

        for (int i = 0; i < ingredientListButtonTexts.Count && i < currentIngredientOptions.Length; i++)
            UpdateSingleIngredientButtonText(ingredientListButtonTexts[i], currentIngredientOptions[i], i + 1);

        UpdateIngredientButtonVisuals();
    }

    private void UpdateSingleIngredientButtonText(TMP_Text targetText, string ingredientName, int displayIndex)
    {
        if (targetText == null)
            return;

        if (string.IsNullOrEmpty(ingredientName))
        {
            targetText.text = displayIndex.ToString("00") + "  -";
            return;
        }

        targetText.text = selectedIngredients.Contains(ingredientName)
            ? ingredientName + "(선택)"
            : ingredientName;
    }

    private void UpdateIngredientButtonVisuals()
    {
        for (int i = 0; i < ingredientListButtons.Count; i++)
        {
            Button button = ingredientListButtons[i];
            string ingredientName = i < currentIngredientOptions.Length ? currentIngredientOptions[i] : string.Empty;

            if (button == null)
                continue;

            if (string.IsNullOrEmpty(ingredientName))
            {
                ApplyButtonTheme(button, ButtonDisabledTint, ButtonDisabledTint, ButtonDisabledTint, ButtonDisabledTint, ButtonDisabledTint);
                ApplyIngredientButtonSprite(button, ingredientButtonSpriteOverride);
                continue;
            }

            if (!IsIngredientUnlocked(ingredientName))
            {
                ApplyButtonTheme(button, IngredientLockedTint, IngredientLockedHighlightTint, IngredientLockedPressedTint, IngredientLockedTint, IngredientLockedTint);
                ApplyIngredientButtonSprite(button, lockedIngredientButtonSpriteOverride != null ? lockedIngredientButtonSpriteOverride : ingredientButtonSpriteOverride);
                continue;
            }

            if (selectedIngredients.Contains(ingredientName))
            {
                ApplyButtonTheme(button, ButtonSelectedTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
                ApplyIngredientButtonSprite(button, selectedIngredientButtonSpriteOverride != null ? selectedIngredientButtonSpriteOverride : ingredientButtonSpriteOverride);
                continue;
            }

            ApplyButtonTheme(button, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
            ApplyIngredientButtonSprite(button, ingredientButtonSpriteOverride);
        }
    }

    private bool AddIngredientFromDrag(int index)
    {
        if (index < 0 || index >= currentIngredientOptions.Length)
            return false;

        string ingredientName = currentIngredientOptions[index];
        if (string.IsNullOrEmpty(ingredientName))
            return false;

        if (selectedIngredients.Contains(ingredientName))
        {
            selectedIngredients.Remove(ingredientName);
            UpdateIngredientButtonTexts();
            UpdateIngredientSlots();
            UpdateCookingPotState();
            UpdateCookButtonState();
            ShowPotHint(ingredientName + "을 뚝배기에서 뺐어요.");
            return true;
        }

        if (selectedIngredients.Count >= MaxIngredientSlots)
        {
            ShowPotHint("뚝배기에는 재료를 최대 " + MaxIngredientSlots + "개까지 넣을 수 있어요.");
            return true;
        }

        selectedIngredients.Add(ingredientName);
        UpdateIngredientButtonTexts();
        UpdateIngredientSlots();
        UpdateCookingPotState();
        UpdateCookButtonState();
        return true;
    }

    private void AddIngredientFromList(int index)
    {
        if (cookingGaugeActive)
            return;

        AddIngredientFromDrag(index);
    }

    private bool CanStartIngredientDrag(int index)
    {
        return kitchenPanel != null
            && kitchenPanel.activeInHierarchy
            && index >= 0
            && index < currentIngredientOptions.Length
            && !string.IsNullOrEmpty(currentIngredientOptions[index])
            && IsIngredientUnlocked(currentIngredientOptions[index]);
    }

    private bool TryDropIngredientIntoPot(int index, PointerEventData eventData)
    {
        if (cookingPotDropZone == null || eventData == null)
            return false;

        bool isInsidePot = RectTransformUtility.RectangleContainsScreenPoint(
            cookingPotDropZone,
            eventData.position,
            eventData.pressEventCamera);

        if (!isInsidePot)
            return false;

        return AddIngredientFromDrag(index);
    }

    private void UpdateIngredientSlots()
    {
        EnsureSelectedSlotInteractionLayer();

        for (int i = 0; i < MaxIngredientSlots; i++)
            RefreshFixedSelectedSlot(i);

        UpdateCookingPotState();
    }

    private void RemoveSelectedIngredientAtSlot(int index)
    {
        if (cookingGaugeActive || index < 0 || index >= selectedIngredients.Count)
            return;

        string removedIngredient = selectedIngredients[index];
        selectedIngredients.RemoveAt(index);
        UpdateIngredientButtonTexts();
        UpdateIngredientSlots();
        UpdateCookingPotState();
        UpdateCookButtonState();
        ShowPotHint(removedIngredient + "을 슬롯에서 뺐어요.");
    }

    private Sprite GetIngredientSprite(string ingredientName)
    {
        if (string.IsNullOrEmpty(ingredientName) || ingredientSprites == null)
            return null;

        int index = Array.IndexOf(KitchenIngredientList, ingredientName);
        return index >= 0 && index < ingredientSprites.Length ? ingredientSprites[index] : null;
    }

    private void UpdateCookingPotState()
    {
        if (cookingGaugeActive)
            return;

        if (selectedIngredients.Count == 0)
        {
            ShowPotHint("왼쪽 재료 목록에서\n재료를 선택하세요.");
            return;
        }

        string suffix = selectedIngredients.Count >= MinIngredientSlots
            ? "\n\n조리하기 버튼을 눌러\n게이지를 맞추세요."
            : "\n\n선택한 재료가\n아래 슬롯에 담깁니다.";

        ShowPotHint("담긴 재료\n" + string.Join(" / ", selectedIngredients) + suffix);
    }

    private void UpdateCookButtonState()
    {
        SetInteractable(cookButton, !cookingGaugeActive && selectedIngredients.Count >= MinIngredientSlots);
    }

    private void ResetKitchenIngredientUI()
    {
        PopulateKitchenIngredientOptions();
        selectedIngredients.Clear();
        StopCookingGaugeUi();
        cookingGaugeActive = false;
        lastCookingGaugeSuccess = true;
        cookingGaugeValue = 0f;
        SetButtonLabel(cookButton, "조리하기");
        SetText(ingredientGuideText, "재료 목록");
        UpdateIngredientButtonTexts();
        UpdateIngredientSlots();
        UpdateCookingPotState();
        UpdateCookButtonState();
    }

    private void OpenKitchen()
    {
        SetPanelState(showCustomer: false, showKitchen: true, showResult: false);
        SetActive(menuBoardPanel, false);
        EnsureCookingPotDropZone();
        ConfigureIngredientDragSources();
        PopulateKitchenIngredientOptions();
        UpdateIngredientButtonTexts();
        SetupIngredientsForCurrentCustomer();
        ApplyKitchenArtLayout();
    }

    private void BackToCustomer()
    {
        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(menuBoardPanel, false);
    }

    private void CookSelectedRecipe()
    {
        if (cookingGaugeActive)
            return;

        if (selectedIngredients.Count < MinIngredientSlots)
            return;

        StartCookingGauge();
    }

    private void StartCookingGauge()
    {
        ButtonMashingGauge gauge = EnsureButtonMashingGauge();
        if (gauge == null)
        {
            lastCookingGaugeResult = CookingGaugeResult.Good;
            FinishCookSelectedRecipe();
            return;
        }

        cookingGaugeActive = true;
        lastCookingGaugeSuccess = false;
        cookingGaugeValue = 0f;
        SetActive(cookingPotHintText, false);
        UpdateCookButtonState();
        gauge.StartGauge();
    }

    private void HandleCookingGaugeFinished(ButtonMashingGauge.GaugeResult result)
    {
        cookingGaugeActive = false;
        lastCookingGaugeResult = ConvertGaugeResult(result);
        lastCookingGaugeSuccess = lastCookingGaugeResult == CookingGaugeResult.Good;
        cookingGaugeValue = buttonMashingGauge != null ? buttonMashingGauge.CurrentValue : 0f;
        SetActive(cookingPotHintText, true);
        UpdateCookButtonState();
        FinishCookSelectedRecipe();
    }

    private CookingGaugeResult ConvertGaugeResult(ButtonMashingGauge.GaugeResult result)
    {
        switch (result)
        {
            case ButtonMashingGauge.GaugeResult.Good:
                return CookingGaugeResult.Good;
            case ButtonMashingGauge.GaugeResult.Overheat:
                return CookingGaugeResult.Overheated;
            default:
                return CookingGaugeResult.Low;
        }
    }

    private void FinishCookSelectedRecipe()
    {
        if (selectedIngredients.Count < MinIngredientSlots)
            return;

        CookingResultData result = EvaluateCookingResult();

        Debug.Log("조리하기: " + string.Join(", ", selectedIngredients));
        lastCookingSucceeded = result.Grade >= EvaluationGrade.Good;

        SetPanelState(showCustomer: false, showKitchen: true, showResult: true);
        SetActive(menuBoardPanel, false);

        if (!useStaticDesignerLayout && resultPanel != null)
        {
            resultPanel.name = "resultui";
            resultPanel.transform.SetAsLastSibling();
            Image resultPanelImage = resultPanel.GetComponent<Image>();
            if (resultPanelImage != null)
                resultPanelImage.raycastTarget = false;
        }

        if (!useStaticDesignerLayout && nextDayButton != null)
        {
            nextDayButton.name = "resultokui";
            nextDayButton.transform.SetAsLastSibling();
            ApplyButtonSprite(nextDayButton, resultOkUiSprite);
        }

        if (foodImage != null)
        {
            foodImage.sprite = result.ResultSprite;
            foodImage.preserveAspect = true;
            if (result.ResultSprite == null)
                Debug.LogWarning("찌개 결과 리소스를 찾지 못했습니다. 선택 조합: " + string.Join(", ", selectedIngredients));
        }

        SetText(resultText, GetGradeLabel(result.Grade));
        SetText(reactionText, result.Comment);
        SetText(clueText, result.StewName);
        SetText(unlockTitleText, string.Empty);
        SetActive(unlockTitleText, false);
        SetResultClueLabel("완성된 찌개");
        SetButtonLabel(nextDayButton, "다음으로");
        Bind(nextDayButton, OnClickResultOk);
        ApplyResultArtLayout(result);

        if (result.Grade >= EvaluationGrade.Good && currentDayNumber == 1)
        {
            GameProgression.UnlockIngredients("순두부", "고춧가루", "계란");
            RefreshUnlockedIngredients();
        }

        UpdateMenuButtons();
    }

    private void OnClickResultOk()
    {
        ReturnToCustomerForPostResultDialogue();
    }

    private CookingResultData EvaluateCookingResult()
    {
        CookingResultData result = new CookingResultData
        {
            StewId = CookedStewId.None,
            StewName = "알 수 없는 찌개",
            Grade = EvaluationGrade.Poor,
            ResultSprite = emptyCookingPotSprite
        };

        if (!TryResolveCookedStew(out result.StewId, out result.StewName, out result.HasOptionalIngredient, out result.ResultSprite))
        {
            result.Comment = GetCustomerResultComment(result.Grade);
            Debug.LogWarning("어떤 찌개 조합에도 해당하지 않습니다. 선택 조합: " + string.Join(", ", selectedIngredients));
            return result;
        }

        if (!IsExpectedStewForCurrentDay(result.StewId))
        {
            result.Grade = EvaluationGrade.Poor;
            result.Comment = GetCustomerResultComment(result.Grade);
            return result;
        }

        result.Grade = ResolveResultGrade(result.HasOptionalIngredient, lastCookingGaugeResult);
        result.Comment = GetCustomerResultComment(result.Grade);
        return result;
    }

    private bool IsExpectedStewForCurrentDay(CookedStewId stewId)
    {
        if (currentDayNumber == 1)
            return stewId == CookedStewId.KimchiJjigae;

        if (currentDayNumber == 2)
            return stewId == CookedStewId.SoondubuJjigae;

        return stewId == CookedStewId.DoenjangJjigae;
    }

    private bool TryResolveCookedStew(out CookedStewId stewId, out string stewName, out bool hasOptionalIngredient, out Sprite sprite)
    {
        stewId = CookedStewId.None;
        stewName = "알 수 없는 찌개";
        hasOptionalIngredient = false;
        sprite = emptyCookingPotSprite;

        if (MatchesStew(new[] { "김치", "돼지고기", "버섯" }, "두부", out hasOptionalIngredient))
        {
            stewId = CookedStewId.KimchiJjigae;
            stewName = "김치찌개";
            sprite = hasOptionalIngredient ? tofuKimchiStewSprite : kimchiStewSprite;
            WarnMissingStewSprite(sprite, stewName, hasOptionalIngredient);
            return true;
        }

        if (MatchesStew(new[] { "된장", "두부", "버섯" }, "애호박", out hasOptionalIngredient))
        {
            stewId = CookedStewId.DoenjangJjigae;
            stewName = "된장찌개";
            sprite = hasOptionalIngredient ? pumpkinDoenjangStewSprite : doenjangStewSprite;
            WarnMissingStewSprite(sprite, stewName, hasOptionalIngredient);
            return true;
        }

        if (MatchesStew(new[] { "순두부", "고춧가루", "버섯" }, "조개", out hasOptionalIngredient))
        {
            stewId = CookedStewId.SoondubuJjigae;
            stewName = "순두부찌개";
            sprite = hasOptionalIngredient ? shellSoondubuStewSprite : soondubuStewSprite;
            WarnMissingStewSprite(sprite, stewName, hasOptionalIngredient);
            return true;
        }

        return false;
    }

    private bool MatchesStew(string[] requiredIngredients, string optionalIngredient, out bool hasOptionalIngredient)
    {
        hasOptionalIngredient = selectedIngredients.Contains(optionalIngredient);
        bool hasAllRequiredIngredients = requiredIngredients.All(ingredient => selectedIngredients.Contains(ingredient));
        return hasAllRequiredIngredients;
    }

    private EvaluationGrade ResolveResultGrade(bool hasOptionalIngredient, CookingGaugeResult gaugeResult)
    {
        if (!hasOptionalIngredient)
            return gaugeResult == CookingGaugeResult.Good ? EvaluationGrade.Good : EvaluationGrade.Poor;

        return gaugeResult == CookingGaugeResult.Good ? EvaluationGrade.Perfect : EvaluationGrade.Good;
    }

    private string GetCustomerResultComment(EvaluationGrade grade)
    {
        string customerName = GetCustomerNameForCurrentDay();

        if (customerName == "강태수")
        {
            if (grade == EvaluationGrade.Perfect)
                return "아내가 끓여주던 그 맛이야... 고맙네, 정말.";

            return grade == EvaluationGrade.Good
                ? "그래... 이 매운맛이면 오늘은 버틸 수 있겠군."
                : "역시... 이런 맛까지 바라면 안 됐나.";
        }

        if (customerName == "윤서아")
        {
            if (grade == EvaluationGrade.Perfect)
                return "이 온기 덕분에 다시 사람들을 돌볼 수 있을 것 같아요.";

            return grade == EvaluationGrade.Good
                ? "조금은... 숨을 돌릴 수 있을 것 같아요."
                : "따뜻한 줄 알았는데... 마음이 더 식는 것 같아요.";
        }

        if (customerName == "민준" || customerName == "준")
        {
            if (grade == EvaluationGrade.Perfect)
                return "엄마가 해준 것 같아요... 오늘도 살아볼게요.";

            return grade == EvaluationGrade.Good
                ? "맛있어요... 집 생각이 조금 나네요."
                : "괜찮아요... 제가 너무 기대했나 봐요.";
        }

        Debug.LogWarning("결과 한줄평이 정의되지 않은 손님입니다: " + customerName);
        return grade == EvaluationGrade.Poor ? "다시 조리해보는 게 좋겠어요." : "좋은 한 끼였어요.";
    }

    private void WarnMissingStewSprite(Sprite sprite, string stewName, bool hasOptionalIngredient)
    {
        if (sprite != null)
            return;

        Debug.LogWarning(stewName + " 결과 리소스를 찾지 못했습니다. 선택 재료 포함: " + hasOptionalIngredient);
    }

    private void ReturnToCustomerForPostResultDialogue()
    {
        showingPostResultDialogue = true;
        currentDialogueLines = GetPostResultDialogueLines(lastCookingSucceeded);
        dialogueIndex = 0;

        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(menuBoardPanel, false);
        SetActive(choiceGroup, false);
        SetActive(nextButton, true);
        SetActive(goKitchenButton, false);
        SetInteractable(nextButton, true);
        Bind(nextButton, OnClickNextDialogue);

        if (dayResponseArtView != null)
            dayResponseArtView.gameObject.SetActive(true);

        ShowCurrentDialogue();
    }

    private DialogueLine[] GetPostResultDialogueLines(bool succeeded)
    {
        if (currentDayNumber >= 3)
        {
            return succeeded
                ? new[]
                {
                    CustomerLine("흑…. 와 이거… 엄마가 시험 끝나면 꼭 해줬는데…"),
                    CustomerLine("진짜 이상하네요… 뭔가 울 것 같아요…"),
                    CustomerLine("마음이 강해진 게 아니였네요."),
                    CustomerLine("형.. 아니 사장님… 자주 와서 먹어도 될까요?..."),
                    CustomerLine("정말 감사해요. 엄마가 정말 그리워요.. 잘 지내고 있으실까요?."),
                    CustomerLine("아빠도 드시면 좋을텐데…"),
                    CustomerLine("나중에 모든 게 괜찮아지면 집에 가서 엄마가 해주는 김치찌개 꼭 먹을거에요."),
                    CustomerLine("그러기 위해선 오늘도 살아야겠죠..")
                }
                : new[]
                {
                    CustomerLine("역시…. 너무 큰 욕심인가…"),
                    CustomerLine("엄마… 어디에 있어요?"),
                    CustomerLine("아빠… 회사에 가신거죠?..."),
                    NarrationLine("(뒤틀리는 소리)"),
                    NarrationLine("변이 후 주인공 사망")
                };
        }

        if (currentDayNumber >= 2)
        {
            return succeeded
                ? new[]
                {
                    NarrationLine("잠깐의 침묵 후 손을 떨면서 한 입 먹는다."),
                    CustomerLine("흐으… 너무 따뜻해요…"),
                    NarrationLine("(울먹이며 숟가락을 놓는다.)"),
                    CustomerLine("사장님 정말 감사해요.."),
                    CustomerLine("오랜만에 따뜻한 음식을.. 먹었네요.."),
                    CustomerLine("너무 많은 환자들을 보고.. 치료하고… 죽음을 바라보고.."),
                    CustomerLine("제 마음이 너무 차가웠는데…"),
                    CustomerLine("이제 좀 따뜻한 것 같아요."),
                    CustomerLine("그래요… 어쩔 수 없이 돌아가시는 분들도 많이 계시죠."),
                    CustomerLine("하지만 그 중에서 저 덕분에 다시 일어나신 분들을 보면.."),
                    CustomerLine("너무 행복했어요. 앞으로도.. 사람들을 위해서 노력할게요."),
                    CustomerLine("나중에 천국에 가면.. 먼저 가신 분들에게 죄송하다고.. 말할게요.")
                }
                : new[]
                {
                    CustomerLine("심정지… 사망… 변이…"),
                    CustomerLine("내가 조금만 더 빨랐다면…"),
                    CustomerLine("열이 오르면…또 그것처럼… 변할거야.."),
                    NarrationLine("(뒤틀리는 소리)"),
                    CustomerLine("내가… 치료..해줄게…"),
                    NarrationLine("변이 후 주인공 살해당함.")
                };
        }

        return succeeded
            ? new[]
            {
                CustomerLine("후… 이 냄새… 하…"),
                NarrationLine("(흐느끼는 소리)"),
                NarrationLine("울음을 참고 한 입 먹는다."),
                CustomerLine("그래.. 이 맛.. 쉬는 날마다 아내가 끓여줬어…"),
                CustomerLine("다시는.. 다시는 못 먹을 줄 알았어.."),
                NarrationLine("순식간에 그릇째 들고 마신다."),
                CustomerLine("후… 주인장 고마워. 그래.. 아직 끝난 건 아니지… 끝까지 살아서 아내랑 딸을 만날 때 누구 한 명이라도 더 구했다고.. 이것 때문에 늦었다고 말해줄거야"),
                CustomerLine("주인장… 만약에.. 정말 만약에 삶이 고달프면 앞으로도 와도 될까?.."),
                PlayerDialogueLine("당연하죠. 언제든지 오세요. 그 때도 김치찌개 끓이고 기다릴게요."),
                PlayerDialogueLine("다치지 마시고 삶을 포기하지 마세요."),
                CustomerLine("고마워.. 정말 고마워 주인장…")
            }
            : new[]
            {
                CustomerLine("하.. 역시. 난 살아있는 자체가 죄야."),
                CustomerLine("문 뒤에서 소리 지르던 그 비명들… 죄송합니다.."),
                NarrationLine("(뒤틀리는 소리, 비명,)")
            };
    }

    private static DialogueLine CustomerLine(string text)
    {
        return new DialogueLine { isCustomer = true, text = text };
    }

    private static DialogueLine PlayerDialogueLine(string text)
    {
        return new DialogueLine { isCustomer = false, text = text };
    }

    private static DialogueLine NarrationLine(string text)
    {
        return new DialogueLine { isNarration = true, text = text };
    }

    private void RestartFromFirstDay()
    {
        GameProgression.ResetProgress();
        LoadCustomerScene();
    }

    private void ShowCustomerClueGuide()
    {
        SetText(recipeTitleText, "손님 단서");
        SetText(recipeDetailText, BuildCustomerClueGuide());
    }

    private void ShowIngredientTagGuide()
    {
        string[] options = currentIngredientOptions
            .Where(option => !string.IsNullOrEmpty(option))
            .Select(option => option + ": " + string.Join(", ", ingredientTags[option]))
            .ToArray();

        SetText(recipeTitleText, "재료 속성");
        SetText(recipeDetailText, options.Length == 0
            ? "주방에 들어가면 현재 사용할 수 있는 재료 속성이 표시됩니다."
            : string.Join(Environment.NewLine, options));
    }

    private void ShowEvaluationGuide()
    {
        SetText(recipeTitleText, "평가 기준");
        SetText(recipeDetailText,
            "손님이 원한 속성과 음식 속성이 맞으면 점수가 올라갑니다.\n\n" +
            "싫어하는 속성이나 금기 재료가 들어가면 감점됩니다.\n\n" +
            "이 손님은 메뉴명을 직접 말하지 않으므로 레시피 암기보다 단서 해석이 중요합니다.");
    }

    private void SetResultClueLabel(string label)
    {
        Transform labelTransform = FindChildRecursive(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultClue");
        TMP_Text labelText = labelTransform != null ? labelTransform.GetComponent<TMP_Text>() : null;
        SetText(labelText, label);
    }

    private EvaluationResult EvaluateCustomerMatch()
    {
        if (currentDayNumber == 1)
        {
            return EvaluateExactDayIngredientSet(
                new[] { "김치", "돼지고기", "버섯" },
                "두부",
                "하.. 역시. 난 살아있는 자체가 죄야.",
                "문 뒤에서 소리 지르던 그 비명들... 죄송합니다..");
        }

        if (currentDayNumber == 2)
        {
            return EvaluateExactDayIngredientSet(
                new[] { "순두부", "고춧가루", "버섯" },
                "조개",
                "심정지.... 사망... 변이.... 내가 조금만 더 빨랐다면.... 열이 오르면.... 또 그것처럼 변할 거야..",
                ".......");
        }

        if (currentDayNumber >= 3)
        {
            return EvaluateExactDayIngredientSet(
                new[] { "된장", "두부", "버섯" },
                "애호박",
                "\"역시.... 너무 큰 욕심인가...\"\n\n\"엄마... 어디에 있어요?\"\n\n\"아빠... 회사에 가신거죠?...\"",
                "(뒤틀리는 소리)\n변이 후 주인공 사망");
        }

        string[] foodTags = GetCurrentFoodTags();
        string[] desiredTags = GetDesiredTagsForCurrentCustomer();
        string[] avoidedTags = GetAvoidedTagsForCurrentCustomer();
        string[] forbiddenIngredients = GetForbiddenIngredientsForCurrentCustomer();

        int score = 0;
        score += foodTags.Count(tag => desiredTags.Contains(tag)) * 2;
        score -= foodTags.Count(tag => avoidedTags.Contains(tag)) * 2;
        score -= selectedIngredients.Count(ingredient => forbiddenIngredients.Contains(ingredient)) * 5;

        if (selectedIngredients.Contains("된장") || selectedIngredients.Contains("김치") || selectedIngredients.Contains("순두부"))
            score += 1;

        if (selectedIngredients.Contains("두부"))
            score += 1;

        EvaluationGrade grade = GetGrade(score, 0);
        return new EvaluationResult(
            grade,
            score,
            BuildCustomerReaction(grade, foodTags, desiredTags, avoidedTags, forbiddenIngredients),
            BuildCustomerClue(grade, foodTags, desiredTags, avoidedTags, forbiddenIngredients));
    }

    private EvaluationResult EvaluateExactDayIngredientSet(string[] requiredIngredients, string bonusIngredient, string failureReaction, string failureClue)
    {
        bool hasAllRequiredIngredients = requiredIngredients.All(ingredient => selectedIngredients.Contains(ingredient));
        bool hasOnlyAllowedIngredients = selectedIngredients.All(ingredient => requiredIngredients.Contains(ingredient) || ingredient == bonusIngredient);

        if (!hasAllRequiredIngredients || !hasOnlyAllowedIngredients)
        {
            return new EvaluationResult(
                EvaluationGrade.Poor,
                0,
                failureReaction,
                failureClue);
        }

        bool hasBonusIngredient = selectedIngredients.Contains(bonusIngredient);
        EvaluationGrade grade = hasBonusIngredient ? EvaluationGrade.Perfect : EvaluationGrade.Good;
        int score = hasBonusIngredient ? 100 : 80;

        return new EvaluationResult(
            grade,
            score,
            BuildCustomerReaction(grade, GetCurrentFoodTags(), GetDesiredTagsForCurrentCustomer(), GetAvoidedTagsForCurrentCustomer(), GetForbiddenIngredientsForCurrentCustomer()),
            BuildCustomerClue(grade, GetCurrentFoodTags(), GetDesiredTagsForCurrentCustomer(), GetAvoidedTagsForCurrentCustomer(), GetForbiddenIngredientsForCurrentCustomer()));
    }

    private string[] GetCurrentFoodTags()
    {
        return selectedIngredients
            .Where(ingredientTags.ContainsKey)
            .SelectMany(ingredient => ingredientTags[ingredient])
            .Distinct()
            .ToArray();
    }

    private string[] GetDesiredTagsForCurrentCustomer()
    {
        if (selectedPreference == CustomerPreference.SpicySoup)
            return new[] { "매움", "발효", "해장", "국물", "따뜻함", "든든함", "시원함" };

        if (currentDayNumber >= 3)
            return new[] { "따뜻함", "구수함", "깊은맛", "담백함", "국물" };

        return new[] { "따뜻함", "부드러움", "담백함", "국물", "칼칼함", "고소함" };
    }

    private string[] GetAvoidedTagsForCurrentCustomer()
    {
        if (selectedPreference == CustomerPreference.SpicySoup)
            return Array.Empty<string>();

        return new[] { "매우매움", "자극적", "기름짐" };
    }

    private string[] GetForbiddenIngredientsForCurrentCustomer()
    {
        if (selectedPreference == CustomerPreference.SpicySoup)
            return Array.Empty<string>();

        return Array.Empty<string>();
    }

    private string ResolveCookedFoodName()
    {
        if (selectedIngredients.Contains("김치") && selectedIngredients.Contains("순두부"))
            return "순두부김치찌개";

        if (selectedIngredients.Contains("순두부"))
            return "순두부찌개";

        if (selectedIngredients.Contains("김치"))
            return "김치찌개";

        if (selectedIngredients.Contains("된장"))
            return "된장찌개";

        return "즉석 맞춤 뚝배기";
    }

    private Sprite GetFoodSpriteForCurrentIngredients()
    {
        if (selectedIngredients.Contains("김치") && kimchiJjigaeSprite != null)
            return kimchiJjigaeSprite;

        if (selectedIngredients.Contains("순두부") && jeyukSprite != null)
            return jeyukSprite;

        return bibimbapSprite;
    }

    private string BuildCustomerReaction(
        EvaluationGrade grade,
        string[] foodTags,
        string[] desiredTags,
        string[] avoidedTags,
        string[] forbiddenIngredients)
    {
        string matchedTags = string.Join(", ", foodTags.Intersect(desiredTags));
        string riskyTags = string.Join(", ", foodTags.Intersect(avoidedTags));
        string forbidden = string.Join(", ", selectedIngredients.Where(ingredient => forbiddenIngredients.Contains(ingredient)));

        if (!string.IsNullOrEmpty(forbidden))
        {
            return currentDayNumber >= 2
                ? "손님 반응: " + forbidden + " 때문에 오늘은 속이 조금 부담스러워요."
                : "손님 반응: " + forbidden + " 때문에 찾던 기억에서 멀어진 것 같아.";
        }

        if (currentDayNumber >= 3)
        {
            if (!string.IsNullOrEmpty(riskyTags) && grade <= EvaluationGrade.Okay)
                return "손님 반응: 맛있지만 " + riskyTags + " 느낌이 강해서 엄마가 차려주던 밥상과는 조금 멀어요.";

            switch (grade)
            {
                case EvaluationGrade.Perfect:
                    return "\"흑... 와 이거... 엄마가 시험 끝나면 꼭 해줬는데...\"\n\n\"진짜 이상하네요... 뭔가 울 것 같아요...\"\n\n\"마음이 강해진 게 아니었네요.\"\n\n\"형.. 아니 사장님... 자주 와서 먹어도 될까요?...\"\n\n\"정말 감사해요. 엄마가 정말 그리워요... 잘 지내고 있으실까요?..\"\n\n\"아빠도 드시면 좋을텐데...\"\n\n\"나중에 모든 게 괜찮아지면 집에 가서 엄마가 해주는 된장찌개 꼭 먹을거에요.\"\n\n\"그러기 위해선 오늘도 살아야겠죠..\"";

                case EvaluationGrade.Good:
                    return "손님 반응: 좋아요. 따뜻한 국물 덕분에 조금 안심되는 것 같아요.";

                case EvaluationGrade.Okay:
                    return "손님 반응: 괜찮지만 엄마가 끓여주던 그 맛에는 아직 조금 멀어요.";

                default:
                    return "손님 반응: 먹을 수는 있지만... 제가 찾던 집밥의 느낌은 아니에요.";
            }
        }

        if (currentDayNumber >= 2)
        {
            if (!string.IsNullOrEmpty(riskyTags) && grade <= EvaluationGrade.Okay)
                return "손님 반응: 맛은 있지만 " + riskyTags + " 쪽이 강해서 속이 편한 느낌은 덜하네요.";

            switch (grade)
            {
                case EvaluationGrade.Perfect:
                    return "\"흐으.... 너무 따뜻해요....\"\n\n(울먹이며 손가락을 놓는다.)\n\n\"사장님 정말 감사해요..\n오랜만에 따뜻한 음식을... 먹었네요..\n너무 많은 환자들을 보고 치료하고 죽음을 바라보고...\n제 마음이 너무 차가웠는데...\n이제 좀 따뜻한 것 같아요.\"";

                case EvaluationGrade.Good:
                    return "손님 반응: 좋아요. 따뜻하고 부드러운 국물 덕분에 몸이 조금 풀리는 것 같아요.";

                case EvaluationGrade.Okay:
                    return "손님 반응: 괜찮지만 제가 원한 편안한 맛과는 조금 달라요.";

                default:
                    return "손님 반응: 오늘 제 컨디션에는 이 조합이 조금 부담스러워요.";
            }
        }

        if (!string.IsNullOrEmpty(riskyTags) && grade <= EvaluationGrade.Okay)
            return "손님 반응: 맛은 있지만 " + riskyTags + " 쪽이 강해서 내가 찾던 김치찌개와는 조금 달라.";

        switch (grade)
        {
            case EvaluationGrade.Perfect:
                return "울음을 참고 한 입 먹는다.\n(흐느끼는 소리)\n\n\"그래... 이 맛이야.\n쉬는 날마다 아내가 끓여줬어.\n다시는 못 먹을 줄 알았는데...\"\n\n\"고마워, 주인장.\n아직 끝난 건 아니지.\n앞으로도 와도 될까?..\"";

            case EvaluationGrade.Good:
                return "손님 반응: 좋아. 매콤한 김치 국물 덕분에 조금은 정신이 돌아오는 것 같아.";

            case EvaluationGrade.Okay:
                return "손님 반응: 먹을 만하지만 내가 붙잡고 있던 그 집 냄새와는 조금 달라.";

            default:
                return "손님 반응: 미안하네. 지금 내겐 다른 음식이 들어올 자리가 없어.";
        }
    }

    private string BuildCustomerClue(
        EvaluationGrade grade,
        string[] foodTags,
        string[] desiredTags,
        string[] avoidedTags,
        string[] forbiddenIngredients)
    {
        if (grade == EvaluationGrade.Perfect)
        {
            if (currentDayNumber >= 3)
                return "민준아, 여기서는 혼자 버티지 않아도 돼요.\n오늘은 엄마가 차려준 밥상처럼 따뜻한 한 끼부터 천천히 먹어봐요.";

            return currentDayNumber >= 2
                ? "그래요... 어쩔 수 없이 돌아가시는 분들도 많이 계시죠..\n하지만 그 중에서 서아씨 덕분에 다시 일어나신 분들을 생각하고,\n앞으로도 사람들을 위해서 노력하셨으면 좋을 것 같아요."
                : "당연하죠. 언제든지 오세요.\n그때도 김치찌개 끓이고 기다릴게요.\n다치지 마시고, 삶을 포기하지 마세요.";
        }

        string forbidden = string.Join(", ", selectedIngredients.Where(ingredient => forbiddenIngredients.Contains(ingredient)));
        if (!string.IsNullOrEmpty(forbidden))
        {
            if (currentDayNumber >= 3)
                return "다음 단서: 민준이에게는 엄마가 차려주던 된장찌개의 따뜻한 맛이 중요했습니다.";

            return currentDayNumber >= 2
                ? "다음 단서: 속이 편한 국물을 원하는 손님에게는 " + forbidden + " 같은 강한 재료를 피하세요."
                : "다음 단서: 강태수씨에게 중요한 건 재료보다도 김치찌개 특유의 매콤한 냄새였습니다.";
        }

        string riskyTags = string.Join(", ", foodTags.Intersect(avoidedTags));
        if (!string.IsNullOrEmpty(riskyTags))
        {
            if (currentDayNumber >= 3)
                return "다음 단서: " + riskyTags + " 속성이 강하면 민준이가 찾는 집밥의 안정감에서 멀어집니다.";

            return currentDayNumber >= 2
                ? "다음 단서: " + riskyTags + " 속성이 강하면 2일차 손님이 원한 편안한 맛에서 멀어집니다."
                : "다음 단서: " + riskyTags + " 속성이 강하면 강태수씨가 찾던 가족의 맛에서 멀어집니다.";
        }

        if (currentDayNumber >= 3)
            return "다음 단서: 민준이는 엄마의 손맛과 집밥 같은 된장찌개를 찾고 있었습니다.";

        return currentDayNumber >= 2
            ? "다음 단서: 2일차 손님은 따뜻함, 부드러움, 깊은맛을 원했습니다."
            : "다음 단서: 강태수씨는 매움, 김치, 뜨거운 국물에서 버틸 힘을 찾고 있었습니다.";
    }

    private EvaluationResult EvaluateRecipeCombination(RecipeDefinition recipe)
    {
        int score = 0;
        List<string> missedIngredients = new List<string>();

        foreach (string requiredIngredient in recipe.RequiredIngredients)
        {
            if (selectedIngredients.Contains(requiredIngredient))
                score += 2;
            else
                missedIngredients.Add(requiredIngredient);
        }

        string[] foodTags = selectedIngredients
            .Where(ingredientTags.ContainsKey)
            .SelectMany(ingredient => ingredientTags[ingredient])
            .Distinct()
            .ToArray();

        score += foodTags.Count(tag => recipe.PreferredTags.Contains(tag));
        score -= foodTags.Count(tag => recipe.RiskyTags.Contains(tag));
        score += GetPreferenceScore(recipe.Id, foodTags);

        if (missedIngredients.Count > 0)
            score -= missedIngredients.Count * 3;

        EvaluationGrade grade = GetGrade(score, missedIngredients.Count);
        return new EvaluationResult(
            grade,
            score,
            BuildReaction(recipe, grade, foodTags, missedIngredients),
            BuildClue(recipe, grade, foodTags, missedIngredients));
    }

    private int GetPreferenceScore(MenuId menuId, string[] foodTags)
    {
        switch (selectedPreference)
        {
            case CustomerPreference.MildSoup:
                return (menuId == GetTargetRecipeForCurrentDay() ? 3 : 0)
                    + (foodTags.Contains("담백함") ? 2 : 0)
                    + (foodTags.Contains("부드러움") ? 2 : 0)
                    + (foodTags.Contains("구수함") ? 2 : 0)
                    - (foodTags.Contains("자극적") ? 3 : 0)
                    - (foodTags.Contains("매우매움") ? 4 : 0);

            case CustomerPreference.SpicySoup:
                return (menuId == MenuId.KimchiJjigae ? 3 : 0)
                    + (foodTags.Contains("매움") ? 2 : 0)
                    + (foodTags.Contains("해장") ? 1 : 0);

            default:
                return 0;
        }
    }

    private EvaluationGrade GetGrade(int score, int missedIngredientCount)
    {
        if (missedIngredientCount > 0 || score < 4)
            return EvaluationGrade.Poor;

        if (score < 8)
            return EvaluationGrade.Okay;

        if (score < 11)
            return EvaluationGrade.Good;

        return EvaluationGrade.Perfect;
    }

    private string BuildReaction(
        RecipeDefinition recipe,
        EvaluationGrade grade,
        string[] foodTags,
        List<string> missedIngredients)
    {
        if (missedIngredients.Count > 0)
            return "손님 반응: 핵심 재료가 빠져서 음식 방향이 흐려졌어요. 빠진 재료: " + string.Join(", ", missedIngredients);

        switch (grade)
        {
            case EvaluationGrade.Perfect:
                return "손님 반응: 제가 원하던 맛이에요. 말하지 않은 부분까지 잘 짚어줬네요.";

            case EvaluationGrade.Good:
                return "손님 반응: 좋았어요. 단서에 맞는 재료 선택이 느껴졌어요.";

            case EvaluationGrade.Okay:
                return "손님 반응: 먹을 만하지만 제가 말한 컨디션과는 조금 다른 느낌이었어요.";

            default:
                string riskyTags = string.Join(", ", foodTags.Intersect(recipe.RiskyTags));
                return string.IsNullOrEmpty(riskyTags)
                    ? "손님 반응: 맛의 방향을 다시 잡아볼 필요가 있어 보여요."
                    : "손님 반응: " + riskyTags + " 때문에 지금 컨디션에는 부담스러웠어요.";
        }
    }

    private string BuildClue(
        RecipeDefinition recipe,
        EvaluationGrade grade,
        string[] foodTags,
        List<string> missedIngredients)
    {
        if (grade == EvaluationGrade.Perfect)
            return "다음 단서: 손님 말은 '" + GetPreferenceLabel(selectedPreference) + "' 쪽으로 해석하는 것이 가장 자연스러웠습니다.";

        if (missedIngredients.Count > 0)
            return recipe.DisplayName + "의 핵심 재료는 " + string.Join(", ", recipe.RequiredIngredients) + " 입니다.";

        string riskyTags = string.Join(", ", foodTags.Intersect(recipe.RiskyTags));
        if (!string.IsNullOrEmpty(riskyTags))
            return "다음 단서: 이번 손님에게는 " + riskyTags + " 속성이 감점 요소였습니다.";

        return "다음 단서: 손님 컨디션과 단서에 맞춰 메뉴 방향보다 재료 성향을 먼저 보세요.";
    }

    private void UpdateMenuBoard()
    {
        if (menuListText == null)
            return;

        List<string> lines = new List<string>
        {
            "오늘의 메뉴",
            FormatMenuLine(MenuId.KimchiJjigae),
            FormatMenuLine(MenuId.DoenjangJjigae)
        };

        lines.Add(FormatMenuLine(MenuId.SoondubuJjigae));

        menuListText.text = string.Join(Environment.NewLine, lines);
    }

    private string FormatMenuLine(MenuId menuId)
    {
        RecipeDefinition recipe = recipes[menuId];
        bool recommended = IsRecommended(menuId);
        string suffix = CanUseRecipe(menuId) ? string.Empty : "  잠김";
        return (recommended ? recipe.DisplayName + "  추천" : recipe.DisplayName) + suffix;
    }

    private bool IsRecommended(MenuId menuId)
    {
        return (selectedPreference == CustomerPreference.MildSoup && menuId == GetTargetRecipeForCurrentDay())
            || (selectedPreference == CustomerPreference.SpicySoup && menuId == MenuId.KimchiJjigae);
    }

    private string BuildRecipeDetail(RecipeDefinition recipe)
    {
        return "설명\n"
            + recipe.Description
            + "\n\n재료\n"
            + string.Join(" / ", recipe.RequiredIngredients)
            + "\n\n주의\n"
            + string.Join(" / ", recipe.RiskyTags);
    }

    private string BuildCustomerClueGuide()
    {
        if (currentDayNumber >= 3)
        {
            return "손님 단서\n"
                + "혼자 살아남은 학생 민준\n"
                + "엄마가 차려주던 집밥 같은 된장찌개를 떠올리고 있음\n\n"
                + "목표\n"
                + "구수하고 따뜻한 된장찌개가 안전한 기억을 떠올리도록 조리하세요.";
        }

        if (currentDayNumber >= 2)
        {
            return "손님 단서\n"
                + "2일차 손님\n"
                + "따뜻하고 부드러운 순두부찌개와 적당한 고춧가루, 버섯 향을 찾고 있음\n\n"
                + "목표\n"
                + "순두부의 부드러움이 살아나도록 조리하세요.";
        }

        return "손님 단서\n"
            + "지친 소방관 강태수\n"
            + "매콤한 김치찌개 냄새를 찾고 있음\n\n"
            + "목표\n"
            + "김치찌개의 맛과 냄새가 살아나도록 조리하세요.";
    }

    private void UpdateMenuButtons()
    {
        SetActive(recipeButton1, true);
        SetActive(recipeButton2, true);
        SetActive(recipeButton3, true);
        SetInteractable(recipeButton1, true);
        SetInteractable(recipeButton2, true);
        SetInteractable(recipeButton3, true);
        SetButtonLabel(recipeButton1, "환경설정");
        SetButtonLabel(recipeButton2, "레시피");
        SetButtonLabel(recipeButton3, "메모장");

        SetActive(menuButtonBibimbap, true);
        SetActive(menuButtonKimchiJjigae, true);
        SetActive(menuButtonJeyuk, true);
        SetInteractable(menuButtonBibimbap, CanUseRecipe(MenuId.KimchiJjigae));
        SetInteractable(menuButtonKimchiJjigae, CanUseRecipe(MenuId.DoenjangJjigae));
        SetInteractable(menuButtonJeyuk, CanUseRecipe(MenuId.SoondubuJjigae));
        SetButtonLabel(menuButtonBibimbap, FormatMenuButtonLabel(MenuId.KimchiJjigae));
        SetButtonLabel(menuButtonKimchiJjigae, FormatMenuButtonLabel(MenuId.DoenjangJjigae));
        SetButtonLabel(menuButtonJeyuk, FormatMenuButtonLabel(MenuId.SoondubuJjigae));
    }

    private bool CanUseRecipe(MenuId menuId)
    {
        if (menuId == MenuId.KimchiJjigae)
            return true;

        if (menuId == MenuId.DoenjangJjigae)
            return true;

        if (menuId == MenuId.SoondubuJjigae)
            return true;

        return menuId != MenuId.None
            && recipes.ContainsKey(menuId)
            && recipes[menuId].RequiredIngredients.All(IsIngredientUnlocked);
    }

    private Sprite GetMenuSprite(MenuId menuId)
    {
        switch (menuId)
        {
            case MenuId.DoenjangJjigae:
                return bibimbapSprite;

            case MenuId.KimchiJjigae:
                return kimchiJjigaeSprite;

            case MenuId.SoondubuJjigae:
                return jeyukSprite;

            default:
                return null;
        }
    }

    private string GetPreferenceLabel(CustomerPreference preference)
    {
        switch (preference)
        {
            case CustomerPreference.MildSoup:
                return MildChoiceText + " / 속편함";

            case CustomerPreference.SpicySoup:
                return SpicyChoiceText + " / 해장감";

            default:
                return "아직 선택하지 않음";
        }
    }

    private string GetGradeLabel(EvaluationGrade grade)
    {
        switch (grade)
        {
            case EvaluationGrade.Perfect:
                return "대성공";

            case EvaluationGrade.Good:
                return "성공";

            case EvaluationGrade.Okay:
                return "성공";

            default:
                return "실패";
        }
    }

    private string GetGradeBadge(EvaluationGrade grade)
    {
        return GetGradeLabel(grade);
    }

    private void StartNightFlow()
    {
        string targetNightScene = currentDayNumber >= 3
            ? dayThreeNightSceneName
            : currentDayNumber == 2
                ? dayTwoNightSceneName
                : nightSceneName;
        GameFlowState.RequestNightPlay(currentDayNumber);
        int sceneIndex = SceneFlowUtility.FindSceneIndexByName(targetNightScene);
        if (sceneIndex < 0)
        {
            Debug.LogWarning("Night scene not found: " + targetNightScene);
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    private void RefreshUnlockedIngredients()
    {
        unlockedIngredients = new HashSet<string>(GameProgression.GetUnlockedIngredients());
    }

#if UNITY_EDITOR
    private void ApplyEditorPreviewUnlocks()
    {
        if (currentDayNumber >= 2)
        {
            unlockedIngredients.Add("순두부");
            unlockedIngredients.Add("고춧가루");
            unlockedIngredients.Add("계란");
        }
    }
#endif

    private bool IsIngredientUnlocked(string ingredientName)
    {
        return !string.IsNullOrEmpty(ingredientName);
    }

    private bool IsKimchiJjigaeStarterIngredient(string ingredientName)
    {
        return ingredientName == "김치"
            || ingredientName == "돼지고기"
            || ingredientName == "두부"
            || ingredientName == "대파";
    }

    private bool IsDoenjangJjigaeUnlocked()
    {
        return unlockedIngredients.Contains("된장");
    }

    private bool IsDoenjangJjigaeIngredientUnlocked(string ingredientName)
    {
        return ingredientName == "된장"
            || ingredientName == "버섯"
            || ingredientName == "애호박";
    }

    private bool IsSoondubuJjigaeUnlocked()
    {
        return unlockedIngredients.Contains("순두부");
    }

    private bool IsSoondubuJjigaeIngredientUnlocked(string ingredientName)
    {
        return IsSoondubuJjigaeUnlocked()
            && (ingredientName == "순두부"
                || ingredientName == "고춧가루"
                || ingredientName == "계란");
    }

    private string FormatMenuButtonLabel(MenuId menuId)
    {
        RecipeDefinition recipe = recipes[menuId];
        string index = GetMenuDisplayIndex(menuId).ToString("00");
        return CanUseRecipe(menuId)
            ? index + "  " + recipe.DisplayName
            : index + "  잠김  " + recipe.DisplayName;
    }

    private int GetMenuDisplayIndex(MenuId menuId)
    {
        switch (menuId)
        {
            case MenuId.DoenjangJjigae:
                return 2;

            case MenuId.KimchiJjigae:
                return 1;

            case MenuId.SoondubuJjigae:
                return 3;

            default:
                return 0;
        }
    }

    private void UpdateRecipeButton(Button button, TMP_Text label, MenuId menuId)
    {
        string buttonLabel = FormatMenuButtonLabel(menuId);

        if (label != null)
            label.text = buttonLabel;

        SetButtonLabel(button, buttonLabel);

        SetInteractable(button, CanUseRecipe(menuId));
    }

    private void UpdateUnlockSummary()
    {
        string[] newlyUnlocked = GameProgression.ConsumePendingIngredients();

        if (newlyUnlocked.Length > 0)
        {
            SetText(unlockTitleText, "밤에서 추가된 재료");
            return;
        }

        SetText(unlockTitleText, "새로 해금된 재료");
    }

    private void ApplyLayoutPreset()
    {
        if (useStaticDesignerLayout)
            return;

        if (layoutApplied)
            return;

        layoutApplied = true;

        ApplyActivePanelLayout(
            customerPanel != null && customerPanel.activeSelf,
            kitchenPanel != null && kitchenPanel.activeSelf,
            resultPanel != null && resultPanel.activeSelf);
        StretchPanel(menuBoardPanel, new Vector2(0.16f, 0.12f), new Vector2(0.84f, 0.88f));
    }

    private void ApplyViewportFitPreset()
    {
        if (useStaticDesignerLayout)
            return;

        float widthRatio = Mathf.Clamp01(Screen.width / 1920f);
        float heightRatio = Mathf.Clamp01(Screen.height / 1080f);
        float scale = Mathf.Clamp(Mathf.Min(widthRatio, heightRatio), 0.82f, 1f);

        SetPanelScale(customerPanel, scale);
        SetPanelScale(kitchenPanel, scale);
        SetPanelScale(resultPanel, scale);
        SetPanelScale(menuBoardPanel, scale);
    }

    private void ApplyCustomerOrderLayout()
    {
        if (useStaticDesignerLayout)
            return;

        Transform portraitPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerPortraitPanel");
        Transform speechPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel");
        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");

        SetActive(speechPanel, false);
        SetActive(customerSpeechText, false);

        SetRelativeRect(portraitPanel, new Vector2(0.06f, 0.53f), new Vector2(0.33f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(speechPanel, new Vector2(0.36f, 0.53f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(bottomPanel, new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.46f), Vector2.zero, Vector2.zero);
        ApplyPanelTint(portraitPanel != null ? portraitPanel.gameObject : null, new Color32(246, 235, 210, 255));
        ApplyPanelTint(speechPanel != null ? speechPanel.gameObject : null, new Color32(255, 250, 234, 255));
        ApplyPanelTint(bottomPanel != null ? bottomPanel.gameObject : null, new Color32(232, 216, 187, 255));

        Transform dialogueBox = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "DialogueBox");
        SetRelativeRect(dialogueBox, new Vector2(0.04f, 0.22f), new Vector2(0.70f, 0.88f), Vector2.zero, Vector2.zero);
        ApplyPanelTint(dialogueBox != null ? dialogueBox.gameObject : null, new Color32(255, 248, 226, 255));

        SetRelativeRect(menuOpenButton, new Vector2(0.76f, 0.66f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextButton, new Vector2(0.76f, 0.40f), new Vector2(0.96f, 0.60f), Vector2.zero, Vector2.zero);
        SetRelativeRect(goKitchenButton, new Vector2(0.76f, 0.40f), new Vector2(0.96f, 0.60f), Vector2.zero, Vector2.zero);

        SetRelativeRect(choiceGroup, new Vector2(0.04f, 0.05f), new Vector2(0.70f, 0.18f), Vector2.zero, Vector2.zero);
        SetRelativeRect(choiceButtonA, new Vector2(0f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
        SetRelativeRect(choiceButtonB, new Vector2(0.52f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        SetRelativeRect(dialogueText, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);
        SetRelativeRect(customerSpeechText, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(dialogueText, new Vector4(10f, 8f, 10f, 8f));
        ApplyTextBoxPadding(customerSpeechText, new Vector4(10f, 8f, 10f, 8f));
        SetTextAlignment(dialogueText, TextAlignmentOptions.TopLeft);
        SetTextAlignment(customerSpeechText, TextAlignmentOptions.TopLeft);
        ApplyButtonLabelPadding(menuOpenButton);
        ApplyButtonLabelPadding(nextButton);
        ApplyButtonLabelPadding(goKitchenButton);
        ApplyButtonLabelPadding(choiceButtonA);
        ApplyButtonLabelPadding(choiceButtonB);
    }

    private void ApplyKitchenPrepLayout()
    {
        if (useStaticDesignerLayout)
            return;

        Image kitchenBackground = kitchenPanel != null ? kitchenPanel.GetComponent<Image>() : null;
        if (kitchenBackground != null && kitchenBackgroundSprite != null)
        {
            kitchenBackground.sprite = kitchenBackgroundSprite;
            kitchenBackground.color = Color.white;
            kitchenBackground.preserveAspect = false;
        }

        ApplyButtonSprite(cookButton, cookButtonSprite);
        ApplyButtonSprite(recipeButton1, dayOptionButtonSprite);
        ApplyButtonSprite(recipeButton2, dayMenuButtonSprite);
        ApplyButtonSprite(recipeButton3, dayNoteButtonSprite);

        SetRelativeRect(selectedRecipeText, new Vector2(0.30f, 0.75f), new Vector2(0.70f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(backButton, new Vector2(0.82f, 0.34f), new Vector2(0.95f, 0.44f), Vector2.zero, Vector2.zero);
        SetTextAlignment(selectedRecipeText, TextAlignmentOptions.Center);
        SetTextAlignment(ingredientGuideText, TextAlignmentOptions.Center);
        SetTextAlignment(cookingPotHintText, TextAlignmentOptions.Center);
        ApplyButtonLabelPadding(recipeButton1);
        ApplyButtonLabelPadding(recipeButton2);
        ApplyButtonLabelPadding(recipeButton3);
        ApplyButtonLabelPadding(ingredientButton1);
        ApplyButtonLabelPadding(ingredientButton2);
        ApplyButtonLabelPadding(ingredientButton3);
        ApplyButtonLabelPadding(ingredientButton4);
        ApplyButtonLabelPadding(cookButton);
        ApplyButtonLabelPadding(backButton);
        SetTextAlignment(recipeButton1Text, TextAlignmentOptions.Center);
        SetTextAlignment(recipeButton2Text, TextAlignmentOptions.Center);
        SetTextAlignment(recipeButton3Text, TextAlignmentOptions.Center);
        SetTextAlignment(ingredientButton1Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton2Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton3Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton4Text, TextAlignmentOptions.MidlineLeft);
        ApplyIngredientListButtonLayout();
    }

    private void ApplyKitchenArtLayout()
    {
        if (kitchenPanel == null)
            return;

        if (useStaticDesignerLayout)
        {
            ApplyStaticKitchenArtState();
            return;
        }

        ForceKitchenPanelFullscreen();
        StretchPanel(kitchenPanel, Vector2.zero, Vector2.one);
        HideKitchenTemporaryHeader();
        EnsureCookingPotDropZone();
        EnsureIngredientListButtons();

        Image background = kitchenPanel.GetComponent<Image>();
        if (background != null && kitchenBackgroundSprite != null)
        {
            background.sprite = kitchenBackgroundSprite;
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;
        }

        ApplyKitchenCameraBackground();
        ConfigureKitchenSeparatedGraphics();
        ConfigureKitchenIngredientPanel();
        ConfigureKitchenPotAndCookButton();
        ConfigureKitchenSelectedSlots();
        ConfigureKitchenSideButtons();

        SetActive(selectedRecipeText, false);
        SetActive(selectedMenuImage, false);
        SetActive(backButton, false);
        UpdateIngredientSlots();
        ApplyKitchenTextReadability();
    }

    private void ApplyStaticKitchenArtState()
    {
        HideKitchenTemporaryHeader();
        EnsureCookingPotDropZone();
        EnsureIngredientListButtons();

        Image background = kitchenPanel != null ? kitchenPanel.GetComponent<Image>() : null;
        if (background != null && kitchenBackgroundSprite != null)
        {
            background.sprite = kitchenBackgroundSprite;
            background.color = Color.white;
            background.preserveAspect = false;
            background.raycastTarget = false;
        }

        ApplyKitchenCameraBackground();
        ApplyKitchenGraphicSpriteOnly("IngredientPanelGraphic", kitchenIngredientPanelSprite);
        ApplyKitchenGraphicSpriteOnly("SelectedSlotPanelGraphic", kitchenSlotPanelSprite);

        if (cookingPotImage != null && emptyCookingPotSprite != null)
        {
            cookingPotImage.sprite = emptyCookingPotSprite;
            cookingPotImage.color = Color.white;
            cookingPotImage.preserveAspect = true;
            cookingPotImage.raycastTarget = true;
        }

        ApplyButtonSprite(recipeButton1, dayOptionButtonSprite);
        ApplyButtonSprite(recipeButton2, dayMenuButtonSprite);
        ApplyButtonSprite(recipeButton3, dayNoteButtonSprite);
        ApplyButtonSprite(cookButton, cookButtonSprite);
        StyleKitchenSideButtonLabels(recipeButton2, recipeButton2Text, "레시피");
        StyleKitchenSideButtonLabels(recipeButton3, recipeButton3Text, "메모장");

        SetActive(selectedRecipeText, false);
        SetActive(selectedMenuImage, false);
        SetActive(backButton, false);
        UpdateIngredientSlots();
        ApplyKitchenTextReadability();
    }

    private void ApplyKitchenGraphicSpriteOnly(string objectName, Sprite sprite)
    {
        if (kitchenPanel == null || sprite == null)
            return;

        Transform existing = kitchenPanel.transform.Find(objectName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private void ConfigureKitchenSeparatedGraphics()
    {
        if (kitchenPanel == null)
            return;

        Transform oldCookMainGraphic = kitchenPanel.transform.Find("CookMainGraphic");
        if (oldCookMainGraphic != null)
            oldCookMainGraphic.gameObject.SetActive(false);

        Transform old = kitchenPanel.transform.Find("day_CookMain");
        if (old != null)
            old.gameObject.SetActive(false);

        ConfigureKitchenGraphic("IngredientPanelGraphic", kitchenIngredientPanelSprite, new Vector2(0.010f, 0.015f), new Vector2(0.225f, 0.985f), 1);
        ConfigureKitchenGraphic("SelectedSlotPanelGraphic", kitchenSlotPanelSprite, new Vector2(0.240f, 0.005f), new Vector2(0.760f, 0.250f), 2);
    }

    private void ConfigureKitchenGraphic(string objectName, Sprite sprite, Vector2 anchorMin, Vector2 anchorMax, int siblingIndex)
    {
        if (kitchenPanel == null || sprite == null)
            return;

        Transform existing = kitchenPanel.transform.Find(objectName);
        GameObject graphicObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        graphicObject.transform.SetParent(kitchenPanel.transform, false);
        SetRelativeRect(graphicObject.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Image image = graphicObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
        image.raycastTarget = false;
        graphicObject.SetActive(true);
        graphicObject.transform.SetSiblingIndex(Mathf.Min(siblingIndex, kitchenPanel.transform.childCount - 1));
    }

    private void HideKitchenTemporaryHeader()
    {
        SetActive(FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "PaperFrameHeader"), false);
        SetActive(FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "PaperFrameHeaderText"), false);
        SetActive(FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "PaperFrameRule"), false);
    }

    private void ConfigureKitchenIngredientPanel()
    {
        EnsureIngredientScrollView();
        ApplyIngredientScrollViewRect();

        Image panelImage = ingredientScrollView != null ? ingredientScrollView.GetComponent<Image>() : null;
        if (panelImage != null)
        {
            panelImage.sprite = null;
            panelImage.type = Image.Type.Simple;
            panelImage.color = new Color(1f, 1f, 1f, 0f);
            panelImage.raycastTarget = true;
        }

        SetText(ingredientGuideText, "재료");
        SetRelativeRect(ingredientGuideText, new Vector2(0.045f, 0.900f), new Vector2(0.185f, 0.970f), Vector2.zero, Vector2.zero);
        ApplyTextStyle(ingredientGuideText, ResolveSceneFont(), 21f, FontStyles.Bold, Color.white);
        SetTextAlignment(ingredientGuideText, TextAlignmentOptions.Midline);

        if (ingredientScrollRect != null && ingredientScrollRect.viewport != null)
        {
            RectTransform viewport = ingredientScrollRect.viewport;
            SetRelativeRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.color = new Color32(255, 255, 255, 1);
                viewportImage.raycastTarget = false;
            }

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }

        ApplyIngredientListButtonLayout();
        ApplyIngredientScrollViewRect();
        if (ingredientScrollRect != null && ingredientScrollRect.viewport != null)
        {
            RectTransform viewport = ingredientScrollRect.viewport;
            SetRelativeRect(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            Image viewportImage = viewport.GetComponent<Image>();
            if (viewportImage != null)
            {
                viewportImage.color = new Color32(255, 255, 255, 1);
                viewportImage.raycastTarget = false;
            }

            if (viewport.GetComponent<RectMask2D>() == null)
                viewport.gameObject.AddComponent<RectMask2D>();
        }
        DisableGeneratedKitchenScrollbar();
    }

    private void ConfigureKitchenPotAndCookButton()
    {
        SetRelativeRect(cookingPotDropZone, new Vector2(0.390f, 0.430f), new Vector2(0.610f, 0.660f), Vector2.zero, Vector2.zero);
        SetRelativeRect(cookingPotHintText, new Vector2(0.360f, 0.470f), new Vector2(0.640f, 0.635f), Vector2.zero, Vector2.zero);
        ResetRectScale(cookingPotDropZone);
        ResetRectScale(cookingPotHintText);

        if (cookingPotImage != null)
        {
            cookingPotImage.sprite = emptyCookingPotSprite;
            cookingPotImage.color = Color.white;
            cookingPotImage.preserveAspect = true;
            cookingPotImage.raycastTarget = true;
        }

        SetRelativeRect(cookButton, new Vector2(0.405f, 0.285f), new Vector2(0.615f, 0.380f), Vector2.zero, Vector2.zero);
        ResetRectScale(cookButton);
        ApplyButtonSprite(cookButton, cookButtonSprite);
        SetButtonLabel(cookButton, "조리하기");

        TMP_Text cookLabel = cookButton != null ? cookButton.GetComponentInChildren<TMP_Text>(true) : null;
        ApplyButtonTextStyle(cookLabel, ResolveSceneFont(), 25f, FontStyles.Bold);
        if (cookLabel != null)
        {
            cookLabel.alignment = TextAlignmentOptions.Midline;
            cookLabel.margin = Vector4.zero;
            cookLabel.color = Color.white;
        }

        if (cookButton != null)
            cookButton.transform.SetAsLastSibling();
    }

    private void ConfigureKitchenSelectedSlots()
    {
        RectTransform slotsPanel = EnsureKitchenSlotsPanel();
        SetRelativeRect(slotsPanel, new Vector2(0.240f, 0.005f), new Vector2(0.760f, 0.250f), Vector2.zero, Vector2.zero);

        HideLegacySelectedSlotTexts();
        EnsureSelectedSlotInteractionLayer();
        SetRelativeRect(selectedSlotInteractionLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        ConfigureFixedSelectedSlot(0, new Vector2(0.040f, 0.120f), new Vector2(0.235f, 0.870f));
        ConfigureFixedSelectedSlot(1, new Vector2(0.280f, 0.120f), new Vector2(0.475f, 0.870f));
        ConfigureFixedSelectedSlot(2, new Vector2(0.525f, 0.120f), new Vector2(0.720f, 0.870f));
        ConfigureFixedSelectedSlot(3, new Vector2(0.765f, 0.120f), new Vector2(0.960f, 0.870f));
    }

    private void ConfigureKitchenSideButtons()
    {
        SetRelativeRect(recipeButton1, new Vector2(0.925f, 0.860f), new Vector2(0.985f, 0.985f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeButton2, new Vector2(0.905f, 0.535f), new Vector2(0.980f, 0.705f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeButton3, new Vector2(0.905f, 0.365f), new Vector2(0.980f, 0.535f), Vector2.zero, Vector2.zero);
        ResetRectScale(recipeButton1);
        ResetRectScale(recipeButton2);
        ResetRectScale(recipeButton3);

        ApplyButtonSprite(recipeButton1, dayOptionButtonSprite);
        ApplyButtonSprite(recipeButton2, dayMenuButtonSprite);
        ApplyButtonSprite(recipeButton3, dayNoteButtonSprite);
        HideKitchenSideButtonChildLabels(recipeButton1);
        HideKitchenSideButtonChildLabels(recipeButton2);
        HideKitchenSideButtonChildLabels(recipeButton3);

        if (recipeButton1 != null)
            recipeButton1.transform.SetAsLastSibling();
        if (recipeButton2 != null)
            recipeButton2.transform.SetAsLastSibling();
        if (recipeButton3 != null)
            recipeButton3.transform.SetAsLastSibling();

        recipeButton1Text = null;
        recipeButton2Text = EnsureKitchenSideButtonOverlayLabel(
            "KitchenRecipeButtonLabel",
            "레시피",
            new Vector2(0.905f, 0.548f),
            new Vector2(0.980f, 0.598f));
        recipeButton3Text = EnsureKitchenSideButtonOverlayLabel(
            "KitchenMemoButtonLabel",
            "메모장",
            new Vector2(0.905f, 0.378f),
            new Vector2(0.980f, 0.428f));
    }

    private void DisableGeneratedKitchenScrollbar()
    {
        if (ingredientScrollRect == null || ingredientScrollView == null)
            return;

        Scrollbar scrollbar = ingredientScrollRect.verticalScrollbar;
        if (scrollbar != null)
            scrollbar.gameObject.SetActive(false);

        Transform generatedScrollbar = ingredientScrollView.transform.Find("Scrollbar");
        if (generatedScrollbar != null)
            generatedScrollbar.gameObject.SetActive(false);

        ingredientScrollRect.verticalScrollbar = null;

        ingredientScrollRect.horizontal = false;
        ingredientScrollRect.vertical = true;
        ingredientScrollRect.movementType = ScrollRect.MovementType.Clamped;
        ingredientScrollRect.scrollSensitivity = 32f;
        ingredientScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
        ingredientScrollRect.verticalScrollbarSpacing = 0f;
    }

    private RectTransform EnsureKitchenSlotsPanel()
    {
        Transform existing = kitchenPanel != null ? kitchenPanel.transform.Find("SelectedIngredientSlotPanel") : null;
        GameObject panelObject = existing != null
            ? existing.gameObject
            : new GameObject("SelectedIngredientSlotPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        panelObject.transform.SetParent(kitchenPanel.transform, false);
        RectTransform rect = panelObject.GetComponent<RectTransform>();

        Image image = panelObject.GetComponent<Image>();
        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;
        if (!useStaticDesignerLayout || !Application.isPlaying)
            panelObject.transform.SetAsLastSibling();
        return rect;
    }

    private void HideLegacySelectedSlotTexts()
    {
        SetActive(slot1Text, false);
        SetActive(slot2Text, false);
        SetActive(slot3Text, false);
    }

    private void EnsureSelectedSlotInteractionLayer()
    {
        if (kitchenPanel == null)
            return;

        RectTransform slotsPanel = EnsureKitchenSlotsPanel();
        Transform existing = slotsPanel.transform.Find("SelectedSlotInteractionLayer");
        GameObject layerObject = existing != null
            ? existing.gameObject
            : new GameObject("SelectedSlotInteractionLayer", typeof(RectTransform));

        layerObject.transform.SetParent(slotsPanel, false);
        selectedSlotInteractionLayer = layerObject.GetComponent<RectTransform>();
        DisableLayoutDrivenResize(layerObject);
        if (!useStaticDesignerLayout || !Application.isPlaying)
        {
            SetRelativeRect(selectedSlotInteractionLayer, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            layerObject.transform.SetAsLastSibling();
        }

        for (int i = 0; i < MaxIngredientSlots; i++)
            EnsureFixedSelectedSlot(i);
    }

    private void ConfigureFixedSelectedSlot(int index, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (index < 0 || index >= MaxIngredientSlots)
            return;

        EnsureFixedSelectedSlot(index);
        Button slotButton = selectedSlotButtons[index];
        if (slotButton == null)
            return;

        RectTransform slotRect = slotButton.transform.parent.GetComponent<RectTransform>();
        SetRelativeRect(slotRect, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        Transform clickArea = slotButton.transform;
        SetRelativeRect(clickArea, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        Image iconImage = selectedSlotIconImages[index];
        if (iconImage != null)
        {
            RectTransform iconRect = iconImage.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(SelectedSlotIconSize, SelectedSlotIconSize);
            iconImage.type = Image.Type.Simple;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
            iconImage.color = Color.white;
        }
    }

    private void EnsureFixedSelectedSlot(int index)
    {
        if (selectedSlotInteractionLayer == null || index < 0 || index >= MaxIngredientSlots)
            return;

        string slotName = "SelectedSlot_" + index;
        Transform slotTransform = selectedSlotInteractionLayer.Find(slotName);
        GameObject slotObject = slotTransform != null
            ? slotTransform.gameObject
            : new GameObject(slotName, typeof(RectTransform));
        slotObject.transform.SetParent(selectedSlotInteractionLayer, false);
        DisableLayoutDrivenResize(slotObject);

        Transform clickTransform = slotObject.transform.Find("ClickArea");
        GameObject clickObject = clickTransform != null
            ? clickTransform.gameObject
            : new GameObject("ClickArea", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        clickObject.transform.SetParent(slotObject.transform, false);
        DisableLayoutDrivenResize(clickObject);

        Image clickImage = clickObject.GetComponent<Image>();
        clickImage.sprite = null;
        clickImage.type = Image.Type.Simple;
        clickImage.color = new Color(1f, 1f, 1f, 0f);
        clickImage.raycastTarget = true;

        Button button = clickObject.GetComponent<Button>();
        button.targetGraphic = clickImage;
        button.onClick.RemoveAllListeners();
        int capturedIndex = index;
        button.onClick.AddListener(() => RemoveSelectedIngredientAtSlot(capturedIndex));
        selectedSlotButtons[index] = button;

        Transform iconTransform = slotObject.transform.Find("IngredientIcon");
        GameObject iconObject = iconTransform != null
            ? iconTransform.gameObject
            : new GameObject("IngredientIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(slotObject.transform, false);
        DisableLayoutDrivenResize(iconObject);
        selectedSlotIconImages[index] = iconObject.GetComponent<Image>();
        iconObject.transform.SetAsLastSibling();
    }

    private void RefreshFixedSelectedSlot(int index)
    {
        if (index < 0 || index >= MaxIngredientSlots)
            return;

        EnsureFixedSelectedSlot(index);

        bool hasIngredient = index < selectedIngredients.Count;
        Image iconImage = selectedSlotIconImages[index];
        if (iconImage != null)
        {
            iconImage.sprite = hasIngredient ? GetIngredientSprite(selectedIngredients[index]) : null;
            iconImage.gameObject.SetActive(hasIngredient && iconImage.sprite != null);
        }

        Button button = selectedSlotButtons[index];
        if (button != null)
            button.interactable = hasIngredient && !cookingGaugeActive;
    }

    private void StyleKitchenSideButtonLabel(TMP_Text label, string text)
    {
        if (label == null)
            return;

        label.text = text;
        label.gameObject.SetActive(!string.IsNullOrWhiteSpace(text));
        if (string.IsNullOrWhiteSpace(text))
            return;

        label.color = kitchenSideButtonLabelColor;
        label.fontSize = 17f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11f;
        label.fontSizeMax = 17f;
        label.alignment = TextAlignmentOptions.Center;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
        label.outlineColor = kitchenSideButtonLabelOutlineColor;
        label.outlineWidth = kitchenSideButtonLabelOutlineWidth;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0.04f, 0.05f);
        rect.anchorMax = new Vector2(0.96f, 0.35f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        Shadow shadow = label.GetComponent<Shadow>();
        if (shadow == null)
            shadow = label.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0.05f, 0.04f, 0.05f, 0.82f);
        shadow.effectDistance = new Vector2(1.2f, -1.2f);
        shadow.useGraphicAlpha = true;
    }

    private TMP_Text StyleKitchenSideButtonLabels(Button button, TMP_Text primaryLabel, string text)
    {
        TMP_Text[] labels = button != null
            ? button.GetComponentsInChildren<TMP_Text>(true)
            : Array.Empty<TMP_Text>();

        TMP_Text targetLabel = primaryLabel;
        if (targetLabel == null && labels.Length > 0)
            targetLabel = labels[0];
        if (targetLabel == null && button != null && !string.IsNullOrWhiteSpace(text))
            targetLabel = CreateKitchenSideButtonLabel(button);

        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null || label == targetLabel)
                continue;

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }

        StyleKitchenSideButtonLabel(targetLabel, text);
        return targetLabel;
    }

    private void HideKitchenSideButtonChildLabels(Button button)
    {
        if (button == null)
            return;

        TMP_Text[] labels = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            TMP_Text label = labels[i];
            if (label == null)
                continue;

            label.text = string.Empty;
            label.gameObject.SetActive(false);
        }
    }

    private TMP_Text EnsureKitchenSideButtonOverlayLabel(string objectName, string text, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (kitchenPanel == null)
            return null;

        Transform existing = kitchenPanel.transform.Find(objectName);
        GameObject labelObject = existing != null
            ? existing.gameObject
            : new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));

        labelObject.transform.SetParent(kitchenPanel.transform, false);
        labelObject.transform.SetAsLastSibling();

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        TMP_FontAsset font = ResolveSceneFont();
        if (font != null)
            label.font = font;

        StyleKitchenSideButtonLabel(label, text);
        SetRelativeRect(label, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        label.fontSize = 15f;
        label.fontSizeMin = 10f;
        label.fontSizeMax = 15f;
        label.raycastTarget = false;
        return label;
    }

    private TMP_Text CreateKitchenSideButtonLabel(Button button)
    {
        if (button == null)
            return null;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        labelObject.transform.SetParent(button.transform, false);
        labelObject.transform.SetAsLastSibling();

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        TMP_FontAsset font = ResolveSceneFont();
        if (font != null)
            label.font = font;

        label.raycastTarget = false;
        return label;
    }

    private void ApplyKitchenCameraBackground()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
            return;

        mainCamera.backgroundColor = kitchenCameraBackgroundColor;
    }

    private void ApplyResultLayout()
    {
        if (useStaticDesignerLayout)
            return;

        StretchPanel(resultPanel, new Vector2(0.23f, 0.12f), new Vector2(0.77f, 0.88f));
        SetRelativeRect(foodImage, new Vector2(0.30f, 0.53f), new Vector2(0.70f, 0.76f), Vector2.zero, Vector2.zero);
        SetRelativeRect(resultText, new Vector2(0.18f, 0.405f), new Vector2(0.82f, 0.505f), Vector2.zero, Vector2.zero);
        SetRelativeRect(reactionText, new Vector2(0.12f, 0.225f), new Vector2(0.88f, 0.365f), Vector2.zero, Vector2.zero);
        SetRelativeRect(clueText, new Vector2(0.18f, 0.475f), new Vector2(0.82f, 0.525f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockTitleText, new Vector2(0.20f, 0.17f), new Vector2(0.80f, 0.21f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextDayButton, new Vector2(0.36f, 0.055f), new Vector2(0.64f, 0.145f), Vector2.zero, Vector2.zero);

        ApplyPanelTint(foodImage != null ? foodImage.gameObject : null, new Color32(255, 248, 228, 255));
        ApplyTextBoxPadding(resultText, new Vector4(8f, 4f, 8f, 4f));
        ApplyTextBoxPadding(reactionText, new Vector4(12f, 8f, 12f, 8f));
        ApplyTextBoxPadding(clueText, new Vector4(12f, 8f, 12f, 8f));
        SetTextAlignment(resultText, TextAlignmentOptions.Midline);
        SetTextAlignment(reactionText, TextAlignmentOptions.Midline);
        SetTextAlignment(clueText, TextAlignmentOptions.Midline);
        SetTextAlignment(unlockTitleText, TextAlignmentOptions.Midline);
        ApplyButtonLabelPadding(nextDayButton);
    }

    private void ApplyResultArtLayout(CookingResultData result)
    {
        if (resultPanel == null)
            return;

        if (useStaticDesignerLayout)
        {
            ApplyStaticResultArtState(result);
            return;
        }

        StretchPanel(resultPanel, Vector2.zero, Vector2.one);
        HideResultTemporaryChrome();

        Image panelImage = resultPanel.GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.preserveAspect = false;
            panelImage.raycastTarget = false;
        }

        HideResultExtraImages();

        SetRelativeRect(resultText, new Vector2(0.355f, 0.625f), new Vector2(0.645f, 0.720f), Vector2.zero, Vector2.zero);
        SetRelativeRect(reactionText, new Vector2(0.145f, 0.355f), new Vector2(0.500f, 0.530f), Vector2.zero, Vector2.zero);
        SetRelativeRect(clueText, new Vector2(0.525f, 0.355f), new Vector2(0.855f, 0.530f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextDayButton, new Vector2(0.425f, 0.090f), new Vector2(0.575f, 0.165f), Vector2.zero, Vector2.zero);

        bool artHasResultSprite = result != null && result.ResultSprite != null && result.ResultSprite != emptyCookingPotSprite;
        if (foodImage != null)
        {
            SetRelativeRect(foodImage, new Vector2(0.430f, 0.520f), new Vector2(0.570f, 0.625f), Vector2.zero, Vector2.zero);
            foodImage.gameObject.SetActive(artHasResultSprite);
            foodImage.color = Color.white;
            foodImage.preserveAspect = true;
            foodImage.raycastTarget = false;
        }

        StyleResultText(resultText, 32f, FontStyles.Bold, new Color32(246, 236, 218, 255), TextAlignmentOptions.Center);
        StyleResultText(reactionText, 17f, FontStyles.Normal, new Color32(246, 236, 218, 255), TextAlignmentOptions.TopLeft);
        StyleResultText(clueText, 17f, FontStyles.Bold, new Color32(246, 236, 218, 255), TextAlignmentOptions.TopLeft);

        TMP_Text buttonText = nextDayButton != null ? nextDayButton.GetComponentInChildren<TMP_Text>(true) : null;
        StyleResultText(buttonText, 18f, FontStyles.Bold, new Color32(246, 236, 218, 255), TextAlignmentOptions.Center);

        SetActive(unlockTitleText, false);

        if (nextDayButton != null)
            nextDayButton.transform.SetAsLastSibling();
    }

    private void ApplyStaticResultArtState(CookingResultData result)
    {
        StretchPanel(resultPanel, Vector2.zero, Vector2.one);
        RemoveDeletedResultUiObjects();

        Image panelImage = resultPanel.GetComponent<Image>();
        if (panelImage != null)
            panelImage.raycastTarget = false;

        HideResultTemporaryChrome();
        DisableOpaqueLegacyResultImages();
        bool staticHasResultSprite = result != null && result.ResultSprite != null && result.ResultSprite != emptyCookingPotSprite;
        if (foodImage != null)
        {
            foodImage.gameObject.SetActive(staticHasResultSprite);
            foodImage.color = Color.white;
            foodImage.preserveAspect = true;
            foodImage.raycastTarget = false;
        }

        StyleResultText(resultText, 34f, FontStyles.Bold, new Color32(246, 236, 218, 255), TextAlignmentOptions.Center);
        StyleResultText(reactionText, 18f, FontStyles.Normal, new Color32(246, 236, 218, 255), TextAlignmentOptions.TopLeft);
        StyleResultText(clueText, 18f, FontStyles.Normal, new Color32(246, 236, 218, 255), TextAlignmentOptions.TopLeft);
        StyleResultText(unlockTitleText, 20f, FontStyles.Bold, new Color32(226, 116, 103, 255), TextAlignmentOptions.TopLeft);

        TMP_Text buttonText = nextDayButton != null ? nextDayButton.GetComponentInChildren<TMP_Text>(true) : null;
        StyleResultText(buttonText, 18f, FontStyles.Bold, new Color32(246, 236, 218, 255), TextAlignmentOptions.Center);

        ApplyButtonSprite(nextDayButton, resultOkUiSprite);
        if (nextDayButton != null)
            nextDayButton.transform.SetAsLastSibling();
    }

    private void EnsureStaticResultPanelVisible(string objectName)
    {
        Transform target = resultPanel != null ? resultPanel.transform.Find(objectName) : null;
        if (target == null)
            return;

        target.gameObject.SetActive(true);
        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color32(31, 42, 67, 145);
            image.raycastTarget = false;
        }
    }

    private void DisableOpaqueLegacyResultImages()
    {
        if (resultPanel == null)
            return;

        Image rootImage = resultPanel.GetComponent<Image>();
        Image[] images = resultPanel.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null || image == rootImage)
                continue;

            Transform imageTransform = image.transform;
            if (foodImage != null && (imageTransform == foodImage.transform || imageTransform.IsChildOf(foodImage.transform)))
                continue;

            if (nextDayButton != null && (imageTransform == nextDayButton.transform || imageTransform.IsChildOf(nextDayButton.transform)))
                continue;

            if (image.color.a > 0.85f && image.sprite == null)
                image.gameObject.SetActive(false);
        }
    }

    private void HideResultExtraImages()
    {
        if (resultPanel == null)
            return;

        Image rootImage = resultPanel.GetComponent<Image>();
        Image[] images = resultPanel.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null || image == rootImage)
                continue;

            Transform imageTransform = image.transform;
            if (foodImage != null && (imageTransform == foodImage.transform || imageTransform.IsChildOf(foodImage.transform)))
                continue;

            if (nextDayButton != null && (imageTransform == nextDayButton.transform || imageTransform.IsChildOf(nextDayButton.transform)))
                continue;

            image.gameObject.SetActive(false);
        }
    }

    private void HideResultTemporaryChrome()
    {
        Transform root = resultPanel != null ? resultPanel.transform : null;
        SetActive(FindChildRecursive(root, "PaperFrameHeader"), false);
        SetActive(FindChildRecursive(root, "PaperFrameHeaderText"), false);
        SetActive(FindChildRecursive(root, "PaperFrameRule"), false);
        SetActive(FindChildRecursive(root, "SectionLabel_ResultFood"), false);
        SetActive(FindChildRecursive(root, "SectionLabel_ResultReaction"), false);
        SetActive(FindChildRecursive(root, "SectionLabel_ResultClue"), false);
        SetActive(FindChildRecursive(root, "SectionLabel_ResultUnlock"), false);
        SetActive(FindChildRecursive(root, "SectionRule_ResultLeft"), false);
        SetActive(FindChildRecursive(root, "SectionRule_ResultRight"), false);
    }

    private void StyleResultText(TMP_Text text, float fontSize, FontStyles style, Color color, TextAlignmentOptions alignment)
    {
        if (text == null)
            return;

        if (ShouldPreserveInspectorResultText(text))
        {
            text.gameObject.SetActive(true);
            return;
        }

        TMP_FontAsset font = ResolveSceneFont();
        if (font != null)
            text.font = font;

        text.gameObject.SetActive(true);
        text.color = color;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.enableAutoSizing = true;
        text.fontSizeMin = Mathf.Max(10f, fontSize - 6f);
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.margin = Vector4.zero;
        text.outlineColor = new Color32(34, 27, 31, 255);
        text.outlineWidth = 0.12f;
        text.raycastTarget = false;
    }

    private void ApplyMenuBoardLayout()
    {
        if (useStaticDesignerLayout)
            return;

        if (menuBoardPanel == null)
            return;

        StretchPanel(menuBoardPanel, new Vector2(0.20f, 0.12f), new Vector2(0.80f, 0.88f));
        ApplyPanelTint(menuBoardPanel, new Color32(54, 39, 27, 215));

        Transform boardBackground = FindChildRecursive(menuBoardPanel.transform, "BoardBackground");
        SetRelativeRect(boardBackground, new Vector2(0.03f, 0.05f), new Vector2(0.97f, 0.90f), Vector2.zero, Vector2.zero);
        ApplyPanelTint(boardBackground != null ? boardBackground.gameObject : null, new Color32(255, 246, 224, 255));

        Transform leftMenuArea = FindChildRecursive(menuBoardPanel.transform, "LeftMenuArea");
        Transform rightDetailArea = FindChildRecursive(menuBoardPanel.transform, "RightDetailArea");
        SetRelativeRect(leftMenuArea, new Vector2(0.05f, 0.14f), new Vector2(0.35f, 0.82f), Vector2.zero, Vector2.zero);
        SetRelativeRect(rightDetailArea, new Vector2(0.40f, 0.14f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);

        ApplyPanelTint(leftMenuArea != null ? leftMenuArea.gameObject : null, new Color32(247, 232, 198, 255));
        ApplyPanelTint(rightDetailArea != null ? rightDetailArea.gameObject : null, new Color32(255, 250, 235, 255));

        SetRelativeRect(menuButtonBibimbap, new Vector2(0.08f, 0.70f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        SetRelativeRect(menuButtonKimchiJjigae, new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.65f), Vector2.zero, Vector2.zero);
        SetRelativeRect(menuButtonJeyuk, new Vector2(0.08f, 0.24f), new Vector2(0.92f, 0.42f), Vector2.zero, Vector2.zero);
        SetRelativeRect(closeButton, new Vector2(0.76f, 0.04f), new Vector2(0.95f, 0.12f), Vector2.zero, Vector2.zero);

        SetRelativeRect(recipeTitleText, new Vector2(0.06f, 0.78f), new Vector2(0.94f, 0.94f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeDetailText, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.74f), Vector2.zero, Vector2.zero);

        ApplyTextStyle(recipeTitleText, ResolveSceneFont(), 26f, FontStyles.Bold, WarningTextTint);
        ApplyTextStyle(recipeDetailText, ResolveSceneFont(), 19f, FontStyles.Normal, SecondaryTextTint);
        ApplyTextBoxPadding(recipeTitleText, new Vector4(12f, 4f, 12f, 4f));
        ApplyTextBoxPadding(recipeDetailText, new Vector4(12f, 10f, 12f, 10f));

        ApplyButtonLabelPadding(menuButtonBibimbap);
        ApplyButtonLabelPadding(menuButtonKimchiJjigae);
        ApplyButtonLabelPadding(menuButtonJeyuk);
        ApplyButtonLabelPadding(closeButton);
    }

    private void ApplyIndieUiPolish()
    {
        if (useStaticDesignerLayout)
            return;

        if (polishApplied)
            return;

        polishApplied = true;
        TMP_FontAsset font = ResolveSceneFont();

        ApplyTextRhythm(nameText, -1f, 2f, 0f);
        ApplyTextRhythm(customerSpeechText, -2f, 5f, 2f);
        ApplyTextRhythm(dialogueText, -2f, 5f, 2f);
        ApplyTextRhythm(recipeDetailText, -1.5f, 6f, 4f);
        ApplyTextRhythm(ingredientGuideText, -1.5f, 4f, 2f);
        ApplyTextRhythm(cookingPotHintText, -1f, 3f, 0f);
        ApplyTextRhythm(resultText, -1f, 4f, 2f);
        ApplyTextRhythm(reactionText, -1f, 3f, 2f);
        ApplyTextRhythm(clueText, -1f, 3f, 1f);

        ApplyButtonRhythm(choiceButtonAText);
        ApplyButtonRhythm(choiceButtonBText);
        ApplyButtonRhythm(recipeButton1Text);
        ApplyButtonRhythm(recipeButton2Text);
        ApplyButtonRhythm(recipeButton3Text);
        ApplyButtonRhythm(ingredientButton1Text);
        ApplyButtonRhythm(ingredientButton2Text);
        ApplyButtonRhythm(ingredientButton3Text);
        ApplyButtonRhythm(ingredientButton4Text);

        Transform portraitPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerPortraitPanel");
        Transform speechPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel");
        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");

        AddUiLabel(portraitPanel, "SectionLabel_Customer", "방문 손님", new Vector2(0.08f, 0.84f), new Vector2(0.92f, 0.94f), font, 15f, MutedTextTint);
        AddUiLabel(speechPanel, "SectionLabel_Speech", "손님 메모", new Vector2(0.06f, 0.82f), new Vector2(0.94f, 0.94f), font, 15f, MutedTextTint);
        AddUiDivider(speechPanel, "SectionRule_Speech", new Vector2(0.06f, 0.79f), new Vector2(0.94f, 0.805f));
        AddUiLabel(bottomPanel, "SectionLabel_Order", "주문 상담", new Vector2(0.04f, 0.86f), new Vector2(0.70f, 0.96f), font, 15f, MutedTextTint);
        AddUiDivider(bottomPanel, "SectionRule_Order", new Vector2(0.04f, 0.83f), new Vector2(0.70f, 0.845f));

        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Recipes", "선택 메뉴", new Vector2(0.30f, 0.84f), new Vector2(0.70f, 0.89f), font, 16f, SecondaryTextTint);
        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Pot", "뚝배기", new Vector2(0.36f, 0.72f), new Vector2(0.66f, 0.77f), font, 16f, SecondaryTextTint);
        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Shelf", "재료 목록", new Vector2(0.05f, 0.89f), new Vector2(0.27f, 0.94f), font, 16f, SecondaryTextTint);

        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultFood", "완성 음식", new Vector2(0.07f, 0.79f), new Vector2(0.37f, 0.84f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultReaction", "손님 반응", new Vector2(0.08f, 0.54f), new Vector2(0.51f, 0.59f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultClue", "플레이어 대화", new Vector2(0.56f, 0.54f), new Vector2(0.91f, 0.59f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultUnlock", "해금 기록", new Vector2(0.56f, 0.31f), new Vector2(0.91f, 0.36f), font, 15f, MutedTextTint);
        AddUiDivider(resultPanel != null ? resultPanel.transform : null, "SectionRule_ResultLeft", new Vector2(0.08f, 0.535f), new Vector2(0.51f, 0.545f));
        AddUiDivider(resultPanel != null ? resultPanel.transform : null, "SectionRule_ResultRight", new Vector2(0.56f, 0.535f), new Vector2(0.91f, 0.545f));

        ApplyTextBlockBackdrop(reactionText);
        ApplyTextBlockBackdrop(clueText);
    }

    private void ApplyTextPlacementPolish()
    {
        if (useStaticDesignerLayout)
            return;

        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");
        Transform speechPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel");

        SetActive(speechPanel, false);
        SetActive(customerSpeechText, false);

        SetRelativeRect(portraitImage, new Vector2(0.24f, 0.35f), new Vector2(0.76f, 0.74f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nameText, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
        SetTextAlignment(nameText, TextAlignmentOptions.Center);

        SetRelativeRect(customerSpeechText, new Vector2(0.09f, 0.16f), new Vector2(0.91f, 0.74f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(customerSpeechText, new Vector4(10f, 8f, 10f, 8f));
        SetTextAlignment(customerSpeechText, TextAlignmentOptions.TopLeft);

        Transform dialogueBox = FindChildRecursive(bottomPanel, "DialogueBox");
        SetRelativeRect(dialogueBox, new Vector2(0.04f, 0.22f), new Vector2(0.70f, 0.88f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dialogueText, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.90f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(dialogueText, new Vector4(10f, 8f, 10f, 8f));
        SetTextAlignment(dialogueText, TextAlignmentOptions.TopLeft);

        SetRelativeRect(choiceGroup, new Vector2(0.04f, 0.06f), new Vector2(0.70f, 0.18f), Vector2.zero, Vector2.zero);
        SetRelativeRect(menuOpenButton, new Vector2(0.77f, 0.65f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextButton, new Vector2(0.77f, 0.42f), new Vector2(0.95f, 0.59f), Vector2.zero, Vector2.zero);
        SetRelativeRect(goKitchenButton, new Vector2(0.77f, 0.42f), new Vector2(0.95f, 0.59f), Vector2.zero, Vector2.zero);

        SetRelativeRect(selectedRecipeText, new Vector2(0.30f, 0.75f), new Vector2(0.70f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(ingredientGuideText, new Vector2(0.05f, 0.80f), new Vector2(0.27f, 0.89f), Vector2.zero, Vector2.zero);
        SetRelativeRect(backButton, new Vector2(0.82f, 0.34f), new Vector2(0.95f, 0.44f), Vector2.zero, Vector2.zero);
        SetTextAlignment(selectedRecipeText, TextAlignmentOptions.Center);
        SetTextAlignment(ingredientGuideText, TextAlignmentOptions.Center);
        SetTextAlignment(cookingPotHintText, TextAlignmentOptions.Center);
        ApplyIngredientListButtonLayout();

        SetRelativeRect(resultText, new Vector2(0.18f, 0.405f), new Vector2(0.82f, 0.505f), Vector2.zero, Vector2.zero);
        SetRelativeRect(reactionText, new Vector2(0.12f, 0.225f), new Vector2(0.88f, 0.365f), Vector2.zero, Vector2.zero);
        SetRelativeRect(clueText, new Vector2(0.18f, 0.475f), new Vector2(0.82f, 0.525f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockTitleText, new Vector2(0.20f, 0.17f), new Vector2(0.80f, 0.21f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(reactionText, new Vector4(10f, 6f, 10f, 6f));
        ApplyTextBoxPadding(clueText, new Vector4(10f, 6f, 10f, 6f));
        SetTextAlignment(resultText, TextAlignmentOptions.Midline);
        SetTextAlignment(reactionText, TextAlignmentOptions.Midline);
        SetTextAlignment(clueText, TextAlignmentOptions.Midline);
        SetTextAlignment(unlockTitleText, TextAlignmentOptions.Midline);
    }

    private void ApplyDayArtSceneLayout()
    {
        if (useStaticDesignerLayout)
            return;

        if (!useDayArtLayout)
            return;

        StretchPanel(customerPanel, Vector2.zero, Vector2.one);

        Transform portraitPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerPortraitPanel");
        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");
        Transform dialogueBox = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "DialogueBox");
        Transform speechPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel");

        MakeImagesTransparentInChildren(customerPanel);
        MakeLegacyTextsTransparentInChildren(customerPanel);

        SetActive(menuBoardPanel, false);
        SetActive(speechPanel, false);
        SetActive(customerSpeechText, false);
        SetActive(customerInfoText, true);
        SetActive(menuListText, false);
        SetActive(nameText, true);
        SetActive(dialogueText, true);

        SetRelativeRect(portraitPanel, new Vector2(0.155f, 0.455f), new Vector2(0.365f, 0.825f), Vector2.zero, Vector2.zero);
        SetRelativeRect(portraitImage, new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.98f), Vector2.zero, Vector2.zero);

        SetRelativeRect(bottomPanel, new Vector2(0.027f, 0.045f), new Vector2(0.973f, 0.345f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dialogueBox, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        SetRelativeRect(customerInfoText, new Vector2(0.175f, 0.565f), new Vector2(0.345f, 0.735f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nameText, new Vector2(0.068f, 0.250f), new Vector2(0.310f, 0.305f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dialogueText, new Vector2(0.035f, 0.12f), new Vector2(0.965f, 0.80f), Vector2.zero, Vector2.zero);
        SetRelativeRect(choiceGroup, new Vector2(0.04f, 0.04f), new Vector2(0.50f, 0.23f), Vector2.zero, Vector2.zero);

        SetRelativeRect(menuOpenButton, new Vector2(0.903f, 0.625f), new Vector2(0.974f, 0.790f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextButton, new Vector2(0.027f, 0.045f), new Vector2(0.973f, 0.345f), Vector2.zero, Vector2.zero);
        SetRelativeRect(goKitchenButton, new Vector2(0.795f, 0.080f), new Vector2(0.945f, 0.150f), Vector2.zero, Vector2.zero);

        SetButtonImageColor(menuOpenButton, Color.white);
        MakeButtonTransparent(nextButton);
        ApplyDayArtGoKitchenButtonStyle();
        ApplyDayArtTextPlacement();
        ApplyTextBoxPadding(dialogueText, new Vector4(0f, 0f, 0f, 0f));
        SetTextAlignment(dialogueText, TextAlignmentOptions.TopLeft);

        if (portraitImage != null)
        {
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;
        }
    }

    private void ApplyDayArtTextPlacement()
    {
        if (dayResponseArtView == null)
            return;

        SetActive(dayResponseArtView.npcInfoTitleText, true);
        SetActive(dayResponseArtView.npcInfoText, true);
        SetActive(dayResponseArtView.speakerText, true);
        SetActive(dayResponseArtView.dialogueText, true);

        SetRelativeRect(dayResponseArtView.npcInfoTitleText, new Vector2(0.175f, 0.745f), new Vector2(0.345f, 0.795f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dayResponseArtView.npcInfoText, new Vector2(0.175f, 0.565f), new Vector2(0.345f, 0.735f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dayResponseArtView.speakerText, new Vector2(0.068f, 0.250f), new Vector2(0.310f, 0.305f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dayResponseArtView.dialogueText, new Vector2(0.068f, 0.120f), new Vector2(0.915f, 0.242f), Vector2.zero, Vector2.zero);

        ApplyDayArtTextStyle(dayResponseArtView.npcInfoTitleText, 24f, TextAlignmentOptions.MidlineLeft, Color.white);
        ApplyDayArtTextStyle(dayResponseArtView.npcInfoText, 19f, TextAlignmentOptions.TopLeft, Color.white);
        ApplyDayArtTextStyle(dayResponseArtView.speakerText, 22f, TextAlignmentOptions.MidlineLeft, Color.white);
        ApplyDayArtTextStyle(dayResponseArtView.dialogueText, 22f, TextAlignmentOptions.TopLeft, new Color32(30, 27, 25, 255));

        dayResponseArtView.npcInfoText.margin = Vector4.zero;
        dayResponseArtView.speakerText.margin = Vector4.zero;
        dayResponseArtView.dialogueText.margin = Vector4.zero;
    }

    private void ApplyDayArtGoKitchenButtonStyle()
    {
        if (goKitchenButton == null)
            return;

        Image image = goKitchenButton.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color32(72, 84, 112, 230);
            image.raycastTarget = true;
        }

        TMP_Text label = goKitchenButton.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        label.color = Color.white;
        label.fontSize = 22f;
        label.enableAutoSizing = true;
        label.fontSizeMin = 14f;
        label.fontSizeMax = 22f;
        label.alignment = TextAlignmentOptions.Midline;
        label.margin = Vector4.zero;
        label.raycastTarget = false;
    }

    private static void ApplyDayArtTextStyle(TMP_Text target, float fontSize, TextAlignmentOptions alignment, Color color)
    {
        if (target == null)
            return;

        target.fontSize = fontSize;
        target.enableAutoSizing = true;
        target.fontSizeMin = Mathf.Max(10f, fontSize - 6f);
        target.fontSizeMax = fontSize;
        target.alignment = alignment;
        target.color = color;
        target.textWrappingMode = TextWrappingModes.Normal;
        target.overflowMode = TextOverflowModes.Ellipsis;
        target.raycastTarget = false;
    }

    private void BindDayResponseArtView()
    {
        if (!useDayArtLayout)
            return;

        if (dayResponseArtView == null)
            dayResponseArtView = FindAnyObjectByType<DayResponseArtView>();

        if (dayResponseArtView == null)
            return;

        if (dayResponseArtView.npcImage != null)
            portraitImage = dayResponseArtView.npcImage;

        if (dayResponseArtView.speakerText != null)
            nameText = dayResponseArtView.speakerText;

        if (dayResponseArtView.dialogueText != null)
            dialogueText = dayResponseArtView.dialogueText;

        if (dayResponseArtView.npcInfoText != null)
            customerInfoText = dayResponseArtView.npcInfoText;

        if (dayResponseArtView.recipeButton != null)
            menuOpenButton = dayResponseArtView.recipeButton;

        if (dayResponseArtView.noteButton != null)
            dayArtNoteButton = dayResponseArtView.noteButton;

        if (dayResponseArtView.optionButton != null)
            dayArtOptionButton = dayResponseArtView.optionButton;

        if (dayResponseArtView.dialogueAdvanceButton != null)
            nextButton = dayResponseArtView.dialogueAdvanceButton;

        if (dayResponseArtView.goToKitchenButton != null)
            goKitchenButton = dayResponseArtView.goToKitchenButton;

        if (portraitImage != null && customerPortrait == null)
            customerPortrait = portraitImage.sprite;
    }

    private void ApplyColorPreset()
    {
        if (useStaticDesignerLayout)
            return;

        if (colorApplied)
            return;

        colorApplied = true;

        ApplyPanelTint(customerPanel, PanelCustomerTint);
        ApplyPanelTint(kitchenPanel, PanelKitchenTint);
        ApplyPanelTint(resultPanel, PanelResultTint);
        ApplyPanelTint(menuBoardPanel, PanelMenuTint);

        ApplyNamedPanelTint(customerPanel, "CustomerPortraitPanel", new Color32(255, 246, 224, 248));
        ApplyNamedPanelTint(customerPanel, "CustomerSpeechPanel", new Color32(255, 249, 232, 250));
        ApplyNamedPanelTint(customerPanel, "BottomPanel", new Color32(248, 234, 206, 248));
        ApplyNamedPanelTint(customerPanel, "DialogueBox", new Color32(255, 248, 226, 255));
        ApplyPanelTint(foodImage != null ? foodImage.gameObject : null, new Color32(255, 248, 228, 255));

        TMP_FontAsset referenceFont = ResolveSceneFont();
        ApplyPaperFrame(customerPanel, "오늘의 한식", new Color32(165, 49, 35, 255), referenceFont);
        ApplyPaperFrame(kitchenPanel, "주방 조리대", new Color32(128, 70, 34, 255), referenceFont);
        ApplyPaperFrame(resultPanel, "요리결과", new Color32(151, 57, 39, 255), referenceFont);
        ApplyPaperFrame(menuBoardPanel, "한식 메뉴판", new Color32(151, 57, 39, 255), referenceFont);

        ApplyButtonTheme(choiceButtonA, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(choiceButtonB, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(nextButton, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(goKitchenButton, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(menuOpenButton, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(closeButton, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(menuButtonBibimbap, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(menuButtonKimchiJjigae, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(menuButtonJeyuk, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(recipeButton1, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(recipeButton2, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(recipeButton3, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(ingredientButton1, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(ingredientButton2, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(ingredientButton3, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(ingredientButton4, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(cookButton, new Color32(255, 236, 201, 255), new Color32(245, 214, 149, 255), new Color32(227, 168, 89, 255), new Color32(205, 215, 170, 255), ButtonDisabledTint);
        ApplyButtonTheme(backButton, ButtonNormalTint, ButtonHighlightTint, ButtonPressedTint, ButtonSelectedTint, ButtonDisabledTint);
        ApplyButtonTheme(nextDayButton, new Color32(255, 232, 190, 255), new Color32(244, 206, 132, 255), new Color32(214, 141, 71, 255), new Color32(190, 205, 154, 255), ButtonDisabledTint);

        if (portraitImage != null)
            portraitImage.color = new Color32(222, 210, 187, 255);

        if (selectedMenuImage != null)
            selectedMenuImage.color = new Color32(214, 202, 176, 255);

        if (cookingPotImage != null)
            cookingPotImage.color = new Color32(182, 160, 128, 255);

        if (foodImage != null)
            foodImage.color = new Color32(248, 241, 222, 255);

        if (cookingPotHintText != null)
            cookingPotHintText.color = PrimaryTextTint;

        ApplyPanelDepth(customerPanel, PanelShadowTint, PanelShadowOffset);
        ApplyPanelDepth(kitchenPanel, PanelShadowTint, PanelShadowOffset);
        ApplyPanelDepth(resultPanel, PanelShadowTint, PanelShadowOffset);
        ApplyPanelDepth(menuBoardPanel, PanelShadowTint, PanelShadowOffset);
        ApplyPanelDepth(foodImage != null ? foodImage.gameObject : null, new Color32(82, 57, 33, 80), new Vector2(3f, -3f));

        ApplyButtonDepth(choiceButtonA, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(choiceButtonB, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(nextButton, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(goKitchenButton, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(menuOpenButton, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(closeButton, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(menuButtonBibimbap, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(menuButtonKimchiJjigae, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(menuButtonJeyuk, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(recipeButton1, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(recipeButton2, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(recipeButton3, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(ingredientButton1, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(ingredientButton2, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(ingredientButton3, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(ingredientButton4, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(cookButton, new Color32(18, 14, 12, 140), ButtonShadowOffset);
        ApplyButtonDepth(backButton, ButtonShadowTint, ButtonShadowOffset);
        ApplyButtonDepth(nextDayButton, new Color32(18, 14, 12, 140), ButtonShadowOffset);
    }

    private void ApplyTypographyPreset()
    {
        if (useStaticDesignerLayout)
            return;

        if (typographyApplied)
            return;

        typographyApplied = true;

        TMP_FontAsset bodyFont = ResolveSceneFont();

        ApplyTextDefaultsInPanel(customerPanel, bodyFont);
        ApplyTextDefaultsInPanel(kitchenPanel, bodyFont);
        ApplyTextDefaultsInPanel(resultPanel, bodyFont);
        ApplyTextDefaultsInPanel(menuBoardPanel, bodyFont);

        ApplyTextStyle(nameText, bodyFont, 30f, FontStyles.Bold, WarningTextTint);
        ApplyTextStyle(dialogueText, bodyFont, 25f, FontStyles.Bold, PrimaryTextTint);
        ApplyTextStyle(customerSpeechText, bodyFont, 23f, FontStyles.Bold, SecondaryTextTint);
        ApplyTextStyle(customerInfoText, bodyFont, 18f, FontStyles.Normal, MutedTextTint);

        ApplyTextStyle(menuListText, bodyFont, 20f, FontStyles.Normal, SecondaryTextTint);
        ApplyTextStyle(recipeTitleText, bodyFont, 28f, FontStyles.Bold, WarningTextTint);
        ApplyTextStyle(recipeDetailText, bodyFont, 20f, FontStyles.Normal, SecondaryTextTint);

        ApplyTextStyle(selectedRecipeText, bodyFont, 20f, FontStyles.Bold, AccentTextTint);
        ApplyTextStyle(ingredientGuideText, bodyFont, 18f, FontStyles.Bold, SecondaryTextTint);
        ApplyTextStyle(cookingPotHintText, bodyFont, 22f, FontStyles.Bold, PrimaryTextTint);

        ApplyTextStyle(resultText, bodyFont, 28f, FontStyles.Bold, PrimaryTextTint);
        ApplyTextStyle(reactionText, bodyFont, 17f, FontStyles.Normal, SecondaryTextTint);
        ApplyTextStyle(clueText, bodyFont, 16f, FontStyles.Normal, MutedTextTint);
        ApplyTextStyle(unlockTitleText, bodyFont, 24f, FontStyles.Bold, WarningTextTint);

        ApplyButtonTextStyle(choiceButtonAText, bodyFont, 20f, FontStyles.Bold);
        ApplyButtonTextStyle(choiceButtonBText, bodyFont, 20f, FontStyles.Bold);
        ApplyButtonTextStyle(recipeButton1Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(recipeButton2Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(recipeButton3Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(ingredientButton1Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(ingredientButton2Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(ingredientButton3Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(ingredientButton4Text, bodyFont, 17f, FontStyles.Bold);
        ApplyButtonTextStyle(menuButtonBibimbap.GetComponentInChildren<TMP_Text>(true), bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(menuButtonKimchiJjigae.GetComponentInChildren<TMP_Text>(true), bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(menuButtonJeyuk.GetComponentInChildren<TMP_Text>(true), bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(nextButton != null ? nextButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(goKitchenButton != null ? goKitchenButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(menuOpenButton != null ? menuOpenButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(closeButton != null ? closeButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(cookButton != null ? cookButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 20f, FontStyles.Bold);
        ApplyButtonTextStyle(backButton != null ? backButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
        ApplyButtonTextStyle(nextDayButton != null ? nextDayButton.GetComponentInChildren<TMP_Text>(true) : null, bodyFont, 18f, FontStyles.Bold);
    }

private static TMP_FontAsset LoadFontAsset(string assetName)
    {
#if UNITY_EDITOR
        TMP_FontAsset editorFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/" + assetName + ".asset");
        if (editorFont != null)
            return editorFont;
#endif
        return Resources.Load<TMP_FontAsset>("Fonts/" + assetName);
    }

    private TMP_FontAsset ResolveSceneFont()
    {
        if (nameText != null && nameText.font != null)
            return nameText.font;

        if (dialogueText != null && dialogueText.font != null)
            return dialogueText.font;

        if (customerSpeechText != null && customerSpeechText.font != null)
            return customerSpeechText.font;

        if (recipeTitleText != null && recipeTitleText.font != null)
            return recipeTitleText.font;

        if (resultText != null && resultText.font != null)
            return resultText.font;

        return LoadFontAsset("GALMURI11_TMP");
    }

    private static void ApplyButtonTextStyle(TMP_Text target, TMP_FontAsset font, float size, FontStyles style)
    {
        ApplyTextStyle(target, font, size, style, ButtonLabelTint);

        if (target == null)
            return;

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.gameObject.AddComponent<Shadow>();

        shadow.effectColor = TextShadowTint;
        shadow.effectDistance = TextShadowOffset;
        shadow.useGraphicAlpha = true;

        Outline outline = target.GetComponent<Outline>();
        if (outline == null)
            outline = target.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color32(255, 250, 240, 28);
        outline.effectDistance = new Vector2(0.5f, -0.5f);
    }

    private static void ApplyTextStyle(TMP_Text target, TMP_FontAsset font, float size, FontStyles style, Color color)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target))
            return;

        if (font != null)
            target.font = font;

        target.fontSize = size;
        target.fontStyle = style;
        target.color = color;
        target.enableAutoSizing = true;
        target.fontSizeMin = Mathf.Max(14f, size - 6f);
        target.fontSizeMax = size;
        target.richText = true;
        target.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void ApplyTextDefaultsInPanel(GameObject root, TMP_FontAsset font)
    {
        if (root == null)
            return;

        TMP_Text[] labels = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label == null)
                continue;

            if (ShouldPreserveInspectorResultText(label))
                continue;

            if (font != null)
                label.font = font;

            label.enableAutoSizing = true;
            label.fontSizeMin = 13f;
            label.fontSizeMax = Mathf.Max(16f, label.fontSize);
            label.richText = true;
            label.textWrappingMode = TextWrappingModes.Normal;

            if (label.gameObject.name != "PaperFrameHeaderText" && IsNearWhite(label.color))
                label.color = PrimaryTextTint;
        }
    }

    private static bool ShouldPreserveInspectorResultText(TMP_Text target)
    {
        if (!Application.isPlaying || target == null)
            return false;

        string objectName = target.gameObject.name;
        return objectName == "ReactionText" || objectName == "ClueText";
    }

    private static bool IsNearWhite(Color color)
    {
        return color.r > 0.82f && color.g > 0.82f && color.b > 0.82f;
    }

    private static void StretchPanel(GameObject target, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (target == null)
            return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetRelativeRect(Component target, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target as TMP_Text))
            return;

        SetRelativeRect(target.transform, anchorMin, anchorMax, offsetMin, offsetMax);
    }

    private static void SetRelativeRect(GameObject target, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (target == null)
            return;

        SetRelativeRect(target.transform, anchorMin, anchorMax, offsetMin, offsetMax);
    }

    private static void SetRelativeRect(Transform target, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target.GetComponent<TMP_Text>()))
            return;

        RectTransform rect = target.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void ApplyTextBoxPadding(TMP_Text target, Vector4 margin)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target))
            return;

        target.margin = margin;
        target.alignment = TextAlignmentOptions.MidlineLeft;
        target.overflowMode = TextOverflowModes.Ellipsis;
        target.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void ApplyTextRhythm(TMP_Text target, float characterSpacing, float lineSpacing, float paragraphSpacing)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target))
            return;

        target.characterSpacing = characterSpacing;
        target.lineSpacing = lineSpacing;
        target.paragraphSpacing = paragraphSpacing;
        target.wordSpacing = 0f;
        target.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void ApplyButtonRhythm(TMP_Text target)
    {
        ApplyTextRhythm(target, -1f, 1f, 0f);
    }

    private static TMP_Text AddUiLabel(
        Transform parent,
        string objectName,
        string text,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TMP_FontAsset font,
        float fontSize,
        Color color)
    {
        if (parent == null)
            return null;

        Transform existing = parent.Find(objectName);
        GameObject labelObject;
        if (existing == null)
        {
            labelObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(parent, false);
        }
        else
        {
            labelObject = existing.gameObject;
        }

        SetRelativeRect(labelObject.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);

        TMP_Text label = labelObject.GetComponent<TMP_Text>();
        if (label == null)
            label = labelObject.AddComponent<TextMeshProUGUI>();

        if (font != null)
            label.font = font;

        label.text = text;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.enableAutoSizing = true;
        label.fontSizeMin = 11f;
        label.fontSizeMax = fontSize;
        label.characterSpacing = 1.5f;
        label.raycastTarget = false;
        labelObject.transform.SetAsLastSibling();
        return label;
    }

    private static void AddUiDivider(Transform parent, string objectName, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (parent == null)
            return;

        Transform existing = parent.Find(objectName);
        GameObject dividerObject;
        if (existing == null)
        {
            dividerObject = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            dividerObject.transform.SetParent(parent, false);
        }
        else
        {
            dividerObject = existing.gameObject;
        }

        SetRelativeRect(dividerObject.transform, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        Image image = dividerObject.GetComponent<Image>();
        if (image == null)
            image = dividerObject.AddComponent<Image>();

        image.color = new Color32(93, 69, 45, 70);
        image.raycastTarget = false;
        dividerObject.transform.SetAsLastSibling();
    }

    private static void ApplyTextBlockBackdrop(TMP_Text target)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target))
            return;

        Shadow shadow = target.GetComponent<Shadow>();
        if (shadow == null)
            shadow = target.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color32(255, 248, 228, 70);
        shadow.effectDistance = new Vector2(0.8f, -0.8f);
        shadow.useGraphicAlpha = true;
    }

    private static void SetTextAlignment(TMP_Text target, TextAlignmentOptions alignment)
    {
        if (target == null)
            return;

        if (ShouldPreserveInspectorResultText(target))
            return;

        target.alignment = alignment;
    }

    private static void ApplyButtonLabelPadding(Button button)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        RectTransform rect = label.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(14f, 5f);
            rect.offsetMax = new Vector2(-14f, -5f);
        }

        label.margin = Vector4.zero;
        label.alignment = TextAlignmentOptions.Center;
        label.overflowMode = TextOverflowModes.Ellipsis;
    }

    private void ApplyIngredientItemArt(Button button, int index)
    {
        if (button == null)
            return;

        DisableLayoutDrivenResize(button.gameObject);
        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null && ingredientItemSprite != null)
        {
            buttonImage.sprite = ingredientItemSprite;
            buttonImage.type = Image.Type.Simple;
            buttonImage.color = Color.white;
        }

        Transform iconTransform = button.transform.Find("IngredientIcon");
        if (iconTransform == null)
            iconTransform = button.transform.Find("Icon_Image");
        GameObject iconObject = iconTransform != null
            ? iconTransform.gameObject
            : new GameObject("IngredientIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        iconObject.name = "IngredientIcon";
        iconObject.transform.SetParent(button.transform, false);
        DisableLayoutDrivenResize(iconObject);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        iconRect.anchorMin = new Vector2(0.055f, 0.145f);
        iconRect.anchorMax = new Vector2(0.285f, 0.855f);
        iconRect.pivot = new Vector2(0.5f, 0.5f);
        iconRect.offsetMin = Vector2.zero;
        iconRect.offsetMax = Vector2.zero;

        Image iconImage = iconObject.GetComponent<Image>();
        if (ingredientSprites != null && index >= 0 && index < ingredientSprites.Length)
            iconImage.sprite = ingredientSprites[index];
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;
        iconImage.color = Color.white;
        iconObject.transform.SetAsFirstSibling();

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return;

        DisableLayoutDrivenResize(label.gameObject);
        label.name = "Name_Text";
        label.enableAutoSizing = false;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.fontSize = 15f;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = new Vector2(0.320f, 0.150f);
            labelRect.anchorMax = new Vector2(0.940f, 0.850f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }
    }

    private static void DisableLayoutDrivenResize(GameObject target)
    {
        if (target == null)
            return;

        ContentSizeFitter[] fitters = target.GetComponents<ContentSizeFitter>();
        for (int i = 0; i < fitters.Length; i++)
            fitters[i].enabled = false;

        LayoutGroup[] layoutGroups = target.GetComponents<LayoutGroup>();
        for (int i = 0; i < layoutGroups.Length; i++)
            layoutGroups[i].enabled = false;

        LayoutElement layoutElement = target.GetComponent<LayoutElement>();
        if (layoutElement != null)
            layoutElement.ignoreLayout = true;
    }

    private static void ResetRectScale(Component target)
    {
        if (target != null)
            target.transform.localScale = Vector3.one;
    }

    [ContextMenu("Apply Designer Sprite Overrides")]
    public void ApplyDesignerSpriteOverrides()
    {
        ApplySpriteOverride(customerPanel, customerPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(kitchenPanel, kitchenPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(menuBoardPanel, menuBoardPanelSpriteOverride, preserveAspect: false);

        ApplySpriteOverride(FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerPortraitPanel"), portraitPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel"), customerSpeechPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "DialogueBox"), dialogueBoxSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel"), bottomPanelSpriteOverride, preserveAspect: false);

        ApplySpriteOverride(cookingPotImage, cookingPotSpriteOverride, preserveAspect: true);
        ApplyButtonSprite(cookButton, cookButtonSpriteOverride);
        ApplyButtonSprite(recipeButton1, kitchenSideButtonSpriteOverride);
        ApplyButtonSprite(recipeButton2, kitchenSideButtonSpriteOverride);
        ApplyButtonSprite(recipeButton3, kitchenSideButtonSpriteOverride);
        ApplyButtonSprite(nextDayButton, resultNextButtonSpriteOverride);
        ApplySpriteOverride(foodImage, resultFoodPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "SelectedSlotPanelGraphic"), selectedSlotPanelSpriteOverride, preserveAspect: false);
        ApplySpriteOverride(FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "SelectedIngredientSlotPanel"), selectedSlotPanelSpriteOverride, preserveAspect: false);

        for (int i = 0; i < ingredientListButtons.Count; i++)
            ApplyIngredientButtonSprite(ingredientListButtons[i], ingredientButtonSpriteOverride);

        if (dayResponseArtView != null)
            dayResponseArtView.ApplyDesignerSpriteOverrides();

        ApplyExtraSpriteOverrides();
    }

    private void ApplyStaticCustomerNpcPlacement()
    {
        if (!useStaticDesignerLayout || !applyStaticNpcPlacement || portraitImage == null)
            return;

        Vector2 anchorMin = staticNpcAnchorMin + new Vector2(staticNpcCenterOffsetX, 0f);
        Vector2 anchorMax = staticNpcAnchorMax + new Vector2(staticNpcCenterOffsetX, 0f);
        SetRelativeRect(portraitImage, anchorMin, anchorMax, Vector2.zero, Vector2.zero);
        portraitImage.preserveAspect = true;
        portraitImage.raycastTarget = false;
    }

    private void ApplyKitchenTextReadability()
    {
        if (!enhanceKitchenWorldTextReadability || kitchenPanel == null)
            return;

        TMP_Text[] texts = kitchenPanel.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text == null || text.GetComponentInParent<Button>() != null)
                continue;

            ApplyReadableKitchenLabel(text);
        }
    }

    private void ApplyReadableKitchenLabel(TMP_Text text)
    {
        if (!enhanceKitchenWorldTextReadability || text == null)
            return;

        text.color = kitchenWorldTextColor;
        text.outlineColor = kitchenWorldTextOutlineColor;
        text.outlineWidth = kitchenWorldTextOutlineWidth;

        if (text == cookingPotHintText)
        {
            TMP_FontAsset font = ResolveSceneFont();
            if (font != null)
                text.font = font;

            text.enableAutoSizing = true;
            text.fontSize = cookingGaugeActive ? 23f : 18f;
            text.fontSizeMin = cookingGaugeActive ? 17f : 13f;
            text.fontSizeMax = cookingGaugeActive ? 25f : 20f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.lineSpacing = cookingGaugeActive ? -12f : -4f;
            text.margin = Vector4.zero;
        }

        Shadow shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();

        shadow.effectColor = new Color(0.08f, 0.045f, 0.025f, 0.78f);
        shadow.effectDistance = new Vector2(1.1f, -1.1f);
        shadow.useGraphicAlpha = true;
    }

    private void ApplyExtraSpriteOverrides()
    {
        if (extraSpriteOverrides == null)
            return;

        for (int i = 0; i < extraSpriteOverrides.Length; i++)
        {
            if (extraSpriteOverrides[i] != null)
                extraSpriteOverrides[i].Apply();
        }
    }

    private static void ApplySpriteOverride(Component target, Sprite sprite, bool preserveAspect)
    {
        if (target == null)
            return;

        ApplySpriteOverride(target.GetComponent<Image>(), sprite, preserveAspect);
    }

    private static void ApplySpriteOverride(GameObject target, Sprite sprite, bool preserveAspect)
    {
        if (target == null)
            return;

        ApplySpriteOverride(target.GetComponent<Image>(), sprite, preserveAspect);
    }

    private static void ApplySpriteOverride(Image image, Sprite sprite, bool preserveAspect)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }

    private static void ApplyIngredientButtonSprite(Button button, Sprite sprite)
    {
        if (button == null || sprite == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
    }

    private static void ApplyButtonSprite(Button button, Sprite sprite)
    {
        if (button == null || sprite == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image == null)
            return;

        image.sprite = sprite;
        image.color = Color.white;
        image.preserveAspect = true;
    }

    private static void ApplyPanelTint(GameObject target, Color color)
    {
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static void SetPanelScale(GameObject target, float scale)
    {
        if (target == null)
            return;

        target.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ApplyPanelDepth(GameObject target, Color shadowColor, Vector2 shadowOffset)
    {
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image != null)
        {
            Shadow shadow = image.GetComponent<Shadow>();
            if (shadow == null)
                shadow = image.gameObject.AddComponent<Shadow>();

            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowOffset;
            shadow.useGraphicAlpha = true;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color32(71, 55, 39, 58);
            outline.effectDistance = new Vector2(0.5f, -0.5f);
        }
    }

    private static void ApplyButtonDepth(Button button, Color shadowColor, Vector2 shadowOffset)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Shadow shadow = image.GetComponent<Shadow>();
            if (shadow == null)
                shadow = image.gameObject.AddComponent<Shadow>();

            shadow.effectColor = shadowColor;
            shadow.effectDistance = shadowOffset;
            shadow.useGraphicAlpha = true;

            Outline outline = image.GetComponent<Outline>();
            if (outline == null)
                outline = image.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color32(79, 58, 36, 52);
            outline.effectDistance = new Vector2(0.5f, -0.5f);
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Outline outline = label.GetComponent<Outline>();
            if (outline == null)
                outline = label.gameObject.AddComponent<Outline>();

            outline.effectColor = new Color32(33, 26, 21, 0);
            outline.effectDistance = Vector2.zero;
        }
    }

    private static void ApplyNamedPanelTint(GameObject root, string childName, Color color)
    {
        if (root == null)
            return;

        Transform child = FindChildRecursive(root.transform, childName);
        if (child == null)
            return;

        Image image = child.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                return child;

            Transform nested = FindChildRecursive(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private static void ApplyPaperFrame(GameObject target, string headerTitle, Color32 headerColor, TMP_FontAsset referenceFont)
    {
        if (target == null)
            return;

        if (target.transform.Find("PaperFrameHeader") == null)
        {
            GameObject header = new GameObject("PaperFrameHeader", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            header.transform.SetParent(target.transform, false);

            RectTransform headerRect = header.GetComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.anchoredPosition = Vector2.zero;
            headerRect.sizeDelta = new Vector2(0f, 58f);

            Image headerImage = header.GetComponent<Image>();
            headerImage.color = headerColor;

            GameObject label = new GameObject("PaperFrameHeaderText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            label.transform.SetParent(header.transform, false);
            RectTransform labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = new Vector2(18f, 8f);
            labelRect.offsetMax = new Vector2(-18f, -8f);

            TMP_Text labelText = label.GetComponent<TMP_Text>();
            if (referenceFont != null)
                labelText.font = referenceFont;
            labelText.text = headerTitle;
            labelText.fontSize = 22f;
            labelText.fontStyle = FontStyles.Bold;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            labelText.color = new Color32(252, 246, 232, 255);
            labelText.raycastTarget = false;
            labelText.enableAutoSizing = true;
            labelText.fontSizeMin = 16f;
            labelText.fontSizeMax = 22f;

            Outline outline = labelText.gameObject.GetComponent<Outline>();
            if (outline == null)
                outline = labelText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color32(45, 28, 18, 200);
            outline.effectDistance = new Vector2(0.8f, -0.8f);

            Shadow shadow = headerImage.GetComponent<Shadow>();
            if (shadow == null)
                shadow = headerImage.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color32(53, 34, 20, 120);
            shadow.effectDistance = new Vector2(3f, -3f);
            shadow.useGraphicAlpha = true;

            header.transform.SetAsLastSibling();
        }

        if (target.transform.Find("PaperFrameRule") == null)
        {
            GameObject rule = new GameObject("PaperFrameRule", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            rule.transform.SetParent(target.transform, false);

            RectTransform ruleRect = rule.GetComponent<RectTransform>();
            ruleRect.anchorMin = new Vector2(0f, 1f);
            ruleRect.anchorMax = new Vector2(1f, 1f);
            ruleRect.pivot = new Vector2(0.5f, 1f);
            ruleRect.anchoredPosition = new Vector2(0f, -58f);
            ruleRect.sizeDelta = new Vector2(0f, 2f);

            Image ruleImage = rule.GetComponent<Image>();
            ruleImage.color = new Color32(86, 66, 46, 120);

            rule.transform.SetAsLastSibling();
        }

    }

    private static void ApplyButtonTheme(Button button, Color normalColor, Color highlightedColor, Color pressedColor, Color selectedColor, Color disabledColor)
    {
        if (button == null)
            return;

        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = highlightedColor;
        colors.pressedColor = pressedColor;
        colors.selectedColor = selectedColor;
        colors.disabledColor = disabledColor;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = normalColor;
    }

    private void ApplyActivePanelLayout(bool showCustomer, bool showKitchen, bool showResult)
    {
        if (useStaticDesignerLayout)
        {
            if (showKitchen)
            {
                ForceKitchenPanelFullscreen();
                StretchPanel(kitchenPanel, Vector2.zero, Vector2.one);
            }

            if (showResult)
                StretchPanel(resultPanel, Vector2.zero, Vector2.one);

            return;
        }

        if (showCustomer)
        {
            if (useDayArtLayout)
                StretchPanel(customerPanel, Vector2.zero, Vector2.one);
            else
                StretchPanel(customerPanel, new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.93f));
        }

        if (showKitchen)
        {
            ForceKitchenPanelFullscreen();
            StretchPanel(kitchenPanel, Vector2.zero, Vector2.one);
        }

        if (showResult)
            StretchPanel(resultPanel, new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f));
    }

    private void SetPanelState(bool showCustomer, bool showKitchen, bool showResult)
    {
        GameObject dimOverlay = EnsureResultDimOverlay();
        bool showResultOverKitchen = showKitchen && showResult;

        SetActive(customerPanel, showCustomer);
        SetActive(kitchenPanel, showKitchen);
        SetActive(dimOverlay, showResultOverKitchen);
        SetActive(resultPanel, showResult);
        if (dayResponseArtView != null)
            dayResponseArtView.gameObject.SetActive(showCustomer);
        ApplyActivePanelLayout(showCustomer, showKitchen, showResult);
        ApplyResultOverlayLayering(showResultOverKitchen);
        if (showCustomer)
            ApplyStaticCustomerNpcPlacement();
        ApplyDesignerSpriteOverrides();
    }

    private GameObject EnsureResultDimOverlay()
    {
        if (resultDimOverlay != null)
            return resultDimOverlay;

        Transform parent = resultPanel != null ? resultPanel.transform.parent : null;
        if (parent == null && kitchenPanel != null)
            parent = kitchenPanel.transform.parent;
        if (parent == null)
            return null;

        Transform existing = parent.Find("ResultDimOverlay");
        resultDimOverlay = existing != null
            ? existing.gameObject
            : new GameObject("ResultDimOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        resultDimOverlay.transform.SetParent(parent, false);

        RectTransform rect = resultDimOverlay.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;
        }

        Image image = resultDimOverlay.GetComponent<Image>();
        if (image != null)
        {
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = true;
        }

        resultDimOverlay.SetActive(false);
        return resultDimOverlay;
    }

    private void ApplyResultOverlayLayering(bool showResultOverKitchen)
    {
        GameObject dimOverlay = EnsureResultDimOverlay();
        if (!showResultOverKitchen || dimOverlay == null || resultPanel == null || kitchenPanel == null)
            return;

        if (kitchenPanel.transform.parent == dimOverlay.transform.parent)
            dimOverlay.transform.SetSiblingIndex(kitchenPanel.transform.GetSiblingIndex() + 1);

        if (resultPanel.transform.parent == dimOverlay.transform.parent)
            resultPanel.transform.SetAsLastSibling();
    }

    private void ForceKitchenPanelFullscreen()
    {
        if (kitchenPanel == null)
            return;

        Transform current = kitchenPanel.transform;
        while (current != null)
        {
            RectTransform rect = current.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.localScale = Vector3.one;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            if (current.GetComponent<Canvas>() != null)
                break;

            current = current.parent;
        }
    }

    private ButtonMashingGauge EnsureButtonMashingGauge()
    {
        if (buttonMashingGauge == null)
        {
            Transform existing = FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, "ButtonMashingGauge");
            buttonMashingGauge = existing != null ? existing.GetComponent<ButtonMashingGauge>() : null;
        }

        if (buttonMashingGauge == null)
        {
            ButtonMashingGauge[] sceneGauges = Resources.FindObjectsOfTypeAll<ButtonMashingGauge>();
            for (int i = 0; i < sceneGauges.Length; i++)
            {
                if (sceneGauges[i] != null && sceneGauges[i].gameObject.scene.IsValid())
                {
                    buttonMashingGauge = sceneGauges[i];
                    break;
                }
            }
        }

        if (buttonMashingGauge == null)
            return null;

        buttonMashingGauge.OnGaugeFinished -= HandleCookingGaugeFinished;
        buttonMashingGauge.OnGaugeFinished += HandleCookingGaugeFinished;
        buttonMashingGauge.StopGauge();
        return buttonMashingGauge;
    }

    private void StopCookingGaugeUi()
    {
        if (buttonMashingGauge != null)
            buttonMashingGauge.StopGauge();
        SetActive(cookingPotHintText, true);
    }


    private void ClearIngredientOptions()
    {
        for (int i = 0; i < currentIngredientOptions.Length; i++)
            currentIngredientOptions[i] = string.Empty;
    }

    private void PopulateKitchenIngredientOptions()
    {
        ClearIngredientOptions();

        for (int i = 0; i < KitchenIngredientList.Length && i < currentIngredientOptions.Length; i++)
            currentIngredientOptions[i] = KitchenIngredientList[i];
    }

    private void PopulateKitchenIngredientOptionsIfEmpty()
    {
        bool hasAnyIngredient = currentIngredientOptions.Any(option => !string.IsNullOrEmpty(option));
        if (!hasAnyIngredient)
            PopulateKitchenIngredientOptions();
    }

    private void ShowPotHint(string message)
    {
        SetText(cookingPotHintText, message);
        ApplyReadableKitchenLabel(cookingPotHintText);
    }

    private Button FindNamedButton(string objectName)
    {
        Transform target = FindChildRecursive(kitchenPanel != null ? kitchenPanel.transform : null, objectName);
        if (target == null)
            target = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, objectName);

        return target != null ? target.GetComponent<Button>() : null;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private static void SetActive(GameObject target, bool isActive)
    {
        if (target != null)
            target.SetActive(isActive);
    }

    private static void SetActive(Component target, bool isActive)
    {
        if (target != null)
            target.gameObject.SetActive(isActive);
    }

    private static void SetInteractable(Selectable target, bool isInteractable)
    {
        if (target != null)
            target.interactable = isInteractable;
    }

    private static void MakeImageTransparent(GameObject target)
    {
        if (target == null)
            return;

        MakeImageTransparent(target.transform);
    }

    private static void MakeImageTransparent(Transform target)
    {
        if (target == null)
            return;

        Image image = target.GetComponent<Image>();
        if (image == null)
            return;

        Color color = image.color;
        color.a = 0f;
        image.color = color;
        image.raycastTarget = false;
    }

    private static void MakeImagesTransparentInChildren(GameObject target)
    {
        if (target == null)
            return;

        Image[] images = target.GetComponentsInChildren<Image>(true);
        foreach (Image image in images)
        {
            if (image == null)
                continue;

            Color color = image.color;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void MakeLegacyTextsTransparentInChildren(GameObject target)
    {
        if (target == null)
            return;

        TMP_Text[] texts = target.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null || text == dialogueText || text == nameText)
                continue;

            Color color = text.color;
            color.a = 0f;
            text.color = color;
            text.raycastTarget = false;
        }
    }

    private static void MakeButtonTransparent(Button button)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
        {
            Color color = image.color;
            color.a = 0f;
            image.color = color;
            image.raycastTarget = true;
        }

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Color color = label.color;
            color.a = 0f;
            label.color = color;
        }
    }

    private static void SetButtonImageColor(Button button, Color color)
    {
        if (button == null)
            return;

        Image image = button.GetComponent<Image>();
        if (image != null)
            image.color = color;
    }

    private static void SetButtonLabel(Button button, string text)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        SetText(label, text);
    }

    private static void SetPanelHeaderTitle(GameObject panel, string title)
    {
        if (panel == null)
            return;

        Transform header = panel.transform.Find("PaperFrameHeader");
        Transform label = header != null ? header.Find("PaperFrameHeaderText") : null;
        TMP_Text labelText = label != null ? label.GetComponent<TMP_Text>() : null;
        SetText(labelText, title);
    }
}
