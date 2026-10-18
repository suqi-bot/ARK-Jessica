using UnityEngine;
using UnityEngine.UI;

public class ItemOc : MonoBehaviour
{
    public Button deleteBtn; // 删除按钮
    public Button useBtn; // 使用按钮
    public BagPanel bagPanel; // 背包面板 
    private int id; // 物品id
    private string itemName;// 物品名称
    private int type;// 物品类型
    private IUsed user;
    void Start()
    {
        
        deleteBtn.onClick.AddListener(() =>
        {

            bagPanel.RemoveBagItem(id, type);
        });
        useBtn.onClick.AddListener(() =>
        {
            user = Resources.Load<HpRecord>("Item/"+ itemName);
            bagPanel.UseBagItem(user, id);
        });
    }

    public void SetId(int id, int type, string itemName)
    {
        this.id = id;
        this.type = type;
        this.itemName = itemName;
    }
    
    public void ShowMe()
    {
        this.gameObject.SetActive(true);
    }

    public void HideMe()
    {
        gameObject.SetActive(false);
    }
}
