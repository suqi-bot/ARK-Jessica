using UnityEngine;
using UnityEngine.UI;


public class ItemBar : MonoBehaviour
{
    public Image iconImg; //图标图片
    public Text iconContext; // 图标描述
    public Text itemNum; // 物品数量和价格描述
    /// <summary>
    /// 设置图标和描述信息
    /// </summary>
    /// <param name="icon"></param>
    /// <param name="text"></param>
    public void ShowIcon(Item item , int num)
    {
        iconImg.sprite = Resources.Load<Sprite>("Icon/"+ item.icon+"_icon");
        iconContext.text = item.tips;
        itemNum.text = "价值：" + item.price + "\n" + "拥有：X" + num;
    }
        

 
}

