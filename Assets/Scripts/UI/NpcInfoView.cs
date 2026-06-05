using TMPro;
using UnityEngine;

public sealed class NpcInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text infoText;

    public void Bind(TMP_Text title, TMP_Text info)
    {
        titleText = title;
        infoText = info;
    }

    public void SetNpc(CustomerNpcData npcData)
    {
        if (titleText != null)
            titleText.text = "정보";

        if (infoText == null || npcData == null)
            return;

        infoText.text =
            "이름: " + npcData.npcName + "\n" +
            "성별: " + npcData.gender + "\n" +
            "직업: " + npcData.job + "\n" +
            "특징: " + npcData.trait;
    }
}
