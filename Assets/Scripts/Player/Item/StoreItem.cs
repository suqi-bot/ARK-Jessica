using UnityEngine;
using UnityEngine.UI;

public class StoreItem : MonoBehaviour
{
    public Image itemImg;
    public Button itemBtn;
    public Text itemNum;
    private Item itemInfo;
    private ItemData itemData;
    private void Start()
    {
        itemBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.GetPanel<StorePanel>().cont.SetItem(Resources.Load<Sprite>("Icon/" + itemInfo.icon + "_icon"), itemInfo.tips, itemInfo.price.ToString() + "$");
            UIManager.Instance.GetPanel<StorePanel>().nowItemdata = itemData;
        });
    }

    public void SetItem(ItemData itemData)
    {
        this.itemData = itemData;
        itemInfo = GameDataMgr.Instance.GetItemInfo(itemData.itemId);
        if(itemInfo != null)
        {
            itemImg.sprite = Resources.Load<Sprite>("Icon/" + itemInfo.icon + "_icon");
            itemNum.text = itemData.itemCount.ToString();
        }
        
    }

    public ItemData GetItemData()
    {
        return itemData;
    }

}
