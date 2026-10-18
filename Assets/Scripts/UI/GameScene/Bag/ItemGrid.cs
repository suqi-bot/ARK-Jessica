using UnityEngine;
using UnityEngine.UI;

public class ItemGrid : MonoBehaviour
{
    public Image itemImg; // 物品图片
    public Button button; // 物品按钮
    private int id; // 物品id
    private int num; // 物品数量
    private BagPanel bg;
    // Start is called before the first frame update

    private void Start()
    {
        bg = GetComponentInParent<BagPanel>();
        button.onClick.AddListener(() =>
        {
            bg.ClickItemEvent(id , num);    
        });
    }
    /// <summary>
    /// 设置单个物品
    /// </summary>
    /// <param name="id"></param>
    /// <param name="img"></param>
    public void SetItem(int id, Sprite img, int num)
    {
        itemImg.sprite = img;
        this.id = id;
        this.num = num;
    }

    public int getId()
    {
        return id;
    }
    public int getNum()
    {
        return num;
    }
}
