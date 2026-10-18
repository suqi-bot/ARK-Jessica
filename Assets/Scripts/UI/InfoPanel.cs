using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InfoPanel : BasePanel
{
    public Image backImg;//背景图片
    public Text infoText;//提示信息
    public int imgWidth = 1220; //图片宽度
    private float tempTime;//临时时间
    public override void Init()
    {
        backImg.rectTransform.sizeDelta = new Vector2(0 , backImg.rectTransform.sizeDelta.y);
        tempTime = 0;
        Invoke("HideSelf", 3);
    }

    protected override void Update()
    {
        base.Update();
        tempTime += Time.deltaTime;
        if (tempTime < 1)
            UpdateImg();
    }

    /// <summary>
    /// 设置log信息
    /// </summary>
    /// <param name="info"></param>
    public void SetInfoText(string info)
    {
        infoText.text = info;
    }

    /// <summary>
    /// 更新背景动画
    /// </summary>
    public void UpdateImg()
    {
        backImg.rectTransform.sizeDelta = new Vector2(imgWidth * tempTime, backImg.rectTransform.sizeDelta.y);
    }
    
    public void HideSelf()
    {
        UIManager.Instance.HidePanel<InfoPanel>(false);
    }
}
