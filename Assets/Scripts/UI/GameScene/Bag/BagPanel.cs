using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BagPanel : BasePanel
{
    public Button closeBtn;//关闭按钮
    public Transform contextFather;//格子父对象
    PlayerItemData playerItem;//获取背包集合类
    public List<GameObject> items;//生成物品数

    public ItemOc itemOc;//物品操作类
    public ItemBar itemBar;//物品描述类
    public Text itemGold; //金钱数
    public Player player;//玩家类
    public override void Init()
    {
        closeBtn.onClick.AddListener(() =>
        {
            UIManager.Instance.HidePanel<BagPanel>();
        });
        PlayerBlackboard blackboard = PlayerStateMgr.Instance.getFsm().blackBoard as PlayerBlackboard;
        player = blackboard.player;
        playerItem = GameDataMgr.Instance.playerItemData;
        itemGold.text = GameDataMgr.Instance.playerItemData.money.ToString();
        UpdateBagItem();
        ClickItemEvent(items[0].GetComponent<ItemGrid>().getId(), items[0].GetComponent<ItemGrid>().getNum());
    }
    
    /// <summary>
    /// 更新背包物品状态
    /// </summary>
    public void UpdateBagItem()
    {
        if(items != null)
        {
            foreach (var item in items)
            {
                Destroy(item);
            }
            items.Clear();
        }
        
        CreateItem(playerItem.weapens);
        CreateItem(playerItem.items);
    }

    /// <summary>
    /// 创建物品
    /// </summary>
    /// <param name="list"></param>
    public void CreateItem(List<ItemData> list)
    {
        if(list == null)
        {
            return;
        }
        foreach (var item in list)
        {
            GameObject temp = Instantiate(Resources.Load<GameObject>("Item/ItemGrid"), contextFather);
            temp.GetComponent<ItemGrid>().SetItem(item.itemId, Resources.Load<Sprite>("Icon/"+GameDataMgr.Instance.GetItemInfo(item.itemId).icon+"_icon"), item.itemCount);
            items.Add(temp);
        }
    }
    /// <summary>
    /// 丢弃物品
    /// </summary>
    public void RemoveBagItem(int id, int type)
    {
        GameDataMgr.Instance.RemovePlayerItemData(id, type);
        UpdateBagItem();
        ClickItemEvent(items[0].GetComponent<ItemGrid>().getId(), items[0].GetComponent<ItemGrid>().getNum());
    }
    int tempCount;
    bool isUsed;
    /// <summary>
    /// 使用物品
    /// </summary>
    public void UseBagItem(IUsed user, int id)
    {
        isUsed = user.Used(player);
        if (!isUsed)
        {
            return;
        }
        List<ItemData> temp = GameDataMgr.Instance.GetItemInfoList(id);
        foreach (var item in temp)
        {
            if (item.itemId == id)
            {
                tempCount = item.itemCount;
            }
        }
        ClickItemEvent(id, tempCount--);
    }

    Item item;
    /// <summary>
    /// 单个项目点击后触发的事件方法
    /// </summary>
    public void ClickItemEvent(int id, int num)
    {
        if (num == 0)
        {
            foreach (var item in items)
            {
                if(item.GetComponent<ItemGrid>().getId() == id)
                {
                    Destroy(item.gameObject);
                    RemoveBagItem(id, GameDataMgr.Instance.GetItemInfo(id).type);
                    ClickItemEvent(items[0].GetComponent<ItemGrid>().getId(), items[0].GetComponent<ItemGrid>().getNum());
                    return;
                }
            }
        }
        item = GameDataMgr.Instance.GetItemInfo(id);
        itemBar.ShowIcon(item, num);
        itemOc.SetId(id , GameDataMgr.Instance.GetItemInfo(id).type, GameDataMgr.Instance.GetItemInfo(id).icon);
        Item_Type type = (Item_Type)GameDataMgr.Instance.GetItemInfo(id).type;
        Debug.Log(type);
        switch (type)
        {
            case Item_Type.Weapon:
            case Item_Type.Equip:
                itemOc.HideMe();
                break;
            case Item_Type.Item:
            case Item_Type.Projectile:
                itemOc.ShowMe();
                break;
        }
    }
}
