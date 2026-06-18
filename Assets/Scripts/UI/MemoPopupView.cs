using TMPro;
using UnityEngine;

public sealed class MemoPopupView : MonoBehaviour
{
    [SerializeField] private TMP_InputField memoInput;

    public string MemoText
    {
        get { return memoInput != null ? memoInput.text : string.Empty; }
    }

    public void Bind(TMP_InputField input)
    {
        memoInput = input;
    }
}
