using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class DayUIManager : MonoBehaviour
{
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

    [Serializable]
    public class DialogueLine
    {
        public bool isCustomer;

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
                transform.SetParent(rootCanvas.transform, true);

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
                transform.SetParent(originalParent, true);

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

    [Header("Result UI")]
    public Image foodImage;
    public TMP_Text resultText;
    public TMP_Text reactionText;
    public TMP_Text clueText;
    public Button nextDayButton;

    public TMP_Text unlockTitleText;
    public TMP_Text unlockMenuText;

    [Header("Scene Flow")]
    [SerializeField] private string nightSceneName = "Stage01_CyberStreet";

    [Header("Test Data")]
    public Sprite customerPortrait;
    public Sprite bibimbapSprite;
    public Sprite kimchiJjigaeSprite;
    public Sprite jeyukSprite;

    private const int ChoiceDialogueIndex = 2;
    private const int MaxIngredientSlots = 3;
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
            text = "순두부요. 부드럽게 넘어가는 게 좋고, 고춧가루가 적당히 들어갔으면 좋겠어요. 참고로 전 매운 걸 잘 못 먹어요. 그리고 완전히 안 익은 계란을 먹으면 마음이 조금 가라앉아요."
        },
        new DialogueLine
        {
            isCustomer = false,
            text = "좋아요. 순두부와 적당한 고춧가루, 계란을 적당히 풀어 따뜻하게 끓여볼게요."
        },
        new DialogueLine
        {
            isCustomer = true,
            text = "부탁드릴게요. 오늘은 조용히 속을 데우고 싶어요."
        }
    };

    private readonly Dictionary<MenuId, RecipeDefinition> recipes = new Dictionary<MenuId, RecipeDefinition>();
    private readonly Dictionary<string, string[]> ingredientTags = new Dictionary<string, string[]>();
    private readonly List<string> selectedIngredients = new List<string>(MaxIngredientSlots);
    private readonly string[] currentIngredientOptions = new string[4];
    private HashSet<string> unlockedIngredients = new HashSet<string>();
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

    private void Awake()
    {
        BuildIngredientTags();
        BuildRecipes();
    }

    private void Start()
    {
        rootCanvas = GetComponentInParent<Canvas>();
        InitUI();
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
        BindButtons();
        EnsureCookingPotDropZone();
        ConfigureIngredientDragSources();
        LoadCustomerScene();
    }

    private void InitUI()
    {
        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(choiceGroup, false);
        SetActive(menuBoardPanel, false);
        SetActive(goKitchenButton, false);
        SetInteractable(cookButton, false);
    }

    private void BindButtons()
    {
        Bind(nextButton, OnClickNextDialogue);
        Bind(goKitchenButton, OpenKitchen);
        Bind(choiceButtonA, () => OnChoiceSelected(CustomerPreference.MildSoup));
        Bind(choiceButtonB, () => OnChoiceSelected(CustomerPreference.SpicySoup));

        Bind(recipeButton1, () => SelectRecipe(MenuId.KimchiJjigae));
        Bind(recipeButton2, () => SelectRecipe(MenuId.DoenjangJjigae));
        Bind(recipeButton3, () => SelectRecipe(MenuId.SoondubuJjigae));

        Bind(cookButton, CookSelectedRecipe);
        Bind(backButton, BackToCustomer);
        Bind(nextDayButton, StartNightFlow);

        Bind(menuOpenButton, OpenMenuBoard);
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
            cookingPotImage.raycastTarget = true;

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
        ConfigureIngredientDragSource(ingredientButton1, 0);
        ConfigureIngredientDragSource(ingredientButton2, 1);
        ConfigureIngredientDragSource(ingredientButton3, 2);
        ConfigureIngredientDragSource(ingredientButton4, 3);
    }

    private void ConfigureIngredientDragSource(Button button, int index)
    {
        if (button == null)
            return;

        DraggableIngredientUI dragSource = button.GetComponent<DraggableIngredientUI>();
        if (dragSource == null)
            dragSource = button.gameObject.AddComponent<DraggableIngredientUI>();

        dragSource.Initialize(this, index, rootCanvas);
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
                "김치와 돼지고기, 두부를 넣어 얼큰하고 든든한 국물 요리를 만든다.",
                new[] { "김치", "돼지고기", "두부", "대파" },
                new[] { "김치", "돼지고기", "두부" },
                new[] { "매움", "해장", "든든함" },
                new[] { "기름짐" }));

        recipes.Add(
            MenuId.SoondubuJjigae,
            new RecipeDefinition(
                MenuId.SoondubuJjigae,
                "순두부찌개",
                "순두부와 적당한 고춧가루, 계란을 넣어 따뜻하고 부드러운 국물 요리를 만든다.",
                new[] { "순두부", "고춧가루", "계란", "두부" },
                new[] { "순두부", "고춧가루", "계란" },
                new[] { "부드러움", "따뜻함", "담백함", "국물", "칼칼함" },
                new[] { "매우매움", "기름짐" }));
    }

    private void LoadCustomerScene()
    {
        if (!GameFlowState.ConsumeReturnedFromNight())
            GameProgression.ResetProgress();

        RefreshUnlockedIngredients();
        currentDayNumber = GameProgression.GetCurrentDayNumber();
        currentDialogueLines = GetDialogueLinesForCurrentDay();
        dialogueIndex = 0;
        choiceAnswered = true;
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
        SetText(customerInfoText, GetCustomerInfoForCurrentDay());
        SetPanelHeaderTitle(customerPanel, currentDayNumber + "일차 오늘의 한식");
        SetPanelHeaderTitle(kitchenPanel, currentDayNumber + "일차 주방 조리대");
        SetPanelHeaderTitle(resultPanel, currentDayNumber + "일차 식사 평가");
        SetText(choiceButtonAText, MildChoiceText);
        SetText(choiceButtonBText, SpicyChoiceText);
        SetText(selectedRecipeText, "선택한 메뉴\n없음");
        SetText(recipeTitleText, "손님 단서 노트");
        SetText(recipeDetailText, BuildCustomerClueGuide());
        UpdateUnlockSummary();
        SetButtonLabel(nextDayButton, "밤 파트 시작");

        if (portraitImage != null && customerPortrait != null)
            portraitImage.sprite = customerPortrait;

        UpdateMenuButtons();
        UpdateMenuBoard();
        ResetKitchenIngredientUI();
        ShowCurrentDialogue();
    }

    private void ShowCurrentDialogue()
    {
        if (currentDialogueLines == null)
            currentDialogueLines = GetDialogueLinesForCurrentDay();

        if (dialogueIndex < 0 || dialogueIndex >= currentDialogueLines.Length)
            return;

        DialogueLine line = currentDialogueLines[dialogueIndex];

        if (line.isCustomer)
        {
            lastCustomerSpeech = line.text;
            SetText(customerSpeechText, line.text);
        }
        else
        {
            SetText(dialogueText, line.text);
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

        SetActive(nextButton, false);
        SetActive(goKitchenButton, true);
        SetText(dialogueText, GetKitchenMoveTextForCurrentDay());
    }

    private void ShowChoice()
    {
        SetActive(choiceGroup, true);
        SetInteractable(nextButton, false);
        EnsureCustomerSpeechVisible();
        SetText(dialogueText, "강태수씨가 붙잡고 있는 음식의 기억을 짚어보세요.");
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

        SetText(customerSpeechText, lastCustomerSpeech);
    }

    private void OnChoiceSelected(CustomerPreference preference)
    {
        choiceAnswered = true;
        selectedPreference = preference;

        SetActive(choiceGroup, false);
        SetInteractable(nextButton, true);
        SetText(customerInfoText, "해석된 단서: " + GetPreferenceLabel(preference));

        UpdateMenuBoard();

        dialogueIndex++;
        ShowCurrentDialogue();
        lastCustomerSpeech = GetCustomerReplyForChoice(preference);
        SetText(customerSpeechText, lastCustomerSpeech);
    }

    private string GetCustomerReplyForChoice(CustomerPreference preference)
    {
        if (currentDayNumber >= 2)
        {
            switch (preference)
            {
                case CustomerPreference.MildSoup:
                    return "맞아요. 오늘은 구수하고 따뜻한 된장 국물이 좋겠어요.";

                case CustomerPreference.SpicySoup:
                    return "오늘은 매운 냄새보다 속이 편한 국물이 더 필요해요.";

                default:
                    return "맞아요. 오늘은 구수하고 따뜻한 된장 국물이 좋겠어요.";
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
        return currentDayNumber >= 2 ? dayTwoDialogueLines : dayOneDialogueLines;
    }

    private string GetCustomerNameForCurrentDay()
    {
        return currentDayNumber >= 2 ? "윤서아" : "강태수";
    }

    private string GetCustomerInfoForCurrentDay()
    {
        return currentDayNumber >= 2
            ? "2일차 단서: 따뜻함, 부드러운 순두부, 적당한 고춧가루"
            : "1일차 단서: 매콤한 냄새, 김치찌개, 가족의 기억";
    }

    private string GetKitchenMoveTextForCurrentDay()
    {
        return currentDayNumber >= 2
            ? "2일차 손님의 단서에 맞춰 주방으로 이동해 조리해요."
            : "강태수씨의 기억에 맞춰 주방으로 이동해 조리해요.";
    }

    private CustomerPreference GetPreferenceForCurrentDay()
    {
        return currentDayNumber >= 2 ? CustomerPreference.MildSoup : CustomerPreference.SpicySoup;
    }

    private MenuId GetTargetRecipeForCurrentDay()
    {
        return currentDayNumber >= 2 ? MenuId.SoondubuJjigae : MenuId.KimchiJjigae;
    }

    private string GetWrongRecipeReactionForCurrentDay()
    {
        return currentDayNumber >= 2
            ? "손님 반응: 맛은 있지만 오늘 제 속에는 조금 강하게 느껴져요."
            : "손님 반응: 하... 역시. 내가 떠올리고 싶던 냄새는 이게 아니었어.";
    }

    private string GetWrongRecipeClueForCurrentDay()
    {
        return currentDayNumber >= 2
            ? "다음 단서: 2일차 손님은 따뜻하고 부드러운 순두부찌개를 원했습니다."
            : "다음 단서: 강태수씨는 담백한 국물이 아니라 가족과 먹던 매콤한 김치찌개의 냄새를 찾고 있었습니다.";
    }

    public void OpenMenuBoard()
    {
        SetActive(menuBoardPanel, true);
        ApplyMenuBoardLayout();
        if (menuBoardPanel != null)
            menuBoardPanel.transform.SetAsLastSibling();
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
        ClearIngredientOptions();

        for (int i = 0; i < recipe.IngredientOptions.Length && i < currentIngredientOptions.Length; i++)
            currentIngredientOptions[i] = recipe.IngredientOptions[i];

        SetText(ingredientGuideText, "필수 재료 " + recipe.RequiredIngredients.Length + "개 선택");
        UpdateIngredientButtonTexts();
        UpdateIngredientSlots();
        ShowPotHint("재료를 뚝배기에 넣어 조리 준비를 하세요.");
        UpdateCookButtonState();
    }

    private void SetupIngredientsForCurrentCustomer()
    {
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
        UpdateSingleIngredientButtonText(ingredientButton1Text, currentIngredientOptions[0], 1);
        UpdateSingleIngredientButtonText(ingredientButton2Text, currentIngredientOptions[1], 2);
        UpdateSingleIngredientButtonText(ingredientButton3Text, currentIngredientOptions[2], 3);
        UpdateSingleIngredientButtonText(ingredientButton4Text, currentIngredientOptions[3], 4);
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

        if (!IsIngredientUnlocked(ingredientName))
        {
            targetText.text = displayIndex.ToString("00") + "  잠김  " + ingredientName;
            return;
        }

        targetText.text = selectedIngredients.Contains(ingredientName)
            ? displayIndex.ToString("00") + "  담음  " + ingredientName
            : displayIndex.ToString("00") + "  " + ingredientName;
    }

    private bool AddIngredientFromDrag(int index)
    {
        if (index < 0 || index >= currentIngredientOptions.Length)
            return false;

        string ingredientName = currentIngredientOptions[index];
        if (string.IsNullOrEmpty(ingredientName))
            return false;

        if (!IsIngredientUnlocked(ingredientName))
        {
            ShowPotHint(ingredientName + "은 아직 밤 파트에서 해금되지 않았어요.");
            return true;
        }

        if (selectedIngredients.Contains(ingredientName))
        {
            ShowPotHint(ingredientName + "은 이미 뚝배기에 들어가 있어요.");
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
        SetActive(slot1Text, false);
        SetActive(slot2Text, false);
        SetActive(slot3Text, false);
        UpdateCookingPotState();
    }

    private void UpdateCookingPotState()
    {
        if (selectedIngredients.Count == 0)
        {
            ShowPotHint("재료를 뚝배기에\n넣어주세요");
            return;
        }

        ShowPotHint("담긴 재료\n" + string.Join(" / ", selectedIngredients));
    }

    private void UpdateCookButtonState()
    {
        SetInteractable(cookButton, selectedIngredients.Count == MaxIngredientSlots);
    }

    private void ResetKitchenIngredientUI()
    {
        ClearIngredientOptions();
        selectedIngredients.Clear();
        SetText(ingredientGuideText, "손님 단서에 맞춰 재료 선택");
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
        SetupIngredientsForCurrentCustomer();
    }

    private void BackToCustomer()
    {
        SetPanelState(showCustomer: true, showKitchen: false, showResult: false);
        SetActive(menuBoardPanel, false);
    }

    private void CookSelectedRecipe()
    {
        if (selectedIngredients.Count != MaxIngredientSlots)
            return;

        string cookedFoodName = ResolveCookedFoodName();
        EvaluationResult evaluation = EvaluateCustomerMatch();

        SetPanelState(showCustomer: false, showKitchen: false, showResult: true);
        SetActive(menuBoardPanel, false);

        Sprite sprite = GetFoodSpriteForCurrentIngredients();
        if (foodImage != null && sprite != null)
            foodImage.sprite = sprite;

        SetText(resultText, cookedFoodName + "\n" + GetGradeBadge(evaluation.Grade));
        SetText(reactionText, evaluation.Reaction);
        SetText(clueText, evaluation.Clue);
        SetResultClueLabel("플레이어 대화");

        if (evaluation.Grade == EvaluationGrade.Perfect)
        {
            if (currentDayNumber == 1)
            {
                GameProgression.UnlockIngredients("순두부", "고춧가루", "계란");
                RefreshUnlockedIngredients();
                SetText(unlockTitleText, "새로 해금된 메뉴");
                SetText(unlockMenuText, "순두부찌개");
            }
            else
            {
                SetText(unlockTitleText, "2일차 완료");
                SetText(unlockMenuText, "다음 손님은 준비 중입니다.");
            }

            SetButtonLabel(nextDayButton, "밤 파트 시작");
            Bind(nextDayButton, StartNightFlow);
        }
        else
        {
            SetText(unlockTitleText, "요리 재도전");
            SetText(unlockMenuText, "평가가 완벽해야 순두부찌개가 해금됩니다.");
            SetButtonLabel(nextDayButton, "다시 요리하기");
            Bind(nextDayButton, OpenKitchen);
        }

        UpdateMenuButtons();
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
        MenuId targetRecipe = GetTargetRecipeForCurrentDay();

        if (selectedRecipeId != targetRecipe)
        {
            return new EvaluationResult(
                EvaluationGrade.Poor,
                0,
                GetWrongRecipeReactionForCurrentDay(),
                GetWrongRecipeClueForCurrentDay());
        }

        if (currentDayNumber == 1 && selectedRecipeId == MenuId.KimchiJjigae)
        {
            string[] requiredKimchiJjigaeIngredients = { "김치", "두부", "돼지고기" };
            bool hasAllRequiredIngredients = requiredKimchiJjigaeIngredients.All(ingredient => selectedIngredients.Contains(ingredient));

            if (!hasAllRequiredIngredients)
            {
                return new EvaluationResult(
                    EvaluationGrade.Poor,
                    0,
                    "하.. 역시. 난 살아있는 자체가 죄야.",
                    "문 뒤에서 소리 지르던 그 비명들... 죄송합니다..");
            }
        }

        if (currentDayNumber >= 2 && selectedRecipeId == MenuId.SoondubuJjigae)
        {
            string[] requiredSoondubuJjigaeIngredients = { "순두부", "고춧가루", "계란" };
            bool hasAllRequiredIngredients = requiredSoondubuJjigaeIngredients.All(ingredient => selectedIngredients.Contains(ingredient));

            if (!hasAllRequiredIngredients)
            {
                return new EvaluationResult(
                    EvaluationGrade.Poor,
                    0,
                    "심정지.... 사망... 변이.... 내가 조금만 더 빨랐다면.... 열이 오르면.... 또 그것처럼 변할 거야..",
                    ".......");
            }
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
            return currentDayNumber >= 2
                ? "그래요... 어쩔 수 없이 돌아가시는 분들도 많이 계시죠..\n하지만 그 중에서 서아씨 덕분에 다시 일어나신 분들을 생각하고,\n앞으로도 사람들을 위해서 노력하셨으면 좋을 것 같아요."
                : "당연하죠. 언제든지 오세요.\n그때도 김치찌개 끓이고 기다릴게요.\n다치지 마시고, 삶을 포기하지 마세요.";
        }

        string forbidden = string.Join(", ", selectedIngredients.Where(ingredient => forbiddenIngredients.Contains(ingredient)));
        if (!string.IsNullOrEmpty(forbidden))
        {
            return currentDayNumber >= 2
                ? "다음 단서: 속이 편한 국물을 원하는 손님에게는 " + forbidden + " 같은 강한 재료를 피하세요."
                : "다음 단서: 강태수씨에게 중요한 건 재료보다도 김치찌개 특유의 매콤한 냄새였습니다.";
        }

        string riskyTags = string.Join(", ", foodTags.Intersect(avoidedTags));
        if (!string.IsNullOrEmpty(riskyTags))
        {
            return currentDayNumber >= 2
                ? "다음 단서: " + riskyTags + " 속성이 강하면 2일차 손님이 원한 편안한 맛에서 멀어집니다."
                : "다음 단서: " + riskyTags + " 속성이 강하면 강태수씨가 찾던 가족의 맛에서 멀어집니다.";
        }

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
                return (menuId == MenuId.SoondubuJjigae ? 3 : 0)
                    + (foodTags.Contains("담백함") ? 2 : 0)
                    + (foodTags.Contains("부드러움") ? 2 : 0)
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
        return (selectedPreference == CustomerPreference.MildSoup && menuId == MenuId.SoondubuJjigae)
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
        if (currentDayNumber >= 2)
        {
            return "손님 단서\n"
                + "2일차 손님\n"
                + "따뜻하고 부드러운 순두부찌개와 적당한 고춧가루, 계란을 찾고 있음\n\n"
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
        UpdateRecipeButton(recipeButton1, recipeButton1Text, MenuId.KimchiJjigae);
        UpdateRecipeButton(recipeButton2, recipeButton2Text, MenuId.DoenjangJjigae);
        UpdateRecipeButton(recipeButton3, recipeButton3Text, MenuId.SoondubuJjigae);

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
            return IsSoondubuJjigaeUnlocked();

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
                return "완벽";

            case EvaluationGrade.Good:
                return "만족";

            case EvaluationGrade.Okay:
                return "애매함";

            default:
                return "불만";
        }
    }

    private string GetGradeBadge(EvaluationGrade grade)
    {
        return "평가: " + GetGradeLabel(grade);
    }

    private void StartNightFlow()
    {
        GameFlowState.RequestNightPlay();
        int sceneIndex = SceneFlowUtility.FindSceneIndexByName(nightSceneName);
        if (sceneIndex < 0)
        {
            Debug.LogWarning("Night scene not found: " + nightSceneName);
            return;
        }

        SceneManager.LoadScene(sceneIndex);
    }

    private void RefreshUnlockedIngredients()
    {
        unlockedIngredients = new HashSet<string>(GameProgression.GetUnlockedIngredients());
    }

    private bool IsIngredientUnlocked(string ingredientName)
    {
        return !string.IsNullOrEmpty(ingredientName)
            && (IsKimchiJjigaeStarterIngredient(ingredientName)
                || IsDoenjangJjigaeIngredientUnlocked(ingredientName)
                || IsSoondubuJjigaeIngredientUnlocked(ingredientName)
                || unlockedIngredients.Contains(ingredientName));
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
            SetText(unlockMenuText, string.Join(", ", newlyUnlocked));
            return;
        }

        SetText(unlockTitleText, "새로 해금된 재료");
        SetText(unlockMenuText, "해금 없음");
    }

    private void ApplyLayoutPreset()
    {
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
        Transform portraitPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerPortraitPanel");
        Transform speechPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "CustomerSpeechPanel");
        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");

        SetRelativeRect(portraitPanel, new Vector2(0.06f, 0.53f), new Vector2(0.33f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(speechPanel, new Vector2(0.36f, 0.53f), new Vector2(0.94f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(bottomPanel, new Vector2(0.06f, 0.09f), new Vector2(0.94f, 0.46f), Vector2.zero, Vector2.zero);
        ApplyPanelTint(portraitPanel != null ? portraitPanel.gameObject : null, new Color32(246, 235, 210, 255));
        ApplyPanelTint(speechPanel != null ? speechPanel.gameObject : null, new Color32(255, 250, 234, 255));
        ApplyPanelTint(bottomPanel != null ? bottomPanel.gameObject : null, new Color32(232, 216, 187, 255));

        Transform dialogueBox = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "DialogueBox");
        SetRelativeRect(dialogueBox, new Vector2(0.04f, 0.25f), new Vector2(0.70f, 0.86f), Vector2.zero, Vector2.zero);
        ApplyPanelTint(dialogueBox != null ? dialogueBox.gameObject : null, new Color32(255, 248, 226, 255));

        SetRelativeRect(menuOpenButton, new Vector2(0.76f, 0.66f), new Vector2(0.96f, 0.86f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextButton, new Vector2(0.76f, 0.40f), new Vector2(0.96f, 0.60f), Vector2.zero, Vector2.zero);
        SetRelativeRect(goKitchenButton, new Vector2(0.76f, 0.40f), new Vector2(0.96f, 0.60f), Vector2.zero, Vector2.zero);

        SetRelativeRect(choiceGroup, new Vector2(0.04f, 0.05f), new Vector2(0.70f, 0.18f), Vector2.zero, Vector2.zero);
        SetRelativeRect(choiceButtonA, new Vector2(0f, 0f), new Vector2(0.48f, 1f), Vector2.zero, Vector2.zero);
        SetRelativeRect(choiceButtonB, new Vector2(0.52f, 0f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        SetRelativeRect(dialogueText, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);
        SetRelativeRect(customerSpeechText, new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(dialogueText, Vector4.zero);
        ApplyTextBoxPadding(customerSpeechText, Vector4.zero);
        SetTextAlignment(dialogueText, TextAlignmentOptions.Center);
        SetTextAlignment(customerSpeechText, TextAlignmentOptions.Center);
        ApplyButtonLabelPadding(menuOpenButton);
        ApplyButtonLabelPadding(nextButton);
        ApplyButtonLabelPadding(goKitchenButton);
        ApplyButtonLabelPadding(choiceButtonA);
        ApplyButtonLabelPadding(choiceButtonB);
    }

    private void ApplyKitchenPrepLayout()
    {
        SetRelativeRect(selectedRecipeText, new Vector2(0.07f, 0.77f), new Vector2(0.30f, 0.86f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeButton1, new Vector2(0.07f, 0.65f), new Vector2(0.30f, 0.74f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeButton2, new Vector2(0.07f, 0.53f), new Vector2(0.30f, 0.62f), Vector2.zero, Vector2.zero);
        SetRelativeRect(recipeButton3, new Vector2(0.07f, 0.41f), new Vector2(0.30f, 0.50f), Vector2.zero, Vector2.zero);

        SetRelativeRect(ingredientGuideText, new Vector2(0.36f, 0.76f), new Vector2(0.67f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(cookingPotDropZone, new Vector2(0.38f, 0.29f), new Vector2(0.66f, 0.66f), Vector2.zero, Vector2.zero);
        SetRelativeRect(cookingPotHintText, new Vector2(0.39f, 0.37f), new Vector2(0.65f, 0.58f), Vector2.zero, Vector2.zero);

        SetRelativeRect(ingredientButton1, new Vector2(0.71f, 0.61f), new Vector2(0.93f, 0.70f), Vector2.zero, Vector2.zero);
        SetRelativeRect(ingredientButton2, new Vector2(0.71f, 0.49f), new Vector2(0.93f, 0.58f), Vector2.zero, Vector2.zero);
        SetRelativeRect(ingredientButton3, new Vector2(0.71f, 0.37f), new Vector2(0.93f, 0.46f), Vector2.zero, Vector2.zero);
        SetRelativeRect(ingredientButton4, new Vector2(0.71f, 0.25f), new Vector2(0.93f, 0.34f), Vector2.zero, Vector2.zero);

        SetRelativeRect(cookButton, new Vector2(0.59f, 0.10f), new Vector2(0.80f, 0.19f), Vector2.zero, Vector2.zero);
        SetRelativeRect(backButton, new Vector2(0.82f, 0.10f), new Vector2(0.93f, 0.19f), Vector2.zero, Vector2.zero);

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
        SetTextAlignment(recipeButton1Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(recipeButton2Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(recipeButton3Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton1Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton2Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton3Text, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(ingredientButton4Text, TextAlignmentOptions.MidlineLeft);
    }

    private void ApplyResultLayout()
    {
        SetRelativeRect(foodImage, new Vector2(0.08f, 0.58f), new Vector2(0.34f, 0.78f), Vector2.zero, Vector2.zero);
        SetRelativeRect(resultText, new Vector2(0.39f, 0.62f), new Vector2(0.91f, 0.78f), Vector2.zero, Vector2.zero);
        SetRelativeRect(reactionText, new Vector2(0.08f, 0.23f), new Vector2(0.51f, 0.54f), Vector2.zero, Vector2.zero);
        SetRelativeRect(clueText, new Vector2(0.56f, 0.34f), new Vector2(0.91f, 0.54f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockTitleText, new Vector2(0.56f, 0.25f), new Vector2(0.91f, 0.31f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockMenuText, new Vector2(0.56f, 0.18f), new Vector2(0.91f, 0.24f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextDayButton, new Vector2(0.41f, 0.06f), new Vector2(0.59f, 0.14f), Vector2.zero, Vector2.zero);

        ApplyPanelTint(foodImage != null ? foodImage.gameObject : null, new Color32(255, 248, 228, 255));
        ApplyTextBoxPadding(resultText, new Vector4(8f, 4f, 8f, 4f));
        ApplyTextBoxPadding(reactionText, new Vector4(12f, 8f, 12f, 8f));
        ApplyTextBoxPadding(clueText, new Vector4(12f, 8f, 12f, 8f));
        SetTextAlignment(resultText, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(reactionText, TextAlignmentOptions.TopLeft);
        SetTextAlignment(clueText, TextAlignmentOptions.TopLeft);
        SetTextAlignment(unlockTitleText, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(unlockMenuText, TextAlignmentOptions.MidlineLeft);
        ApplyButtonLabelPadding(nextDayButton);
    }

    private void ApplyMenuBoardLayout()
    {
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

        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Recipes", "메뉴 선택", new Vector2(0.07f, 0.86f), new Vector2(0.30f, 0.91f), font, 16f, SecondaryTextTint);
        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Pot", "뚝배기", new Vector2(0.38f, 0.67f), new Vector2(0.66f, 0.72f), font, 16f, SecondaryTextTint);
        AddUiLabel(kitchenPanel != null ? kitchenPanel.transform : null, "SectionLabel_Shelf", "재료 선반", new Vector2(0.71f, 0.71f), new Vector2(0.93f, 0.76f), font, 16f, SecondaryTextTint);

        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultFood", "완성 음식", new Vector2(0.07f, 0.79f), new Vector2(0.37f, 0.84f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultReaction", "손님 반응", new Vector2(0.08f, 0.54f), new Vector2(0.51f, 0.59f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultClue", "플레이어 대화", new Vector2(0.56f, 0.54f), new Vector2(0.91f, 0.59f), font, 15f, MutedTextTint);
        AddUiLabel(resultPanel != null ? resultPanel.transform : null, "SectionLabel_ResultUnlock", "해금 기록", new Vector2(0.56f, 0.31f), new Vector2(0.91f, 0.36f), font, 15f, MutedTextTint);
        AddUiDivider(resultPanel != null ? resultPanel.transform : null, "SectionRule_ResultLeft", new Vector2(0.08f, 0.535f), new Vector2(0.51f, 0.545f));
        AddUiDivider(resultPanel != null ? resultPanel.transform : null, "SectionRule_ResultRight", new Vector2(0.56f, 0.535f), new Vector2(0.91f, 0.545f));

        ApplyTextBlockBackdrop(reactionText);
        ApplyTextBlockBackdrop(clueText);
        ApplyTextBlockBackdrop(unlockMenuText);
    }

    private void ApplyTextPlacementPolish()
    {
        Transform bottomPanel = FindChildRecursive(customerPanel != null ? customerPanel.transform : null, "BottomPanel");

        SetRelativeRect(portraitImage, new Vector2(0.24f, 0.35f), new Vector2(0.76f, 0.74f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nameText, new Vector2(0.12f, 0.10f), new Vector2(0.88f, 0.25f), Vector2.zero, Vector2.zero);
        SetTextAlignment(nameText, TextAlignmentOptions.Center);

        SetRelativeRect(customerSpeechText, new Vector2(0.09f, 0.16f), new Vector2(0.91f, 0.74f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(customerSpeechText, Vector4.zero);
        SetTextAlignment(customerSpeechText, TextAlignmentOptions.Center);

        Transform dialogueBox = FindChildRecursive(bottomPanel, "DialogueBox");
        SetRelativeRect(dialogueBox, new Vector2(0.04f, 0.24f), new Vector2(0.70f, 0.80f), Vector2.zero, Vector2.zero);
        SetRelativeRect(dialogueText, new Vector2(0.07f, 0.16f), new Vector2(0.93f, 0.84f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(dialogueText, Vector4.zero);
        SetTextAlignment(dialogueText, TextAlignmentOptions.Center);

        SetRelativeRect(choiceGroup, new Vector2(0.04f, 0.06f), new Vector2(0.70f, 0.18f), Vector2.zero, Vector2.zero);
        SetRelativeRect(menuOpenButton, new Vector2(0.77f, 0.65f), new Vector2(0.95f, 0.82f), Vector2.zero, Vector2.zero);
        SetRelativeRect(nextButton, new Vector2(0.77f, 0.42f), new Vector2(0.95f, 0.59f), Vector2.zero, Vector2.zero);
        SetRelativeRect(goKitchenButton, new Vector2(0.77f, 0.42f), new Vector2(0.95f, 0.59f), Vector2.zero, Vector2.zero);

        SetRelativeRect(selectedRecipeText, new Vector2(0.07f, 0.76f), new Vector2(0.30f, 0.84f), Vector2.zero, Vector2.zero);
        SetRelativeRect(ingredientGuideText, new Vector2(0.37f, 0.75f), new Vector2(0.66f, 0.83f), Vector2.zero, Vector2.zero);
        SetRelativeRect(cookingPotHintText, new Vector2(0.39f, 0.37f), new Vector2(0.65f, 0.58f), Vector2.zero, Vector2.zero);
        SetTextAlignment(selectedRecipeText, TextAlignmentOptions.Center);
        SetTextAlignment(ingredientGuideText, TextAlignmentOptions.Center);
        SetTextAlignment(cookingPotHintText, TextAlignmentOptions.Center);

        SetRelativeRect(resultText, new Vector2(0.39f, 0.60f), new Vector2(0.91f, 0.76f), Vector2.zero, Vector2.zero);
        SetRelativeRect(reactionText, new Vector2(0.08f, 0.23f), new Vector2(0.51f, 0.54f), Vector2.zero, Vector2.zero);
        SetRelativeRect(clueText, new Vector2(0.56f, 0.34f), new Vector2(0.91f, 0.54f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockTitleText, new Vector2(0.56f, 0.25f), new Vector2(0.91f, 0.31f), Vector2.zero, Vector2.zero);
        SetRelativeRect(unlockMenuText, new Vector2(0.56f, 0.18f), new Vector2(0.91f, 0.24f), Vector2.zero, Vector2.zero);
        ApplyTextBoxPadding(reactionText, new Vector4(10f, 6f, 10f, 6f));
        ApplyTextBoxPadding(clueText, new Vector4(10f, 6f, 10f, 6f));
        SetTextAlignment(resultText, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(reactionText, TextAlignmentOptions.TopLeft);
        SetTextAlignment(clueText, TextAlignmentOptions.TopLeft);
        SetTextAlignment(unlockTitleText, TextAlignmentOptions.MidlineLeft);
        SetTextAlignment(unlockMenuText, TextAlignmentOptions.MidlineLeft);
    }

    private void ApplyColorPreset()
    {
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
        ApplyPaperFrame(resultPanel, "식사 평가", new Color32(151, 57, 39, 255), referenceFont);
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

        ApplyTextStyle(slot1Text, bodyFont, 16f, FontStyles.Normal, MutedTextTint);
        ApplyTextStyle(slot2Text, bodyFont, 16f, FontStyles.Normal, MutedTextTint);
        ApplyTextStyle(slot3Text, bodyFont, 16f, FontStyles.Normal, MutedTextTint);

        ApplyTextStyle(resultText, bodyFont, 28f, FontStyles.Bold, PrimaryTextTint);
        ApplyTextStyle(reactionText, bodyFont, 17f, FontStyles.Normal, SecondaryTextTint);
        ApplyTextStyle(clueText, bodyFont, 16f, FontStyles.Normal, MutedTextTint);
        ApplyTextStyle(unlockTitleText, bodyFont, 24f, FontStyles.Bold, WarningTextTint);
        ApplyTextStyle(unlockMenuText, bodyFont, 20f, FontStyles.Normal, PrimaryTextTint);

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

        return LoadFontAsset("Korean_Full_TMP");
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

        target.margin = margin;
        target.alignment = TextAlignmentOptions.MidlineLeft;
        target.overflowMode = TextOverflowModes.Ellipsis;
        target.textWrappingMode = TextWrappingModes.Normal;
    }

    private static void ApplyTextRhythm(TMP_Text target, float characterSpacing, float lineSpacing, float paragraphSpacing)
    {
        if (target == null)
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
        if (showCustomer)
            StretchPanel(customerPanel, new Vector2(0.18f, 0.07f), new Vector2(0.82f, 0.93f));

        if (showKitchen)
            StretchPanel(kitchenPanel, new Vector2(0.12f, 0.07f), new Vector2(0.88f, 0.93f));

        if (showResult)
            StretchPanel(resultPanel, new Vector2(0.20f, 0.10f), new Vector2(0.80f, 0.90f));
    }

    private void SetPanelState(bool showCustomer, bool showKitchen, bool showResult)
    {
        SetActive(customerPanel, showCustomer);
        SetActive(kitchenPanel, showKitchen);
        SetActive(resultPanel, showResult);
        ApplyActivePanelLayout(showCustomer, showKitchen, showResult);
    }

    private void ClearIngredientOptions()
    {
        for (int i = 0; i < currentIngredientOptions.Length; i++)
            currentIngredientOptions[i] = string.Empty;
    }

    private void ShowPotHint(string message)
    {
        SetText(cookingPotHintText, message);
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
