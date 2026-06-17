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

    [Header("Designer Static Layout")]
    [SerializeField] private bool useStaticDesignerLayout = true;

    private int selectedIndex = -1;

    private void Awake()
    {
        NormalizeRecipeEntries();
        BindButtonEvents();
        ApplyLayout();
        ValidateReferences();
    }

    private void OnEnable()
    {
        NormalizeRecipeEntries();
        ApplyLayout();
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
        ApplyLayout();
    }

    public void SetRecipes(int day, RecipePopupEntry[] recipes)
    {
        recipeEntries = recipes;
        NormalizeRecipeEntries();
        Open(day);
    }

    public void Open(int day)
    {
        currentDay = Mathf.Max(1, day);
        ApplyLayout();
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

    private void ApplyLayout()
    {
        if (useStaticDesignerLayout)
            return;

        RectTransform buttonRoot = null;
        if (recipeButtons != null && recipeButtons.Length > 0 && recipeButtons[0] != null)
            buttonRoot = recipeButtons[0].transform.parent as RectTransform;

        if (buttonRoot != null)
            Stretch(buttonRoot, new Vector2(0.15f, 0.67f), new Vector2(0.85f, 0.82f));

        ApplyButtonRect(0, new Vector2(0.00f, 0.54f), new Vector2(0.48f, 1.00f));
        ApplyButtonRect(1, new Vector2(0.52f, 0.54f), new Vector2(1.00f, 1.00f));
        ApplyButtonRect(2, new Vector2(0.00f, 0.03f), new Vector2(0.48f, 0.47f));

        if (recipeTitleText != null)
        {
            Stretch(recipeTitleText.rectTransform, new Vector2(0.16f, 0.53f), new Vector2(0.84f, 0.62f));
            recipeTitleText.alignment = TextAlignmentOptions.MidlineLeft;
            recipeTitleText.fontSize = 22f;
            recipeTitleText.margin = new Vector4(8f, 0f, 8f, 0f);
            recipeTitleText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (recipeContentText != null)
        {
            Stretch(recipeContentText.rectTransform, new Vector2(0.16f, 0.20f), new Vector2(0.84f, 0.52f));
            recipeContentText.alignment = TextAlignmentOptions.TopLeft;
            recipeContentText.fontSize = 22f;
            recipeContentText.lineSpacing = 8f;
            recipeContentText.margin = new Vector4(8f, 8f, 8f, 8f);
            recipeContentText.textWrappingMode = TextWrappingModes.NoWrap;
        }

        if (recipeButtonTexts == null)
            return;

        foreach (TMP_Text label in recipeButtonTexts)
        {
            if (label == null)
                continue;

            label.alignment = TextAlignmentOptions.Midline;
            label.fontSize = 15f;
            label.margin = new Vector4(4f, 0f, 4f, 0f);
            label.textWrappingMode = TextWrappingModes.NoWrap;
        }
    }

    private void ApplyButtonRect(int index, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (recipeButtons == null || index < 0 || index >= recipeButtons.Length || recipeButtons[index] == null)
            return;

        RectTransform rectTransform = recipeButtons[index].GetComponent<RectTransform>();
        Stretch(rectTransform, anchorMin, anchorMax);
    }

    private static void Stretch(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
    {
        if (rectTransform == null)
            return;

        rectTransform.anchorMin = anchorMin;
        rectTransform.anchorMax = anchorMax;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;
    }

    private bool IsUnlocked(RecipePopupEntry entry)
    {
        if (entry == null)
            return false;

        if (entry.recipeName == "순두부찌개")
            return true;

        return currentDay >= entry.unlockDay;
    }

    private void NormalizeRecipeEntries()
    {
        recipeEntries = new[]
        {
            new RecipePopupEntry { recipeName = "김치찌개", unlockDay = 1, recipeContent = "- 김치\n- 돼지고기\n- 버섯" },
            new RecipePopupEntry { recipeName = "된장찌개", unlockDay = 1, recipeContent = "- 된장\n- 두부\n- 버섯" },
            new RecipePopupEntry { recipeName = "순두부찌개", unlockDay = 1, recipeContent = "- 순두부\n- 고춧가루\n- 버섯" }
        };
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
