using UnityEngine;
using UnityEngine.UI;

public class StoreCont : MonoBehaviour
{
    public Image iconImg;
    public Text conText;
    public Text goldText;
    
    public void SetItem(Sprite sprite, string conText, string goldText)
    {
        iconImg.sprite = sprite;
        this.conText.text = conText;
        this.goldText.text = goldText;
    }
}
