using System;
using UnityEngine;

[Serializable]
public sealed class RecipePopupEntry
{
    public string recipeName = "김치찌개";
    public int unlockDay = 1;

    [TextArea(3, 8)]
    public string recipeContent = "+ 물\n+ 고춧가루\n+ 돼지고기\n+ 두부(선택)";
}
