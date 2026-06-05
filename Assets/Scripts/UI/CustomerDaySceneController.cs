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
        new DialogueLine { speakerName = "강태수 [NPC]", dialogueText = "안녕하세요. 오늘은 얼큰한 찌개를 먹고 싶어요." },
        new DialogueLine { speakerName = "김태현 [플레이어]", dialogueText = "어떤 맛을 좋아하시나요?" }
    };
    [SerializeField] private RecipePopupEntry[] recipes =
    {
        new RecipePopupEntry { recipeName = "김치찌개", recipeDetail = "김치 + 돼지고기 + 물", unlockDay = 1 },
        new RecipePopupEntry { recipeName = "된장찌개", recipeDetail = "된장 + 두부 + 애호박", unlockDay = 1 },
        new RecipePopupEntry { recipeName = "순두부찌개", recipeDetail = "순두부 + 고춧가루 + 달걀", unlockDay = 2 }
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
            new DialogueLine { speakerName = "강태수 [NPC]", dialogueText = "안녕하세요. 오늘은 얼큰한 찌개를 먹고 싶어요." },
            new DialogueLine { speakerName = "김태현 [플레이어]", dialogueText = "어떤 맛을 좋아하시나요?" }
        };
        recipes = new[]
        {
            new RecipePopupEntry { recipeName = "김치찌개", recipeDetail = "김치 + 돼지고기 + 물", unlockDay = 1 },
            new RecipePopupEntry { recipeName = "된장찌개", recipeDetail = "된장 + 두부 + 애호박", unlockDay = 1 },
            new RecipePopupEntry { recipeName = "순두부찌개", recipeDetail = "순두부 + 고춧가루 + 달걀", unlockDay = 2 }
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
            kitchenPanel.SetActive(true);

        Debug.Log("주방으로 이동: CustomerPanel off, DayArtLayer off, KitchenPanel on.");
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
}
