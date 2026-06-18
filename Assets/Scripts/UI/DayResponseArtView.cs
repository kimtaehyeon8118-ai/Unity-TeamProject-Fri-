using UnityEngine;
using UnityEngine.UI;
using TMPro;

public sealed class DayResponseArtView : MonoBehaviour
{
    [Header("Background Layers")]
    public Image backgroundBack;
    public Image npcImage;
    public Image backgroundFrame;
    public Image mainOverlay;

    [Header("Buttons")]
    public Button optionButton;
    public Button recipeButton;
    public TMP_Text recipeButtonLabelText;
    public Button noteButton;
    public TMP_Text noteButtonLabelText;
    public Button dialogueAdvanceButton;

    [Header("HUD Texts")]
    public TMP_Text timeText;
    public TMP_Text dayText;

    [Header("Customer Info")]
    public TMP_Text npcInfoTitleText;
    public TMP_Text npcInfoText;

    [Header("Dialogue")]
    public TMP_Text speakerText;
    public TMP_Text dialogueText;
    public Button goToKitchenButton;
    public TMP_Text goToKitchenButtonText;

    [Header("Popups")]
    public Button dimButton;
    public GameObject recipePopup;
    public Button[] recipePopupButtons;
    public TMP_Text[] recipePopupButtonTexts;
    public TMP_Text recipePopupTitleText;
    public TMP_Text recipePopupText;
    public GameObject memoPopup;
    public TMP_InputField memoInputField;

    [Header("Designer Sprite Overrides")]
    public Sprite backgroundBackSprite;
    public Sprite npcSprite;
    public Sprite backgroundFrameSprite;
    public Sprite mainOverlaySprite;
    public Sprite optionButtonSprite;
    public Sprite recipeButtonSprite;
    public Sprite noteButtonSprite;
    public Sprite dialogueAdvanceButtonSprite;
    public Sprite goToKitchenButtonSprite;
    public Sprite recipePopupSprite;
    public Sprite memoPopupSprite;
    public DaySceneSpriteOverrideSlot[] extraSpriteOverrides;

    private void Awake()
    {
        ApplyDesignerSpriteOverrides();
    }

    private void OnEnable()
    {
        ApplyDesignerSpriteOverrides();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ApplyDesignerSpriteOverrides();
    }
#endif

    [ContextMenu("Apply Designer Sprite Overrides")]
    public void ApplyDesignerSpriteOverrides()
    {
        ApplySprite(backgroundBack, backgroundBackSprite, preserveAspect: false);
        ApplySprite(npcImage, npcSprite);
        ApplySprite(backgroundFrame, backgroundFrameSprite, preserveAspect: false);
        ApplySprite(mainOverlay, mainOverlaySprite, preserveAspect: false);
        ApplyButtonSprite(optionButton, optionButtonSprite);
        ApplyButtonSprite(recipeButton, recipeButtonSprite);
        ApplyButtonSprite(noteButton, noteButtonSprite);
        ApplyButtonSprite(dialogueAdvanceButton, dialogueAdvanceButtonSprite);
        ApplyButtonSprite(goToKitchenButton, goToKitchenButtonSprite);
        ApplySprite(recipePopup != null ? recipePopup.GetComponent<Image>() : null, recipePopupSprite, preserveAspect: false);
        ApplySprite(memoPopup != null ? memoPopup.GetComponent<Image>() : null, memoPopupSprite, preserveAspect: false);
        ApplyExtraSpriteOverrides();
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

    private static void ApplyButtonSprite(Button button, Sprite sprite)
    {
        if (button == null)
            return;

        ApplySprite(button.GetComponent<Image>(), sprite, preserveAspect: true);
    }

    private static void ApplySprite(Image image, Sprite sprite, bool preserveAspect = true)
    {
        if (image == null || sprite == null)
            return;

        image.sprite = sprite;
        image.preserveAspect = preserveAspect;
        image.color = Color.white;
    }
}
