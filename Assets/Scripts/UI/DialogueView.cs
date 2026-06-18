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
        StartDialogue();
    }

    public void BindActions(Action kitchenAction)
    {
        if (advanceButton != null)
        {
            advanceButton.onClick.RemoveAllListeners();
            advanceButton.onClick.AddListener(Advance);
        }

        if (goToKitchenButton != null)
        {
            goToKitchenButton.onClick.RemoveAllListeners();
            goToKitchenButton.onClick.AddListener(() => kitchenAction());
        }

        ValidateReferences();
    }

    public void StartDialogue()
    {
        currentIndex = 0;
        RefreshDialogue();
    }

    public void Advance()
    {
        AdvanceDialogue();
    }

    public void AdvanceDialogue()
    {
        if (lines == null || lines.Count == 0)
        {
            SetKitchenButtonVisible(false);
            return;
        }

        if (currentIndex >= lines.Count - 1)
        {
            currentIndex = lines.Count - 1;
            SetKitchenButtonVisible(true);
            return;
        }

        currentIndex++;
        RefreshDialogue();
    }

    private void RefreshDialogue()
    {
        if (lines == null || lines.Count == 0)
        {
            SetText(speakerText, string.Empty);
            SetText(dialogueText, string.Empty);
            SetKitchenButtonVisible(false);
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, lines.Count - 1);

        DialogueLine line = lines[currentIndex];
        SetText(speakerText, line.DisplaySpeakerName);
        SetText(dialogueText, line.dialogueText);

        if (advanceButton != null)
            advanceButton.interactable = true;

        bool isLastLine = currentIndex >= lines.Count - 1;
        SetKitchenButtonVisible(isLastLine);

        if (isLastLine && Finished != null)
            Finished.Invoke();
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

    private void ValidateReferences()
    {
        if (speakerText == null)
            Debug.LogWarning("[DialogueView] Speaker text reference is missing.", this);

        if (dialogueText == null)
            Debug.LogWarning("[DialogueView] Dialogue body text reference is missing.", this);

        if (advanceButton == null)
            Debug.LogWarning("[DialogueView] Dialogue click button reference is missing.", this);

        if (goToKitchenButton == null)
            Debug.LogWarning("[DialogueView] GoToKitchenButton reference is missing.", this);

        if (lines == null || lines.Count == 0)
            Debug.LogWarning("[DialogueView] Dialogue lines are empty.", this);
    }
}
