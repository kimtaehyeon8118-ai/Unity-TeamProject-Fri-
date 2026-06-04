using TMPro;
using UnityEngine;

public sealed class NpcInfoView : MonoBehaviour
{
    [SerializeField] private TMP_Text infoText;

    public void Bind(TMP_Text targetText)
    {
        infoText = targetText;
    }

    public void SetNpc(CustomerNpcData npcData)
    {
        if (infoText == null || npcData == null)
            return;

        infoText.text =
            "이름: " + npcData.npcName + "\n" +
            "성별: " + npcData.gender + "\n" +
            "직업: " + npcData.job + "\n" +
            "특징: " + npcData.trait;
    }
}
