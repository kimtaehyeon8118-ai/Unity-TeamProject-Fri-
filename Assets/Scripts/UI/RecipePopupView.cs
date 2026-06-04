using TMPro;
using UnityEngine;

public sealed class RecipePopupView : MonoBehaviour
{
    [SerializeField] private TMP_Text recipeListText;
    [SerializeField] private string lockedText = "잠김";

    public void Bind(TMP_Text targetText)
    {
        recipeListText = targetText;
    }

    public void SetRecipes(int currentDay, RecipePopupEntry[] recipes)
    {
        if (recipeListText == null || recipes == null)
            return;

        System.Text.StringBuilder builder = new System.Text.StringBuilder();
        for (int i = 0; i < recipes.Length; i++)
        {
            RecipePopupEntry recipe = recipes[i];
            if (recipe == null)
                continue;

            bool isUnlocked = currentDay >= recipe.unlockDay;
            builder.Append(recipe.recipeName);
            builder.Append(": ");
            builder.Append(isUnlocked ? recipe.recipeDetail : recipe.unlockDay + "일차 이후 " + lockedText);

            if (i < recipes.Length - 1)
                builder.AppendLine();
        }

        recipeListText.text = builder.ToString();
    }
}
