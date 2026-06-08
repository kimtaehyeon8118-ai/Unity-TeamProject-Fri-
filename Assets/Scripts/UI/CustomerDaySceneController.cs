using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CustomerDaySceneController : MonoBehaviour
{
    [Header("State")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private string fixedTimeText = "12:00";

    [Header("Data")]
    [SerializeField] private CustomerNpcData npcData = new CustomerNpcData();
    [SerializeField] private DialogueLine[] dialogueLines =
    {
        new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "안녕하세요. 오늘은 얼큰한 찌개가 먹고 싶네요." },
        new DialogueLine { speakerName = "김태현", speakerRoleLabel = "플레이어", dialogueText = "어서 오세요. 어떤 맛을 좋아하시나요?" },
        new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "너무 맵지는 않고, 국물이 깊은 음식이면 좋겠습니다." },
        new DialogueLine { speakerName = "김태현", speakerRoleLabel = "플레이어", dialogueText = "알겠습니다. 어울리는 음식을 준비해보겠습니다." },
        new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "기대하겠습니다." }
    };
    [SerializeField] private RecipePopupEntry[] recipes =
    {
        new RecipePopupEntry { recipeName = "김치찌개", unlockDay = 1, recipeContent = "- 김치\n- 돼지고기\n- 버섯" },
        new RecipePopupEntry { recipeName = "된장찌개", unlockDay = 1, recipeContent = "- 된장\n- 두부\n- 버섯" },
        new RecipePopupEntry { recipeName = "순두부찌개", unlockDay = 1, recipeContent = "- 순두부\n- 고춧가루\n- 버섯" }
    };

    [Header("Scene Panels")]
    [SerializeField] private GameObject customerPanel;
    [SerializeField] private GameObject kitchenPanel;
    [SerializeField] private GameObject dayArtLayer;

    [Header("HUD")]
    [SerializeField] private TMP_Text timeText;
    [SerializeField] private TMP_Text dayText;

    [Header("Views")]
    [SerializeField] private DayResponseArtView artView;
    [SerializeField] private NpcInfoView npcInfoView;
    [SerializeField] private DialogueView dialogueView;
    [SerializeField] private RecipePopupView recipePopupView;
    [SerializeField] private MemoPopupView memoPopupView;
    [SerializeField] private PopupRootController popupRoot;

    [Header("Buttons")]
    [SerializeField] private Button recipeButton;
    [SerializeField] private Button memoButton;
    [SerializeField] private Button settingsButton;

    private void OnEnable()
    {
        StartCoroutine(InitializeAfterLegacyUi());
    }

    private IEnumerator InitializeAfterLegacyUi()
    {
        yield return null;
        yield return new WaitForEndOfFrame();
        AutoBindSceneReferences();
        ValidateRequiredReferences();
        RefreshAll();
        BindButtons();
    }

    public void RefreshAll()
    {
        ApplyProgressionContent();
        SetText(timeText, fixedTimeText);
        SetText(dayText, currentDay + "일차");

        if (npcInfoView != null)
            npcInfoView.SetNpc(npcData);

        if (dialogueView != null)
            dialogueView.SetLines(dialogueLines);

        if (recipePopupView != null)
            recipePopupView.SetRecipes(currentDay, recipes);

        if (popupRoot != null)
            popupRoot.Initialize();
    }

    private void ApplyProgressionContent()
    {
        currentDay = Mathf.Clamp(GameProgression.GetCurrentDayNumber(), 1, 3);
        fixedTimeText = "12:00";

        if (currentDay == 1)
        {
            npcData = new CustomerNpcData
            {
                npcName = "강태수",
                gender = "남성",
                job = "손님",
                trait = "얼큰한 국물 요리를 좋아함"
            };
            dialogueLines = new[]
            {
                NpcLine("강태수", "불 냄새만 맡으면 아직도 그날이 떠올라."),
                PlayerLine("그 손으로 지금도 버티고 계시잖아요. 오늘은 손님을 위한 따뜻한 한 끼부터 만들게요."),
                NpcLine("강태수", "내 딸도... 아내도... 난 왜 살아있지..."),
                PlayerLine("살아남은 이유를 오늘 다 설명하지 않아도 괜찮아요. 그래도 지금은 드셔야 해요."),
                NpcLine("강태수", "매콤한 냄새가 그리워... 쉬는 날마다 집에서 나던 그 냄새가."),
                PlayerLine("그 기억에 가까운 음식이라면, 김치찌개가 좋겠네요. 매콤하고 뜨거운 국물로요."),
                NpcLine("강태수", "어이 주인장. 날 위해서 매콤한 음식 만들어줄 수 있나?"),
                PlayerLine("알겠습니다. 그날의 냄새가 아니라 집의 냄새가 떠오르도록 끓여볼게요."),
                PlayerLine("혹시 김치찌개에서 어떤 식재료를 좋아하세요?"),
                NpcLine("강태수", "두부가 좋더군. 매운 국물 사이에서 부드럽게 풀리는 게... 그 사람도 꼭 넣었어."),
                PlayerLine("좋아요. 김치와 돼지고기에 두부를 넉넉히 넣어서, 그 기억에 가까운 맛으로 끓여볼게요."),
                NpcLine("강태수", "그래.. 맛있는 한 끼 부탁하마..")
            };
        }
        else if (currentDay == 2)
        {
            npcData = new CustomerNpcData
            {
                npcName = "윤서아",
                gender = "여성",
                job = "의료진",
                trait = "따뜻하고 부드러운 음식을 원함"
            };
            dialogueLines = new[]
            {
                PlayerLine("어서 오세요. 많이 지쳐 보이세요. 잠시 앉아서 숨부터 고르셔도 괜찮아요."),
                NpcLine("윤서아", "괜찮다고... 환자들과 그 가족들에게 거짓말 하는 것도... 이제 너무 지쳤어요.."),
                PlayerLine("계속 버티느라 마음이 많이 닳으셨겠어요. 여기서는 괜찮은 척하지 않으셔도 됩니다."),
                NpcLine("윤서아", "살려달라는 말을 너무 많이 들었어요..."),
                PlayerLine("그 말들이 아직도 마음에 남아 있는 거군요. 오늘은 자극적이지 않고 속을 감싸주는 음식이 좋겠어요."),
                NpcLine("윤서아", "혹시 따뜻하고 부드러운 음식... 만들어 주실 수 있으신가요?"),
                PlayerLine("그럼 순두부찌개로 따뜻하게 끓여볼게요. 순두부찌개에서 특히 좋아하는 재료가 있으세요?"),
                NpcLine("윤서아", "순두부요. 부드럽게 넘어가는 게 좋고, 고춧가루가 적당히 들어갔으면 좋겠어요. 참고로 전 매운 걸 잘 못 먹어요. 버섯 향이 있으면 마음이 조금 가라앉고, 조개가 들어가면 더 좋을 것 같아요."),
                PlayerLine("좋아요. 순두부와 적당한 고춧가루, 버섯을 넣고 조개로 시원하게 끓여볼게요."),
                NpcLine("윤서아", "부탁드릴게요. 오늘은 조용히 속을 데우고 싶어요.")
            };
        }
        else
        {
            npcData = new CustomerNpcData
            {
                npcName = "민준",
                gender = "남성",
                job = "학생",
                trait = "집밥 같은 된장찌개를 그리워함"
            };
            dialogueLines = new[]
            {
                PlayerLine("어서 와요. 여기엔 안전하게 쉬어갈 수 있어요. 천천히 앉아도 괜찮아요."),
                NpcLine("민준", "저기... 여기엔 좀비들 없죠?..."),
                PlayerLine("없어요. 지금은 문도 닫혀 있고, 제가 곁에 있어요. 배부터 조금 채워볼까요?"),
                NpcLine("민준", "솔직히 배고픈 건 참을 수 있는데,... 혼자 사는 건... 무서워요..."),
                PlayerLine("혼자 버틴 시간이 너무 길었겠네요. 여기서는 밝은 척하지 않아도 괜찮아요."),
                NpcLine("민준", "혹시... 된장찌개... 가능할까요?..."),
                PlayerLine("가능해요. 그런데 민준이가 떠올리는 집밥의 안전한 맛을 조금 더 따라가볼게요."),
                NpcLine("민준", "엄마가 시험날에 먹고 가라고 하셨는데 안 먹은 게 후회가..."),
                PlayerLine("그 기억이라면 따뜻한 된장찌개가 잘 맞을 것 같아요. 엄마가 차려준 밥상처럼 끓여볼게요."),
                NpcLine("민준", "엄마의 손 맛을 느끼고 싶어요. 애호박도 좋아해요."),
                PlayerLine("좋아요. 된장과 두부, 버섯을 넣어서 집밥처럼 따뜻하게 끓여볼게요.")
            };
        }

        recipes = new[]
        {
            new RecipePopupEntry { recipeName = "김치찌개", unlockDay = 1, recipeContent = "- 김치\n- 돼지고기\n- 버섯" },
            new RecipePopupEntry { recipeName = "된장찌개", unlockDay = 1, recipeContent = "- 된장\n- 두부\n- 버섯" },
            new RecipePopupEntry { recipeName = "순두부찌개", unlockDay = 1, recipeContent = "- 순두부\n- 고춧가루\n- 버섯" }
        };
    }

    private static DialogueLine NpcLine(string speakerName, string text)
    {
        return new DialogueLine
        {
            speakerName = speakerName,
            speakerRoleLabel = "NPC",
            dialogueText = text
        };
    }

    private static DialogueLine PlayerLine(string text)
    {
        return new DialogueLine
        {
            speakerName = "플레이어",
            speakerRoleLabel = string.Empty,
            dialogueText = text
        };
    }

    public void ConfigureSceneReferences(GameObject customer, GameObject kitchen, GameObject artLayer)
    {
        customerPanel = customer;
        kitchenPanel = kitchen;
        dayArtLayer = artLayer;
    }

    public void ConfigureDefaultContent()
    {
        currentDay = 1;
        fixedTimeText = "12:00";
        npcData = new CustomerNpcData
        {
            npcName = "강태수",
            gender = "남성",
            job = "손님",
            trait = "얼큰한 국물 요리를 좋아함"
        };
        dialogueLines = new[]
        {
            new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "안녕하세요. 오늘은 얼큰한 찌개가 먹고 싶네요." },
            new DialogueLine { speakerName = "김태현", speakerRoleLabel = "플레이어", dialogueText = "어서 오세요. 어떤 맛을 좋아하시나요?" },
            new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "너무 맵지는 않고, 국물이 깊은 음식이면 좋겠습니다." },
            new DialogueLine { speakerName = "김태현", speakerRoleLabel = "플레이어", dialogueText = "알겠습니다. 어울리는 음식을 준비해보겠습니다." },
            new DialogueLine { speakerName = "강태수", speakerRoleLabel = "NPC", dialogueText = "기대하겠습니다." }
        };
        recipes = new[]
        {
            new RecipePopupEntry { recipeName = "김치찌개", unlockDay = 1, recipeContent = "- 김치\n- 돼지고기\n- 버섯" },
            new RecipePopupEntry { recipeName = "된장찌개", unlockDay = 1, recipeContent = "- 된장\n- 두부\n- 버섯" },
            new RecipePopupEntry { recipeName = "순두부찌개", unlockDay = 1, recipeContent = "- 순두부\n- 고춧가루\n- 버섯" }
        };
    }

    private void BindButtons()
    {
        if (recipeButton != null)
        {
            recipeButton.onClick.RemoveAllListeners();
            recipeButton.onClick.AddListener(OpenRecipePopup);
        }

        if (memoButton != null)
        {
            memoButton.onClick.RemoveAllListeners();
            memoButton.onClick.AddListener(OpenMemoPopup);
        }

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(OnClickSettings);
        }

        if (dialogueView != null)
            dialogueView.BindActions(GoToKitchen);
    }

    private void AutoBindSceneReferences()
    {
        if (artView == null)
            artView = GetComponent<DayResponseArtView>();

        if (artView == null)
            artView = FindAnyObjectByType<DayResponseArtView>();

        if (artView != null)
        {
            if (timeText == null)
                timeText = artView.timeText;

            if (dayText == null)
                dayText = artView.dayText;

            if (recipeButton == null)
                recipeButton = artView.recipeButton;

            if (memoButton == null)
                memoButton = artView.noteButton;

            if (settingsButton == null)
                settingsButton = artView.optionButton;

            if (dayArtLayer == null)
                dayArtLayer = artView.gameObject;
        }

        GameObject canvas = GameObject.Find("Canvas");
        if (canvas == null)
            return;

        if (customerPanel == null)
        {
            Transform target = canvas.transform.Find("CustomerPanel");
            customerPanel = target != null ? target.gameObject : null;
        }

        if (kitchenPanel == null)
        {
            Transform target = canvas.transform.Find("KitchenPanel");
            kitchenPanel = target != null ? target.gameObject : null;
        }

        if (dayArtLayer == null)
        {
            Transform target = canvas.transform.Find("DayArtLayer");
            dayArtLayer = target != null ? target.gameObject : null;
        }
    }

    private void ValidateRequiredReferences()
    {
        LogMissing(customerPanel, "CustomerPanel");
        LogMissing(kitchenPanel, "KitchenPanel");
        LogMissing(dayArtLayer, "DayArtLayer");
        LogMissing(timeText, "TimeText");
        LogMissing(dayText, "DayText");
        LogMissing(npcInfoView, "NpcInfoView");
        LogMissing(dialogueView, "DialogueView");
        LogMissing(recipePopupView, "RecipePopupView");
        LogMissing(memoPopupView, "MemoPopupView");
        LogMissing(popupRoot, "PopupRootController");
    }

    public void OpenRecipePopup()
    {
        if (recipePopupView != null)
            recipePopupView.SetRecipes(currentDay, recipes);

        if (popupRoot != null)
            popupRoot.ShowRecipe();
    }

    public void OpenMemoPopup()
    {
        if (popupRoot != null)
            popupRoot.ShowMemo();
    }

    public void GoToKitchen()
    {
        AutoBindSceneReferences();

        if (customerPanel != null)
            customerPanel.SetActive(false);

        if (dayArtLayer != null)
            dayArtLayer.SetActive(false);

        if (kitchenPanel != null)
        {
            kitchenPanel.SetActive(true);
            DisableKitchenBackButtons(kitchenPanel.transform);
        }

        Debug.Log("[CustomerDaySceneController] Switched to KitchenPanel.");
    }

    private void OnClickSettings()
    {
        Debug.Log("설정 버튼 클릭: 아직 연결된 설정 기능은 없습니다.");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    private static void LogMissing(Object target, string referenceName)
    {
        if (target == null)
            Debug.LogWarning("[CustomerDaySceneController] Missing reference: " + referenceName);
    }

    private static void DisableKitchenBackButtons(Transform root)
    {
        if (root == null)
            return;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            if (IsBackButton(button.transform))
                button.gameObject.SetActive(false);
        }
    }

    private static bool IsBackButton(Transform target)
    {
        string name = target.name.ToLowerInvariant();
        if (name.Contains("back") || name.Contains("return") || name.Contains("close"))
            return true;

        TMP_Text label = target.GetComponentInChildren<TMP_Text>(true);
        if (label == null)
            return false;

        string text = label.text;
        return text.Contains("뒤로") || text.Contains("돌아") || text.Contains("Back") || text.Contains("Return");
    }
}
