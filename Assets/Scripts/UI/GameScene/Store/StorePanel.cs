using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum Store_Type
{
    Store1,
    Store2,
    Store3
}

public class StorePanel : BasePanel
{
    public Button sureBtn;
    public Button cancelBtn;
    public StoreCont cont;
    public Text nowGold;
    public Transform fatherTrans;
    public List<StoreItem> pool;
    public ItemData nowItemdata;
    public Store_Type storeType = Store_Type.Store1;
    public override void Init()
    {
        pool = new List<StoreItem>();
        sureBtn.onClick.AddListener(() =>
        {
            if (nowItemdata.itemCount > 0)
            {
                int itemPrice = GameDataMgr.Instance.GetItemInfo(nowItemdata.itemId).price;
                int playerMoney = GameDataMgr.Instance.playerItemData.money;
                if (playerMoney >= itemPrice)
                {
                    GameDataMgr.Instance.playerItemData.money -= itemPrice;
                    nowItemdata.itemCount--;
                    GameDataMgr.Instance.AddPlayerItemData(nowItemdata.itemId, 1);
                    UpdateItemList(storeType);
                }
                else
                {
                    UIManager.Instance.ShowPanel<InfoPanel>().SetInfoText("米不够哩");
                }

            }
            else
            {
                UIManager.Instance.ShowPanel<InfoPanel>().SetInfoText("商品已售空");
            }
        });
        cancelBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<StorePanel>();
        });
        UpdateItemList(storeType);
        SelectFirstStoreItem();
    }

    /// <summary>
    /// 更新项目
    /// </summary>
    public void UpdateItemList(Store_Type type)
    {
        ClearPool();
        nowGold.text = GameDataMgr.Instance.playerItemData.money.ToString();
        List<ItemData> itemInfos = GameDataMgr.Instance.GetStoreItemListById(type.ToString());
        foreach (var item in itemInfos)
        {
            StoreItem storeItem = Instantiate(Resources.Load<StoreItem>("Item/StoreItem"), fatherTrans);
            pool.Add(storeItem);
            storeItem.SetItem(item);
        }
    }

    public void ClearPool()
    {
        foreach (var item in pool)
        {
            Destroy(item.gameObject);
        }
        pool.Clear();
    }

    public void SelectFirstStoreItem()
    {
        if(pool.Count != 0)
        {
            nowItemdata = pool[0].GetItemData();
            Item item = GameDataMgr.Instance.GetItemInfo(nowItemdata.itemId);
            cont.SetItem(Resources.Load<Sprite>("Icon/" + item.icon+ "_icon"), item.tips, item.price.ToString() + "$");
        }
    }

    
}
