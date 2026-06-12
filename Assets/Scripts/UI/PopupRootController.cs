using UnityEngine;
using UnityEngine.UI;

public sealed class PopupRootController : MonoBehaviour
{
    [SerializeField] private Button dimButton;
    [SerializeField] private GameObject recipePopup;
    [SerializeField] private GameObject memoPopup;

    private void Awake()
    {
        Initialize();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            CloseAll();
    }

    public void Bind(Button dim, GameObject recipe, GameObject memo)
    {
        dimButton = dim;
        recipePopup = recipe;
        memoPopup = memo;
    }

    public void Initialize()
    {
        CloseAll();

        if (dimButton != null)
        {
            dimButton.onClick.RemoveAllListeners();
            dimButton.onClick.AddListener(CloseAll);
        }
    }

    public void ShowRecipe()
    {
        ShowOnly(recipePopup);
    }

    public void ShowMemo()
    {
        ShowOnly(memoPopup);
    }

    public void CloseAll()
    {
        SetActive(recipePopup, false);
        SetActive(memoPopup, false);

        if (dimButton != null)
            dimButton.gameObject.SetActive(false);
    }

    private void ShowOnly(GameObject popup)
    {
        SetActive(recipePopup, popup == recipePopup);
        SetActive(memoPopup, popup == memoPopup);

        if (dimButton != null)
            dimButton.gameObject.SetActive(popup != null);
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
