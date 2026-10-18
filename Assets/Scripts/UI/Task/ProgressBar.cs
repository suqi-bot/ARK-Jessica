using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    public Image backgroudImg; //背景图片
    public Image fillImg;//前景图片
    public Text progressText; //进度文本

    /// <summary>
    /// 设置进度
    /// </summary>
    /// <param name="progress"></param>
    public void SetProgress(int progress, int target_amount)
    {
        progressText.text = progress + "/" + target_amount;
        if (progress > target_amount) 
            progress = target_amount;
        float temp = backgroudImg.rectTransform.sizeDelta.x;
        fillImg.rectTransform.sizeDelta = new Vector2(Mathf.Max(0, (float) progress / target_amount * temp), fillImg.rectTransform.sizeDelta.y);
        
    }
}
