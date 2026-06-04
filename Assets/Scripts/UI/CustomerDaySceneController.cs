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
        new DialogueLine { speakerName = "손님", dialogueText = "안녕하세요. 오늘은 얼큰한 찌개를 먹고 싶어요." },
        new DialogueLine { speakerName = "플레이어", dialogueText = "어떤 맛을 좋아하시나요?" }
    };
    [SerializeField] private RecipePopupEntry[] recipes =
    {
        new RecipePopupEntry { recipeName = "김치찌개", recipeDetail = "김치 + 돼지고기 + 물", unlockDay = 1 },
        new RecipePopupEntry { recipeName = "된장찌개", recipeDetail = "된장 + 두부 + 애호박", unlockDay = 1 },
        new RecipePopupEntry { recipeName = "순두부찌개", recipeDetail = "순두부 + 고춧가루 + 달걀", unlockDay = 2 }
    };

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

    private IEnumerator Start()
    {
        yield return null;
        AutoBindFromArtView();
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
            dialogueView.BindActions(dialogueView.Advance, GoToKitchen);
    }

    private void AutoBindFromArtView()
    {
        if (artView == null)
            artView = GetComponent<DayResponseArtView>();

        if (artView == null)
            artView = FindAnyObjectByType<DayResponseArtView>();

        if (artView == null)
            return;

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
        Debug.Log("GoToKitchen requested. Connect this to the kitchen scene later.");
    }

    private void OnClickSettings()
    {
        Debug.Log("Settings button clicked. No settings popup is connected yet.");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
