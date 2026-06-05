using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class RecipePopupView : MonoBehaviour
{
    [Header("Day")]
    [SerializeField] private int currentDay = 1;

    [Header("Recipe Data")]
    [SerializeField] private RecipePopupEntry[] recipeEntries;

    [Header("Recipe Buttons")]
    [SerializeField] private Button[] recipeButtons;
    [SerializeField] private TMP_Text[] recipeButtonTexts;

    [Header("Recipe Texts")]
    [SerializeField] private TMP_Text recipeTitleText;
    [SerializeField] private TMP_Text recipeContentText;

    [Header("Locked State")]
    [SerializeField] private string lockedSuffix = "잠김";

    private int selectedIndex = -1;

    private void Awake()
    {
        BindButtonEvents();
        ValidateReferences();
    }

    private void OnEnable()
    {
        Open(currentDay);
    }

    public void Bind(TMP_Text targetText)
    {
        recipeContentText = targetText;
    }

    public void Bind(Button[] buttons, TMP_Text[] buttonTexts, TMP_Text titleText, TMP_Text contentText)
    {
        recipeButtons = buttons;
        recipeButtonTexts = buttonTexts;
        recipeTitleText = titleText;
        recipeContentText = contentText;
        BindButtonEvents();
    }

    public void SetRecipes(int day, RecipePopupEntry[] recipes)
    {
        recipeEntries = recipes;
        Open(day);
    }

    public void Open(int day)
    {
        currentDay = Mathf.Max(1, day);
        RefreshButtons();
        SelectFirstUnlockedRecipe();
    }

    private void BindButtonEvents()
    {
        if (recipeButtons == null)
            return;

        for (int i = 0; i < recipeButtons.Length; i++)
        {
            int capturedIndex = i;
            Button button = recipeButtons[i];
            if (button == null)
                continue;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectRecipe(capturedIndex));
        }
    }

    private void RefreshButtons()
    {
        if (recipeButtons == null || recipeEntries == null)
            return;

        int count = Mathf.Min(recipeButtons.Length, recipeEntries.Length);
        for (int i = 0; i < count; i++)
        {
            RecipePopupEntry entry = recipeEntries[i];
            Button button = recipeButtons[i];
            if (button == null || entry == null)
                continue;

            bool unlocked = IsUnlocked(entry);
            button.interactable = unlocked;

            if (recipeButtonTexts != null && i < recipeButtonTexts.Length && recipeButtonTexts[i] != null)
                recipeButtonTexts[i].text = unlocked ? entry.recipeName : entry.recipeName + "\n" + lockedSuffix;
        }
    }

    private void SelectFirstUnlockedRecipe()
    {
        if (recipeEntries == null)
            return;

        for (int i = 0; i < recipeEntries.Length; i++)
        {
            if (IsUnlocked(recipeEntries[i]))
            {
                SelectRecipe(i);
                return;
            }
        }

        selectedIndex = -1;
        SetText(recipeTitleText, "레시피 없음");
        SetText(recipeContentText, "현재 확인 가능한 레시피가 없습니다.");
    }

    public void SelectRecipe(int index)
    {
        if (recipeEntries == null || index < 0 || index >= recipeEntries.Length)
            return;

        RecipePopupEntry entry = recipeEntries[index];
        if (entry == null || !IsUnlocked(entry))
            return;

        selectedIndex = index;
        SetText(recipeTitleText, entry.recipeName + " 레시피");
        SetText(recipeContentText, entry.recipeContent);
    }

    private bool IsUnlocked(RecipePopupEntry entry)
    {
        return entry != null && currentDay >= entry.unlockDay;
    }

    private void ValidateReferences()
    {
        if (recipeEntries == null || recipeEntries.Length == 0)
            Debug.LogWarning("[RecipePopupView] Recipe entries are empty.", this);

        if (recipeButtons == null || recipeButtons.Length == 0)
            Debug.LogWarning("[RecipePopupView] Recipe buttons are empty.", this);

        if (recipeTitleText == null)
            Debug.LogWarning("[RecipePopupView] RecipeTitleText reference is missing.", this);

        if (recipeContentText == null)
            Debug.LogWarning("[RecipePopupView] RecipeContentText reference is missing.", this);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
