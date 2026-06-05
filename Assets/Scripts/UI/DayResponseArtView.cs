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
}
