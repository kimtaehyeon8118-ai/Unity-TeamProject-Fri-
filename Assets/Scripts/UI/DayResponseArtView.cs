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
    public Button noteButton;
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
    public TMP_Text recipePopupText;
    public GameObject memoPopup;
    public TMP_InputField memoInputField;
}
