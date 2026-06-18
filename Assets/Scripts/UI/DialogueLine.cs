using System;

[Serializable]
public sealed class DialogueLine
{
    public string speakerName = "강태수";
    public string speakerRoleLabel = "NPC";
    public bool isNarration;
    public string dialogueText = "안녕하세요. 오늘은 얼큰한 찌개를 먹고 싶어요.";

    public string DisplaySpeakerName
    {
        get
        {
            if (isNarration)
                return string.Empty;

            return string.IsNullOrWhiteSpace(speakerRoleLabel)
                ? speakerName
                : speakerName + " [" + speakerRoleLabel + "]";
        }
    }
}
