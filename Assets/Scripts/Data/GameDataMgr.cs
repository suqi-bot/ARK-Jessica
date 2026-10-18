using LitJson;
using System.Collections.Generic;
using UnityEngine;


public class GameDataMgr
{
    private static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;


    public AudioData audioData;// 音效相关数据
    public PlayerItemData playerItemData;// 玩家物品相关数据
    public StoreItemData storeItemData;// 商店物品相关数据
    public Dictionary<int, Item> itemInfos = new Dictionary<int, Item>(); // 物品字典
    public Dictionary<string, int> targetProgress = new Dictionary<string, int>();// 任务目标进度
    public TaskCfg taskCfg = new TaskCfg();// 任务数据
    public TaskData taskData = new TaskData();// 任务列表
    private GameDataMgr()
    {
        // 初始化一些初始化数据
        audioData = JsonMgr.Instance.LoadData<AudioData>("MusicData");
        storeItemData = JsonMapper.ToObject<StoreItemData>(Resources.Load("Json/StoreData").ToString());
        ItemDetailed items = JsonUtility.FromJson<ItemDetailed>(Resources.Load("Json/ItemData").ToString());
        
        foreach (var item in items.info)
        {
            itemInfos.Add(item.id, item);
        }
        taskCfg.LoadCfg();
        taskData = JsonMgr.Instance.LoadData<TaskData>("TaskDataList");
        playerItemData = JsonMgr.Instance.LoadData<PlayerItemData>("PlayerItemData");
        taskData.AddDataOrUpdate(new TaskDataItem() { task_chain_id = 1, task_sub_id = 1, progress = 0, task_target = "enemy_1007_slime", award_is_get = 0 });
        playerItemData.weapens.Add(new ItemData() { itemId = 2, itemCount = 1 });
        playerItemData.items.Add(new ItemData() { itemId = 1, itemCount = 10 });
    }

    /// <summary>
    /// 存储音效数据
    /// </summary>
    public void SaveMusicData()
    {
        JsonMgr.Instance.SaveData(audioData, "MusicData");
    }
    /// <summary>
    /// 存储背包资源
    /// </summary>
    public void SavePlayerItemData()
    {
        JsonMgr.Instance.SaveData(playerItemData, "PlayerItemData");
    }
    /// <summary>
    /// 存储任务列表
    /// </summary>
    public void SaveTaskData()
    {
        JsonMgr.Instance.SaveData(taskData, "TaskDataList");
    }
    /// <summary>
    /// 存储商店信息
    /// </summary>
    public void SaveStoreData()
    {
        JsonMgr.Instance.SaveData(storeItemData, "StoreItemData");
    }


    List<ItemData> tempList = new List<ItemData>();
    /// <summary>
    /// 移除道具
    /// </summary>
    /// <param name="id"></param>
    /// <param name="type"></param>
    public void RemovePlayerItemData(int id, int type)
    {
        foreach (var item in GetItemInfoList(type))
        {
            if (item.itemId == id)
            {
                tempList.Remove(item);
                return;
            }
        }
    }

    /// <summary>
    /// 添加物品
    /// </summary>
    /// <param name="id"></param>
    /// <param name="num"></param>
    public void AddPlayerItemData(int id, int num)
    {
        foreach (var item in GetItemInfoList(id))
        {
            if (item.itemId == id)
            {
                item.itemCount += num;
                return;
            }
        }
        GetItemInfoList(id).Add(new ItemData() { itemId = id, itemCount = num });
    }

    /// <summary>
    /// 获取不同种类的ItemInfo列表
    /// </summary>
    /// <param name="id">0武器、1物品、2投掷物、3装备</param>
    /// <returns></returns>
    public List<ItemData> GetItemInfoList(int id)
    {
        Item_Type type = (Item_Type)itemInfos[id].type;
        switch (type)
        {
            case Item_Type.Weapon:
                tempList = playerItemData.weapens;
                break;
            case Item_Type.Item:
                tempList = playerItemData.items;
                break;
            case Item_Type.Projectile:
                break;
            case Item_Type.Equip:
                tempList = playerItemData.equips;
                break;
        }
        return tempList;
    }

    /// <summary>
    /// 根据道具ID 获取道具详细信息
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public Item GetItemInfo(int id)
    {
        if (itemInfos.ContainsKey(id))
            return itemInfos[id];
        return null;
    }

    /// <summary>
    /// 添加或者更新进度
    /// </summary>
    /// <param name="target"></param>
    /// <param name="progress"></param>
    public void AddOrUpdateTargetProgress(string target, int progress)
    {
        Debug.Log(target + "progress：" + progress);
        if (targetProgress.ContainsKey(target))
            targetProgress[target] += progress;
        else
            targetProgress.Add(target, progress);
        progress = targetProgress[target];
        foreach (var item in taskData.SearchItemsByTaskname(target))
        {
            TaskLogic.Instance.AddProgress(item.task_chain_id, item.task_sub_id, progress, (i, b) =>
            {
                string name = taskCfg.GetCfgItem(item.task_chain_id, item.task_sub_id).desc;
                if (i == 0 && b)
                    UIManager.Instance.ShowPanel<InfoPanel>().SetInfoText(name.Split(":")[0] + "任务完成");
            });
        }
    }
    /// <summary>
    /// 获取目标进度
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public int GetTargetProgress(string target)
    {
        if (targetProgress.ContainsKey(target))
            return targetProgress[target];
        return 0;
    }
    /// <summary>
    /// 获取商店项目
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public List<ItemData> GetStoreItemListById(string id)
    {
        return storeItemData.storeItem[id];
    }


}
