using System;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class DaySceneSpriteOverrideSlot
{
    public string label;
    public Image targetImage;
    public Sprite replacementSprite;
    public bool preserveAspect = true;
    public bool keepImageColor = true;
    public Color imageColor = Color.white;

    public void Apply()
    {
        if (targetImage == null || replacementSprite == null)
            return;

        targetImage.sprite = replacementSprite;
        targetImage.preserveAspect = preserveAspect;

        if (!keepImageColor)
            targetImage.color = imageColor;
    }
}
