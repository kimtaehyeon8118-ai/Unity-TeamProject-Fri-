using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueView : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button advanceButton;
    [SerializeField] private Button goToKitchenButton;
    [SerializeField] private TMP_Text goToKitchenButtonText;

    private IList<DialogueLine> lines;
    private int currentIndex;

    public event Action Finished;

    public void Bind(
        TMP_Text speaker,
        TMP_Text dialogue,
        Button advance,
        Button kitchenButton,
        TMP_Text kitchenButtonText)
    {
        speakerText = speaker;
        dialogueText = dialogue;
        advanceButton = advance;
        goToKitchenButton = kitchenButton;
        goToKitchenButtonText = kitchenButtonText;
    }

    public void SetLines(IList<DialogueLine> dialogueLines)
    {
        lines = dialogueLines;
        currentIndex = 0;
        SetKitchenButtonVisible(false);
        ShowCurrentLine();
    }

    public void BindActions(Action advanceAction, Action kitchenAction)
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveAllListeners();
            advanceButton.onClick.AddListener(() => advanceAction());
        }

        if (goToKitchenButton != null)
        {
            goToKitchenButton.onClick.RemoveAllListeners();
            goToKitchenButton.onClick.AddListener(() => kitchenAction());
        }
    }

    public void Advance()
    {
        if (lines == null || lines.Count == 0)
            return;

        if (currentIndex < lines.Count - 1)
        {
            currentIndex++;
            ShowCurrentLine();
            return;
        }

        SetKitchenButtonVisible(true);
        if (advanceButton != null)
            advanceButton.interactable = false;

        if (Finished != null)
            Finished.Invoke();
    }

    private void ShowCurrentLine()
    {
        if (lines == null || lines.Count == 0)
        {
            SetText(speakerText, string.Empty);
            SetText(dialogueText, string.Empty);
            return;
        }

        DialogueLine line = lines[Mathf.Clamp(currentIndex, 0, lines.Count - 1)];
        SetText(speakerText, line.speakerName);
        SetText(dialogueText, line.dialogueText);

        if (advanceButton != null)
            advanceButton.interactable = true;
    }

    private void SetKitchenButtonVisible(bool visible)
    {
        if (goToKitchenButton != null)
            goToKitchenButton.gameObject.SetActive(visible);

        SetText(goToKitchenButtonText, "주방으로 이동");
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }
}
