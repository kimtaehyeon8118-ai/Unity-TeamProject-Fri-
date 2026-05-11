using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TitleScreenController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "DayScene";
    [SerializeField] private Text titleText;
    [SerializeField] private Text promptText;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (titleText != null)
        {
            titleText.text = "Taehyeon 2NE";
        }

        if (promptText != null)
        {
            promptText.text = "Press Space, Enter, or South Button to Start";
        }
    }

    private void Update()
    {
        if (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame || Keyboard.current.enterKey.wasPressedThisFrame))
        {
            LoadGameplay();
            return;
        }

        if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
        {
            LoadGameplay();
        }
    }

    public void LoadGameplay()
    {
        int buildIndex = SceneFlowUtility.FindSceneIndexByName(gameplaySceneName);
        SceneManager.LoadScene(buildIndex >= 0 ? buildIndex : 0);
    }
}
